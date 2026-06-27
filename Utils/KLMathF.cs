namespace KL.Utils;

public static class KLMathF
{
    /// <summary>
    /// 限制在min和max之间的lerp
    /// </summary>
    /// <param name="t"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public static float ClampLerp(float min, float max, float t)
    {
        float lerp = MathHelper.Lerp(min, max, t);
        if(min>max) return MathHelper.Clamp(lerp, max, min);
        return MathHelper.Clamp(lerp, min, max);
    }
}