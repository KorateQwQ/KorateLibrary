namespace KL.Utils;

public static class KeyBindExtentions 
{
    /// <summary>
    /// 检查是否绑定了键
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool HasKeybind(this ModKeybind key)
    {
        return key.GetAssignedKeys().Count > 0;
    }
    
    /// <summary>
    /// 强制绑定键某个按键
    /// </summary>
    /// <param name="key"></param>
    /// <param name="keyName"></param>
    
    public static void BindKey(this ModKeybind key, string keyName)
    {
        string keyFullName = key.GetKeybindFullName();
        if (keyFullName == null) return;
        PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard].KeyStatus[keyFullName] = new List<string> { keyName };
    }

    /// <summary>
    /// 获取默认绑定名称，只有按键的名字如"Q"
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string GetKeyDefaultName(this ModKeybind key)
    {
        if(key == null) return null;
        var type = key.GetType();
        var property = type.GetProperty("DefaultBinding", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return property?.GetValue(key) as string;
    }
    
    /// <summary>
    /// 获取键当前绑定全名（会包括模组名和命名空间等一长串字符）
    /// </summary>
    /// <param name="keybind"></param>
    /// <returns></returns>
    public static string GetKeybindFullName(this ModKeybind keybind)
    {
        if (keybind == null) return null;
        var type = keybind.GetType();
        var property = type.GetProperty("FullName", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return property?.GetValue(keybind) as string;
    }
    
    
}