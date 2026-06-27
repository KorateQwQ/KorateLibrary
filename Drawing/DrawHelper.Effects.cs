using ReLogic.Utilities;

namespace KL.Drawing;

public partial class DrawHelper : ModSystem
{
    public enum ReColorState
    {
        None,
        NoBlack,
        ScreenMode,
        Grey,
        Bloom,
    }




    /// <summary>
    /// 径向消融效果。
    /// </summary>
    /// <param name="dissolveTex">用于决定消融边界形状的噪声贴图。</param>
    /// <param name="distortTex">用于整体扭曲画面的扰动贴图。</param>
    /// <param name="imageColor">整体颜色。</param>
    /// <param name="progress">消融进度，通常为 0~1。</param>
    /// <param name="dissolveTime">消融贴图的流动偏移。</param>
    /// <param name="dissolveScale">消融贴图大小缩放。</param>
    /// <param name="edgeWidth">消融边缘宽度，越小末端越实，越大末端越虚。</param>
    /// <param name="noiseStrength">消融贴图对边界形状的影响强度。</param>
    /// <param name="curveStrength">裁切线弯曲强度，0 为水平裁切，正值凸起，负值凹下去。</param>
    /// <param name="radialCenter">裁切线弯曲的中心位置，默认在图片中心。</param>
    /// <param name="sweepDirection">消融推进方向。</param>
    /// <param name="distortTime">扰动贴图的流动偏移。</param>
    /// <param name="distortStrength">整体扰动强度。</param>
    /// <param name="distortScale">扰动贴图大小缩放。</param>
    
    public static void RadialDissolve(Vector4? imageColor = null, 
        //***消融相关参数***//
        Texture2D dissolveTex = null, float progress = 0.0f, Vector2? dissolveTime = null, Vector2? dissolveScale = null,
        float edgeWidth = 0.08f, float noiseStrength = 0.22f, float curveStrength = 0.0f, Vector2? radialCenter = null, Vector2? sweepDirection = null, 
        //***扰动相关参数***//
        Texture2D distortTex = null, Vector2? distortTime = null, float distortStrength = 0.0f, Vector2? distortScale = null,
        //***内部纹理参数***//
        Texture2D imageTex = null, Vector2? internalTextureScale = null, Vector2? internalTextureOffset = null)
    {
        dissolveTex ??= PerLinNoiseX;
        distortTex ??= PerLinNoiseX;
        imageColor ??= Vector4.One;
        dissolveTime ??= Vector2.Zero;
        dissolveScale ??= Vector2.One;
        radialCenter ??= new Vector2(0.5f, 0.5f);
        sweepDirection ??= new Vector2(0.0f, 1.0f);
        distortTime ??= Vector2.Zero;
        distortScale ??= new Vector2(2.0f, 2.0f);
        internalTextureScale ??= Vector2.One;
        internalTextureOffset ??= Vector2.Zero;

        radialDissolve.SetValue("iColor", imageColor.Value);
        radialDissolve.SetValue("iTimeProgress", progress);
        radialDissolve.SetValue("iTimeDisolve", dissolveTime.Value);
        radialDissolve.SetValue("dissolveScale", dissolveScale.Value);
        radialDissolve.SetValue("edgeWidth", edgeWidth);
        radialDissolve.SetValue("noiseStrength", noiseStrength);
        radialDissolve.SetValue("curveStrength", curveStrength);
        radialDissolve.SetValue("radialCenter", radialCenter.Value);
        radialDissolve.SetValue("sweepDirection", sweepDirection.Value);
        radialDissolve.SetValue("iTimeDistort", distortTime.Value);
        radialDissolve.SetValue("distortStrength", distortStrength);
        radialDissolve.SetValue("distortScale", distortScale.Value);
        radialDissolve.SetValue("internalTextureScale", internalTextureScale.Value);
        radialDissolve.SetValue("internalTextureOffset", internalTextureOffset.Value);
        radialDissolve.SetValue("iUseInternalTexture", imageTex != null);

        radialDissolve.SetTexture(1, dissolveTex);
        radialDissolve.SetTexture(2, distortTex);
        radialDissolve.SetTexture(3, imageTex);
        radialDissolve.Apply();
    }
    /// <summary>
    /// 圆环进度条效果（对应 [CircleBar.fx](D:/Documents/My%20Games/Terraria/tModLoader/ModSources/KL/Effects/Content/CircleBar.fx)）。

    /// 调用该方法会写入参数并 <c>Apply</c> 当前 Pass，用于随后绘制的 Sprite。
    /// </summary>
    /// <param name="ringWidth">圆环宽度（UV 半径方向的宽度，建议范围 0~0.5）。</param>
    /// <param name="progress">进度（0~1）。0 表示不显示进度段，1 表示整圈进度段。</param>
    /// <param name="ringColor">圆环底色（RGBA，0~1）。传 null 使用默认值。</param>
    /// <param name="progressColor">进度段颜色（RGBA，0~1）。传 null 使用默认值。</param>
    /// <param name="startAngle">起始角度（弧度制）。默认值为 -/2（12 点方向）。</param>
    /// <param name="bloomStrength">进度部分的光晕强度</param>
    public static void ProgressCircleEffect(float progress , Vector4? ringColor = null, Vector4? progressColor = null,
        float ringWidth = 0.08f, float startAngle = -1.57079632679f,float bloomStrength = 0.00f)
    {
        ringColor ??= new Vector4(0.20f, 0.20f, 0.20f, 0.80f);
        progressColor ??= new Vector4(0.30f, 0.80f, 1.00f, 0.90f);

        circleBar.SetValue("RingWidth", ringWidth);
        circleBar.SetValue("StartAngle", startAngle);
        circleBar.SetValue("Progress", progress);
        circleBar.SetValue("RingColor", ringColor.Value);
        circleBar.SetValue("ProgressColor", progressColor.Value);
        circleBar.SetValue("BloomStrength", bloomStrength);

        circleBar.Apply();
    }

    /// <summary>
    /// 闪电effect,最好使用KL/Effects/Tex/Lightning绘制。
    /// </summary>
    /// <param name="coreStrength">闪电抖动强度，0为完全直线</param>
    /// <param name="coreFrequency">闪电抖动频率，值越小出现的抖动数量越少，反之则倾向于锯齿化</param>
    /// <param name="coreTime">闪电抖动iTime</param>
    /// <param name="edgeStrength">毛边强度，为0则不生效，过大时将完全溶解闪电</param>
    /// <param name="edgeFrequency">毛边频率</param>
    /// <param name="edgeTime">毛边iTime</param>
    /// <param name="bloomColor">额外颜色</param>
    /// <param name="unlockEndLightning">闪电默认起点和终点不受抖动影响，开启后终点会受到抖动影响。</param>
    /// <param name="coreEndFadeLength">闪电起点终点的范围，默认为贴图的0.12部分。这一部分不受抖动影响并且会逐渐缩窄</param>
    public static void LightningEffect(float coreStrength=0.3f, float coreFrequency = 0.2f, float coreTime = 0f, 
        float edgeStrength = 0f, float edgeFrequency = 0.7f,float edgeTime = 0,Vector4? bloomColor = null,bool unlockEndLightning = false,float coreEndFadeLength = 0.12f)
    {
        bloomColor ??= Vector4.One;
        
        lightning.SetValue("EdgeStrength", edgeStrength);
        lightning.SetValue("EdgeFrequency", edgeFrequency);
        lightning.SetValue("EdgeTime",edgeTime);

        lightning.SetValue("CoreStrength", coreStrength);
        lightning.SetValue("CoreFrequency", coreFrequency);
        lightning.SetValue("CoreTime",coreTime);
        lightning.SetValue("CoreRightEndFree",unlockEndLightning);
        lightning.SetValue("CoreEndFade",coreEndFadeLength);
        
        lightning.SetValue("BloomColor",bloomColor.Value);
        lightning.SetTexture(1,PerLinNoiseX);
        lightning.Apply();
    }
    /// <summary>
    /// 龙卷风
    /// </summary>
    /// <param name="effectColor">额外颜色</param>
    /// <param name="useRForAlpha">使用R通道作为透明度</param>
    /// <param name="iTime">时间参数（通常传 <c>Main.GlobalTimeWrappedHourly</c> 或自行累加的时间）。</param>
    /// <param name="cameraZ">透视强度（越小越强）。</param>
    /// <param name="drawScale">绘制缩放倍率（用于避免倾斜+置换后绘制不完全）。传 null 使用默认值。</param>
    /// <param name="rotationSpeed">旋转速度。</param>
    /// <param name="texScale">贴图缩放倍率（控制纹理密度）。传 null 使用默认值。</param>
    /// <param name="tilt">整体倾斜强度（沿倾斜方向拉伸/偏移的感觉）。</param>
    /// <param name="dissolveTex">消融贴图（对应 iChannel1）。传 null 视为关闭消融。</param>
    /// <param name="dissolveTexTiling">消融贴图倍率/平铺倍率（float2）。传 null 使用默认值。</param>
    /// <param name="dissolveThreshold">消融阈值（越大消融越多）。当 <paramref name="dissolveTex"/> 为 null 时会被强制为 0。</param>
    /// <param name="dissolveEdgeWidth">消融边界宽度。</param>
    /// <param name="dissolveEdgeColor">消融边界颜色（RGBA）。传 null 使用默认值。</param>
    /// <param name="horizontalFadeRange">水平边缘减淡范围（用于裁边/淡出）。</param>
    /// <param name="verticalFadeRange">垂直边缘减淡范围（用于裁边/淡出）。</param>
    /// <param name="noiseTex">置换噪声贴图（对应 iChannel2）。传 null 视为关闭置换。</param>
    /// <param name="displaceAmount">置换强度。当 <paramref name="noiseTex"/> 为 null 时会被强制为 0。</param>
    /// <param name="displaceNoiseScale">噪声图缩放倍率（越大噪声越密）。传 null 使用默认值。</param>
    /// <param name="displaceNoiseSpeed">噪声滚动速度（单位：UV/秒）</param>
    /// <param name="displaceNoiseContrast">噪声对比度：1=原样；大于1 更硬更碎；小于1 更柔</param>
    /// <param name="displaceNoiseBias">噪声偏置：让整体更偏向凸/凹（可为负）,默认为0</param>
    /// <param name="maxInset">贝塞尔形变最大内收/外扩幅度（控制轮廓变化强度）。</param>
    /// <param name="bezierP0">贝塞尔控制点 P0,代表着贴图左上角-1，0点 使用默认值。</param>
    /// <param name="bezierP1">贝塞尔控制点 P1。代表着贴图第一个控制点，默认为左侧四分之一处</param>
    /// <param name="bezierP2">贝塞尔控制点 P2。代表着贴图第二个控制点，默认为左侧四分之三处</param>
    /// <param name="bezierP3">贝塞尔控制点 P3，贝塞尔结束点，代表着贴图左下角-1，1点。</param>
    public static void TornadoEffect(
        // 主要绘制
        Vector4? effectColor = null,bool useRForAlpha=true, float iTime = 0f,  float cameraZ = 3.5f, Vector2? drawScale = null, float rotationSpeed = 0.2f,
        // 材质
        Vector2? texScale = null, float tilt = 2.0f,
        // 消融
        Texture2D dissolveTex = null, Vector2? dissolveTexTiling = null, float dissolveThreshold = 0.0f, float dissolveEdgeWidth = 0.05f, Vector4? dissolveEdgeColor = null,
        // 淡出（边缘减淡）
        float horizontalFadeRange = 0.02f, float verticalFadeRange = 0.02f,
        // 置换（噪声）
        Texture2D noiseTex = null, float displaceAmount = 0.05f, Vector2? displaceNoiseScale = null, Vector2? displaceNoiseSpeed = null, float displaceNoiseContrast = 1.0f, float displaceNoiseBias = 0.0f,
        // 贝塞尔（形状）
        float maxInset = 0.25f, Vector2? bezierP0 = null, Vector2? bezierP1 = null, Vector2? bezierP2 = null, Vector2? bezierP3 = null)
    {
        if(dissolveTex == null) dissolveThreshold = 0.0f;
        if(noiseTex == null) displaceAmount = 0.00f;

        effectColor ??= Vector4.One;
        drawScale ??= new Vector2(1);
        texScale ??= new Vector2(1f);

        dissolveTexTiling ??= new Vector2(1.0f, 1.0f);
        dissolveEdgeColor ??= new Vector4(1.0f, 0.6f, 0.2f, 1.0f);

        displaceNoiseScale ??= new Vector2(1.0f, 1.0f);
        displaceNoiseSpeed ??= new Vector2(0.1f, 0.1f);

        bezierP0 ??= new Vector2(-1.0f, 0.0f);
        bezierP1 ??= new Vector2(-1.0f, 0.25f);
        bezierP2 ??= new Vector2(-1.0f, 0.75f);
        bezierP3 ??= new Vector2(-1.0f, 1.0f);

        // ===== 主要绘制相关（默认值与 Tornado2.fx 一致） =====
        tornado.SetValue("EffectColor", effectColor.Value);
        tornado.SetValue("useRForAlpha", useRForAlpha);
        tornado.SetValue("CameraZ", cameraZ);
        tornado.SetValue("DrawScale", drawScale.Value);
        tornado.SetValue("RotationSpeed", rotationSpeed);

        // ===== 材质相关 =====
        tornado.SetValue("TexScale", texScale.Value);
        tornado.SetValue("Tilt", tilt);

        // ===== 消融参数（尽可能简单） =====
        tornado.SetValue("DissolveTexMultiplier", dissolveTexTiling.Value);
        tornado.SetValue("DissolveThreshold", dissolveThreshold);
        tornado.SetValue("DissolveEdgeWidth", dissolveEdgeWidth);
        tornado.SetValue("DissolveEdgeColor", dissolveEdgeColor.Value);

        // ===== 淡出参数 =====
        tornado.SetValue("HorizontalFadeRange", horizontalFadeRange);
        tornado.SetValue("VerticalFadeRange", verticalFadeRange);

        // ===== 置换图 =====
        tornado.SetValue("DisplaceAmount", displaceAmount);
        tornado.SetValue("DisplaceNoiseScale", displaceNoiseScale.Value);
        tornado.SetValue("DisplaceNoiseSpeed", displaceNoiseSpeed.Value);
        tornado.SetValue("DisplaceNoiseContrast", displaceNoiseContrast);
        tornado.SetValue("DisplaceNoiseBias", displaceNoiseBias);

        // ===== 贝塞尔曲线形变 =====
        tornado.SetValue("MaxInset", maxInset);
        tornado.SetValue("BezierP0", bezierP0.Value);
        tornado.SetValue("BezierP1", bezierP1.Value);
        tornado.SetValue("BezierP2", bezierP2.Value);
        tornado.SetValue("BezierP3", bezierP3.Value);

        tornado.SetValue("iTime", iTime);

        tornado.SetTexture(1, dissolveTex);
        tornado.SetTexture(2, noiseTex);

        tornado.Apply();
    }

    /// <summary>
    /// 绘制球体
    /// </summary>
    /// <param name="worldRotation"></param>
    /// <param name="localRotation"></param>
    /// <param name="drawInner">仅绘制球体背面内壁，默认为绘制正面</param>
    /// <param name="sphereColor">球体颜色</param>
    /// <param name="clipImageX">是否裁切贴图X轴</param>
    /// <param name="clipImageY">是否裁切贴图Y轴</param>
    /// <param name="specialYScale">Y轴缩放倍率，对应最左，中间，最右三个位置的图片Y轴额外缩放倍率</param>
    /// <param name="sphereScale">图片绘制倍率</param>
    /// <param name="texScale">贴图缩放倍率，可以使得纹理更密集或分散</param>
    /// <param name="dissolveTex">溶解贴图</param>
    /// <param name="dissolveAmount">溶解程度</param>
    /// <param name="dissolveTexScale">溶解贴图缩放倍率</param>
    /// <param name="dissolveTexFlow">溶解贴图流动速度</param>
    public static void SphereEffect(Vector3? worldRotation = null, Vector3? localRotation= null,bool drawInner = false,Vector4? sphereColor =null,
        bool clipImageX = false, bool clipImageY = false, Vector3? specialYScale = null,Vector2? sphereScale = null, Vector2? texScale = null,
        Texture2D dissolveTex = null, float dissolveAmount = 0.0f, Vector2? dissolveTexScale = null, Vector2? dissolveTexFlow = null)
    {
        worldRotation ??= Vector3.Zero;
        localRotation ??= Vector3.Zero;
        specialYScale ??= Vector3.One;
        sphereScale ??= Vector2.One;
        sphereColor ??= Vector4.One;
        texScale ??= Vector2.One;
        dissolveTexScale ??= Vector2.One;
        dissolveTexFlow ??= Vector2.Zero;
        
        sphereEffect.SetValue("RotWorldX",worldRotation.Value.X);
        sphereEffect.SetValue("RotWorldY",worldRotation.Value.Y);
        sphereEffect.SetValue("RotWorldZ",worldRotation.Value.Z);
        sphereEffect.SetValue("RotLocalX",localRotation.Value.X);
        sphereEffect.SetValue("RotLocalY",localRotation.Value.Y);
        sphereEffect.SetValue("RotLocalZ",localRotation.Value.Z);
        sphereEffect.SetValue("SphereColor",sphereColor.Value);
        sphereEffect.SetValue("clipImageX",clipImageX);
        sphereEffect.SetValue("clipImageY",clipImageY);
        sphereEffect.SetValue("SpecialYScale",specialYScale.Value);
        sphereEffect.SetValue("ImageScale",sphereScale.Value);
        sphereEffect.SetValue("TexScale",texScale.Value);
        
        sphereEffect.SetValue("dissolveAmount",dissolveAmount);
        sphereEffect.SetValue("dissolveTexScale",dissolveTexScale.Value);
        sphereEffect.SetValue("dissolveTexFlow",dissolveTexFlow.Value);
        
        sphereEffect.SetTexture(1,dissolveTex);
        sphereEffect.Apply(drawInner ? 1 : 0);
    }

    public static void SpinEffect(float rotation, float scale = 1, Color? color = null)
    {
        color ??= Color.White;
        spinEffect.SetValue("rot",rotation);
        spinEffect.SetValue("scale",scale);
        spinEffect.SetValue("bloomColor",color.Value.ToVector4());
        
        spinEffect.Apply();
    }

    public static void GlassEffect(Texture2D screenTargetForGlass, Vector2 offset,float strength = 1)
    {
        screenRefraction.SetValue("offset", offset);
        screenRefraction.SetValue("strength",strength);
        screenRefraction.SetTexture(1,screenTargetForGlass);
        //Main.graphics.GraphicsDevice.Textures[1] = screenTargetForGlass;
        //screenRefraction.CurrentTechnique.Passes[0].Apply();

        screenRefraction.Apply();
    }

    /// <summary>
    /// 直接创造一个径向模糊
    /// </summary>
    /// <param name="position">径向模糊中心</param>
    /// <param name="strength">初始强度，随时间消减为0</param>
    /// <param name="totalFrames"></param>
    /// <param name="decay"></param>
    /// <param name="iteration"></param>
    public static void CreateRadialBlur(Vector2 position,float strength = 0.004f, int totalFrames = 16, DecayType decay = DecayType.Lerp,int iteration = 30)
    {        
        DrawSystem.RadialBlurInfos.Add(new RadialBlurInfo(position,strength, totalFrames, decay,iteration));
    }

    public static void CreateRadialWaveWarp(Vector2 position, float minRadius = 50, float maxRadius = 400,
        int totalFrames = 30, float maxScale = 2.5f)
    {
        DrawSystem.RadialWaveWarpList.Add(new RadialWaveWarp(position,minRadius, maxRadius, totalFrames, maxScale));
    }
    
    
    //注意，目前ScreenMode模式只适用于render，并且需要传递当前RenderTarget
    public static void ReColorEffect(Vector4 color,ReColorState state = ReColorState.None, Texture2D background = null)
    {
        reColor.Parameters["newColor"].SetValue(color);
        Main.graphics.GraphicsDevice.Textures[1] = background;

        if (state == ReColorState.NoBlack)
        {
            reColor.CurrentTechnique.Passes[2].Apply();
        }
        else if (state == ReColorState.ScreenMode)
        {
           reColor.CurrentTechnique.Passes[1].Apply();
        }
        else if(state == ReColorState.Grey)
        {
            reColor.CurrentTechnique.Passes[3].Apply();
        }
        else if(state==ReColorState.Bloom)
        {
            reColor.CurrentTechnique.Passes[4].Apply();
        }
        else
        {
            reColor.CurrentTechnique.Passes[0].Apply();
        }
    }

    /// <summary>
    /// 用一个材质做遮罩切除图片超出遮罩的部分，另一个噪声材质用于消融图片内部。
    /// </summary>
    /// <param name="clipValueInside"></param>
    /// <param name="clipValueOutside"></param>
    /// <param name="imageColor"></param>
    /// <param name="Edge"></param>
    /// <param name="edgeColor"></param>
    /// <param name="uTime"></param>
    /// <param name="uTimeOut"></param>
    /// <param name="scale"></param>
    /// <param name="scaleOutside"></param>
    /// <param name="insideTex"></param>
    /// <param name="outsideTex"></param>
    /// <param name="colorMask"></param>
    public static void CommonMagicEffect(float clipValueInside = 0,float clipValueOutside = 0, Vector4 imageColor = (default),float Edge = 0, Vector4 edgeColor = (default),
        Vector2 uTime = default, Vector2 uTimeOut = default,Vector2? scale = null,Vector2? scaleOutside = null, Texture2D insideTex = null, Texture2D outsideTex = null,Texture2D colorMask = null)
    {
        colorMask ??= TextureAssets.MagicPixel.Value;
        scale ??=Vector2.One;
        scaleOutside ??=Vector2.One;
        insideTex ??= TextureAssets.MagicPixel.Value;
        outsideTex ??= TextureAssets.MagicPixel.Value;
        
        commonMagic.Parameters["clipValue"].SetValue(clipValueInside);
        commonMagic.Parameters["clipValueOut"].SetValue(clipValueOutside);
        commonMagic.Parameters["Edge"].SetValue(Edge);
        commonMagic.Parameters["EdgeColor"].SetValue(edgeColor);
        commonMagic.Parameters["imageColor"].SetValue(imageColor);
        commonMagic.Parameters["uTime"].SetValue(uTime);
        commonMagic.Parameters["uTimeOut"].SetValue(uTimeOut);
        commonMagic.Parameters["scale"].SetValue(scale.Value);
        commonMagic.Parameters["scale2"].SetValue(scaleOutside.Value);

        Main.graphics.GraphicsDevice.Textures[1] = insideTex;
        Main.graphics.GraphicsDevice.Textures[2] = outsideTex;
        Main.graphics.GraphicsDevice.Textures[3] = colorMask;

        commonMagic.CurrentTechnique.Passes[0].Apply();
    }
    /// <summary>
    /// 消融，裁切shader。当mask低于阈值时，将图片裁切。处于阈值和边缘之间时，以边缘颜色绘制
    /// </summary>
    /// <param name="threshold"></param>
    /// <param name="edge"></param>
    /// <param name="edgeColor"></param>
    /// <param name="mask"></param>
    public static void ClipEffect(float threshold = 0.5f,float edge = 0,Vector4? edgeColor = null, Vector4? imageColor = null,Texture2D mask = null, Vector2? maskScale = null,Vector2 maskTime = default)
    {
        edgeColor ??= Vector4.One;
        imageColor ??= Vector4.One;
        mask ??= PerLinNoiseX;
        maskScale ??= Vector2.One;
        
        clip.Parameters["ClipValue"].SetValue(threshold);
        clip.Parameters["Edge"].SetValue(edge);
        clip.Parameters["EdgeColor"].SetValue(edgeColor.Value);
        clip.Parameters["imageColor"].SetValue(imageColor.Value);
        clip.Parameters["noiseScale"].SetValue(maskScale.Value);
        clip.Parameters["noiseTime"].SetValue(maskTime);
        
        Main.graphics.GraphicsDevice.Textures[1] = mask;
        
        clip.CurrentTechnique.Passes[0].Apply();
    }
    
    /// <summary>
    /// Bloom方法，开启后的draw自带外发光效果（仅绘制发光，本体需重新绘制），默认外发光颜色为图片颜色*draw输入颜色。
    /// </summary>
    /// <param name="imageSize"></param>
    /// <param name="strength"></param>
    /// <param name="iteration"></param>
    public static void GaussianBlur(Vector2 imageSize,float strength=1)
    {
        bloom.Parameters["ImageSize"].SetValue(imageSize);
        bloom.Parameters["strength"].SetValue(strength);
        bloom.CurrentTechnique.Passes[0].Apply();
    }
    
    public static void GaussianBlurTwice(Vector2 imageSize,float strength=1,float strength2 = 1,Texture2D LastResult = null, float LastResultScale = 1)
    {
        bloom.Parameters["ImageSize"].SetValue(imageSize);
        bloom.Parameters["strength"].SetValue(strength);
        bloom.Parameters["strength2"].SetValue(strength2);
        bloom.Parameters["scale"].SetValue(LastResultScale);

        Main.graphics.GraphicsDevice.Textures[1] = LastResult;
        bloom.CurrentTechnique.Passes[1].Apply();
    }

    internal static void DrawOverflowColorEffect()
    {
        bloom.CurrentTechnique.Passes[2].Apply();
    }

    internal static void DrawUnderflowColorEffect()
    {
        bloom.CurrentTechnique.Passes[3].Apply();
    }
    
    /// <summary>
    /// 不采用图片本身的颜色，仅使用draw颜色作为外发光颜色。
    /// </summary>
    /// <param name="imageSize"></param>
    /// <param name="strength"></param>
    /// <param name="iteration"></param>
    public static void BloomEffectByGivenColor(Vector2 imageSize,float strength=1)
    {
        bloom.Parameters["ImageSize"].SetValue(imageSize);
        bloom.Parameters["strength"].SetValue(strength);
        bloom.CurrentTechnique.Passes[2].Apply();
    }

    public static void ScaleEffect(float scale)
    {
        scaleEffect.Parameters["scale"].SetValue(scale);
        scaleEffect.CurrentTechnique.Passes[0].Apply();
    }
    
    /// <summary>
    /// 以屏幕上的一点发起径向模糊
    /// </summary>
    /// <param name="screenPosition">屏幕坐标，如果是世界坐标需要先使用GetToScreen转换一次</param>
    /// <param name="strength"></param>
    /// <param name="iteration">迭代次数，不要超过30</param>
    public static void RadialBlurEffect(Vector2 screenPosition,float strength,int iteration=20)
    {
        radialBlur.Parameters["center"].SetValue(screenPosition/new Vector2(Main.screenWidth, Main.screenHeight));
        radialBlur.Parameters["Iteration"].SetValue(iteration);
        radialBlur.Parameters["strength"].SetValue(strength);
        
        radialBlur.CurrentTechnique.Passes[0].Apply();
    }

    /// <summary>
    /// 把一个材质作为形状，以该形状对屏幕进行空气扭曲。
    /// </summary>
    /// <param name="shape">材质形状，一般为用于绘制的原图或render</param>
    /// <param name="strength"></param>
    /// <param name="position">世界坐标</param>
    /// <param name="shapeSize">扭曲绘制的大小</param>
    public static void AirDistortionEffect(Texture2D shape, float strength,Vector2 position, Vector2? shapeSize=null)
    {
        shapeSize ??= Vector2.One;
        
        airDistortion.Parameters["strength"].SetValue(strength);
        Main.graphics.GraphicsDevice.Textures[1] = 空间扭曲角度;

        airDistortion.CurrentTechnique.Passes[0].Apply();
        
        
        RenderHelper.SaveScreenTarget();
        RenderHelper.SwitchRender(RenderHelper.Render);
        EndBeginDraw();
            
        DrawInWorld(空间扭曲角度,position,Color.White,shapeSize.Value);
        DrawInWorld(shape,position,Color.White,shapeSize.Value);
            
        RenderHelper.SwitchRender(Main.screenTarget);
            
        EndBeginDraw(0,1,false);
        airDistortion.Parameters["strength"].SetValue(strength);
        Main.graphics.GraphicsDevice.Textures[1] = RenderHelper.Render;
        airDistortion.CurrentTechnique.Passes[0].Apply();
            
        Main.spriteBatch.Draw(Main.screenTargetSwap,Vector2.Zero,Color.White);
        EndBeginDraw();
    }
    
    /// <summary>
    /// 空气扭曲，径向放大镜。以屏幕上一点为中心放大，放大效果会随着半径衰减。
    /// 如果随着时间逐渐放大半径，并减小强度和放大倍率的话，就可以实现爆炸时造成的空气扭曲效果
    /// </summary>
    /// <param name="screenPosition">屏幕坐标</param>
    /// <param name="strength">放大镜的总体影响强度</param>
    /// <param name="maxScale">放大镜的放大倍率，低于1会置为1</param>
    /// <param name="radius">放大镜的放大半径</param>
    public static void AirDistortionEffect_RadialWaveWarp(Vector2 screenPosition, float strength, float maxScale,float radius)
    {
        radius /= Main.screenHeight;
        
        airDistortion.Parameters["strength"].SetValue(strength);
        airDistortion.Parameters["uScreenResolution"].SetValue(Main.screenTarget.Size());
        airDistortion.Parameters["maxScale"].SetValue(maxScale);
        airDistortion.Parameters["screenPosition"].SetValue(screenPosition/new Vector2(Main.screenWidth, Main.screenHeight));
        airDistortion.Parameters["radius"].SetValue(radius);
        airDistortion.CurrentTechnique.Passes[1].Apply();
        
    }
    
    public static void QuickRadialBlurEffect(Vector2 screenPosition, float strength, int iteration = 30)
    {
        RenderHelper.SaveScreenTarget();
        RenderHelper.SwitchRender(Main.screenTarget,false);
        RadialBlurEffect(screenPosition,strength);
        Main.spriteBatch.Draw(RenderHelper.SaveScreenRender,Vector2.Zero, Color.White);
        
    }
    public static void HeatNoisePerturbEffect(float strength, Texture2D noise = null,Vector2? uTime = null)
    {
        noise ??= PerLinNoiseX;
        if(uTime==null)uTime = Vector2.Zero;

        heatNoisePerturb.Parameters["uTime"].SetValue(uTime.Value);
        heatNoisePerturb.Parameters["strength"].SetValue(strength);

        Main.graphics.GraphicsDevice.Textures[1] = noise;
        heatNoisePerturb.CurrentTechnique.Passes["Apply1"]?.Apply();
    }

    //去除灰度图的黑色，将颜色R作为透明度使用`z
    public static void NoBlackEffect()=>NoBlack.CurrentTechnique.Passes[0].Apply();

    //大于指定阈值时，
    public static void NoWhiteEffect(float threshold = 1f,int mode/*默认模式针对灰度图，将图片的r作为透明度使用，模式1针对普通图片，将a作为透明度使用*/ = 0)
    {
        noWhite.Parameters["threshold"].SetValue(0.8f);
        if(mode==0)
            noWhite.CurrentTechnique.Passes[0].Apply();
        else noWhite.CurrentTechnique.Passes[1].Apply();

    }
    
}