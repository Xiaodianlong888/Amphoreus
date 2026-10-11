using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Amphoreus.Systems.XiLian_Systems.Xinyuan
{
    public enum MimiQuestType
    {
        LifeCrystalStage1,
        LifeCrystalStage2,
        LifeCrystalStage3
    }

    public class MimiQuestData
    {
        public MimiQuestType QuestType { get; set; }

        // 本地化键 - 使用完整路径
        private string TitleKey => $"Systems.XiLian_Systems.Xinyuan.MimiQuest.{QuestType}.Title";
        private string DescriptionKey => $"Systems.XiLian_Systems.Xinyuan.MimiQuest.{QuestType}.Description";
        private string CompletionTextKey => $"Systems.XiLian_Systems.Xinyuan.MimiQuest.{QuestType}.CompletionText";
        private string RewardDescriptionKey => $"Systems.XiLian_Systems.Xinyuan.MimiQuest.{QuestType}.RewardDescription";
        private string LockedHintKey => $"Systems.XiLian_Systems.Xinyuan.MimiQuest.{QuestType}.LockedHint";

        public Dictionary<int, int> RequiredItems { get; set; } = new Dictionary<int, int>();

        // 状态
        public bool IsUnlocked { get; set; } = false;
        public bool IsActive { get; set; } = false;
        public bool IsCompleted { get; set; } = false;
        public bool HasUnlockNotified { get; set; } = false;

        // 属性 - 返回本地化后的文本
        public string Title => GetLocalizedText(TitleKey);
        public string Description => GetLocalizedText(DescriptionKey);
        public string CompletionText => GetLocalizedText(CompletionTextKey);
        public string RewardDescription => GetLocalizedText(RewardDescriptionKey);
        public string LockedHint => GetLocalizedText(LockedHintKey);

        // 获取本地化文本的辅助方法 - 无备用文本
        private string GetLocalizedText(string key)
        {
            if (MimiQuestSystem.ModInstance == null)
            {
                // 如果ModInstance为null，返回键（在测试中我们希望看到问题）
                return key;
            }

            // 直接获取本地化文本
            return MimiQuestSystem.ModInstance.GetLocalization(key).Value;
        }
    }

    public class MimiQuestSystem : ModSystem
    {
        private static Mod modInstance;

        public static MimiQuestData CurrentQuest { get; private set; } = null;
        public static List<MimiQuestData> AllQuests { get; private set; } = new List<MimiQuestData>();

        // 公共静态属性，供MimiQuestData访问
        public static Mod ModInstance => modInstance;

        // 奖励状态（世界进度）
        public static bool HasUnlockedLargeClaw { get; private set; } = false;
        public static bool HasUnlockedBuffAbility { get; private set; } = false;
        public static bool HasUnlockedDodgeAbility { get; private set; } = false;

        public override void Load()
        {
            modInstance = Mod;
        }

        public override void Unload()
        {
            modInstance = null;
            AllQuests.Clear();
            CurrentQuest = null;
        }

        public override void OnWorldLoad()
        {
            InitializeQuests();
            CheckQuestUnlockStatus();

            // 同步奖励状态到BookDataSystem（世界进度）
            SyncRewardStatusToBookData();
        }

        // 检查Boss击败状态并解锁任务
        public static void CheckBossDefeatsForQuestUnlock()
        {
            // 检查任务2的解锁：击败克脑/世吞且完成任务1
            var quest2 = GetQuest(MimiQuestType.LifeCrystalStage2);
            if (quest2 != null && !quest2.IsUnlocked)
            {
                bool boss2Defeated = NPC.downedBoss2; // 克脑/世吞
                bool quest1Completed = GetQuest(MimiQuestType.LifeCrystalStage1)?.IsCompleted == true;

                quest2.IsUnlocked = boss2Defeated && quest1Completed;

                if (quest2.IsUnlocked && !quest2.HasUnlockNotified)
                {
                    quest2.HasUnlockNotified = true;
                    if (Main.netMode != NetmodeID.Server)
                    {
                        string newWishText = modInstance.GetLocalization("Systems.XiLian_Systems.Xinyuan.MimiQuest.System.NewWish").Value;
                        Main.NewText(newWishText, Color.LightPink);
                    }
                }
            }

            // 检查任务3的解锁：击败骷髅王且完成任务2
            var quest3 = GetQuest(MimiQuestType.LifeCrystalStage3);
            if (quest3 != null && !quest3.IsUnlocked)
            {
                bool boss3Defeated = NPC.downedBoss3; // 骷髅王
                bool quest2Completed = GetQuest(MimiQuestType.LifeCrystalStage2)?.IsCompleted == true;

                quest3.IsUnlocked = boss3Defeated && quest2Completed;

                if (quest3.IsUnlocked && !quest3.HasUnlockNotified)
                {
                    quest3.HasUnlockNotified = true;
                    if (Main.netMode != NetmodeID.Server)
                    {
                        string canSpeakText = modInstance.GetLocalization("Systems.XiLian_Systems.Xinyuan.MimiQuest.System.CanSpeak").Value;
                        Main.NewText(canSpeakText, Color.LightPink);
                    }
                }
            }
        }

        private void InitializeQuests()
        {
            if (AllQuests.Count > 0) return;

            // 任务1：恢复吊坠的力量
            var quest1 = new MimiQuestData
            {
                QuestType = MimiQuestType.LifeCrystalStage1,
                RequiredItems = new Dictionary<int, int>
                {
                    { ItemID.LifeCrystal, 3 }
                }
            };
            AllQuests.Add(quest1);

            // 任务2：增强吊坠的力量
            var quest2 = new MimiQuestData
            {
                QuestType = MimiQuestType.LifeCrystalStage2,
                RequiredItems = new Dictionary<int, int>
                {
                    { ItemID.LifeCrystal, 6 }
                }
            };
            AllQuests.Add(quest2);

            // 任务3：魔力恢复实验
            var quest3 = new MimiQuestData
            {
                QuestType = MimiQuestType.LifeCrystalStage3,
                RequiredItems = new Dictionary<int, int>
                {
                    { ItemID.LifeCrystal, 4 },
                    { ItemID.ManaCrystal, 12 }
                }
            };
            AllQuests.Add(quest3);
        }

        public static void CheckQuestUnlockStatus()
        {
            bool hasMetMimi = Systems.Ruwosuoshu_Systems.BookDataSystem.HasMetMimi;

            foreach (var quest in AllQuests)
            {
                bool isUnlocked = false;

                switch (quest.QuestType)
                {
                    case MimiQuestType.LifeCrystalStage1:
                        isUnlocked = hasMetMimi;
                        break;

                    case MimiQuestType.LifeCrystalStage2:
                        isUnlocked = NPC.downedBoss2 &&
                                     GetQuest(MimiQuestType.LifeCrystalStage1)?.IsCompleted == true;
                        break;

                    case MimiQuestType.LifeCrystalStage3:
                        isUnlocked = NPC.downedBoss3 &&
                                     GetQuest(MimiQuestType.LifeCrystalStage2)?.IsCompleted == true;
                        break;
                }

                quest.IsUnlocked = isUnlocked;
            }
        }

        public static MimiQuestData GetQuest(MimiQuestType type)
        {
            return AllQuests.Find(q => q.QuestType == type);
        }

        public static MimiQuestData GetFirstUnlockedIncompleteQuest()
        {
            return AllQuests.Find(q => q.IsUnlocked && !q.IsCompleted && !q.IsActive);
        }

        public static bool HasAvailableQuest()
        {
            return GetFirstUnlockedIncompleteQuest() != null;
        }

        public static void AcceptQuest(MimiQuestType type)
        {
            var quest = GetQuest(type);
            if (quest != null && quest.IsUnlocked && !quest.IsCompleted && !quest.IsActive)
            {
                quest.IsActive = true;
                CurrentQuest = quest;

                Main.NewText($"迷迷：{quest.Description}", Color.LightPink);
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
            }
        }

        public static bool CanCompleteCurrentQuest(Player player)
        {
            if (CurrentQuest == null || !CurrentQuest.IsActive) return false;

            foreach (var itemReq in CurrentQuest.RequiredItems)
            {
                int itemId = itemReq.Key;
                int requiredCount = itemReq.Value;
                int playerCount = CountItemsInInventory(player, itemId);

                if (playerCount < requiredCount) return false;
            }

            CheckBossDefeatsForQuestUnlock();

            return true;
        }

        public static void CompleteCurrentQuest(Player player)
        {
            if (CurrentQuest == null || !CurrentQuest.IsActive) return;
            if (!CanCompleteCurrentQuest(player)) return;

            // 消耗物品
            foreach (var itemReq in CurrentQuest.RequiredItems)
            {
                int itemId = itemReq.Key;
                int requiredCount = itemReq.Value;
                int remaining = requiredCount;

                for (int i = 0; i < player.inventory.Length; i++)
                {
                    if (remaining <= 0) break;

                    if (player.inventory[i].type == itemId)
                    {
                        int stack = player.inventory[i].stack;
                        if (stack <= remaining)
                        {
                            remaining -= stack;
                            player.inventory[i].TurnToAir();
                        }
                        else
                        {
                            player.inventory[i].stack -= remaining;
                            remaining = 0;
                        }
                    }
                }
            }

            // 更新状态
            CurrentQuest.IsActive = false;
            CurrentQuest.IsCompleted = true;

            // 发放奖励
            switch (CurrentQuest.QuestType)
            {
                case MimiQuestType.LifeCrystalStage1:
                    HasUnlockedLargeClaw = true;
                    Main.NewText(CurrentQuest.CompletionText, Color.LightGreen);
                    break;

                case MimiQuestType.LifeCrystalStage2:
                    HasUnlockedBuffAbility = true;
                    Main.NewText(CurrentQuest.CompletionText, Color.LightGreen);
                    break;

                case MimiQuestType.LifeCrystalStage3:
                    HasUnlockedDodgeAbility = true;
                    Main.NewText(CurrentQuest.CompletionText, Color.LightGreen);
                    break;
            }

            // 同步奖励状态（世界进度）
            SyncRewardStatusToBookData();

            // 检查下一个任务
            CheckQuestUnlockStatus();

            // 清除当前任务
            CurrentQuest = null;

            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item29);
        }

        public static int CountItemsInInventory(Player player, int itemId)
        {
            int count = 0;
            for (int i = 0; i < player.inventory.Length; i++)
            {
                if (player.inventory[i].type == itemId)
                {
                    count += player.inventory[i].stack;
                }
            }
            return count;
        }

        // 同步奖励状态到BookDataSystem（世界进度）
        private static void SyncRewardStatusToBookData()
        {
            Systems.Ruwosuoshu_Systems.BookDataSystem.HasUnlockedLargeClaw = HasUnlockedLargeClaw;
            Systems.Ruwosuoshu_Systems.BookDataSystem.HasUnlockedBuffAbility = HasUnlockedBuffAbility;
            Systems.Ruwosuoshu_Systems.BookDataSystem.HasUnlockedDodgeAbility = HasUnlockedDodgeAbility;
        }

        // 获取任务标题的方法（供显示用）
        public static string GetQuestTitle(MimiQuestType questType)
        {
            var quest = GetQuest(questType);
            return quest?.Title ?? "未知任务";
        }

        // 获取任务描述的方法（供显示用）
        public static string GetQuestDescription(MimiQuestType questType)
        {
            var quest = GetQuest(questType);
            return quest?.Description ?? "";
        }

        public override void SaveWorldData(TagCompound tag)
        {
            // 保存任务状态
            var questData = new List<TagCompound>();
            foreach (var quest in AllQuests)
            {
                var questTag = new TagCompound
                {
                    ["QuestType"] = (int)quest.QuestType,
                    ["IsUnlocked"] = quest.IsUnlocked,
                    ["IsActive"] = quest.IsActive,
                    ["IsCompleted"] = quest.IsCompleted
                };
                questData.Add(questTag);
            }
            tag["MimiQuests"] = questData;

            // 保存当前任务
            tag["CurrentQuestType"] = CurrentQuest != null ? (int)CurrentQuest.QuestType : -1;

            // 保存奖励状态
            tag["HasUnlockedLargeClaw"] = HasUnlockedLargeClaw;
            tag["HasUnlockedBuffAbility"] = HasUnlockedBuffAbility;
            tag["HasUnlockedDodgeAbility"] = HasUnlockedDodgeAbility;

            tag["MimiQuestSystemVersion"] = 1;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            int version = tag.ContainsKey("MimiQuestSystemVersion") ? tag.GetInt("MimiQuestSystemVersion") : 0;

            InitializeQuests();

            if (tag.ContainsKey("MimiQuests"))
            {
                var questData = tag.GetList<TagCompound>("MimiQuests");
                foreach (var questTag in questData)
                {
                    MimiQuestType questType = (MimiQuestType)questTag.GetInt("QuestType");
                    var quest = GetQuest(questType);

                    if (quest != null)
                    {
                        quest.IsUnlocked = questTag.GetBool("IsUnlocked");
                        quest.IsActive = questTag.GetBool("IsActive");
                        quest.IsCompleted = questTag.GetBool("IsCompleted");
                    }
                }
            }

            // 加载当前任务
            if (tag.ContainsKey("CurrentQuestType"))
            {
                int currentQuestType = tag.GetInt("CurrentQuestType");
                if (currentQuestType >= 0)
                {
                    CurrentQuest = GetQuest((MimiQuestType)currentQuestType);
                }
            }

            // 加载奖励状态
            if (tag.ContainsKey("HasUnlockedLargeClaw"))
                HasUnlockedLargeClaw = tag.GetBool("HasUnlockedLargeClaw");
            if (tag.ContainsKey("HasUnlockedBuffAbility"))
                HasUnlockedBuffAbility = tag.GetBool("HasUnlockedBuffAbility");
            if (tag.ContainsKey("HasUnlockedDodgeAbility"))
                HasUnlockedDodgeAbility = tag.GetBool("HasUnlockedDodgeAbility");

            // 同步到BookDataSystem
            SyncRewardStatusToBookData();
        }

        public override void OnWorldUnload()
        {
            AllQuests.Clear();
            CurrentQuest = null;
        }
    }
}