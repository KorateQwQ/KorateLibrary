sampler uImage0 : register(s0);
static const float PI = 3.14159265f;
float time;

float flashTime;

// 技能仍有可用层数时为 1：图标保持彩色，未转好的部分压暗，用来显示下一层的恢复进度。
float ready;

float2 rotatedBy(float2 vec, float radians)
{
    float num = (float) cos(radians);
    float num2 = (float) sin(radians);
    float2 v = vec;
    float2 result = float2(0, 0);
    result.x += (v.x * num - v.y * num2);
    result.y += (v.x * num2 + v.y * num);
    return result;
}
float Luminance(float3 color)//灰度化
{
    return dot(color, float3(0.3, 0.59, 0.11));
}

float4 PSFunction(float4 color0:COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 v = coords - float2(0.5, 0.5);
    float rot = atan2(v.y, v.x) + PI / 2;

    bool hidden;
    if (rot > 0)
    {
        float rot2 = lerp(0, PI, time / 0.5);//0-0.5时间时，取0-pi，没有大于pi的，
        hidden = rot > rot2;
    }
    else
    {
        float rot2 = lerp(-PI, 0, (time - 0.5) / 0.5); //0-0.5时间时，取0-pi，没有大于pi的，
        hidden = rot > rot2;
    }

    float4 color = tex2D(uImage0, coords);
    if (time < 1)
    {
        //仍有层数时可以释放：转好的部分正常显示，未转好的部分压暗但保留不透明度。
        if (ready > 0.5)
            return hidden ? float4(color.rgb * 0.4, color.a) * color0 : color * color0;

        if (hidden)
            return float4(0, 0, 0, 0);
        //对于还在冷却阶段的技能，转好的部分使用浅色替代。
        return Luminance(color.rgb) * 0.5f;
    }

    return color * color0 * lerp(2, 1, abs(flashTime - 0.5) / 0.5);
}


technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PSFunction();
    }
}
