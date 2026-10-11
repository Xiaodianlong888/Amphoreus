using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Amphoreus.Systems
{
    public class GuideDialogueSystem : ModSystem
    {
        // 标记是否已经给过笔（世界数据）
        public static bool HasGivenQuill = false;

        public override void OnWorldLoad()
        {
            // 加载世界时重置状态
            HasGivenQuill = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            // 保存是否给过笔的状态
            tag["HasGivenQuill"] = HasGivenQuill;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            // 加载是否给过笔的状态
            HasGivenQuill = tag.GetBool("HasGivenQuill");
        }

        // 统一的本地化调用方法
        private string GetLocalizedText(string key, params object[] args)
        {
            string fullKey = $"Mods.Amphoreus.{key}";
            string text = Language.GetTextValue(fullKey);

            // 检查是否获取到了实际文本（而不是键名本身）
            if (text == fullKey)
            {
                // 本地化键未找到，直接返回键名以便调试
                return $"[{key}]";
            }

            // 如果有参数，格式化文本
            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(text, args);
                }
                catch
                {
                    return text;
                }
            }

            return text;
        }

        // 网络同步
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(HasGivenQuill);
        }

        public override void NetReceive(BinaryReader reader)
        {
            HasGivenQuill = reader.ReadBoolean();
        }
    }

    // 扩展GlobalNPC类来处理向导对话
    public class GuideQuillGiverNPC : GlobalNPC
    {
        public override bool AppliesToEntity(NPC npc, bool lateInstantiation)
        {
            // 只应用于向导
            return npc.type == NPCID.Guide;
        }

        // 统一的本地化调用方法
        private string GetLocalizedText(string key, params object[] args)
        {
            string fullKey = $"Mods.Amphoreus.{key}";
            string text = Language.GetTextValue(fullKey);

            // 检查是否获取到了实际文本（而不是键名本身）
            if (text == fullKey)
            {
                // 本地化键未找到，直接返回键名以便调试
                return $"[{key}]";
            }

            // 如果有参数，格式化文本
            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(text, args);
                }
                catch
                {
                    return text;
                }
            }

            return text;
        }

        public override void GetChat(NPC npc, ref string chat)
        {
            // 只在服务器端或单人模式中处理
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // === 修复：只在第一次向导对话时给予笔，且世界唯一 ===
            if (!GuideDialogueSystem.HasGivenQuill)
            {
                // 给予笔并修改对话
                GiveQuillToPlayer(npc);
                GuideDialogueSystem.HasGivenQuill = true;

                // 在多人模式中同步世界数据
                if (Main.netMode == NetmodeID.Server)
                {
                    // 通知所有客户端更新世界数据
                    NetMessage.SendData(MessageID.WorldData);
                }

                // 修改对话内容 - 使用本地化文本
                chat = GetLocalizedText("GuideDialogue.GiveQuill");
            }
        }

        private void GiveQuillToPlayer(NPC npc)
        {
            Player player = Main.LocalPlayer;

            // 给予笔
            int itemType = ModContent.ItemType<Items.XiLian_Items.RemembrancersQuill>();
            int itemIndex = Item.NewItem(
                new EntitySource_Gift(npc),
                player.Center,
                itemType,
                1,
                noBroadcast: false
            );

            // 在多人模式中同步物品
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itemIndex, 1f);
            }

            // 播放获得物品的音效
            SoundEngine.PlaySound(SoundID.Item59, player.Center);

            // 显示获得物品的消息 - 使用本地化文本
            Item item = new Item();
            item.SetDefaults(itemType);
            string itemName = item.Name;

            string obtainedText = GetLocalizedText("GuideDialogue.ObtainedItem", player.name, itemName);
            Main.NewText(obtainedText, 255, 200, 255);

            // 同时添加到玩家的背包中，避免物品掉落在地上
            player.QuickSpawnItem(player.GetSource_GiftOrReward("GuideGift"), itemType, 1);
        }
    }
}