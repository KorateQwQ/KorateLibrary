sampler2D uImage0 : register(s0);

float Luminance(float3 color)
{
    return dot(color, float3(0.3, 0.59, 0.11));
}

float4 MainPS(float2 texCoord : TEXCOORD0,float4 color : COLOR0) : COLOR0
{
    float4 baseColor = tex2D(uImage0, texCoord)*color;
    float3 greyColor = Luminance(baseColor.rgb);
    
    return float4(greyColor,baseColor.a);

}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}