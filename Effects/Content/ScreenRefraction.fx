sampler uImage0 : register(s0);//默认绘制屏幕
sampler uImage1 : register(s1);//用于折射屏幕的粒子之类的东西，需要先画在另一个render上。

//默认偏移量
float2 offset;

//偏移强度
float strength;


//屏幕扰动效果，用某个纹理或render的形状扭曲屏幕，使屏幕呈现这个形状的波纹
float4 PSFunction(float2 coords : TEXCOORD0,float4 inputColor : COLOR0) : COLOR0 //用一张图片 (uImagel) 去扭曲uImage0，r代表方向，g代表大小
{
    float4 DustColor = tex2D(uImage1, coords);
    
    //偏移量,由颜色r,g决定并映射为区间[-0.5,0.5]
    float2 moveVec = DustColor.rg-0.5;
    if (!any(DustColor))
        return tex2D(uImage0, coords);
    
    float4 ScreenColor = tex2D(uImage0, coords + (moveVec+offset) * strength);
    return ScreenColor * inputColor;
    
}

technique Technique1
{
    pass expand
    {
        PixelShader = compile ps_3_0 PSFunction();
    }

}