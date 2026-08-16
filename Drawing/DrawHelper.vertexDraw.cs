namespace KL.Drawing;

public partial class DrawHelper : ModSystem
{
    /// <summary>
    /// 贝塞尔曲线
    /// </summary>
    /// <param name="progress">起始点到终点的进度</param>
    /// <param name="startPosition"></param>
    /// <param name="endPosition"></param>
    /// <param name="pointA">控制点，用于控制曲线曲率</param>
    /// <param name="pointB">控制点，用于控制曲线曲率</param>
    /// <returns></returns>
    public static Vector2 GetBezierPoint(float progress, Vector2 startPosition, Vector2 endPosition,
        Vector2? pointA = null,
        Vector2? pointB = null)
    {
        Vector2 toward = endPosition - startPosition;
        toward = toward.SafeNormalize(toward) * (endPosition - startPosition).Length() / 3f;

        pointA ??= startPosition + toward;
        pointB ??= startPosition + toward * 2f;


        Vector2 point1 = Vector2.Lerp(startPosition, pointA.Value, progress);
        Vector2 point2 = Vector2.Lerp(pointA.Value, pointB.Value, progress);
        Vector2 point3 = Vector2.Lerp(pointB.Value, endPosition, progress);

        Vector2 Point1 = Vector2.Lerp(point1, point2, progress);
        Vector2 Point2 = Vector2.Lerp(point2, point3, progress);

        Vector2 finalPosition = Vector2.Lerp(Point1, Point2, progress);

        return finalPosition;
    }

    /// <summary>
    /// 二次贝塞尔曲线（一个控制点）
    /// </summary>
    /// <param name="progress">起始点到终点的进度</param>
    /// <param name="startPosition">起始点</param>
    /// <param name="endPosition">终点</param>
    /// <param name="controlPoint">控制点，用于控制曲线曲率</param>
    /// <returns>贝塞尔曲线上的点</returns>
    public static Vector2 GetQuadraticBezierPoint(float progress, Vector2 startPosition, Vector2 endPosition,
        Vector2? controlPoint = null)
    {
        // 如果没有提供控制点，使用默认位置（起点和终点的中点向上偏移）
        //controlPoint ??= (startPosition + endPosition) / 2f + new Vector2(0, (endPosition - startPosition).Length() * 0.3f);

        //
        controlPoint ??= (startPosition + endPosition) / 2f;

        float t = progress;
        float oneMinusT = 1f - t;

        Vector2 point = oneMinusT * oneMinusT * startPosition +
                        2f * oneMinusT * t * controlPoint.Value +
                        t * t * endPosition;

        return point;
    }


    public static void DrawBall(Texture2D mainTex, Vector2 center, float radius, Color color, float alpha = 1f,
        int drawTimes = 1, Vector2 scale = default, Vector2 uTime = default,
        Texture2D disTex = null, float threshold = 0, float edge = 0, Vector4 edgeColor = default)
    {
        if (scale == default) scale = Vector2.One;

        List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();
        triangleList.Add(new CustomVertexInfo((center - new Vector2(radius, radius)), color,
            new Vector3(-1, 1, alpha)));
        triangleList.Add(new CustomVertexInfo((center - new Vector2(radius, -radius)), color,
            new Vector3(-1, -1, alpha)));
        triangleList.Add(new CustomVertexInfo((center - new Vector2(-radius, -radius)), color,
            new Vector3(1, -1, alpha)));

        triangleList.Add(new CustomVertexInfo((center - new Vector2(radius, radius)), color,
            new Vector3(-1, 1, alpha)));
        triangleList.Add(new CustomVertexInfo((center - new Vector2(-radius, -radius)), color,
            new Vector3(1, -1, alpha)));
        triangleList.Add(new CustomVertexInfo((center - new Vector2(-radius, radius)), color,
            new Vector3(1, 1, alpha)));

        var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
        var model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) *
                    Main.Transform;

        spherePerspective.Parameters["uTransform"].SetValue(model * projection);
        spherePerspective.Parameters["circleCenter"].SetValue(new Vector3(0, 0, -2));
        spherePerspective.Parameters["radiusOfCircle"].SetValue(0.5f);
        spherePerspective.Parameters["uTime"].SetValue(uTime + new Vector2(0f, 0.5f));

        spherePerspective.Parameters["scale"].SetValue(scale);
        spherePerspective.Parameters["dissolveTime"].SetValue(threshold);
        spherePerspective.Parameters["dissolveEdge"].SetValue(edge);
        spherePerspective.Parameters["EdgeColor"].SetValue(edgeColor);

        if (disTex == null) disTex = PerLinNoiseX;
        Main.graphics.GraphicsDevice.Textures[0] = mainTex;
        Main.graphics.GraphicsDevice.Textures[1] = disTex;

        Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
        spherePerspective.CurrentTechnique.Passes[0].Apply();

        for (int i = 0; i < drawTimes; i++)
            Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0,
                triangleList.Count / 3);
    }


    /// <summary>
    /// 利用贝塞尔曲线生成锥形数组
    /// </summary>
    /// <param name="startPosition"></param>
    /// <param name="endPosition"></param>
    /// <param name="arrayLength"></param>
    /// <param name="width">尾部宽度</param>
    /// <param name="controlPointWidth">此宽度决定贝塞尔曲线的控制点的位置</param>
    /// <param name="controlPointPosPercent"></param>
    /// <returns></returns>
    public static Vector2[] QuickConePoints(Vector2 startPosition,Vector2 endPosition, int arrayLength =50, float width = 50f,float controlPointWidth = 50f, float controlPointPosPercent = 0.5f)
    {
        Vector2 toward = endPosition - startPosition;
        toward *= controlPointPosPercent;
        
        Vector2 normalDir = Vector2.Normalize(new Vector2(-toward.Y, toward.X));
        
        Vector2[] result = new Vector2[arrayLength];
        for (int i = 0; i < result.Length; i += 2)
        {
            result[i] = GetQuadraticBezierPoint((float)i / (result.Length-1), startPosition, endPosition + normalDir*width,startPosition + toward  +normalDir*controlPointWidth);
            result[i+1] = GetQuadraticBezierPoint((float)i / (result.Length-1), startPosition, endPosition- normalDir*width,startPosition + toward +normalDir*-controlPointWidth);

        }

        return result;
    }

    //以顶点绘制拖尾,alpha是用于bloom的绘制强度
    public static void TrailEffect(Texture2D mainTex, Vector2[] topPoints, Color startColor, Color endColor,
        float maxWidth = 5f, float endWidth = 5f, float startAlpha = 1f,
        float endAlpha = 0.1f, int drawTimes = 1, int blendState = 0, Vector2? uTime = null, Vector2? imageScale = null,
        Vector2 attachPoint = default,float attachRotation = 0f,
        //消融相关参数
        float threshold = 0.5f, float edge = 0, Vector4? edgeColor = null, Texture2D clipMask = null,
        Vector2? maskScale = null, Vector2 maskTime = default,
        //绘制调试相关
        bool useRforAlpha = false /*使用颜色R通道作为透明度，这对纯灰度图来说可以正确的使用non模式绘制*/, bool debugPoint = false)
    {
        if (mainTex == null)
        {
            Main.NewText("Get Null Tex in TrailEffect", Color.Red);
            return;
        }

        uTime ??= Vector2.Zero;
        imageScale ??= Vector2.One;
        maskScale ??= Vector2.One;
        edgeColor ??= Vector4.One;

        // 把所有的点都生成出来，按照顺序
        List<CustomVertexInfo> bars = new List<CustomVertexInfo>();
        List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();

        for (int i = 0; i < topPoints.Length; i++)
        {
            //if (坐标组[i] == Vector2.Zero) break;

            float width = MathHelper.Lerp(maxWidth, endWidth, (float)i / topPoints.Length);

            var normalDir = Vector2.Zero;
            //if (normalDir == Vector2.Zero) break;
            if (i < topPoints.Length - 1)
            {
                normalDir = topPoints[i] - topPoints[i + 1];
            }
            else
            {
                normalDir = topPoints[i - 1] - topPoints[i];
            }
            normalDir = normalDir.RotatedBy(attachRotation);

            {
                normalDir = Vector2.Normalize(new Vector2(-normalDir.Y, normalDir.X));
                
                var factor = i / (float)topPoints.Length; //(float)坐标组.Length;
                //var color = new Color(255, 123, 35, 255);//Color.Lerp(Color.White,Color.Red , factor);//Projectile.GetFairyQueenWeaponsColor(0f)//从头部到尾部渐变颜色
                var lerpAlpha = MathHelper.Lerp(startAlpha, endAlpha, factor); //从头部到尾部越来越透明
                if(lerpAlpha<0) lerpAlpha = 0;
                var color = Color.Lerp(startColor, endColor, factor);

                var trans = Main.GameViewMatrix != null ? Main.GameViewMatrix.TransformationMatrix : Matrix.Identity;
                if (debugPoint)
                {
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                        attachPoint + topPoints[i].RotatedBy(attachRotation) - Main.screenPosition,
                        new Rectangle(0, 0, 1, 1), Color.White, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None,
                        0f);

                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                        attachPoint + topPoints[i].RotatedBy(attachRotation) + normalDir * width * trans.M11 - Main.screenPosition,
                        new Rectangle(0, 0, 1, 1), Color.Red, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);

                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                        attachPoint + topPoints[i].RotatedBy(attachRotation) + normalDir * -width * trans.M11 - Main.screenPosition,
                        new Rectangle(0, 0, 1, 1), Color.Blue, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);
                }

                bars.Add(new CustomVertexInfo(attachPoint + topPoints[i].RotatedBy(attachRotation) + normalDir* width * trans.M11, color,
                    new Vector3(factor, 1, lerpAlpha))); //最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
                bars.Add(new CustomVertexInfo(attachPoint + topPoints[i].RotatedBy(attachRotation) + normalDir * -width * trans.M11, color,
                    new Vector3(factor, 0, lerpAlpha))); //(float)Math.Sqrt(factor)
            }
        }

        if (bars.Count > 2)
        {
            // 按照顺序连接三角形
            triangleList.Add(bars[0]);
            //尖端位置，暂时取中点。
            var vertex = new CustomVertexInfo((bars[0].Position + bars[1].Position) * 0.5f, Color.White,
                new Vector3(0, 0.5f, startAlpha));
            triangleList.Add(bars[1]);
            triangleList.Add(vertex);
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
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, bs, SamplerState.LinearWrap, DepthStencilState.Default,
                RasterizerState.CullNone, null,Main.Transform);
            RasterizerState originalState = Main.graphics.GraphicsDevice.RasterizerState;

            // 干掉注释掉就可以只显示三角形栅格
            /*RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            rasterizerState.FillMode = FillMode.WireFrame;
            Main.graphics.GraphicsDevice.RasterizerState = rasterizerState;*/

            var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
            var model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) *
                        Main.Transform;


            vertexDraw.Parameters["uTransform"].SetValue(model * projection);
            vertexDraw.Parameters["ImageScale"].SetValue(imageScale.Value);

            if (uTime != Vector2.Zero)
            {
                vertexDraw.Parameters["useTime"].SetValue(true);
                vertexDraw.Parameters["uTimex"].SetValue(uTime.Value.X);
                vertexDraw.Parameters["uTimey"].SetValue(uTime.Value.Y);
            }
            else
            {
                vertexDraw.Parameters["useTime"].SetValue(false);
            }

            // 添加新的参数设置，条件是clipMask不为null时
            if (clipMask != null)
            {
                vertexDraw.Parameters["threshold"].SetValue(threshold);
                vertexDraw.Parameters["edge"].SetValue(edge);
                vertexDraw.Parameters["edgeColor"].SetValue(edgeColor.Value);
                vertexDraw.Parameters["maskScale"].SetValue(maskScale.Value);
                vertexDraw.Parameters["maskTime"].SetValue(maskTime);
                Main.graphics.GraphicsDevice.Textures[1] = clipMask;
            }

            vertexDraw.Parameters["shouldClip"].SetValue(clipMask != null);
            vertexDraw.Parameters["useRforAlpha"].SetValue(useRforAlpha);

            Main.graphics.GraphicsDevice.Textures[0] = mainTex;
            vertexDraw.CurrentTechnique.Passes[0].Apply();

            for (int i = 0; i < drawTimes; i++)
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0,
                    triangleList.Count / 3);


            Main.graphics.GraphicsDevice.RasterizerState = originalState;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null,
                Main.GameViewMatrix.TransformationMatrix);
        }
    }

    //顶点绘制，通过上下两组顶点确定贴图绘制位置从而绘制条带。
    public static void VertexDrawEffect(Texture2D mainTex, Vector2[] topPoints,Vector2[] bottomPoints,  Color startColor, Color endColor, 
        float startAlpha = 1f, float endAlpha = 0.1f, int drawTimes = 1, int blendState = 0, Vector2? uTime = null, Vector2? imageScale = null,
        //可以附着于某个位置。如果附着对象还涉及旋转时，这个参数很有用
        Vector2 attachPoint = default,float attachRotation = 0f,
        //消融相关参数
        float threshold = 0.5f, float edge = 0, Vector4? edgeColor = null, Texture2D clipMask = null,
        Vector2? maskScale = null, Vector2 maskTime = default,
        //绘制调试相关
        bool useRforAlpha = false /*使用颜色R通道作为透明度，这对纯灰度图来说可以正确的使用non模式绘制*/, bool debugPoint = false)
    {
        if (mainTex == null)
        {
            Main.NewText("Get Null Tex in TrailEffect", Color.Red);
            return;
        }

        uTime ??= Vector2.Zero;
        imageScale ??= Vector2.One;
        maskScale ??= Vector2.One;
        edgeColor ??= Vector4.One;

        // 把所有的点都生成出来，按照顺序
        List<CustomVertexInfo> bars = new List<CustomVertexInfo>();
        List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();

        for (int i = 0; i < topPoints.Length; i++)
        {
            var factor = i / (float)topPoints.Length; //(float)坐标组.Length;
            //var color = new Color(255, 123, 35, 255);//Color.Lerp(Color.White,Color.Red , factor);//Projectile.GetFairyQueenWeaponsColor(0f)//从头部到尾部渐变颜色
            var lerpAlpha = MathHelper.Lerp(startAlpha, endAlpha, factor); //从头部到尾部越来越透明
            var color = Color.Lerp(startColor, endColor, factor);

            var trans = Main.GameViewMatrix != null ? Main.GameViewMatrix.TransformationMatrix : Matrix.Identity;
            if (debugPoint)
            {
                    
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    attachPoint + bottomPoints[i].RotatedBy(attachRotation) - Main.screenPosition,
                    new Rectangle(0, 0, 1, 1), Color.Red, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);

                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    attachPoint + topPoints[i].RotatedBy(attachRotation) - Main.screenPosition,
                    new Rectangle(0, 0, 1, 1), Color.Blue, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);
            }

            bars.Add(new CustomVertexInfo(attachPoint + topPoints[i].RotatedBy(attachRotation), color,
                new Vector3(/*贴图uv，factor取值从0到1意味着uv的x坐标为0-1*/factor, /*贴图uv，固定为1意味着是上端顶点*/1, lerpAlpha))); //最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
            bars.Add(new CustomVertexInfo(attachPoint + bottomPoints[i].RotatedBy(attachRotation) , color,
                new Vector3(/*贴图uv，factor取值从0到1意味着uv的x坐标为0-1*/factor, /*贴图uv，固定为0意味着是下端顶点*/0, lerpAlpha))); //(float)Math.Sqrt(factor)
        }

        if (bars.Count > 2)
        {
            // 按照顺序连接三角形
            triangleList.Add(bars[0]);
            //尖端位置，暂时取中点。
            var vertex = new CustomVertexInfo((bars[0].Position + bars[1].Position) * 0.5f, Color.White,
                new Vector3(0, 0.5f, startAlpha));
            triangleList.Add(bars[1]);
            triangleList.Add(vertex);
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
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, bs, SamplerState.LinearWrap, DepthStencilState.Default,
                RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            RasterizerState originalState = Main.graphics.GraphicsDevice.RasterizerState;

            // 干掉注释掉就可以只显示三角形栅格
            /*RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            rasterizerState.FillMode = FillMode.WireFrame;
            Main.graphics.GraphicsDevice.RasterizerState = rasterizerState;*/

            var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
            var model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) *
                        Main.Transform;


            vertexDraw.Parameters["uTransform"].SetValue(model * projection);
            vertexDraw.Parameters["ImageScale"].SetValue(imageScale.Value);

            if (uTime != Vector2.Zero)
            {
                vertexDraw.Parameters["useTime"].SetValue(true);
                vertexDraw.Parameters["uTimex"].SetValue(uTime.Value.X);
                vertexDraw.Parameters["uTimey"].SetValue(uTime.Value.Y);
            }
            else
            {
                vertexDraw.Parameters["useTime"].SetValue(false);
            }

            // 添加新的参数设置，条件是clipMask不为null时
            if (clipMask != null)
            {
                vertexDraw.Parameters["threshold"].SetValue(threshold);
                vertexDraw.Parameters["edge"].SetValue(edge);
                vertexDraw.Parameters["edgeColor"].SetValue(edgeColor.Value);
                vertexDraw.Parameters["maskScale"].SetValue(maskScale.Value);
                vertexDraw.Parameters["maskTime"].SetValue(maskTime);
                Main.graphics.GraphicsDevice.Textures[1] = clipMask;
            }

            vertexDraw.Parameters["shouldClip"].SetValue(clipMask != null);
            vertexDraw.Parameters["useRforAlpha"].SetValue(useRforAlpha);

            Main.graphics.GraphicsDevice.Textures[0] = mainTex;
            vertexDraw.CurrentTechnique.Passes[0].Apply();

            for (int i = 0; i < drawTimes; i++)
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0,
                    triangleList.Count / 3);


            Main.graphics.GraphicsDevice.RasterizerState = originalState;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null,
                Main.GameViewMatrix.TransformationMatrix);
        }
    }
    
    //需要手动计算出所有顶点的位置。只包含一组顶点，每间隔一个对于顶点的上下位置。
    public static void VertexDrawEffect(Texture2D mainTex, Vector2[] topPoints, Color startColor, Color endColor,
        float startAlpha = 1f, float endAlpha = 0.1f, int drawTimes = 1, int blendState = 0, Vector2? uTime = null, Vector2? imageScale = null,
        //可以附着于某个位置。如果附着对象还涉及旋转时，这个参数很有用
        Vector2 attachPoint = default,float attachRotation = 0f,
        //消融相关参数
        float threshold = 0.5f, float edge = 0, Vector4? edgeColor = null, Texture2D clipMask = null,
        Vector2? maskScale = null, Vector2 maskTime = default,
        //绘制调试相关
        bool useRforAlpha = false /*使用颜色R通道作为透明度，这对纯灰度图来说可以正确的使用non模式绘制*/, bool debugPoint = false)
    {
        if (mainTex == null)
        {
            Main.NewText("Get Null Tex in TrailEffect", Color.Red);
            return;
        }

        uTime ??= Vector2.Zero;
        imageScale ??= Vector2.One;
        maskScale ??= Vector2.One;
        edgeColor ??= Vector4.One;

        // 把所有的点都生成出来，按照顺序
        List<CustomVertexInfo> bars = new List<CustomVertexInfo>();
        List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();

        for (int i = 0; i < topPoints.Length; i+=2)
        {
            var factor = i / (float)topPoints.Length; //(float)坐标组.Length;
            //var color = new Color(255, 123, 35, 255);//Color.Lerp(Color.White,Color.Red , factor);//Projectile.GetFairyQueenWeaponsColor(0f)//从头部到尾部渐变颜色
            var lerpAlpha = MathHelper.Lerp(startAlpha, endAlpha, factor); //从头部到尾部越来越透明
            var color = Color.Lerp(startColor, endColor, factor);

            var trans = Main.GameViewMatrix != null ? Main.GameViewMatrix.TransformationMatrix : Matrix.Identity;
            if (debugPoint)
            {
                    
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    attachPoint + topPoints[i].RotatedBy(attachRotation) - Main.screenPosition,
                    new Rectangle(0, 0, 1, 1), Color.Red, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);

                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    attachPoint + topPoints[i+1].RotatedBy(attachRotation) - Main.screenPosition,
                    new Rectangle(0, 0, 1, 1), Color.Blue, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);
            }

            bars.Add(new CustomVertexInfo(attachPoint + topPoints[i].RotatedBy(attachRotation), color,
                new Vector3(1-factor, 1, lerpAlpha))); //最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
            bars.Add(new CustomVertexInfo(attachPoint +topPoints[i+1].RotatedBy(attachRotation) , color,
                new Vector3(1-factor, 0, lerpAlpha))); //(float)Math.Sqrt(factor)
        }

        if (bars.Count > 2)
        {
            // 按照顺序连接三角形
            triangleList.Add(bars[0]);
            //尖端位置，暂时取中点。
            var vertex = new CustomVertexInfo((bars[0].Position + bars[1].Position) * 0.5f, Color.White,
                new Vector3(0, 0.5f, startAlpha));
            triangleList.Add(bars[1]);
            triangleList.Add(vertex);
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
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, bs, SamplerState.LinearWrap, DepthStencilState.Default,
                RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            RasterizerState originalState = Main.graphics.GraphicsDevice.RasterizerState;

            // 干掉注释掉就可以只显示三角形栅格
            /*RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            rasterizerState.FillMode = FillMode.WireFrame;
            Main.graphics.GraphicsDevice.RasterizerState = rasterizerState;*/

            var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
            var model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) *
                        Main.Transform;


            vertexDraw.Parameters["uTransform"].SetValue(model * projection);
            vertexDraw.Parameters["ImageScale"].SetValue(imageScale.Value);

            if (uTime != Vector2.Zero)
            {
                vertexDraw.Parameters["useTime"].SetValue(true);
                vertexDraw.Parameters["uTimex"].SetValue(uTime.Value.X);
                vertexDraw.Parameters["uTimey"].SetValue(uTime.Value.Y);
            }
            else
            {
                vertexDraw.Parameters["useTime"].SetValue(false);
            }

            // 添加新的参数设置，条件是clipMask不为null时
            if (clipMask != null)
            {
                vertexDraw.Parameters["threshold"].SetValue(threshold);
                vertexDraw.Parameters["edge"].SetValue(edge);
                vertexDraw.Parameters["edgeColor"].SetValue(edgeColor.Value);
                vertexDraw.Parameters["maskScale"].SetValue(maskScale.Value);
                vertexDraw.Parameters["maskTime"].SetValue(maskTime);
                Main.graphics.GraphicsDevice.Textures[1] = clipMask;
            }

            vertexDraw.Parameters["shouldClip"].SetValue(clipMask != null);
            vertexDraw.Parameters["useRforAlpha"].SetValue(useRforAlpha);

            Main.graphics.GraphicsDevice.Textures[0] = mainTex;
            vertexDraw.CurrentTechnique.Passes[0].Apply();

            for (int i = 0; i < drawTimes; i++)
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0,
                    triangleList.Count / 3);


            Main.graphics.GraphicsDevice.RasterizerState = originalState;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null,
                Main.GameViewMatrix.TransformationMatrix);
        }
    }

    
    
    //类似于普通拖尾，但是传递数组为刀光的顶部以及底部
    public static void SlashEffect(Texture2D mainTex, Vector2[] topPoints, Vector2[] bottomPoints, Color color,
        float endAlpha = 0.1f, int drawTimes = 1, int blendState = 0, Vector2? uTime = null, Vector2? imageScale = null,
        bool debugPoint = false)
    {
        if (mainTex == null)
        {
            Main.NewText("Get Null Tex in TrailEffect", Color.Red);
            return;
        }

        uTime ??= Vector2.Zero;
        imageScale ??= Vector2.One;

        float startAlpha = 1f;

        // 把所有的点都生成出来，按照顺序
        List<CustomVertexInfo> bars = new List<CustomVertexInfo>();
        List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();


        for (int i = 0; i < topPoints.Length; i++)
        {
            var factor = i / (float)topPoints.Length; //(float)坐标组.Length;
            //var color = new Color(255, 123, 35, 255);//Color.Lerp(Color.White,Color.Red , factor);//Projectile.GetFairyQueenWeaponsColor(0f)//从头部到尾部渐变颜色
            var lerpAlpha = MathHelper.Lerp(startAlpha, endAlpha, factor); //从头部到尾部越来越透明
            var trans = Main.GameViewMatrix != null ? Main.GameViewMatrix.TransformationMatrix : Matrix.Identity;
            if (debugPoint)
            {
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, topPoints[i] - Main.screenPosition,
                    new Rectangle(0, 0, 1, 1), Color.Red, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);

                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, bottomPoints[i] - Main.screenPosition,
                    new Rectangle(0, 0, 1, 1), Color.Blue, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);
            }

            bars.Add(new CustomVertexInfo(topPoints[i], color * lerpAlpha,
                new Vector3(factor, 1, lerpAlpha))); //最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
            bars.Add(new CustomVertexInfo(bottomPoints[i], color * lerpAlpha,
                new Vector3(factor, 0, lerpAlpha))); //(float)Math.Sqrt(factor)
        }

        if (bars.Count > 2)
        {
            // 按照顺序连接三角形
            triangleList.Add(bars[0]);
            //尖端位置，暂时取中点。
            var vertex = new CustomVertexInfo((bars[0].Position + bars[1].Position) * 0.5f, Color.White,
                new Vector3(0, 0.5f, startAlpha));
            triangleList.Add(bars[1]);
            triangleList.Add(vertex);
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
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, bs, SamplerState.PointWrap, DepthStencilState.Default,
                RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            RasterizerState originalState = Main.graphics.GraphicsDevice.RasterizerState;

            // 干掉注释掉就可以只显示三角形栅格
            /*RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            rasterizerState.FillMode = FillMode.WireFrame;
            Main.graphics.GraphicsDevice.RasterizerState = rasterizerState;*/

            var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
            var model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) *
                        Main.Transform;


            vertexDraw.Parameters["uTransform"].SetValue(model * projection);
            vertexDraw.Parameters["ImageScale"].SetValue(imageScale.Value);

            if (uTime != Vector2.Zero)
            {
                vertexDraw.Parameters["useTime"].SetValue(true);
                vertexDraw.Parameters["uTimex"].SetValue(uTime.Value.X);
                vertexDraw.Parameters["uTimey"].SetValue(uTime.Value.Y);
            }
            else
            {
                vertexDraw.Parameters["useTime"].SetValue(false);
            }

            Main.graphics.GraphicsDevice.Textures[0] = mainTex;
            vertexDraw.CurrentTechnique.Passes[0].Apply();

            for (int i = 0; i < drawTimes; i++)
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0,
                    triangleList.Count / 3);


            Main.graphics.GraphicsDevice.RasterizerState = originalState;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null,
                Main.GameViewMatrix.TransformationMatrix);
        }
    }
    
    public static List<Vector2> GetCircleRingVertices(float startAngle, float majorAxis, float minorAxis, int numPoints = 50)
    {
        List<Vector2> result = new List<Vector2>(numPoints);
        // 生成椭圆顶点
        for (int i = 0; i < numPoints; i++)
        {
            // 计算角度（0到2π）
            float angle = startAngle + (float)(i) / numPoints * MathHelper.TwoPi;
        
            // 计算未旋转的椭圆点（基于长轴和短轴）
            float x = majorAxis * (float)Math.Cos(angle);
            float y = minorAxis * (float)Math.Sin(angle);
            Vector2 point = new Vector2(x, y);
            
            result.Add(point);
        }
        return result;
    }
    public static void CircleRingVertexEffect(Texture2D mainTex, Vector2[] torwardsVector,float circleWidth,  Color frontColor, Color backColor,
        //圆环边缘宽度倍率
        float sideWidthScale = 0f,
        float startAlpha = 1f, float endAlpha = 0.1f, int drawTimes = 1, int blendState = 0, Vector2? uTime = null,
        Vector2? imageScale = null,
        //圆圈的中心点，以及圆圈的旋转度
        Vector2 attachPoint = default, float attachRotation = 0f,
        //消融相关参数
        float threshold = 0.5f, float edge = 0, Vector4? edgeColor = null, Texture2D clipMask = null,
        Vector2? maskScale = null, Vector2 maskTime = default,
        //绘制调试相关
        bool useRforAlpha = false /*使用颜色R通道作为透明度，这对纯灰度图来说可以正确的使用non模式绘制*/, bool debugPoint = false)
    {
        if (mainTex == null)
        {
            Main.NewText("Get Null Tex in TrailEffect", Color.Red);
            return;
        }

        uTime ??= Vector2.Zero;
        imageScale ??= Vector2.One;
        maskScale ??= Vector2.One;
        edgeColor ??= Vector4.One;

        // 把所有的点都生成出来，按照顺序
        List<CustomVertexInfo> bars = new List<CustomVertexInfo>();
        List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();

        for (int i = 0; i < torwardsVector.Length; i++)
        {
            var factor = i / (float)torwardsVector.Length; //(float)坐标组.Length;
            //var color = new Color(255, 123, 35, 255);//Color.Lerp(Color.White,Color.Red , factor);//Projectile.GetFairyQueenWeaponsColor(0f)//从头部到尾部渐变颜色
            var lerpAlpha = MathHelper.Lerp(startAlpha, endAlpha, factor); //从头部到尾部越来越透明
            var color = Color.Lerp(frontColor, backColor, factor);

            float rotEffect = (attachRotation / MathHelper.Pi) % 1;
            //rotEffect = 0.998627f;
            //Main.NewText(attachRotation+" " +rotEffect);

            if (torwardsVector[i].Y < 0)
            {
                if (rotEffect < 0.5f)
                {
                    //Main.NewText(1);
                    color = backColor;
                }
                else
                {
                    //Main.NewText(2);
                    color = frontColor;
                }
            }
            else
            {
                if (rotEffect< 0.5f)
                {
                    //Main.NewText(3);
                    color = frontColor;

                }
                else
                {
                    //Main.NewText(4);
                    color = backColor;
                }
            }
            
            var trans = Main.GameViewMatrix != null ? Main.GameViewMatrix.TransformationMatrix : Matrix.Identity;

            Vector2 move = new Vector2(0, 1).RotatedBy(attachRotation) * circleWidth;
            
            float halfLength = torwardsVector.Length * 0.5f;

            // 前半段：从0到1线性增长
            if (i <= halfLength)
            {
                move *= MathHelper.SmoothStep(sideWidthScale, 1f, i / halfLength);
            }
            else
            {
                move *= MathHelper.SmoothStep(1f, sideWidthScale, (i - halfLength) / halfLength);
            }
            
            if (debugPoint)
            {
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    attachPoint + torwardsVector[i].RotatedBy(attachRotation) - Main.screenPosition,
                    new Rectangle(0, 0, 1, 1), Color.White, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);
                    
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    attachPoint + torwardsVector[i].RotatedBy(attachRotation)+move - Main.screenPosition,
                    new Rectangle(0, 0, 1, 1), Color.Red, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);

                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    attachPoint + torwardsVector[i].RotatedBy(attachRotation)-move - Main.screenPosition,
                    new Rectangle(0, 0, 1, 1), Color.Blue, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);
            }

            bars.Add(new CustomVertexInfo(attachPoint + torwardsVector[i].RotatedBy(attachRotation)+move, color,
                new Vector3(/*贴图uv，factor取值从0到1意味着uv的x坐标为0-1*/factor, /*贴图uv，固定为1意味着是上端顶点*/1, lerpAlpha))); //最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
            bars.Add(new CustomVertexInfo(attachPoint + torwardsVector[i].RotatedBy(attachRotation)-move , color,
                new Vector3(/*贴图uv，factor取值从0到1意味着uv的x坐标为0-1*/factor, /*贴图uv，固定为0意味着是下端顶点*/0, lerpAlpha))); //(float)Math.Sqrt(factor)
        }

        if (bars.Count > 2)
        {
            // 按照顺序连接三角形
            triangleList.Add(bars[0]);
            //尖端位置，暂时取中点。
            var vertex = new CustomVertexInfo((bars[0].Position + bars[1].Position) * 0.5f, Color.White,
                new Vector3(0, 0.5f, startAlpha));
            triangleList.Add(bars[1]);
            triangleList.Add(vertex);
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
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, bs, SamplerState.LinearWrap, DepthStencilState.Default,
                RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            RasterizerState originalState = Main.graphics.GraphicsDevice.RasterizerState;

            // 干掉注释掉就可以只显示三角形栅格
            /*RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            rasterizerState.FillMode = FillMode.WireFrame;
            Main.graphics.GraphicsDevice.RasterizerState = rasterizerState;*/

            var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
            var model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) *
                        Main.Transform;


            vertexDraw.Parameters["uTransform"].SetValue(model * projection);
            vertexDraw.Parameters["ImageScale"].SetValue(imageScale.Value);

            if (uTime != Vector2.Zero)
            {
                vertexDraw.Parameters["useTime"].SetValue(true);
                vertexDraw.Parameters["uTimex"].SetValue(uTime.Value.X);
                vertexDraw.Parameters["uTimey"].SetValue(uTime.Value.Y);
            }
            else
            {
                vertexDraw.Parameters["useTime"].SetValue(false);
            }

            // 添加新的参数设置，条件是clipMask不为null时
            if (clipMask != null)
            {
                vertexDraw.Parameters["threshold"].SetValue(threshold);
                vertexDraw.Parameters["edge"].SetValue(edge);
                vertexDraw.Parameters["edgeColor"].SetValue(edgeColor.Value);
                vertexDraw.Parameters["maskScale"].SetValue(maskScale.Value);
                vertexDraw.Parameters["maskTime"].SetValue(maskTime);
                Main.graphics.GraphicsDevice.Textures[1] = clipMask;
            }

            vertexDraw.Parameters["shouldClip"].SetValue(clipMask != null);
            vertexDraw.Parameters["useRforAlpha"].SetValue(useRforAlpha);

            Main.graphics.GraphicsDevice.Textures[0] = mainTex;
            vertexDraw.CurrentTechnique.Passes[0].Apply();

            for (int i = 0; i < drawTimes; i++)
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0,
                    triangleList.Count / 3);


            Main.graphics.GraphicsDevice.RasterizerState = originalState;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null,
                Main.GameViewMatrix.TransformationMatrix);
        }
    }

        
    // Perlin噪声算法实现
    public static float GenPerlinNoise(float x, float y, float scale = 1f)
    {
        // 简单的Perlin噪声实现
        int xi = (int)MathF.Floor(x);
        int yi = (int)MathF.Floor(y);
        
        float xf = x - xi;
        float yf = y - yi;
        
        // 平滑插值函数
        float Smooth(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }
        
        // 伪随机梯度函数
        float DotGridGradient(int ix, int iy, float x, float y)
        {
            // 使用简单的哈希函数生成伪随机梯度
            float random = 2920f * MathF.Sin(ix * 21942f + iy * 171324f + 8912f) * MathF.Cos(ix * 23157f * iy * 217832f + 97531f);
            float angle = random % 6.283185f; // 2π
            
            Vector2 gradient = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 distance = new Vector2(x - ix, y - iy);
            
            return Vector2.Dot(gradient, distance);
        }
        
        // 四个角的梯度值
        float n00 = DotGridGradient(xi, yi, x, y);
        float n10 = DotGridGradient(xi + 1, yi, x, y);
        float n01 = DotGridGradient(xi, yi + 1, x, y);
        float n11 = DotGridGradient(xi + 1, yi + 1, x, y);
        
        // X方向插值
        float ix0 = MathHelper.Lerp(n00, n10, Smooth(xf));
        float ix1 = MathHelper.Lerp(n01, n11, Smooth(xf));
        
        // Y方向插值
        return MathHelper.Lerp(ix0, ix1, Smooth(yf)) * scale;
    }
    
    /// <summary>
    /// 改进的多层Perlin噪声（分形噪声）
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="octaves">每增加一层，就添加更高频率的细节。层数越多，噪声越复杂、细节越丰富。建议范围：2-6层</param>
    /// <param name="persistence">值接近1.0：各层强度相似，噪声更粗糙。值接近0.0：高频层衰减快，噪声更光滑</param>
    /// <param name="scale">控制所有层噪声的整体强度</param>
    /// <returns>噪声结果</returns>
    public static float FractalNoise(float x, float y, int octaves = 4, float persistence = 0.5f, float scale = 1f)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f;
        
        for (int i = 0; i < octaves; i++)
        {
            total += GenPerlinNoise(x * frequency, y * frequency, scale) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2f;
        }
        
        return total / maxValue;
    }
}