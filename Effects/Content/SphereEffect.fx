sampler iChannel0 : register(s0);
sampler iChannel1 : register(s1);//消融纹理

float RotWorldX ; // Command(min=0,max=4,step=0.1)
float RotWorldY;
float RotWorldZ;
float RotLocalX;
float RotLocalY;
float RotLocalZ;

float4 SphereColor = float4(1.0, 1.0, 1.0, 1.0);

bool clipImageX = false;
bool clipImageY = false;

//额外Y轴缩放，和图片缩放乘算，对应图片最左，中，最右三个部分应该应用的缩放，比如float3(0.0, 0.5, 1.0)意味着图片从左到右线性增大呈三角形。
float3 SpecialYScale = float3(1.0, 1.0, 1.0);
float2 ImageScale = float2(1.0, 1.0);

// 贴图缩放（>1 表示更密，<1 表示更稀）
float2 TexScale = float2(1.0, 1.0);


// ===== 消融效果  =====
//消融阈值,默认为0,表示不消融
float dissolveAmount = 0.0f;
//消融贴图大小
float2 dissolveTexScale = float2(1.0f, 1.0f);
float2 dissolveTexFlow = float2(0.0f, 0.0f);

float4 MainPS(float2 texCoord : TEXCOORD0,float4 Color: COLOR0) : COLOR0
{
    //RotWorldX = iTime*2.0f;
    const float PI = 3.14159265;

    float2 uv = texCoord * 2.0 - 1.0;
    float r2 = dot(uv, uv);
    if (r2 > 1.0)
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    float z = sqrt(1.0 - r2);
    float3 posFront = float3(uv.x, uv.y, z);

    float cwx = cos(RotWorldY);
    float swx = sin(RotWorldY);
    float cwy = cos(RotWorldX);
    float swy = sin(RotWorldX);
    float cwz = cos(RotWorldZ);
    float swz = sin(RotWorldZ);

    float clx = cos(RotLocalY);
    float slx = sin(RotLocalY);
    float cly = cos(RotLocalX);
    float sly = sin(RotLocalX);
    float clz = cos(RotLocalZ);
    float slz = sin(RotLocalZ);

    float3 worldXFront = float3(posFront.x, posFront.y * cwx - posFront.z * swx, posFront.y * swx + posFront.z * cwx);
    float3 worldYFront = float3(worldXFront.x * cwy + worldXFront.z * swy, worldXFront.y, -worldXFront.x * swy + worldXFront.z * cwy);
    float3 worldZFront = float3(worldYFront.x * cwz - worldYFront.y * swz, worldYFront.x * swz + worldYFront.y * cwz, worldYFront.z);

    float3 localXFront = float3(worldZFront.x, worldZFront.y * clx - worldZFront.z * slx, worldZFront.y * slx + worldZFront.z * clx);
    float3 localYFront = float3(localXFront.x * cly + localXFront.z * sly, localXFront.y, -localXFront.x * sly + localXFront.z * cly);
    float3 localZFront = float3(localYFront.x * clz - localYFront.y * slz, localYFront.x * slz + localYFront.y * clz, localYFront.z);
    
    float uFront = atan2(localZFront.x, localZFront.z) / (2.0 * PI) + 0.5;
    float vFront = asin(localZFront.y) / PI + 0.5;


    float2 scale = max(ImageScale, float2(0.0001, 0.0001));
    float2 scaledUVFront = (float2(uFront, vFront) - 0.5) / scale + 0.5;

    float tFront = saturate(scaledUVFront.x);

    float oneMinusFront = 1.0 - tFront;

    float yScaleFront = SpecialYScale.x * oneMinusFront * oneMinusFront
        + 2.0 * SpecialYScale.y * oneMinusFront * tFront
        + SpecialYScale.z * tFront * tFront;
    

    float yScaleFrontSafe = max(yScaleFront, 0.0001);

    scaledUVFront.y = (scaledUVFront.y - 0.5) / yScaleFrontSafe + 0.5;

    // 应用贴图缩放，并通过 frac 实现 UV 自循环（贴图相连）
    float2 texScaleSafe = max(TexScale, float2(0.0001, 0.0001));
    float2 tiledUVFront = frac((scaledUVFront - 0.5) * texScaleSafe + 0.5);

    bool outFrontX = clipImageX && (scaledUVFront.x < 0.0 || scaledUVFront.x > 1.0);
    bool outFrontY = clipImageY && (scaledUVFront.y < 0.0 || scaledUVFront.y > 1.0);

    float4 frontColor = (outFrontX || outFrontY) ? float4(0.0, 0.0, 0.0, 0.0) : tex2D(iChannel0, tiledUVFront)*Color*SphereColor;

    //实现消融效果：
    float2 fadeUVFront = (float2(uFront, vFront) - 0.5) / dissolveTexScale + 0.5;
    float4 fadeColor = (outFrontX || outFrontY) ? float4(0.0, 0.0, 0.0, 0.0) : tex2D(iChannel1, fadeUVFront);
    if (fadeColor.r<dissolveAmount)
        return float4(0.0, 0.0, 0.0, 0.0);
    
    return frontColor;
}

//专门用来绘制球体背面，节省计算。
float4 MainPS2(float2 texCoord : TEXCOORD0,float4 Color: COLOR0) : COLOR0
{
    //RotWorldX = iTime*2.0f;
    const float PI = 3.14159265;

    float2 uv = texCoord * 2.0 - 1.0;
    float r2 = dot(uv, uv);
    if (r2 > 1.0)
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    float z = sqrt(1.0 - r2);
    float3 posBack = float3(uv.x, uv.y, -z);

    float cwx = cos(RotWorldY);
    float swx = sin(RotWorldY);
    float cwy = cos(RotWorldX);
    float swy = sin(RotWorldX);
    float cwz = cos(RotWorldZ);
    float swz = sin(RotWorldZ);

    float clx = cos(RotLocalY);
    float slx = sin(RotLocalY);
    float cly = cos(RotLocalX);
    float sly = sin(RotLocalX);
    float clz = cos(RotLocalZ);
    float slz = sin(RotLocalZ);
    
    float3 worldXBack = float3(posBack.x, posBack.y * cwx - posBack.z * swx, posBack.y * swx + posBack.z * cwx);
    float3 worldYBack = float3(worldXBack.x * cwy + worldXBack.z * swy, worldXBack.y, -worldXBack.x * swy + worldXBack.z * cwy);
    float3 worldZBack = float3(worldYBack.x * cwz - worldYBack.y * swz, worldYBack.x * swz + worldYBack.y * cwz, worldYBack.z);

    float3 localXBack = float3(worldZBack.x, worldZBack.y * clx - worldZBack.z * slx, worldZBack.y * slx + worldZBack.z * clx);
    float3 localYBack = float3(localXBack.x * cly + localXBack.z * sly, localXBack.y, -localXBack.x * sly + localXBack.z * cly);
    float3 localZBack = float3(localYBack.x * clz - localYBack.y * slz, localYBack.x * slz + localYBack.y * clz, localYBack.z);
    
    float uBack = atan2(localZBack.x, localZBack.z) / (2.0 * PI) + 0.5;
    float vBack = asin(localZBack.y) / PI + 0.5;

    float2 scale = max(ImageScale, float2(0.0001, 0.0001));
    float2 scaledUVBack = (float2(uBack, vBack) - 0.5) / scale + 0.5;

    float tBack = saturate(scaledUVBack.x);

    float oneMinusBack = 1.0 - tBack;

    float yScaleBack = SpecialYScale.x * oneMinusBack * oneMinusBack
        + 2.0 * SpecialYScale.y * oneMinusBack * tBack
        + SpecialYScale.z * tBack * tBack;

    float yScaleBackSafe = max(yScaleBack, 0.0001);

    scaledUVBack.y = (scaledUVBack.y - 0.5) / yScaleBackSafe + 0.5;

    // 应用贴图缩放，并通过 frac 实现 UV 自循环（贴图相连）
    float2 texScaleSafe = max(TexScale, float2(0.0001, 0.0001));
    float2 tiledUVBack = frac((scaledUVBack - 0.5) * texScaleSafe + 0.5);
    
    bool outBackX = clipImageX && (scaledUVBack.x < 0.0 || scaledUVBack.x > 1.0);
    bool outBackY = clipImageY && (scaledUVBack.y < 0.0 || scaledUVBack.y > 1.0);

    float4 backColor = (outBackX || outBackY) ? float4(0.0, 0.0, 0.0, 0.0) : tex2D(iChannel0, tiledUVBack)*Color*SphereColor;

    //实现消融效果：
    float2 fadeUV = (float2(uBack, vBack) - 0.5) / dissolveTexScale + 0.5;
    float4 fadeColor = (outBackX || outBackY) ? float4(0.0, 0.0, 0.0, 0.0) : tex2D(iChannel1, fadeUV);
    if (fadeColor.r<dissolveAmount)
        return float4(0.0, 0.0, 0.0, 0.0);

    return backColor;
}
technique Technique1
{
    pass expand
    {
        PixelShader = compile ps_3_0 MainPS();
    }
    pass expand
    {
        PixelShader = compile ps_3_0 MainPS2();
    }
}