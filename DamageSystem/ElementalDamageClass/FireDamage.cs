namespace KL.DamageSystem.ElementalDamageClass;

public class FireDamage : ElementalDamage
{
    public override void Load()
    {
        DamageManager.RegisterDamageColor(this, new Color(255, 50, 42, 255));
        base.Load();
    }
}