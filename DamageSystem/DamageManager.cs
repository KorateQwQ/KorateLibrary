namespace KL.DamageSystem;

public class DamageManager : ModSystem
{
    static Dictionary<DamageClass, Color> DamageColors = new Dictionary<DamageClass, Color>();

    public static void RegisterDamageColor(DamageClass damageClass, Color color)
    {
        DamageColors[damageClass] = color;
    }

    public static void ModifyDamageColor(DamageClass damageClass, Color color)
    {
        if (DamageColors.ContainsKey(damageClass))
            DamageColors[damageClass] = color;
    }

    public static Color? GetDamageColor(DamageClass damageClass)
    {
        if (DamageColors.TryGetValue(damageClass, out Color color))
            return color;

        return null;
    }
}