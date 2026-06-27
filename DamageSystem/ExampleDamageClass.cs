namespace KL.DamageSystem;

public class ExampleDamageClass : DamageClass
{
    // This is an example damage class designed to demonstrate all the current functionality of the feature and explain how to create one of your own, should you need one.
    // 这是一个示例伤害类，旨在展示该功能的所有当前功能，并解释如何创建自己的伤害类（如果需要的话）。
    // For information about how to apply stat bonuses to specific damage classes, please instead refer to ExampleMod/Content/Items/Accessories/ExampleStatBonusAccessory.
    // 有关如何将状态加成应用于特定伤害类的信息，请参考 ExampleMod/Content/Items/Accessories/ExampleStatBonusAccessory。
    public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
    {
        // This method lets you make your damage class benefit from other classes' stat bonuses by default, as well as universal stat bonuses.
        // 此方法允许你让伤害类默认受益于其他类的状态加成以及通用状态加成。
        // To briefly summarize the two nonstandard damage class names used by DamageClass:
        // 简要总结 DamageClass 使用的两个非标准伤害类名称：
        // Default is, you guessed it, the default damage class. It doesn't scale off of any class-specific stat bonuses or universal stat bonuses.
        // Default 是默认伤害类。它不继承任何特定类的状态加成或通用状态加成。
        // There are a number of items and projectiles that use this, such as thrown waters and the Bone Glove's bones.
        // 有许多物品和射弹使用此类，例如投掷水和骨手套的骨头。
        // Generic, on the other hand, scales off of all universal stat bonuses and nothing else; it's the base damage class upon which all others that aren't Default are built.
        // Generic 则继承所有通用状态加成，是除 Default 外所有其他伤害类的基础。
        if (damageClass == DamageClass.Generic)
            return StatInheritanceData.Full;

        return new StatInheritanceData(
            damageInheritance: 0f,
            critChanceInheritance: 0f,
            attackSpeedInheritance: 0f,
            armorPenInheritance: 0f,
            knockbackInheritance: 0f
        );
        // Now, what exactly did we just do, you might ask? Well, let's see here...
        // 现在，你可能会问我们刚才到底做了什么？让我们来看看...
        // StatInheritanceData is a struct which you'll need to return one of for any given outcome this method.
        // StatInheritanceData 是一个结构体，你需要为此方法的任何给定结果返回一个实例。
        // Normally, the latter of these two would be written as "StatInheritanceData.None", rather than being typed out by hand...
        // 通常，后一种情况会写成 "StatInheritanceData.None"，而不是手动输入...
        // ...but for the sake of clarity, we've written it out and labeled each parameter in order; they should be self-explanatory.
        // ...但为了清晰起见，我们将其写出并按顺序标记了每个参数；它们应该是自解释的。
        // To explain how these return values work, each one behaves like a percentage, with 0f being 0%, 1f being 100%, and so on.
        // 要解释这些返回值的工作原理，每个值都像一个百分比，0f 表示 0%，1f 表示 100%，依此类推。
        // The return value indicates how much your class will scale off of the stat in question for whatever damage class(es) you've returned it for.
        // 返回值表示你的类将从你为其返回的任何伤害类的相关状态中继承多少。
        // If you create a StatInheritanceData without any parameters, all of them will be set to 1f.
        // 如果你创建没有参数的 StatInheritanceData，所有值都将设置为 1f。
        // For example, if we propose a hypothetical alternate return for DamageClass.Ranged...
        // 例如，如果我们为 DamageClass.Ranged 提出一个假设的替代返回值...
        /*
        if (damageClass == DamageClass.Ranged)
            return new StatInheritanceData(
                damageInheritance: 1f,
                critChanceInheritance: -1f,
                attackSpeedInheritance: 0.4f,
                armorPenInheritance: 2.5f,
                knockbackInheritance: 0f
            );
        */
        // This would allow our custom class to benefit from the following ranged stat bonuses:
        // 这将允许我们的自定义类从以下远程状态加成中受益：
        // - Damage, at 100% effectiveness
        // - 伤害，100% 效果
        // - Attack speed, at 40% effectiveness
        // - 攻击速度，40% 效果
        // - Crit chance, at -100% effectiveness (this means anything that raises ranged crit chance specifically will lower the crit chance of our custom class by the same amount)
        // - 暴击几率，-100% 效果（这意味着任何专门提高远程暴击几率的东西都会以相同数量降低我们自定义类的暴击几率）
        // - Armor penetration, at 250% effectiveness
        // - 护甲穿透，250% 效果

        // CAUTION: There is no hardcap on what you can set these to. Please be aware and advised that whatever you set them to may have unintended consequences,
        // 注意：你可以设置的数值没有硬性上限。请注意，你设置的任何值都可能产生意想不到的后果，
        // and that we are NOT responsible for any temporary or permanent damage caused to you, your character, or your world as a result of your morbid curiosity.
        // 我们不对因你的病态好奇心而对你、你的角色或你的世界造成的任何暂时或永久损害负责。
        // To refer to a non-vanilla damage class for these sorts of things, use "ModContent.GetInstance<TargetDamageClassHere>()" instead of "DamageClass.XYZ".
        // 要引用非原版伤害类进行此类操作，请使用 "ModContent.GetInstance<TargetDamageClassHere>()" 而不是 "DamageClass.XYZ"。
    }

    public override bool GetEffectInheritance(DamageClass damageClass)
    {
        // This method allows you to make your damage class benefit from and be able to activate other classes' effects (e.g. Spectre bolts, Magma Stone) based on what returns true.
        // 此方法允许你让伤害类受益于并能够激活其他类的效果（例如幽灵螺栓、岩浆石），基于返回 true 的内容。
        // Note that unlike our stat inheritance methods up above, you do not need to account for universal bonuses in this method.
        // 请注意，与上面的状态继承方法不同，你不需要在此方法中考虑通用加成。
        // For this example, we'll make our class able to activate melee- and magic-specifically effects.
        // 对于此示例，我们将使我们的类能够激活近战和魔法特定的效果。
        if (damageClass == DamageClass.Melee)
            return true;
        if (damageClass == DamageClass.Magic)
            return true;

        return false;
    }

    public override void SetDefaultStats(Player player)
    {
        // This method lets you set default statistical modifiers for your example damage class.
        // 此方法允许你为示例伤害类设置默认的统计修饰符。
        // Here, we'll make our example damage class have more critical strike chance and armor penetration than normal.
        // 在这里，我们将使示例伤害类比正常情况下具有更高的暴击几率和护甲穿透。
        player.GetCritChance<ExampleDamageClass>() += 4;
        player.GetArmorPenetration<ExampleDamageClass>() += 10;
        // These sorts of modifiers also exist for damage (GetDamage), knockback (GetKnockback), and attack speed (GetAttackSpeed).
        // 这些类型的修饰符也存在于伤害（GetDamage）、击退（GetKnockback）和攻击速度（GetAttackSpeed）中。
        // You'll see these used all around in reference to vanilla classes and our example class here. Familiarize yourself with them.
        // 你会在原版类和我们的示例类中到处看到这些用法。熟悉它们。
    }

    // This property lets you decide whether or not your damage class can use standard critical strike calculations.
    // 此属性允许你决定伤害类是否可以使用标准暴击计算。
    // Note that setting it to false will also prevent the critical strike chance tooltip line from being shown.
    // 请注意，将其设置为 false 也会阻止显示暴击几率工具提示行。
    // This prevention will overrule anything set by ShowStatTooltipLine, so be careful!
    // 此阻止将覆盖 ShowStatTooltipLine 设置的任何内容，所以要小心！
    public override bool UseStandardCritCalcs => true;

    public override bool ShowStatTooltipLine(Player player, string lineName)
    {
        // This method lets you prevent certain common statistical tooltip lines from appearing on items associated with this DamageClass.
        // 此方法允许你阻止某些常见的统计工具提示行出现在与此 DamageClass 关联的物品上。
        // The four line names you can use are "Damage", "CritChance", "Speed", and "Knockback". All four cases default to true, and thus will be shown. For example...
        // 你可以使用的四个行名称是 "Damage"、"CritChance"、"Speed" 和 "Knockback"。所有四种情况默认为 true，因此将显示。例如...
        if (lineName == "Speed")
            return false;

        return true;
        // PLEASE BE AWARE that this hook will NOT be here forever; only until an upcoming revamp to tooltips as a whole comes around.
        // 请注意，此钩子不会永远存在；只会在即将到来的整个工具提示重做之前存在。
        // Once this happens, a better, more versatile explanation of how to pull this off will be showcased, and this hook will be removed.
        // 一旦发生这种情况，将展示更好、更通用的解释如何实现此功能，并且此钩子将被移除。
    }
}