using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace KL.Drawing.ThreeD
{
    /// <summary>
    /// 3D顶点数据结构
    /// </summary>
    /*public struct Vertex3D : IVertexType
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

        public Vertex3D(Vector3 position, Vector3 texCoord, Color color)
        {
            Position = position;
            TexCoord = texCoord;
            Color = color;
        }

        public VertexDeclaration VertexDeclaration
        {
            get => _vertexDeclaration;
        }
    }*/

    /// <summary>
    /// 带法线的3D顶点数据结构
    /// </summary>
    public struct Vertex3D : IVertexType
    {
        private static VertexDeclaration _vertexDeclaration = new VertexDeclaration(new VertexElement[4]
        {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color,0),
                new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate,0),
                new VertexElement(24, VertexElementFormat.Vector3, VertexElementUsage.Normal,0)
        });

        public Vector3 Position;
        public Color Color;
        public Vector2 TexCoord;
        public Vector3 Normal;

        public Vertex3D(Vector3 position, Vector2 texCoord, Color color, Vector3 normal)
        {
            Position = position;
            TexCoord = texCoord;
            Color = color;
            Normal = normal;
        }

        public Vertex3D(Vector3 position)
        {
            Position = position;
            TexCoord = Vector2.Zero;
            Color = Color.White;
            Normal = Vector3.Zero;
        }
        public VertexDeclaration VertexDeclaration
        {
            get => _vertexDeclaration;
        }
    }
}