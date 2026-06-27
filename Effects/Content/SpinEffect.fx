sampler uImage0 : register(s0);

float rot;
float scale;

float4 bloomColor;

float2 rotatedBy(float2 vec, float radians)
{
    float num = (float) cos(radians);
    float num2 = (float) sin(radians);
    float2 v = vec;
    float2 result = float2(0, 0);
    result.x += (v.x * num - v.y * num2);
    result.y += (v.x * num2 + v.y * num);
    return result;
}

float4 PSFunction(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0 
{
    float2 move = coords - float2(0.5, 0.5);
    float finalScale = scale;
    if (finalScale != 0)
        finalScale = 1 / finalScale;
    float2 finalCoords = float2(0.5, 0.5) + move * finalScale;
    if (finalCoords.x < 0 || finalCoords.x > 1 || finalCoords.y < 0 || finalCoords.y > 1)
        return float4(0, 0, 0, 0);
    
    float2 v = finalCoords - float2(0.5, 0.5);
    v = rotatedBy(v, rot);
    float2 res = float2(0.5, 0.5) + v;
    
    
    float4 color = tex2D(uImage0, res);
    
    return color * bloomColor*inputColor;
}


technique Technique1
{
    pass expand
    {
        PixelShader = compile ps_2_0 PSFunction();
    }
}