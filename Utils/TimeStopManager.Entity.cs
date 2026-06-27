using Terraria.DataStructures;

namespace KL.Utils;

public partial class TimeStopManager : ModSystem
{
    public class TimeStopPlayer : ModPlayer
    {
        //每帧词条，可以由饰品或buff短暂改变
        public bool CanMoveInTimeStop = false;

        //永久词条，拥有此词条的玩家不会受到时停影响
        public bool ImmuneTimeStop = false;

        public override void ResetEffects()
        {
            CanMoveInTimeStop = ImmuneTimeStop;
            
            base.ResetEffects();
        }

        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            if (timeStop) return false;
            return base.CanBeHitByNPC(npc, ref cooldownSlot);
        }

        public override bool CanBeHitByProjectile(Projectile proj)
        {
            if (timeStop) return false;

            return base.CanBeHitByProjectile(proj);
        }
    }

    public class TimeStopGlobalProjectile : GlobalProjectile
    {
        public static bool SpecialDrawInTimeStop = false;
        
        public int time = 0;
        public bool HeldProj = false;
        public int MaxMoveTime = 0;
        bool StraightMove = false;
        bool StraightMove2 = false;

        public Vector2 oldCenter = Vector2.Zero;
        
        
        /// <summary>
        /// 此弹幕不受时停影响
        /// </summary>
        public bool ImmuneTimeStop = false;

        public override bool InstancePerEntity => true;

        public bool CanMoveInTimeStop()
        {
            return (time < MaxMoveTime || HeldProj || ImmuneTimeStop);
        }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            MaxMoveTime = Main.rand.Next(20, 41);
            base.OnSpawn(projectile, source);
        }

        public override bool PreAI(Projectile projectile)
        {
            oldCenter = projectile.Center;
            //Main.NewText(projectile.Name + " " + projectile.type);
            //此为最终棱镜的射线，应该判断为手持弹幕
            if (projectile.type == ProjectileID.LastPrismLaser) ImmuneTimeStop = true;
            if (((projectile.position - projectile.oldPosition) == projectile.velocity))
            {   
            }
            
            float distanceToOwner = (projectile.Center).Distance(Main.projectile[projectile.owner].Center);
            //Main.NewText(projectile.Name+" " + projectile.type + " " + distanceToOwner);
			return base.PreAI(projectile);
        }
        public override void DrawBehind(Projectile projectile, int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            //已正常颜色显示的弹幕    
            if (GreyEffect && CanMoveInTimeStop())
            {
                if (!overWiresUI.Contains(index) && (!projectile.GetGlobalProjectile<TimeStopGlobalProjectile>().HeldProj||projectile.type== ProjectileID.LastPrismLaser))
                {
                    overWiresUI.Add(index);
                }
            }
            base.DrawBehind(projectile, index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (GreyEffect && !SpecialDrawInTimeStop && CanMoveInTimeStop())
            {
                return false;
            }
            return base.PreDraw(projectile, ref lightColor);    
        }

        public override void AI(Projectile projectile)
        {
            base.AI(projectile);
        }
        public override void PostAI(Projectile projectile)
        {
            if (Main.player[projectile.owner].heldProj == projectile.whoAmI)
            {
                HeldProj = true;
                time = 0;
            }
            else HeldProj = false;

            if (CanMoveInTimeStop()&&time < MaxMoveTime)
            {
                time++;
                //Main.NewText(projectile.position + " " + projectile.oldPosition + " " + projectile.velocity);
                if(((projectile.position- projectile.oldPosition)- projectile.velocity).Length()<1)
                {
                    if (StraightMove) StraightMove2 = true;
                    StraightMove = true;
                    //Main.NewText((projectile.position - projectile.oldPosition)+" "+ projectile.velocity);
                }
                if (timeStop&& StraightMove&& StraightMove2&& ProjectileLoader.ShouldUpdatePosition(projectile))
                {
                    Vector2 velocity = projectile.velocity * MathHelper.Lerp(0, 1, time / (float)MaxMoveTime);
                    projectile.position-=velocity;
                    if((projectile.oldVelocity == projectile.velocity))
                    {
                        int TrailLen = 0;
                        for (float i = 0; i < projectile.oldPos.Length; i++)
                        {
                            if (projectile.oldPos[(int)i] != Vector2.Zero)
                            {
                                TrailLen++;
                            }
                        }
                        for (float i = 0; i < TrailLen; i++)
                        {
                            projectile.oldPos[(int)i] -= velocity * MathHelper.Lerp(1, 0, i / TrailLen);
                        }
                    }

                    //projectile.position = (projectile.oldPosition - projectile.position) *MathHelper.Lerp(0, 1, time / (float)MaxMoveTime);
                }

            }


			base.PostAI(projectile);
        }
    }
}