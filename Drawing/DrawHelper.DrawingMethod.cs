using SilkyUIFramework.Graphics2D;

namespace KL.Drawing;

public enum PingPongWaveType
{
    Linear,
    SmoothStep,
    Sin
}

public partial class DrawHelper : ModSystem
{
    private static readonly FieldInfo BeginCalledField = typeof(SpriteBatch).GetField("beginCalled", BindingFlags.NonPublic | BindingFlags.Instance);
    
    #region 快捷绘制方法以及绘制模式
    static BlendState GetBlendState(int blendState)
    {
        BlendState bs = BlendState.AlphaBlend;
        if (blendState == 1) bs = BlendState.Additive;
        if(blendState == 2) bs = BlendState.NonPremultiplied;
        if (blendState == 3) bs = ReverseBS;
        if (blendState == 4) bs = AlphaBlendNormal;
        return bs;
    }
    public static void DrawInWorld(TextureInfo textureInfo,Vector2? position =null,Color? color = null,Vector2? scale = null,float rotation = 0,SpriteEffects spriteEffects = SpriteEffects.None)
    {
        position??=Vector2.Zero;
        color??=Color.White;
        scale??=Vector2.One;
        
        Main.spriteBatch.Draw(textureInfo.Texture, position.Value - Main.screenPosition, textureInfo.Texture.GetRec(textureInfo.CurrentFrame,textureInfo.XFrames,textureInfo.YFrames), 
            color.Value, rotation, textureInfo.Texture.Origin(textureInfo.XFrames,textureInfo.YFrames)+textureInfo.OriginOffset, scale.Value, spriteEffects, 0);
    }
    public static void DrawInScreen(TextureInfo textureInfo,Vector2? position =null,Color? color = null,Vector2? scale = null,float rotation = 0,SpriteEffects spriteEffects = SpriteEffects.None)
    {
        position??=Vector2.Zero;
        color??=Color.White;
        scale??=Vector2.One;
        
        Main.spriteBatch.Draw(textureInfo.Texture, position.Value, textureInfo.Texture.GetRec(textureInfo.CurrentFrame,textureInfo.XFrames,textureInfo.YFrames), 
            color.Value, rotation, textureInfo.Texture.Origin(textureInfo.XFrames,textureInfo.YFrames)+textureInfo.OriginOffset, scale.Value, spriteEffects, 0);
    }
    public static void DrawInWorld(Texture2D texture,Vector2? position =null,Color? color = null,Vector2? scale = null,float rotation = 0,SpriteEffects spriteEffects = SpriteEffects.None)
    {
        TextureInfo textureInfo = new TextureInfo(texture);
        position??=Vector2.Zero;
        color??=Color.White;
        scale??=Vector2.One; 
        
        DrawInWorld(textureInfo,position,color,scale, rotation, spriteEffects);
    }
    
    public static void DrawItemInWorld(Item item,Vector2? position =null,Color? color = null,Vector2? scale = null,float rotation = 0,SpriteEffects spriteEffects = SpriteEffects.None)
    {
        Main.instance.LoadItem(item.type);
        Texture2D texture = TextureAssets.Item[item.type].Value;

        position??=item.Center;
        color??=Color.White;
        scale??=Vector2.One;

        Rectangle rec = new Rectangle();
        if (Main.itemAnimations[item.type] != null) rec = Main.itemAnimations[item.type].GetFrame(texture, -1);
        else rec = texture.Frame();
        
        Main.spriteBatch.Draw(texture, position.Value - Main.screenPosition, rec, 
            color.Value, rotation, new Vector2(rec.Width*0.5f, rec.Height * 0.5f), scale.Value, spriteEffects, 0);
    }
    
    public static void DrawInScreen(Texture2D texture,Vector2? position =null,Color? color = null,Vector2? scale = null,float rotation = 0,SpriteEffects spriteEffects = SpriteEffects.None)
    {
        TextureInfo textureInfo = new TextureInfo(texture);
        position??=Vector2.Zero;
        color??=Color.White;
        scale??=Vector2.One;
        DrawInScreen(textureInfo, position, color, scale, rotation, spriteEffects);
    }
    
    #endregion
    #region 算弧线以及平滑处理
    public static Vector2 GetPointOnParabola(Vector2 center, float focalLength, float rotation, float t)
    {
        // Calculate the y value using the standard form of the parabola equation: y = (1 / (4 * focalLength)) * t^2
        float a = 1 / (4 * focalLength);
        float y = a * t * t;

        // Create the point vector
        Vector2 point = new Vector2(t, y);

        // Create the rotation matrix
        Matrix rotationMatrix = Matrix.CreateRotationZ(rotation);

        // Rotate the point
        Vector2 rotatedPoint = Vector2.Transform(point, rotationMatrix);

        // Translate the point to the parabola's vertex
        Vector2 finalPoint = center + rotatedPoint;

        return finalPoint;
    }
    public Vector2[] smooth(Vector2[] vecs, int extraLength)//平滑处理，增加标记的坐标点
    {
        int l = vecs.Length;
        extraLength += l;

        Vector2[] scVecs = new Vector2[extraLength];
        for (int n = 0; n < extraLength; n++)
        {
            float t = n / (float)extraLength;
            float k = (l - 1) * t;
            int i = (int)k;
            float vk = k % 1;
            if (i == 0)
            {
                scVecs[n] = Vector2.CatmullRom(2 * vecs[0] - vecs[1], vecs[0], vecs[1], vecs[2], vk);
            }
            else if (i == l - 2)
            {
                scVecs[n] = Vector2.CatmullRom(vecs[l - 3], vecs[l - 2], vecs[l - 1], 2 * vecs[l - 1] - vecs[l - 2], vk);
            }
            else
            {
                scVecs[n] = Vector2.CatmullRom(vecs[i - 1], vecs[i], vecs[i + 1], vecs[i + 2], vk);
            }
        }
        return scVecs;
    }

    #endregion
    
    
    /// <summary>
    /// 根据最小值以及最大值给出一个上下波动的曲线
    /// </summary>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <param name="cycleTime">完整循环周期</param>
    /// <param name="referenceTime">参考时间</param>
    /// <param name="waveType">曲线类型</param>
    /// <returns></returns>
    public static float PingPongWave(float min, float max, float cycleTime, double referenceTime, PingPongWaveType waveType = PingPongWaveType.Linear)
    {
        if (cycleTime <= 0f)
        {
            return max;
        }

        double timeInCycle = referenceTime % cycleTime;
        if (timeInCycle < 0)
        {
            timeInCycle += cycleTime;
        }

        float halfCycleTime = cycleTime * 0.5f;
        float waveProgress = (float)(timeInCycle / halfCycleTime);
        if (waveProgress > 1f)
        {
            waveProgress = 2f - waveProgress;
        }

        waveProgress = waveType switch
        {
            PingPongWaveType.SmoothStep => waveProgress * waveProgress * (3f - 2f * waveProgress),
            PingPongWaveType.Sin => MathF.Sin(waveProgress * MathHelper.PiOver2),
            _ => waveProgress
        };

        return min + (max - min) * waveProgress;
    }
    
    public static void EndBeginDraw(int state=0, int defferred = 0, bool adjustToScreen = true, SamplerState ss = null,Effect shader = null) 
    {
        BlendState blendState = BlendState.AlphaBlend;
        if(state==1)blendState = BlendState.Additive;
        if(state==2) blendState = BlendState.NonPremultiplied;
        if (state == 3) blendState = ReverseBS;
        if (state == 4) blendState = AlphaBlendNormal;
        Main.spriteBatch.End();
        if (ss == null) ss = Main.DefaultSamplerState;
        if (!adjustToScreen)
        {
            Main.spriteBatch.Begin((SpriteSortMode)defferred, blendState, ss,
                DepthStencilState.None, RasterizerState.CullNone, shader);
        }
        else
        {
            Main.spriteBatch.Begin((SpriteSortMode)defferred, blendState, ss,
                DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
        }
    }
    
    public static void EndBeginDraw(BlendState state, int defferred = 0, bool adjustToScreen = true, SamplerState ss = null,Effect shader = null) 
    {
        BlendState blendState = BlendState.AlphaBlend;
        
        Main.spriteBatch.End();
        if (ss == null) ss = Main.DefaultSamplerState;
        if (!adjustToScreen)
        {
            Main.spriteBatch.Begin((SpriteSortMode)defferred, blendState, ss,
                DepthStencilState.None, RasterizerState.CullNone, shader);
        }
        else
        {
            Main.spriteBatch.Begin((SpriteSortMode)defferred, blendState, ss,
                DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    public static void EndBeginDraw3D(int state = 0, SamplerState ss = null)
    {
        BlendState blendState = GetBlendState(state);
        ss ??= SamplerState.LinearWrap;

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(
            SpriteSortMode.Immediate,
            blendState,
            ss,
            DepthStencilState.Default,
            RasterizerState.CullNone,
            null,
            Main.Transform
        );
    }

    public static void BeginDraw3D(int state = 0, SamplerState ss = null)
    {
        BlendState blendState = GetBlendState(state);
        ss ??= SamplerState.LinearWrap;
        
        Main.spriteBatch.Begin(
            SpriteSortMode.Immediate,
            blendState,
            ss,
            DepthStencilState.Default,
            RasterizerState.CullClockwise,
            null,
            Main.Transform
        );
    }

    public static void EndBeginDrawUI(int state=0, int defferred = 0, bool adjustToScreen = true, SamplerState ss = null,Effect shader = null) 
    {
        BlendState blendState = BlendState.AlphaBlend;
        if(state==1)blendState = BlendState.Additive;
        if(state==2) blendState = BlendState.NonPremultiplied;
        if (state == 3) blendState = ReverseBS;
        if (state == 4) blendState = AlphaBlendNormal;
            
        Main.spriteBatch.End();
        if (ss == null) ss = Main.DefaultSamplerState;
        if (!adjustToScreen)
        {
            Main.spriteBatch.Begin((SpriteSortMode)defferred, blendState, ss,
                DepthStencilState.None, RasterizerState.CullNone, shader);
        }
        else
        {
            Main.spriteBatch.Begin((SpriteSortMode)defferred, blendState, ss,
                DepthStencilState.None, RasterizerState.CullNone, shader, Main.UIScaleMatrix);
        }
    }
    
    //是否开启了绘制，防炸
    public static bool IsBeginDrawCalled()
    {
        if (BeginCalledField == null)
        {
            return false;
        }

        return (bool)(BeginCalledField.GetValue(Main.spriteBatch) ?? false);
    }

    /// <summary>
    /// 用于刀光的进度曲线，比较平滑（看着像smoothstep）
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static float BezierEase(float time)
    {
        return time * time / ((2f * ((time * time) - time)) + 1f);
    }
    
    /// <summary>
    /// 用于刀光的进度曲线，比较平滑（看着像smoothstep）
    /// </summary>
    /// <param name="timeLeft">可以直接使用projectile.timeLeft</param>
    /// <param name="maxTime"></param>
    /// <returns></returns>
    public static float BezierEase(int timeLeft, float maxTime)
    {
        float time = (float)timeLeft / maxTime;
        return time * time / ((2f * ((time * time) - time)) + 1f);
    }


    #region 几何图形绘制方法

    /// <summary>
    /// 绘制菱形
    /// </summary>
    /// <param name="position"></param>
    /// <param name="size"></param>
    /// <param name="color"></param>
    /// <param name="rotation"></param>
    /// <param name="corner"></param>
    /// <param name="border"></param>
    /// <param name="borderColor"></param>
    /// <param name="filled"></param>
    /// <param name="alpha"></param>
    /// <param name="blendState"></param>
    /// <param name="drawInWolrd"></param>
    public static void DrawDiamond(Vector2 position, Vector2 size, Color? color= null,float rotation = 0,float corner = 0, 
        float border = 0,Color? borderColor = null,
        bool filled = true,float alpha = 1,int blendState = 0,bool drawInWolrd = false)
    {
        color ??= Color.White;
        borderColor ??= Color.White;

        // 把所有的点都生成出来，按照顺序
        List<CustomVertexInfo> bars = new List<CustomVertexInfo>();
        List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();
        
        bars.Add(new CustomVertexInfo(position + new Vector2(0,-size.Y/2).RotatedBy(rotation), color.Value,
            new Vector3(0, 1, alpha)));
        bars.Add(new CustomVertexInfo(position + new Vector2(-size.X/2,0).RotatedBy(rotation) , color.Value,
            new Vector3(0, 0, alpha))); 
        
        bars.Add(new CustomVertexInfo(position + new Vector2(size.X/2,0).RotatedBy(rotation), color.Value,
            new Vector3(1, 1, alpha)));
        bars.Add(new CustomVertexInfo(position + new Vector2(0,size.Y/2).RotatedBy(rotation) , color.Value,
            new Vector3(1, 0, alpha)));
        
        var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
        var model = Matrix.CreateTranslation(new Vector3(0, 0, 0));
        if (drawInWolrd)
        {
            model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) * Main.Transform;
        }

        Main.graphics.GraphicsDevice.Textures[0] = TextureAssets.MagicPixel.Value;
        basicShapeDraw.Parameters["uTransform"].SetValue(model * projection);
        basicShapeDraw.Parameters["cornerRadius"].SetValue(corner);
        basicShapeDraw.Parameters["width"].SetValue(MathF.Sqrt(size.X * size.X + size.Y * size.Y) );
        basicShapeDraw.Parameters["height"].SetValue(MathF.Sqrt(size.X * size.X + size.Y * size.Y) );
        basicShapeDraw.Parameters["filled"].SetValue(filled);

        basicShapeDraw.Parameters["borderWidth"].SetValue(border);
        basicShapeDraw.Parameters["borderColor"].SetValue(borderColor.Value.ToVector4());
        CollectAndDrawVertexInfo(bars, basicShapeDraw, blendState,drawInWolrd);
    }

    public static void DrawRectangle(Vector2 position, Vector2 size, Color? color, float rotation = 0, float corner = 0, float skew = 0,
        float border = 0,Color? borderColor = null,
        bool filled = true, float alpha = 1, int blendState = 0, bool drawInWolrd = false,
        Texture2D texture = null, Vector2? innerTextureScale = null, Vector2 innerTextureOffset = default)
    {
        color ??= Color.White;
        borderColor ??= Color.White;
        texture ??= TextureAssets.MagicPixel.Value;
        innerTextureScale ??= Vector2.One;
        // 把所有的点都生成出来，按照顺序
        List<CustomVertexInfo> bars = new List<CustomVertexInfo>();

        float skewValue = skew * (size.X);
        
        bars.Add(new CustomVertexInfo(position + new Vector2(-size.X/2+skewValue,-size.Y/2).RotatedBy(rotation), color.Value,
            new Vector3(0, 0, alpha)));
        bars.Add(new CustomVertexInfo(position + new Vector2(-size.X/2,size.Y/2).RotatedBy(rotation) , color.Value,
            new Vector3(0, 1, alpha))); 
        
        bars.Add(new CustomVertexInfo(position + new Vector2(size.X/2+skewValue,-size.Y/2).RotatedBy(rotation), color.Value,
            new Vector3(1, 0, alpha)));
        bars.Add(new CustomVertexInfo(position + new Vector2(size.X/2,size.Y/2).RotatedBy(rotation) , color.Value,
            new Vector3(1, 1, alpha)));
        
        var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
        var model = Matrix.CreateTranslation(new Vector3(0, 0, 0));
        if (drawInWolrd)
        {
            model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) * Main.Transform;
        }

        Main.graphics.GraphicsDevice.Textures[0] = texture;
        basicShapeDraw.Parameters["uTransform"].SetValue(model * projection);
        basicShapeDraw.Parameters["cornerRadius"].SetValue(corner);
        basicShapeDraw.Parameters["width"].SetValue(size.X);
        basicShapeDraw.Parameters["height"].SetValue(size.Y);
        basicShapeDraw.Parameters["filled"].SetValue(filled);
        
        basicShapeDraw.Parameters["innerTextureScale"].SetValue(innerTextureScale.Value);
        basicShapeDraw.Parameters["innerTextureOffset"].SetValue(innerTextureOffset);

        basicShapeDraw.Parameters["borderWidth"].SetValue(border);
        basicShapeDraw.Parameters["borderColor"].SetValue(borderColor.Value.ToVector4());
        CollectAndDrawVertexInfo(bars, basicShapeDraw, blendState,drawInWolrd);
        
    }
    
    /// <summary>
    /// 绘制胶囊体
    /// </summary>
    /// <param name="position"></param>
    /// <param name="size"></param>
    /// <param name="color"></param>
    /// <param name="rotation"></param>
    /// <param name="capsuleSharpness">头部圆润程度，0为圆1为尖</param>
    /// <param name="skew"></param>
    /// <param name="border"></param>
    /// <param name="borderColor"></param>
    /// <param name="filled"></param>
    /// <param name="alpha"></param>
    /// <param name="blendState"></param>
    /// <param name="drawInWolrd"></param>
    /// <param name="texture"></param>
        public static void DrawCapsuleRectangle(Vector2 position, Vector2 size, Color? color, float rotation = 0, float capsuleSharpness = 0,float skew = 0,
        float border = 0,Color? borderColor = null,
        bool filled = true, float alpha = 1, int blendState = 0, bool drawInWolrd = false,
        Texture2D texture = null,Vector2? innerTextureScale = null,Vector2 innerTextureOffset = default)
    {
        color ??= Color.White;
        borderColor ??= Color.White;
        texture ??= TextureAssets.MagicPixel.Value;
        innerTextureScale ??= new Vector2(1, 1);
        // 把所有的点都生成出来，按照顺序
        List<CustomVertexInfo> bars = new List<CustomVertexInfo>();

        float skewValue = skew * (size.X);
        
        bars.Add(new CustomVertexInfo(position + new Vector2(-size.X/2+skewValue,-size.Y/2).RotatedBy(rotation), color.Value,
            new Vector3(0, 0, alpha)));
        bars.Add(new CustomVertexInfo(position + new Vector2(-size.X/2,size.Y/2).RotatedBy(rotation) , color.Value,
            new Vector3(0, 1, alpha))); 
        
        bars.Add(new CustomVertexInfo(position + new Vector2(size.X/2+skewValue,-size.Y/2).RotatedBy(rotation), color.Value,
            new Vector3(1, 0, alpha)));
        bars.Add(new CustomVertexInfo(position + new Vector2(size.X/2,size.Y/2).RotatedBy(rotation) , color.Value,
            new Vector3(1, 1, alpha)));
        
        var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
        var model = Matrix.CreateTranslation(new Vector3(0, 0, 0));
        if (drawInWolrd)
        {
            model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) * Main.Transform;
        }

        Main.graphics.GraphicsDevice.Textures[0] = texture;
        basicShapeDraw.Parameters["uTransform"].SetValue(model * projection);
        basicShapeDraw.Parameters["width"].SetValue(size.X);
        basicShapeDraw.Parameters["height"].SetValue(size.Y);
        basicShapeDraw.Parameters["filled"].SetValue(filled);
        basicShapeDraw.Parameters["capsuleSharpness"].SetValue(capsuleSharpness);
        
        basicShapeDraw.Parameters["innerTextureScale"].SetValue(innerTextureScale.Value);
        basicShapeDraw.Parameters["innerTextureOffset"].SetValue(innerTextureOffset);

        basicShapeDraw.Parameters["borderWidth"].SetValue(border);
        basicShapeDraw.Parameters["borderColor"].SetValue(borderColor.Value.ToVector4());
        CollectAndDrawVertexInfo(bars, basicShapeDraw, blendState,drawInWolrd,1);
        
    }

    /// <summary>
    /// 绘制十字星
    /// </summary>
    /// <param name="position"></param>
    /// <param name="size"></param>
    /// <param name="color"></param>
    /// <param name="rotation"></param>
    /// <param name="crossStarCurve">曲率，0为直边星，1为曲边星</param>
    /// <param name="crossStarInnerScale">腰部比例，越小越尖</param>
    /// <param name="crossStarTipScale">上右下左四个方向的尖端长度缩放</param>
    /// <param name="skew"></param>
    /// <param name="border"></param>
    /// <param name="borderColor"></param>
    /// <param name="filled"></param>
    /// <param name="alpha"></param>
    /// <param name="blendState"></param>
    /// <param name="drawInWolrd"></param>
    /// <param name="texture"></param>
    public static void DrawCrossStar(Vector2 position, Vector2 size, Color? color, float rotation = 0, float crossStarCurve = 0,
        float crossStarInnerScale = 0.35f, Vector4? crossStarTipScale = null, float skew = 0, float border = 0, Color? borderColor = null,
        bool filled = true, float alpha = 1, int blendState = 0, bool drawInWolrd = false,
        Texture2D texture = null, Vector2? innerTextureScale = null, Vector2 innerTextureOffset = default)
    {
        color ??= Color.White;
        borderColor ??= Color.White;
        texture ??= TextureAssets.MagicPixel.Value;
        innerTextureScale ??= Vector2.One;
        crossStarTipScale ??= Vector4.One;
        List<CustomVertexInfo> bars = new List<CustomVertexInfo>();

        float skewValue = skew * size.X;

        bars.Add(new CustomVertexInfo(position + new Vector2(-size.X / 2 + skewValue, -size.Y / 2).RotatedBy(rotation), color.Value,
            new Vector3(0, 0, alpha)));
        bars.Add(new CustomVertexInfo(position + new Vector2(-size.X / 2, size.Y / 2).RotatedBy(rotation), color.Value,
            new Vector3(0, 1, alpha)));

        bars.Add(new CustomVertexInfo(position + new Vector2(size.X / 2 + skewValue, -size.Y / 2).RotatedBy(rotation), color.Value,
            new Vector3(1, 0, alpha)));
        bars.Add(new CustomVertexInfo(position + new Vector2(size.X / 2, size.Y / 2).RotatedBy(rotation), color.Value,
            new Vector3(1, 1, alpha)));

        var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
        var model = Matrix.CreateTranslation(new Vector3(0, 0, 0));
        if (drawInWolrd)
        {
            model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) * Main.Transform;
        }

        Main.graphics.GraphicsDevice.Textures[0] = texture;
        basicShapeDraw.Parameters["uTransform"].SetValue(model * projection);
        basicShapeDraw.Parameters["width"].SetValue(size.X);
        basicShapeDraw.Parameters["height"].SetValue(size.Y);
        basicShapeDraw.Parameters["filled"].SetValue(filled);
        basicShapeDraw.Parameters["crossStarCurve"].SetValue(crossStarCurve);
        basicShapeDraw.Parameters["crossStarInnerScale"].SetValue(crossStarInnerScale);
        basicShapeDraw.Parameters["crossStarTipScale"].SetValue(crossStarTipScale.Value);

        basicShapeDraw.Parameters["innerTextureScale"].SetValue(innerTextureScale.Value);
        basicShapeDraw.Parameters["innerTextureOffset"].SetValue(innerTextureOffset);

        basicShapeDraw.Parameters["borderWidth"].SetValue(border);
        basicShapeDraw.Parameters["borderColor"].SetValue(borderColor.Value.ToVector4());
        CollectAndDrawVertexInfo(bars, basicShapeDraw, blendState, drawInWolrd, 2);
    }
    #endregion

    private static void CollectAndDrawVertexInfo(List<CustomVertexInfo> bars,Effect effect, int blendState = 0, bool drawInWorld = false,int pass = 0)
    {
        List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();
        if (bars.Count > 2)
        {
            for (int i = 0; i < bars.Count - 2; i += 2)
            {
                triangleList.Add(bars[i]);
                triangleList.Add(bars[i + 2]);
                triangleList.Add(bars[i + 1]);

                triangleList.Add(bars[i + 1]);
                triangleList.Add(bars[i + 2]);
                triangleList.Add(bars[i + 3]);
            }

            BlendState bs = GetBlendState(blendState);
            RasterizerState originalState = Main.graphics.GraphicsDevice.RasterizerState;
            
            if (drawInWorld)
            {
                EndBeginDraw(blendState,1);
            }
            else
            {
                EndBeginDrawUI(blendState,1,false);
            }
            // 干掉注释掉就可以只显示三角形栅格
            /*RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            rasterizerState.FillMode = FillMode.WireFrame;
            Main.graphics.GraphicsDevice.RasterizerState = rasterizerState;*/
            
            //effect.Parameters["uTransform"].SetValue(model * projection);
            //effect.Parameters["cornerRadius"].SetValue(0.1f);

            //Main.graphics.GraphicsDevice.Textures[0] = TextureAssets.MagicPixel.Value;
            effect.CurrentTechnique.Passes[pass].Apply();
            
            Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0,
                triangleList.Count / 3);


            Main.graphics.GraphicsDevice.RasterizerState = originalState;
            if (drawInWorld)
            {
                EndBeginDraw();
            }
            else
            {
                EndBeginDrawUI();
            }
        }
    }
}