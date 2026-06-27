
sampler2D uImage0 : register(s0);

float width = 10;// 真实宽度
float height = 10;// 真实高度

float cornerRadius = 4;// 圆角半径

float4 borderColor = float4(0,0,0,1);// 边框颜色

float lineAngle = 0;// 角度，单位为度，0在右侧，顺时针增加
float lineLength = 0.15;// 线段长度，小于等于1时表示整圈百分比，大于1时表示角度值
float lineWidth = 2;// 线宽

float PI = 3.14159265;
float INV_TWO_PI = 0.15915494;

// 计算圆角矩形的有符号距离：>0 在外部，=0 在边界，<0 在内部
float sdRoundRect(float2 p, float2 b, float r)
{
    float2 q = abs(p) - b + r;
    float2 h = max(q, 0.0);
    return length(h) + min(max(q.x, q.y), 0.0) - r;
}

// 返回不为 0 的符号
float2 SignNotZero(float2 p)
{
    return float2(p.x < 0.0 ? -1.0 : 1.0, p.y < 0.0 ? -1.0 : 1.0);
}

// 获取边框中线上的对应位置
float2 GetBorderMidPoint(float2 p, float2 b, float r, float halfWidth)
{
    float2 signValue = SignNotZero(p);
    float2 a = abs(p);
    float2 cornerCenter = b - r;
    float2 midPoint = a;
    float midRadius = max(r - halfWidth, 0.0);

    if (a.x > cornerCenter.x && a.y > cornerCenter.y)
    {
        float2 v = a - cornerCenter;
        float lenV = length(v);
        if (lenV < 0.0001)
        {
            v = float2(1.0, 0.0);
            lenV = 1.0;
        }
        midPoint = cornerCenter + v * (midRadius / lenV);
    }
    else
    {
        float dx = b.x - a.x;
        float dy = b.y - a.y;
        if (dx < dy)
        {
            midPoint = float2(b.x - halfWidth, a.y);
        }
        else
        {
            midPoint = float2(a.x, b.y - halfWidth);
        }
    }

    return midPoint * signValue;
}

// 归一化到 0~1 的环形角度
float NormalizeLoop01(float value)
{
    return frac(value + 1.0);
}

// 计算环形角度差
float AngleDelta01(float a, float b)
{
    float delta = abs(a - b);
    return min(delta, 1.0 - delta);
}

float4 PSRectangle(float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 uv = texCoord * 2.0 - 1.0;

    float2 b = float2(width, height);
    float r = min(cornerRadius, min(b.x, b.y));
    float2 p = float2(uv.x * b.x, uv.y * b.y);

    float dist = sdRoundRect(p, b, r);
    float shapeAlpha = saturate(0.5 - dist);
    if (shapeAlpha <= 0.0)
    {
        return float4(0, 0, 0, 0);
    }

    float lineAlpha = 0.0;
    if (lineWidth > 0.0)
    {
        float innerAlpha = saturate(0.5 - (dist + lineWidth));
        lineAlpha = saturate(shapeAlpha - innerAlpha);
    }

    if (lineAlpha <= 0.0)
    {
        return float4(0, 0, 0, 0);
    }

    float centerAngle01 = NormalizeLoop01(lineAngle / 360.0);
    float halfWidth = lineWidth * 0.5;
    float2 borderMidPoint = GetBorderMidPoint(p, b, r, halfWidth);
    float pixelAngle01 = NormalizeLoop01(atan2(borderMidPoint.y, borderMidPoint.x) * INV_TWO_PI);

    float halfAngle01 = 0.0;
    if (lineLength <= 1.0)
    {
        halfAngle01 = lineLength * 0.5;
    }
    else
    {
        halfAngle01 = lineLength / 720.0;
    }

    if (AngleDelta01(pixelAngle01, centerAngle01) > halfAngle01)
    {
        return float4(0, 0, 0, 0);
    }

    return borderColor * lineAlpha;
}
technique Technique1
{
    pass expand
    {
        PixelShader = compile ps_3_0 PSRectangle();
    }

}