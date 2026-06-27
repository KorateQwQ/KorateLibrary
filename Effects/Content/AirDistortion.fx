sampler uImage0 : register(s0);
sampler uImagel : register(s1);

float strength;
float radius; //半径，表示放大镜的范围
float2 uScreenResolution; //屏幕分辨率，x为宽度，y为高度
float2 screenPosition; //屏幕位置，x为横坐标，y为纵坐标
float maxScale;//最大放大比例,必须大于1

//屏幕扰动效果，用某个纹理或render的形状扭曲屏幕，使屏幕呈现这个形状的波纹
float4 PSFunction(float2 coords : TEXCOORD0,float4 inputColor : COLOR0) : COLOR0 //用一张图片 (uImagel) 去扭曲uImage0，r代表方向，g代表大小
{
    float4 color = tex2D(uImage0, coords);
    float4 color2 = tex2D(uImagel, coords);

    float2 vec = float2(0, 0); //表示移动的向量
    float rot = color2.r * 6.28; //把r(0~1)转化为弧度制的角度(0~2*pi)
        
    vec = float2(cos(rot), sin(rot)) * color2.g * strength;
    return tex2D(uImage0, coords + vec) * inputColor;


}

//放大镜效果，用于空气波纹
float4 PSFunction2(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0 //放大镜，越靠近中心放大越明显
{
    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return color;
    
    // pos 就是中心了
    float2 pos = screenPosition;
    // offset 是中心到当前点的向量
    float2 offset = (coords - pos);
    // 因为长宽比不同进行修正
    float2 rpos = offset * float2(uScreenResolution.x / uScreenResolution.y, 1);
    float dis = length(rpos);
    dis = min(dis, radius); // 限制最大距离为半径

    float2 effectOffset = lerp(offset / maxScale, offset, dis / radius);
    //根据强度做插值，强度越小越接近原图效果，0则为完全无影响,strength必须为区间0-1之间
    float2 finalOffset = lerp(offset, effectOffset, strength);
    return tex2D(uImage0, pos + finalOffset) * inputColor;

}
technique Technique1
{
    pass expand
    {
        PixelShader = compile ps_2_0 PSFunction();
    }
    pass expand2
    {
        PixelShader = compile ps_2_0 PSFunction2();
    }
}