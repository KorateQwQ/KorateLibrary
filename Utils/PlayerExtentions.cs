namespace KL.Utils;

public static class PlayerExtentions
{
    /// <summary>
    /// Removes every debuff currently applied to the player.
    /// </summary>
    /// <remarks>
    /// Buff slots are compacted by <see cref="Player.DelBuff(int)"/> after a
    /// removal, so the slots must be visited from the end to avoid skipping
    /// the buff that shifts into the removed slot.
    /// </remarks>
    public static void ClearDebuff(this Player player)
    {
        if (player == null)
            return;

        for (int index = player.buffType.Length - 1; index >= 0; index--)
        {
            int buffType = player.buffType[index];
            if (buffType > 0 && buffType < Main.debuff.Length && Main.debuff[buffType])
                player.DelBuff(index);
        }
    }

    public static Vector2 VisualCenter(this Player player)
    {
        return player.MountedCenter + new Vector2(0, player.gfxOffY);
    }
    
}
