sampler uImage0 : register(s0);
float4x4 uTransform;

float width;// 真实宽度
float height;// 真实高度

float cornerRadius;// 圆角半径

float capsuleSharpness = 0.0;// 胶囊端头尖锐度，0 为圆头，1 为尖头
float capsulePointLengthScale = 0.75;// 三角端头长度系数，基于半高计算

float crossStarCurve = 0.0;// 十字星曲率，0 为直边星，1 为曲边星
float crossStarInnerScale = 0.35;// 十字星腰部比例，越小越尖
float4 crossStarTipScale = float4(1.0, 1.0, 1.0, 1.0);// 上右下左四个方向的尖端长度缩放

float2 innerTextureScale = float2(1.0, 1.0);// 内部纹理缩放，1 为原始大小
float2 innerTextureOffset = float2(0.0, 0.0);// 内部纹理偏移，单位为 UV

bool filled;// 是否填充
float borderWidth;// 边框宽度
float4 borderColor;// 边框颜色
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

// 计算圆角矩形的有符号距离场：>0 在外部，=0 在边界，<0 在内部
float sdRoundRect(float2 p, float2 b, float r)
{
    float2 q = abs(p) - b + r;
    float2 h = max(q, 0.0);
    return length(h) + min(max(q.x, q.y), 0.0) - r;
}

// 计算矩形的有符号距离场
float sdBox(float2 p, float2 b)
{
    float2 q = abs(p) - b;
    float2 h = max(q, 0.0);
    return length(h) + min(max(q.x, q.y), 0.0);
}

// 计算点到线段的距离
float sdSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float baDot = max(dot(ba, ba), 0.001);
    float h = clamp(dot(pa, ba) / baDot, 0.0, 1.0);
    return length(pa - ba * h);
}

// 计算二维叉积
float cross2(float2 a, float2 b)
{
    return a.x * b.y - a.y * b.x;
}

// 计算左右带三角突起的六边形条形距离场
float sdPointedCapsuleRect(float2 p, float2 halfSize)
{
    float pointLength = clamp(capsulePointLengthScale * halfSize.y, 0.0, halfSize.x);
    float bodyHalfWidth = max(halfSize.x - pointLength, 0.0);

    float2 v0 = float2(-bodyHalfWidth - pointLength, 0.0);
    float2 v1 = float2(-bodyHalfWidth, halfSize.y);
    float2 v2 = float2(bodyHalfWidth, halfSize.y);
    float2 v3 = float2(bodyHalfWidth + pointLength, 0.0);
    float2 v4 = float2(bodyHalfWidth, -halfSize.y);
    float2 v5 = float2(-bodyHalfWidth, -halfSize.y);

    float dist = sdSegment(p, v0, v1);
    dist = min(dist, sdSegment(p, v1, v2));
    dist = min(dist, sdSegment(p, v2, v3));
    dist = min(dist, sdSegment(p, v3, v4));
    dist = min(dist, sdSegment(p, v4, v5));
    dist = min(dist, sdSegment(p, v5, v0));

    float side0 = cross2(v1 - v0, p - v0);
    float side1 = cross2(v2 - v1, p - v1);
    float side2 = cross2(v3 - v2, p - v2);
    float side3 = cross2(v4 - v3, p - v3);
    float side4 = cross2(v5 - v4, p - v4);
    float side5 = cross2(v0 - v5, p - v5);

    float maxSide = max(max(max(side0, side1), max(side2, side3)), max(side4, side5));
    return maxSide <= 0.0 ? -dist : dist;
}

// 左右两端为圆头或尖头的横向条形
float sdCapsuleRect(float2 p, float2 halfSize)
{
    float radius = max(halfSize.y, 0.001);
    float sharp = saturate(capsuleSharpness);

    float roundBodyHalfWidth = max(halfSize.x - radius, 0.0);
    float2 nearest = float2(clamp(p.x, -roundBodyHalfWidth, roundBodyHalfWidth), 0.0);
    float roundDist = length(p - nearest) - radius;

    float pointedDist = sdPointedCapsuleRect(p, halfSize);
    return lerp(roundDist, pointedDist, sharp);
}

float2 getCrossStarTipHalfSize(float2 p, float2 halfSize)
{
    float topScale = max(crossStarTipScale.x, 0.05);
    float rightScale = max(crossStarTipScale.y, 0.05);
    float bottomScale = max(crossStarTipScale.z, 0.05);
    float leftScale = max(crossStarTipScale.w, 0.05);
    float tipScaleX = p.x >= 0.0 ? rightScale : leftScale;
    float tipScaleY = p.y >= 0.0 ? topScale : bottomScale;
    return max(halfSize * float2(tipScaleX, tipScaleY), float2(0.001, 0.001));
}

float getCrossStarStraightField(float2 p, float2 halfSize)
{
    float2 tipHalfSize = getCrossStarTipHalfSize(p, halfSize);
    float innerScale = clamp(crossStarInnerScale, 0.05, 0.49);
    float2 normalized = abs(p / tipHalfSize);
    float majorAxis = max(normalized.x, normalized.y);
    float minorAxis = min(normalized.x, normalized.y);
    float sideFactor = 1.0 / innerScale - 1.0;
    return majorAxis + sideFactor * minorAxis - 1.0;
}

float getCrossStarCurvedField(float2 p, float2 halfSize)
{
    float2 tipHalfSize = getCrossStarTipHalfSize(p, halfSize);
    float innerScale = clamp(crossStarInnerScale, 0.05, 0.49);
    float exponent = clamp(log(0.5) / log(innerScale), 0.2, 1.0);
    float2 normalized = abs(p / tipHalfSize);
    return pow(normalized.x, exponent) + pow(normalized.y, exponent) - 1.0;
}

float getCrossStarField(float2 p, float2 halfSize)
{
    float straightField = getCrossStarStraightField(p, halfSize);
    float curvedField = getCrossStarCurvedField(p, halfSize);
    return lerp(straightField, curvedField, saturate(crossStarCurve));
}

float2 getCrossStarStraightPoint(float t, float innerScale)
{
    float tClamped = saturate(t);
    if (tClamped < 0.5)
    {
        float segmentT = tClamped * 2.0;
        return lerp(float2(0.0, 1.0), float2(innerScale, innerScale), segmentT);
    }

    float segmentT = (tClamped - 0.5) * 2.0;
    return lerp(float2(innerScale, innerScale), float2(1.0, 0.0), segmentT);
}

float2 getCrossStarCurvedPoint(float t, float innerScale)
{
    float tClamped = saturate(t);
    float exponent = clamp(log(0.5) / log(innerScale), 0.2, 1.0);
    float power = 2.0 / exponent;
    float angle = (1.0 - tClamped) * 1.5707963;
    float cosValue = max(cos(angle), 0.0);
    float sinValue = max(sin(angle), 0.0);
    return float2(pow(cosValue, power), pow(sinValue, power));
}

float2 getCrossStarBoundaryPoint(float t, float2 tipHalfSize)
{
    float innerScale = clamp(crossStarInnerScale, 0.05, 0.49);
    float2 straightPoint = getCrossStarStraightPoint(t, innerScale);
    float2 curvedPoint = getCrossStarCurvedPoint(t, innerScale);
    float2 boundaryPoint = lerp(straightPoint, curvedPoint, saturate(crossStarCurve));
    return boundaryPoint * tipHalfSize;
}

// 计算四尖十字星的分方向近似距离场
float sdCrossStar(float2 p, float2 halfSize)
{
    float2 mirroredPoint = abs(p);
    float2 tipHalfSize = getCrossStarTipHalfSize(p, halfSize);
    float minDistance = 1000000.0;
    float2 previousPoint = getCrossStarBoundaryPoint(0.0, tipHalfSize);
    float segmentCount = 24.0;
    float step = 1.0 / segmentCount;

    for (float i = 1.0; i <= segmentCount; i += 1.0)
    {
        float t = i * step;
        float2 currentPoint = getCrossStarBoundaryPoint(t, tipHalfSize);
        minDistance = min(minDistance, sdSegment(mirroredPoint, previousPoint, currentPoint));
        previousPoint = currentPoint;
    }

    float field = getCrossStarField(p, halfSize);
    return field <= 0.0 ? -minDistance : minDistance;
}

float2 getInnerTextureUV(float2 uv)
{
    float2 safeScale = max(innerTextureScale, float2(0.001, 0.001));
    float2 centeredUV = uv - 0.5;
    return centeredUV / safeScale + 0.5 + innerTextureOffset;
}

float4 PSRectangle(PSInput input) : COLOR0
{
    float3 coord = input.Texcoord;
    float2 textureUV = getInnerTextureUV(coord.xy);
    float4 sourceColor = tex2D(uImage0, textureUV) * input.Color;
    float4 fillColor = filled ? sourceColor : float4(0, 0, 0, 0);

    float2 halfSize = float2(width, height) * 0.5;
    float radius = min(cornerRadius, min(halfSize.x, halfSize.y));
    float lineWidth = max(borderWidth, 0.0);

    // 将纹理坐标映射到以中心为原点的像素空间
    float2 uv = coord.xy * 2.0 - 1.0;
    float2 p = uv * halfSize;
    float dist = sdRoundRect(p, halfSize, radius);

    // 圆角矩形整体覆盖率，支持半像素边缘
    float shapeAlpha = saturate(0.5 - dist);
    if (shapeAlpha <= 0.0)
    {
        return float4(0, 0, 0, 0);
    }

    float borderAlpha = 0.0;
    if (lineWidth > 0.0)
    {
        // 内轮廓覆盖率，和外轮廓相减后得到边框覆盖率
        float innerAlpha = saturate(0.5 - (dist + lineWidth));
        borderAlpha = saturate(shapeAlpha - innerAlpha);
    }

    float fillAlpha = saturate(shapeAlpha - borderAlpha);
    float4 result = fillColor * fillAlpha + borderColor * borderAlpha;
    return result * coord.z;
}

float4 PSCapsuleRectangle(PSInput input) : COLOR0
{
    float3 coord = input.Texcoord;
    float2 textureUV = getInnerTextureUV(coord.xy);
    float4 sourceColor = tex2D(uImage0, textureUV) * input.Color;
    float4 fillColor = filled ? sourceColor : float4(0, 0, 0, 0);

    float2 halfSize = float2(width, height) * 0.5;
    float lineWidth = max(borderWidth, 0.0);

    // 将纹理坐标映射到以中心为原点的像素空间
    float2 uv = coord.xy * 2.0 - 1.0;
    float2 p = uv * halfSize;
    float dist = sdCapsuleRect(p, halfSize);

    // 胶囊矩形整体覆盖率，支持半像素边缘
    float shapeAlpha = saturate(0.5 - dist);
    if (shapeAlpha <= 0.0)
    {
        return float4(0, 0, 0, 0);
    }

    float borderAlpha = 0.0;
    if (lineWidth > 0.0)
    {
        // 内轮廓覆盖率，和外轮廓相减后得到边框覆盖率
        float innerAlpha = saturate(0.5 - (dist + lineWidth));
        borderAlpha = saturate(shapeAlpha - innerAlpha);
    }

    float fillAlpha = saturate(shapeAlpha - borderAlpha);
    float4 result = fillColor * fillAlpha + borderColor * borderAlpha;
    return result * coord.z;
}

float4 PSCrossStar(PSInput input) : COLOR0
{
    float3 coord = input.Texcoord;
    float2 textureUV = getInnerTextureUV(coord.xy);
    float4 sourceColor = tex2D(uImage0, textureUV) * input.Color;
    float4 fillColor = filled ? sourceColor : float4(0, 0, 0, 0);

    float2 halfSize = float2(width, height) * 0.5;
    float lineWidth = max(borderWidth, 0.0);

    // 将纹理坐标映射到以中心为原点的像素空间
    float2 uv = coord.xy * 2.0 - 1.0;
    float2 p = uv * halfSize;
    float dist = sdCrossStar(p, halfSize);

    // 十字星整体覆盖率，支持半像素边缘
    float shapeAlpha = saturate(0.5 - dist);
    if (shapeAlpha <= 0.0)
    {
        return float4(0, 0, 0, 0);
    }

    float borderAlpha = 0.0;
    if (lineWidth > 0.0)
    {
        // 内轮廓覆盖率，和外轮廓相减后得到边框覆盖率
        float innerAlpha = saturate(0.5 - (dist + lineWidth));
        borderAlpha = saturate(shapeAlpha - innerAlpha);
    }

    float fillAlpha = saturate(shapeAlpha - borderAlpha);
    float4 result = fillColor * fillAlpha + borderColor * borderAlpha;
    return result * coord.z;
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
    pass Rectangle
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PSRectangle();
    }
    
    pass CapsuleRectangle
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PSCapsuleRectangle();
    }
    
    pass CrossStar
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PSCrossStar();
    }
    
}