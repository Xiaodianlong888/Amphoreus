using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System;
using Amphoreus.Players.Shenyupai_Players;
using Amphoreus.Buffs.Shenyupai_Buffs;

namespace Amphoreus.Systems.Shenyupai
{
    public class Shenyupai_System : ModSystem
    {
        // 职业类型枚举
        public enum PlayerClass
        {
            Any,
            Melee,
            Ranged,
            Magic,
            Summoner,
            Mixed
        }

        // 神谕牌buff定义
        public class ShenyupaiBuff
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public PlayerClass Class { get; set; }
            public int BuffType { get; set; } = -1; // ModBuff类型
            public int Duration { get; set; } = 60 * 60 * 24; // 24分钟游戏时间
            public Action<Player> ApplyEffect { get; set; }
            public Action<Player> RemoveEffect { get; set; }
            public float Weight { get; set; } = 1.0f; // 抽取权重
            public bool IsSpecial { get; set; } = false;
            public List<string> RequiredUnlocks { get; set; } = new List<string>(); // 解锁条件
        }

        // 所有可用的神谕牌
        public static List<ShenyupaiBuff> AllCards = new List<ShenyupaiBuff>();

        // 已解锁的牌
        public static List<string> UnlockedCards = new List<string>();

        public override void Load()
        {
            InitializeCards();
        }

        public override void Unload()
        {
            AllCards.Clear();
            UnlockedCards.Clear();
        }

        private void InitializeCards()
        {
            AllCards.Clear();

            // === 创生三泰坦 ===

            // 1. 全世之座 (负世之泰坦)
            AllCards.Add(new ShenyupaiBuff
            {
                Name = "全世之座",
                Description = "提供高额属性加成，额外拥有所有神谕牌的效果n禁用其他神谕牌的效果",
                Class = PlayerClass.Any,
                BuffType = ModContent.BuffType<Buff_FuShi>(),
                ApplyEffect = player =>
                {
                    try
                    {
                        // 获取玩家组件
                        var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                        var fuShiPlayer = player.GetModPlayer<Shenyupai_FuShi_Ultimate>();

                        if (cardPlayer != null)
                        {
                            // 清除其他玩家的所有神谕牌buff（包括负世之牌）
                            ClearAllOtherPlayersCards(player.whoAmI);

                            // 如果当前已经有卡牌，先清除
                            if (cardPlayer.HasActiveBuff && cardPlayer.CurrentCard != null)
                            {
                                // 清除当前卡牌效果，包括负世之牌
                                cardPlayer.RemoveCurrentCardEffect();
                            }

                            // 设置为负世之牌
                            cardPlayer.CurrentCard = GetCardByName("全世之座");
                            cardPlayer.BuffStartTime = Main.GameUpdateCount;

                            // 立即清除玩家身上的所有神谕牌buff
                            for (int i = 0; i < Player.MaxBuffs; i++)
                            {
                                int buffType = player.buffType[i];
                                if (buffType > 0)
                                {
                                    // 检查是否为神谕牌buff
                                    if (IsShenyupaiBuff(buffType))
                                    {
                                        player.DelBuff(i);
                                        i--; // 因为删除了一个buff，索引回退
                                    }
                                }
                            }

                            // 重新添加负世之牌buff
                            player.AddBuff(ModContent.BuffType<Buff_FuShi>(), cardPlayer.BuffDuration);
                        }

                        if (fuShiPlayer != null)
                        {
                            fuShiPlayer.ActivateFuShi();
                        }
                    }
                    catch (Exception ex)
                    {
                        Mod.Logger.Error($"激活负世之牌时发生错误: {ex.Message}");
                    }
                },
                RemoveEffect = player =>
                {
                    try
                    {
                        // 停用终极负世之牌
                        var fuShiPlayer = player.GetModPlayer<Shenyupai_FuShi_Ultimate>();
                        if (fuShiPlayer != null)
                        {
                            fuShiPlayer.DeactivateFuShi();
                        }
                    }
                    catch (Exception ex)
                    {
                        Mod.Logger.Error($"停用负世之牌时发生错误: {ex.Message}");
                    }
                },
                Weight = 0.001f, // 极低概率
                IsSpecial = true,
                RequiredUnlocks = new List<string>() // 需要已解锁所有其他牌
            });

            // 2. 黄金之茧 (浪漫之泰坦)
            AllCards.Add(new ShenyupaiBuff
            {
                Name = "黄金之茧",
                Description = "增加移速\n攻速越高收益越高",
                Class = PlayerClass.Any,
                BuffType = ModContent.BuffType<Buff_LangMan>(),
                ApplyEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasLangMan = true;

                    // 初始化新速度系统
                    cardPlayer.langManBaseMoveSpeedBonus = 0.30f; // 固定30%移速
                    cardPlayer.langManAttackTimer = 0;
                    cardPlayer.langManHitsInWindow = 0;
                    cardPlayer.langManSpeedStacks = 0;
                    cardPlayer.langManStackTimers.Clear();
                    cardPlayer.langManSpeedBonusPerStack = 0.05f; // 每层5%移速
                    cardPlayer.langManDamageBonusPerStack = 0.02f; // 每层2%伤害
                    cardPlayer.langManDodgeAvailable = false;
                    cardPlayer.langManDodgeCooldown = 0;
                    cardPlayer.langManDodgeActive = false;
                    cardPlayer.langManDodgeActiveTimer = 0;
                    cardPlayer.langManVisualTimer = 0;
                },
                RemoveEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasLangMan = false;

                    // 重置速度系统
                    cardPlayer.langManAttackTimer = 0;
                    cardPlayer.langManHitsInWindow = 0;
                    cardPlayer.langManSpeedStacks = 0;
                    cardPlayer.langManStackTimers.Clear();
                    cardPlayer.langManDodgeAvailable = false;
                    cardPlayer.langManDodgeCooldown = 0;
                    cardPlayer.langManDodgeActive = false;
                    cardPlayer.langManDodgeActiveTimer = 0;
                },
                Weight = 0.08f,
                IsSpecial = false
            });

            // 3. 分裂之枝 (理性之泰坦)
            AllCards.Add(new ShenyupaiBuff
            {
                Name = "分裂之枝",
                Description = "增加远程属性，攻击附加减益",
                Class = PlayerClass.Ranged,
                BuffType = ModContent.BuffType<Buff_LiXing>(),
                ApplyEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasLiXing = true;
                    cardPlayer.rangedSpeedBonus = 0.20f;
                    cardPlayer.rangedCritBonus = 15;
                    cardPlayer.debuffChance = 1.0f; // 100%概率附加debuff
                    cardPlayer.liXingDamageBonusPerDebuff = 0.05f; // 每层debuff增加5%伤害
                    cardPlayer.liXingMaxDamageBonus = 0.50f; // 最大50%伤害加成
                },
                RemoveEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasLiXing = false;
                    cardPlayer.rangedSpeedBonus = 0f;
                    cardPlayer.rangedCritBonus = 0;
                    cardPlayer.debuffChance = 0f;
                    cardPlayer.liXingDamageBonusPerDebuff = 0f;
                    cardPlayer.liXingMaxDamageBonus = 0f;
                },
                Weight = 1.0f,
                IsSpecial = false
            });


            // === 命运三泰坦 ===

            // 4. 万径之门 (门径之泰坦)
            AllCards.Add(new ShenyupaiBuff
            {
                Name = "万径之门",
                Description = "拥有召唤物越多伤害越高\n技能：复制所有召唤物，可在控件绑定键位",
                Class = PlayerClass.Summoner,
                BuffType = ModContent.BuffType<Buff_MenJing>(),
                ApplyEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasMenJing = true;
                    cardPlayer.minionSlotsBonus = 2;
                    cardPlayer.summonDamageBonus = 0.20f;
                    cardPlayer.perMinionBonus = 0.03f;
                },
                RemoveEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasMenJing = false;
                    cardPlayer.minionSlotsBonus = 0;
                    cardPlayer.summonDamageBonus = 0f;
                    cardPlayer.perMinionBonus = 0f;
                },
                Weight = 1.0f,
                IsSpecial = false
            });

            // 5. 公正之秤 (律法之泰坦)
            AllCards.Add(new ShenyupaiBuff
            {
                Name = "公正之秤",
                Description = "可触发双倍伤害免疫魔力病\n技能：交换生命与魔力，可在控件绑定键位",
                Class = PlayerClass.Magic,
                BuffType = ModContent.BuffType<Buff_LvFa>(),
                ApplyEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasLvFa = true;
                    cardPlayer.manaCostReduction = 0.25f;
                    cardPlayer.doubleDamageTimer = 0;
                    cardPlayer.isDoubleDamageActive = false;
                    cardPlayer.doubleDamageActiveTimer = 0;
                },
                RemoveEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasLvFa = false;
                    cardPlayer.manaCostReduction = 0f;
                    cardPlayer.doubleDamageTimer = 0;
                    cardPlayer.isDoubleDamageActive = false;
                    cardPlayer.doubleDamageActiveTimer = 0;
                },
                Weight = 1.0f,
                IsSpecial = false
            });

            // 6. 永夜之帷 (岁月之泰坦)
            AllCards.Add(new ShenyupaiBuff
            {
                Name = "永夜之帷",
                Description = "按时间增加自身与敌人的易伤比例\n技能：时间回溯，可在控件绑定键位",
                Class = PlayerClass.Any,
                BuffType = ModContent.BuffType<Buff_SuiYue>(),
                ApplyEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasSuiYue = true;
                    cardPlayer.timeControlCooldown = 60;
                    cardPlayer.survivalDamagePerMinute = 0.01f;
                    cardPlayer.survivalTime = 0;
                },
                RemoveEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasSuiYue = false;
                    cardPlayer.timeControlCooldown = 0;
                    cardPlayer.survivalDamagePerMinute = 0f;
                },
                Weight = 0.05f,
                IsSpecial = true
            });


            // === 支柱三泰坦 ===

            // 7. 磐岩之脊 (大地之泰坦)
            AllCards.Add(new ShenyupaiBuff
            {
                Name = "磐岩之脊",
                Description = "免疫多种负面效果，获得基于防御力的大地护盾",
                Class = PlayerClass.Any,
                BuffType = ModContent.BuffType<Buffs.Shenyupai_Buffs.Buff_DaDi>(),
                ApplyEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Players.Shenyupai_Players.Shenyupai_Player>();
                    cardPlayer.hasDaDi = true;
                    cardPlayer.groundMoveSpeedBonus = 0.35f;

                    // 设置免疫
                    cardPlayer.immuneToLava = true;
                    cardPlayer.immuneToPetrify = true;
                    cardPlayer.immuneToKnockback = true;
                    cardPlayer.immuneToFireBlocks = true;
                    cardPlayer.immuneToWeakness = true;
                    cardPlayer.immuneToBrokenArmor = true;

                    // 立即应用免疫效果
                    player.noKnockback = true;
                    player.lavaImmune = true;
                    player.lavaMax = 99999;
                },
                RemoveEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Players.Shenyupai_Players.Shenyupai_Player>();
                    cardPlayer.hasDaDi = false;
                    cardPlayer.groundMoveSpeedBonus = 0f;

                    // 移除免疫
                    cardPlayer.immuneToLava = false;
                    cardPlayer.immuneToPetrify = false;
                    cardPlayer.immuneToKnockback = false;
                    cardPlayer.immuneToFireBlocks = false;
                    cardPlayer.immuneToWeakness = false;
                    cardPlayer.immuneToBrokenArmor = false;
                },
                Weight = 0.07f,
                IsSpecial = false
            });


            // 8. 满溢之杯 (海洋之泰坦)
            AllCards.Add(new ShenyupaiBuff
            {
                Name = "满溢之杯",
                Description = "水中畅通无阻，水/雨中伤害翻倍",
                Class = PlayerClass.Any,
                BuffType = ModContent.BuffType<Buff_HaiYang>(),
                ApplyEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasHaiYang = true;
                    cardPlayer.waterMoveSpeedBonus = 0.80f;
                    cardPlayer.waterLifeRegenBonus = 5;
                },
                RemoveEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasHaiYang = false;
                    cardPlayer.waterMoveSpeedBonus = 0f;
                    cardPlayer.waterLifeRegenBonus = 0;
                },
                Weight = 0.07f,
                IsSpecial = false
            });

            // 9. 晨昏之眼 (天空之泰坦)
            AllCards.Add(new ShenyupaiBuff
            {
                Name = "晨昏之眼",
                Description = "无限飞行，晴天增加伤害，雷雨增加暴击",
                Class = PlayerClass.Any,
                BuffType = ModContent.BuffType<Buff_TianKong>(),
                ApplyEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasTianKong = true;
                    cardPlayer.flightSpeedBonus = 0.50f;
                    cardPlayer.wingHoverBonus = 1.0f;
                    cardPlayer.highAltitudeDamageBonus = 0.15f;
                },
                RemoveEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasTianKong = false;
                    cardPlayer.flightSpeedBonus = 0f;
                    cardPlayer.wingHoverBonus = 0f;
                    cardPlayer.highAltitudeDamageBonus = 0f;
                },
                Weight = 0.07f,
                IsSpecial = false
            });

            // === 灾厄三泰坦 ===

            // 10. 天谴之矛 (纷争之泰坦)
            AllCards.Add(new ShenyupaiBuff
            {
                Name = "天谴之矛",
                Description = "增加近战属性，防御转生命，嘲讽敌人",
                Class = PlayerClass.Melee,
                BuffType = ModContent.BuffType<Buff_FenZheng>(),
                ApplyEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasFenZheng = true;
                    cardPlayer.meleeDamageBonus = 0.30f;
                    cardPlayer.meleeSpeedBonus = 0.15f;
                    cardPlayer.defenseToLifeConversion = 20;
                    cardPlayer.defenseToRegenConversion = 1;
                    cardPlayer.fenZhengDeathImmuneCooldown = 15;
                    cardPlayer.lastFenZhengDeathImmuneTime = -999;
                    cardPlayer.tauntEnemies = true;
                    cardPlayer.damageTakenCap = 0.75f;
                },
                RemoveEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasFenZheng = false;
                    cardPlayer.meleeDamageBonus = 0f;
                    cardPlayer.meleeSpeedBonus = 0f;
                    cardPlayer.defenseToLifeConversion = 0;
                    cardPlayer.defenseToRegenConversion = 0;
                    cardPlayer.fenZhengDeathImmuneCooldown = 0;
                    cardPlayer.tauntEnemies = false;
                    cardPlayer.damageTakenCap = 1f;
                },
                Weight = 1.0f,
                IsSpecial = false
            });

            // 11. 翻飞之币 (诡计之泰坦)
            AllCards.Add(new ShenyupaiBuff
            {
                Name = "翻飞之币",
                Description = "可隐身移速+15%，概率不消耗宝藏袋，攻击标记‘爆金币’",
                Class = PlayerClass.Any,
                BuffType = ModContent.BuffType<Buff_GuiJi>(),
                ApplyEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasGuiJi = true;
                    cardPlayer.moveSpeedBonus = 0.15f;
                    cardPlayer.itemMagnetRangeBonus = 800f;
                    cardPlayer.isInvisible = false;
                    // 初始化隐身系统（使用公共字段）
                    cardPlayer.guiJiDownPressCount = 0;
                    cardPlayer.guiJiLastDownPressTime = 0;
                    cardPlayer.guiJiWasControlDown = false;
                },
                RemoveEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasGuiJi = false;
                    cardPlayer.moveSpeedBonus = 0f;
                    cardPlayer.itemMagnetRangeBonus = 0f;
                    cardPlayer.isInvisible = false;
                    player.invis = false;
                },
                Weight = 0.05f,
                IsSpecial = true
            });

            // 12. 灰黯之手 (死亡之泰坦)
            AllCards.Add(new ShenyupaiBuff
            {
                Name = "灰黯之手",
                Description = "损失生命增加伤害，恢复生命积攒新蕊，可免疫死亡，可召唤鬼手汲取生命",
                Class = PlayerClass.Any,
                BuffType = ModContent.BuffType<Buff_SiWang>(),
                ApplyEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasSiWang = true;
                    cardPlayer.deathImmunityCooldown = 300; // 300秒冷却
                    cardPlayer.lastDeathImmunityTime = -999;
                    cardPlayer.ghostHandChance = 0.25f; // 25%概率
                    cardPlayer.lifeStealAmount = 0.10f; // 10%回血
                    cardPlayer.teamDamageTransfer = false;
                    cardPlayer.deathImmunityUsed = false;
                },
                RemoveEffect = player =>
                {
                    var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                    cardPlayer.hasSiWang = false;
                    cardPlayer.deathImmunityCooldown = 0;
                    cardPlayer.ghostHandChance = 0f;
                    cardPlayer.lifeStealAmount = 0f;
                    cardPlayer.teamDamageTransfer = false;
                    cardPlayer.deathImmunityUsed = false;
                },
                Weight = 0.05f,
                IsSpecial = true
            });
        }

        // 检查是否为神谕牌buff
        private static bool IsShenyupaiBuff(int buffType)
        {
            // 检查所有神谕牌buff
            foreach (var card in AllCards)
            {
                if (card.BuffType > 0 && buffType == card.BuffType)
                {
                    return true;
                }
            }
            return false;
        }

        // 清理所有其他玩家卡牌效果的方法
        private static void ClearAllOtherPlayersCards(int excludePlayerId)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (i == excludePlayerId) continue; // 跳过负世之牌持有者

                Player otherPlayer = Main.player[i];
                if (otherPlayer.active && !otherPlayer.dead)
                {
                    var otherCardPlayer = otherPlayer.GetModPlayer<Shenyupai_Player>();
                    if (otherCardPlayer != null)
                    {
                        // 清除其他玩家的卡牌效果
                        otherCardPlayer.RemoveCurrentCardEffect();

                        // 清除其他玩家身上的所有神谕牌buff
                        for (int j = 0; j < Player.MaxBuffs; j++)
                        {
                            int buffType = otherPlayer.buffType[j];
                            if (buffType > 0 && IsShenyupaiBuff(buffType))
                            {
                                otherPlayer.DelBuff(j);
                                j--; // 因为删除了一个buff，索引回退
                            }
                        }
                    }

                    // 清除其他玩家的负世之牌效果
                    var otherFuShiPlayer = otherPlayer.GetModPlayer<Shenyupai_FuShi_Ultimate>();
                    if (otherFuShiPlayer != null && otherFuShiPlayer.IsFuShiActive)
                    {
                        otherFuShiPlayer.DeactivateFuShi();
                    }
                }
            }
        }

        // 抽取一张随机卡牌
        public static ShenyupaiBuff DrawRandomCard(Player player = null)
        {
            if (AllCards.Count == 0)
                return null;

            // 获取玩家数据
            var cardPlayer = player?.GetModPlayer<Shenyupai_Player>();

            // 计算总权重和过滤符合条件的卡牌
            List<ShenyupaiBuff> availableCards = new List<ShenyupaiBuff>();
            float totalWeight = 0;

            foreach (var card in AllCards)
            {
                // 检查解锁条件
                bool canDraw = true;

                // 检查特殊解锁条件
                if (card.Name == "全世之座")
                {
                    // 全世之座需要已解锁至少11张其他牌
                    int unlockedCount = UnlockedCards.Count;
                    if (unlockedCount < 11 || UnlockedCards.Contains("全世之座"))
                    {
                        canDraw = false;
                    }
                }

                if (canDraw)
                {
                    availableCards.Add(card);
                    totalWeight += card.Weight;
                }
            }

            if (availableCards.Count == 0)
                return null;

            // 随机选择
            float random = Main.rand.NextFloat() * totalWeight;
            float current = 0;

            foreach (var card in availableCards)
            {
                current += card.Weight;
                if (random <= current)
                {
                    // 如果是首次抽取，添加到解锁列表
                    if (!UnlockedCards.Contains(card.Name))
                    {
                        UnlockedCards.Add(card.Name);
                    }

                    return card;
                }
            }

            return availableCards[0]; // 保底
        }

        // 根据名称获取卡牌
        public static ShenyupaiBuff GetCardByName(string name)
        {
            foreach (var card in AllCards)
            {
                if (card.Name == name)
                {
                    return card;
                }
            }
            return null;
        }

        // 获取所有卡牌名称列表
        public static List<string> GetAllCardNames()
        {
            List<string> names = new List<string>();
            foreach (var card in AllCards)
            {
                names.Add(card.Name);
            }
            return names;
        }

        // 获取所有卡牌（按顺序）
        public static List<ShenyupaiBuff> GetAllCards()
        {
            return AllCards;
        }
    }
}