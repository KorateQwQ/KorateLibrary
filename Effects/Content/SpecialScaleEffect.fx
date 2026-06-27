sampler iChannel0 : register(s0); // 主要材质

// 整体缩放倍率：1=不变，>1=整体变大（放大），<1=整体变小（缩小）
float TotalScale = 1.0f;

// 左侧曲线对“左右内缩”的最大幅度（UV单位，0.25 表示左右各最多吃掉 25% 宽度）
float MaxInset = 0.25f;

// 完整 2D 三次贝塞尔（用来描述“左侧的形变曲线”）
// - 点坐标在 UV 空间内：x 表示“内缩轮廓”(0=不内缩，1=最大轮廓)，y 表示高度位置
// - P0/P3 决定形变的起点/终点：你可以通过改它们的 y 来控制顶部/底部哪些区域参与拉伸
// - P1/P2 决定形状：你可以通过改它们的 x/y 来决定中间凹凸的位置与强度
//   约束建议：为了让反求稳定，尽量保证 y 单调递增（P0.y <= P1.y <= P2.y <= P3.y）
float2 BezierP0 = float2(0.0f, 0.0f);
float2 BezierP1 = float2(0.85f, 0.25f);
float2 BezierP2 = float2(0.85f, 0.75f);
float2 BezierP3 = float2(0.0f, 1.0f);

float CubicBezier1D(float p0, float p1, float p2, float p3, float t)
{
    float u = 1.0f - t;
    return (u * u * u) * p0 + (3.0f * u * u * t) * p1 + (3.0f * u * t * t) * p2 + (t * t * t) * p3;
}

float CubicBezier1DDerivative(float p0, float p1, float p2, float p3, float t)
{
    float u = 1.0f - t;
    // B'(t) = 3*(u^2*(p1-p0) + 2*u*t*(p2-p1) + t^2*(p3-p2))
    return 3.0f * (u * u * (p1 - p0) + 2.0f * u * t * (p2 - p1) + t * t * (p3 - p2));
}

float4 MainPS(float2 texCoord : TEXCOORD0, float4 Color : COLOR0) : COLOR0
{
    float2 uv = texCoord;

    // 先做整体缩放（以中心 0.5,0.5 为锚点）：
    // TotalScale > 1 => 取样更靠近中心 => 画面看起来被放大
    float s = max(0.0001f, TotalScale);
    uv = (uv - 0.5f) / s + 0.5f;

    float inset = 0.0f;

    // 仅在 [BezierP0.y, BezierP3.y] 的高度范围内启用形变
    float yMin = min(BezierP0.y, BezierP3.y);
    float yMax = max(BezierP0.y, BezierP3.y);

    if (uv.y >= yMin && uv.y <= yMax && abs(yMax - yMin) > 0.0001f)
    {
        // 反求 t：给定目标 y=uv.y，求 bezierY(t)=y
        // 用 P0->P3 的线性插值作为初始值，再做少量牛顿迭代（固定次数，性能可控）
        float t = saturate((uv.y - BezierP0.y) / (BezierP3.y - BezierP0.y));

        [unroll]
        for (int i = 0; i < 4; i++)
        {
            float by = CubicBezier1D(BezierP0.y, BezierP1.y, BezierP2.y, BezierP3.y, t);
            float dy = CubicBezier1DDerivative(BezierP0.y, BezierP1.y, BezierP2.y, BezierP3.y, t);
            dy = (abs(dy) < 0.0001f) ? 0.0001f : dy;
            t = saturate(t - (by - uv.y) / dy);
        }

        // 形变轮廓（0~1）
        float profileX = CubicBezier1D(BezierP0.x, BezierP1.x, BezierP2.x, BezierP3.x, t);

        // 左右对称的“内缩量”（UV单位）
        inset = MaxInset * profileX;
    }

    // 把显示空间 [inset, 1-inset] 拉伸映射回采样空间 [0,1]
    float width = 1.0f - 2.0f * inset;
    if (width <= 0.0001f)
        return float4(0, 0, 0, 0);

    float2 sampleUV = float2((uv.x - inset) / width, uv.y);

    // 超出范围透明（这样边缘会形成你说的“凹进去”的轮廓）
    if (sampleUV.x < 0.0f || sampleUV.x > 1.0f || sampleUV.y < 0.0f || sampleUV.y > 1.0f)
        return float4(0, 0, 0, 0);

    return tex2D(iChannel0, sampleUV) * Color;
}

technique Technique1
{
    pass expand
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}