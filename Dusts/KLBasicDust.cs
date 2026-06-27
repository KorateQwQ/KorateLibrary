using KL.Utils;
using Terraria.Graphics;

namespace KL.Dusts;

public abstract class KLBasicDust : ModDust
{
    
    public Texture2D MainTexture = null;
    public TextureTowards textureTowards = TextureTowards.Right;
    public bool ShouldBloom = false;
    
    public enum TextureTowards
    {
        //从左往右切分读取帧图
        Right,
        Left,
        //随机读取帧图
        Random
    }

    /// <summary>
    /// 将指定粒子以指定弧形范围发射，360度则为圆形发射
    /// </summary>
    /// <param name="position">锚点位置</param>
    /// <param name="dustType"></param>
    /// <param name="spawnAmount"></param>
    /// <param name="velocity"></param>
    /// <param name="rotRange">发射角度范围，默认为1也就是一个小弧形范围</param>
    /// <param name="lifeTime"></param>
    /// <param name="color"></param>
    /// <param name="scale"></param>
    /// <param name="startDistance">起始位置偏移，会从锚点到距离锚点offset的位置为原点生成每一个粒子</param>
    /// <param name="startOffset">起始位置偏移范围，粒子的最终位置是起始点+偏移距离+偏移范围</param>
    /// <param name="scaleOffset">大小偏移，粒子的大小会在scale+-scaleOffset之间</param>
    /// <param name="lifeOffset">存活时间偏移，粒子的存活时间会在lifeTime+-lifeOffset之间</param>
    public static void SpawnDustsCircle(Vector2 position, int dustType,int spawnAmount, Vector2? velocity = null, float rotRange = 1,
        float lifeTime = 60, Color? color = null, Vector2? scale = null,float startDistance = 0, float startOffset = 0,
        Vector2 scaleOffset = default, int lifeOffset = 0, float velocityOffset = 0,Entity attachedEntity = null,object dustData = null)
    {
        velocity??=Vector2.Zero;
        color??=Color.White;
        scale ??= Vector2.One;
        
        Vector2 baseVelocity = velocity.Value;
        
        for(int i =0;i<spawnAmount;i++)
        {
            float rot = Main.rand.NextFloat(- rotRange / 2f,rotRange/2f) ;
            velocity = baseVelocity.RotatedBy(rot);
            Vector2 move = new Vector2(1, 0);
            if (velocity != Vector2.Zero) move = velocity.Value;
            move.Normalize();
            
            Vector2 finalScale = scale.Value +Main.rand.NextVector2Circular(scaleOffset.X,scaleOffset.Y);
            
            SpawnDust(position + move * (startDistance+Main.rand.NextFloat(0,startOffset)),dustType,velocity * Main.rand.NextFloat(1-velocityOffset,1+velocityOffset),lifeTime+Main.rand.Next(-lifeOffset,lifeOffset),color,finalScale,attachedEntity,dustData);
        }
    }
    
    
    public static void SpawnDust(Vector2 position,int dustType,Vector2? velocity=null,float lifeTime = 60, Color? color = null,Vector2? scale = null,Entity attachedEntity = null,object dustData = null)
    {
        velocity??=Vector2.Zero;
        color??=Color.White;
        scale ??= Vector2.One;

        Dust dust =Dust.NewDustDirect(position, 0, 0, dustType, velocity.Value.X, velocity.Value.Y, 0, color.Value, 1);
        dust.velocity = velocity.Value;
        dust.position = position;
        dust.customData = new DustInfo(lifeTime,scale.Value,attachedEntity);
        
        ModDust modDust = DustLoader.GetDust(dust.type);
        if (modDust != null)
        {
            if(modDust is KLBasicDust)
            {
                //提前更新一次，否则绘制会先于update出现
                modDust.Update(dust);
                KLBasicDust klBasicDust = (KLBasicDust)modDust;
                //klBasicDust.MainTexture ??= 
            }
        }
        if(dustData!=null) dust.DustInfo().DustData = dustData;
    }
    
    public class DustInfo
    {
        public float CurrentLifeTime = 0;
        public float LifeTime = 0;
        public float LifeProgress = 0;
        public Vector2 scale = Vector2.One;
        public Entity AttachedEntity = null;
        public int randFrame = -1;
        
        //自己塞东西，等于customData
        public object DustData = null;
        public DustInfo(float lifeTime,Vector2 scale,Entity attachedEntity = null)
        {
            LifeTime = lifeTime;
            CurrentLifeTime = lifeTime;
            AttachedEntity = attachedEntity;
            this.scale = scale;
        }
    }
    
    public override void OnSpawn(Dust dust)
    {
        MainTexture ??= ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {
        ModDust modDust = DustLoader.GetDust(dust.type);
        if (modDust is KLBasicDust { ShouldBloom: true })
        {
            DrawSystem.KLDustList.Add(dust);
        }
        return false;
    }

    public override bool MidUpdate(Dust dust)
    {
        return base.MidUpdate(dust);
    }

    /// <summary>
    /// 自动计算粒子生命，自动将速度应用到粒子旋转，自动调整粒子的位置以及可以选择相对位置
    /// </summary>
    /// <param name="dust"></param>
    /// <returns></returns>
    public override bool Update(Dust dust)
    {
        if (dust.customData == null) return false;
        DustInfo dustInfo = (DustInfo)dust.customData;
        dustInfo.CurrentLifeTime--;
        dustInfo.LifeProgress = 1-dustInfo.CurrentLifeTime / dustInfo.LifeTime;

        if (dustInfo.AttachedEntity != null&&dustInfo.AttachedEntity.active)
        {
            //Main.NewText("KLDust:" + (dustInfo.AttachedEntity.position +" "+ dustInfo.AttachedEntity.oldPosition));
            Vector2 oldPosition = dustInfo.AttachedEntity.oldPosition;
            Vector2 position = dustInfo.AttachedEntity.position;
            if (dustInfo.AttachedEntity is Projectile projectile)
            {
                if(projectile.GetGlobalProjectile<TimeStopManager.TimeStopGlobalProjectile>() !=null)
                {
                    oldPosition = projectile.GetGlobalProjectile<TimeStopManager.TimeStopGlobalProjectile>().oldCenter;
                    position = projectile.Center;
                }
            }
            dust.position = dust.position+ dust.velocity + position - oldPosition;
        }
        else
        {
            dust.position += dust.velocity;
        }
        dust.rotation = dust.velocity.ToRotation();
        if (dustInfo.CurrentLifeTime <0) dust.active = false;
        return false;
    }
    
    public virtual void DrawBloom(Dust dust)
    {
        
    }

}
public static class KLBasicDustExtensions
{
    public static Vector2 Size(this Dust dust) => ((KLBasicDust.DustInfo)dust.customData).scale;
    
    public static Vector2 SetSize(this Dust dust, Vector2 size) => ((KLBasicDust.DustInfo)dust.customData).scale = size;
    
    public static float LifeTime(this Dust dust)  => ((KLBasicDust.DustInfo)dust.customData).LifeTime;
    
    public static float CurrentLifeTime(this Dust dust)  => ((KLBasicDust.DustInfo)dust.customData).CurrentLifeTime;
    
    public static Entity GetAttachedEntity(this Dust dust)  => ((KLBasicDust.DustInfo)dust.customData).AttachedEntity;
    /// <summary>
    /// 粒子生命进度，随着粒子消逝而最终为1
    /// </summary>
    /// <param name="dust"></param>
    /// <returns></returns>
    public static float LifeProgress(this Dust dust)  => ((KLBasicDust.DustInfo)dust.customData).LifeProgress;
    
    private static bool CanMoveInTimeStop(this Dust dust)
    {
        if (TimeStopDustList[dust.dustIndex].timeInStop > 0) return true;
        return false;
    }
    public static bool NeedGreyEffect(this Dust dust) => !dust.CanMoveInTimeStop() && GreyEffect;
    public static Color GetRealColor(this Dust dust,Color color)
    {
        //if(dust.NeedGreyEffect())return new Color(255, 255, 255, 255)*0.7f;
        return color;
    }
    public static Vector4 GetRealColor(this Dust dust, Vector4 color)
    {
        //if(dust.NeedGreyEffect())return new Vector4(1, 1, 1, 1)*0.7f;
        return color;
    }
    
    public static KLBasicDust.DustInfo DustInfo(this Dust dust)=>((KLBasicDust.DustInfo)dust.customData);

}