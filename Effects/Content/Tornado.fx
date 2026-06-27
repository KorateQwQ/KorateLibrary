sampler iChannel0 : register(s0); //主要材质
sampler iChannel1 : register(s1); //消融材质
sampler iChannel2 : register(s2); //噪声，用于置换图效果


// ===== 主要绘制相关=====
float4 EffectColor = float4(1.0, 1.0, 1.0, 1.0);
bool useRForAlpha = false;
// 相机距离：越大越接近正交，越小透视越强（建议 1.5 ~ 6）
float CameraZ = 3.5;

// 整体绘制缩放：数值越大绘制越小
float2 DrawScale = float2(1.0, 1.0)*0.5;

// 旋转速度：贴图沿圆柱水平方向滚动（单位：圈/秒；1=每秒转一圈）
float RotationSpeed = 0.2;

// ===== 材质相关=====

// 贴图缩放：>1 更密，<1 更稀
float2 TexScale = float2(10.0, 2.0);

// 贴图倾斜度：0=不倾斜；正值会让纹理沿圆周方向“往上爬”（圆柱上呈斜向上升）
float Tilt = 2.0;

// ===== 消融参数（尽可能简单）=====
// 消融材质倍率：控制消融噪声贴图的重复/缩放（越大越密），可分别控制UV
float2 DissolveTexMultiplier = float2(1.0, 1.0);
// 消融阈值：0~1，越大消融越多；0 表示默认不消融
float DissolveThreshold = 0.0;
// 消融边界宽度：0~1（建议 0.02~0.15），0 表示硬切
float DissolveEdgeWidth = 0.05;
// 消融边界颜色：rgb=颜色，a=强度
float4 DissolveEdgeColor = float4(1.0, 0.6, 0.2, 1.0);

// ===== 淡出参数（0=无影响，数值越大边缘淡出范围越宽，单位为UV）=====
// 水平淡出：左右边缘在指定范围内从可见渐变到完全透明
float HorizontalFadeRange = 0.02;
// 垂直淡出：上下边缘在指定范围内从可见渐变到完全透明
float VerticalFadeRange = 0.02;

// ===== 置换图（用于“轮廓”凹凸，不是贴图表面抖动）=====
// 原理类似 AE 的 Displacement Map：用噪声贴图的灰度当作“位移场”，把内缩量 inset 推来推去
// 注意：默认 DisplaceAmount=0 时完全关闭，不影响现有画面

// 置换强度（UV单位）：0=关闭；数值越大左右轮廓起伏越明显
float DisplaceAmount = 0.05;

// 噪声图缩放倍率：越大越密
float2 DisplaceNoiseScale = float2(1.1, 1.1);

// 噪声滚动速度（单位：UV/秒）
float2 DisplaceNoiseSpeed = float2(0.5, 0.5);

// 噪声对比度：1=原样；>1 更硬更碎；<1 更柔
float DisplaceNoiseContrast = 1.0;

// 噪声偏置：让整体更偏向凸/凹（可为负）
float DisplaceNoiseBias = 0.0;


// ===== 贝塞尔曲线形变（移植自 SpecialScaleEffect.fx，作用于“最终绘制结果矩形整体”）=====
// 把屏幕上的矩形结果视为 0..1 UV
// - 在不同高度(y)处，根据三次贝塞尔曲线得到“左右对称内缩量 inset”
// - 将显示空间 [inset, 1-inset] 反向拉伸映射回采样空间 [0,1]
// - 超出范围直接透明，从而形成两侧圆弧凹陷（沙漏轮廓）

// 左右内缩的最大幅度（UV单位，0.25 表示左右各最多吃掉 25% 宽度）
float MaxInset = 0.25;

// 完整 2D 三次贝塞尔（描述“左侧的形变曲线”），左右会自动镜像成对称效果
// 点坐标在 UV 空间内：x 表示“内缩轮廓”(0=不内缩，1=最大轮廓)，y 表示高度位置
float2 BezierP0 = float2(-1.5, 0.0);
float2 BezierP1 = float2(1.85, 0.55);
float2 BezierP2 = float2(0.85, 0.95);
float2 BezierP3 = float2(-0.9, 1.0);

// 时间（由引擎传入）
float iTime;

const float PI = 3.14159265;
const float INV_TWOPI = 0.15915494309; // 1 / (2*pi)

float CubicBezier1D(float p0, float p1, float p2, float p3, float t)
{
    float u = 1.0 - t;
    return (u * u * u) * p0 + (3.0 * u * u * t) * p1 + (3.0 * u * t * t) * p2 + (t * t * t) * p3;
}

float CubicBezier1DDerivative(float p0, float p1, float p2, float p3, float t)
{
    float u = 1.0 - t;
    // B'(t) = 3*(u^2*(p1-p0) + 2*u*t*(p2-p1) + t^2*(p3-p2))
    return 3.0 * (u * u * (p1 - p0) + 2.0 * u * t * (p2 - p1) + t * t * (p3 - p2));
}

// 针对本效果的特化圆柱求交：
// - 相机固定在 ro=(0,0,CameraZ)
// - 投影平面点为 (p.x,p.y,0)
// - 射线方向取 rd=(p.x,p.y,-CameraZ)（不需要 normalize）
bool RayCylinderFast(float2 p, out float t, out float3 hit)
{
    // 由判别式化简可得：disc/4 = CameraZ^2 - (CameraZ^2 - 1) * p.x^2
    float cz2 = CameraZ * CameraZ;
    float inner = cz2 - (cz2 - 1.0) * (p.x * p.x);
    if (inner <= 0.0)
        return false;

    float s = sqrt(inner);

    // A = p.x^2 + CameraZ^2
    float A = p.x * p.x + cz2;

    // 取更近的那个交点（对应 t0）
    t = (cz2 - s) / A;
    if (t <= 0.0)
        return false;

    // hit = ro + rd * t，其中 ro=(0,0,CameraZ), rd=(p.x,p.y,-CameraZ)
    hit.x = p.x * t;
    hit.y = p.y * t;
    hit.z = CameraZ * (1.0 - t);

    if (abs(hit.y) > 1.0)
        return false;

    return true;
}

float4 MainPS(float2 texCoord : TEXCOORD0, float4 Color : COLOR0) : COLOR0
{
    // ===== 先做“最终绘制结果矩形整体”的贝塞尔形变（屏幕空间）=====
    float2 uv = texCoord;
    
    float inset = 0.0;

    // 仅在 [BezierP0.y, BezierP3.y] 的高度范围内启用形变
    float yMin = min(BezierP0.y, BezierP3.y);
    float yMax = max(BezierP0.y, BezierP3.y);

    if (uv.y >= yMin && uv.y <= yMax && abs(yMax - yMin) > 0.0001)
    {
        // 反求 t：给定目标 y=uv.y，求 bezierY(t)=y
        float t = saturate((uv.y - BezierP0.y) / (BezierP3.y - BezierP0.y));

        //[unroll]
        for (int i = 0; i < 2; i++)
        {
            float by = CubicBezier1D(BezierP0.y, BezierP1.y, BezierP2.y, BezierP3.y, t);
            float dy = CubicBezier1DDerivative(BezierP0.y, BezierP1.y, BezierP2.y, BezierP3.y, t);
            dy = (abs(dy) < 0.0001) ? 0.0001 : dy;
            t = saturate(t - (by - uv.y) / dy);
        }

        // 形变轮廓（0~1）
        float profileX = CubicBezier1D(BezierP0.x, BezierP1.x, BezierP2.x, BezierP3.x, t);

        // 左右对称的“内缩量”（UV单位）
        inset = MaxInset * profileX;
    }

    // 把显示空间 [inset, 1-inset] 拉伸映射回采样空间 [0,1]
    // 注意：inset 过于接近 0.5 会导致 width≈0，从而在高处出现不正常“截断/闪断”。
    // 这里留出一点安全边界（0.49 => 最小宽度 0.02），同时允许 inset 为负（轮廓外扩）。
    inset = clamp(inset, -0.49, 0.49);

    float width = 1.0 - 2.0 * inset;
    if (width <= 0.0001)
        return float4(0, 0, 0, 0);

    float2 warpedUV = float2((uv.x - inset) / width, uv.y);

    // 超出范围透明（形成沙漏轮廓）
    if (warpedUV.x < 0.0 || warpedUV.x > 1.0 || warpedUV.y < 0.0 || warpedUV.y > 1.0)
        return float4(0, 0, 0, 0);
    
    if (DisplaceAmount > 0.0001)
    {
        // 用 warpedUV.x 近似圆柱角度轴，并叠加 Tilt，让噪声沿斜向分布
        // 同时加上一点 RotationSpeed，让置换跟随材质滚动，避免纯屏幕空间“钉死”。
        float uApprox = warpedUV.x + iTime * RotationSpeed;
        float2 nUV = float2(uApprox, warpedUV.y + uApprox * Tilt);
        nUV = nUV * DisplaceNoiseScale + iTime * DisplaceNoiseSpeed;

        float n = tex2D(iChannel2, frac(nUV)).r; // 0..1
        float centered = (n - 0.5) * 2.0; // -1..1

        // 低指令“对比度”近似：x + k*x^3
        float ax = abs(centered);
        float k = max(0.0, DisplaceNoiseContrast - 1.0);
        ax = ax + k * ax * ax * ax;
        ax = saturate(ax);
        centered = (centered >= 0.0) ? ax : -ax;

        centered += DisplaceNoiseBias;

        inset = clamp(inset + centered * DisplaceAmount, -0.49, 0.49);

        // 更新 warpedUV（轮廓变化）
        width = 1.0 - 2.0 * inset;
        if (width <= 0.0001)
            return float4(0, 0, 0, 0);

        warpedUV = float2((uv.x - inset) / width, uv.y);
        if (warpedUV.x < 0.0 || warpedUV.x > 1.0 || warpedUV.y < 0.0 || warpedUV.y > 1.0)
            return float4(0, 0, 0, 0);
    }

    // ===== 进入圆柱投影阶段：用（最终）warpedUV 当作屏幕采样位置 =====

    // 屏幕坐标映射到 [-1, 1]，再用 DrawScale 调整视野（x=水平缩放，y=垂直缩放）
    float2 p = (warpedUV - 0.5) * 2.0;
    p /= DrawScale;

    float tHit;
    float3 hit;
    if (!RayCylinderFast(p, tHit, hit))
        return float4(0, 0, 0, 0);

    // 圆柱侧壁UV：
    // - baseU：仅由圆柱角度决定（不含动画）
    // - u：动画后的采样U（用于“水平旋转”）
    // - v：高度
    float theta = atan2(hit.x, hit.z); // [-pi, pi]
    float baseU = theta * INV_TWOPI + 0.5;

    float u = baseU + iTime * RotationSpeed;
    float v = hit.y * 0.5 + 0.5;

    // 倾斜（螺旋贴图）：倾斜只跟圆柱角度绑定，不跟时间绑定
    v += baseU * Tilt;

    float2 cylUV = float2(frac(u), v) * TexScale;
    float4 tex = tex2D(iChannel0, frac(cylUV));

    // ===== 边缘淡出（单位为 UV；会同时考虑 DrawScale、贝塞尔形变边界、以及圆柱自身投影轮廓）=====
    float fade = 1.0;

    // 将投影平面距离转换回 UV 距离：p = ((warpedUV-0.5)*2)/DrawScale  =>  duv = dp * DrawScale / 2
    float2 pToUv = DrawScale * 0.5;

    if (HorizontalFadeRange > 0.0001)
    {
        // 1) 贝塞尔形变后的矩形边界（屏幕uv空间）
        float distBezierUV = min(uv.x - inset, (1.0 - inset) - uv.x);

        // 2) 圆柱投影的左右轮廓边界（投影平面 p 空间）
        //    透视投影下的切线位置（推导自判别式=0）：|p.x| = CameraZ / sqrt(CameraZ^2 - 1)
        float denom = max(CameraZ * CameraZ - 1.0, 1e-6);
        float xTan = CameraZ * rsqrt(denom);
        float distCylP = xTan - abs(p.x);

        // distCylP -> warpedUV 距离：duvWarped = dp * DrawScale / 2
        // warpedUV -> 最终屏幕uv 距离：duvScreen = duvWarped * width（因为 warpedUV.x = (uv.x - inset)/width）
        float distCylUV = distCylP * pToUv.x * width;

        float distUV = max(0.0, min(distBezierUV, distCylUV));
        fade *= saturate(distUV / HorizontalFadeRange);

    }

    if (VerticalFadeRange > 0.0001)
    {
        // 1) 屏幕上下边界（uv空间）
        float distScreenUV = min(uv.y, 1.0 - uv.y);

        // 2) 圆柱端盖（y=±1）在屏幕上的投影边界：
        //    对固定的 (hit.x, hit.z)，透视缩放因子 scale = CameraZ/(CameraZ-hit.z)
        //    投影平面坐标满足：p.y = hit.y * scale，因此端盖边界为 |p.y| = 1 * scale
        float scale = CameraZ / max(CameraZ - hit.z, 1e-6);
        float distCapP = scale - abs(p.y);
        float distCapUV = distCapP * pToUv.y;

        float distUV = max(0.0, min(distScreenUV, distCapUV));
        fade *= saturate(distUV / VerticalFadeRange);

    }

    float4 outCol = tex * Color;
    outCol *= fade;

    // ===== 消融 =====
    float dissolve = tex2D(iChannel1, frac(cylUV * DissolveTexMultiplier)).r;

    // DissolveThreshold=0 时视为“关闭消融”，保持默认画面不变
    float enabled = step(0.0001, DissolveThreshold);

    float visibleDissolve = step(DissolveThreshold, dissolve);
    float visible = lerp(1.0, visibleDissolve, enabled);

    float edgeWidthEnabled = step(0.0001, DissolveEdgeWidth);
    float inEdgeBand = visibleDissolve * (1.0 - step(DissolveThreshold + DissolveEdgeWidth, dissolve));
    float edge = enabled * edgeWidthEnabled * inEdgeBand;

    // 裁剪
    outCol.rgb *= visible;
    outCol.a *= visible;
    if (useRForAlpha)
        outCol.a = outCol.r;

    // 边缘直接替换成边界颜色（边界颜色也要吃 fade，确保边缘淡出优先）
    outCol.rgb = lerp(outCol.rgb, DissolveEdgeColor.rgb * fade, edge);
    outCol.a *= lerp(1.0, DissolveEdgeColor.a, edge);

    if(edge>0.0)return outCol;
    return outCol*EffectColor ;

}


technique Technique1
{
    pass expand
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}