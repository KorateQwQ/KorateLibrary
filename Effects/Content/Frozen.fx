sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);

float2 uFrameUVMin = float2(0.0, 0.0); // 当前帧在 uImage0 上的UV左上角
float2 uFrameUVMax = float2(1.0, 1.0); // 当前帧在 uImage0 上的UV右下角

float2 uFrostScale = float2(1.0, 1.0); // 冰冻纹理在“帧内局部UV(0-1)”空间的缩放(>1为平铺)
float2 uFrostOffset = float2(0.0, 0.0); // 冰冻纹理在“帧内局部UV(0-1)”空间的偏移
float uFrostStrength = 1.0; // 冰冻叠加强度(0-1)

float4 MainPS(float2 texCoord : TEXCOORD0,float4 color : COLOR0) : COLOR0
{
    float4 baseColor = tex2D(uImage0, texCoord);
    if (baseColor.a <= 0.0)
        return baseColor*color;

    float2 frameSize = uFrameUVMax - uFrameUVMin;
    frameSize = max(frameSize, float2(0.00001, 0.00001));

    float2 localFrameUV = (texCoord - uFrameUVMin) / frameSize;

    float2 frostUV = localFrameUV * uFrostScale + uFrostOffset;
    float4 frostColor = tex2D(uImage1, frostUV);

    float w = saturate(frostColor.a * uFrostStrength);
    float3 outRgb = lerp(baseColor.rgb, frostColor.rgb, w);

    return float4(outRgb, baseColor.a)*color;
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}