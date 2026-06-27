using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;
using Terraria.Enums;
using Terraria.Graphics.CameraModifiers;

namespace KL.Extensions
{
    public static class GamePlayStatic
    {
        public enum LogLevel
        {
            Log,
            Warn,
            Error
        }

        public static bool ServerOrLocalMode()
        {
            return Main.netMode is 2 or 0;
        }

        private static string GetNetSideName()
        {
            return Main.netMode switch
            {
                NetmodeID.Server => "Server",
                NetmodeID.MultiplayerClient => "Client",
                _ => "SinglePlayer"
            };
        }

        private static LogLevel ResolveLogLevel(object input, LogLevel level)
        {
            if (level != LogLevel.Log || input is not string text)
            {
                return level;
            }

            string trimText = text.TrimStart();
            if (trimText.StartsWith("Error", StringComparison.OrdinalIgnoreCase) ||
                trimText.StartsWith("Exception", StringComparison.OrdinalIgnoreCase))
            {
                return LogLevel.Error;
            }

            if (trimText.StartsWith("Warn", StringComparison.OrdinalIgnoreCase) ||
                trimText.StartsWith("Fail", StringComparison.OrdinalIgnoreCase))
            {
                return LogLevel.Warn;
            }

            return LogLevel.Log;
        }

        private static string BuildLogPrefix(LogLevel level)
        {
            return $"[{level}][{GetNetSideName()}] ";
        }

        private static bool ShouldOutput(LogLevel level)
        {
            return KL.ShouldShowDebug || level != LogLevel.Log;
        }

        private static Color GetLogColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Warn => Color.Gold,
                LogLevel.Error => Color.OrangeRed,
                _ => Color.White
            };
        }

        private static void WriteLogger(LogLevel level, string message)
        {
            Mod mod = KL.KLInstance;
            if (mod == null)
            {
                Console.WriteLine(message);
                return;
            }

            switch (level)
            {
                case LogLevel.Warn:
                    mod.Logger.Warn(message);
                    break;
                case LogLevel.Error:
                    mod.Logger.Error(message);
                    break;
                default:
                    mod.Logger.Info(message);
                    break;
            }
        }

        public static void Log(object input, LogLevel level = LogLevel.Log)
        {
            LogLevel finalLevel = ResolveLogLevel(input, level);
            if (!ShouldOutput(finalLevel)) return;

            string message = BuildLogPrefix(finalLevel) + (input?.ToString() ?? "null");
            WriteLogger(finalLevel, message);
        }

        public static void Warn(object input)
        {
            Log(input, LogLevel.Warn);
        }

        public static void Error(object input)
        {
            Log(input, LogLevel.Error);
        }

        public static void PrintText(object input, LogLevel level = LogLevel.Log)
        {
            LogLevel finalLevel = ResolveLogLevel(input, level);
            if (!ShouldOutput(finalLevel)) return;

            string message = BuildLogPrefix(finalLevel) + (input?.ToString() ?? "null");
            if (Main.netMode == NetmodeID.Server)
            {
                WriteLogger(finalLevel, message);
            }
            else
            {
                Main.NewText(message, GetLogColor(finalLevel));
            }
        }
        
        
        /// <summary>
        /// 判断鼠标左键是否刚刚被点击
        /// </summary>
        public static bool IsLeftClick()
        {
            return Main.mouseLeft && Main.mouseLeftRelease;
        }

        /// <summary>
        /// 判断鼠标右键是否刚刚被点击
        /// </summary>
        public static bool IsRightClick()
        {
            return Main.mouseRight && Main.mouseRightRelease;
        }

        /// <summary>
        /// 获得一个可旋转的矩形碰撞箱，需结合目标的矩形碰撞箱
        /// </summary>
        /// <param name="targetRect">目标的碰撞箱</param>
        /// <param name="startPosition">判定起始点</param>
        /// <param name="endPosition">判定结束点</param>
        /// <param name="width">宽度</param>
        /// <returns></returns>
        public static bool AABBvLineCollision(Rectangle targetRect,Vector2 startPosition,Vector2 endPosition,float width)
        {
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetRect.TopLeft(), targetRect.Size(), startPosition, endPosition, width, ref point); 
        }
        
        /// <summary>
        /// 计算从矩形中心发射的射线到矩形边界的距离，辅助aabb碰撞方法使得激光可以刚好在敌人碰撞箱的边缘
        /// </summary>
        /// <param name="rectWidth">矩形总宽度</param>
        /// <param name="rectHeight">矩形总高度</param>
        /// <param name="direction">射线方向（单位向量）</param>
        /// <returns>从中心到边界的距离</returns>
        public static float GetRayDistanceToBorder(float rectWidth, float rectHeight, Vector2 direction)
        {
            // 确保方向向量是归一化的
            direction.Normalize();
        
            float halfWidth = rectWidth / 2f;
            float halfHeight = rectHeight / 2f;
        
            // 防止除零错误
            float cosTheta = Math.Abs(direction.X) < 1e-6f ? 1e-6f : Math.Abs(direction.X);
            float sinTheta = Math.Abs(direction.Y) < 1e-6f ? 1e-6f : Math.Abs(direction.Y);
        
            // 计算到垂直边和水平边的距离
            float distanceToVertical = halfWidth / cosTheta;
            float distanceToHorizontal = halfHeight / sinTheta;
        
            // 返回较小的距离（先碰到的边界）
            return Math.Min(distanceToVertical, distanceToHorizontal);
        }
        
        /// <summary>
        /// 计算激光是否与目标发生碰撞，并返回如果碰撞时激光的长度。（这个长度会略微比计算的大一些，防止因为激光缩短立刻脱离碰撞。）
        /// </summary>
        /// <param name="targetRect"></param>
        /// <param name="startPosition"></param>
        /// <param name="endPosition"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static bool GetAABBvLineCollisionPoint(Rectangle targetRect,Vector2 startPosition,Vector2 endPosition,float width,out float finalLength)
        {
            float point = 0f;
            float originLength = (endPosition - startPosition).Length();
            float hitLength = (targetRect.Center.ToVector2() - startPosition).Length();
            
            float length = GetRayDistanceToBorder(targetRect.Width, targetRect.Height, startPosition - endPosition);

            if (Collision.CheckAABBvLineCollision(targetRect.TopLeft(), targetRect.Size(), startPosition, endPosition,
                    width, ref point))
            {
                finalLength = hitLength - length * 0.8f;
                return true;
            }

            finalLength = originLength;
            return false; 
        }

        /// <summary>
        /// 判定范围内是否和液体发生碰撞。默认只和普通水判定(并且一定包含普通水判定，单独判定岩浆请使用Collision.LavaCollision)，可以额外包括蜂蜜，岩浆，微光
        /// </summary>
        /// <param name="position"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="includeHoney"></param>
        /// <param name="includeLava"></param>
        /// <param name="includeShimmer"></param>
        /// <returns></returns>
        public static bool WetCollision(Vector2 position, int width, int height, bool includeHoney = false,
            bool includeLava = false, bool includeShimmer = false)
        {
            //和任意液体有碰撞
            bool WetCol = Collision.WetCollision(position, width, height);

            if (includeHoney && includeLava && includeShimmer) return WetCol;

            //和岩浆有碰撞
            bool LavaCol = Collision.LavaCollision(position, width, height);

            if (!includeLava && LavaCol) return false;

            //和蜂蜜有碰撞
            bool HoneyCol = Collision.honey;

            if (!includeHoney && HoneyCol) return false;

            //和微光有碰撞
            bool ShimmerCol = Collision.shimmer;
            if (!includeShimmer && ShimmerCol) return false;
            
            return WetCol;
        }

        /// <summary>
        /// 检测激光与墙体是否发生碰撞，例如终极棱镜。
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="towards"></param>
        /// <param name="width"></param>
        /// <param name="length">此值会作为判定的参考，并且会被修改为一个无法碰撞墙体的长度</param>
        /// <param name="precisionNum">判定精度，默认为5</param>
        /// <returns></returns>
        public static bool LaserCollision(Vector2 startPosition,Vector2 towards,float width, ref float length,int precisionNum = 5)
        {
            towards = towards.SafeNormalize(towards);
            float[] samples = new float[precisionNum];
            Collision.LaserScan(startPosition, towards, width, length, samples);
            bool collide = false;
            
            for (int i = 1; i < precisionNum; i++)
            {
                if (length > samples[i])
                {
                    length = samples[i];
                    collide = true;
                }
            }

            return collide;
        }

        /// <summary>
        /// 割草，检测世界位置对应的物块并判定是否可以被破坏。
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <param name="netUpdate"></param>
        public static void CutTile(Vector2 targetPosition,bool netUpdate = false)
        {
            Tile tile = Framing.GetTileSafely(targetPosition);
            int tileX = (int)(targetPosition.X / 16f);
            int tileY = (int)(targetPosition.Y / 16f);
            if (Main.tileCut[tile.TileType] && WorldGen.CanCutTile(tileX,tileY, TileCuttingContext.AttackProjectile))
            {
                WorldGen.KillTile(tileX, tileY);
                if (Main.netMode != NetmodeID.SinglePlayer && netUpdate) 
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, tileX, tileY);
            }
        }
        
        /// <summary>
        /// 每帧更新，试图朝向目标位置追击
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="targetPos">锁定的追击位置</param>
        /// <param name="targetVelocity">目标速度，追击速度会朝着目标速度靠拢</param>
        /// <param name="strength"></param>
        public static void TraceTargetPosition(this Entity entity,Vector2 targetPos,float targetVelocity,float strength)
        {
            strength = MathHelper.Clamp(strength, 0.1f, 1);
            Vector2 targetVec = targetPos - entity.Center;
            targetVec.Normalize();
            // 目标向量是朝向目标的大小为20的向量
            targetVec *= targetVelocity;
            // 朝向npc的单位向量*20 + 3.33%偏移量
            entity.velocity = (entity.velocity*(1-strength) + targetVec * strength) / targetVelocity;
            //entity.velocity.Normalize();
            //entity.velocity *= 20f;
        }
        
        /// <summary>
        /// 每帧更新，试图朝向目标位置追击
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="targetPos">锁定的追击位置</param>
        /// <param name="targetVelocity">目标速度，追击速度会朝着目标速度靠拢</param>
        /// <param name="strength"></param>
        public static void TraceTargetPosition(this Item entity,Vector2 targetPos,float targetVelocity,float strength)
        {
            strength = MathHelper.Clamp(strength, 0.1f, 1);
            Vector2 targetVec = targetPos - entity.Center;
            targetVec.Normalize();
            // 目标向量是朝向目标的大小为20的向量
            targetVec *= targetVelocity;
            // 朝向npc的单位向量*20 + 3.33%偏移量
            entity.velocity = Vector2.Lerp(entity.velocity, targetVec, strength);
            
            
            //entity.velocity.Normalize();
            //entity.velocity *= 20f;
        }

        /// <summary>
        /// 摇晃屏幕
        /// </summary>
        /// <param name="shakeCenter"></param>
        /// <param name="shakeDirection"></param>
        /// <param name="shakeStrength"></param>
        /// <param name="shakeFrequency">振动频率，越高则抖动的越快，最多取fps/2，也就是当60帧时，此值最高为30</param>
        /// <param name="shakeTotalFrames"></param>
        public static void ShakeScreen(Vector2 shakeCenter,Vector2 shakeDirection,float shakeStrength = 10f,float shakeFrequency =10f,int shakeTotalFrames = 8)
        {
            shakeDirection = shakeDirection.SafeNormalize(shakeDirection);
            //if (shakeFrequency > shakeTotalFrames) shakeFrequency = shakeTotalFrames;
            
            // 创建屏幕震动效果
            PunchCameraModifier modifier = new PunchCameraModifier(shakeCenter, 
                shakeDirection,shakeStrength, shakeFrequency, shakeTotalFrames);
            Main.instance.CameraModifiers.Add(modifier);
        }
    }
}