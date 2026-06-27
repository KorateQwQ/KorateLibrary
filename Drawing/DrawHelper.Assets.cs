using System.Linq;

namespace KL.Drawing;

public partial class DrawHelper : ModSystem
{
    public const int AlphaBlend = 0;
    public const int Additive = 1;
    public const int NonPremultiplied = 2;
    
    //消融，裁切shader
    static Effect commonMagic = null!;

    static Effect reColor = null!;
    
    static Effect airDistortion = null!;

    //消融，裁切shader
    static Effect clip = null!;
    
    //顶点绘制shader
    static Effect vertexDraw = null!;
    static Effect vertexDraw2 = null!;
    static Effect iceConeVertexDraw = null!;
    
    //圆角基本图形相关绘制

    static Effect basicShapeDraw = null!;
    //扰动shader
    static Effect heatNoisePerturb = null!;
    //径向模糊shader
    private static Effect radialBlur = null!;
    //Bloom外发光
    private static Effect bloom = null!;
    
    //将材质映射为球体
    private static Effect spherePerspective = null!;
    
    //将材质映射为球体
    private static Effect sphereEffect = null!;
    
    //缩放图片
    private static Effect scaleEffect = null!;

    //用于灰度图，将r值作为透明度使用
    public static Effect NoBlack = null!;
    
    //用于灰度图，将白色反转为黑色
    static Effect noWhite = null!;
    
    //屏幕折射，使用法线图来制作玻璃折射的效果
    static Effect screenRefraction = null!;
    
    //龙卷风effect
    static Effect tornado = null!;

    //旋转effect
    static Effect spinEffect = null!;
    
    //进度圈effect
    static Effect circleBar = null!;
    
    //雷电effect
    static Effect lightning = null!;
    
    //径向溶解effect
    static Effect radialDissolve = null!;
    
    //冻结effect
    internal static Effect frozen = null!;
    
    //黑白shader,很简单的shader无参数shader所以直接用。
    public static Effect grey = null!;
    
    
    //更好的柏林噪声
    public static Texture2D PerLinNoiseX = null!;
    
    //用于空气扭曲
    public static Texture2D 空间扭曲角度;
    
    public static Texture2D LightBloom;

    
    public static BlendState AlphaBlendNormal = new BlendState()//配置透明度保留状态
    {
        AlphaBlendFunction = BlendState.AlphaBlend.AlphaBlendFunction,
        AlphaDestinationBlend = BlendState.AlphaBlend.AlphaDestinationBlend,
        AlphaSourceBlend = BlendState.AlphaBlend.AlphaSourceBlend,
        ColorBlendFunction = (BlendFunction)0,
        ColorDestinationBlend = (Blend)5,
        ColorSourceBlend = BlendState.Additive.ColorSourceBlend,
        ColorWriteChannels = ColorWriteChannels.All,
        ColorWriteChannels1 = ColorWriteChannels.All,
        ColorWriteChannels2 = ColorWriteChannels.All,
        ColorWriteChannels3 = ColorWriteChannels.All,
        BlendFactor = Color.White,
        MultiSampleMask = -1
    };
    public static BlendState ReverseBS = new BlendState()//配置反色混合状态
    {
        AlphaBlendFunction = BlendState.Additive.AlphaBlendFunction,
        AlphaDestinationBlend = BlendState.Additive.AlphaDestinationBlend,
        AlphaSourceBlend = BlendState.Additive.AlphaSourceBlend,
        ColorBlendFunction = BlendFunction.ReverseSubtract,
        ColorDestinationBlend = BlendState.Additive.ColorDestinationBlend,
        ColorSourceBlend = BlendState.Additive.ColorSourceBlend,
        ColorWriteChannels = ColorWriteChannels.All,
        ColorWriteChannels1 = ColorWriteChannels.All,
        ColorWriteChannels2 = ColorWriteChannels.All,
        ColorWriteChannels3 = ColorWriteChannels.All,
        BlendFactor = Color.White,
        MultiSampleMask = -1
    };
    
    // 滤色混合模式（Screen）
    public static BlendState ScreenBlend = new BlendState()
    {
        AlphaBlendFunction = BlendFunction.Add,
        AlphaDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.One,
        ColorBlendFunction = BlendFunction.Add,
        ColorDestinationBlend = Blend.InverseSourceColor,
        ColorSourceBlend = Blend.One,
        ColorWriteChannels = ColorWriteChannels.All,
        ColorWriteChannels1 = ColorWriteChannels.All,
        ColorWriteChannels2 = ColorWriteChannels.All,
        ColorWriteChannels3 = ColorWriteChannels.All,
        BlendFactor = Color.White,
        MultiSampleMask = -1
    };
    #region 顶点结构体
    private struct CustomVertexInfo : IVertexType
    {
        private static VertexDeclaration _vertexDeclaration = new VertexDeclaration(new VertexElement[3]
        {
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
        });
        public Vector2 Position;
        public Color Color;
        public Vector3 TexCoord;

        public CustomVertexInfo(Vector2 position, Color color, Vector3 texCoord)
        {
            Position = position;
            Color = color;
            TexCoord = texCoord;
        }
        public CustomVertexInfo(Vector2 position, Vector4 color, Vector3 texCoord)
        {
            Position = position;
            Color = new Color(color.X, color.Y, color.Z, color.W)*255f;
            TexCoord = texCoord;
        }

        public VertexDeclaration VertexDeclaration
        {
            get
            {
                return _vertexDeclaration;
            }
        }
    }
    #endregion

    public static SpriteEffects GetCorrectSpriteEffect(Player player)
    {
        return (player.direction > 0) ? SpriteEffects.None | (player.gravDir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically) : SpriteEffects.FlipHorizontally | (player.gravDir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
    }
    
    public override void Load()
    {
        //Effects
        AutoLoadEffet();
        
        //Textures
        PerLinNoiseX = Mod.Assets.Request<Texture2D>("Effects/Tex/PerlinX", AssetRequestMode.ImmediateLoad).Value;
        空间扭曲角度 = Mod.Assets.Request<Texture2D>("Effects/Tex/空间扭曲角度", AssetRequestMode.ImmediateLoad).Value;
        LightBloom = Mod.Assets.Request<Texture2D>("Effects/Tex/LightBloom", AssetRequestMode.ImmediateLoad).Value;
        
        
        base.Load();
    }

    //自动加载所有声明的effect
    void AutoLoadEffet()
    {
        // 只获取Effect类型的字段
        FieldInfo[] effectFields = typeof(DrawHelper).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(Effect))
            .ToArray();

        foreach (FieldInfo field in effectFields)
        {
            string fieldName = field.Name;
            // 去掉开头的斜杠
            if (fieldName.StartsWith("_"))
                fieldName = fieldName.Substring(1);
            
            // 首字母大写
            if (fieldName.Length > 0)
                fieldName = char.ToUpper(fieldName[0]) + fieldName.Substring(1);

            // 使用EffectExtentions.LoadEffect加载特效
            Effect effect = EffectExtentions.LoadEffect(fieldName);
            
            // 设置字段值
            field.SetValue(null, effect);
        }
    }
    
    public struct TextureInfo
    {
        public Texture2D Texture;

        public int CurrentFrame = 0;

        public int XFrames = 1;
        
        public int YFrames = 1;
        
        public Vector2 OriginOffset = Vector2.Zero;

        public TextureInfo(Texture2D texture,int currentFrame = 0, int xFrames = 1,int yFrames = 1,Vector2 originOffset = default)
        {
            Texture = texture;
            CurrentFrame = currentFrame;
            XFrames = xFrames;
            YFrames = yFrames;
            OriginOffset = originOffset;
        }
    }
    //衰减类型，smoothStep和lerp
    public enum DecayType
    {
        Lerp,
        SmoothStep,
    }
    
    /// <summary>
    /// 用于径向模糊
    /// </summary>
    public class RadialBlurInfo
    {
        public DecayType Decay = DecayType.Lerp;
        public float Strength = 0.004f;
        public int TotalFrames = 16;
        public float CurrentFrame = 0;
        public bool Active = false;
        public int Iterations = 30;
        public Vector2 Position = Vector2.Zero;

        public RadialBlurInfo(Vector2 position,float strength = 0.004f, int totalFrames = 16, DecayType decay = DecayType.Lerp,int iterations = 30)
        {
            Active = true;
            Position = position;
            Strength = strength;
            TotalFrames = totalFrames;
            Decay = decay;
            Iterations = iterations;
        }

        public void Update()
        {
            if(!Active)return;
            CurrentFrame++;
            if (CurrentFrame >= TotalFrames)
            {
                Active = false;
            }
        }
    }
    
    public class RadialWaveWarp
    {
        public int TotalFrames = 16;
        public float CurrentFrame = 0;
        public bool Active = false;
        public float Radius = 200;
        public float MinRadius = 50;
        public float MaxRadius = 200;
        public Vector2 Position = Vector2.Zero;

        
        //本质为放大镜，放大倍率
        public float MaxScale = 2.5f;

        public RadialWaveWarp(Vector2 position, float minRadius = 50, float maxRadius = 200, int totalFrames = 16, float maxScale = 2.5f)
        {
            Active = true;
            Position = position;
            MinRadius = minRadius;
            MaxRadius = maxRadius;
            TotalFrames = totalFrames;
            MaxScale = maxScale;
        }

        public void Update()
        {
            if(!Active)return;
            CurrentFrame++;
            if (CurrentFrame >= TotalFrames)
            {
                Active = false;
            }
        }

    }

    public enum FrameType
    {
        Lerp,
        SmoothStep
    }
    /// <summary>
    /// 用于连续帧动画，传入多个FrameInfo可以对应多个连续帧并返回对应值。
    /// </summary>
    public class FrameInfo
    {
        public float MinValue = 0;
        public float MaxValue = 1;
        public int EndFrame = 10;
        public FrameType Type = FrameType.Lerp;

        /// <summary>
        /// 动画帧节点，会从MinValue到MaxValue之间进行插值
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="endFrame"></param>
        /// <param name="type"></param>
        public FrameInfo(float minValue, float maxValue, int endFrame, FrameType type = FrameType.Lerp)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            EndFrame = endFrame;
            Type = type;
        }
    }
    
    public struct HeatNoiseInfo
    {
        public bool Enable = false;

        public Texture2D Noise = null;
        
        public float Strength = 0.2f;
        
        public Vector2 TexScale = Vector2.One;

        public Vector2 uTime = Vector2.Zero;
        
        public HeatNoiseInfo(bool enable, Texture2D noise, float strength=0.2f, Vector2? texScale=null, Vector2 uTime = default)
        {
            texScale??= Vector2.One;
            
            Enable = enable;
            Noise = noise;
            Strength = strength;
            TexScale = texScale.Value;
            this.uTime = uTime;
        }
    }
    
    public struct ClipInfo
    {
        public bool Enable = false;

        public Texture2D Noise = null;
        
        public float Threshold = 0.2f;
        
        public float Edge = 0.0f;
        
        public Color EdgeColor = Color.White;

        
        public Vector2 TexScale = Vector2.One;

        public Vector2 uTime = Vector2.Zero;

        public ClipInfo(bool enable, Texture2D noise, float threshold = 0.2f, float edge = 0.0f,Color edgeColor = default,
            Vector2? texScale = null, Vector2 uTime = default)
        {
            texScale??= Vector2.One;
            if(edgeColor == default)edgeColor = Color.White;
            EdgeColor = edgeColor;
            
            Enable = enable;
            Noise = noise;
            Threshold = threshold;
            Edge = edge;
            TexScale = texScale.Value;
            this.uTime = uTime;
        }
    }

    /// <summary>
    /// 用于连续帧动画时取值
    /// </summary>
    /// <param name="frameInfos">每多一个节点，就需要一个帧信息，记录一个区间的最小值和最大值，以及平滑方式。</param>
    /// <param name="currentFrame">实际帧</param>
    /// <param name="startFrame">起始帧</param>
    /// <returns></returns>
    public static float GetFrameValue(List<FrameInfo> frameInfos,int currentFrame, int startFrame = 0,bool clamp = false)
    {
        float result = 0;
        int lastEndFrame = 0;
        currentFrame-=startFrame;
        if(currentFrame<0)currentFrame = 0;
        foreach (var frameInfo in frameInfos)
        {
            int maxFrame = frameInfo.EndFrame-lastEndFrame;
            float realFrame = currentFrame - lastEndFrame;
            lastEndFrame = frameInfo.EndFrame+1;

            if (currentFrame >= lastEndFrame)
            {
                continue;
            }

            if(clamp&&realFrame > maxFrame)realFrame = maxFrame;
            
            //Main.NewText("realFrame: " + realFrame + " maxFrame: " + maxFrame + " lastEndFrame " + lastEndFrame);

            if (frameInfo.Type == FrameType.Lerp)
            {
                result = MathHelper.Lerp(frameInfo.MinValue, frameInfo.MaxValue, realFrame / maxFrame);
                break;
            }
            else
            {
                result = MathHelper.SmoothStep(frameInfo.MinValue, frameInfo.MaxValue, realFrame / maxFrame);
                break;
            }
        }
        return result;
    }

}