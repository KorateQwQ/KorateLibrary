using System.IO;
using System.Security.Policy;
using KL.DamageSystem;
using KL.Utils;
using Terraria.DataStructures;

namespace KL.Projectiles;

public abstract class KLProjectile : ModProjectile
{
    /// <summary>
    /// 弹幕默认元素元素
    /// </summary>
    public ElementType InfusionElement;

    //拖尾位置，弹幕的中心
    public Vector2 [] OldCenter;

    private Vector2[] oldCenterTrailPositions = Array.Empty<Vector2>();

    private int oldCenterTrailPointCount;

    public bool ImmuneTimeStop = false;
    
    //拖尾记录的长度，大于0说明此弹幕为拖尾弹幕
    public int TrailLength = 0;
    public Texture2D ThisTex => TextureAssets.Projectile[Projectile.type].Value;

    public Player Owner => Main.player[Projectile.owner];

    //联机同步，是否为本地控制的弹幕
    public bool HasAuthority() => Owner.whoAmI == Main.myPlayer;

    //根据指定位置（通常为激光对着鼠标位置）旋转，如果没有速度则是立即抵达，否则根据速度靠近

    private bool _born = false;

    public int NumOfOwingProjectiles => Projectile.GetGlobalProjectile<KLGlobalProjectile>().NumOfOwingProjectiles;
    #region AI
    public virtual void OnSpawn_AllClient()
    {
    }

    public override bool PreAI()
    {
        if (!_born)
        {
            if (InfusionElement != ElementType.None)
            {
                Projectile.GetGlobalProjectile<ElementalGlobalProjectile>().InfusionElement = InfusionElement;
            }
            _born = true;
            OnSpawn_AllClient();
            if (HeldInfo.IsHeld && HasAuthority())
            {
                Vector2 direction = Main.MouseWorld - Owner.MountedCenter;
                Vector2 direction2 = Projectile.Center - Owner.MountedCenter;

                switch (HeldInfo.LockType)
                {
                    case HeldProjLockType.MousePosition:
                    {
                    }
                        break;
                    case HeldProjLockType.FreePosition:
                    {
                        direction = HeldInfo.LockPosition;
                    }
                        break;
                }
                float targetRotation = (float)Math.Atan2(direction.Y, direction.X);
                float targetRotation2 = (float)Math.Atan2(direction2.Y, direction2.X);
                HeldInfo.Rotation = targetRotation;
                HeldInfo.RotationOffset = targetRotation - targetRotation2;
                
            }
        }
        
        return base.PreAI();
    }

    public override void AI()
    {
        if (HeldInfo.IsHeld) Projectile.GetGlobalProjectile<TimeStopGlobalProjectile>().time = 0;
        base.AI();
    }

    public override void PostAI()
    {
        if (HasAuthority())
        {
            if (HeldInfo.IsHeld)
            {
                //当目标旋转和当前预期旋转差距过大，且鼠标处于停止状态时，进行发包
                if (Math.Abs(HeldInfo.LastRotation - HeldInfo.RealRotation) >
                    0.05f) //&&lastMouseScreen==Main.MouseScreen
                {
                    Projectile.netUpdate = true;
                    HeldInfo.LastRotation = HeldInfo.RealRotation;
                }
                switch (HeldInfo.LockType)
                {
                    case HeldProjLockType.MousePosition:
                    {
                        HeldInfo.LockPosition = Main.MouseWorld;
                    }
                        break;
                    case HeldProjLockType.FreePosition:
                    {
                    }
                        break;
                }    
            
            }
        }

        if (HeldInfo.IsHeld)
        {
            RotateToPosition(HeldInfo.LockPosition, HeldInfo.RotationSpeed);
            if(HeldInfo.IsControlling) Projectile.timeLeft = HeldInfo.RemainingTime;
            
            Projectile.Center = Owner.MountedCenter + new Vector2(1, 0).RotatedBy(HeldInfo.RealRotation) * HeldInfo.HeldDistance;

        }
        base.PostAI();
    }

    #endregion

    public bool CanMoveInTimeStop => Projectile.GetGlobalProjectile<TimeStopGlobalProjectile>().CanMoveInTimeStop();
    protected bool NeedGreyEffect =>
        !CanMoveInTimeStop && GreyEffect;

    public override void Load()
    {
        if(GetType().FullName!=null) KL.NetInstance.Add(GetType().FullName, this);
        base.Load();
    }

    #region 网络RPC

    protected void RPC(string methodName, object[] parameters = null,KLNetModule.NetSendType netSendType = KLNetModule.NetSendType.ClientToAll)
    {
        KLNetModule.RPC(GetType().FullName,Projectile.whoAmI, methodName, parameters,netSendType);
    }
    protected void RPC(string methodName,KLNetModule.NetSendType netSendType = KLNetModule.NetSendType.ClientToAll)
    {
        KLNetModule.RPC(GetType().FullName,Projectile.whoAmI, methodName,[],netSendType);
    }

    #endregion


    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(HeldInfo.IsHeld);
        writer.Write(HeldInfo.IsControlling);

        writer.WriteVector2(HeldInfo.LockPosition);
        writer.Write(HeldInfo.RotationOffset);
        base.SendExtraAI(writer);
    }


    public override void ReceiveExtraAI(BinaryReader reader)
    {
        HeldInfo.IsHeld = reader.ReadBoolean();
        HeldInfo.IsControlling = reader.ReadBoolean();
        
        HeldInfo.LockPosition = reader.ReadVector2();
        HeldInfo.RotationOffset = reader.ReadSingle();
        base.ReceiveExtraAI(reader);
    }
    
    public Vector4 GetColor(Vector4 color)
    {
        //if(NeedGreyEffect)return new Vector4(1, 1, 1, 1)*0.7f;
        return color;
    }

    public Color GetColor(Color color)
    {
        //if(NeedGreyEffect)return new Color(255, 255, 255, 255)*0.7f;
        return color;
    }
    
    protected enum HeldProjLockType
    {
        MousePosition,
        FreePosition,
    }
    protected struct HeldProjInfo
    {
        //说明此时为手持弹幕逻辑，弹幕将会遵循：在指定的半径上，以指定的速度朝目标位置旋转。这个位置默认为鼠标位置。在掌控时(IsControlling)持续保持弹幕的剩余时间
        public bool IsHeld = false;

        public bool IsControlling = true;
        //手持时弹幕剩余持续时间
        public int RemainingTime = 20;
        
        //手持弹幕渐进至目标朝向的旋转速度，为0时立刻转向至目标朝向
        public float RotationSpeed  = 0.2f;
        
        //手持弹幕时，弹幕距离玩家的距离
        public float HeldDistance = 100;

        public float LastRotation;
        
        public float Rotation;

        public float RotationOffset;

        public HeldProjLockType LockType = HeldProjLockType.MousePosition;

        public float RealRotation => Rotation + RotationOffset;

        //如果是鼠标锁定模式，此值无效。
        public Vector2 LockPosition;

        //当前旋转速度，只读
        public float CurRotVelocity = 0;
        public HeldProjInfo(bool isHeld, int remainingTime = 20, float rotationSpeed = 0.2f,float heldDistance = 100,HeldProjLockType lockType = HeldProjLockType.MousePosition)
        {
            IsHeld = isHeld;
            RemainingTime = remainingTime;
            RotationSpeed = rotationSpeed;
            HeldDistance = heldDistance;
            LockType = lockType;
        }
    }
    
    
    private Vector2 mousePosition;
    
    protected HeldProjInfo HeldInfo = new HeldProjInfo(false);
    
    public override bool ShouldUpdatePosition()
    {
        if (HeldInfo.IsHeld) return false;
        
        return base.ShouldUpdatePosition();
    }
    protected void RotateToPosition(Vector2 targetPos, float rotVelocity =0, bool smooth = true)
    {
        // 计算从弹幕位置指向目标位置的方向向量
        Vector2 direction = targetPos - Owner.MountedCenter;

        // 计算目标角度（弧度）
        float targetRotation = (float)Math.Atan2(direction.Y, direction.X);

        // 如果旋转速度为0，立即对准目标
        if (rotVelocity == 0)
        {
            HeldInfo.Rotation = targetRotation;
            return;
        }
        
        // 计算当前角度和目标角度的差值
        float angleDiff = targetRotation - HeldInfo.Rotation;
        
        // 将角度差标准化到[-π, π]范围内
        while (angleDiff > Math.PI)
            angleDiff -= (float)(2 * Math.PI);
        while (angleDiff < -Math.PI)
            angleDiff += (float)(2 * Math.PI);
        
        // 根据是否启用平滑模式选择旋转方式
        if (smooth)
        {
            // 平滑模式：距离目标角度越近，角速度越低
            // 使用角度差的绝对值作为减速因子，当角度差接近0时，旋转速度也接近0
            float smoothFactor = Math.Abs(angleDiff) / MathHelper.Pi; // 归一化到[0,1]范围
            float currentRotVelocity = rotVelocity * smoothFactor;
            
            // 确保最小旋转速度，避免在接近目标时旋转过慢
            currentRotVelocity = Math.Max(currentRotVelocity, 0.005f);

            if (Math.Abs(angleDiff) < 0.005f)
            {
                HeldInfo.Rotation = targetRotation;
                HeldInfo.CurRotVelocity = 0;

            }
            else
            {
                HeldInfo.Rotation += Math.Sign(angleDiff) * currentRotVelocity;
                HeldInfo.CurRotVelocity = Math.Sign(angleDiff) *currentRotVelocity;

            }
        }
        else
        {
            // 非平滑模式：保持恒定角速度
            if (Math.Abs(angleDiff) < rotVelocity)
            {
                HeldInfo.Rotation = targetRotation;
                HeldInfo.CurRotVelocity = 0;
            }
            else
            {
                float currentRotVelocity = Math.Sign(angleDiff) * rotVelocity;
                HeldInfo.Rotation += currentRotVelocity;
                HeldInfo.CurRotVelocity = currentRotVelocity;

            }
        }
    }
    
    public override void SetDefaults()
    {
        /*if (TrailLength > 0)
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }*/

        Projectile.GetGlobalProjectile<TimeStopGlobalProjectile>().ImmuneTimeStop = ImmuneTimeStop;
        base.SetDefaults();
    }

    protected bool ValidTrailArray(int minPointCount = 2)
    {
        return TrailLength > 0 && OldCenter != null && OldCenter.Length >= minPointCount;
    }
    
    /// <summary>
    /// 如果使用拖尾，则需要调用base来记录OldCenter,OldCenter可能为空。
    /// </summary>
    /// <param name="lightColor"></param>
    /// <returns></returns>
    public override bool PreDraw(ref Color lightColor)
    {
        if (Main.gamePaused) return false;
        if (TrailLength > 0)
        {
            if (oldCenterTrailPositions.Length != TrailLength)
            {
                oldCenterTrailPositions = new Vector2[TrailLength];
                oldCenterTrailPointCount = 0;
            }

            for (int i = oldCenterTrailPositions.Length - 1; i > 0; i--)
            {
                oldCenterTrailPositions[i] = oldCenterTrailPositions[i - 1];
            }

            oldCenterTrailPositions[0] = Projectile.Center;
            oldCenterTrailPointCount = Math.Min(oldCenterTrailPointCount + 1, oldCenterTrailPositions.Length);

            OldCenter = new Vector2[oldCenterTrailPointCount];
            Array.Copy(oldCenterTrailPositions, OldCenter, oldCenterTrailPointCount);
        }
        return false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }

    public static void SpawnWindCircle(IEntitySource source,Texture2D tex,Vector2 center, Vector2 velocity, Color? frontColor = null,
        Color? backColor = null,float height = 150,float width = 20,int drawTimes = 1,int frontBlendState = 1,int backBlendState = 0)
    {
        frontColor??=Color.White;
        backColor??=Color.Black;

        if (Projectile.NewProjectileDirect(source,center,velocity, ModContent.ProjectileType<WindCircle>(), 0, 0, Main.myPlayer).ModProjectile is WindCircle proj)
        {
            proj.FrontColor = frontColor.Value;
            proj.BackColor = backColor.Value;
            proj.defHeight = height;
            proj.defWidth = width;
            proj.tex = tex;
            proj.DrawTimes = drawTimes;
            proj.BackBlendState = backBlendState;
            proj.FrontBlendState = frontBlendState;
        }

    }
    public static void SpawnWindCircle(IEntitySource source,Vector2 center, Vector2 velocity, Color? frontColor = null,
        Color? backColor = null,float height = 150,float width = 20,int drawTimes = 1,int frontBlendState = 1,int backBlendState = 0)
    {
        frontColor??=Color.White;
        backColor??=Color.Black;

        if (Projectile.NewProjectileDirect(source,center,velocity, ModContent.ProjectileType<WindCircle>(), 0, 0, Main.myPlayer).ModProjectile is WindCircle proj)
        {
            proj.FrontColor = frontColor.Value;
            proj.BackColor = backColor.Value;
            proj.defHeight = height;
            proj.defWidth = width;
            proj.DrawTimes = drawTimes;
            proj.BackBlendState = backBlendState;
            proj.FrontBlendState = frontBlendState;
        }
    }
}