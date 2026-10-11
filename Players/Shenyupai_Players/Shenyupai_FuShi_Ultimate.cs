using Amphoreus.Buffs.Shenyupai_Buffs;
using Amphoreus.Systems.Shenyupai;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Amphoreus.Players.Shenyupai_Players
{
    public class Shenyupai_FuShi_Ultimate : ModPlayer
    {
        // ====== 核心状态 ======
        public bool IsFuShiActive { get; set; } = false;
        public int FuShiBuffType => ModContent.BuffType<Buff_FuShi>();
        public const int FuShiDuration = 60 * 60 * 24; // 24分钟

        // ====== 创生三泰坦 ======
        public float FuShiAllDamageBonus = 0.50f; // 50%全伤害
        public int FuShiDefenseBonus = 50; // 50防御
        public float FuShiDamageReflect = 0.50f; // 反弹50%伤害

        // 浪漫系统：越战越勇
        public int CombatTime = 0; // 战斗时长（帧）
        public const int CombatTimeMax = 60 * 60 * 10; // 10分钟上限
        public float CombatBonusMultiplier => (float)CombatTime / (60 * 60); // 每分钟加成
        public float CombatDamageBonus => Math.Min(CombatBonusMultiplier * 0.05f, 0.30f); // 每分钟5%，上限30%
        public float CombatSpeedBonus => Math.Min(CombatBonusMultiplier * 0.03f, 0.20f); // 每分钟3%，上限20%
        public float CombatCritBonus => Math.Min(CombatBonusMultiplier * 1.0f, 20f); // 每分钟1%，上限20%
        public float CombatDefenseBonus => Math.Min(CombatBonusMultiplier * 2.0f, 30f); // 每分钟2%，上限30%

        // 理性系统：弱点击破
        public Dictionary<int, int> EnemyDebuffStacks = new Dictionary<int, int>();
        public const int MaxDebuffStacks = 20;
        public float DamagePerDebuffStack = 0.08f; // 每层8%增伤
        public int LastAppliedDebuffType = 0;
        public int DebuffApplyTimer = 0;
        public const int DebuffApplyInterval = 30; // 每0.5秒尝试附加

        // ====== 命运三泰坦 ======
        // 门径：人多力量大
        public int OriginalMinionSlots = 0;
        public int FuShiMinionSlotsBonus = 0; // 双倍召唤栏
        public float DamagePerMinion = 0.10f; // 每个召唤物10%增伤
        public float MaxMinionDamageBonus = 3.00f; // 最高300%

        // 律法：征服
        public bool ImmuneToManaSickness = true;
        public float PermanentDamageMultiplier = 2.0f; // 永久×2伤害

        // 岁月：回溯
        public bool TimeRewindAvailable = true;
        public int TimeRewindCooldown = 13 * 60; // 13秒冷却
        public int TimeRewindCurrentCooldown = 0;
        public struct PlayerSnapshot
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public int Life;
            public int Mana;
            public int[] BuffTypes;
            public int[] BuffTimes;
        }
        public Queue<PlayerSnapshot> TimeSnapshots = new Queue<PlayerSnapshot>();
        public const int MaxSnapshots = 60; // 存储1秒历史（60帧）

        // ====== 大地：防御减伤 ======
        public float DefenseDamageReduction = 0f;
        public const float DefenseReductionRate = 0.001f; // 每点防御提供0.1%减伤
        public const float MaxDefenseReduction = 0.50f; // 最大50%减伤

        // 免疫系统
        public bool ImmuneToAllDebuffs = true;
        public bool ImmuneToKnockback = true;
        public bool ImmuneToLava = true;
        public bool ImmuneToFallDamage = true;

        // 海洋：基于生命上限的生命恢复
        public int FuShiLifeRegenBonus = 0; // 额外生命恢复

        // 天空：雷击标记
        public Dictionary<int, int> MarkedEnemies = new Dictionary<int, int>(); // NPC索引 -> 标记层数
        public const int MaxMarkStacks = 5;
        public float DamageBonusPerMark = 0.25f; // 每层25%增伤
        public int MarkApplyTimer = 0;
        public const int MarkApplyInterval = 90; // 每1.5秒尝试标记
        public float MarkSpreadRange = 400f; // 标记传播范围

        // ====== 灾厄三泰坦 ======
        // 纷争：生命换算
        public int BonusHealthFromDefense => Player.statDefense * 3; // 每防御3点生命
        public int CombatTimeForRevive = 0;
        public const int ReviveRequirementTime = 60 * 30; // 战斗30秒获得一次
        public bool ExtraReviveAvailable = false;

        // 诡计：隐身
        public bool IsInvisible = false;
        public int InvisibleCooldown = 0;
        public const int InvisibleCooldownDuration = 13 * 60; // 13秒冷却
        public bool NextAttackBreaksInvis = false;
        public float InvisBreakDamageMultiplier = 4.0f; // 破隐400%伤害
        public int LastDownPressTime = 0;
        public bool WasDownPressed = false;
        public const int DoublePressWindow = 15; // 双击判定窗口

        // 死亡：复活
        public bool HasDeathImmunity = true; // 固定免疫一次死亡
        public bool DeathImmunityUsed = false;
        public float LifeStealPercent = 0.15f; // 攻击恢复15%伤害值
        public int ResurrectionCooldown = 60 * 60; // 60秒冷却
        public int LastResurrectionTime = -999;

        // ====== 生命回复溢出系统 ======
        public float OverflowHealing = 0f;
        public float OverflowToDamageRatio = 0.001f; // 每点溢出治疗增加0.1%伤害
        public float MaxOverflowDamageBonus = 1.00f; // 最高100%
        public float OverflowDamageBonus = 0f;

        // ====== 初始化 ======
        public override void Initialize()
        {
            ResetAll();
        }

        private void ResetAll()
        {
            IsFuShiActive = false;
            CombatTime = 0;
            EnemyDebuffStacks.Clear();
            OriginalMinionSlots = 0;
            FuShiMinionSlotsBonus = 0;
            TimeRewindAvailable = true;
            TimeRewindCurrentCooldown = 0;
            TimeSnapshots.Clear();
            DefenseDamageReduction = 0f;
            FuShiLifeRegenBonus = 0;
            MarkedEnemies.Clear();
            MarkApplyTimer = 0;
            CombatTimeForRevive = 0;
            ExtraReviveAvailable = false;
            IsInvisible = false;
            InvisibleCooldown = 0;
            NextAttackBreaksInvis = false;
            LastDownPressTime = 0;
            WasDownPressed = false;
            HasDeathImmunity = true;
            DeathImmunityUsed = false;
            OverflowHealing = 0f;
            OverflowDamageBonus = 0f;
        }

        // ====== 激活/停用 ======
        public void ActivateFuShi()
        {
            if (IsFuShiActive) return;

            IsFuShiActive = true;
            CombatTime = 0;

            // 记录原始召唤栏
            OriginalMinionSlots = Player.maxMinions;
            FuShiMinionSlotsBonus = OriginalMinionSlots; // 双倍召唤栏

            // 添加buff
            Player.AddBuff(FuShiBuffType, FuShiDuration);
        }

        public void DeactivateFuShi()
        {
            if (!IsFuShiActive) return;

            IsFuShiActive = false;

            // 清除buff
            Player.ClearBuff(FuShiBuffType);

            // 重置所有状态
            ResetAll();
        }

        // ====== 主更新循环 ======
        public override void PostUpdateEquips()
        {
            if (!IsFuShiActive) return;

            // 更新所有系统
            UpdateCombatTime();
            UpdateDebuffSystem();
            UpdateMinionSystem();
            UpdateTimeRewindSystem();
            UpdateMarkSystem();
            UpdateInvisibilitySystem();
            UpdateResurrectionSystem();
            UpdateOverflowSystem();

            // 应用所有加成
            ApplyAllBonuses();

            // 存储时间快照
            RecordSnapshot();
        }

        // ====== 战斗时长系统 ======
        private void UpdateCombatTime()
        {
            // 检查附近是否有敌人
            bool inCombat = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.lifeMax > 5 &&
                    !npc.dontTakeDamage && npc.type != NPCID.TargetDummy &&
                    Vector2.Distance(Player.Center, npc.Center) < 800f)
                {
                    inCombat = true;
                    break;
                }
            }

            if (inCombat)
            {
                CombatTime++;
                CombatTimeForRevive++;

                // 检查是否获得额外复活
                if (CombatTimeForRevive >= ReviveRequirementTime && !ExtraReviveAvailable)
                {
                    ExtraReviveAvailable = true;
                    CombatTimeForRevive = 0;
                }
            }
            else
            {
                // 非战斗状态缓慢减少战斗时长
                if (CombatTime > 0 && Main.GameUpdateCount % 60 == 0)
                {
                    CombatTime = Math.Max(0, CombatTime - 60); // 每秒减少1秒
                }
            }

            CombatTime = Math.Min(CombatTime, CombatTimeMax);
        }

        // ====== Debuff弱点击破系统 ======
        private void UpdateDebuffSystem()
        {
            DebuffApplyTimer++;

            if (DebuffApplyTimer >= DebuffApplyInterval)
            {
                // 尝试为附近敌人附加随机debuff
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && npc.lifeMax > 5 &&
                        !npc.dontTakeDamage && npc.type != NPCID.TargetDummy &&
                        Vector2.Distance(Player.Center, npc.Center) < 600f)
                    {
                        TryApplyRandomDebuff(npc);
                    }
                }

                DebuffApplyTimer = 0;
            }
        }

        private void TryApplyRandomDebuff(NPC npc)
        {
            // 随机选择一种debuff
            int debuffType = GetRandomDebuffType();
            if (debuffType == 0) return;

            // 检查是否免疫
            if (!IsDebuffEffective(npc, debuffType)) return;

            // 附加debuff
            int debuffTime = 180 + Main.rand.Next(180); // 3-6秒
            npc.AddBuff(debuffType, debuffTime);

            // 更新层数
            if (!EnemyDebuffStacks.ContainsKey(npc.whoAmI))
                EnemyDebuffStacks[npc.whoAmI] = 0;

            EnemyDebuffStacks[npc.whoAmI]++;
            EnemyDebuffStacks[npc.whoAmI] = Math.Min(EnemyDebuffStacks[npc.whoAmI], MaxDebuffStacks);

            LastAppliedDebuffType = debuffType;
        }

        private int GetRandomDebuffType()
        {
            // 原版debuff列表
            int[] vanillaDebuffs = new int[]
            {
                BuffID.Poisoned,
                BuffID.OnFire,
                BuffID.Frostburn,
                BuffID.CursedInferno,
                BuffID.ShadowFlame,
                BuffID.Venom,
                BuffID.Ichor,
                BuffID.Electrified,
                BuffID.Confused,
                BuffID.Slow,
                BuffID.Weak,
                BuffID.Silenced,
                BuffID.BrokenArmor,
                BuffID.Cursed,
                BuffID.Darkness,
                BuffID.Chilled,
                BuffID.WitheredArmor,
                BuffID.WitheredWeapon,
            };

            if (vanillaDebuffs.Length == 0) return 0;
            return vanillaDebuffs[Main.rand.Next(vanillaDebuffs.Length)];
        }

        private bool IsDebuffEffective(NPC npc, int debuffType)
        {
            // 简化处理，默认有效
            return true;
        }

        // ====== 召唤系统 ======
        private void UpdateMinionSystem()
        {
            // 更新召唤栏
            if (FuShiMinionSlotsBonus > 0)
            {
                Player.maxMinions = OriginalMinionSlots + FuShiMinionSlotsBonus;
                Player.maxMinions = Math.Min(Player.maxMinions, 100); // 上限100个
            }
        }

        // ====== 时间回溯系统 ======
        private void UpdateTimeRewindSystem()
        {
            if (TimeRewindCurrentCooldown > 0)
            {
                TimeRewindCurrentCooldown--;
                if (TimeRewindCurrentCooldown <= 0)
                {
                    TimeRewindAvailable = true;
                }
            }
        }

        private void RecordSnapshot()
        {
            if (TimeSnapshots.Count >= MaxSnapshots)
            {
                TimeSnapshots.Dequeue();
            }

            // 记录当前buff状态
            List<int> buffTypes = new List<int>();
            List<int> buffTimes = new List<int>();

            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                if (Player.buffType[i] > 0)
                {
                    buffTypes.Add(Player.buffType[i]);
                    buffTimes.Add(Player.buffTime[i]);
                }
            }

            PlayerSnapshot snapshot = new PlayerSnapshot
            {
                Position = Player.position,
                Velocity = Player.velocity,
                Life = Player.statLife,
                Mana = Player.statMana,
                BuffTypes = buffTypes.ToArray(),
                BuffTimes = buffTimes.ToArray()
            };

            TimeSnapshots.Enqueue(snapshot);
        }

        public void ActivateTimeRewind()
        {
            if (!TimeRewindAvailable || TimeSnapshots.Count < 30) return; // 至少需要0.5秒历史

            TimeRewindAvailable = false;
            TimeRewindCurrentCooldown = TimeRewindCooldown;

            // 获取1秒前的快照（60帧前）
            int targetFrame = Math.Max(0, TimeSnapshots.Count - 60);
            PlayerSnapshot? targetSnapshot = null;
            int currentIndex = 0;

            foreach (var snapshot in TimeSnapshots)
            {
                if (currentIndex == targetFrame)
                {
                    targetSnapshot = snapshot;
                    break;
                }
                currentIndex++;
            }

            if (targetSnapshot.HasValue)
            {
                PlayerSnapshot snapshot = targetSnapshot.Value;

                // 恢复状态
                Player.position = snapshot.Position;
                Player.velocity = snapshot.Velocity;
                Player.statLife = snapshot.Life;
                Player.statMana = snapshot.Mana;

                // 恢复buff - 先清除所有buff
                for (int i = Player.MaxBuffs - 1; i >= 0; i--)
                {
                    if (Player.buffType[i] > 0)
                    {
                        Player.DelBuff(i);
                    }
                }

                // 然后添加快照中的buff
                for (int i = 0; i < snapshot.BuffTypes.Length; i++)
                {
                    Player.AddBuff(snapshot.BuffTypes[i], snapshot.BuffTimes[i]);
                }

                // 刷新其他技能冷却
                RefreshOtherCooldowns();

                // 治疗效果
                int healAmount = (int)(Player.statLifeMax2 * 0.15f);
                Player.statLife = Math.Min(Player.statLife + healAmount, Player.statLifeMax2);
                Player.HealEffect(healAmount);

                SoundEngine.PlaySound(SoundID.Item8, Player.Center);
            }
        }

        private void RefreshOtherCooldowns()
        {
            // 刷新隐身冷却
            if (InvisibleCooldown > 0)
            {
                InvisibleCooldown = Math.Max(0, InvisibleCooldown - (13 * 30)); // 减少13秒的一半
            }
        }

        // ====== 标记系统 ======
        private void UpdateMarkSystem()
        {
            MarkApplyTimer++;

            if (MarkApplyTimer >= MarkApplyInterval)
            {
                // 寻找最近的敌人标记
                NPC closest = null;
                float closestDist = float.MaxValue;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && npc.lifeMax > 5 &&
                        !npc.dontTakeDamage && npc.type != NPCID.TargetDummy &&
                        Vector2.Distance(Player.Center, npc.Center) < 600f)
                    {
                        float dist = Vector2.Distance(Player.Center, npc.Center);
                        if (dist < closestDist)
                        {
                            closest = npc;
                            closestDist = dist;
                        }
                    }
                }

                if (closest != null)
                {
                    ApplyMark(closest);

                    // 传播标记到附近敌人
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.active && !npc.friendly && npc.whoAmI != closest.whoAmI &&
                            Vector2.Distance(closest.Center, npc.Center) < MarkSpreadRange)
                        {
                            if (Main.rand.NextFloat() < 0.3f) // 30%传播概率
                            {
                                ApplyMark(npc);
                            }
                        }
                    }
                }

                MarkApplyTimer = 0;
            }

            // 清理无效标记
            List<int> toRemove = new List<int>();
            foreach (var kvp in MarkedEnemies)
            {
                if (!Main.npc[kvp.Key].active || Main.npc[kvp.Key].life <= 0)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (int key in toRemove)
            {
                MarkedEnemies.Remove(key);
            }
        }

        private void ApplyMark(NPC npc)
        {
            if (!MarkedEnemies.ContainsKey(npc.whoAmI))
                MarkedEnemies[npc.whoAmI] = 0;

            MarkedEnemies[npc.whoAmI]++;
            MarkedEnemies[npc.whoAmI] = Math.Min(MarkedEnemies[npc.whoAmI], MaxMarkStacks);
        }

        // ====== 隐身系统 ======
        private void UpdateInvisibilitySystem()
        {
            if (InvisibleCooldown > 0)
            {
                InvisibleCooldown--;
            }

            UpdateInvisibilityInput();

            if (IsInvisible)
            {
                Player.invis = true;
                Player.immuneAlpha = Math.Max(Player.immuneAlpha, 100);
            }
        }

        private void UpdateInvisibilityInput()
        {
            bool isDownPressed = Player.controlDown;
            int currentTime = (int)Main.GameUpdateCount;

            if (isDownPressed && !WasDownPressed)
            {
                if (currentTime - LastDownPressTime < DoublePressWindow)
                {
                    // 双击下键
                    ToggleInvisibility();
                }

                LastDownPressTime = currentTime;
            }

            WasDownPressed = isDownPressed;
        }

        private void ToggleInvisibility()
        {
            if (InvisibleCooldown > 0) return;

            IsInvisible = !IsInvisible;

            if (IsInvisible)
            {
                InvisibleCooldown = InvisibleCooldownDuration;
                NextAttackBreaksInvis = true;

                SoundEngine.PlaySound(SoundID.Item8, Player.Center);
            }
            else
            {
                Player.invis = false;
                SoundEngine.PlaySound(SoundID.Item8, Player.Center);
            }
        }

        // ====== 复活系统 ======
        private void UpdateResurrectionSystem()
        {
            // 检查复活冷却
            int currentTime = (int)(Main.GameUpdateCount / 60);
            if (LastResurrectionTime > 0 && currentTime - LastResurrectionTime > ResurrectionCooldown)
            {
                HasDeathImmunity = true;
                DeathImmunityUsed = false;
            }
        }

        // ====== 溢出治疗系统 ======
        private void UpdateOverflowSystem()
        {
            // 溢出治疗转化为伤害加成
            if (OverflowHealing > 0)
            {
                OverflowDamageBonus = OverflowHealing * OverflowToDamageRatio;
                OverflowDamageBonus = Math.Min(OverflowDamageBonus, MaxOverflowDamageBonus);

                // 缓慢衰减
                if (Main.GameUpdateCount % 60 == 0) // 每秒衰减
                {
                    OverflowHealing *= 0.95f; // 衰减5%
                    if (OverflowHealing < 1f)
                    {
                        OverflowHealing = 0f;
                        OverflowDamageBonus = 0f;
                    }
                }
            }
        }

        // ====== 应用所有加成 ======
        private void ApplyAllBonuses()
        {
            // 创生三泰坦
            Player.GetDamage(DamageClass.Generic) += FuShiAllDamageBonus;
            Player.statDefense += FuShiDefenseBonus;

            // 浪漫：战斗时长加成
            Player.GetDamage(DamageClass.Generic) += CombatDamageBonus;
            Player.moveSpeed += CombatSpeedBonus;
            Player.GetCritChance(DamageClass.Generic) += CombatCritBonus;
            Player.statDefense += (int)CombatDefenseBonus;

            // 理性：debuff加成（在攻击时计算）

            // 门径：召唤物加成
            int minionCount = CountActiveMinions();
            float minionDamageBonus = Math.Min(minionCount * DamagePerMinion, MaxMinionDamageBonus);
            Player.GetDamage(DamageClass.Summon) += minionDamageBonus;

            // 律法：永久双倍伤害（在攻击时应用）

            // 大地：防御减伤（每点防御提供0.1%减伤，最高50%）
            if (Player.statDefense > 0)
            {
                DefenseDamageReduction = Math.Min(Player.statDefense * 0.001f, 0.50f);
                Player.endurance += DefenseDamageReduction;
            }

            // 海洋：基于生命上限的生命恢复（每秒恢复1.5%最大生命）
            FuShiLifeRegenBonus = (int)(Player.statLifeMax2 * 0.015f);
            Player.lifeRegen += FuShiLifeRegenBonus;

            // 天空：标记加成（在攻击时计算）

            // 纷争：生命加成
            Player.statLifeMax2 += BonusHealthFromDefense;

            // 溢出伤害加成
            Player.GetDamage(DamageClass.Generic) += OverflowDamageBonus;

            // 免疫效果
            ApplyImmunities();
        }

        private int CountActiveMinions()
        {
            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == Player.whoAmI &&
                    (proj.minion || proj.sentry) && proj.minionSlots > 0)
                {
                    count++;
                }
            }
            return count;
        }

        private void ApplyImmunities()
        {
            if (ImmuneToAllDebuffs)
            {
                // 免疫所有debuff
                for (int i = Player.MaxBuffs - 1; i >= 0; i--)
                {
                    if (Player.buffType[i] > 0 && Main.debuff[Player.buffType[i]])
                    {
                        Player.DelBuff(i);
                    }
                }
            }

            if (ImmuneToKnockback)
            {
                Player.noKnockback = true;
            }

            if (ImmuneToLava)
            {
                Player.lavaImmune = true;
                Player.lavaMax = 99999;
            }

            if (ImmuneToFallDamage)
            {
                Player.noFallDmg = true;
            }

            if (ImmuneToManaSickness && Player.HasBuff(BuffID.ManaSickness))
            {
                Player.ClearBuff(BuffID.ManaSickness);
            }
        }

        // ====== 伤害处理 ======
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!IsFuShiActive) return;

            // 反弹伤害
            if (FuShiDamageReflect > 0)
            {
                // 计算反弹的伤害
                float reflectedDamage = modifiers.FinalDamage.Flat * FuShiDamageReflect;
                if (reflectedDamage > 0)
                {
                    // 对周围敌人反弹伤害
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.active && !npc.friendly && npc.lifeMax > 5 &&
                            !npc.dontTakeDamage && npc.type != NPCID.TargetDummy &&
                            Vector2.Distance(Player.Center, npc.Center) < 300f)
                        {
                            npc.SimpleStrikeNPC((int)reflectedDamage, 0);
                        }
                    }
                }
            }
        }

        // ====== 攻击处理 ======
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!IsFuShiActive) return;

            // 理性：基于debuff层数的增伤
            if (EnemyDebuffStacks.ContainsKey(target.whoAmI))
            {
                int stacks = EnemyDebuffStacks[target.whoAmI];
                float damageBonus = stacks * DamagePerDebuffStack;
                modifiers.FinalDamage *= (1f + damageBonus);
            }

            // 律法：永久双倍伤害
            modifiers.FinalDamage *= PermanentDamageMultiplier;

            // 天空：基于标记的增伤
            if (MarkedEnemies.ContainsKey(target.whoAmI))
            {
                int markStacks = MarkedEnemies[target.whoAmI];
                float markBonus = markStacks * DamageBonusPerMark;
                modifiers.FinalDamage *= (1f + markBonus);

                // 清除标记（一次性消耗）
                MarkedEnemies.Remove(target.whoAmI);
            }

            // 诡计：破隐一击
            if (IsInvisible && NextAttackBreaksInvis)
            {
                modifiers.FinalDamage *= InvisBreakDamageMultiplier;
                NextAttackBreaksInvis = false;
                IsInvisible = false;
                Player.invis = false;

                SoundEngine.PlaySound(SoundID.Item8, target.Center);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!IsFuShiActive) return;

            // 死亡：生命偷取
            if (LifeStealPercent > 0)
            {
                int healAmount = (int)(damageDone * LifeStealPercent);
                if (healAmount > 0)
                {
                    Player.statLife += healAmount;
                    if (Player.statLife > Player.statLifeMax2)
                    {
                        int overflow = Player.statLife - Player.statLifeMax2;
                        Player.statLife = Player.statLifeMax2;
                        OverflowHealing += overflow;
                    }

                    Player.HealEffect(healAmount);
                }
            }

            // 理性：尝试附加新的debuff
            TryApplyRandomDebuff(target);
        }

        // ====== 死亡处理 ======
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (!IsFuShiActive) return true;

            // 死亡：固定免疫一次
            if (HasDeathImmunity && !DeathImmunityUsed)
            {
                TriggerDeathImmunity();
                DeathImmunityUsed = true;
                HasDeathImmunity = false;
                LastResurrectionTime = (int)(Main.GameUpdateCount / 60);
                return false;
            }

            // 纷争：额外复活
            if (ExtraReviveAvailable)
            {
                TriggerExtraRevive();
                ExtraReviveAvailable = false;
                CombatTimeForRevive = 0;
                return false;
            }

            return true;
        }

        private void TriggerDeathImmunity()
        {
            Player.statLife = Player.statLifeMax2 / 2;
            Player.immune = true;
            Player.immuneTime = 180;

            SoundEngine.PlaySound(SoundID.Item8, Player.Center);
        }

        private void TriggerExtraRevive()
        {
            Player.statLife = Player.statLifeMax2;
            Player.immune = true;
            Player.immuneTime = 240;

            // 清除负面效果
            ClearAllNegativeBuffs();

            SoundEngine.PlaySound(SoundID.Item4, Player.Center);
        }

        private void ClearAllNegativeBuffs()
        {
            for (int i = Player.MaxBuffs - 1; i >= 0; i--)
            {
                if (Player.buffType[i] > 0 && Main.debuff[Player.buffType[i]])
                {
                    Player.DelBuff(i);
                }
            }
        }

        // ====== 按键处理 ======
        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet)
        {
            if (!IsFuShiActive) return;

            // 检查Q键技能绑定
            if (Shenyupai_Keybinds.SkillQ?.JustPressed == true && TimeRewindAvailable)
            {
                ActivateTimeRewind();
            }
        }

        // ====== 数据保存 - 不保存负世之牌状态 ======
        public override void SaveData(TagCompound tag)
        {
            // 不保存负世之牌状态，确保退出游戏后清除
            // 如果需要保存，可以在这里添加
        }

        public override void LoadData(TagCompound tag)
        {
            // 不加载负世之牌状态，确保进入游戏时是未激活状态
            ResetAll();
        }
    }
}