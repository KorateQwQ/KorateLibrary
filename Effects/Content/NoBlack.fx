sampler uImage0 : register(s0); //MainColor

//针对灰度图，使用灰度作为透明度
float4 PixelShaderFunction(float2 coords : TEXCOORD0,float4 Color: COLOR0) : COLOR0
{
    float4 tex = tex2D(uImage0, coords);
    
    return float4(tex.rgb*Color.rgb,tex.r*Color.a);
}

technique Technique1
{
    pass Apply1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}