// 顶部椭圆弧 + 下端噪声消融；无内圆判定。
// UV 的 Y 轴向下。uImage0 为主材质，uImage1 为消融噪声。
sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);

// ===== 整体颜色与朝向（圆弧、消融共用） =====
float4 EffectColor = float4(1.0, 1.0, 1.0, 1.0);
// 默认圆弧朝上、消融端朝下；零向量回退到此方向。
float2 sweepDirection = float2(0.0, -1.0);

// ===== 扇形 / 圆弧轮廓 =====
float2 ArcCenter = float2(0.5, 0.5);
float OuterRadius = 0.45;
// 0 表示使用 OuterRadius；较小的 Y 半径使顶部圆弧更平。
float OuterRadiusY = 0.35;
float EdgeSoftness = 0.002;

// ===== 主材质 =====
float TextureRotation = 0.0;
float2 TextureScale = float2(1.0, 1.0);
float2 TextureFlow = float2(0.0, 0.0);
bool useRGBforApha = false;

// ===== 消融：进度与轮廓 =====
// 消融进度：0 完整显示，1 完全消失；不自动循环。
float iTimeProgress = 0.4;
float edgeWidth = 0.05;
float noiseStrength = 0.3;
float curveStrength = 0.25;
float2 radialCenter = float2(0.5, 0.5);

// ===== 消融：噪声材质 =====
// 消融贴图绕 UV 中心旋转，弧度制；正值使显示内容逆时针旋转。
// 独立于主材质旋转和 sweepDirection；先旋转，再缩放、流动。
float dissolveRotation = 0.0;
// X 方向较密、Y 方向拉长的噪声产生纵向火舌。
float2 dissolveScale = float2(4.0, 1.5);
// 外部随时间更新该偏移，即可让末端持续变化；固定进度也能产生动画。
float2 iTimeDisolve = float2(0.0, 0.0);

float4 MainPS(float2 uv : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float directionLength = length(sweepDirection);
    float2 dir = directionLength > 0.0001
        ? sweepDirection / max(directionLength, 0.0001) : float2(0.0, -1.0);
    float2 tangent = float2(-dir.y, dir.x);
    float rx = max(OuterRadius, 0.0001);
    float ry = OuterRadiusY > 0.0001 ? OuterRadiusY : rx;
    float2 p = uv - ArcCenter;
    // 在朝向的局部坐标中裁剪：rx 为横向半径，ry 为朝向轴半径。
    // 正面保留椭圆弧，背面无限延伸；绕 ArcCenter 跟随 dir 转向。
    float2 capPosition = float2(dot(p, tangent), max(dot(p, dir), 0.0));
    float capField = length(capPosition / float2(rx, ry));
    float capEdge = max(max(EdgeSoftness, 0.0001), fwidth(capField));
    float capMask = 1.0 - smoothstep(1.0 - capEdge, 1.0 + capEdge, capField);

    float c = cos(TextureRotation);
    float s = sin(TextureRotation);
    float2 centeredUV = uv - 0.5;
    float2 materialUV = float2(centeredUV.x * c - centeredUV.y * s,
                              centeredUV.x * s + centeredUV.y * c);
    materialUV = materialUV * TextureScale + 0.5 + TextureFlow;
    float4 material = tex2D(uImage0, materialUV);

    // 与 RadialDissolve 相同的方向投影 + 二次曲率 + 流动噪声场。
    float dissolveCos = cos(dissolveRotation);
    float dissolveSin = sin(dissolveRotation);
    float2 rotatedDissolveUV = float2(
        centeredUV.x * dissolveCos - centeredUV.y * dissolveSin,
        centeredUV.x * dissolveSin + centeredUV.y * dissolveCos) + 0.5;
    // 默认角度为 0 时保留原有噪声采样位置。
    float2 dissolveUV = rotatedDissolveUV * dissolveScale + iTimeDisolve;
    float noise = tex2D(uImage1, frac(dissolveUV)).r;
    float sweepValue = dot(uv - 0.5, dir) + 0.5;
    float tangentValue = dot(uv - radialCenter, float2(dir.y, -dir.x));
    float field = saturate(sweepValue + tangentValue * tangentValue * curveStrength
        + (noise - 0.5) * noiseStrength);
    float fadeWidth = max(max(edgeWidth, 0.0001), fwidth(field));
    float progress = saturate(iTimeProgress) * (1.0 + 2.0 * fadeWidth) - fadeWidth;
    float visible = smoothstep(progress, progress + fadeWidth, field);

    float alpha = (useRGBforApha ? material.r : material.a)
        * capMask * visible * inputColor.a * EffectColor.a;
    float3 rgb = material.rgb * inputColor.rgb * EffectColor.rgb * alpha;
    return float4(rgb, alpha);
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
