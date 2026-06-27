using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace KL.Drawing;

public class PlayerEffectHelper : ModSystem
{
    public static Asset<Effect> PlayerEffectTemplate = null!;
    
    /// <summary>
    /// 故障特效
    /// </summary>
    public static Asset<Effect> FaultEffect = null!;

    /// <summary>
    /// 存储所有玩家特效的映射ID
    /// </summary>
    public static Dictionary<Asset<Effect>, int> PlayerEffectMap = new();

    #region Register Player Effect

    int _nextDummyItemType = -1;

    Dictionary<int, int>? TryGetArmorShaderLookupDictionary()
    {
        FieldInfo? field = typeof(ArmorShaderDataSet).GetField("_shaderLookupDictionary",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        return field?.GetValue(GameShaders.Armor) as Dictionary<int, int>;
    }

    List<ArmorShaderData>? TryGetArmorShaderDataList()
    {
        FieldInfo? field = typeof(ArmorShaderDataSet).GetField("_shaderData",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        return field?.GetValue(GameShaders.Armor) as List<ArmorShaderData>;
    }

    int AllocateDummyItemTypeAvoidCollision()
    {
        Dictionary<int, int>? dict = TryGetArmorShaderLookupDictionary();

        int candidate = _nextDummyItemType;
        if (dict != null)
        {
            while (candidate < 0 && dict.ContainsKey(candidate))
                candidate--;
        }

        _nextDummyItemType = candidate - 1;
        return candidate;
    }

    public override void Load()
    {
        if (Main.dedServ)
            return;

        AutoLoadPlayerEffet();
    }

    public override void Unload()
    {
        PlayerEffectMap.Clear();
        _nextDummyItemType = -1;
    }

    //自动加载所有声明的effect
    void AutoLoadPlayerEffet()
    {
        // 只获取Effect类型的字段
        FieldInfo[] effectFields = typeof(PlayerEffectHelper)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(Asset<Effect>))
            .ToArray();

        foreach (FieldInfo field in effectFields)
        {
            string fieldName = field.Name;
            // 去掉开头的斜杠
            if (fieldName.StartsWith("_"))
                fieldName = fieldName.Substring(1);

            // 首字母大写
            if (fieldName.Length > 0)
                fieldName = char.ToUpper(fieldName[0]) + fieldName.Substring(1);

            // 使用EffectExtentions.LoadEffect加载特效
            Asset<Effect> effect = EffectExtentions.LoadPlayerEffect(fieldName);

            // 设置字段值
            field.SetValue(null, effect);

            var data = new ArmorShaderData(effect, "Pass1");

            int dummyItemType = AllocateDummyItemTypeAvoidCollision();
            GameShaders.Armor.BindShader(dummyItemType, data);

            int shaderId = GameShaders.Armor.GetShaderIdFromItemId(dummyItemType);
            PlayerEffectMap[effect] = shaderId;
        }
    }

    private readonly struct DyeBackup
    {
        private readonly int cHead;
        private readonly int cBody;
        private readonly int cLegs;
        private readonly int cHandOn;
        private readonly int cHandOff;
        private readonly int cWings;
        private readonly int cBack;
        private readonly int cFront;
        private readonly int cShoe;
        private readonly int cWaist;
        private readonly int cNeck;
        private readonly int cShield;
        private readonly int cBalloon;
        private readonly int cBalloonFront;
        private readonly int cFace;
        private readonly int cFaceHead;
        private readonly int cBeard;
        private readonly int cPet;
        private readonly int cLight;

        private DyeBackup(Player p)
        {
            cHead = p.cHead;
            cBody = p.cBody;
            cLegs = p.cLegs;
            cHandOn = p.cHandOn;
            cHandOff = p.cHandOff;
            cWings = p.cWings;
            cBack = p.cBack;
            cFront = p.cFront;
            cShoe = p.cShoe;
            cWaist = p.cWaist;
            cNeck = p.cNeck;
            cShield = p.cShield;
            cBalloon = p.cBalloon;
            cBalloonFront = p.cBalloonFront;
            cFace = p.cFace;
            cFaceHead = p.cFaceHead;
            cBeard = p.cBeard;
            //cPet = p.cPet;
            cLight = p.cLight;
        }

        public static DyeBackup From(Player p) => new(p);

        public void ApplyAll(Player p, int shaderId)
        {
            p.cHead = shaderId;
            p.cBody = shaderId;
            p.cLegs = shaderId;
            p.cHandOn = shaderId;
            p.cHandOff = shaderId;
            p.cWings = shaderId;
            p.cBack = shaderId;
            p.cFront = shaderId;
            p.cShoe = shaderId;
            p.cWaist = shaderId;
            p.cNeck = shaderId;
            p.cShield = shaderId;
            p.cBalloon = shaderId;
            p.cBalloonFront = shaderId;
            p.cFace = shaderId;
            p.cFaceHead = shaderId;
            p.cBeard = shaderId;
            //p.cPet = shaderId;
            p.cLight = shaderId;
        }

        public void Restore(Player p)
        {
            p.cHead = cHead;
            p.cBody = cBody;
            p.cLegs = cLegs;
            p.cHandOn = cHandOn;
            p.cHandOff = cHandOff;
            p.cWings = cWings;
            p.cBack = cBack;
            p.cFront = cFront;
            p.cShoe = cShoe;
            p.cWaist = cWaist;
            p.cNeck = cNeck;
            p.cShield = cShield;
            p.cBalloon = cBalloon;
            p.cBalloonFront = cBalloonFront;
            p.cFace = cFace;
            p.cFaceHead = cFaceHead;
            p.cBeard = cBeard;
            //p.cPet = cPet;
            p.cLight = cLight;
        }
    }

    #endregion

    public int GetPlayerEffectId(Asset<Effect> effect)
    {
        return PlayerEffectMap[effect];
    }

    public static void DrawPlayerWithEffect(Camera camera, Player player, Asset<Effect> effect, Vector2 pos,float rotation,Vector2 rotationOrigin, float shadow = 0f, float scale = 1f)
    {
        int shaderId = effect.GetPlayerEffectID();
        if (shaderId != 0)
        {
            var backup = DyeBackup.From(player);
            try
            {
                backup.ApplyAll(player, shaderId);
                Main.PlayerRenderer.DrawPlayer(camera, player, pos, rotation, rotationOrigin, shadow,scale);
            }
            finally
            {
                backup.Restore(player);
            }
        }
        else
        {
            Main.PlayerRenderer.DrawPlayer(camera, player, pos, rotation, rotationOrigin, shadow,scale);
        }

    }

    public static void ApplyEffect(Player player, Asset<Effect> effect)
    {
        int shaderId = effect.GetPlayerEffectID();
        if (shaderId != 0)
        {
            var backup = DyeBackup.From(player);
            backup.ApplyAll(player, shaderId);
        }
    }
    void DebugEffectAmount()
    {
        int shaderDataCount = TryGetArmorShaderDataList()?.Count ?? -1;
        int lookupCount = TryGetArmorShaderLookupDictionary()?.Count ?? -1;
        PrintText($"ArmorShaderDataSet: _shaderData.Count={shaderDataCount}, _shaderLookupDictionary.Count={lookupCount}");
    }

    class DrawTestPlayer : ModPlayer
    {
        public override void DrawPlayer(Camera camera)
        {
            //PlayerEffectHelper.FaultEffect.Value.SetValue("iTime", Main.GameUpdateCount % 1200 * 0.02f);
            //PlayerEffectHelper.ApplyEffect(Main.LocalPlayer, PlayerEffectHelper.FaultEffect);
            base.DrawPlayer(camera);
        }
    }
}