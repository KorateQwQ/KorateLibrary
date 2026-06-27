sampler uImage0 : register(s0); //MainColor
sampler uImage1 : register(s1); //ClipNoise
sampler uImage2 : register(s2); //HeatNoise


float4x4 uTransform;
float uTime;

bool useTime;
float uTimex;
float uTimey;
float2 ImageScale;

//消融相关参数：
float threshold;
float edge;
float4 edgeColor;
float2 maskScale;
float2 maskTime;
bool shouldClip;

//热扰动相关参数：
bool useHeat;
float heatStrength;
float2 heatNoiseScale;
float2 heatNoiseTime;


//绘制方法
bool useRforAlpha;

struct VSInput
{
    float2 Pos : POSITION0;
    float4 Color : COLOR0;
    float3 Texcoord : TEXCOORD0;
};

struct PSInput
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR0;
    float3 Texcoord : TEXCOORD0;
};

float3 hsv2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs((c.xxx + K.xyz - floor(c.xxx + K.xyz)) * 6.0 - K.www);
    
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float4 PixelShaderFunction(PSInput input) : COLOR0
{
    float3 coord = input.Texcoord;
    float2 Toward = float2(coord.x,coord.y) - float2(0.5f, 0.5f);
    float2 FinalScale = ImageScale;
    FinalScale = 1 / FinalScale;

    float2 FinalCoord = float2(0.5, 0.5) + Toward * FinalScale;
    float2 heatMove = float2(0, 0);
    
    if (useHeat)
    {
        float2 heatFinalScale = heatNoiseScale;
        heatFinalScale = 1 / heatFinalScale;
        float2 heatFinalCoords = float2(0.5, 0.5) + Toward * heatFinalScale;
        
        float2 heatNoise = tex2D(uImage2, frac(heatFinalCoords + heatNoiseTime));
        heatMove = heatNoise.rg * heatStrength;
    }
    
    float4 c = tex2D(uImage0, float2(FinalCoord.x, FinalCoord.y)+heatMove);
    if (useTime)
        c = tex2D(uImage0, float2(FinalCoord.x+uTimex, FinalCoord.y+uTimey)+heatMove);
    
    if (!shouldClip)
    {
        if (useRforAlpha)
            return float4((input.Color * coord.z).rgb, coord.z * c.r);
        return c * input.Color * float4(coord.z, coord.z, coord.z, coord.z);
    }
    
    float2 move = float2(coord.x, coord.y) - float2(0.5, 0.5);
    float2 finalScale = maskScale;
    finalScale = 1 / finalScale;
    
    float2 finalCoords = float2(0.5, 0.5) + move * finalScale;
    
    float4 noise = tex2D(uImage1, frac(finalCoords + maskTime));
    if (noise.r < threshold - edge)
        return float4(0, 0, 0, 0);
    else if (noise.r < threshold)
        return edgeColor;

    if (useRforAlpha)
        return float4((input.Color * coord.z).rgb, coord.z * c.r);
    return c * input.Color * float4(coord.z, coord.z, coord.z, coord.z);
}

PSInput VertexShaderFunction(VSInput input)
{
    PSInput output;
    output.Color = input.Color;
    output.Texcoord = input.Texcoord;
    output.Pos = mul(float4(input.Pos, 0, 1), uTransform);
    return output;
}


technique Technique1
{
    pass ColorBar
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}