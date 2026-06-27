using System.Diagnostics;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;

namespace KL.Utils;

public partial class TimeStopManager : ModSystem
{
    public static bool timeStop
    {
        get => _timeStopRequests.Count > 0;
    }

    //启用后，所有在时停中无法移动的物体变为黑白，可以移动的则正常绘制
    static bool _greyFilter = false;

    public static bool GreyEffect => timeStop && _greyFilter;

    protected static int _greyTime = 0;
    
    // 存储所有活动的时间停止请求及其剩余时间
    private static Dictionary<object, float> _timeStopRequests = new Dictionary<object, float>();
    private static double stopEffectTime;
   
    /// <summary>
    /// 请求时间停止
    /// </summary>
    /// <param name="requester"></param>
    /// <param name="duration">持续的帧数</param>
    public static void RequestTimeStop(object requester, int duration)
    {
        if (!_timeStopRequests.TryAdd(requester, duration))
        {
            // 如果已经存在请求，更新持续时间（取最大值）
            _timeStopRequests[requester] = Math.Max(_timeStopRequests[requester], duration);
        }
    }

    /// <summary>
    /// 请求时停期间所有物体变为黑白（非时停期间不生效）
    /// </summary>
    /// <param name="time"></param>
    public static void RequestGreyFilter(int time = 1)
    {
        _greyTime = time;
    }
    // 结束特定请求者的时间停止
    public static void EndTimeStop(object requester)
    {
        _timeStopRequests.Remove(requester);
    }

    public override void Load()
    {
        
        //时停on大全
        On_Rain.Update += On_Rain_Update;
        On_Main.DoUpdateInWorld += On_Main_DoUpdateInWorld;
        On_Cloud.UpdateClouds += On_Cloud_UpdateClouds;
        On_Main.DoUpdate_AnimateWaterfalls += On_Main_DoUpdate_AnimateWaterfalls;
        On_Main.DoUpdate_AnimateWalls += On_Main_DoUpdate_AnimateWalls;
        On_Main.AnimateTiles += On_Main_AnimateTiles;
        On_Main.DrawSurfaceBG += On_Main_DrawSurfaceBG;//背景云的位置
        On_SkyManager.Update += On_SkyManager_Update;//特殊背景
        
        //时停中moddust可以短暂行动
        //TimeStopDustList = new TimeStopDustInfo[Main.dust.Length];
        On_Dust.NewDust += On_Dust_NewDust;
        On_Dust.UpdateDust += On_Dust_UpdateDust;
        
        base.Load();
    }

    private void On_Rain_Update(On_Rain.orig_Update orig, Rain self)
    {
        if (timeStop) return;
        orig(self);
    }
    public static List<TimeStopDustInfo> StopDustList = new List<TimeStopDustInfo>(Main.dust.Length);
    private void On_Dust_UpdateDust(On_Dust.orig_UpdateDust orig)
    {
        if (timeStop)
        {

            //记录所有dust的时停信息，并标记不能行动的粒子
            foreach (var dustInfo in TimeStopDustList)
            {
                if (dustInfo.dustIndex >= 0 && dustInfo.dustIndex < Main.dust.Length)
                {
                    if(!Main.dust[dustInfo.dustIndex].active)continue;
                    
                    if(dustInfo.timeInStop>0)dustInfo.timeInStop--;
                    if(dustInfo.timeInStop <= 0)
                    {
                        StopDustList.Add(dustInfo);
                        Main.dust[dustInfo.dustIndex].active = false;
                    }
                }
            }
            orig();
            
            //还原不能行动的粒子
            //Main.NewText("StopDustList: "+StopDustList.Count);
            foreach (var dustInfo in StopDustList)
            {
                if(dustInfo.dustIndex>=0 && dustInfo.dustIndex<Main.dust.Length)
                {
                    Main.dust[dustInfo.dustIndex].active = true;
                }
            }
            //移除已经消失的dust
            for (int i = TimeStopDustList.Count - 1; i >= 0; i--)
            {
                var dustInfo = TimeStopDustList[i];
                if(dustInfo.dustIndex>=0 && dustInfo.dustIndex<Main.dust.Length)
                {
                    if (!Main.dust[dustInfo.dustIndex].active)
                    {
                        TimeStopDustList.RemoveAt(i);
                    }
                }
            }
            //Main.NewText("TimeStopTotalList: "+TimeStopDustList.Count);
        }
        else orig();
    }

    private void On_SkyManager_Update(On_SkyManager.orig_Update orig, SkyManager self, GameTime gameTime)
    {
        if (timeStop)
        {
            Main.worldEventUpdates = 0;
            Main.GlobalTimeWrappedHourly -= (float)(gameTime.TotalGameTime.TotalSeconds % 3600.0);
            return;
        }
        orig(self,gameTime);
    }

    private void On_Main_DrawSurfaceBG(On_Main.orig_DrawSurfaceBG orig, Main self)
    {
        if (timeStop)
        {
            float windSpeedCurrent = Main.windSpeedCurrent;
            Main.windSpeedCurrent = 0;
            orig(self);
            Main.windSpeedCurrent = windSpeedCurrent;

            return;
        }
        orig(self);
    }

    private void On_Main_AnimateTiles(On_Main.orig_AnimateTiles orig)
    {
        if (timeStop) return;
        orig();
    }

    private void On_Main_DoUpdate_AnimateWalls(On_Main.orig_DoUpdate_AnimateWalls orig)
    {
        if (timeStop) return;
        orig();
    }

    private void On_Main_DoUpdate_AnimateWaterfalls(On_Main.orig_DoUpdate_AnimateWaterfalls orig, Main self)
    {
        if (timeStop) return;
        orig(self);
    }

    private void On_Cloud_UpdateClouds(On_Cloud.orig_UpdateClouds orig)
    {
        if (timeStop)
        {
            for (int i = 0; i < 200; i++)
            {
                if (Main.cloud[i].active)
                {
                    float num = 0.13f;
                    float num5 = Main.screenPosition.X - Main.screenLastPosition.X;
                    Main.cloud[i].position.X -= num5 * num;
                }
            }
            return;
        }
        orig();
    }

    private void On_Main_DoUpdateInWorld(On_Main.orig_DoUpdateInWorld orig, Main self, Stopwatch sw)
    {
        if (!Main.gamePaused)
        {   
            //可以用于更新一些时停中需要更新的东西

        }
        List<object> toRemove = new List<object>();
        float deltaTime = 1;//嘻嘻，泰拉没有deltaTime，但我就是要写XD
        
        if(_greyTime>0)_greyTime--;
        if (_greyTime > 0)_greyFilter = true;
        else _greyFilter = false;
        
        // 更新所有时间停止请求的剩余时间
        foreach (var pair in _timeStopRequests)
        {
            float remaining = pair.Value - deltaTime;
            if (remaining <= 0)
            {
                toRemove.Add(pair.Key);
            }
            else
            {
                _timeStopRequests[pair.Key] = remaining;
            }
        }
        // 移除已经结束的时停请求
        foreach (var requester in toRemove)
        {
            _timeStopRequests.Remove(requester);
        }
        
        
        if (!timeStop)//正常
        {
            stopEffectTime = Main.timeForVisualEffects;
            orig(self, sw);
            return;
        }
        //Main.gamePaused = false;
        Main.timeForVisualEffects = stopEffectTime;
        UpdatePlayer();
        UpdateProjectile(self);
        UpdateItem();
        UpdateDust();
    }

    //static public mydust[] mDust;
    static public List<TimeStopDustInfo> TimeStopDustList = new List<TimeStopDustInfo>(6000);
    public class TimeStopDustInfo
    {
        public int dustIndex;
        public int timeInStop;

        public TimeStopDustInfo(int dustIndex, int timeInStop)
        {
            this.dustIndex = dustIndex;
            this.timeInStop = timeInStop;
        }
    }
    private static int On_Dust_NewDust(On_Dust.orig_NewDust orig, Vector2 Position, int Width, int Height, int Type, float SpeedX, float SpeedY, int Alpha, Color newColor, float Scale)
    {
        int dustId = orig(Position, Width, Height, Type, SpeedX,SpeedY, Alpha, newColor, Scale);
        if (dustId != 6000)
        {
            //mDust[dustId].timeInStop = Main.rand.Next(10,17);
            TimeStopDustList.Add(new TimeStopDustInfo(dustId,Main.rand.Next(10,17)));
        }

        return dustId;
    }
    private void UpdateDust()
    {
        SystemLoader.PreUpdateDusts();

        if (Main.ignoreErrors)
        {
            try
            {
                Dust.UpdateDust();
            }
            catch
            {
                for (int num5 = 0; num5 < 6000; num5++)
                {
                    Main.dust[num5] = new Dust();
                    Main.dust[num5].dustIndex = num5;
                }
            }
        }
        else
        {
            Dust.UpdateDust();
        }

        SystemLoader.PostUpdateDusts();
    }

    private void UpdateItem()
    {
        SystemLoader.PreUpdateItems();

        Item.numberOfNewItems = 0;
        for (int num4 = 0; num4 < 400; num4++)
        {
            if (Main.ignoreErrors)
            {
                try
                {
                    Main.item[num4].UpdateItem(num4);
                }
                catch
                {
                    Main.item[num4] = new Item();
                }
            }
            else
            {
                Main.item[num4].UpdateItem(num4);
            }
        }

        SystemLoader.PostUpdateItems();
    }

    private void UpdateProjectile(Main main)
    {
        SystemLoader.PreUpdateProjectiles();

        LockOnHelper.SetUP();
        Main.CurrentFrameFlags.HadAnActiveInteractibleProjectile = false;
        main.SpelunkerProjectileHelper.OnPreUpdateAllProjectiles();
        main.ChumBucketProjectileHelper.OnPreUpdateAllProjectiles();


        for (int n = 0; n < 1000; n++)
        {
            Main.ProjectileUpdateLoopIndex = n;
            bool canUpdate = true;

            if (Main.projectile[n].active)
                canUpdate = (Main.projectile[n].GetGlobalProjectile<TimeStopGlobalProjectile>().CanMoveInTimeStop());

            if (Main.ignoreErrors)
            {
                try
                {
                    if(canUpdate)
                        Main.projectile[n].Update(n);
                }
                catch
                {
                    Main.projectile[n] = new Projectile();
                }
            }
            else
            {
                if (canUpdate)
                    Main.projectile[n].Update(n);
            }
        }

        Main.ProjectileUpdateLoopIndex = -1;
        LockOnHelper.SetDOWN();
        SystemLoader.PostUpdateProjectiles();
    }

    private static void UpdatePlayer()
    {
        SystemLoader.PreUpdatePlayers();

        foreach (NPC npc in Main.npc)
        {
            if (npc.active)
            {
                for (int i = 0; i < npc.immune.Length; i++)
                {
                    if (npc.immune[i] > 0) npc.immune[i]--;
                }
            }
        }

        int num = 0;
        int num2 = 0;
        Main.sittingManager.ClearPlayerAnchors();
        Main.sleepingManager.ClearPlayerAnchors();
        for (int i = 0; i < 255; i++)
        {
            if (!Main.player[i].active)
                continue;

            try
            {
                var modplayer = Main.player[i].GetModPlayer<TimeStopPlayer>();
                if (modplayer.CanMoveInTimeStop)
                {
                    Main.player[i].Update(i);
                }

                if (Main.player[i].active)
                {
                    num++;
                    if (Main.player[i].sleeping.FullyFallenAsleep)
                        num2++;
                }
            }
            catch
            {
                if (!Main.ignoreErrors)
                    throw;
            }
        }

        Main.CurrentFrameFlags.ActivePlayersCount = num;
        Main.CurrentFrameFlags.SleepingPlayersCount = num2;
        if (Main.netMode != 2)
        {
            int num3 = Main.myPlayer;
            if (Main.player[num3].creativeGodMode)
            {
                Main.player[num3].statLife = Main.player[num3].statLifeMax2;
                Main.player[num3].statMana = Main.player[num3].statManaMax2;
                Main.player[num3].breath = Main.player[num3].breathMax;
            }
        }
        SystemLoader.PostUpdatePlayers();

        Type type = typeof(Main);
        FieldInfo fieldInfo = type.GetField("_gameUpdateCount", BindingFlags.NonPublic | BindingFlags.Static);
        if (fieldInfo != null)
        {
            uint updatedValue = (uint)fieldInfo.GetValue(null) + 1;
            fieldInfo.SetValue(null, updatedValue);
        }

        LockOnHelper.SetUP();
        Main.CurrentFrameFlags.HadAnActiveInteractibleProjectile = false;
    }

    
}
