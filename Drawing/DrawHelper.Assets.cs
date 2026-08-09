using System.Linq;

namespace KL.Drawing;

public partial class DrawHelper : ModSystem
{
    public const int AlphaBlend = 0;
    public const int Additive = 1;
    public const int NonPremultiplied = 2;
    
    //消融，裁切shader
    static Effect commonMagic = null!;

    static Effect reColor = null!;
    
    static Effect airDistortion = null!;

    //消融，裁切shader
    static Effect clip = null!;
    
    //顶点绘制shader
    static Effect vertexDraw = null!;
    static Effect vertexDraw2 = null!;
    static Effect iceConeVertexDraw = null!;
    
    //圆角基本图形相关绘制

    static Effect basicShapeDraw = null!;
    //扰动shader
    static Effect heatNoisePerturb = null!;
    //径向模糊shader
    private static Effect radialBlur = null!;
    //Bloom外发光
    private static Effect bloom = null!;
    
    //将材质映射为球体
    private static Effect spherePerspective = null!;
    
    //将材质映射为球体
    private static Effect sphereEffect = null!;
    
    //缩放图片
    private static Effect scaleEffect = null!;

    //用于灰度图，将r值作为透明度使用
    public static Effect NoBlack = null!;
    
    //用于灰度图，将白色反转为黑色
    static Effect noWhite = null!;
    
    //屏幕折射，使用法线图来制作玻璃折射的效果
    static Effect screenRefraction = null!;
    
    //龙卷风effect
    static Effect tornado = null!;

    //旋转effect
    static Effect spinEffect = null!;
    
    //进度圈effect
    static Effect circleBar = null!;
    
    //雷电effect
    static Effect lightning = null!;
    
    //径向溶解effect
    static Effect radialDissolve = null!;
    
    //冻结effect
    internal static Effect frozen = null!;
    
    //黑白shader,很简单的shader无参数shader所以直接用。
    public static Effect grey = null!;
    
    
    //更好的柏林噪声
    public static Texture2D PerLinNoiseX = null!;
    
    //用于空气扭曲
    public static Texture2D 空间扭曲角度;
    
    public static Texture2D LightBloom;

    
    public static BlendState AlphaBlendNormal = new BlendState()//配置透明度保留状态
    {
        AlphaBlendFunction = BlendState.AlphaBlend.AlphaBlendFunction,
        AlphaDestinationBlend = BlendState.AlphaBlend.AlphaDestinationBlend,
        AlphaSourceBlend = BlendState.AlphaBlend.AlphaSourceBlend,
        ColorBlendFunction = (BlendFunction)0,
        ColorDestinationBlend = (Blend)5,
        ColorSourceBlend = BlendState.Additive.ColorSourceBlend,
        ColorWriteChannels = ColorWriteChannels.All,
        ColorWriteChannels1 = ColorWriteChannels.All,
        ColorWriteChannels2 = ColorWriteChannels.All,
        ColorWriteChannels3 = ColorWriteChannels.All,
        BlendFactor = Color.White,
        MultiSampleMask = -1
    };
    public static BlendState ReverseBS = new BlendState()//配置反色混合状态
    {
        AlphaBlendFunction = BlendState.Additive.AlphaBlendFunction,
        AlphaDestinationBlend = BlendState.Additive.AlphaDestinationBlend,
        AlphaSourceBlend = BlendState.Additive.AlphaSourceBlend,
        ColorBlendFunction = BlendFunction.ReverseSubtract,
        ColorDestinationBlend = BlendState.Additive.ColorDestinationBlend,
        ColorSourceBlend = BlendState.Additive.ColorSourceBlend,
        ColorWriteChannels = ColorWriteChannels.All,
        ColorWriteChannels1 = ColorWriteChannels.All,
        ColorWriteChannels2 = ColorWriteChannels.All,
        ColorWriteChannels3 = ColorWriteChannels.All,
        BlendFactor = Color.White,
        MultiSampleMask = -1
    };
    
    // 滤色混合模式（Screen）
    public static BlendState ScreenBlend = new BlendState()
    {
        AlphaBlendFunction = BlendFunction.Add,
        AlphaDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.One,
        ColorBlendFunction = BlendFunction.Add,
        ColorDestinationBlend = Blend.InverseSourceColor,
        ColorSourceBlend = Blend.One,
        ColorWriteChannels = ColorWriteChannels.All,
        ColorWriteChannels1 = ColorWriteChannels.All,
        ColorWriteChannels2 = ColorWriteChannels.All,
        ColorWriteChannels3 = ColorWriteChannels.All,
        BlendFactor = Color.White,
        MultiSampleMask = -1
    };
    #region 顶点结构体
    private struct CustomVertexInfo : IVertexType
    {
        private static VertexDeclaration _vertexDeclaration = new VertexDeclaration(new VertexElement[3]
        {
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
        });
        public Vector2 Position;
        public Color Color;
        public Vector3 TexCoord;

        public CustomVertexInfo(Vector2 position, Color color, Vector3 texCoord)
        {
            Position = position;
            Color = color;
            TexCoord = texCoord;
        }
        public CustomVertexInfo(Vector2 position, Vector4 color, Vector3 texCoord)
        {
            Position = position;
            Color = new Color(color.X, color.Y, color.Z, color.W)*255f;
            TexCoord = texCoord;
        }

        public VertexDeclaration VertexDeclaration
        {
            get
            {
                return _vertexDeclaration;
            }
        }
    }
    #endregion

    public static SpriteEffects GetCorrectSpriteEffect(Player player)
    {
        return (player.direction > 0) ? SpriteEffects.None | (player.gravDir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically) : SpriteEffects.FlipHorizontally | (player.gravDir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
    }
    
    public override void Load()
    {
        //Effects
        AutoLoadEffet();
        
        //Textures
        PerLinNoiseX = Mod.Assets.Request<Texture2D>("Effects/Tex/PerlinX", AssetRequestMode.ImmediateLoad).Value;
        空间扭曲角度 = Mod.Assets.Request<Texture2D>("Effects/Tex/空间扭曲角度", AssetRequestMode.ImmediateLoad).Value;
        LightBloom = Mod.Assets.Request<Texture2D>("Effects/Tex/LightBloom", AssetRequestMode.ImmediateLoad).Value;
        
        
        base.Load();
    }

    //自动加载所有声明的effect
    void AutoLoadEffet()
    {
        // 只获取Effect类型的字段
        FieldInfo[] effectFields = typeof(DrawHelper).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(Effect))
            .ToArray();

        foreach (FieldInfo field in effectFields)
        {
            string fieldName = field.Name;
            // 去掉开头的斜杠
            if (fieldName.StartsWith("_"))
                fieldName = fieldName.Substring(1);
            
            // 首字母大写
            if (fieldName.Length > 0)
                fieldName = char.ToUpper(fieldName[0]) + fieldName.Substring(1);

            // 使用EffectExtentions.LoadEffect加载特效
            Effect effect = EffectExtentions.LoadEffect(fieldName);
            
            // 设置字段值
            field.SetValue(null, effect);
        }
    }
    
    public struct TextureInfo
    {
        public Texture2D Texture;

        public int CurrentFrame = 0;

        public int XFrames = 1;
        
        public int YFrames = 1;
        
        public Vector2 OriginOffset = Vector2.Zero;

        public TextureInfo(Texture2D texture,int currentFrame = 0, int xFrames = 1,int yFrames = 1,Vector2 originOffset = default)
        {
            Texture = texture;
            CurrentFrame = currentFrame;
            XFrames = xFrames;
            YFrames = yFrames;
            OriginOffset = originOffset;
        }
    }
    //衰减类型，smoothStep和lerp
    public enum DecayType
    {
        Lerp,
        SmoothStep,
    }
    
    /// <summary>
    /// 用于径向模糊
    /// </summary>
    public class RadialBlurInfo
    {
        public DecayType Decay = DecayType.Lerp;
        public float Strength = 0.004f;
        public int TotalFrames = 16;
        public float CurrentFrame = 0;
        public bool Active = false;
        public int Iterations = 30;
        public Vector2 Position = Vector2.Zero;

        public RadialBlurInfo(Vector2 position,float strength = 0.004f, int totalFrames = 16, DecayType decay = DecayType.Lerp,int iterations = 30)
        {
            Active = true;
            Position = position;
            Strength = strength;
            TotalFrames = totalFrames;
            Decay = decay;
            Iterations = iterations;
        }

        public void Update()
        {
            if(!Active)return;
            CurrentFrame++;
            if (CurrentFrame >= TotalFrames)
            {
                Active = false;
            }
        }
    }
    
    public class RadialWaveWarp
    {
        public int TotalFrames = 16;
        public float CurrentFrame = 0;
        public bool Active = false;
        public float Radius = 200;
        public float MinRadius = 50;
        public float MaxRadius = 200;
        public Vector2 Position = Vector2.Zero;

        
        //本质为放大镜，放大倍率
        public float MaxScale = 2.5f;

        public RadialWaveWarp(Vector2 position, float minRadius = 50, float maxRadius = 200, int totalFrames = 16, float maxScale = 2.5f)
        {
            Active = true;
            Position = position;
            MinRadius = minRadius;
            MaxRadius = maxRadius;
            TotalFrames = totalFrames;
            MaxScale = maxScale;
        }

        public void Update()
        {
            if(!Active)return;
            CurrentFrame++;
            if (CurrentFrame >= TotalFrames)
            {
                Active = false;
            }
        }

    }

    /// <summary>
    /// Tween缓动类型。
    /// Linear：匀速变化，适合持续移动、旋转。
    /// SmoothStep / EaseInOut：两头慢中间快，适合自然出现/消失。
    /// EaseIn：开始慢后面快，适合蓄力后收束。
    /// EaseOut：开始快后面慢，适合冲击、扩散、弹幕生成。
    /// ExpoOut：爆发感强，适合瞬间放大、闪光。
    /// BackOut：会略微超过目标再回弹，适合UI按钮、法术弹出场。
    /// </summary>
    public enum TweenEase
    {
        Linear,
        SmoothStep,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        SineIn,
        SineOut,
        SineInOut,
        ExpoOut,
        BackOut
    }

    public enum FrameType
    {
        Lerp,
        SmoothStep
    }

    /// <summary>
    /// 连续帧动画的一个片段：在Duration帧内从MinValue过渡到MaxValue。
    /// 用法示例：
    /// List&lt;FrameInfo&gt; scaleFrames =
    /// [
    ///     new(0f, 1.2f, 8, TweenEase.BackOut), // 前8帧从0放大到1.2，并带一点回弹感
    ///     new(1.2f, 1f, 6, TweenEase.SmoothStep), // 接下来6帧从1.2收回到1
    ///     new(1f, 0f, 10, TweenEase.SineIn) // 最后10帧淡出/缩小到0
    /// ];
    /// float scale = DrawHelper.EvaluateTween(scaleFrames, time);
    ///
    /// 注意：Duration是“这一段持续多少帧”，不是绝对结束帧。
    /// 上例总时长是8 + 6 + 10 = 24帧。
    /// </summary>
    public class FrameInfo
    {
        public float MinValue = 0;
        public float MaxValue = 1;
        public int Duration = 10;
        public TweenEase Ease = TweenEase.Linear;

        /// <summary>
        /// 兼容旧代码：旧字段名EndFrame现在表示这一段的持续帧数。
        /// </summary>
        public int EndFrame
        {
            get => Duration;
            set => Duration = value;
        }

        /// <summary>
        /// 兼容旧代码：Lerp/SmoothStep映射到新的TweenEase。
        /// </summary>
        public FrameType Type
        {
            get => Ease == TweenEase.SmoothStep ? FrameType.SmoothStep : FrameType.Lerp;
            set => Ease = value == FrameType.SmoothStep ? TweenEase.SmoothStep : TweenEase.Linear;
        }

        public FrameInfo(float minValue, float maxValue, int duration, FrameType type = FrameType.Lerp)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Duration = duration;
            Type = type;
        }

        public FrameInfo(float minValue, float maxValue, int duration, TweenEase ease)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Duration = duration;
            Ease = ease;
        }
    }
    
    public struct HeatNoiseInfo
    {
        public bool Enable = false;

        public Texture2D Noise = null;
        
        public float Strength = 0.2f;
        
        public Vector2 TexScale = Vector2.One;

        public Vector2 uTime = Vector2.Zero;
        
        public HeatNoiseInfo(bool enable, Texture2D noise, float strength=0.2f, Vector2? texScale=null, Vector2 uTime = default)
        {
            texScale??= Vector2.One;
            
            Enable = enable;
            Noise = noise;
            Strength = strength;
            TexScale = texScale.Value;
            this.uTime = uTime;
        }
    }
    
    public struct ClipInfo
    {
        public bool Enable = false;

        public Texture2D Noise = null;
        
        public float Threshold = 0.2f;
        
        public float Edge = 0.0f;
        
        public Color EdgeColor = Color.White;

        
        public Vector2 TexScale = Vector2.One;

        public Vector2 uTime = Vector2.Zero;

        public ClipInfo(bool enable, Texture2D noise, float threshold = 0.2f, float edge = 0.0f,Color edgeColor = default,
            Vector2? texScale = null, Vector2 uTime = default)
        {
            texScale??= Vector2.One;
            if(edgeColor == default)edgeColor = Color.White;
            EdgeColor = edgeColor;
            
            Enable = enable;
            Noise = noise;
            Threshold = threshold;
            Edge = edge;
            TexScale = texScale.Value;
            this.uTime = uTime;
        }
    }

    /// <summary>
    /// 旧接口：按帧求连续Tween值。新代码更推荐直接使用EvaluateTween。
    /// 用法：
    /// float alpha = DrawHelper.GetFrameValue(alphaFrames, time, clamp: true);
    /// </summary>
    public static float GetFrameValue(List<FrameInfo> frameInfos, int currentFrame, int startFrame = 0, bool clamp = false)
    {
        return EvaluateTween(frameInfos, currentFrame, startFrame, clamp);
    }

    /// <summary>
    /// 计算某一帧对应的Tween值。
    /// frameInfos：按顺序填写多个动画片段。
    /// currentFrame：当前计时帧，通常传Projectile.ai计时、time、timer等。
    /// startFrame：延迟到第几帧开始播放；例如startFrame=20表示前20帧保持第一个MinValue。
    /// clamp：为true时，动画结束后保持最后一段MaxValue；通常建议保持true。
    /// 用法示例：
    /// List&lt;FrameInfo&gt; alphaFrames =
    /// [
    ///     new(0f, 1f, 6, TweenEase.SineOut),
    ///     new(1f, 1f, 12),
    ///     new(1f, 0f, 10, TweenEase.SineIn)
    /// ];
    /// float alpha = DrawHelper.EvaluateTween(alphaFrames, time);
    /// </summary>
    public static float EvaluateTween(IReadOnlyList<FrameInfo> frameInfos, int currentFrame, int startFrame = 0, bool clamp = true)
    {
        if (frameInfos == null || frameInfos.Count == 0)
        {
            return 0f;
        }

        int localFrame = currentFrame - startFrame;
        if (localFrame <= 0)
        {
            return frameInfos[0].MinValue;
        }

        int frameCursor = 0;
        FrameInfo lastValidFrameInfo = frameInfos[0];
        foreach (FrameInfo frameInfo in frameInfos)
        {
            int duration = Math.Max(frameInfo.Duration, 1);
            int nextFrame = frameCursor + duration;
            lastValidFrameInfo = frameInfo;

            if (localFrame <= nextFrame)
            {
                float t = (localFrame - frameCursor) / (float)duration;
                if (clamp)
                {
                    t = MathHelper.Clamp(t, 0f, 1f);
                }

                return MathHelper.Lerp(frameInfo.MinValue, frameInfo.MaxValue, ApplyEase(t, frameInfo.Ease));
            }

            frameCursor = nextFrame;
        }

        return clamp ? lastValidFrameInfo.MaxValue : EvaluateTweenSegment(lastValidFrameInfo, localFrame - frameCursor);
    }

    /// <summary>
    /// 只计算单个Tween片段的值，适合临时测试某一种Ease曲线。
    /// 用法：float value = DrawHelper.EvaluateTweenSegment(new FrameInfo(0f, 1f, 20, TweenEase.ExpoOut), time);
    /// </summary>
    public static float EvaluateTweenSegment(FrameInfo frameInfo, int frame)
    {
        int duration = Math.Max(frameInfo.Duration, 1);
        float t = MathHelper.Clamp(frame / (float)duration, 0f, 1f);
        return MathHelper.Lerp(frameInfo.MinValue, frameInfo.MaxValue, ApplyEase(t, frameInfo.Ease));
    }

    /// <summary>
    /// 将0到1的线性进度转换成指定缓动曲线的进度。
    /// 一般不需要直接调用，EvaluateTween内部会自动使用。
    /// 如果要手动插值，也可以这样用：
    /// float t = DrawHelper.ApplyEase(rawProgress, TweenEase.EaseOutCubic);
    /// float value = MathHelper.Lerp(start, end, t);
    /// </summary>
    public static float ApplyEase(float t, TweenEase ease)
    {
        t = MathHelper.Clamp(t, 0f, 1f);

        return ease switch
        {
            TweenEase.SmoothStep => t * t * (3f - 2f * t),
            TweenEase.EaseInQuad => t * t,
            TweenEase.EaseOutQuad => 1f - (1f - t) * (1f - t),
            TweenEase.EaseInOutQuad => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) * 0.5f,
            TweenEase.EaseInCubic => t * t * t,
            TweenEase.EaseOutCubic => 1f - MathF.Pow(1f - t, 3f),
            TweenEase.EaseInOutCubic => t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) * 0.5f,
            TweenEase.SineIn => 1f - MathF.Cos(t * MathHelper.PiOver2),
            TweenEase.SineOut => MathF.Sin(t * MathHelper.PiOver2),
            TweenEase.SineInOut => -(MathF.Cos(MathHelper.Pi * t) - 1f) * 0.5f,
            TweenEase.ExpoOut => t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t),
            TweenEase.BackOut => 1f + 2.70158f * MathF.Pow(t - 1f, 3f) + 1.70158f * MathF.Pow(t - 1f, 2f),
            _ => t
        };
    }

}