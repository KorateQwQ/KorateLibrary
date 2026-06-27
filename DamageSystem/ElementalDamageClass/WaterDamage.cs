namespace KL.DamageSystem.ElementalDamageClass;

public class WaterDamage : ElementalDamage
{
    public override void Load()
    {
        DamageManager.RegisterDamageColor(this,new Color(0, 146, 255, 150));
        base.Load();
    }
}