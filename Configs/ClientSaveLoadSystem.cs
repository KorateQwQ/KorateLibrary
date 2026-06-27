using System.IO;
using Terraria.ModLoader.IO;

namespace KL.Configs;

public abstract class ClientSaveLoadSystem : ModSystem
{
    public static bool HasInitializedKeybinds { get; private set; }
    private string ClientFlagsPath =>
        Path.Combine(Main.SavePath, "Mods", Mod.GetType().Name, GetType().Name);

    public override void Load()
    {
        PreLoadClientFlags();
    }

    public override void Unload()
    {
        HasInitializedKeybinds = false;
    }

    public override void PreUpdateWorld()
    {
        base.PreUpdateWorld();
    }

    public override void PreSaveAndQuit()
    {
        string directoryPath = Path.GetDirectoryName(ClientFlagsPath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        
        TagCompound tag = new TagCompound();
        SaveClientFlags(tag);
        
        TagIO.ToFile(tag, ClientFlagsPath);

        base.PreSaveAndQuit();
    }
    private void PreLoadClientFlags()
    {
        if (!File.Exists(ClientFlagsPath))
        {
            HasInitializedKeybinds = false;
            return;
        }
        TagCompound tag = TagIO.FromFile(ClientFlagsPath);
        LoadClientFlags(tag);
    }
    
    /// <summary>
    /// 自定义加载客户端字段
    /// </summary>
    /// <param name="tag"></param>
    public virtual void LoadClientFlags(TagCompound tag)
    {
        //HasInitializedKeybinds = tag.GetBool("HasInitializedKeybinds");
    }

    /// <summary>
    /// 自定义保存客户端字段
    /// </summary>
    /// <param name="tag"></param>
    public virtual void SaveClientFlags(TagCompound tag)
    {
        //tag["HasInitializedKeybinds"] = HasInitializedKeybinds;
    }
}