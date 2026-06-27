using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Terraria.ModLoader;

namespace KL.Drawing.ThreeD
{
    public static class ObjLoader
    {
        private readonly struct ObjFaceVertex
        {
            public readonly int PositionIndex;
            public readonly int TexCoordIndex;
            public readonly int NormalIndex;

            public ObjFaceVertex(int positionIndex, int texCoordIndex, int normalIndex)
            {
                PositionIndex = positionIndex;
                TexCoordIndex = texCoordIndex;
                NormalIndex = normalIndex;
            }
        }

        public static ObjModel Load(string assetPath, Color? color = null)
        {
            string resolvedAssetPath = ResolveAssetPath(assetPath);
            byte[] fileBytes = ModContent.GetFileBytes(resolvedAssetPath);
            if (fileBytes == null)
                throw new FileNotFoundException($"OBJ 模型不存在：{resolvedAssetPath}");

            string text = Encoding.UTF8.GetString(fileBytes);
            return Parse(text, color ?? Color.White);
        }

        public static ObjModel Load(Mod mod, string assetPath, Color? color = null)
        {
            if (mod == null)
                throw new ArgumentNullException(nameof(mod));

            string qualifiedAssetPath = CombineAssetPath(mod, assetPath);
            return Load(qualifiedAssetPath, color);
        }

        public static ObjModel Parse(string text, Color color)
        {
            List<Vector3> positions = new();
            List<Vector2> texCoords = new();
            List<Vector3> normals = new();
            List<Vertex3D> vertices = new();

            using StringReader reader = new StringReader(text);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                switch (parts[0])
                {
                    case "v":
                        positions.Add(ParseVector3(parts));
                        break;
                    case "vt":
                        texCoords.Add(ParseVector2(parts));
                        break;
                    case "vn":
                        normals.Add(ParseVector3(parts));
                        break;
                    case "f":
                        ParseFace(parts, positions, texCoords, normals, color, vertices);
                        break;
                }
            }

            if (vertices.Count == 0)
                throw new InvalidDataException("OBJ 文件中没有可用于绘制的三角面。");

            return new ObjModel(vertices.ToArray());
        }

        private static void ParseFace(string[] parts, List<Vector3> positions, List<Vector2> texCoords, List<Vector3> normals, Color color, List<Vertex3D> vertices)
        {
            if (parts.Length < 4)
                return;

            List<ObjFaceVertex> faceVertices = new(parts.Length - 1);
            for (int index = 1; index < parts.Length; index++)
            {
                faceVertices.Add(ParseFaceVertex(parts[index], positions.Count, texCoords.Count, normals.Count));
            }

            for (int index = 1; index < faceVertices.Count - 1; index++)
            {
                AddVertex(faceVertices[0], positions, texCoords, normals, color, vertices);
                AddVertex(faceVertices[index], positions, texCoords, normals, color, vertices);
                AddVertex(faceVertices[index + 1], positions, texCoords, normals, color, vertices);
            }
        }

        private static void AddVertex(ObjFaceVertex faceVertex, List<Vector3> positions, List<Vector2> texCoords, List<Vector3> normals, Color color, List<Vertex3D> vertices)
        {
            Vector3 position = positions[faceVertex.PositionIndex];
            Vector2 texCoord = faceVertex.TexCoordIndex >= 0 ? texCoords[faceVertex.TexCoordIndex] : Vector2.Zero;
            Vector3 normal = faceVertex.NormalIndex >= 0 ? normals[faceVertex.NormalIndex] : Vector3.Zero;
            vertices.Add(new Vertex3D(position, texCoord, color, normal));
        }

        private static ObjFaceVertex ParseFaceVertex(string token, int positionCount, int texCoordCount, int normalCount)
        {
            string[] indexParts = token.Split('/');
            int positionIndex = ResolveIndex(indexParts[0], positionCount);
            int texCoordIndex = indexParts.Length > 1 && !string.IsNullOrWhiteSpace(indexParts[1]) ? ResolveIndex(indexParts[1], texCoordCount) : -1;
            int normalIndex = indexParts.Length > 2 && !string.IsNullOrWhiteSpace(indexParts[2]) ? ResolveIndex(indexParts[2], normalCount) : -1;
            return new ObjFaceVertex(positionIndex, texCoordIndex, normalIndex);
        }

        private static int ResolveIndex(string indexText, int count)
        {
            int rawIndex = int.Parse(indexText, CultureInfo.InvariantCulture);
            if (rawIndex > 0)
                return rawIndex - 1;
            if (rawIndex < 0)
                return count + rawIndex;
            throw new InvalidDataException("OBJ 索引不能为 0。");
        }

        private static Vector3 ParseVector3(string[] parts)
        {
            if (parts.Length < 4)
                throw new InvalidDataException("OBJ 顶点数据格式不正确。");

            return new Vector3(
                ParseFloat(parts[1]),
                ParseFloat(parts[2]),
                ParseFloat(parts[3]));
        }

        private static Vector2 ParseVector2(string[] parts)
        {
            if (parts.Length < 3)
                throw new InvalidDataException("OBJ 纹理坐标格式不正确。");

            return new Vector2(
                ParseFloat(parts[1]),
                1f - ParseFloat(parts[2]));
        }

        private static float ParseFloat(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        private static string CombineAssetPath(Mod mod, string assetPath)
        {
            string normalizedPath = NormalizeAssetPath(assetPath);
            int separatorIndex = normalizedPath.IndexOf('/');
            if (separatorIndex > 0)
            {
                string firstSegment = normalizedPath.Substring(0, separatorIndex);
                if (ModLoader.TryGetMod(firstSegment, out _))
                    return normalizedPath;
            }

            return $"{mod.Name}/{normalizedPath}";
        }

        private static string ResolveAssetPath(string assetPath)
        {
            string normalizedPath = NormalizeAssetPath(assetPath);
            int separatorIndex = normalizedPath.IndexOf('/');
            if (separatorIndex > 0)
            {
                string firstSegment = normalizedPath.Substring(0, separatorIndex);
                if (ModLoader.TryGetMod(firstSegment, out _))
                {
                    if (ModContent.FileExists(normalizedPath))
                        return normalizedPath;

                    throw new FileNotFoundException($"OBJ 模型不存在：{normalizedPath}");
                }
            }

            List<string> matchedPaths = new();
            foreach (Mod mod in ModLoader.Mods)
            {
                if (mod.FileExists(normalizedPath))
                    matchedPaths.Add($"{mod.Name}/{normalizedPath}");
            }

            if (matchedPaths.Count == 1)
                return matchedPaths[0];

            if (matchedPaths.Count > 1)
                throw new InvalidOperationException($"OBJ 路径存在多个匹配，请补全模组名前缀：{string.Join(", ", matchedPaths)}");

            throw new FileNotFoundException($"OBJ 模型不存在：{assetPath}");
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                throw new ArgumentException("OBJ 路径不能为空。", nameof(assetPath));

            string trimmedPath = assetPath.Trim().Replace('\\', '/');
            if (trimmedPath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                trimmedPath = trimmedPath.Substring(0, trimmedPath.Length - 4);

            string normalizedPath = trimmedPath.Replace('.', '/');
            return normalizedPath + ".obj";
        }
    }
}