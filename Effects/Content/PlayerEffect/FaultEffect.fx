///故障效果
sampler uImage0 : register(s0);
sampler uImage1 : register(s1);


float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uTargetPosition;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;

//——————自定义参数区域——————//
sampler uImage2 : register(s2);
float GlitchChance = 0.33;     // 触发概率
float MaxOffset    = 0.325;    // 最大水平位移(uv)
float StripsY      = 18.0;    // 长条数量(越大越细)
float NoiseScroll  = 0.15;     // 噪声滚动速度
float MinHold      = 0.12;     // 最短保持时间(秒)
float MaxHold      = 0.22;     // 最长保持时间(秒)
float NoiseMix     = 0.0;     // 噪声参与权重

float iTime;
float Hash11(float n)
{
    return frac(sin(n) * 43758.5453);
}

float4 MainPS(float2 texCoord : TEXCOORD0,  float4 inputColor : COLOR0) : COLOR0
{
    float2 uv = texCoord;

    // 将噪声图严格映射到玩家当前帧（0-1空间）
    float2 noiseCoords = (uv * uImageSize0 - uSourceRect.xy) / uSourceRect.zw;

    // 当前帧在图集上的UV边界
    float2 frameMinUV = uSourceRect.xy / uImageSize0;
    float2 frameMaxUV = (uSourceRect.xy + uSourceRect.zw) / uImageSize0;

    // 位移幅度从“帧内UV”换算到“图集UV”
    float frameUvToAtlasUvX = uSourceRect.z / uImageSize0.x;

    // 只按Y分段（帧内），保证触发时横跨全宽
    float stripId = floor(noiseCoords.y * StripsY);

    // 每条长条拥有不同的保持时长
    float hold = lerp(MinHold, MaxHold, Hash11(stripId * 19.19 + 7.13));
    float seg  = floor(iTime / max(hold, 0.0001));

    // 每个时间段内保持不变的随机种子
    float seed = stripId + seg * 157.0;
    float r0 = Hash11(seed);
    float r1 = Hash11(seed + 13.37);

    // 噪声只随Y变化（取帧内中心x），保证同一条长条全宽一致
    float2 nUV;
    nUV.x = 0.5;
    nUV.y = noiseCoords.y + iTime * NoiseScroll;
    float n = tex2D(uImage2, nUV).r;

    // 结合hash与噪声决定是否触发
    float trigger = lerp(r0, n, NoiseMix);
    float active  = step(1.0 - GlitchChance, trigger);

    // 触发后该条长条产生统一的水平位移（先按帧内UV，再换算到图集UV）
    float offsetFrame = (r1 * 2.0 - 1.0) * MaxOffset * (0.35 + 0.65 * n);
    uv.x += offsetFrame * frameUvToAtlasUvX * active;

    // 采样限制在玩家当前帧子矩形内，避免串到相邻帧
    float2 halfTexel = 0.5 / uImageSize0;
    uv = clamp(uv, frameMinUV + halfTexel, frameMaxUV - halfTexel);

    return tex2D(uImage0, uv)*float4(uColor,1)*inputColor;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}