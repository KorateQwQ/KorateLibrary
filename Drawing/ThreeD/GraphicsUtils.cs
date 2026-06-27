using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace KL.Drawing.ThreeD
{
    public enum ProjectionMode
    {
        Perspective,
        Orthographic
    }

    public static class GraphicsUtils
    {
        //坐标系：设定z增加方向指向屏幕内

        public static Vector2 ScreenResolution => new Vector2(Main.screenWidth, Main.screenHeight);

        public static Vector2 ScreenCenter => Main.screenPosition + ScreenResolution / 2;

        /// <summary>
        /// 确定摄像机位置
        /// </summary>
        /// /// <param name="fov">视场角</param>
        /// <returns></returns>
        public static Vector3 CameraPos(float fov) => new Vector3(ScreenCenter, CameraZ(fov));

        /// <summary>
        /// 获取相机z坐标
        /// </summary>
        /// <param name="fov">视场角</param>
        /// <returns></returns>
        public static float CameraZ(float fov)
        {
            float viewWidth = Main.screenWidth / Main.Transform.M11;
            float factor = (float)Main.screenHeight / Main.screenWidth;
            return factor * -viewWidth / 2 / MathF.Tan(fov / 2);
        }

        public static Vector2 ProjectWorldToScreen(Vector3 worldPosition, ProjectionMode projectionMode = ProjectionMode.Perspective, float fov = MathF.PI / 3f)
        {
            return projectionMode == ProjectionMode.Orthographic
                ? new Vector2(worldPosition.X, worldPosition.Y)
                : ProjectWorldToScreenPerspective(worldPosition, fov);
        }

        private static Vector2 ProjectWorldToScreenPerspective(Vector3 worldPosition, float fov)
        {
            Vector3 cameraPosition = CameraPos(fov);
            Vector3 relative = cameraPosition - worldPosition;
            if (MathF.Abs(relative.Z) <= 0.0001f)
            {
                return new Vector2(worldPosition.X, worldPosition.Y);
            }

            float perspective = cameraPosition.Z / relative.Z;
            Vector2 projectedOffset = new Vector2(relative.X, relative.Y) * perspective;
            return Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f - projectedOffset;
        }

        public static Vector3 GetViewDirection(ProjectionMode projectionMode, Vector3 worldPosition, float fov = MathF.PI / 3f)
        {
            Vector3 viewDirection = projectionMode == ProjectionMode.Orthographic
                ? Vector3.UnitZ
                : CameraPos(fov) - worldPosition;

            if (viewDirection.LengthSquared() <= 0.0001f)
            {
                return Vector3.UnitZ;
            }

            viewDirection.Normalize();
            return viewDirection;
        }

        public static Matrix GetVPMatrix(ProjectionMode projectionMode = ProjectionMode.Perspective, float fov = MathF.PI / 3f, float near = 10f, float far = 5000f)
        {
            Vector3 cameraPos = CameraPos(fov);
            Matrix view = Matrix.CreateLookAt(cameraPos, cameraPos + new Vector3(0, 0, 1), Vector3.Down);
            Matrix projection = projectionMode == ProjectionMode.Orthographic
                ? CreateOrthographicProjection(near, far)
                : Matrix.CreatePerspectiveFieldOfView(fov, Main.graphics.GraphicsDevice.Viewport.AspectRatio, near, far);

            //若考虑重力翻转
            Matrix grav = Matrix.Identity;
            if (Main.LocalPlayer.gravDir == -1)
                grav = Matrix.CreateScale(1, -1, 1);
            return view * projection * grav;
        }

        private static Matrix CreateOrthographicProjection(float near, float far)
        {
            float viewWidth = Main.screenWidth / Main.Transform.M11;
            float viewHeight = Main.screenHeight / Main.Transform.M22;
            return Matrix.CreateOrthographic(viewWidth, viewHeight, near, far);
        }

        public struct VertexInfo3 : IVertexType
        {
            private static VertexDeclaration _vertexDeclaration = new VertexDeclaration(new VertexElement[3]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color,0),
                new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate,0)
            });

            public Vector3 Position;
            public Color Color;
            public Vector3 TexCoord;

            public VertexInfo3(Vector3 position, Vector3 texCoord, Color color)
            {
                Position = position;
                TexCoord = texCoord;
                Color = color;
            }

            public VertexDeclaration VertexDeclaration
            {
                get => _vertexDeclaration;
            }
        }
    }
}
