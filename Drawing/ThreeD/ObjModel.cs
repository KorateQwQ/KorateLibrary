using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace KL.Drawing.ThreeD
{
    public class ObjModel
    {
        private VertexBuffer vertexBuffer;

        public Vertex3D[] Vertices { get; }

        public int PrimitiveCount => Vertices.Length / 3;

        public ObjModel(Vertex3D[] vertices)
        {
            Vertices = vertices;
        }

        public static ObjModel Load(string assetPath, Color? color = null)
        {
            if(Main.netMode== NetmodeID.Server)return null;
            ObjModel model = ObjLoader.Load(assetPath, color);
            model.GetOrCreateVertexBufferOnMainThread();
            return model;
        }

        public static ObjModel Load(Mod mod, string assetPath, Color? color = null)
        {
            if(Main.netMode== NetmodeID.Server)return null;
            ObjModel model = ObjLoader.Load(mod, assetPath, color);
            model.GetOrCreateVertexBufferOnMainThread();
            return model;
        }

        public VertexBuffer GetOrCreateVertexBuffer(GraphicsDevice graphicsDevice)
        {
            if (vertexBuffer != null && !vertexBuffer.IsDisposed)
                return vertexBuffer;

            vertexBuffer = new VertexBuffer(graphicsDevice, typeof(Vertex3D), Vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(Vertices);
            return vertexBuffer;
        }

        public VertexBuffer GetOrCreateVertexBufferOnMainThread()
        {
            if (vertexBuffer != null && !vertexBuffer.IsDisposed)
                return vertexBuffer;

            Main.RunOnMainThread(() =>
            {
                vertexBuffer = GetOrCreateVertexBuffer(Main.graphics.GraphicsDevice);
            });
            return vertexBuffer;
        }
    }
}