namespace KL.DamageSystem.ElementalDamageClass;

public class WindDamage: ElementalDamage
{
    public override void Load()
    {
        DamageManager.RegisterDamageColor(this, new Color(107, 250, 232, 255));
        base.Load();
    }
}