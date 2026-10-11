using Amphoreus.Systems.XiLian_Systems.Xinyuan;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Amphoreus.Systems.Ruwosuoshu_Systems
{
    public class BookDataSystem : ModSystem
    {
        // === 世界数据 ===
        public static List<WishTemplate> WishTemplates { get; private set; } = new List<WishTemplate>();
        public static bool HasMetMimi { get; set; } = false;
        public static Vector2 BookIconPosition { get; set; } = Vector2.Zero;

        // 奖励状态（世界进度）
        public static bool HasUnlockedLargeClaw { get; set; } = false;
        public static bool HasUnlockedBuffAbility { get; set; } = false;
        public static bool HasUnlockedDodgeAbility { get; set; } = false;
        public static bool HasUnlockedXilian { get; private set; } = false;

        public override void OnWorldLoad()
        {
            // 初始化世界上的心愿模板
            WishTemplates.Clear();
            WishTemplates.Add(new WishTemplate
            {
                WishId = "mimi_wish1",
                Title = "暗淡的吊坠",
                RequiredQuest = MimiQuestType.LifeCrystalStage1
            });
            WishTemplates.Add(new WishTemplate
            {
                WishId = "mimi_wish2",
                Title = "恢复力量",
                RequiredQuest = MimiQuestType.LifeCrystalStage2
            });
            WishTemplates.Add(new WishTemplate
            {
                WishId = "mimi_wish3",
                Title = "魔力恢复",
                RequiredQuest = MimiQuestType.LifeCrystalStage3
            });

            // 其他世界状态
            HasUnlockedXilian = NPC.downedBoss3;
        }

        // 检查心愿是否对玩家解锁（根据世界进度）
        public static bool IsWishUnlockedForPlayer(string wishId)
        {
            var template = WishTemplates.Find(t => t.WishId == wishId);
            if (template == null) return false;

            // 获取对应的任务状态
            var quest = MimiQuestSystem.GetQuest(template.RequiredQuest);
            return quest != null && quest.IsUnlocked;
        }

        // === 静态辅助方法（方便调用） ===
        public static bool WriteWish(Player player, string wishId)
        {
            var modPlayer = player.GetModPlayer<PlayerWishData>();
            return modPlayer.WriteWish(wishId);
        }

        public static bool CompleteWish(Player player, string wishId)
        {
            var modPlayer = player.GetModPlayer<PlayerWishData>();
            return modPlayer.CompleteWish(wishId);
        }

        // 当与迷迷对话时调用
        public static void OnMimiDialogue()
        {
            HasMetMimi = true;
        }

        // 保存世界数据
        public override void SaveWorldData(TagCompound tag)
        {
            // 心愿模板不需要保存，因为是硬编码的

            // 其他世界状态
            tag["BookIconX"] = BookIconPosition.X;
            tag["BookIconY"] = BookIconPosition.Y;
            tag["HasUnlockedXilian"] = HasUnlockedXilian;
            tag["HasMetMimi"] = HasMetMimi;
            tag["HasUnlockedLargeClaw"] = HasUnlockedLargeClaw;
            tag["HasUnlockedBuffAbility"] = HasUnlockedBuffAbility;
            tag["HasUnlockedDodgeAbility"] = HasUnlockedDodgeAbility;
        }

        // 加载世界数据
        public override void LoadWorldData(TagCompound tag)
        {
            // 先调用OnWorldLoad初始化模板
            OnWorldLoad();

            // 加载其他状态
            if (tag.ContainsKey("BookIconX"))
            {
                float x = tag.GetFloat("BookIconX");
                float y = tag.GetFloat("BookIconY");
                BookIconPosition = new Vector2(x, y);
            }

            if (tag.ContainsKey("HasUnlockedXilian"))
                HasUnlockedXilian = tag.GetBool("HasUnlockedXilian");

            if (tag.ContainsKey("HasMetMimi"))
                HasMetMimi = tag.GetBool("HasMetMimi");

            if (tag.ContainsKey("HasUnlockedLargeClaw"))
                HasUnlockedLargeClaw = tag.GetBool("HasUnlockedLargeClaw");
            if (tag.ContainsKey("HasUnlockedBuffAbility"))
                HasUnlockedBuffAbility = tag.GetBool("HasUnlockedBuffAbility");
            if (tag.ContainsKey("HasUnlockedDodgeAbility"))
                HasUnlockedDodgeAbility = tag.GetBool("HasUnlockedDodgeAbility");
        }

        public override void OnWorldUnload()
        {
            // 清除非持久化状态
            HasMetMimi = false;
            // 但保留世界进度相关的奖励状态，它们在SaveWorldData中已保存
        }

        // 新增的辅助方法
        public static string GetWishTitle(string wishId)
        {
            var template = WishTemplates.Find(t => t.WishId == wishId);
            return template?.Title ?? "未知心愿";
        }

        // 修复错误：移除 GetRequiredQuest 方法，因为 MimiQuestType 枚举没有 None 值
        // 或者如果有 MimiQuestType 枚举的定义，我们需要知道正确的默认值
        // 暂时注释掉这个方法，因为它可能不需要
        /*
        public static MimiQuestType GetRequiredQuest(string wishId)
        {
            var template = WishTemplates.Find(t => t.WishId == wishId);
            // 这里需要根据 MimiQuestType 枚举的实际定义来返回默认值
            // 假设 MimiQuestType 枚举有默认值，比如第一个值
            return template?.RequiredQuest ?? default(MimiQuestType);
        }
        */
    }
}