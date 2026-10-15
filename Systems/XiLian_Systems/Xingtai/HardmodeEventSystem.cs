namespace Amphoreus.Systems.XiLian_Systems.Xingtai
{
//using Microsoft.Xna.Framework;
//using Terraria;
//using Terraria.ModLoader;
//using Terraria.ID;
//using Terraria.Chat;
//using Terraria.Localization;

//namespace Amphoreus.Systems.XiLian_Systems.Xingtai
//{
//    public class HardmodeEventSystem : ModSystem
//    {
//        private bool wasHardmode = false;
//        private bool hasPerformedDemonstration = false; // 标记是否已执行演示转换

//        public override void OnWorldLoad()
//        {
//            // 世界加载时检查一次
//            if (Main.hardMode)
//            {
//                wasHardmode = true;
//                hasPerformedDemonstration = true; // 如果已经是困难模式，标记为已执行
//            }
//        }

//        public override void PostUpdateWorld()
//        {
//            // 只在状态变化时检测一次
//            if (!wasHardmode && Main.hardMode)
//            {
//                wasHardmode = true;

//                // 只执行一次演示转换
//                if (!hasPerformedDemonstration)
//                {
//                    OnHardmodeActivated();
//                    hasPerformedDemonstration = true;
//                }
//            }
//        }

//        private void OnHardmodeActivated()
//        {
//            // 困难模式激活时，执行一次演示转换
//            PerformDemonstrationConversion();
//        }

//        private void PerformDemonstrationConversion()
//        {
//            // 1. 查找所有迷迷和昔涟，记录最后的位置
//            Vector2 lastMimiPosition = Vector2.Zero;
//            Vector2 lastXilianPosition = Vector2.Zero;
//            bool hasMimi = false;
//            bool hasXilian = false;

//            // 记录最后一个迷迷的位置
//            for (int i = 0; i < Main.maxNPCs; i++)
//            {
//                if (Main.npc[i].active)
//                {
//                    if (Main.npc[i].type == ModContent.NPCType<NPCs.XiLian.Mimi>())
//                    {
//                        lastMimiPosition = Main.npc[i].Center;
//                        hasMimi = true;
//                    }
//                    else if (Main.npc[i].type == ModContent.NPCType<NPCs.XiLian.Xilian>())
//                    {
//                        lastXilianPosition = Main.npc[i].Center;
//                        hasXilian = true;
//                    }
//                }
//            }

//            // 2. 如果没有迷迷，尝试使用世界保存的位置或玩家位置
//            if (!hasMimi)
//            {
//                lastMimiPosition = WorldStateSystem.LastKnownPosition;
//                if (lastMimiPosition == Vector2.Zero)
//                {
//                    Player player = FindNearestPlayer();
//                    if (player != null)
//                    {
//                        lastMimiPosition = player.Center;
//                    }
//                    else
//                    {
//                        // 没有合适的位置，放弃转换
//                        return;
//                    }
//                }
//            }

//            // 3. 删除所有迷迷和昔涟
//            RemoveAllNPCsOfType(ModContent.NPCType<NPCs.XiLian.Mimi>());
//            RemoveAllNPCsOfType(ModContent.NPCType<NPCs.XiLian.Xilian>());

//            // 4. 生成昔涟（演示转换）
//            int xilianIndex = NPC.NewNPC(NPC.GetSource_NaturalSpawn(),
//                (int)lastMimiPosition.X, (int)lastMimiPosition.Y, ModContent.NPCType<NPCs.XiLian.Xilian>());

//            // 5. 更新世界状态
//            WorldStateSystem.UpdateFormState(WorldStateSystem.NpcForm.Xilian, lastMimiPosition);

//            // 6. 特效
//            SpawnTransformEffect(lastMimiPosition);

//            // 7. 显示消息
//            ShowMessage("迷迷在击败肉山后化为了昔涟！", Color.LightGreen);
//        }

//        private void RemoveAllNPCsOfType(int npcType)
//        {
//            for (int i = 0; i < Main.maxNPCs; i++)
//            {
//                if (Main.npc[i].active && Main.npc[i].type == npcType)
//                {
//                    if (Main.netMode == NetmodeID.MultiplayerClient || Main.netMode == NetmodeID.Server)
//                    {
//                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
//                    }
//                    Main.npc[i].active = false;
//                }
//            }
//        }

//        private Player FindNearestPlayer()
//        {
//            foreach (Player player in Main.player)
//            {
//                if (player.active && !player.dead)
//                {
//                    return player;
//                }
//            }
//            return null;
//        }

//        private void SpawnTransformEffect(Vector2 position)
//        {
//            for (int i = 0; i < 15; i++)
//            {
//                Dust.NewDustDirect(position, 20, 20,
//                    DustID.PurpleTorch, 0f, 0f, 150, default, 1.5f);
//            }
//            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, position);
//        }

//        private void ShowMessage(string text, Color color)
//        {
//            if (Main.netMode == NetmodeID.SinglePlayer)
//            {
//                Main.NewText(text, color);
//            }
//            else if (Main.netMode == NetmodeID.Server)
//            {
//                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
//            }
//        }
//    }
//}
}
