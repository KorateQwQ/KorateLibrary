


namespace KL.Extensions;

public static class ItemExtentions
{
    public static bool IsTool(this Item item)
    {
        return item.pick > 0 || item.axe > 0 || item.hammer > 0;
    }
    
    public static string ZHlan(this string s,string s2)
    {
        if(Language.ActiveCulture.Name == "zh-Hans")s = s2;
        return s;
    }

    /// <summary>
    /// 计算物品绘制中心点，对于多帧图物品来说很好用
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public static Vector2 CenterForDraw(this Item item)
    {
        Texture2D texture = TextureAssets.Item[item.type].Value;
        Rectangle rec = new Rectangle();
        if (Main.itemAnimations[item.type] != null) rec = Main.itemAnimations[item.type].GetFrame(texture, -1);
        else rec = texture.Frame();
                
        Vector2 vector = rec.Size() / 2f;
        Vector2 vector2 = new Vector2((float)(item.width / 2) - vector.X, item.height - rec.Height);
        Vector2 vector3 = item.position + vector + vector2;

        return vector3;
    }

    /// <summary>
    /// 同步item中所有需要同步的属性。当客户端调用时，数据仅传递给服务器。（但是，此方法中服务器会再次传输数据给客户端）。服务端调用时，数据传递给所有其他客户端。
    /// </summary>
    /// <param name="item"></param>
    public static void NetUpdate(this Item item)
    {
        NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item.whoAmI);
    }
}