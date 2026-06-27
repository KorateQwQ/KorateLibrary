sampler uImage0 : register(s0); //MainColor

//强制使用绘制的透明度而非图片透明度
float4 PixelShaderFunction(float2 coords : TEXCOORD0,float4 Color: COLOR0) : COLOR0
{
    float4 tex = tex2D(uImage0, coords);
    if (tex.a > 0)
    {
        return float4(tex.rgb * Color.rgb,Color.a);
    }
    
    return tex;
}

technique Technique1
{
    pass Apply1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}