namespace KL.Extensions;

public static class EffectExtentions
{
    public static Effect LoadEffect(string path,AssetRequestMode mode = AssetRequestMode.ImmediateLoad)
    {
        string assetPath = "KL/Effects/Content/" + path;
        string assetPath2 = "KL/Effects/Content/BasicShape/"+path;

        if (ModContent.RequestIfExists<Effect>(assetPath, out var asset, mode)) {
            return asset.Value;
        }
        
        if (ModContent.RequestIfExists<Effect>(assetPath2, out var asset2, mode))
        {
            return asset2.Value;
        }

        return null;
    }
    
    public static Asset<Effect> LoadPlayerEffect(string path,AssetRequestMode mode = AssetRequestMode.ImmediateLoad)
    {
        return ModContent.Request<Effect>("KL/Effects/Content/PlayerEffect/"+path, mode);
    }
    
    /// <summary>
    /// 获取玩家特效ID,此effect必须是KL通过PlayerEffectHelper自动加载的Effect。
    /// </summary>
    /// <param name="effect"></param>
    /// <returns></returns>
    public static int GetPlayerEffectID(this Asset<Effect> effect)
    {
        PlayerEffectHelper.PlayerEffectMap.TryGetValue(effect, out int id);
        return id;
    }
    
    public static void Apply(this Effect effect, int index=0)
    {
        effect.CurrentTechnique.Passes[index].Apply();
    }

    public static void SetTexture(this Effect effect,  int index, Texture2D value)
    {
        Main.graphics.GraphicsDevice.Textures[index] = value;
    }
    
    public static void SetValue(this Effect effect, string name, bool value)
    {
        effect.Parameters[name].SetValue(value);
    }
    
    public static void SetValue(this Effect effect, string name, bool[] value)
    {
        effect.Parameters[name].SetValue(value);
    }
    
    public static void SetValue(this Effect effect, string name, float value)
    {
        effect.Parameters[name].SetValue(value);
    }
    public static void SetValue(this Effect effect, string name, float[] value)
    {
        effect.Parameters[name].SetValue(value);
    }
    
    public static void SetValue(this Effect effect, string name, int value)
    {
        effect.Parameters[name].SetValue(value);
    }
    public static void SetValue(this Effect effect, string name, int[] value)
    {
        effect.Parameters[name].SetValue(value);
    }
    public static void SetValue(this Effect effect, string name, string value)
    {
        effect.Parameters[name].SetValue(value);
    }
    public static void SetValue(this Effect effect, string name, Texture value)
    {
        effect.Parameters[name].SetValue(value);
    }
    public static void SetValue(this Effect effect, string name, Texture2D value)
    {
        effect.Parameters[name].SetValue(value);
    }
    public static void SetValue(this Effect effect, string name, Vector2 value)
    {
        effect.Parameters[name].SetValue(value);
    }
    public static void SetValue(this Effect effect, string name, Vector3 value)
    {
        effect.Parameters[name].SetValue(value);
    }
    
    public static void SetValue(this Effect effect, string name, Vector4 value)
    {
        effect.Parameters[name].SetValue(value);
    }

    public static void SetValue(this Effect effect, string name, Matrix value)
    {
        effect.Parameters[name].SetValue(value);
    }

}