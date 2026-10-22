using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace KL.Drawing;

/// <summary>
/// TornadoLayered.fx 中的一层，对应原本的一次 TornadoEffect + DrawInWorld。
/// </summary>
public readonly struct TornadoLayer
{
    /// <summary>主要材质（同一次绘制最多两张不同贴图）</summary>
    public Texture2D Texture { get; init; }
    /// <summary>原本 DrawInWorld 的顶点颜色</summary>
    public Color Color { get; init; }
    /// <summary>原本 DrawInWorld 的缩放，矩形尺寸 = 贴图尺寸 * Scale</summary>
    public Vector2 Scale { get; init; }
    /// <summary>原本 TornadoEffect 的 effectColor</summary>
    public Vector4 EffectColor { get; init; }
    /// <summary>贴图倾斜度，同时影响置换噪声，因此也决定轮廓</summary>
    public float Tilt { get; init; }
    public bool UseRForAlpha { get; init; }
    /// <summary>true 等价于 Additive 混合，false 等价于 NonPremultiplied 混合</summary>
    public bool Additive { get; init; }
    /// <summary>大于 0 时按贴图亮度 * RampGain 从 TornadoLayerShape.RampTexture 取色（再乘 Color），0 为直接用 贴图 * Color</summary>
    public float RampGain { get; init; }
    /// <summary>消融边缘画 TornadoLayerShape.DissolveEdgeColor 烧边</summary>
    public bool BurnEdge { get; init; }

    public TornadoLayer(Texture2D texture, Color color, Vector2 scale)
    {
        Texture = texture;
        Color = color;
        Scale = scale;
        EffectColor = Vector4.One;
        Tilt = 2f;
    }
}

/// <summary>
/// 一组叠层共享的形状参数，含义与默认值同 DrawHelper.TornadoEffect（不含消融描边）。
/// </summary>
public sealed class TornadoLayerShape
{
    public float Time;
    public float CameraZ = 3.5f;
    public Vector2 DrawScale = Vector2.One;
    public float RotationSpeed = 0.2f;
    public Vector2 TexScale = Vector2.One;

    public Texture2D DissolveTexture;
    public Vector2 DissolveTexTiling = Vector2.One;
    public float DissolveThreshold;
    /// <summary>消融烧边宽度，0 关闭；只作用于 BurnEdge 的层</summary>
    public float DissolveEdgeWidth;
    /// <summary>烧边颜色（rgb=颜色，a=透明度倍率）</summary>
    public Vector4 DissolveEdgeColor = new(1.0f, 0.6f, 0.2f, 1.0f);

    /// <summary>色带贴图（横向 0~1 对应贴图亮度，如 256x1 的 PNG），用于 RampGain 大于 0 的层</summary>
    public Texture2D RampTexture;

    public float HorizontalFadeRange = 0.02f;
    public float VerticalFadeRange = 0.02f;

    public Texture2D NoiseTexture;
    public float DisplaceAmount = 0.05f;
    public Vector2 DisplaceNoiseScale = Vector2.One;
    public Vector2 DisplaceNoiseSpeed = new(0.1f);
    public float DisplaceNoiseContrast = 1f;
    public float DisplaceNoiseBias;

    public float MaxInset = 0.25f;
    public Vector2 BezierP0 = new(-1f, 0f);
    public Vector2 BezierP1 = new(-1f, 0.25f);
    public Vector2 BezierP2 = new(-1f, 0.75f);
    public Vector2 BezierP3 = new(-1f, 1f);
}

/// <summary>
/// 把形状参数相同的多层龙卷风合并成一次绘制。shader 内按顺序合成后输出预乘颜色，
/// 结果与逐层使用 NonPremultiplied / Additive 绘制相同，但只写一次像素。
/// </summary>
public static class TornadoLayerRenderer
{
    public const int MaxLayers = 4;

    private static readonly Vector4[] LayerTint = new Vector4[MaxLayers];
    private static readonly Vector4[] LayerEffectColor = new Vector4[MaxLayers];
    private static readonly Vector4[] LayerParams = new Vector4[MaxLayers];
    private static readonly Vector4[] LayerShape = new Vector4[MaxLayers];
    private static readonly Vector4[] LayerRamp = new Vector4[MaxLayers];

    /// <summary>
    /// 需在 SpriteSortMode.Immediate、BlendState.AlphaBlend 的 SpriteBatch 中调用，
    /// 例如 EndBeginDraw(0, 1, ss: SamplerState.LinearWrap)。所有层以 position 为中心。
    /// </summary>
    public static void Draw(SpriteBatch spriteBatch, Effect effect, Vector2 position, TornadoLayerShape shape,
        ReadOnlySpan<TornadoLayer> layers)
    {
        if (layers.Length is 0 or > MaxLayers)
            throw new ArgumentOutOfRangeException(nameof(layers), $"TornadoLayered 支持 1~{MaxLayers} 层。");

        Texture2D slot0 = layers[0].Texture;
        Texture2D slot1 = null;
        Vector2 drawSize = Vector2.Zero;
        foreach (TornadoLayer layer in layers)
            drawSize = Vector2.Max(drawSize, Size(layer.Texture) * layer.Scale);

        for (int i = 0; i < MaxLayers; i++)
        {
            if (i >= layers.Length)
            {
                LayerParams[i] = Vector4.Zero;
                LayerShape[i] = Vector4.One;
                LayerRamp[i] = Vector4.Zero;
                continue;
            }

            TornadoLayer layer = layers[i];
            float textureSlot = 0f;
            if (layer.Texture != slot0)
            {
                if (slot1 != null && layer.Texture != slot1)
                    throw new ArgumentException("TornadoLayered 同一次绘制最多使用两张不同贴图。", nameof(layers));
                slot1 = layer.Texture;
                textureSlot = 1f;
            }

            Vector2 groupScale = Size(layer.Texture) * layer.Scale / drawSize;
            LayerTint[i] = layer.Color.ToVector4();
            LayerEffectColor[i] = layer.EffectColor;
            LayerParams[i] = new Vector4(layer.UseRForAlpha ? 1f : 0f, layer.Additive ? 1f : 0f, textureSlot, 1f);
            LayerShape[i] = new Vector4(groupScale, layer.Tilt, 0f);
            LayerRamp[i] = new Vector4(layer.RampGain > 0f ? 1f : 0f, layer.RampGain, layer.BurnEdge ? 1f : 0f, 0f);
            if (layer.RampGain > 0f && shape.RampTexture == null)
                throw new ArgumentException("使用火焰色带的层需要 TornadoLayerShape.RampTexture。", nameof(layers));
        }

        SetShapeParameters(effect, shape);
        effect.Parameters["LayerTint"].SetValue(LayerTint);
        effect.Parameters["LayerEffectColor"].SetValue(LayerEffectColor);
        effect.Parameters["LayerParams"].SetValue(LayerParams);
        effect.Parameters["LayerShape"].SetValue(LayerShape);
        effect.Parameters["LayerRamp"].SetValue(LayerRamp);

        GraphicsDevice device = spriteBatch.GraphicsDevice;
        effect.Parameters["ClampLayers"].SetValue(TargetClampsOutput(device) ? 1f : 0f);
        device.Textures[1] = shape.DissolveTexture;
        device.Textures[2] = shape.NoiseTexture;
        device.Textures[3] = slot1;
        device.SamplerStates[3] = device.SamplerStates[0];
        device.Textures[4] = shape.RampTexture;
        device.SamplerStates[4] = SamplerState.LinearClamp;
        effect.CurrentTechnique.Passes[0].Apply();

        spriteBatch.Draw(slot0, position, null, Color.White, 0f, Size(slot0) / 2f, drawSize / Size(slot0),
            SpriteEffects.None, 0f);
        device.Textures[3] = null;
        device.Textures[4] = null;
    }

    private static void SetShapeParameters(Effect effect, TornadoLayerShape shape)
    {
        float cameraZ2 = shape.CameraZ * shape.CameraZ;
        float displaceAmount = shape.NoiseTexture == null ? 0f : shape.DisplaceAmount;
        float dissolveThreshold = shape.DissolveTexture == null ? 0f : shape.DissolveThreshold;
        Vector2 p0 = shape.BezierP0, p1 = shape.BezierP1, p2 = shape.BezierP2, p3 = shape.BezierP3;
        float bezierHeight = p3.Y - p0.Y;
        bool bezierEnabled = Math.Abs(bezierHeight) > 0.0001f;

        effect.Parameters["BezierX"].SetValue(new Vector4(p0.X, p1.X, p2.X, p3.X) * shape.MaxInset);
        effect.Parameters["BezierY"].SetValue(new Vector4(p0.Y, p1.Y, p2.Y, p3.Y));
        effect.Parameters["BezierDY"].SetValue(new Vector3(p1.Y - p0.Y, p2.Y - p1.Y, p3.Y - p2.Y) * 3f);
        effect.Parameters["BezierRange"].SetValue(new Vector4(Math.Min(p0.Y, p3.Y), Math.Max(p0.Y, p3.Y),
            bezierEnabled ? 1f / bezierHeight : 0f, bezierEnabled ? 1f : 0f));

        effect.Parameters["RotationOffset"].SetValue(shape.Time * shape.RotationSpeed);
        effect.Parameters["Displace"].SetValue(new Vector3(displaceAmount,
            Math.Max(0f, shape.DisplaceNoiseContrast - 1f), shape.DisplaceNoiseBias));
        effect.Parameters["DisplaceNoise"].SetValue(new Vector4(shape.DisplaceNoiseScale, 0f, 0f)
            + new Vector4(0f, 0f, shape.DisplaceNoiseSpeed.X, shape.DisplaceNoiseSpeed.Y) * shape.Time);

        effect.Parameters["Cylinder"].SetValue(new Vector4(shape.CameraZ, cameraZ2, cameraZ2 - 1f,
            shape.CameraZ / MathF.Sqrt(Math.Max(cameraZ2 - 1f, 1e-6f))));
        effect.Parameters["ProjectScale"].SetValue(new Vector2(2f) / shape.DrawScale);
        effect.Parameters["Fade"].SetValue(new Vector4(
            shape.HorizontalFadeRange > 0.0001f ? 1f / shape.HorizontalFadeRange : 0f,
            shape.VerticalFadeRange > 0.0001f ? 1f / shape.VerticalFadeRange : 0f,
            shape.DrawScale.X * 0.5f, shape.DrawScale.Y * 0.5f));

        effect.Parameters["TexScale"].SetValue(shape.TexScale);
        effect.Parameters["DissolveTexMultiplier"].SetValue(shape.DissolveTexTiling);
        effect.Parameters["DissolveThreshold"].SetValue(dissolveThreshold);
        // 不消融时烧边宽度必须为 0，shader 依赖这一点关闭烧边
        effect.Parameters["DissolveEdgeWidth"].SetValue(dissolveThreshold > 0.0001f ? shape.DissolveEdgeWidth : 0f);
        effect.Parameters["DissolveEdgeColor"].SetValue(shape.DissolveEdgeColor);
    }

    /// <summary>
    /// 当前目标是否为 8 位格式（关闭 Bloom 时直接画到场景上）。8 位目标会把每次绘制的输出截断到 0~1，
    /// 合并绘制只能在 shader 内模拟逐层截断，无法模拟混合后对目标颜色的截断。
    /// </summary>
    public static bool TargetClampsOutput(GraphicsDevice device)
    {
        RenderTargetBinding[] targets = device.GetRenderTargets();
        return targets.Length == 0 || targets[0].RenderTarget is not RenderTarget2D
        {
            Format: SurfaceFormat.Vector4 or SurfaceFormat.HalfVector4 or SurfaceFormat.HdrBlendable
        };
    }

    private static Vector2 Size(Texture2D texture) => new(texture.Width, texture.Height);
}
