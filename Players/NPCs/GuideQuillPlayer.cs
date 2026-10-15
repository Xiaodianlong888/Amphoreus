using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Amphoreus.Players.NPCs
{
    public class GuideQuillPlayer : ModPlayer
    {
        // 每个玩家独立的标记，表示是否已经获得过笔
        public bool hasReceivedQuill = false;

        public override void OnEnterWorld()
        {
            // 进入世界时检查是否需要给予笔
            if (Main.netMode != NetmodeID.MultiplayerClient && Main.myPlayer == Player.whoAmI)
            {
                CheckAndGiveQuill();
            }
        }

        public override void PostUpdate()
        {
            // 持续检查是否正在与向导对话
            if (!hasReceivedQuill && !Player.HasItem(ModContent.ItemType<Items.XiLian_Items.RemembrancersQuill>()))
            {
                CheckAndGiveQuill();
            }
        }

        private void CheckAndGiveQuill()
        {
            // 检查玩家是否正在与向导对话
            if (Player.talkNPC >= 0 && Main.npc[Player.talkNPC].type == NPCID.Guide)
            {
                // 获取当前对话
                string currentChat = Main.npcChatText;

                // 如果是向导的初始对话（包含特定关键词），给予笔
                if (IsGuideIntroDialogue(currentChat) && !hasReceivedQuill)
                {
                    GiveQuillToPlayer();
                    hasReceivedQuill = true;
                }
            }
        }

        private bool IsGuideIntroDialogue(string chat)
        {
            // 检测是否是向导的自我介绍对话
            return chat.Contains("欢迎") ||
                   chat.Contains("新手") ||
                   chat.Contains("帮助") ||
                   chat.Contains("craft") ||
                   chat.Contains("制作") ||
                   chat.Length > 50; // 向导的初始对话通常较长
        }

        private void GiveQuillToPlayer()
        {
            // 设置特殊对话
            Main.npcChatText = "嘿，等等...我感觉到这支笔在寻找你！\n" +
                              "[c/FF6BCB:（向导从口袋里掏出一支闪烁紫色光芒的羽毛笔）]\n" +
                              "它散发着某种魔力，也许这就是命运的安排？\n" +
                              "试试用它记录下你的冒险吧！";

            // 给予笔
            int itemType = ModContent.ItemType<Items.XiLian_Items.RemembrancersQuill>();

            // 直接添加到玩家背包
            int itemIndex = Player.QuickSpawnItem(Player.GetSource_GiftOrReward("GuideGift"), itemType, 1);

            // 播放音效
            SoundEngine.PlaySound(SoundID.Item59, Player.Center);

            // 显示获得信息
            Main.NewText($"[c/FF6BCB:{Player.name}] 获得了 [c/AA66FF:记忆之笔]！", 255, 200, 255);
        }

        public override void SaveData(TagCompound tag)
        {
            tag["hasReceivedQuill"] = hasReceivedQuill;
        }

        public override void LoadData(TagCompound tag)
        {
            hasReceivedQuill = tag.GetBool("hasReceivedQuill");
        }
    }
}