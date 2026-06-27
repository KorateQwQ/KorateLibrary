sampler uImage0 : register(s0);
float4x4 uTransform;
float4x4 uWorld;
float3 uLightDirection = float3(-0.35, -0.65, -0.7);
float3 uLightColor = float3(1.0, 1.0, 1.0);
float uAmbientStrength = 0.35;
float uDiffuseStrength = 0.85;
struct VSInput
{
    float3 Pos : POSITION0;
    float4 Color : COLOR0;
    float2 Texcoord : TEXCOORD0;
    float3 Normal : NORMAL0;
};

struct PSInput
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR0;
    float2 Texcoord : TEXCOORD0;
    float3 Normal : TEXCOORD1;
};
PSInput VertexShaderFunction(VSInput input)
{
    PSInput output;
    float4 normal4 = float4(input.Normal, 0.0);
    float4 worldNormal4 = mul(normal4, uWorld);
    output.Texcoord = input.Texcoord;
    output.Normal = normalize(worldNormal4.xyz);
    output.Pos = mul(float4(input.Pos, 1.0), uTransform);
    output.Color = input.Color;
    return output;
}

float4 PixelShaderFunction(PSInput input) : COLOR0
{
    float3 normal = normalize(input.Normal);
    float3 lightDirection = normalize(uLightDirection);
    float diffuse = saturate(dot(normal, lightDirection));
    float3 lighting = uLightColor * (uAmbientStrength + diffuse * uDiffuseStrength);
    float4 c = tex2D(uImage0, input.Texcoord);
    c.rgb *= input.Color.rgb * lighting;
    c.a *= input.Color.a;
    return c;

}

technique Technique1
{
    pass Base
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }

}

