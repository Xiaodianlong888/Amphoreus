using Amphoreus.Projectiles;
using Amphoreus.Projectiles.XiLian_Projectiles.Xilian;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Items.XiLian_Items
{
    public class RemembrancersQuill : ModItem
    {
        public override string Texture => "Amphoreus/Items/XiLian_Items/RemembrancersQuill";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = Item.sellPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;

            Item.damage = 1;
            Item.knockBack = 0f;
            Item.useTime = 8;       // 无人机发射间隔
            Item.useAnimation = 8;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.DamageType = DamageClass.Magic;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.noMelee = true;
            Item.noUseGraphic = false;
            Item.mana = 0;          // 手动扣魔
            Item.shoot = ModContent.ProjectileType<QuillInkProjectile>();
            Item.shootSpeed = 15f;

            Item.staff[Type] = true; // 贴图握把居中、笔尖朝右，自动指向鼠标
            Item.UseSound = null;
        }

        public override void HoldItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return;

            // 只处理右键橡皮擦；持握位置/角度交给原版 staff 逻辑自动处理
            HandleEraser(player);
        }

        // 右键：橡皮擦
        private void HandleEraser(Player player)
        {
            if (!(Main.mouseRight && Main.mouseRightRelease))
                return;

            int eraserType = ModContent.ProjectileType<EraserProjectile>();

            // 避免重复生成
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == eraserType)
                    return;
            }

            Projectile.NewProjectile(
                player.GetSource_ItemUse(Item),
                Main.MouseWorld,
                Vector2.Zero,
                eraserType,
                0, 0f, player.whoAmI);

            SoundEngine.PlaySound(SoundID.Item15.WithVolumeScale(0.4f), player.Center);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "InkCost", "左键：发射一枚墨水，命中位置后停留（消耗 1 魔力）"));
            tooltips.Add(new TooltipLine(Mod, "EraserCost", "右键：橡皮擦，清除弹幕时按该弹幕伤害消耗魔力"));
        }

        public override bool CanUseItem(Player player) => true;
        public override bool AltFunctionUse(Player player) => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 右键不发射
            if (player.altFunctionUse == 2)
                return false;

            // 仅左键实际按下时发射
            if (!Main.mouseLeft)
                return false;

            // 左键每次消耗 1 魔力
            if (!player.CheckMana(1, true))
                return false;

            // 目标位置：鼠标世界坐标
            Vector2 targetPos = Main.MouseWorld;

            // 发射起点：法杖尖端
            Vector2 startPos = player.Center + new Vector2(player.direction * 24f, -14f);

            // 计算朝目标的速度
            Vector2 toTarget = targetPos - startPos;
            float distance = toTarget.Length();
            if (distance < 1f) distance = 1f;
            Vector2 shootVelocity = toTarget / distance * Item.shootSpeed;

            // 生成墨水弹幕，目标位置通过 ai[0], ai[1] 传入
            Projectile.NewProjectile(
                source,
                startPos,
                shootVelocity,
                ModContent.ProjectileType<QuillInkProjectile>(),
                damage,
                knockback,
                player.whoAmI,
                targetPos.X,   // ai[0]
                targetPos.Y    // ai[1]
            );

            SoundEngine.PlaySound(SoundID.Item46.WithVolumeScale(0.4f), startPos);
            return false;
        }

        public override void HoldItemFrame(Player player)
        {
            if (player.ItemAnimationActive)
                player.bodyFrame.Y = player.bodyFrame.Height * 3;
        }
    }
}