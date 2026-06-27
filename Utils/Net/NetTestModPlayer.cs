namespace KL.Utils.Net;

public class NetTestModPlayer : KLModPlayer
{
    public override void ResetEffects()
    {
        base.ResetEffects();
    }

    public override void FrameEffects()
    {
        base.FrameEffects();
    }

    public void TestFunction()
    {
        PrintText("Test Function Was Called!");
    }
}