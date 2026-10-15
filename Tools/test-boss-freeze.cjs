'use strict';
// Runs the real anchor helpers, buildup registry and cooldown code in a headless NPC host.
const fs=require('node:fs'),path=require('node:path'),os=require('node:os'),assert=require('node:assert/strict');
const {spawnSync}=require('node:child_process');
const root=path.resolve(__dirname,'..');
const scratch=fs.mkdtempSync(path.join(process.argv[2]||os.tmpdir(),'boss-freeze-'));
const read=p=>fs.readFileSync(path.join(root,p),'utf8');
function method(source,marker) {
 const start=source.indexOf(marker),brace=source.indexOf('{',start);assert(start>=0,marker);
 let end=brace+1,depth=1;for(;depth;end++){if(source[end]==='{')depth++;if(source[end]==='}')depth--;}
 return source.slice(start,end);
}
const ext=read('Utils/NPCExtentions.cs'),global=read('DamageSystem/ElementalGlobalNPC.cs');
const helpers=['public static NPC GetRealNpc(', 'public static NPC GetLogicNpc(', 'public static NPC GetElementAnchorNPC(',
 'public static int GetFreezeCooldown(', 'public static bool IsFreezeAccumulating(', 'public static void ReduceFreezeCooldown(',
 'public static void AddBuffToAnchorParts('].map(m=>method(ext,m)).join('\n');
const cooldown=global.split('#region 冻结冷却')[1].split('#endregion')[0];
const threshold=Number(read('DamageSystem/ElementBuildUpSystem.cs').match(/RegisterHitCount\([\s\S]*?\b240,\s*(\d+)f/)[1]);
const xml=s=>s.replaceAll('&','&amp;').replaceAll('"','&quot;');
fs.writeFileSync(path.join(scratch,'Tests.csproj'),`<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><Compile Include="${xml(path.join(root,'DamageSystem/ElementBuildupRegistry.cs'))}" Link="Registry.cs" /></ItemGroup></Project>`);
fs.writeFileSync(path.join(scratch,'Host.cs'),String.raw`
global using Terraria;
global using Terraria.ID;
global using KL.Extensions;
global using KL.DamageSystem;
global using static HostLog;
public static class HostLog {public static void Log(string s){} }
namespace Terraria.DataStructures {}
namespace KL.Utils {public static class KLMathF {public static float ClampLerp(float a,float b,float t)=>a+(b-a)*Math.Clamp(t,0,1);}}
namespace Terraria.ID {
 public static class NetmodeID {public const int MultiplayerClient=1;}
 public static class NPCID {
  public const int SkeletronHead=35,SkeletronHand=36,SkeletronPrime=127,PrimeCannon=128,PrimeSaw=129,PrimeVice=130,PrimeLaser=131;
  public const int Golem=245,GolemHead=246,GolemFistLeft=247,GolemFistRight=248,GolemHeadFree=249;
  public const int MoonLordCore=398,MoonLordHand=397,MoonLordHead=396,MartianSaucerCore=395,MartianSaucerTurret=393,MartianSaucerCannon=394,PirateShip=491,PirateShipCannon=492;
 }
}
namespace Terraria {
 public static class Main {public static NPC[] npc=Enumerable.Range(0,40).Select(i=>new NPC{whoAmI=i}).ToArray();public static int netMode;public static IEnumerable<NPC> ActiveNPCs=>npc.Where(n=>n.active);}
 public class NPC {
  public static int golemBoss=-1;
  public int whoAmI,type,realLife=-1,lifeMax=10000;public bool active;public float[] ai=new float[4];
  public bool[] buffImmune=new bool[20];public int FreezeTime;
  private readonly ElementalGlobalNPC data=new();
  public T GetGlobalNPC<T>() where T:class=>data as T;
  public void AddBuffFix(int id,int duration,bool quiet=false){if(!buffImmune[id]){FreezeTime=duration;data.Frozen=true;}}
  public void AddBuffToSelfAndChildren(int id,int duration,bool forcedAddBuff=false)=>throw new Exception("Ice must route to anchor parts");
  public struct HitInfo {public int SourceDamage;}
 }
}
namespace KL.Extensions {public static class NPCExtentions {
`+helpers+String.raw`
}}
namespace KL.DamageSystem {
 public enum ElementType {None,Ice}
 public class ElementalGlobalNPC {
  public bool Frozen,IsLogicFrozen;
  public bool GetAffliction(ElementType type)=>Frozen;
  public Dictionary<ElementType,BuildUpProgressContext> ElementAccumulation=new();
  public class BuildUpProgressContext {public float Current,Max,VisualTime;public int IdleResetTime,MaxDuration,IdleTime;}
`+cooldown+String.raw`
  public void Tick(NPC npc)=>UpdateFreezeCooldown(npc);
 }
}
`);
fs.writeFileSync(path.join(scratch,'Program.cs'),String.raw`
static void Check(bool b,string message){if(!b)throw new Exception(message);}
static NPC Spawn(int slot,int type){var n=new NPC{whoAmI=slot,type=type,active=true};Main.npc[slot]=n;return n;}
static void Reset(){for(int i=0;i<Main.npc.Length;i++)Main.npc[i]=new NPC{whoAmI=i};NPC.golemBoss=-1;Main.netMode=0;}
static float Progress(NPC n)=>n.GetGlobalNPC<ElementalGlobalNPC>().ElementAccumulation.TryGetValue(ElementType.Ice,out var p)?p.Current:0;
static bool Hit(NPC n)=>ElementBuildupBuffRegistry.TryApply(new(n,ElementType.Ice,new NPC.HitInfo{SourceDamage=1}));
const int threshold=THRESHOLD;
ElementBuildupBuffRegistry.RegisterHitCount(ElementType.Ice,1,240,threshold,1,c=>c.Target.IsFreezeAccumulating(),routeToAnchor:true);
var groups=new[]{
 ("Skeletron",NPCID.SkeletronHead,new[]{NPCID.SkeletronHand,NPCID.SkeletronHand},1),
 ("Prime",NPCID.SkeletronPrime,new[]{NPCID.PrimeCannon,NPCID.PrimeSaw,NPCID.PrimeVice,NPCID.PrimeLaser},1),
 ("Golem",NPCID.Golem,new[]{NPCID.GolemHead,NPCID.GolemFistLeft,NPCID.GolemFistRight,NPCID.GolemHeadFree},-1),
 ("Moon Lord",NPCID.MoonLordCore,new[]{NPCID.MoonLordHead,NPCID.MoonLordHand,NPCID.MoonLordHand},3)
};
foreach(var (name,bodyType,partTypes,link) in groups) {
 Reset();var body=Spawn(0,bodyType);if(link<0)NPC.golemBoss=0;
 var parts=partTypes.Select((type,i)=>Spawn(i+1,type)).ToArray();
 foreach(var p in parts){if(link>=0)p.ai[link]=body.whoAmI;p.buffImmune[1]=true;Check(p.GetElementAnchorNPC()==body,name+" anchor");}
 var unrelated=Spawn(20,999);var other=Spawn(21,bodyType);
 NPC otherPart=null;
 if(link>=0){otherPart=Spawn(22,partTypes[0]);otherPart.ai[link]=21;}
 for(int i=0;i<threshold-1;i++)Check(Hit(parts[i%parts.Length]),name+" accepts hit");
 Check(Progress(body)==threshold-1&&body.FreezeTime==0,name+" only body stores buildup");
 Check(parts.All(p=>Progress(p)==0&&p.FreezeTime==0),name+" no per-limb buildup");
 Check(Hit(parts[^1]),name+" trigger hit");
 Check(body.FreezeTime==240&&parts.All(p=>p.FreezeTime==240),name+" whole group frozen, including immune/free head");
 Check(unrelated.FreezeTime==0&&other.FreezeTime==0&&(otherPart==null||otherPart.FreezeTime==0),name+" no cross-boss freeze");
 Check(!Hit(parts[0]),name+" no buildup while frozen");
 body.GetGlobalNPC<ElementalGlobalNPC>().Tick(body);
 parts[0].ReduceFreezeCooldown(.5f);Check(body.GetFreezeCooldown()==0,name+" cannot precharge cooldown while frozen");
 foreach(var n in parts.Append(body)){n.FreezeTime=0;n.GetGlobalNPC<ElementalGlobalNPC>().Frozen=false;}
 body.GetGlobalNPC<ElementalGlobalNPC>().Tick(body);
 Check(body.GetFreezeCooldown()==3600&&parts.All(p=>p.GetFreezeCooldown()==3600),name+" shared cooldown begins at thaw");
 parts[0].ReduceFreezeCooldown(.5f);Check(body.GetFreezeCooldown()==3570&&parts.All(p=>p.GetFreezeCooldown()==3570),name+" water hit reduces body cooldown");
 Check(!Hit(parts[^1]),name+" no accumulation during cooldown");
 parts[^1].ReduceFreezeCooldown(60);Check(body.GetFreezeCooldown()==0&&Hit(parts[0])&&Progress(body)==1,name+" new shared cycle");
 Console.WriteLine("PASS "+name+": mixed limb hits -> shared buildup -> all parts frozen -> shared thaw cooldown");
}
// Destroying a limb must not discard the buildup already accumulated on the body.
Reset();var head=Spawn(0,NPCID.SkeletronHead);var left=Spawn(1,NPCID.SkeletronHand);var right=Spawn(2,NPCID.SkeletronHand);
for(int i=0;i<30;i++)Hit(left);left.active=false;
for(int i=30;i<threshold;i++)Hit(right);
Check(head.FreezeTime==240&&right.FreezeTime==240&&left.FreezeTime==0,"dead limb does not lose body progress or receive buff");
// Invalid, inactive or reused parent slots must not attach a limb to an unrelated NPC.
foreach(int parent in new[]{-1,40,5}) {
 Reset();var hand=Spawn(1,NPCID.SkeletronHand);hand.ai[1]=parent;
 if(parent==5)Spawn(5,NPCID.Golem);
 Check(hand.GetElementAnchorNPC()==hand,"invalid/wrong parent falls back to self");
}
Reset();var stale=Spawn(1,NPCID.PrimeSaw);stale.ai[1]=2;Main.npc[2].type=NPCID.SkeletronPrime;
Check(stale.GetElementAnchorNPC()==stale,"inactive parent rejected");
Reset();var fist=Spawn(1,NPCID.GolemFistLeft);NPC.golemBoss=39;
Check(fist.GetElementAnchorNPC()==fist,"inactive golem rejected");
Reset();var wormHead=Spawn(0,13);var segment=Spawn(1,14);segment.realLife=0;
Check(segment.GetElementAnchorNPC()==wormHead,"existing shared-life routing preserved");
Console.WriteLine("PASS limb death continuity, invalid parent isolation, existing worm routing");
`.replace('THRESHOLD',String(threshold)));
const run=spawnSync('dotnet',['run','--project',path.join(scratch,'Tests.csproj'),'-v:q'],{encoding:'utf8'});
process.stdout.write(run.stdout);process.stderr.write(run.stderr);if(run.status!==0)process.exit(run.status||1);
