using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria.Audio; // 添加这个
using Amphoreus.Systems; // 添加这个
using Amphoreus.Players;
using Amphoreus.Players.Shenyupai_Players;
using Amphoreus.Systems.Shenyupai; // 添加这个

namespace Amphoreus.Items.XiLian_Items
{
    public class Shenyupai_Zu : ModItem
    {
        public override void SetStaticDefaults()
        {
            //DisplayName.SetDefault("神谕切换卷轴");
            //Tooltip.SetDefault("使用后随机切换到一个新的神谕牌效果\n无使用限制 - 测试专用\n'命运的涟漪在卷轴上荡漾'");
        }
        public override string Texture => "Amphoreus/Items/XiLian_Items/Shenyupai_Zu";
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 38;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 0, 1, 0); // 1银币
            Item.rare = ItemRarityID.Purple; // 紫色表示特殊/测试物品
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item8;
            Item.consumable = false;
            Item.noMelee = true;
        }

        public override bool CanUseItem(Player player)
        {
            return true; // 无限制使用
        }

        public override bool? UseItem(Player player)
        {
            var cardPlayer = player.GetModPlayer<Shenyupai_Player>(); // 使用完整的命名空间

            // 切换到新卡牌
            cardPlayer.SwitchToNewCard();

            if (cardPlayer.CurrentCard != null)
            {
                // 显示消息
                Main.NewText($"切换到：【{cardPlayer.CurrentCard.Name}】", 255, 215, 0); // 金色
                Main.NewText($"{cardPlayer.CurrentCard.Description}", 200, 255, 200);
                Main.NewText($"持续时间: 24分钟游戏时间", 150, 200, 255);

                // 特效
                for (int i = 0; i < 25; i++)
                {
                    Vector2 speed = Main.rand.NextVector2Circular(3f, 3f);
                    Dust.NewDustPerfect(
                        player.Center + new Vector2(0, -20),
                        DustID.PurpleTorch,
                        speed,
                        150,
                        default,
                        1.5f
                    );
                }

                // 音效
                SoundEngine.PlaySound(SoundID.Item8, player.Center);
            }

            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // 显示当前buff信息
            var player = Main.LocalPlayer;
            var cardPlayer = player.GetModPlayer<Shenyupai_Player>();

            if (cardPlayer.HasActiveBuff)
            {
                tooltips.Add(new TooltipLine(Mod, "CurrentCard",
                    $"当前神谕: {cardPlayer.CurrentCard.Name}"));
                tooltips.Add(new TooltipLine(Mod, "CurrentEffect",
                    $"效果: {cardPlayer.CurrentCard.Description}"));
                tooltips.Add(new TooltipLine(Mod, "TimeLeft",
                    $"剩余时间: {cardPlayer.BuffTimeLeftFormatted}"));
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "NoCard", "当前没有激活的神谕牌"));
            }

            // 添加说明
            tooltips.Add(new TooltipLine(Mod, "Usage", "右键: 随机切换神谕牌效果"));
            tooltips.Add(new TooltipLine(Mod, "TestNote", "测试物品 - 无使用限制"));
        }

        public override void AddRecipes()
        {
            // 简单配方，方便测试
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Book, 1);
            recipe.AddIngredient(ItemID.FallenStar, 1);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }

        public override void HoldItem(Player player)
        {
            // 显示当前激活的buff信息在屏幕上
            var cardPlayer = player.GetModPlayer<Shenyupai_Player>();

            if (cardPlayer.HasActiveBuff && Main.mouseRight && Main.mouseRightRelease)
            {
                // 右键切换可以切换到下一个特定的buff（循环）
                SwitchToNextSpecificCard(player);
            }
        }

        private void SwitchToNextSpecificCard(Player player)
        {
            var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
            var allCards = Shenyupai_System.GetAllCardNames();

            if (allCards.Count == 0) return;

            string currentCardName = cardPlayer.CurrentCard?.Name;
            int currentIndex = -1;

            if (!string.IsNullOrEmpty(currentCardName))
            {
                currentIndex = allCards.IndexOf(currentCardName);
            }

            // 循环到下一个卡牌
            int nextIndex = (currentIndex + 1) % allCards.Count;
            string nextCardName = allCards[nextIndex];

            // 切换到特定卡牌
            cardPlayer.SwitchToSpecificCard(nextCardName);

            // 显示消息
            Main.NewText($"切换到: 【{nextCardName}】", 255, 215, 0);
        }
    }
}