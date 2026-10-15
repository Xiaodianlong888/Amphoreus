namespace Amphoreus.Items.CeShi
{
//using Amphoreus.NPCs.CeShi;
//using Microsoft.Xna.Framework;
//using Terraria;
//using Terraria.Audio;
//using Terraria.DataStructures;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace Amphoreus.Items.CeShi
//{
//    public class TestSummoner : ModItem
//    {
//        public override void SetStaticDefaults()
//        {

//        }

//        public override void SetDefaults()
//        {
//            Item.width = 32;
//            Item.height = 32;
//            Item.useTime = 20;
//            Item.useAnimation = 20;
//            Item.useStyle = ItemUseStyleID.HoldUp;
//            Item.noMelee = true;
//            Item.knockBack = 0f;
//            Item.value = Item.buyPrice(0, 1, 0, 0);
//            Item.rare = ItemRarityID.Blue;
//            Item.autoReuse = false;
//            Item.noUseGraphic = false;
//            Item.UseSound = null; // 在CanUseItem中动态设置
//        }

//        public override bool AltFunctionUse(Player player)
//        {
//            return true; // 允许右键使用
//        }

//        public override bool CanUseItem(Player player)
//        {
//            if (player.altFunctionUse == 2) // 右键
//            {
//                Item.UseSound = SoundID.Item8; // 召唤音效

//                // 检查是否已经有蠕虫存在
//                bool wormExists = false;
//                for (int i = 0; i < Main.maxNPCs; i++)
//                {
//                    if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<NPCs.CeShi.WormHead>())
//                    {
//                        wormExists = true;
//                        break;
//                    }
//                }

//                if (wormExists)
//                {
//                    Main.NewText("已经存在一个测试蠕虫！", Color.Red);
//                    return false;
//                }

//                return true;
//            }
//            else // 左键
//            {
//                Item.UseSound = SoundID.Item4; // 信息音效
//                return true;
//            }
//        }

//        public override bool? UseItem(Player player)
//        {
//            if (player.altFunctionUse == 2) // 右键：生成蠕虫首领
//            {
//                Vector2 spawnPosition = Main.MouseWorld;

//                // 确保生成位置在屏幕内
//                if (spawnPosition.X < 100) spawnPosition.X = 100;
//                if (spawnPosition.Y < 100) spawnPosition.Y = 100;
//                if (spawnPosition.X > Main.maxTilesX * 16 - 100) spawnPosition.X = Main.maxTilesX * 16 - 100;
//                if (spawnPosition.Y > Main.maxTilesY * 16 - 100) spawnPosition.Y = Main.maxTilesY * 16 - 100;

//                // 生成蠕虫头部
//                int npcType = ModContent.NPCType<NPCs.CeShi.WormHead>();

//                int npcIndex = NPC.NewNPC(
//                    new EntitySource_ItemUse(player, Item),
//                    (int)spawnPosition.X,
//                    (int)spawnPosition.Y,
//                    npcType
//                );

//                if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
//                {
//                    NPC npc = Main.npc[npcIndex];
//                    npc.target = player.whoAmI;
//                    npc.netUpdate = true;

//                    // 生成召唤特效
//                    for (int i = 0; i < 30; i++)
//                    {
//                        Dust summonDust = Dust.NewDustPerfect(
//                            spawnPosition,
//                            DustID.GreenTorch,
//                            Main.rand.NextVector2Circular(5f, 5f),
//                            150,
//                            Color.Green,
//                            1.5f
//                        );
//                        summonDust.noGravity = true;
//                    }


//                    // 召唤音效
//                    SoundEngine.PlaySound(SoundID.Roar, spawnPosition);

//                    // 显示召唤信息
//                    if (Main.netMode == NetmodeID.SinglePlayer)
//                    {
//                        Main.NewText("测试蠕虫首领已苏醒！", Color.Green);
//                    }
//                }
//            }
//            else // 左键：显示信息
//            {
//                Main.NewText($"=== 测试蠕虫首领信息 ===", Color.Green);
//                Main.NewText($"特性：", Color.LightGreen);
//                Main.NewText($"  - 共享血条系统（总血量: 10000）", Color.LightGreen);
//                Main.NewText($"  - 免疫击退", Color.LightGreen);

//                Main.NewText($"  - 多体节协同AI", Color.LightGreen);
//                Main.NewText($"  - 体节可受到伤害（伤害传递）", Color.LightGreen);
//                Main.NewText($"右键在鼠标位置召唤", Color.Yellow);
//                Main.NewText($"注意：蠕虫体节不会单独死亡", Color.Orange);
//            }

//            return true;
//        }

//        public override void AddRecipes()
//        {
//            // 简单配方，方便测试
//            CreateRecipe()
//                .AddIngredient(ItemID.Wood, 10)
//                .AddTile(TileID.WorkBenches)
//                .Register();
//        }

//        public override Color? GetAlpha(Color lightColor)
//        {
//            // 物品发光效果
//            return Color.Lerp(lightColor, Color.Green, 0.3f);
//        }
//    }
//}
}
