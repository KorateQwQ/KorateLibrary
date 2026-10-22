// 多层龙卷风合并绘制：把形状参数相同的 Tornado.fx 叠层放进一次绘制，减少大面积 overdraw。
// 每层的计算与 Tornado.fx 一致（不含消融描边），最后在 shader 内按绘制顺序合成，
// 输出预乘颜色，需配合 BlendState.AlphaBlend（One / InvSrcAlpha）使用：
// - 普通层等价于 NonPremultiplied 叠加（SrcAlpha / InvSrcAlpha）
// - Additive 层等价于 Additive 叠加（SrcAlpha / One），不改变覆盖度
// 参数由 KL/Drawing/TornadoLayers.cs 设置，只依赖参数的部分已在 C# 中预先算好。

sampler iChannel0 : register(s0); //贴图槽 0（SpriteBatch 绘制的贴图）
sampler iChannel1 : register(s1); //消融材质
sampler iChannel2 : register(s2); //噪声，用于置换图效果
sampler iChannel3 : register(s3); //贴图槽 1

#define MAX_LAYERS 4

// ===== 共享形状参数 =====
// 贝塞尔 x 控制点（已乘 MaxInset）
float4 BezierX;
// 贝塞尔 y 控制点
float4 BezierY;
// 贝塞尔 y 导数系数：3*(P1-P0, P2-P1, P3-P2)
float3 BezierDY;
// x=yMin, y=yMax, z=1/(P3.y-P0.y), w=是否启用形变
float4 BezierRange;

// iTime * RotationSpeed
float RotationOffset;

// x=DisplaceAmount（0=关闭）, y=对比度系数 max(0, Contrast-1), z=Bias
float3 Displace;
// xy=噪声缩放, zw=iTime*噪声滚动速度
float4 DisplaceNoise;

// x=CameraZ, y=CameraZ^2, z=CameraZ^2-1, w=圆柱投影切线位置
float4 Cylinder;
// 2/DrawScale
float2 ProjectScale;
// x=1/HorizontalFadeRange（0=关闭）, y=1/VerticalFadeRange（0=关闭）, zw=DrawScale*0.5
float4 Fade;

float2 TexScale = float2(1.0, 1.0);
float2 DissolveTexMultiplier = float2(1.0, 1.0);
// 0 表示不消融
float DissolveThreshold = 0.0;

// ===== 每层参数（按绘制顺序）=====
// 原本 DrawInWorld 传入的顶点颜色
float4 LayerTint[MAX_LAYERS];
// 原本 TornadoEffect 的 EffectColor
float4 LayerEffectColor[MAX_LAYERS];
// x=useRForAlpha, y=Additive, z=贴图槽(0=iChannel0, 1=iChannel3), w=启用
float4 LayerParams[MAX_LAYERS];
// xy=该层矩形尺寸 / 实际绘制矩形尺寸（<=1，两者中心相同），z=Tilt
// 与上一层相同时直接复用上一层的形状计算
float4 LayerShape[MAX_LAYERS];

// 1 表示目标为 8 位格式，模拟逐层绘制时输出被截断到 0~1 的行为
float ClampLayers = 0.0;

static const float INV_TWOPI = 0.15915494309; // 1 / (2*pi)

float4 BezierWeights(float t)
{
    float u = 1.0 - t;
    return float4(u * u * u, 3.0 * u * u * t, 3.0 * u * t * t, t * t * t);
}

float InUnitRect(float2 uv)
{
    float2 inside = step(0.0, uv) * step(uv, 1.0);
    return inside.x * inside.y;
}

struct TornadoShape
{
    float mask;  // 0 表示该层在此像素没有内容
    float baseU; // 圆柱角度 U（不含动画）
    float v;     // 圆柱高度 V（不含倾斜）
    float fade;  // 边缘淡出
};

// 与 Tornado.fx 的 MainPS 前半段相同，提前 return 的地方改为 mask=0。
// 在动态循环中调用，因此用 tex2Dlod 采样（贴图无 mipmap，结果与 tex2D 相同）
TornadoShape EvaluateShape(float2 uv, float tilt)
{
    TornadoShape shape;
    float mask = InUnitRect(uv);

    float inset = 0.0;
    if (BezierRange.w > 0.5 && uv.y >= BezierRange.x && uv.y <= BezierRange.y)
    {
        float t = saturate((uv.y - BezierY.x) * BezierRange.z);
        for (int i = 0; i < 2; i++)
        {
            float u = 1.0 - t;
            float by = dot(BezierY, BezierWeights(t));
            float dy = dot(BezierDY, float3(u * u, 2.0 * u * t, t * t));
            dy = (abs(dy) < 0.0001) ? 0.0001 : dy;
            t = saturate(t - (by - uv.y) / dy);
        }
        inset = dot(BezierX, BezierWeights(t));
    }
    inset = clamp(inset, -0.49, 0.49);

    float width = 1.0 - 2.0 * inset;
    float2 warpedUV = float2((uv.x - inset) / width, uv.y);
    mask *= InUnitRect(warpedUV);

    if (Displace.x > 0.0001)
    {
        float uApprox = warpedUV.x + RotationOffset;
        float2 nUV = float2(uApprox, warpedUV.y + uApprox * tilt) * DisplaceNoise.xy + DisplaceNoise.zw;

        float n = tex2Dlod(iChannel2, float4(frac(nUV), 0.0, 0.0)).r;
        float centered = (n - 0.5) * 2.0;

        float ax = abs(centered);
        ax = saturate(ax + Displace.y * ax * ax * ax);
        centered = (centered >= 0.0) ? ax : -ax;
        centered += Displace.z;

        inset = clamp(inset + centered * Displace.x, -0.49, 0.49);
        width = 1.0 - 2.0 * inset;
        warpedUV = float2((uv.x - inset) / width, uv.y);
        mask *= InUnitRect(warpedUV);
    }

    float2 p = (warpedUV - 0.5) * ProjectScale;

    // 圆柱求交（同 Tornado.fx 的 RayCylinderFast）
    float inner = Cylinder.y - Cylinder.z * (p.x * p.x);
    float tHit = (Cylinder.y - sqrt(max(inner, 0.0))) / (p.x * p.x + Cylinder.y);
    float3 hit = float3(p.x * tHit, p.y * tHit, Cylinder.x * (1.0 - tHit));
    mask *= step(1e-8, inner) * step(1e-8, tHit) * step(abs(hit.y), 1.0);

    shape.baseU = atan2(hit.x, hit.z) * INV_TWOPI + 0.5;
    shape.v = hit.y * 0.5 + 0.5;

    float fade = 1.0;
    if (Fade.x > 0.0)
    {
        float distBezierUV = min(uv.x - inset, (1.0 - inset) - uv.x);
        float distCylUV = (Cylinder.w - abs(p.x)) * Fade.z * width;
        fade *= saturate(max(0.0, min(distBezierUV, distCylUV)) * Fade.x);
    }
    if (Fade.y > 0.0)
    {
        float distScreenUV = min(uv.y, 1.0 - uv.y);
        float capScale = Cylinder.x / max(Cylinder.x - hit.z, 1e-6);
        float distCapUV = (capScale - abs(p.y)) * Fade.w;
        fade *= saturate(max(0.0, min(distScreenUV, distCapUV)) * Fade.y);
    }

    shape.fade = fade;
    shape.mask = mask;
    return shape;
}

float4 MainPS(float2 texCoord : TEXCOORD0) : COLOR0
{
    float enabledDissolve = step(0.0001, DissolveThreshold);
    float3 premul = 0.0;
    float coverage = 0.0;

    float3 shapeKey = float3(-1.0, -1.0, -1.0);
    TornadoShape shape = (TornadoShape)0;

    [loop]
    for (int i = 0; i < MAX_LAYERS; i++)
    {
        float4 layer = LayerParams[i];
        [branch]
        if (layer.w > 0.5)
        {
            float3 key = LayerShape[i].xyz;
            [branch]
            if (any(key != shapeKey))
            {
                shape = EvaluateShape((texCoord - 0.5) / key.xy + 0.5, key.z);
                shapeKey = key;
            }

            float2 cylUV = float2(frac(shape.baseU + RotationOffset), shape.v + shape.baseU * key.z) * TexScale;
            float4 texUV = float4(frac(cylUV), 0.0, 0.0);
            float4 tex;
            [branch]
            if (layer.z < 0.5)
                tex = tex2Dlod(iChannel0, texUV);
            else
                tex = tex2Dlod(iChannel3, texUV);

            float4 col = tex * LayerTint[i] * shape.fade;

            float dissolve = tex2Dlod(iChannel1, float4(frac(cylUV * DissolveTexMultiplier), 0.0, 0.0)).r;
            col *= lerp(1.0, step(DissolveThreshold, dissolve), enabledDissolve);
            col.a = lerp(col.a, col.r, layer.x);
            col *= LayerEffectColor[i] * shape.mask;
            col = lerp(col, saturate(col), ClampLayers);

            float occlusion = col.a * (1.0 - layer.y);
            premul = col.rgb * col.a + premul * (1.0 - occlusion);
            coverage = occlusion + coverage * (1.0 - occlusion);
        }
    }

    return float4(premul, coverage);
}

technique Technique1
{
    pass expand
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
