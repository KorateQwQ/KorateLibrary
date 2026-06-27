namespace KL.Configs;

#if DEBUG

public class KeyBinds : ModSystem
{
    public static ModKeybind 上个技能 { get; private set; }
    public static ModKeybind 下个技能 { get; private set; }
    
    public static ModKeybind 释放技能 { get; private set; }

    public override void Load()
    {
        上个技能 = KeybindLoader.RegisterKeybind(Mod, "上个技能", "Q");
        下个技能 = KeybindLoader.RegisterKeybind(Mod, "下个技能", "E");
        释放技能 = KeybindLoader.RegisterKeybind(Mod, "释放技能", "Z");
        base.Load();
    }
    
}
#endif