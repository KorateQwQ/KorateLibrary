sampler uImage0 : register(s0); //MainColor

float threshold;

//针对灰度图，使用灰度作为透明度
float4 PixelShaderFunction(float2 coords : TEXCOORD0,float4 Color: COLOR0) : COLOR0
{
    float4 tex = tex2D(uImage0, coords);
    if (tex.r>threshold&&tex.g>threshold&&tex.b>threshold)tex.rgb = float3(0,0,0);
    
    return float4(tex.rgb*Color.rgb,tex.r*Color.a);
}

//针对普通图片，使用alpha作为透明度
float4 PixelShaderFunctio2(float2 coords : TEXCOORD0,float4 Color: COLOR0) : COLOR0
{
    float4 tex = tex2D(uImage0, coords);
    if (tex.r>threshold&&tex.g>threshold&&tex.b>threshold)tex.rgb = float3(0,0,0);
    
    return float4(tex.rgb*Color.rgb,tex.a*Color.a);
}
technique Technique1
{
    pass Apply1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
    pass Apply2
    {
        PixelShader = compile ps_3_0 PixelShaderFunctio2();
    }
}