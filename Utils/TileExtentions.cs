namespace KL.Extensions;

public static class TileExtentions
{
    public static void ForceChangeActice(this Tile tile,bool active)
    {
        MethodInfo activeMethod = typeof(Tile).GetMethod("active", 
            BindingFlags.NonPublic | BindingFlags.Instance, 
            null, 
            new Type[] { typeof(bool) }, 
            null);

        if (activeMethod != null)
        {
            object[] args = { true }; // 参数值
            activeMethod.Invoke(tile, args); // 通过反射调用
        }
    }
    
    //该物块为雪地类型物块
    public static bool IsSnowBiomeTile(this Tile tile)
    {
        return TileID.Sets.SnowBiome[tile.TileType]>0;
    } 
    
}