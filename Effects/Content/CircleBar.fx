sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);

// 圆环宽度（纹理空间，建议范围 0.01 ~ 0.25）
float RingWidth = 0.08;

// 圆环起始角（弧度制；0 表示从右侧开始，逆时针；默认从顶部开始）
float StartAngle = -1.57079632679;

// 进度（0~1）
float Progress = 0.65;

// 圆环底色（含 alpha）
float4 RingColor = float4(0.20, 0.20, 0.20, 0.80);

// 圆环进度色（含 alpha）
float4 ProgressColor = float4(0.30, 0.80, 1.00, 0.90);

// Bloom 强度（0 关闭；默认 1 开启）
float BloomStrength = 0.0;

float4 MainPS(float2 texCoord : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float2 p = texCoord - 0.5;
    float r = length(p);

    float outerR = 0.4;
    float width = saturate(RingWidth);
    float innerR = max(0.0, outerR - width);

    float aaR = max(0.00001, fwidth(r));
    float ringMask = smoothstep(innerR - aaR, innerR + aaR, r) * (1.0 - smoothstep(outerR - aaR, outerR + aaR, r));

    float angle = atan2(p.y, p.x);
    float u = frac((angle - StartAngle) / 6.28318530718);

    float prog = saturate(Progress);
    float aaU = max(0.00001, fwidth(u));
    float progMask = (prog >= 1.0) ? 1.0 : (1.0 - smoothstep(prog - aaU, prog + aaU, u));

    float4 ringCol = RingColor;
    float4 progCol = ProgressColor;

    float4 baseCol = lerp(ringCol, progCol, progMask);

    float a = baseCol.a * ringMask * inputColor.a;
    float3 rgb = baseCol.rgb * inputColor.rgb * a;

    float bloom = max(0.0, BloomStrength);

    float glowExtra = width * 1.5;
    float glowInnerR = max(0.0, innerR - glowExtra);
    float glowOuterR = outerR + glowExtra;

    float glowMask = smoothstep(glowInnerR, innerR, r) * (1.0 - smoothstep(outerR, glowOuterR, r));
    float glowEdge = saturate(glowMask - ringMask) * progMask;

    float glowA = progCol.a * glowEdge * bloom * inputColor.a;
    float3 glowRGB = progCol.rgb * inputColor.rgb * glowA;

    return float4(rgb + glowRGB, min(1.0, a + glowA));

}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
