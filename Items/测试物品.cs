using System.IO;
using KL.Configs;
using KL.DamageSystem;
using KL.DamageSystem.ElementalDamageClass;
using KL.Drawing;
using KL.Projectiles;
using KL.SkillSystem;
using KL.Utils;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace KL.Items
{

    public class 测试物品 : ModItem
    {
        public override void NetSend(BinaryWriter writer)
        {
            base.NetSend(writer);
        }

        public override void SetDefaults()
        {
            Item.noMelee = false;
            Item.width = 60;
            Item.height = 60;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = false;
            Item.healMana = 0;
            Item.rare = 6;
            Item.damage = 10;

        }
        
        public override void UseItemFrame(Player player)
        {
            Item.DamageType = ModContent.GetInstance<WaterDamage>();
            Item.crit = 10;
            Item.autoReuse = false;
            if (player.ItemAnimationJustStarted)
            {
                Projectile.NewProjectile(null, Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<DrawTestProj>(), 0,
                    0);
            }

        }
        public override void AddRecipes()
        {
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale,
            int whoAmI)
        {
            
            return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
        }
    }
}