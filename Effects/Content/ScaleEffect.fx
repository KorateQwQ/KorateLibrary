sampler uImage0 : register(s0); //MainColor

float scale;
//将图片以某个比例缩放
float4 PixelShaderFunction(float2 coords : TEXCOORD0,float4 Color: COLOR0) : COLOR0
{
    float2 move = coords - float2(0.5, 0.5);
    float finalScale = scale;
    if (finalScale != 0)
        finalScale = 1 / finalScale;
    float2 finalCoords = float2(0.5, 0.5) + move * finalScale;
    if(finalCoords.x<0||finalCoords.x>1||finalCoords.y<0||finalCoords.y>1)
        return float4(0, 0, 0, 0);
    
    return tex2D(uImage0, finalCoords) * Color;
}

technique Technique1
{
    pass Apply1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}