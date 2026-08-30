sampler2D uImage0 : register(s0);  // 主贴图（要绘制的圆环材质）
sampler2D uImage1 : register(s1);  // 噪声贴图（用于毛边消融效果）

// 圆环宽度（纹理空间，建议范围 0.01 ~ 0.5）
float RingWidth = 0.1;

// 圆环外半径（默认 0.4，内半径 = 外半径 - 宽度）
float OuterRadius = 0.4;

// 描边大小（默认 0 表示无描边）
float BorderWidth = 0.0;

// 描边颜色
float4 BorderColor = float4(1.0, 1.0, 1.0, 1.0);

// 圆环整体颜色（与贴图相乘）
float4 RingColor = float4(1.0, 1.0, 1.0, 1.0);

// 贴图缩放倍率（控制贴图在圆环上的重复次数）
float2 TexScale = float2(1.0, 1.0);

// 贴图旋转角度（弧度制）
float TexRotation = 0.0;

// 贴图偏移
float2 TexOffset = float2(0.0, 0.0);

// 是否使用极坐标映射（true = 贴图沿圆环环绕，false = 径向映射）
bool UsePolarMapping = true;

// 极坐标映射时，是否交换UV（true = 贴图从上到下沿圆环环绕，false = 贴图从左到右沿圆环环绕）
bool SwapUV = false;

// === 毛边消融参数 ===
// 毛边强度（0 = 无毛边效果，值越大毛边越明显）
float EdgeNoiseStrength = 0.0;

// 噪声贴图缩放
float2 EdgeNoiseScale = float2(1.0, 1.0);

// 噪声贴图偏移（用于动画）
float2 EdgeNoiseOffset = float2(0.0, 0.0);

float4 MainPS(float2 texCoord : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    // 计算相对于中心的坐标
    float2 p = texCoord - 0.5;
    float r = length(p);

    // 圆环遮罩
    float outerR = saturate(OuterRadius);
    float width = saturate(RingWidth);
    float innerR = max(0.0, outerR - width);

    // 抗锯齿
    float aaR = max(0.00001, fwidth(r));

    // 计算基础 UV（用于采样材质）
    float angle = atan2(p.y, p.x);
    float u = (angle + 3.14159265359) / 6.28318530718;
    float v = (r - innerR) / max(width, 0.001);

    // 应用旋转
    u = frac(u + TexRotation / 6.28318530718);

    // === 毛边消融效果 ===
    float edgeNoise = 0.0;
    if (EdgeNoiseStrength > 0.0001)
    {
        // 噪声采样使用角度和绝对半径，而不是圆环内的相对位置
        // 这样噪声在整个圆环上连续变化，产生凹凸的毛边效果
        float2 noiseUV = float2(u, r) * EdgeNoiseScale + EdgeNoiseOffset;

        // 如果需要交换UV
        if (SwapUV)
            noiseUV = float2(noiseUV.y, noiseUV.x);

        // 采样噪声贴图
        float4 noiseSample = tex2D(uImage1, noiseUV);
        edgeNoise = noiseSample.r; // 使用红色通道，范围 0-1
    }

    // 根据噪声调整当前位置的圆环边界
    // 噪声越大，此处从两边向中心收缩越多
    float widthReduction = edgeNoise * EdgeNoiseStrength;
    float modifiedInnerR = innerR + widthReduction;
    float modifiedOuterR = outerR - widthReduction;

    // 使用调整后的半径计算圆环遮罩
    float ringMask = smoothstep(modifiedInnerR - aaR, modifiedInnerR + aaR, r) *
                     (1.0 - smoothstep(modifiedOuterR - aaR, modifiedOuterR + aaR, r));

    // 如果不在圆环范围内，直接返回透明
    if (ringMask < 0.001)
        return float4(0, 0, 0, 0);

    // 计算贴图采样坐标（材质）
    float2 uv;
    if (UsePolarMapping)
    {
        // 极坐标映射：贴图沿圆环环绕
        float angle = atan2(p.y, p.x);
        float u = (angle + 3.14159265359) / 6.28318530718; // 归一化到 [0, 1]
        float v = (r - innerR) / width; // 径向位置 [0, 1]

        // 应用旋转
        u = frac(u + TexRotation / 6.28318530718);

        // 如果需要交换UV，贴图从上到下沿圆环环绕，从左到右从内到外
        uv = SwapUV ? float2(v, u) : float2(u, v);
    }
    else
    {
        // 直角坐标映射：径向映射
        float angle = atan2(p.y, p.x) + TexRotation;
        float normalizedR = (r - innerR) / width;

        uv = float2(cos(angle), sin(angle)) * normalizedR * 0.5 + 0.5;
    }

    // 应用缩放和偏移
    uv = uv * TexScale + TexOffset;

    // 采样贴图
    float4 texColor = tex2D(uImage0, uv);

    // 与圆环颜色和输入颜色相乘
    float4 finalColor = texColor * RingColor * inputColor;

    // 应用圆环遮罩
    finalColor.a *= ringMask;

    // 描边效果
    if (BorderWidth > 0.0001)
    {
        float borderInnerR = innerR - BorderWidth;
        float borderOuterR = outerR + BorderWidth;

        // 外描边遮罩
        float outerBorderMask = smoothstep(outerR - aaR, outerR + aaR, r) *
                                (1.0 - smoothstep(borderOuterR - aaR, borderOuterR + aaR, r));

        // 内描边遮罩
        float innerBorderMask = smoothstep(borderInnerR - aaR, borderInnerR + aaR, r) *
                                (1.0 - smoothstep(innerR - aaR, innerR + aaR, r));

        float borderMask = outerBorderMask + innerBorderMask;

        // 混合描边
        float4 borderFinal = BorderColor * inputColor;
        borderFinal.a *= borderMask;

        // 使用 alpha 混合
        finalColor.rgb = lerp(borderFinal.rgb, finalColor.rgb, finalColor.a);
        finalColor.a = saturate(finalColor.a + borderFinal.a);
    }

    return finalColor;
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
