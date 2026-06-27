sampler uImage0 : register(s0);
texture2D tex0;

sampler2D uImage1 = sampler_state //voronoi噪声图
{
    Texture = <tex0>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

float uTime;
float time;
float strength;
float noiseStrength;
float curveStrength;

float sinx;
float range;
float rand;
float4 PSFunction(float2 coords : TEXCOORD0, float4 Color : COLOR0) : COLOR0 //将image0进行曲线变换

{
    float y = coords.y;
    float moveByX = smoothstep(0, 0.3, coords.x);
    if (coords.x > 0.7)
        moveByX = smoothstep(0, 0.3, 1-coords.x);
    float DtoY = abs(y - 0.5);
    if (DtoY > moveByX)//越接近边缘，DtoY 越接近0.5
        return float4(0, 0, 0, 0);
    if (y > 0.5)
        y = lerp(0.5, 1, DtoY / moveByX);
    else if (y < 0.5)
        y = lerp(0.5, 0, DtoY / moveByX);
    


    float2 start = float2(0, 0.5);
    float2 end = float2(1, 0.5);
    float2 cpoint = float2(0.5, 0.5 + uTime);
    float2 point1 = lerp(start, cpoint, coords.x);
    float2 point2 = lerp(cpoint, end, coords.x);
    float2 finalp = lerp(point1, point2, coords.x);
    float move = (finalp.y - 0.5)*curveStrength;
    float4 noise = tex2D(uImage1, float2(coords.x + rand, 0 + time));
    float2 noisemove = noise.rg * noiseStrength;
    
    float4 image = tex2D(uImage0, float2(coords.x + rand, y + move + sin(lerp(sinx + rand, sinx + rand + range, coords.x + rand)) * strength) + noisemove);

    return Color*image; //
}
technique Technique1
{
    pass move
    {
        PixelShader = compile ps_2_0 PSFunction();
    }

}