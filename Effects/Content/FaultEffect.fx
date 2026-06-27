///故障效果
sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);

float GlitchChance = 0.53;     // 触发概率
float MaxOffset    = 0.125;    // 最大水平位移(uv)
float StripsY      = 18.0;    // 长条数量(越大越细)
float NoiseScroll  = 0.15;     // 噪声滚动速度
float MinHold      = 0.05;     // 最短保持时间(秒)
float MaxHold      = 0.22;     // 最长保持时间(秒)
float NoiseMix     = 0.70;     // 噪声参与权重

float iTime;

float Hash11(float n)
{
    return frac(sin(n) * 43758.5453);
}

float4 MainPS(float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 uv = texCoord;

    // 只按Y分段，保证触发时横跨全宽
    float stripId = floor(uv.y * StripsY);

    // 每条长条拥有不同的保持时长
    float hold = lerp(MinHold, MaxHold, Hash11(stripId * 19.19 + 7.13));
    float seg  = floor(iTime / max(hold, 0.0001));

    // 每个时间段内保持不变的随机种子
    float seed = stripId + seg * 157.0;
    float r0 = Hash11(seed);
    float r1 = Hash11(seed + 13.37);

    // 噪声只随Y变化(在x=0.5取样)，保证同一条长条全宽一致
    float2 nUV;
    nUV.x = 0.5;
    nUV.y = (stripId + 0.5) / StripsY + iTime * NoiseScroll;
    float n = tex2D(uImage1, nUV).r;

    // 结合hash与噪声决定是否触发
    float trigger = lerp(r0, n, NoiseMix);
    float activ  = step(1.0 - GlitchChance, trigger);

    // 触发后该条长条产生统一的水平位移
    float offset = (r1 * 2.0 - 1.0) * MaxOffset * (0.35 + 0.65 * n);
    uv.x += offset * activ;

    // 越界处理：夹取可避免采样出界造成杂边
    uv = saturate(uv);

    return tex2D(uImage0, uv);
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
