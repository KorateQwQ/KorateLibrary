using System.IO;
using System.Linq;
using KL.Utils.Net;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.IO;

namespace KL.Utils;

public class KLGameStateManager : KLModSystem
{
	/// <summary>
	/// 物品的类型，分为不指定类型，武器类型，材料类型
	/// </summary>
	public enum StateItemType
	{
		None,
		MeleeWeapon,
		MagicWeapon,
		RangedWeapon,
		SummonWeapon,
		Material
	}
    //NPC死亡委托,给予参与击杀的玩家奖励
    public delegate void NpcLootEvent(NPC self,List<int> killers,int realMaxHp);
    public static event NpcLootEvent OnBossLoot;

    private static bool _hooksRegistered;
    private bool _thisInstanceRegistered;
    
    #region BossChecklist
    // Origin Bosses
    public const float KingSlime = 1f;
    public const float EyeOfCthulhu = 2f;
    public const float EaterOfWorlds = 3f;
    public const float QueenBee = 4f;
    public const float Skeletron = 5f;
    public const float DeerClops = 6f;
    public const float WallOfFlesh = 7f;
    public const float QueenSlime = 8f;
    public const float TheTwins = 9f;
    public const float TheDestroyer = 10f;
    public const float SkeletronPrime = 11f;
    public const float Plantera = 12f;
    public const float Golem = 13f;
    public const float DukeFishron = 14f;
    public const float EmpressOfLight = 15f;//这里我去掉了双足翼龙16，因为他不是一个boss，奇怪的是，火星飞碟是boss。我说tml啥比有没有懂的。
    public const float LunaticCultist = 17f;
    public const float Moonlord = 18f;
    
    private static readonly Version BossChecklistAPIVersion = new Version(1, 6);
    
    public static bool HasBossCheckList{ get; private set; }
    
    public static int TotalBossAmount{ get; private set; }
    
    public class BossChecklistBossInfo
    {
        internal string key = "";
        internal string modSource = "";
        public LocalizedText displayName = null;

        public float progression = 0f;
        internal Func<bool> downed = () => false;

        public bool isBoss = false;
        internal bool isMiniboss = false;
        internal bool isEvent = false;

        internal List<int> npcIDs = new List<int>();
        internal Func<LocalizedText> spawnInfo = null;
        internal List<int> spawnItems = new List<int>();
        internal int treasureBag = 0;
        internal int relic = 0;
        internal List<DropRateInfo> dropRateInfo = new List<DropRateInfo>();
        internal List<int> loot = new List<int>();
        internal List<int> collectibles = new List<int>();
    }

    private class OrigBossChecklistBossInfo
    {
	    internal float progression = 0f;
	    internal Func<bool> downed = () => false;

	    public OrigBossChecklistBossInfo(float progression, Func<bool> downed)
	    {
		    this.progression = progression;
			this.downed = downed;
	    }
    }


    private static Dictionary<string, BossChecklistBossInfo> bossInfos = new Dictionary<string, BossChecklistBossInfo>();
    
    private static Dictionary<int, OrigBossChecklistBossInfo> origBossInfos = new Dictionary<int, OrigBossChecklistBossInfo>();

    public override void PostAddRecipes()
    {
        bossInfos.Clear();
        origBossInfos.Clear();
        
        if (ModLoader.TryGetMod("BossChecklist", out Mod bossChecklist) && bossChecklist.Version >= BossChecklistAPIVersion)
        {
	        object currentBossInfoResponse = bossChecklist.Call("GetBossInfoDictionary", Mod, BossChecklistAPIVersion.ToString());
	        
	        if (currentBossInfoResponse is Dictionary<string, Dictionary<string, object>> bossInfoList)
	        {
		        bossInfos = bossInfoList.ToDictionary(boss => boss.Key, boss => new BossChecklistBossInfo()
		        {
			        key = boss.Value.ContainsKey("key") ? boss.Value["key"] as string : "",
			        modSource = boss.Value.ContainsKey("modSource") ? boss.Value["modSource"] as string : "",
			        displayName = boss.Value.ContainsKey("displayName")
				        ? boss.Value["displayName"] as LocalizedText
				        : null,

			        progression = boss.Value.ContainsKey("progression")
				        ? Convert.ToSingle(boss.Value["progression"])
				        : 0f,
			        downed = boss.Value.ContainsKey("downed") ? boss.Value["downed"] as Func<bool> : () => false,

			        isBoss = boss.Value.ContainsKey("isBoss") ? Convert.ToBoolean(boss.Value["isBoss"]) : false,
			        isMiniboss = boss.Value.ContainsKey("isMiniboss")
				        ? Convert.ToBoolean(boss.Value["isMiniboss"])
				        : false,
			        isEvent = boss.Value.ContainsKey("isEvent") ? Convert.ToBoolean(boss.Value["isEvent"]) : false,

			        npcIDs = boss.Value.ContainsKey("npcIDs") ? boss.Value["npcIDs"] as List<int> : new List<int>(),
			        spawnInfo = boss.Value.ContainsKey("spawnInfo")
				        ? boss.Value["spawnInfo"] as Func<LocalizedText>
				        : null,
			        spawnItems = boss.Value.ContainsKey("spawnItems")
				        ? boss.Value["spawnItems"] as List<int>
				        : new List<int>(),
			        treasureBag =
				        boss.Value.ContainsKey("treasureBag") ? Convert.ToInt32(boss.Value["treasureBag"]) : 0,
			        relic = boss.Value.ContainsKey("relic") ? Convert.ToInt32(boss.Value["relic"]) : 0,
			        dropRateInfo = boss.Value.ContainsKey("dropRateInfo")
				        ? boss.Value["dropRateInfo"] as List<DropRateInfo>
				        : new List<DropRateInfo>(),
			        loot = boss.Value.ContainsKey("loot") ? boss.Value["loot"] as List<int> : new List<int>(),
			        collectibles = boss.Value.ContainsKey("collectibles")
				        ? boss.Value["collectibles"] as List<int>
				        : new List<int>(),
		        });

		        HasBossCheckList = true;
		        
		        //计算所有boss而非事件的数量
		        TotalBossAmount = bossInfos.Count(boss => boss.Value.isBoss);
	        }
        }
        else
        {
	        HasBossCheckList = false;
	        origBossInfos.Add(NPCID.KingSlime,new OrigBossChecklistBossInfo(KingSlime, () => NPC.downedSlimeKing));
	        origBossInfos.Add(NPCID.EyeofCthulhu, new OrigBossChecklistBossInfo(EyeOfCthulhu, () => NPC.downedBoss1));
	        //世界吞噬者和克脑属于同一阶段
	        origBossInfos.Add(NPCID.EaterofWorldsHead, new OrigBossChecklistBossInfo(EaterOfWorlds, () => NPC.downedBoss2));
	        origBossInfos.Add(NPCID.EaterofWorldsBody, new OrigBossChecklistBossInfo(EaterOfWorlds, () => NPC.downedBoss2));
	        origBossInfos.Add(NPCID.EaterofWorldsTail, new OrigBossChecklistBossInfo(EaterOfWorlds, () => NPC.downedBoss2));
	        origBossInfos.Add(NPCID.BrainofCthulhu, new OrigBossChecklistBossInfo(EaterOfWorlds, () => NPC.downedBoss2));

	        origBossInfos.Add(NPCID.QueenBee, new OrigBossChecklistBossInfo(QueenBee, () => NPC.downedQueenBee));
	        origBossInfos.Add(NPCID.SkeletronHead, new OrigBossChecklistBossInfo(Skeletron, () => NPC.downedBoss3));
	        origBossInfos.Add(NPCID.Deerclops, new OrigBossChecklistBossInfo(DeerClops, () => NPC.downedDeerclops));
	        origBossInfos.Add(NPCID.WallofFlesh, new OrigBossChecklistBossInfo(WallOfFlesh, () => Main.hardMode));
	        origBossInfos.Add(NPCID.QueenSlimeBoss, new OrigBossChecklistBossInfo(QueenSlime, () => NPC.downedQueenSlime));
	        origBossInfos.Add(NPCID.Retinazer, new OrigBossChecklistBossInfo(TheTwins, () => NPC.downedMechBoss2));
	        origBossInfos.Add(NPCID.Spazmatism, new OrigBossChecklistBossInfo(TheTwins, () => NPC.downedMechBoss2));
	        
	        origBossInfos.Add(NPCID.TheDestroyer, new OrigBossChecklistBossInfo(TheDestroyer, () => NPC.downedMechBoss1));
			origBossInfos.Add(NPCID.SkeletronPrime, new OrigBossChecklistBossInfo(SkeletronPrime, () => NPC.downedMechBoss3));
			origBossInfos.Add(NPCID.Plantera, new OrigBossChecklistBossInfo(Plantera, () => NPC.downedPlantBoss));
			origBossInfos.Add(NPCID.Golem, new OrigBossChecklistBossInfo(Golem, () => NPC.downedGolemBoss));
			origBossInfos.Add(NPCID.DukeFishron, new OrigBossChecklistBossInfo(DukeFishron, () => NPC.downedFishron));
			origBossInfos.Add(NPCID.HallowBoss, new OrigBossChecklistBossInfo(EmpressOfLight, () => NPC.downedEmpressOfLight));
			origBossInfos.Add(NPCID.CultistBoss, new OrigBossChecklistBossInfo(LunaticCultist, () => NPC.downedAncientCultist));
			
			origBossInfos.Add(NPCID.MoonLordCore, new OrigBossChecklistBossInfo(Moonlord, () => NPC.downedMoonlord));
			origBossInfos.Add(NPCID.MoonLordHead, new OrigBossChecklistBossInfo(Moonlord, () => NPC.downedMoonlord));

			TotalBossAmount = 17;
        }
        base.PostAddRecipes();
    }
    #endregion

    public override void ClearWorld()
    {
	    base.ClearWorld();
    }

    public override void Load()
    {
	    OnBossLoot += OnKillBoss;
	    
	    if (!_hooksRegistered)
	    {
		    On_NPC.NPCLoot_DropItems += On_NPCOnNPCLoot_DropItems;
		    On_NPC.OnGameEventClearedForTheFirstTime += On_NPCOnOnGameEventClearedForTheFirstTime;
		    _hooksRegistered = true;
		    _thisInstanceRegistered = true;
	    }
	    base.Load();
    }

    public override void Unload()
    {
	    OnBossLoot -= OnKillBoss;
	    
	    if (_thisInstanceRegistered)
	    {
		    On_NPC.NPCLoot_DropItems -= On_NPCOnNPCLoot_DropItems;
		    On_NPC.OnGameEventClearedForTheFirstTime -= On_NPCOnOnGameEventClearedForTheFirstTime;
		    
		    _thisInstanceRegistered = false;
		    _hooksRegistered = false;
	    }
	    base.Unload();
    }

    private void On_NPCOnOnGameEventClearedForTheFirstTime(On_NPC.orig_OnGameEventClearedForTheFirstTime orig, int gameEventId)
    {
	    orig(gameEventId);
    }

    public override void LoadWorldData(TagCompound tag)
    {
	    base.LoadWorldData(tag);
    }

    public override void SaveWorldData(TagCompound tag)
    {
	    base.SaveWorldData(tag);
    }

    public override void PostUpdatePlayers()
    {
	    /*if (Main.LocalPlayer.HeldItem != null &&!Main.LocalPlayer.HeldItem.IsAir)
	    {
		    Main.LocalPlayer.HeldItem.damage = 9999;
	    }*/
	    /*if (IsLeftClick())
	    {
		    foreach (var info in bossInfos)
		    {
			    if (info.Value.isBoss)
			    {
				    Log($"{info.Value.displayName} state: {info.Value.progression}");
			    }
		    }
	    }*/

	    if (IsLeftClick())
	    {
		    return;
		    Main.mouseLeftRelease = false;
		    float targetState = 18.6f;

		    //豆包豆包，帮我总结月总后所有boss对应的可用材料
		    foreach (var info in bossInfos)
		    {
			    if (info.Value.isBoss && info.Value.progression >18f)
			    {
				    int maxRare = 0;
				    Item resultItem = null;
				    foreach (var item in info.Value.loot)
				    {
					    if (ContentSamples.ItemsByType.TryGetValue(item, out Item itemSample))
					    {
						    if (itemSample.damage > 0&&maxRare<itemSample.rare)
						    {
							    maxRare = itemSample.rare;
							    resultItem = itemSample;
						    }
					    }

				    }

				    if (maxRare > 0&&resultItem!=null)
				    {
					    Log(info.Value.displayName +" 参照武器：" + resultItem.Name + " 稀有度：" + resultItem.rare);
					    FindItemsByRarity(maxRare, StateItemType.Material);
				    }
			    }
		    }
	    }
	    base.PostUpdatePlayers();
    }

    private void On_NPCOnNPCLoot_DropItems(On_NPC.orig_NPCLoot_DropItems orig, NPC self, Player closestPlayer)
    {
        orig(self, closestPlayer);
        if (self.boss)
        {
            List<int> killers = new List<int>(255);
            foreach (var player in Main.ActivePlayers)
            {
                if (self.playerInteraction[player.whoAmI])
                {
                    killers.Add(player.whoAmI);
                }
            }
            RPC("OnBossLoot", [self, killers,self.GetRealMaxHP()], KLNetModule.NetSendType.ServerToAll);
        }
    }
    
    /// <summary>
    /// 找到指定稀有度的物品
    /// </summary>
    /// <param name="targetRarity"></param>
    /// <returns></returns>
    public static List<int> FindItemsByRarity(int targetRarity,StateItemType itemType)
    {
	    List<int> result = new();

	    foreach ((int type, Item item) in ContentSamples.ItemsByType)
	    {
		    if (!item.IsAir && item.rare == targetRarity)
		    {
			    bool success = false;
			    if (itemType == StateItemType.None)
			    {
				    result.Add(type);
				    success = true;
			    }

			    if (itemType == StateItemType.MeleeWeapon&&item.damage>0&&item.DamageType==DamageClass.Melee)
			    {
				    result.Add(type);
				    success = true;
			    }
			    if (itemType == StateItemType.MagicWeapon&&item.damage>0&&item.DamageType==DamageClass.Magic)
			    {
				    result.Add(type);
				    success = true;
			    }
			    if (itemType == StateItemType.RangedWeapon&&item.damage>0&&item.DamageType==DamageClass.Ranged)
			    {
				    result.Add(type);
				    success = true;
			    }
			    if (itemType == StateItemType.SummonWeapon&&item.damage>0&&item.DamageType==DamageClass.Summon)
			    {
				    result.Add(type);
				    success = true;
			    }
			    if (itemType == StateItemType.Material&&item.material&&item.damage<=0&&item.createTile<0&&!item.consumable&&!item.accessory&&item.ammo==AmmoID.None
			        &&item.headSlot<0&&item.bodySlot<0&&item.legSlot<0&&item.dye<=0)
			    {
				    result.Add(type);
				    success = true;
			    }
			    if(success)
			    {
				    PrintText($"Item:{item.Name} Rare:{item.rare}");
				    Log($"Item:{item.Name} Rare:{item.rare}");
			    }
		    }
	    }

	    return result;
    }
    
    protected virtual void OnKillBoss(NPC self, List<int> killers, int realMaxHp)
    {
	    //PrintText($"OnKillBoss: {self.FullName} npcWhoAmI{self.whoAmI} Npc max hp: {realMaxHp}");
	    //PrintText($"Boss type:{self.type}: BossName: {self.FullName} state: {GetBossState(self)}");
    }
    
    ///获取此boss对应的进度，如果没有则返回-1
    public static float GetBossState(NPC boss)
    {
	    var bossInfo = bossInfos.Values.FirstOrDefault(info => info.npcIDs.Contains(boss.type));
	    if (bossInfo!=null) return bossInfo.progression;
	    
	    var origBossInfo = origBossInfos.GetValueOrDefault(boss.type);
	    if (origBossInfo != null) return origBossInfo.progression;
	    
	    return -1f;
    }
    
    ///获得当前世界击败的进度最高的boss对应的值，在原版最后一个boss月亮领主为18
    public static float GetWorldMaxDefeatedBossValue()
    {
	    if (bossInfos.Count == 0)
	    {
		    if (origBossInfos.Count == 0)
		    {
			    return 0;
		    }
		    var downedOrigBosses = origBossInfos.Values.Where(info => info.downed());
		    return downedOrigBosses.Any() ? downedOrigBosses.Max(info => info.progression) : 0;
	    }
	    
	    var downedBosses = bossInfos.Values.Where(info => info.downed() && info.isBoss) ;
	    
	    return downedBosses.Any() ? downedBosses.Max(info => info.progression) : 0;
    }
    
    ///获取当前世界可能击败的boss对应的值（不需要击败）
    public static float GetWorldMaxBossValue()
    {
	    if (bossInfos.Count == 0)
	    {
		    if (origBossInfos.Count == 0)
		    {
			    return 0;
		    }
		    return origBossInfos.Values.Max(info => info.progression);
	    }
		
	    // 获取当前世界中存在的所有boss（不包括小boss和事件）
	    var availableBosses = bossInfos.Values.Where(info => info.isBoss);
	    return availableBosses.Any() ? availableBosses.Max(info => info.progression) : 0;
    }

	
    public static bool IsBossDowned(NPC boss)
    {
	    if (bossInfos.Count == 0)
	    {
		    return origBossInfos.ContainsKey(boss.type) && origBossInfos[boss.type].downed();
	    }
	    var bossInfo = bossInfos.Values.FirstOrDefault(info => info.npcIDs.Contains(boss.type));
	    if (bossInfo!=null) return bossInfo.downed();

	    return false;
    }

    public static Dictionary<string, BossChecklistBossInfo> GetBossInfos()
    {
	    return bossInfos;
    }
    
    

    class GameStateNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        
        public bool AlreadyLooted = false;

        public override void OnKill(NPC npc)
        {
            base.OnKill(npc);
        }
    }
}