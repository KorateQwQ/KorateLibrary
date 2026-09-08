// 月牙扇形材质：由相交的外圆与内圆自然形成尖端。
sampler2D uImage0 : register(s0);

float4 EffectColor = float4(1.0, 1.0, 1.0, 1.0);

// 月牙外圆 X 半径，单位为纹理空间半径（0.5 为铺满方形画布）。
float OuterRadius = 0.4;
// 月牙外圆 Y 半径；与 OuterRadius 不同时形成椭圆，设为 0 时自动使用 OuterRadius。
float OuterRadiusY = 0.0;
// 内圆 X 半径；内外圆相交后，两条圆弧会在两端形成尖角。
float InnerRadius = 0.4;
// 内圆 Y 半径；设为 0 时自动使用 InnerRadius。
float InnerRadiusY = 0.0;
// 内椭圆偏移，同时决定背面挖空延伸方向；负 X 保留右侧月牙。
// 零偏移默认保留右侧；尖端仍由内外轮廓相交形成。
float2 InnerOffset = float2(-0.14, 0.0);

// 传入材质的旋转角度，弧度制；正值逆时针旋转。
float TextureRotation = 0.0;
// 材质缩放；大于 1 会增加纹理重复次数，使纹理细节变小。
float2 TextureScale = float2(1.0, 1.0);
// 材质流动偏移；可由外部每帧累加，实现风刃流动。
float2 TextureFlow = float2(0.0, 0.0);
float EdgeSoftness = 0.002;

bool useRGBforApha = false;

float2 RotateTextureUV(float2 uv, float angle)
{
    float2 centeredUV = uv - 0.5;
    float rotationCos = cos(angle);
    float rotationSin = sin(angle);
    return float2(
        centeredUV.x * rotationCos - centeredUV.y * rotationSin,
        centeredUV.x * rotationSin + centeredUV.y * rotationCos
    ) + 0.5;
}

float EllipseDistance(float2 p, float2 radius)
{
    float2 safeRadius = max(radius, 0.0001);
    return length(p / safeRadius);
}

// 将内椭圆沿偏移方向扫掠到无穷远：挖空背面，保留正面的椭圆弧。
// 在椭圆归一化空间求点到射线的距离，避免直接半平面裁切产生平端。
float InnerCutoutDistance(float2 p, float2 radius)
{
    float2 safeRadius = max(radius, 0.0001);
    float2 q = (p - InnerOffset) / safeRadius;
    float2 backDirection = dot(InnerOffset, InnerOffset) > 0.00000001
        ? InnerOffset : float2(-1.0, 0.0);
    float2 rayDirection = normalize(backDirection / safeRadius);
    float alongRay = max(dot(q, rayDirection), 0.0);
    return length(q - rayDirection * alongRay);
}

float4 MainPS(float2 texCoord : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float2 p = texCoord - 0.5;
    float outerRadius = max(OuterRadius, 0.0001);
    float outerRadiusY = OuterRadiusY > 0.0001 ? OuterRadiusY : outerRadius;
    float innerRadius = max(InnerRadius, 0.0001);
    float innerRadiusY = InnerRadiusY > 0.0001 ? InnerRadiusY : innerRadius;

    float outerDistance = EllipseDistance(p, float2(outerRadius, outerRadiusY));
    float innerDistance = InnerCutoutDistance(p, float2(innerRadius, innerRadiusY));
    float outerEdge = max(EdgeSoftness, fwidth(outerDistance));
    float innerEdge = max(EdgeSoftness, fwidth(innerDistance));

    float outerMask = 1.0 - smoothstep(1.0 - outerEdge, 1.0 + outerEdge, outerDistance);
    float innerMask = smoothstep(1.0 - innerEdge, 1.0 + innerEdge, innerDistance);
    float mask = outerMask * innerMask;

    float2 materialUV = RotateTextureUV(texCoord, TextureRotation);
    materialUV = (materialUV - 0.5) * TextureScale + 0.5 + TextureFlow;
    float4 material = tex2D(uImage0, materialUV);
    float alpha = (useRGBforApha?material.r:material.a) * mask * inputColor.a * EffectColor.a;
    float3 rgb = material.rgb * inputColor.rgb * EffectColor.rgb;

    return float4(rgb, alpha);
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
