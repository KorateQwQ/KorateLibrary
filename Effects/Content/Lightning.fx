
sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);

float EdgeStrength = 2; // 毛边强度（建议 0-1）
float EdgeFrequency = 0.2; // 毛边频率（建议 4-32）

float CoreStrength = 0.28; // 核心摆动幅度（建议 0-0.2）
float CoreFrequency = 0.5; // 核心摆动频率（建议 1-12）
float CoreEndFade = 0.12; // 两端锁定过渡宽度（建议 0.02-0.3）
bool CoreRightEndFree = false; // 右端不锁定（允许末端摆动）

float EdgeTime = 0.0; // 毛边时间驱动
float CoreTime = 0.0; // 核心摆动时间驱动

float4 BloomColor = float4(1.0, 1.0, 1.0, 1.0); // 亮光颜色

float iTime;

float4 MainPS(float2 texCoord : TEXCOORD0, float4 inputColor :COLOR0) : COLOR0
{
    float2 uv = texCoord;

    // 噪声沿线段长度(X)变化；用时间滚动避免图案静止
    // 上边缘与下边缘使用不同采样轨迹，避免毛边上下镜像
    float2 noiseUVTop = frac(float2(uv.x * EdgeFrequency + EdgeTime * 0.27 + 13.71, EdgeTime * 0.11 + 2.13));
    float2 noiseUVBot = frac(float2(uv.x * (EdgeFrequency * 1.37) + EdgeTime * 0.19 + 53.19, EdgeTime * 0.08 + 7.77));

    float noiseTop01 = tex2D(uImage1, noiseUVTop).r;
    float noiseBot01 = tex2D(uImage1, noiseUVBot).r;

    // 核心沿X方向上下摆动（先对空间做Y位移，再在位移后的空间里做毛边压缩）
    float2 coreNoiseUV = frac(float2(uv.x * CoreFrequency + CoreTime * 0.22 + 101.9, CoreTime * 0.05 + 11.4));

    float coreNoise01 = tex2D(uImage1, coreNoiseUV).r;
    float centerYOffset = (coreNoise01 * 2.0 - 1.0) * CoreStrength;

    float endFade = max(CoreEndFade, 0.001);

    float endMaskLeft = smoothstep(0.0, endFade, uv.x);
    float endMaskRight = smoothstep(0.0, endFade, 1.0 - uv.x);

    float rightFree = CoreRightEndFree ? 1.0 : 0.0;
    float swayMask = endMaskLeft * lerp(endMaskRight, 1.0, rightFree);
    float taperMask = endMaskLeft * endMaskRight;

    centerYOffset *= swayMask;

    float2 uvWarp = uv;
    uvWarp.y = uv.y - centerYOffset;

    float centerY = 0.5;

    // 上下边缘分别向中线压缩
    float tTop = EdgeStrength * noiseTop01;
    float tBot = EdgeStrength * noiseBot01;

    float tTopClamped = saturate(tTop);
    float tBotClamped = saturate(tBot);

    float scaleTop = 1.0 - tTopClamped;
    float scaleBot = 1.0 - tBotClamped;

    // 两端逐渐收尖：在 CoreEndFade 区域内进一步缩窄上下半区高度
    scaleTop *= taperMask;
    scaleBot *= taperMask;

    // 输出空间的收缩遮罩：上/下半区间高度独立变化
    float upperY = saturate(centerY + 0.5 * scaleTop);
    float lowerY = saturate(centerY - 0.5 * scaleBot);

    float shapeMask = step(lowerY, uvWarp.y) * step(uvWarp.y, upperY);

    float minScaleY = 0.001;

    // 采样空间的反向映射：上半区与下半区分别按各自 scale 反推
    float isTop = step(centerY, uvWarp.y);

    float uvInYTop = centerY + (uvWarp.y - centerY) / max(scaleTop, minScaleY);
    float uvInYBot = centerY + (uvWarp.y - centerY) / max(scaleBot, minScaleY);

    uvInYTop = lerp(uvInYTop, centerY, step(1.0, tTop));
    uvInYBot = lerp(uvInYBot, centerY, step(1.0, tBot));

    float uvInY = lerp(uvInYBot, uvInYTop, isTop);

    // 超出范围的采样直接裁掉，避免被采样器边界颜色影响
    float inRange = step(0.0, uvInY) * step(uvInY, 1.0);
    float mask = shapeMask * inRange;

    float4 src = tex2D(uImage0, float2(uv.x, uvInY));
    float4 baseColor = src * inputColor * mask;

    return baseColor*BloomColor;
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
