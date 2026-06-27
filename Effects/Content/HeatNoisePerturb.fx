sampler uImage0 : register(s0); //MainColor
sampler uImage1 : register(s1); //Noise



float2 uTime;
float strength;

float4 PixelShaderFunction(float2 coords : TEXCOORD0,float4 Color: COLOR0) : COLOR0 //通过uImage1,也就是noise的纹理去扰动uImage0
{
    float4 noise = tex2D(uImage1, float2(coords.x + uTime.r, coords.y + uTime.g));
    float2 move = noise.rg * strength;
    //float streath2 = smoothstep(0.5, 0, abs(0.5 - coords.y));
    float4 color = tex2D(uImage0, coords + move);
    return color * Color;
}

technique Technique1
{
    pass Apply1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}