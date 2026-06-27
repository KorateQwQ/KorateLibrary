sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uTargetPosition;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;


//——————自定义参数区域——————//
float4 effectColor;
sampler uImage2 : register(s2);


float4 ArmorBasic(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    //将噪声图严格映射到玩家当前帧
    float2 noiseCoords = (coords * uImageSize0 - uSourceRect.xy) / uSourceRect.zw;
    //将噪声适应玩家帧图大小，但不严格映射到玩家当前帧。
    //float2 noiseCoords = (coords * uImageSize0 - uSourceRect.xy) / uImageSize1;

    float4 noise = tex2D(uImage2, noiseCoords);

    return color*sampleColor * noise.r*effectColor;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 ArmorBasic();
    }
}