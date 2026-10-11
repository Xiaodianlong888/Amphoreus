using Amphoreus.Projectiles;
using Amphoreus.Projectiles.XiLian_Projectiles.Xilian;
using Amphoreus.Projectiles.XiLian_Projectiles.XiLian;
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

        // 右键蓄力相关
        private int rightClickHoldTime = 0;
        private bool isRightClickHolding = false;
        private bool playedChargeSound = false;

        // 左键轨迹相关
        private Vector2? lastMousePosition = null;
        private bool wasLeftMousePressed = false;   // 记录上一帧左键状态

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
            Item.useTime = 5;
            Item.useAnimation = 5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.DamageType = DamageClass.Magic;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.noMelee = true;
            Item.noUseGraphic = false;
            Item.mana = 0;
            Item.shoot = ModContent.ProjectileType<QuillInkProjectile>();
            Item.shootSpeed = 12f;

            Item.staff[Type] = true;
            Item.UseSound = null;
        }

        public override void UpdateInventory(Player player)
        {
            if (!player.ItemAnimationActive || player.HeldItem != Item)
            {
                lastMousePosition = null;
            }
        }

        public override void HoldItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return;

            HandleRightClickCharging(player);
            HandleLeftClickPress(player);    // 左键按下时断开轨迹
            HandleLeftClickRelease(player);  // 左键松开时清理轨迹
        }

        // 左键按下：强制断开与新线条的连接
        private void HandleLeftClickPress(Player player)
        {
            bool leftPressed = Main.mouseLeft;
            if (!wasLeftMousePressed && leftPressed)
            {
                lastMousePosition = null;   // 清除上次位置，新笔画独立开始
            }
            wasLeftMousePressed = leftPressed;
        }

        // 左键松开：彻底清除轨迹，防止残留
        private void HandleLeftClickRelease(Player player)
        {
            bool leftPressed = Main.mouseLeft;
            if (wasLeftMousePressed && !leftPressed)
            {
                lastMousePosition = null;
            }
            // 注意：不要在这里更新 wasLeftMousePressed，由 Press 方法统一管理
        }

        private void HandleRightClickCharging(Player player)
        {
            bool rightMousePressed = Main.mouseRight && Main.mouseRightRelease;
            bool rightMouseHeld = Main.mouseRight && !Main.mouseRightRelease;
            bool rightMouseReleased = !Main.mouseRight && Main.mouseRightRelease;

            if (rightMousePressed)
            {
                if (!isRightClickHolding)
                {
                    isRightClickHolding = true;
                    rightClickHoldTime = 0;
                    playedChargeSound = false;
                    SoundEngine.PlaySound(SoundID.Item15.WithVolumeScale(0.3f), player.Center);
                    Main.NewText("开始蓄力...", 255, 200, 200);
                }
            }
            else if (rightMouseHeld && isRightClickHolding)
            {
                rightClickHoldTime++;
                if (rightClickHoldTime % 30 == 0)
                {
                    float seconds = rightClickHoldTime / 60f;
                    Main.NewText($"蓄力中: {seconds:F1}秒", 255, 220, 200);
                }
                if (rightClickHoldTime % 10 == 0)
                {
                    Vector2 dustPos = player.Center + new Vector2(Main.rand.Next(-20, 21), Main.rand.Next(-20, 21));
                    Dust dust = Dust.NewDustPerfect(dustPos, DustID.PinkFairy, Vector2.Zero, 0, default, 1f);
                    dust.noGravity = true;
                }
                if (rightClickHoldTime >= 180 && !playedChargeSound)
                {
                    SoundEngine.PlaySound(SoundID.Item43.WithVolumeScale(1.5f), player.Center);
                    playedChargeSound = true;
                    Main.NewText("蓄力完成！松开右键释放光圈", 255, 255, 200);
                }
            }
            else if (rightMouseReleased && isRightClickHolding)
            {
                if (rightClickHoldTime >= 180)
                {
                    Projectile.NewProjectile(
                        player.GetSource_ItemUse(Item),
                        player.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<ClearingAuraProjectile>(),
                        0, 0f, player.whoAmI
                    );
                    SoundEngine.PlaySound(SoundID.Item29.WithVolumeScale(0.7f), player.Center);
                    Main.NewText("释放清除光圈！", 255, 200, 255);
                }
                else if (rightClickHoldTime > 60)
                {
                    Main.NewText($"蓄力不足: {rightClickHoldTime / 60f:F1}秒 (需要3秒)", 255, 150, 150);
                }
                isRightClickHolding = false;
                rightClickHoldTime = 0;
                playedChargeSound = false;
            }

            if (player.HeldItem != Item)
            {
                isRightClickHolding = false;
                rightClickHoldTime = 0;
                playedChargeSound = false;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (isRightClickHolding)
            {
                float chargePercent = Math.Min(100f, (rightClickHoldTime / 180f) * 100f);
                float chargeSeconds = rightClickHoldTime / 60f;
                string statusText = chargePercent >= 100f
                    ? "✓ 蓄力完成！松开右键释放"
                    : $"蓄力中: {chargePercent:F0}% ({chargeSeconds:F1}/3.0秒)";
                tooltips.Add(new TooltipLine(Mod, "Charging", statusText));
            }
        }

        public override bool CanUseItem(Player player) => true;

        public override bool AltFunctionUse(Player player) => true;

        // 不使用 UseItem 重写，让 Terraria 默认处理动画

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 右键不发射
            if (player.altFunctionUse == 2)
                return false;

            // 只有左键实际按下时才生成弹幕（防止残余调用）
            if (!Main.mouseLeft)
                return false;

            Vector2 mouseWorld = Main.MouseWorld;

            if (lastMousePosition.HasValue)
            {
                Vector2 start = lastMousePosition.Value;
                Vector2 end = mouseWorld;
                float distance = Vector2.Distance(start, end);
                int segments = Math.Max(1, (int)(distance / 6f));

                for (int i = 0; i < segments; i++)
                {
                    float t = i / (float)segments;
                    Vector2 pos = Vector2.Lerp(start, end, t);
                    int proj = Projectile.NewProjectile(source, pos, Vector2.Zero,
                        ModContent.ProjectileType<QuillInkProjectile>(), damage, knockback, player.whoAmI);
                    if (i < segments - 1)
                    {
                        float nextT = (i + 1) / (float)segments;
                        Vector2 nextPos = Vector2.Lerp(start, end, nextT);
                        Vector2 dir = nextPos - pos;
                        if (dir != Vector2.Zero)
                            Main.projectile[proj].rotation = dir.ToRotation();
                    }
                }
            }
            else
            {
                Projectile.NewProjectile(source, mouseWorld, Vector2.Zero,
                    ModContent.ProjectileType<QuillInkProjectile>(), damage, knockback, player.whoAmI);
            }

            lastMousePosition = mouseWorld;

            float volume = MathHelper.Clamp(velocity.Length() / 20f, 0.3f, 1f);
            SoundEngine.PlaySound(SoundID.Item46.WithVolumeScale(volume), mouseWorld);

            return false;
        }

        public override void HoldItemFrame(Player player)
        {
            if (player.ItemAnimationActive)
                player.bodyFrame.Y = player.bodyFrame.Height * 3;
        }
    }
}