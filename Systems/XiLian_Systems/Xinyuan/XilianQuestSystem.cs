using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Amphoreus.Systems.Ruwosuoshu;

namespace Amphoreus.Systems.XiLian_Systems.Xinyuan
{
    public enum XiLianQuestType
    {
        /// <summary>花茶·初章：太阳花 + 月光草</summary>
        HerbTeaChapter1,
        /// <summary>花茶·次章：闪耀根 + 水叶草</summary>
        HerbTeaChapter2,
        /// <summary>花茶·终章：火焰花 + 寒颤棘 + 死亡草</summary>
        HerbTeaChapter3
    }

    public class XiLianQuestData
    {
        public XiLianQuestType QuestType { get; set; }
        public Dictionary<int, int> RequiredItems { get; set; } = new Dictionary<int, int>();
        public bool IsUnlocked { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }

        // 遵循既有约定，与 MimiQuest 同级：Systems.XiLian_Systems.Xinyuan.XiLianQuest.*
        private string Key(string field) => $"Mods.Amphoreus.Systems.XiLian_Systems.Xinyuan.XiLianQuest.{QuestType}.{field}";

        public string Title => Language.GetTextValue(Key("Title"));
        public string Description => Language.GetTextValue(Key("Description"));
        public string CompletionText => Language.GetTextValue(Key("CompletionText"));
    }

    /// <summary>
    /// 昔涟的花茶任务：三章，逐章解锁，完成后开放对应花茶售卖。
    /// 任务进度保存在世界数据里（对所有玩家共享）。
    /// </summary>
    public class XiLianQuestSystem : ModSystem
    {
        public static XiLianQuestData CurrentQuest { get; private set; }
        public static List<XiLianQuestData> AllQuests { get; private set; } = new List<XiLianQuestData>();

        // ---- 商店解锁标志（由 Xilian.cs 的 Condition 读取）----
        public static bool Chapter1Completed => GetQuest(XiLianQuestType.HerbTeaChapter1)?.IsCompleted == true;
        public static bool Chapter2Completed => GetQuest(XiLianQuestType.HerbTeaChapter2)?.IsCompleted == true;
        public static bool Chapter3Completed => GetQuest(XiLianQuestType.HerbTeaChapter3)?.IsCompleted == true;

        public override void OnWorldLoad()
        {
            InitializeQuests();
            CheckQuestUnlockStatus();
        }

        public override void OnWorldUnload()
        {
            AllQuests.Clear();
            CurrentQuest = null;
        }

        private static void InitializeQuests()
        {
            if (AllQuests.Count > 0)
            {
                return;
            }

            // 太阳花 = Daybloom(313)，注意不要用 Sunflower(63)（那是向日葵）
            AllQuests.Add(new XiLianQuestData
            {
                QuestType = XiLianQuestType.HerbTeaChapter1,
                RequiredItems = new Dictionary<int, int>
                {
                    { ItemID.Daybloom, 5 },
                    { ItemID.Moonglow, 5 }
                }
            });

            AllQuests.Add(new XiLianQuestData
            {
                QuestType = XiLianQuestType.HerbTeaChapter2,
                RequiredItems = new Dictionary<int, int>
                {
                    { ItemID.Blinkroot, 5 },
                    { ItemID.Waterleaf, 5 }
                }
            });

            AllQuests.Add(new XiLianQuestData
            {
                QuestType = XiLianQuestType.HerbTeaChapter3,
                RequiredItems = new Dictionary<int, int>
                {
                    { ItemID.Fireblossom, 5 },
                    { ItemID.Shiverthorn, 5 },
                    { ItemID.Deathweed, 5 }
                }
            });
        }

        /// <summary>按进度刷新任务的解锁状态，并在刚解锁时提示玩家。</summary>
        public static void CheckQuestUnlockStatus()
        {
            bool hasMetXilian = BookDataSystem.HasMetXilian;

            foreach (XiLianQuestData quest in AllQuests)
            {
                bool shouldUnlock = quest.QuestType switch
                {
                    XiLianQuestType.HerbTeaChapter1 => hasMetXilian,
                    XiLianQuestType.HerbTeaChapter2 => Chapter1Completed,
                    XiLianQuestType.HerbTeaChapter3 => Chapter2Completed,
                    _ => false
                };

                if (!quest.IsUnlocked && shouldUnlock)
                {
                    quest.IsUnlocked = true;

                    if (Main.netMode != NetmodeID.Server)
                    {
                        Main.NewText(
                            Language.GetTextValue("Mods.Amphoreus.Systems.XiLian_Systems.Xinyuan.XiLianQuest.System.NewQuestUnlocked"),
                            Color.LightGoldenrodYellow);
                    }
                }
            }
        }

        public static XiLianQuestData GetQuest(XiLianQuestType type)
        {
            return AllQuests.Find(q => q.QuestType == type);
        }

        public static XiLianQuestData GetFirstAvailableQuest()
        {
            return AllQuests.Find(q => q.IsUnlocked && !q.IsCompleted && !q.IsActive);
        }

        public static void AcceptQuest(XiLianQuestType type)
        {
            XiLianQuestData quest = GetQuest(type);
            if (quest == null || !quest.IsUnlocked || quest.IsCompleted || quest.IsActive)
            {
                return;
            }

            quest.IsActive = true;
            CurrentQuest = quest;

            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText(
                    Language.GetTextValue("Mods.Amphoreus.Systems.XiLian_Systems.Xinyuan.XiLianQuest.System.QuestAccepted", quest.Title),
                    Color.LightGoldenrodYellow);
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public static bool CanCompleteCurrentQuest(Player player)
        {
            if (CurrentQuest == null || !CurrentQuest.IsActive)
            {
                return false;
            }

            foreach (KeyValuePair<int, int> req in CurrentQuest.RequiredItems)
            {
                if (CountItemsInInventory(player, req.Key) < req.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public static void CompleteCurrentQuest(Player player)
        {
            if (!CanCompleteCurrentQuest(player))
            {
                return;
            }

            foreach (KeyValuePair<int, int> req in CurrentQuest.RequiredItems)
            {
                ConsumeItem(player, req.Key, req.Value);
            }

            CurrentQuest.IsActive = false;
            CurrentQuest.IsCompleted = true;

            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText(CurrentQuest.CompletionText, Color.LightGreen);
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29);

            CurrentQuest = null;
            CheckQuestUnlockStatus();
        }

        public static int CountItemsInInventory(Player player, int itemId)
        {
            int total = 0;
            for (int i = 0; i < player.inventory.Length; i++)
            {
                if (player.inventory[i].type == itemId)
                {
                    total += player.inventory[i].stack;
                }
            }
            return total;
        }

        private static void ConsumeItem(Player player, int itemType, int amount)
        {
            for (int i = 0; i < player.inventory.Length && amount > 0; i++)
            {
                if (player.inventory[i].type != itemType)
                {
                    continue;
                }

                int stack = player.inventory[i].stack;
                if (stack <= amount)
                {
                    amount -= stack;
                    player.inventory[i].TurnToAir();
                }
                else
                {
                    player.inventory[i].stack -= amount;
                    amount = 0;
                }
            }
        }

        public override void SaveWorldData(TagCompound tag)
        {
            List<TagCompound> questData = new List<TagCompound>();
            foreach (XiLianQuestData quest in AllQuests)
            {
                questData.Add(new TagCompound
                {
                    ["QuestType"] = (int)quest.QuestType,
                    ["IsUnlocked"] = quest.IsUnlocked,
                    ["IsActive"] = quest.IsActive,
                    ["IsCompleted"] = quest.IsCompleted
                });
            }

            tag["XiLianQuests"] = questData;
            tag["CurrentQuestType"] = CurrentQuest != null ? (int)CurrentQuest.QuestType : -1;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            InitializeQuests();

            if (tag.ContainsKey("XiLianQuests"))
            {
                foreach (TagCompound qTag in tag.GetList<TagCompound>("XiLianQuests"))
                {
                    XiLianQuestData quest = GetQuest((XiLianQuestType)qTag.GetInt("QuestType"));
                    if (quest == null)
                    {
                        continue;
                    }

                    quest.IsUnlocked = qTag.GetBool("IsUnlocked");
                    quest.IsActive = qTag.GetBool("IsActive");
                    quest.IsCompleted = qTag.GetBool("IsCompleted");
                }
            }

            if (tag.ContainsKey("CurrentQuestType"))
            {
                int typeVal = tag.GetInt("CurrentQuestType");
                if (typeVal >= 0)
                {
                    CurrentQuest = GetQuest((XiLianQuestType)typeVal);
                }
            }
        }
    }
}
