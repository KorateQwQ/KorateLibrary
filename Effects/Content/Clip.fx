sampler uImage0 : register(s0); //MainColor
sampler uImage1 : register(s1); //clipNoise

float Edge;
float4 EdgeColor;
float ClipValue;
float4 imageColor;

float2 noiseScale;
float2 noiseTime;
//用噪声图裁剪原图，做出消融效果。
float4 PixelShaderFunction(float2 coords : TEXCOORD0,float4 Color: COLOR0) : COLOR0
{
    
    float2 move = coords - float2(0.5, 0.5);
    float2 finalScale = noiseScale;
    finalScale = 1 / finalScale;
    
    float2 finalCoords = float2(0.5, 0.5) + move * finalScale;
    
    
    float4 tex = tex2D(uImage0, coords);
    float4 noise = tex2D(uImage1, frac(finalCoords + noiseTime));
    if(noise.r<ClipValue-Edge)
        return float4(0, 0, 0, 0);
    else if(noise.r<ClipValue)
        return tex.r * EdgeColor;

    return tex * Color * imageColor;
}

technique Technique1
{
    pass Apply1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}