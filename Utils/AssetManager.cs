using ReLogic.Content;

namespace KL.Utils;

public static class AssetManager
{
    private static string ResolveAssetPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string trimmedPath = path.Trim().Replace('\\', '/');
        if (ModContent.HasAsset(trimmedPath))
        {
            return trimmedPath;
        }

        string dotCompatiblePath = trimmedPath.Replace('.', '/');
        if (dotCompatiblePath != trimmedPath && ModContent.HasAsset(dotCompatiblePath))
        {
            return dotCompatiblePath;
        }

        return null;
    }

    public static Asset<T> Request<T>(string path, AssetRequestMode requestMode = AssetRequestMode.ImmediateLoad) where T : class
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Warn("资源路径为空，无法请求资源");
            return null;
        }

        string assetPath = ResolveAssetPath(path);
        if (assetPath == null)
        {
            Warn($"资源不存在：{path}");
            return null;
        }

        return ModContent.Request<T>(assetPath, requestMode);
    }

    public static Texture2D GetTexture(string path, AssetRequestMode requestMode = AssetRequestMode.ImmediateLoad)
    {
        return Request<Texture2D>(path, requestMode)?.Value;
    }
}