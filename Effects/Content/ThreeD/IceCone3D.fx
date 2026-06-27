sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
float4x4 uWorld;
float4x4 uViewProjection;
float3 uLightDirection = float3(-1.0, -1.0, -0.75);
float3 uLightColor = float3(1.0, 1.0, 1.0);
float3 uCameraPosition = float3(0.0, 0.0, -1000.0);
float4 uBaseColor = float4(1.0, 1.0, 1.0, 1.0);
float4 uOutlineColor = float4(0.0, 0.0, 0.0, 1.0);
float3 uFresnelColor = float3(0.7, 0.9, 1.0);
float3 uSpecularColor = float3(1.0, 1.0, 1.0);
bool uEnableToonShading = false;
bool uUseNormalMap = false;
bool uUseSpecularMap = false;
float uAmbientStrength = 0.7;
float uDiffuseStrength = 1.0;
float uOutlineThickness = 1.0;
float uFresnelStrength = 0.65;
float uNormalStrength = 1.0;
float uSpecularStrength = 0.75;
float uSpecularPower = 24.0;

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
    float3 WorldNormal : TEXCOORD1;
    float3 WorldPosition : TEXCOORD2;
};

PSInput VertexShaderBase(VSInput input)
{
    PSInput output;
    float4 worldPos4 = mul(float4(input.Pos, 1.0), uWorld);
    float4 worldNormal4 = mul(float4(input.Normal, 0.0), uWorld);
    output.Pos = mul(worldPos4, uViewProjection);
    output.Color = input.Color;
    output.Texcoord = input.Texcoord;
    output.WorldNormal = normalize(worldNormal4.xyz);
    output.WorldPosition = worldPos4.xyz;
    return output;
}

PSInput VertexShaderOutline(VSInput input)
{
    PSInput output;
    float4 worldPos4 = mul(float4(input.Pos, 1.0), uWorld);
    float4 worldNormal4 = mul(float4(input.Normal, 0.0), uWorld);
    float3 worldNormal = normalize(worldNormal4.xyz);
    float3 expandedWorldPos = worldPos4.xyz + worldNormal * uOutlineThickness;
    output.Pos = mul(float4(expandedWorldPos, 1.0), uViewProjection);
    output.Color = input.Color;
    output.Texcoord = input.Texcoord;
    output.WorldNormal = worldNormal;
    output.WorldPosition = expandedWorldPos;
    return output;
}

float3 BuildMappedWorldNormal(PSInput input, float3 baseWorldNormal)
{
    float3 sampleNormal = tex2D(uImage1, input.Texcoord).xyz * 2.0 - 1.0;
    float3 positionDx = ddx(input.WorldPosition);
    float3 positionDy = ddy(input.WorldPosition);
    float2 texcoordDx = ddx(input.Texcoord);
    float2 texcoordDy = ddy(input.Texcoord);
    float determinant = texcoordDx.x * texcoordDy.y - texcoordDx.y * texcoordDy.x;

    if (abs(determinant) < 0.00001)
    {
        return baseWorldNormal;
    }

    float3 tangent = normalize(positionDx * texcoordDy.y - positionDy * texcoordDx.y);
    tangent = normalize(tangent - baseWorldNormal * dot(baseWorldNormal, tangent));
    float3 bitangent = normalize(cross(baseWorldNormal, tangent));

    if (determinant < 0.0)
    {
        bitangent = -bitangent;
    }

    float3 mappedWorldNormal = normalize(
        tangent * sampleNormal.x +
        bitangent * sampleNormal.y +
        baseWorldNormal * sampleNormal.z);

    return normalize(lerp(baseWorldNormal, mappedWorldNormal, uNormalStrength));
}

float GetSpecularMask(float2 texcoord)
{
    float3 specularSample = tex2D(uImage2, texcoord).rgb;
    return dot(specularSample, float3(0.299, 0.587, 0.114));
}

float4 PixelShaderBase(PSInput input) : COLOR0
{
    float3 normal = normalize(input.WorldNormal);
    if (uUseNormalMap)
    {
        normal = BuildMappedWorldNormal(input, normal);
    }

    float3 viewDirection = normalize(uCameraPosition - input.WorldPosition);
    float3 lightDirection = normalize(-uLightDirection);
    float3 halfDirection = normalize(lightDirection + viewDirection);
    float fresnelDot = saturate(abs(dot(normal, viewDirection)));
    float fresnel = pow(1.0 - fresnelDot, 4.0) * uFresnelStrength;
    float diffuse = saturate(dot(normal, lightDirection)) * uDiffuseStrength;
    float lighting = saturate(uAmbientStrength + diffuse);

    if (uEnableToonShading)
    {
        float toonLighting = 0.2;
        toonLighting = max(toonLighting, step(0.3, lighting) * 0.55);
        toonLighting = max(toonLighting, step(0.7, lighting) * 1.0);
        lighting = toonLighting;
    }

    float specularMask = 0.0;
    float specular = 0.0;
    if (uUseSpecularMap)
    {
        specularMask = GetSpecularMask(input.Texcoord);
        specular = pow(saturate(dot(normal, halfDirection)), uSpecularPower) * specularMask * uSpecularStrength;
    }

    float4 baseColor = (dot(tex2D(uImage0, input.Texcoord).rgb, float3(0.299, 0.587, 0.114)) + uBaseColor);
    float3 litColor = baseColor.rgb * input.Color.rgb * uLightColor * lighting;
    float3 specularColor = uSpecularColor * uLightColor * specular;
    baseColor.rgb = litColor + specularColor + uFresnelColor * fresnel;
    baseColor.a *= input.Color.a;
    return baseColor;
}

float4 PixelShaderOutline(PSInput input) : COLOR0
{
    float4 outlineColor = uOutlineColor;
    float4 signatureSample = tex2D(uImage0, input.Texcoord);
    float signatureKeep = signatureSample.a;
    outlineColor.rgb *= input.Color.rgb;
    outlineColor.a *= input.Color.a;
    outlineColor.a *= 0.9999 + signatureKeep * 0.0001;
    return outlineColor;
}

technique Technique1
{
    pass Outline
    {
        VertexShader = compile vs_3_0 VertexShaderOutline();
        PixelShader = compile ps_3_0 PixelShaderOutline();
    }

    pass Base
    {
        VertexShader = compile vs_3_0 VertexShaderBase();
        PixelShader = compile ps_3_0 PixelShaderBase();
    }
}
