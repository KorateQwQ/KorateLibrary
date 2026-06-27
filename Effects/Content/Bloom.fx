sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float2 ImageSize;
float strength;
float strength2;
int iteration;
float scale;

float gauss[3][3] =
{
    0.075, 0.124, 0.075,
    0.124, 0.204, 0.124,
    0.075, 0.124, 0.075
};

float gauss2[5][5] =
{
     0.0030, 0.0133, 0.0219, 0.0133, 0.0030 ,
     0.0133, 0.0596, 0.0983, 0.0596, 0.0133 ,
     0.0219, 0.0983, 0.1621, 0.0983, 0.0219 ,
     0.0133, 0.0596, 0.0983, 0.0596, 0.0133 ,
     0.0030, 0.0133, 0.0219, 0.0133, 0.0030 
};
float Luminance(float3 color)
{
    return dot(color, float3(0.3, 0.59, 0.11));
}

float OverLay(float A, float B)
{
    if (B <= 0.5f)
    {
        return 2 * A * B;

    }
    return 1 - 2 * (1 - A) * (1 - B);
}
float HardLight(float B, float A)
{
    if (B <= 0.5f)
    {
        return 2 * A * B;

    }
    return 1 - 2 * (1 - A) * (1 - B);

}

// ACES 将颜色空间从线性空间转换为ACES颜色空间
float3 ACESFilmic(float3 x)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
}

float4 Gaussianblur(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float2 texelSize = float2(1 / ImageSize.x, 1 / ImageSize.y);
    
    float4 blurResult = float4(0, 0, 0, 0);    
    for (int y = -2; y <= 2; y++)
    {
        for (int x = -2; x <= 2; x++)
        {
            // �����������ƫ��
            float2 offset = float2(x, y) * texelSize;
            
            // ��������Ӧ�ø�˹Ȩ��
            float weight = gauss2[y + 2][x + 2];
            float2 uv = coords + offset;
            uv.x = clamp(uv.x, 0, 1);
            uv.y = clamp(uv.y, 0, 1);
            blurResult += tex2D(uImage0, uv) * weight;
        }
    }
    return blurResult * inputColor * strength;

}

float4 GaussianblurTwice(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float4 blurResult = float4(0, 0, 0, 0);
    
    float4 LastblurResult = tex2D(uImage1, coords)*strength;
    
    float2 move = coords - float2(0.5, 0.5);
    float finalScale = scale;
    if (finalScale != 0)
        finalScale = 1 / finalScale;
    float2 finalCoords = float2(0.5, 0.5) + move * finalScale;
    
    if (finalCoords.x < 0 || finalCoords.x > 1 || finalCoords.y < 0 || finalCoords.y > 1)
        LastblurResult = float4(0, 0, 0, 0);
    
    LastblurResult = tex2D(uImage1, finalCoords) * strength;
    

    float2 texelSize = float2(1 / ImageSize.x, 1 / ImageSize.y);
    
    for (int y = -2; y <= 2; y++)
    {
        for (int x = -2; x <= 2; x++)
        {
            float2 offset = float2(x, y) * texelSize;
            
            float weight = gauss2[y + 2][x + 2];
            
            float2 uv = coords + offset;
            uv.x = clamp(uv.x, 0, 1);
            uv.y = clamp(uv.y, 0, 1);
            blurResult += tex2D(uImage0, uv) * weight * strength2;

        }
    }
    
    return blurResult + LastblurResult;//float4(1, 1, 1, 1) - ((float4(1, 1, 1, 1) - blurResult) * (float4(1, 1, 1, 1) - LastblurResult));
    //blurResult + LastblurResult;

}

//提取render中亮度大于1的像素
float4 DrawOverflowColor(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);

    // 计算感知亮度
    float brightness = Luminance(color);
    if (brightness>1)
    {
        // 提取大于1的亮度部分，使用smooth过渡避免硬边缘
        float bloomFactor = saturate((brightness - 1.0) * 0.5); // 0.5控制过渡柔和度
        float4 bloomColor = color * bloomFactor;
    
        // 保持alpha通道
        bloomColor.a = color.a;
        return color;
    }

    return float4(0, 0, 0, 0);

}

//提取render中小于等于1的像素
float4 DrawUnderflowColor(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float4 originalColor = tex2D(uImage0, coords);
    
    // 计算感知亮度
    float brightness = Luminance(originalColor.rgb);

    if (brightness<=1)return originalColor;
    
    float3 sceneColor = originalColor.rgb/brightness;

    
    return float4(sceneColor,originalColor.a);

}



technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 Gaussianblur();
    }
    pass Apply1
    {
        PixelShader = compile ps_3_0 GaussianblurTwice();
    }

    pass Overflow
    {
        PixelShader = compile ps_3_0 DrawOverflowColor();
    }
    pass Underflow
    {
        PixelShader = compile ps_3_0 DrawUnderflowColor();
    }
}