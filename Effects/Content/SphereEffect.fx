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
// ===== 双层合并（pass 2 正面 / pass 3 背面）=====
// 一次绘制同一形状的两层：先 Additive 层，再 NonPremultiplied 层。
// 输出预乘颜色，需配合 BlendState.AlphaBlend 使用；颜色已包含顶点色，绘制时顶点色用白色。
// 仅在浮点目标（Bloom 捕获）上与逐层绘制一致：8 位目标会在 Additive 层后截断目标颜色，需逐层绘制。
float4 DualAddColor = float4(1.0, 1.0, 1.0, 1.0);
float4 DualOverColor = float4(1.0, 1.0, 1.0, 1.0);
float dualAddDissolve = 0.0f;
float dualOverDissolve = 0.0f;

float4 DualPS(float2 texCoord, float zSign)
{
    const float PI = 3.14159265;

    float2 uv = texCoord * 2.0 - 1.0;
    float r2 = dot(uv, uv);
    if (r2 > 1.0)
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    float3 pos = float3(uv.x, uv.y, zSign * sqrt(1.0 - r2));

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

    float3 worldX = float3(pos.x, pos.y * cwx - pos.z * swx, pos.y * swx + pos.z * cwx);
    float3 worldY = float3(worldX.x * cwy + worldX.z * swy, worldX.y, -worldX.x * swy + worldX.z * cwy);
    float3 worldZ = float3(worldY.x * cwz - worldY.y * swz, worldY.x * swz + worldY.y * cwz, worldY.z);

    float3 localX = float3(worldZ.x, worldZ.y * clx - worldZ.z * slx, worldZ.y * slx + worldZ.z * clx);
    float3 localY = float3(localX.x * cly + localX.z * sly, localX.y, -localX.x * sly + localX.z * cly);
    float3 localZ = float3(localY.x * clz - localY.y * slz, localY.x * slz + localY.y * clz, localY.z);

    float u = atan2(localZ.x, localZ.z) / (2.0 * PI) + 0.5;
    float v = asin(localZ.y) / PI + 0.5;

    float2 scale = max(ImageScale, float2(0.0001, 0.0001));
    float2 scaledUV = (float2(u, v) - 0.5) / scale + 0.5;

    float t = saturate(scaledUV.x);
    float oneMinus = 1.0 - t;
    float yScale = SpecialYScale.x * oneMinus * oneMinus
        + 2.0 * SpecialYScale.y * oneMinus * t
        + SpecialYScale.z * t * t;
    scaledUV.y = (scaledUV.y - 0.5) / max(yScale, 0.0001) + 0.5;

    float2 texScaleSafe = max(TexScale, float2(0.0001, 0.0001));
    float2 tiledUV = frac((scaledUV - 0.5) * texScaleSafe + 0.5);

    bool outX = clipImageX && (scaledUV.x < 0.0 || scaledUV.x > 1.0);
    bool outY = clipImageY && (scaledUV.y < 0.0 || scaledUV.y > 1.0);
    if (outX || outY)
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    float4 tex = tex2D(iChannel0, tiledUV);
    float fade = tex2D(iChannel1, (float2(u, v) - 0.5) / dissolveTexScale + 0.5).r;

    float4 addColor = fade < dualAddDissolve ? float4(0.0, 0.0, 0.0, 0.0) : tex * DualAddColor;
    float4 overColor = fade < dualOverDissolve ? float4(0.0, 0.0, 0.0, 0.0) : tex * DualOverColor;

    float3 premul = overColor.rgb * overColor.a + addColor.rgb * addColor.a * (1.0 - overColor.a);
    return float4(premul, overColor.a);
}

float4 DualFrontPS(float2 texCoord : TEXCOORD0) : COLOR0
{
    return DualPS(texCoord, 1.0);
}

float4 DualBackPS(float2 texCoord : TEXCOORD0) : COLOR0
{
    return DualPS(texCoord, -1.0);
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
    pass dualFront
    {
        PixelShader = compile ps_3_0 DualFrontPS();
    }
    pass dualBack
    {
        PixelShader = compile ps_3_0 DualBackPS();
    }
}