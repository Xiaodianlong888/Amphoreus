using Amphoreus.NPCs.Boss.SFeir_Boss;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Items.SFeir_Items
{
    public class SFeirSummonItem : ModItem
    {
        public override string Texture => "Amphoreus/Items/SFeir_Items/SFeirSummonItem";
        public override void SetStaticDefaults()
        {
            // 新版本使用Localization键，这里用ModifyTooltips代替
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 20;
            Item.value = 100;
            Item.rare = ItemRarityID.Blue;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
        }

        // Items/SFeir_Items/SFeirSummonItem.cs
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                int type = ModContent.NPCType<SFeir>();

                // 计算安全的生成位置（玩家上方150像素）
                Vector2 spawnPos = player.Center - new Vector2(0, 150);

                // 确保生成位置在有效空间
                int tileX = (int)(spawnPos.X / 16);
                int tileY = (int)(spawnPos.Y / 16);

                // 向上查找直到找到空位
                while (tileY > 10 && (WorldGen.SolidTile(tileX, tileY) || Main.tile[tileX, tileY].HasTile))
                {
                    tileY--;
                    spawnPos.Y -= 16;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // 使用NewNPC直接生成，获得更多控制
                    int npcIndex = NPC.NewNPC(
                        NPC.GetSource_NaturalSpawn(),
                        (int)spawnPos.X,
                        (int)spawnPos.Y,
                        type
                    );

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
                    }
                }
                else
                {
                    // 客户端发送请求（使用正确的MessageID数字）
                    NetMessage.SendData(4, -1, -1, null, player.whoAmI, type);
                }

                // 显示召唤信息
                if (Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("赛飞儿：游戏开始咯~", Color.Magenta);
                }
            }
            return true;
        }

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "SFeirSummon", "召唤赛飞儿进行测试"));
            tooltips.Add(new TooltipLine(Mod, "SFeirSummon2", "小心她的谎言！"));
        }
    }
}