using Amphoreus.Projectiles;
using Amphoreus.Systems;
using Amphoreus.Systems.Shenyupai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Amphoreus.Players.Shenyupai_Players
{
    public class Shenyupai_Player : ModPlayer
    {
        // ====== 核心系统字段 ======
        public Shenyupai_System.ShenyupaiBuff CurrentCard { get; set; }
        public long BuffStartTime { get; set; } = -1;
        public int BuffDuration { get; set; } = 60 * 60 * 24; // 24分钟游戏时间

        public bool HasActiveBuff => CurrentCard != null && BuffStartTime > 0 &&
                                    (Main.GameUpdateCount - BuffStartTime) < BuffDuration;
        public int BuffTimeLeft
        {
            get
            {
                if (!HasActiveBuff) return 0;
                long elapsed = Main.GameUpdateCount - BuffStartTime;
                long remainingTicks = BuffDuration - elapsed;
                return (int)(remainingTicks / 60);
            }
        }
        public string BuffTimeLeftFormatted
        {
            get
            {
                int seconds = BuffTimeLeft;
                int minutes = seconds / 60;
                seconds = seconds % 60;
                return $"{minutes:D2}:{seconds:D2}";
            }
        }

        // ====== 创生三泰坦 ======
        public bool hasFuShi = false; // 全世之座
        public float allDamageBonus = 0f;

        // ====== 浪漫牌速度系统字段 ======
        public bool hasLangMan = false; // 黄金之茧
        public float langManBaseMoveSpeedBonus = 0.30f; // 固定30%移速加成

        // 攻击计时系统
        public int langManAttackTimer = 0; // 每秒攻击计时器
        private const int LangManAttackWindow = 60; // 1秒窗口（60帧）
        public int langManHitsInWindow = 0; // 当前窗口内的命中次数

        // 层数系统（无上限）
        public int langManSpeedStacks = 0; // 当前速度层数
        public List<int> langManStackTimers = new List<int>(); // 每层独立的计时器
        private const int LangManStackDuration = 10 * 60; // 每层持续10秒（600帧）
        public float langManSpeedBonusPerStack = 0.05f; // 每层提供5%额外移速
        public float langManDamageBonusPerStack = 0.02f; // 每层提供2%伤害加成

        // 闪避系统
        public bool langManDodgeAvailable = false; // 闪避是否可用
        public int langManDodgeCooldown = 0; // 闪避冷却计时器
        public const int LangManDodgeCooldownDuration = 20 * 60; // 20秒冷却
        public bool langManDodgeActive = false; // 闪避是否激活（本次伤害免疫）
        public int langManDodgeActiveTimer = 0; // 闪避激活持续时间
        public const int LangManDodgeActiveDuration = 10; // 闪避持续10帧（约0.17秒）
        private bool langManDodgePromptShown = false; // 闪避提示是否已显示

        // 视觉效果
        public int langManVisualTimer = 0;
        private const int LangManVisualInterval = 5;

        public bool hasLiXing = false; // 分裂之枝
        public float rangedSpeedBonus = 0f;
        public int rangedCritBonus = 0;
        public float debuffChance = 0f;
        public float liXingDamageBonusPerDebuff = 0f; // 每层debuff增加的伤害
        public float liXingMaxDamageBonus = 0f; // 最大伤害加成
        private List<int> vanillaDebuffTypes = new List<int>(); // 原版debuff类型列表

        // ====== 命运三泰坦 ======
        public bool hasMenJing = false; // 万径之门
        public int minionSlotsBonus = 0;
        public float summonDamageBonus = 0f;
        public float perMinionBonus = 0f;

        // 门径技能：临时召唤栏系统
        public int originalMinionSlots = 0; // 按下技能时的原始召唤栏数量
        public int menJingExtraSlots = 0; // 额外增加的召唤栏
        public bool menJingSkillActive = false; // 技能是否激活
        public int menJingSkillTimer = 0; // 技能计时器
        public const int MenJingSkillDuration = 30 * 60; // 30秒持续时间
        public const int MenJingSkillVulnerableTime = 15 * 60; // 15秒后受击恢复

        public bool hasLvFa = false; // 公正之秤
        public float manaCostReduction = 0f;
        public int doubleDamageTimer = 0; // 双倍伤害计时器
        public const int DoubleDamageInterval = 15 * 60; // 15秒触发一次
        public const int DoubleDamageDuration = 5 * 60; // 持续5秒
        public bool isDoubleDamageActive = false; // 双倍伤害状态
        public int doubleDamageActiveTimer = 0; // 双倍伤害状态计时器

        public bool hasSuiYue = false; // 永夜之帷
        public int timeControlCooldown = 0;
        public int lastTimeControlUse = -999;
        public float survivalDamagePerMinute = 0f;
        public int survivalTime = 0;

        // 时间流逝系统
        public float timeFlowRate = 0.005f; // 每秒0.5%
        private int timeFlowTimer = 0;
        private const int TimeFlowInterval = 60; // 每秒检查一次

        // ====== 支柱三泰坦 ======
        public bool hasDaDi = false; // 磐岩之脊
        public float groundMoveSpeedBonus = 0f;

        // ====== 大地之泰坦专属字段 ======
        // 大地护盾系统 - 坦克型护盾
        public float currentShield = 0f;
        public float maxShield = 0f;
        public int shieldRefreshTimer = 0;
        public const int ShieldRefreshCooldown = 30 * 60; // 30秒
        public bool isShieldActive = false;
        public bool isShieldBroken = false;
        public int shieldBreakTimer = 0;

        // 护盾伤害加成和减伤
        public float damageBonusFromShield = 0f;
        public float damageReductionFromShield = 0f;
        public const float DamagePerShield = 0.002f; // 每点护盾增加0.2%伤害
        public const float ReductionPerShield = 0.002f; // 每点护盾增加0.2%减伤
        public const float MaxDamageBonus = 0.20f; // 最大20%伤害加成
        public const float MaxDamageReduction = 0.20f; // 最大20%减伤

        // 免疫系统
        public bool immuneToLava = true; // 免疫岩浆
        public bool immuneToPetrify = true; // 免疫石化
        public bool immuneToKnockback = true; // 免疫击退
        public bool immuneToFireBlocks = true; // 免疫火块
        public bool immuneToWeakness = true; // 免疫弱弱减益
        public bool immuneToBrokenArmor = true; // 免疫破甲减益

        public bool hasHaiYang = false; // 满溢之杯
        public float waterMoveSpeedBonus = 0f;
        public int waterLifeRegenBonus = 0;

        public bool hasTianKong = false; // 晨昏之眼
        public float flightSpeedBonus = 0f;
        public float wingHoverBonus = 0f;
        public float highAltitudeDamageBonus = 0f;

        // ====== 灾厄三泰坦 ======
        public bool hasFenZheng = false; // 天谴之矛
        public float meleeDamageBonus = 0f;
        public float meleeSpeedBonus = 0f;
        public int defenseToLifeConversion = 0;
        public int defenseToRegenConversion = 0;
        public int fenZhengDeathImmuneCooldown = 0;
        public int lastFenZhengDeathImmuneTime = -999;
        public bool tauntEnemies = false;
        public float damageTakenCap = 1f;
        public int fenZhengDamageDealt = 0;
        public int fenZhengDamageTimer = 0;
        public float fenZhengDamagePercent = 0f;
        public bool fenZhengCanTriggerDeathImmune = false;

        public bool hasGuiJi = false; // 翻飞之币
        public float moveSpeedBonus = 0f;
        public float itemMagnetRangeBonus = 0f;
        public bool isInvisible = false;

        public bool hasSiWang = false; // 灰黯之手
        public int deathImmunityCooldown = 0;
        public int lastDeathImmunityTime = -999;
        public float ghostHandChance = 0f;
        public float lifeStealAmount = 0f;
        public bool teamDamageTransfer = false;
        public bool deathImmunityUsed = false;

        // ====== 死亡牌核心系统：新蕊 ======
        public float xinRui = 0f;
        public const float XinRuiMax = 200f;
        public float XinRuiPercent => xinRui / XinRuiMax;
        public const float DamagePerXinRui = 0.0025f;
        public const float DamageReductionPerXinRui = 0.0025f;
        public const float RegenPerXinRui = 0.05f;
        public int xinRuiResurrectionCooldown = 30;
        public int lastXinRuiResurrectionTime = -999;
        public bool XinRuiResurrectionAvailable => xinRui >= 100f &&
            (int)(Main.GameUpdateCount / 60) - lastXinRuiResurrectionTime > xinRuiResurrectionCooldown;

        // 海洋之泰坦专属字段
        public float rainMoveSpeedBonus = 0.40f;
        public int waterCircleTimer = 0;
        public const int WaterCircleInterval = 300;
        public const float WaterCircleRadius = 200f;
        public int lastLeftPressTime = 0;
        public int lastRightPressTime = 0;

        // ====== 天空之泰坦专属字段 ======
        public bool isHovering = false;
        public int hoverTimer = 0;
        public const int MaxHoverTime = 300;
        public int healLightTimer = 0;
        public const int HealLightInterval = 600;
        public List<int> lockedTargets = new List<int>();
        public Dictionary<int, int> targetLockTimers = new Dictionary<int, int>();
        public const int LockDuration = 90;
        public int thunderStrikeTimer = 0;
        public const int ThunderStrikeInterval = 180;

        // ====== 诡计之泰坦专属字段 ======
        public int guiJiDownPressCount = 0;
        public int guiJiLastDownPressTime = 0;
        public bool guiJiWasControlDown = false;
        private const int DoublePressWindow = 15;
        public struct CoinDrop
        {
            public int CoinType;
            public int CoinValue;
            public float DropChance;
        }
        public List<CoinDrop> coinDrops = new List<CoinDrop>();
        public const float BaseCoinDropChance = 0.15f;
        public int totalCoinsDropped = 0;

        // ====== 死亡之泰坦专属字段 ======
        public int attackGhostHandTimer = 0;
        public int hurtGhostHandTimer = 0;
        public int autoGhostHandTimer = 0;
        public const int GhostHandInterval = 180;
        private int lastLifeValue = 0;
        private int lifeLossThisFrame = 0;
        private int lifeGainThisFrame = 0;
        private float overflowHealing = 0f;
        private const float overflowToRegenRatio = 0.1f;
        private const float overflowToDamageReductionRatio = 0.001f;
        private const float MaxRegenBonusFromOverflow = 10f;
        private const float MaxDamageReductionFromOverflow = 0.2f;
        private float currentRegenBonusFromOverflow = 0f;
        private float currentDamageReductionFromOverflow = 0f;
        private float damageBonusFromLifeLoss = 0f;

        // ====== 技能系统 ======
        public bool skillQReady = true;
        public int skillQCooldown = 0;
        private const int MaxCooldown = 60 * 60; // 60秒冷却

        // 时间记录系统
        public Queue<PlayerState> timeRecordQueue = new Queue<PlayerState>();
        private const int MaxTimeRecords = 5 * 60; // 5秒记录，每秒60帧

        // 玩家状态记录结构
        public struct PlayerState
        {
            public Vector2 Position;
            public int Life;
            public int Mana;
            public Vector2 Velocity;
            public long Timestamp;
        }

        // ====== 生命周期方法 ======
        public override void Initialize()
        {
            CurrentCard = null;
            BuffStartTime = -1;
            ResetAllBonuses();
            InitializeCoinDrops();
            InitializeVanillaDebuffList();
            lockedTargets = new List<int>();
            targetLockTimers = new Dictionary<int, int>();

            // 初始化浪漫牌系统
            langManAttackTimer = 0;
            langManHitsInWindow = 0;
            langManSpeedStacks = 0;
            langManStackTimers.Clear();
            langManDodgeAvailable = false;
            langManDodgeCooldown = 0;
            langManDodgeActive = false;
            langManDodgeActiveTimer = 0;
            langManVisualTimer = 0;

            // 初始化大地护盾系统
            currentShield = 0f;
            maxShield = 0f;
            shieldRefreshTimer = 0;
            isShieldActive = false;
            isShieldBroken = false;
            shieldBreakTimer = 0;
        }

        // 只初始化原版debuff列表
        private void InitializeVanillaDebuffList()
        {
            vanillaDebuffTypes.Clear();

            // 原版debuff
            int[] debuffs = new int[]
            {
                BuffID.Poisoned,
                BuffID.OnFire,
                BuffID.Frostburn,
                BuffID.CursedInferno,
                BuffID.ShadowFlame,
                BuffID.Venom,
                BuffID.Ichor,
                BuffID.BetsysCurse,
                BuffID.Electrified,
                BuffID.Oiled,
                BuffID.Wet,
                BuffID.Lovestruck,
                BuffID.Stinky,
                BuffID.Bleeding,
                BuffID.Confused,
                BuffID.Slow,
                BuffID.Weak,
                BuffID.Silenced,
                BuffID.BrokenArmor,
                BuffID.Cursed,
                BuffID.Darkness,
                BuffID.Chilled,
                BuffID.Frozen,
                BuffID.Webbed,
                BuffID.Midas,
                BuffID.Blackout,
                BuffID.Obstructed,
                BuffID.WitheredArmor,
                BuffID.WitheredWeapon,
                BuffID.Horrified,
                BuffID.TheTongue,
                BuffID.Stoned,
                BuffID.VortexDebuff,
                BuffID.Suffocation,
            };

            vanillaDebuffTypes.AddRange(debuffs);
        }

        public override void ResetEffects()
        {
            if (CurrentCard != null && !HasActiveBuff)
            {
                RemoveCurrentCardEffect();
            }
        }

        public override void UpdateBadLifeRegen()
        {
            if (hasDaDi && HasActiveBuff)
            {
                if (immuneToWeakness && Player.HasBuff(BuffID.Weak))
                {
                    Player.ClearBuff(BuffID.Weak);
                }

                if (immuneToBrokenArmor && Player.HasBuff(BuffID.BrokenArmor))
                {
                    Player.ClearBuff(BuffID.BrokenArmor);
                }

                if (immuneToPetrify && Player.HasBuff(BuffID.Stoned))
                {
                    Player.ClearBuff(BuffID.Stoned);
                    Player.stoned = false;
                }
            }

            // 律法：免疫魔力病
            if (hasLvFa && HasActiveBuff && Player.HasBuff(BuffID.ManaSickness))
            {
                Player.ClearBuff(BuffID.ManaSickness);
            }
        }

        public override void PostUpdateEquips()
        {
            ApplyBonuses();

            if (HasActiveBuff)
            {
                survivalTime++;
            }

            UpdateFenZhengDamage();
            HandleSpecialEffects();

            // 时间记录（岁月）
            if (hasSuiYue && HasActiveBuff)
            {
                RecordPlayerState();
                UpdateTimeFlow();
            }

            // 门径技能更新
            if (hasMenJing && HasActiveBuff)
            {
                UpdateMenJingSkill();
            }

            // 律法效果更新
            if (hasLvFa && HasActiveBuff)
            {
                UpdateLvFaEffects();
            }

            // 浪漫牌：更新速度层数和闪避系统
            if (hasLangMan && HasActiveBuff)
            {
                UpdateLangManEffects();
            }

            // 大地护盾更新
            if (hasDaDi && HasActiveBuff)
            {
                UpdateDragonShield();
            }

            if (hasSiWang && HasActiveBuff)
            {
                HandleSiWangEffects();

                if (xinRui > 0)
                {
                    float damageBonus = xinRui * DamagePerXinRui;
                    damageBonus = Math.Min(damageBonus, 0.5f);
                    Player.GetDamage(DamageClass.Generic) += damageBonus;

                    float regenBonus = xinRui * RegenPerXinRui;
                    regenBonus = Math.Min(regenBonus, 10f);
                    Player.lifeRegen += (int)regenBonus;

                    if (Main.rand.NextBool(180) && xinRui > 0)
                    {
                        Color dustColor = Color.Purple;
                        if (xinRui >= 100f) dustColor = Color.Magenta;
                        if (xinRui >= 150f) dustColor = Color.HotPink;

                        Dust.NewDustPerfect(
                            Player.Center + new Vector2(Main.rand.Next(-25, 26), -40),
                            DustID.PurpleTorch,
                            new Vector2(0, -0.3f),
                            150,
                            dustColor,
                            0.8f
                        );
                    }
                }

                UpdateGhostHandTimers();
            }

            if (hasHaiYang && HasActiveBuff)
            {
                HandleWaterCircle();
            }

            if (hasTianKong && HasActiveBuff)
            {
                HandleTianKongHover();
                HandleThunderStrikes();
            }

            if (hasGuiJi && HasActiveBuff)
            {
                HandleGuiJiInvisibility();
            }

            // 更新技能冷却
            UpdateSkillCooldowns();
        }

        // ====== 浪漫牌速度系统核心方法 ======
        private void UpdateLangManEffects()
        {
            // 更新每秒攻击计时器
            langManAttackTimer++;
            if (langManAttackTimer >= LangManAttackWindow)
            {
                // 每秒结算一次，根据命中次数添加层数
                if (langManHitsInWindow > 0)
                {
                    int stacksToAdd = CalculateStacksFromHits(langManHitsInWindow);
                    AddLangManStacks(stacksToAdd);
                }

                // 重置计数器和计时器
                langManHitsInWindow = 0;
                langManAttackTimer = 0;
            }

            // 更新层数计时器
            UpdateStackTimers();

            // 更新闪避冷却
            if (langManDodgeCooldown > 0)
            {
                langManDodgeCooldown--;
            }

            // 更新闪避激活时间
            if (langManDodgeActive)
            {
                langManDodgeActiveTimer--;

                if (langManDodgeActiveTimer <= 0)
                {
                    langManDodgeActive = false;

                    // 闪避结束特效
                    for (int i = 0; i < 10; i++)
                    {
                        Dust.NewDustPerfect(
                            Player.Center,
                            DustID.GoldFlame,
                            Main.rand.NextVector2Circular(4f, 4f),
                            100,
                            Color.Gold,
                            1.0f
                        );
                    }
                }
            }

            // 检查是否可以激活闪避
            if (langManSpeedStacks >= 10 && langManDodgeCooldown <= 0 && !langManDodgeActive)
            {
                if (!langManDodgeAvailable)
                {
                    langManDodgeAvailable = true;
                    langManDodgePromptShown = false;

                    // 闪避可用特效
                    for (int i = 0; i < 5; i++)
                    {
                        Dust.NewDustPerfect(
                            Player.Center,
                            DustID.GoldFlame,
                            Main.rand.NextVector2Circular(6f, 6f),
                            150,
                            Color.Orange,
                            1.2f
                        );
                    }
                }

                // 显示闪避提示（只显示一次）
                if (!langManDodgePromptShown && Main.GameUpdateCount % 30 == 0)
                {
                    langManDodgePromptShown = true;
                    CombatText.NewText(Player.getRect(), Color.Gold, "闪避就绪！", true);
                }
            }
            else
            {
                langManDodgeAvailable = false;
                langManDodgePromptShown = false;
            }

            // 更新视觉效果
            UpdateLangManVisuals();
        }

        // 根据每秒命中次数计算叠层数
        private int CalculateStacksFromHits(int hits)
        {
            // 每秒命中5次以上 = 3层
            if (hits >= 5) return 3;
            // 每秒命中3-4次 = 2层
            if (hits >= 3) return 2;
            // 每秒命中1-2次 = 1层
            if (hits >= 1) return 1;
            // 没命中 = 0层
            return 0;
        }

        // 添加指定数量的层数
        private void AddLangManStacks(int stacksToAdd)
        {
            for (int i = 0; i < stacksToAdd; i++)
            {
                langManStackTimers.Add(LangManStackDuration);
                langManSpeedStacks++;

                // 层数增加特效
                if (i == 0)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        Dust.NewDustPerfect(
                            Player.Center + new Vector2(0, -20),
                            DustID.GoldFlame,
                            Main.rand.NextVector2Circular(2f, 2f),
                            100,
                            Color.Gold,
                            0.6f
                        );
                    }
                }
            }
        }

        // 更新所有层数的计时器
        private void UpdateStackTimers()
        {
            for (int i = langManStackTimers.Count - 1; i >= 0; i--)
            {
                langManStackTimers[i]--;

                if (langManStackTimers[i] <= 0)
                {
                    langManStackTimers.RemoveAt(i);
                    langManSpeedStacks--;
                }
            }

            // 确保层数不为负
            if (langManSpeedStacks < 0) langManSpeedStacks = 0;
        }

        // 记录一次攻击命中
        public void RecordLangManHit()
        {
            if (!hasLangMan || !HasActiveBuff) return;

            langManHitsInWindow++;
        }

        // 激活闪避（在ModifyHurt中调用）
        private bool ActivateLangManDodge()
        {
            if (!langManDodgeAvailable || langManDodgeActive) return false;

            langManDodgeActive = true;
            langManDodgeActiveTimer = LangManDodgeActiveDuration;
            langManDodgeAvailable = false;
            langManDodgeCooldown = LangManDodgeCooldownDuration;

            // 清空所有层数
            langManStackTimers.Clear();
            langManSpeedStacks = 0;

            // 激活特效
            for (int i = 0; i < 30; i++)
            {
                Dust.NewDustPerfect(
                    Player.Center,
                    DustID.GoldFlame,
                    Main.rand.NextVector2Circular(10f, 10f),
                    150,
                    Color.Orange,
                    2.0f
                );
            }

            SoundEngine.PlaySound(SoundID.Item8, Player.Center);
            CombatText.NewText(Player.getRect(), Color.Gold, "闪避！", true);

            return true;
        }

        // 更新浪漫牌视觉效果
        private void UpdateLangManVisuals()
        {
            langManVisualTimer++;
            if (langManVisualTimer >= LangManVisualInterval)
            {
                langManVisualTimer = 0;

                if (langManSpeedStacks > 0)
                {
                    // 根据层数决定粒子密度和大小
                    int particleCount = Math.Min(langManSpeedStacks / 3, 5);
                    if (particleCount > 0)
                    {
                        for (int i = 0; i < particleCount; i++)
                        {
                            Color dustColor = Color.Gold;
                            if (langManSpeedStacks >= 10)
                                dustColor = Color.Orange;
                            else if (langManSpeedStacks >= 5)
                                dustColor = Color.Yellow;

                            float size = 0.4f + (langManSpeedStacks * 0.01f);
                            size = Math.Min(size, 1.0f);

                            Dust.NewDustPerfect(
                                Player.Center + Main.rand.NextVector2Circular(40f, 40f),
                                DustID.GoldFlame,
                                new Vector2(Player.direction * -0.2f, Main.rand.NextFloat(-0.5f, 0.5f)),
                                100,
                                dustColor,
                                size
                            );
                        }
                    }
                }
            }
        }

        // ====== 大地护盾系统方法 ======
        public void UpdateDragonShield()
        {
            if (!hasDaDi || !HasActiveBuff) return;

            // 计算最大护盾值：基础100点 + 每10点防御力增加50点护盾生命
            maxShield = 100f + (Player.statDefense / 10) * 50f;
            maxShield = Math.Max(maxShield, 100f);

            // 初始化护盾（如果没有护盾且没有破碎）
            if (currentShield <= 0 && !isShieldBroken)
            {
                currentShield = maxShield;
                isShieldActive = true;

                // 护盾激活特效
                if (Main.rand.NextBool(10))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Dust.NewDustPerfect(
                            Player.Center,
                            DustID.GoldFlame,
                            Main.rand.NextVector2Circular(3f, 3f),
                            100,
                            Color.Gold,
                            1f
                        );
                    }
                }
            }

            // 计算护盾加成效果（只在护盾未破碎时生效）
            if (isShieldActive && currentShield > 0)
            {
                // 基于当前护盾值计算伤害加成
                damageBonusFromShield = currentShield * DamagePerShield;
                damageBonusFromShield = Math.Min(damageBonusFromShield, MaxDamageBonus);

                // 基于当前护盾值计算减伤比例
                damageReductionFromShield = currentShield * ReductionPerShield;
                damageReductionFromShield = Math.Min(damageReductionFromShield, MaxDamageReduction);
            }
            else
            {
                // 护盾破碎或没有护盾时，没有加成
                damageBonusFromShield = 0f;
                damageReductionFromShield = 0f;
            }

            // 如果护盾已破碎，启动刷新计时器
            if (isShieldBroken)
            {
                shieldBreakTimer++;

                // 显示破碎倒计时
                if (Main.GameUpdateCount % 60 == 0) // 每秒显示一次
                {
                    int secondsLeft = (ShieldRefreshCooldown - shieldBreakTimer) / 60;
                    if (secondsLeft > 0)
                    {
                        CombatText.NewText(
                            Player.getRect(),
                            Color.Gray,
                            $"护盾恢复: {secondsLeft}s"
                        );
                    }
                }

                // 30秒后刷新护盾
                if (shieldBreakTimer >= ShieldRefreshCooldown)
                {
                    currentShield = maxShield;
                    isShieldBroken = false;
                    isShieldActive = true;
                    shieldBreakTimer = 0;

                    // 刷新特效
                    for (int i = 0; i < 10; i++)
                    {
                        Dust.NewDustPerfect(
                            Player.Center,
                            DustID.GoldFlame,
                            Main.rand.NextVector2Circular(3f, 3f),
                            100,
                            Color.Gold,
                            1f
                        );
                    }

                    SoundEngine.PlaySound(SoundID.Item37, Player.Center);
                    CombatText.NewText(Player.getRect(), Color.Gold, $"大地护盾已刷新: {(int)maxShield}点", true);
                }
            }
            else if (currentShield > 0 && currentShield < maxShield)
            {
                // 护盾未破碎且未满时，每10秒恢复10%护盾值
                shieldRefreshTimer++;

                // 每10秒 = 600帧恢复一次
                if (shieldRefreshTimer >= 600)
                {
                    float regenAmount = maxShield * 0.10f; // 10%最大护盾值
                    float oldShield = currentShield;
                    currentShield += regenAmount;
                    currentShield = Math.Min(currentShield, maxShield);
                    shieldRefreshTimer = 0;

                    // 显示恢复量
                    int recovered = (int)(currentShield - oldShield);
                    if (recovered > 0 && Main.rand.NextBool(5))
                    {
                        CombatText.NewText(
                            Player.getRect(),
                            Color.LightGreen,
                            $"护盾恢复: +{recovered}"
                        );
                    }

                    // 恢复特效
                    if (Main.rand.NextBool(3))
                    {
                        Dust.NewDustPerfect(
                            Player.Center,
                            DustID.GoldFlame,
                            Vector2.Zero,
                            100,
                            Color.Gold,
                            0.3f
                        );
                    }
                }
            }

            // 护盾特效 - 只在有护盾值时显示
            if (currentShield > 0 && Main.rand.NextBool(30))
            {
                Color dustColor = Color.Gold;
                if (currentShield < maxShield * 0.3f) dustColor = Color.OrangeRed; // 低护盾时变红色

                Dust.NewDustPerfect(
                    Player.Center + Main.rand.NextVector2Circular(50f, 50f),
                    DustID.GoldFlame,
                    Vector2.Zero,
                    100,
                    dustColor,
                    0.5f
                );
            }
        }

        // ====== 大地护盾伤害吸收方法 ======
        // 这是关键修复：在玩家受到伤害前处理护盾吸收
        public bool ProcessShieldDamage(int incomingDamage, out int actualDamage)
        {
            actualDamage = incomingDamage;

            if (!hasDaDi || !HasActiveBuff || currentShield <= 0)
                return false; // 没有护盾，不处理

            // 计算护盾能吸收的伤害
            int shieldAbsorption = (int)Math.Min(currentShield, incomingDamage);

            // 减少护盾值
            currentShield -= shieldAbsorption;

            // 计算实际伤害
            actualDamage = incomingDamage - shieldAbsorption;

            // 护盾受损特效
            for (int i = 0; i < 3; i++)
            {
                Dust.NewDustPerfect(
                    Player.Center,
                    DustID.GoldFlame,
                    Main.rand.NextVector2Circular(2f, 2f),
                    100,
                    Color.Orange,
                    0.8f
                );
            }

            // 如果护盾耗尽
            if (currentShield <= 0)
            {
                currentShield = 0;
                isShieldBroken = true;
                shieldBreakTimer = 0;

                // 护盾破碎特效
                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDustPerfect(
                        Player.Center,
                        DustID.Stone,
                        Main.rand.NextVector2Circular(4f, 4f),
                        100,
                        Color.Gray,
                        1.2f
                    );
                }

                SoundEngine.PlaySound(SoundID.Item27, Player.Center);
            }

            // 如果护盾完全吸收了伤害，播放特殊音效
            if (actualDamage <= 0)
            {
                SoundEngine.PlaySound(SoundID.Item35, Player.Center);
            }

            return true; // 护盾处理了伤害
        }

        // ====== 按键处理系统 ======
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (!HasActiveBuff) return;

            // 检查键位绑定类是否存在
            if (Shenyupai_Keybinds.SkillQ == null) return;

            // Q键技能检测
            if (Shenyupai_Keybinds.SkillQ.JustPressed && skillQReady)
            {
                if (hasMenJing)
                {
                    // 门径：临时增加召唤栏并召唤额外召唤物
                    ActivateMinionSlots();
                    skillQReady = false;
                    skillQCooldown = 60 * 60;
                }
                else if (hasSuiYue)
                {
                    // 岁月：时间回溯
                    TimeRewind();
                    skillQReady = false;
                    skillQCooldown = 15 * 60;
                }
                else if (hasLvFa)
                {
                    // 律法：交换生命与魔力
                    SwapLifeMana();
                    skillQReady = false;
                    skillQCooldown = 15 * 60;
                }
            }
        }

        private void UpdateSkillCooldowns()
        {
            if (skillQCooldown > 0)
            {
                skillQCooldown--;
                if (skillQCooldown <= 0)
                {
                    skillQReady = true;
                }
            }
        }

        // ====== 命运三泰坦核心实现 ======

        // 门径：临时增加召唤栏并召唤额外召唤物
        private void ActivateMinionSlots()
        {
            if (menJingSkillActive) return;

            // 记录按下技能时的原始召唤栏数量
            originalMinionSlots = Player.maxMinions;

            // 计算增加的召唤栏（翻倍，但不超过1000）
            int newMinionSlots = originalMinionSlots * 2;
            if (newMinionSlots > 1000) newMinionSlots = 1000;

            menJingExtraSlots = newMinionSlots - originalMinionSlots;

            // 激活技能
            menJingSkillActive = true;
            menJingSkillTimer = MenJingSkillDuration;

            // 找到背包中第一根召唤杖（排除鞭子）
            Item summonWeapon = FindFirstSummonWeapon();

            if (summonWeapon != null && summonWeapon.shoot > 0)
            {
                // 召唤额外召唤物（数量等于原始召唤栏数量）
                for (int i = 0; i < originalMinionSlots; i++)
                {
                    SummonMinionFromWeapon(summonWeapon);
                }

                // 视觉效果
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDustPerfect(
                        Player.Center,
                        DustID.PurpleTorch,
                        Main.rand.NextVector2Circular(5f, 5f),
                        150,
                        Color.Purple,
                        1.5f
                    );
                }

                SoundEngine.PlaySound(SoundID.Item8, Player.Center);
            }
            else
            {
                DeactivateMenJingSkill();
                skillQReady = true;
            }
        }

        // 查找背包中第一根召唤杖（排除鞭子）
        private Item FindFirstSummonWeapon()
        {
            // 检查手持物品
            if (Player.HeldItem != null && Player.HeldItem.damage > 0 &&
                Player.HeldItem.CountsAsClass(DamageClass.Summon))
            {
                // 排除鞭子
                if (!IsWhip(Player.HeldItem) && Player.HeldItem.shoot > 0)
                {
                    return Player.HeldItem;
                }
            }

            // 检查背包（从上到下，从左到右）
            for (int i = 0; i < 50; i++)
            {
                if (i < Player.inventory.Length)
                {
                    Item item = Player.inventory[i];
                    if (item != null && item.active && item.damage > 0 &&
                        item.CountsAsClass(DamageClass.Summon) && item.shoot > 0)
                    {
                        // 排除鞭子
                        if (!IsWhip(item))
                        {
                            return item;
                        }
                    }
                }
            }

            return null;
        }

        // 判断是否为鞭子
        private bool IsWhip(Item item)
        {
            if (item == null || !item.active) return false;

            // 检查名称
            if (item.Name != null)
            {
                string nameLower = item.Name.ToLower();
                if (nameLower.Contains("whip") || nameLower.Contains("鞭"))
                {
                    return true;
                }
            }

            // 检查原版鞭子ID
            int[] whipItemIDs = {
                ItemID.ThornWhip, ItemID.BoneWhip, ItemID.FireWhip,
                ItemID.CoolWhip, ItemID.RainbowWhip, ItemID.SwordWhip,
                ItemID.MaceWhip
            };

            if (whipItemIDs.Contains(item.type))
            {
                return true;
            }

            return false;
        }

        // 使用召唤杖召唤召唤物
        private void SummonMinionFromWeapon(Item summonWeapon)
        {
            Vector2 spawnPosition = Player.Center + Main.rand.NextVector2Circular(100f, 50f);

            // 计算伤害（基于武器伤害和玩家属性）
            int damage = (int)(summonWeapon.damage * Player.GetDamage(DamageClass.Summon).Multiplicative);

            int proj = Projectile.NewProjectile(
                Player.GetSource_FromThis("MenJing_Skill"),
                spawnPosition,
                Vector2.Zero,
                summonWeapon.shoot,
                damage,
                summonWeapon.knockBack,
                Player.whoAmI
            );

            if (proj >= 0 && proj < Main.maxProjectiles)
            {
                Projectile projectile = Main.projectile[proj];

                // 确保是召唤物
                projectile.minion = true;
                projectile.minionSlots = 1;
                projectile.netUpdate = true;

                // 添加召唤特效
                for (int i = 0; i < 3; i++)
                {
                    Dust.NewDustPerfect(
                        spawnPosition,
                        DustID.PurpleTorch,
                        Main.rand.NextVector2Circular(3f, 3f),
                        150,
                        Color.Purple,
                        0.8f
                    );
                }
            }
        }

        // 更新门径技能状态
        private void UpdateMenJingSkill()
        {
            if (menJingSkillActive)
            {
                menJingSkillTimer--;

                // 技能时间结束
                if (menJingSkillTimer <= 0)
                {
                    DeactivateMenJingSkill();
                }
            }
        }

        // 停用门径技能
        private void DeactivateMenJingSkill()
        {
            if (!menJingSkillActive) return;

            menJingSkillActive = false;
            menJingExtraSlots = 0;
            menJingSkillTimer = 0;

            // 技能结束特效
            for (int i = 0; i < 15; i++)
            {
                Dust.NewDustPerfect(
                    Player.Center,
                    DustID.PurpleTorch,
                    Main.rand.NextVector2Circular(4f, 4f),
                    150,
                    Color.Purple,
                    1.2f
                );
            }
        }

        // ====== 律法之泰坦效果 ======

        // 更新律法效果
        private void UpdateLvFaEffects()
        {
            // 更新双倍伤害计时器
            doubleDamageTimer++;

            // 每15秒触发一次双倍伤害
            if (doubleDamageTimer >= DoubleDamageInterval && !isDoubleDamageActive)
            {
                isDoubleDamageActive = true;
                doubleDamageActiveTimer = DoubleDamageDuration;
                doubleDamageTimer = 0;

                // 双倍伤害激活效果
                for (int i = 0; i < 15; i++)
                {
                    Dust.NewDustPerfect(
                        Player.Center,
                        DustID.GoldFlame,
                        Main.rand.NextVector2Circular(4f, 4f),
                        150,
                        Color.Gold,
                        1.5f
                    );
                }

                SoundEngine.PlaySound(SoundID.Item8, Player.Center);
                CombatText.NewText(Player.getRect(), Color.Gold, "双倍伤害激活！持续5秒", true);
            }

            // 更新双倍伤害状态计时器
            if (isDoubleDamageActive)
            {
                doubleDamageActiveTimer--;

                // 双倍伤害状态结束
                if (doubleDamageActiveTimer <= 0)
                {
                    isDoubleDamageActive = false;
                    doubleDamageActiveTimer = 0;

                    // 结束效果
                    for (int i = 0; i < 10; i++)
                    {
                        Dust.NewDustPerfect(
                            Player.Center,
                            DustID.GoldFlame,
                            Main.rand.NextVector2Circular(3f, 3f),
                            150,
                            Color.Gold,
                            1.2f
                        );
                    }

                    CombatText.NewText(Player.getRect(), Color.Gold, "双倍伤害结束", true);
                }

                // 双倍伤害状态特效
                if (Main.rand.NextBool(20))
                {
                    Dust.NewDustPerfect(
                        Player.Center + new Vector2(Main.rand.Next(-30, 31), Main.rand.Next(-30, 31)),
                        DustID.GoldFlame,
                        Vector2.Zero,
                        150,
                        Color.Gold,
                        0.8f
                    );
                }
            }
        }

        // 律法：交换生命与魔力（Q技能）
        private void SwapLifeMana()
        {
            float lifePercent = (float)Player.statLife / Player.statLifeMax2;
            float manaPercent = (float)Player.statMana / Player.statManaMax2;

            // 计算交换后的数值
            int newLife = (int)(Player.statLifeMax2 * manaPercent);
            int newMana = (int)(Player.statManaMax2 * lifePercent);

            // 设置新值
            Player.statLife = Math.Max(1, newLife);
            Player.statMana = newMana;

            // 视觉效果
            for (int i = 0; i < 25; i++)
            {
                Dust.NewDustPerfect(
                    Player.Center,
                    DustID.GoldFlame,
                    Main.rand.NextVector2Circular(6f, 6f),
                    150,
                    Color.Gold,
                    1.8f
                );
            }

            SoundEngine.PlaySound(SoundID.Item8, Player.Center);
        }

        // 岁月：时间记录系统
        private void RecordPlayerState()
        {
            if (timeRecordQueue.Count >= MaxTimeRecords)
            {
                timeRecordQueue.Dequeue();
            }

            // 记录当前状态
            PlayerState state = new PlayerState
            {
                Position = Player.position,
                Life = Player.statLife,
                Mana = Player.statMana,
                Velocity = Player.velocity,
                Timestamp = Main.GameUpdateCount
            };

            timeRecordQueue.Enqueue(state);
        }

        private void UpdateTimeFlow()
        {
            // 检查是否在战斗状态（附近有敌人）
            bool inCombat = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.lifeMax > 5 &&
                    Vector2.Distance(Player.Center, npc.Center) < 800f)
                {
                    inCombat = true;
                    break;
                }
            }

            if (inCombat)
            {
                timeFlowTimer++;

                if (timeFlowTimer >= TimeFlowInterval)
                {
                    // 固定流逝生命
                    int lifeLoss = (int)(Player.statLifeMax2 * timeFlowRate);
                    lifeLoss = Math.Max(1, lifeLoss);

                    Player.statLife -= lifeLoss;
                    if (Player.statLife < 1) Player.statLife = 1;

                    // 显示流逝效果
                    if (Main.rand.NextBool(3))
                    {
                        Dust.NewDustPerfect(
                            Player.Center + new Vector2(0, -30),
                            DustID.SandstormInABottle,
                            new Vector2(0, -1f) * Main.rand.NextFloat(1f, 2f),
                            100,
                            Color.SandyBrown,
                            0.8f
                        );
                    }

                    timeFlowTimer = 0;
                }
            }
            else
            {
                // 非战斗状态重置计时器
                timeFlowTimer = 0;
            }

            // 逐渐恢复正常流逝速率
            if (timeFlowRate > 0.005f)
            {
                timeFlowRate -= 0.0001f;
                timeFlowRate = Math.Max(timeFlowRate, 0.005f);
            }
        }

        // 岁月：时间回溯
        private void TimeRewind()
        {
            if (timeRecordQueue.Count == 0) return;

            // 获取5秒前的状态（300帧前）
            long targetTime = Main.GameUpdateCount - 300;
            PlayerState? targetState = null;

            foreach (var state in timeRecordQueue)
            {
                if (state.Timestamp <= targetTime)
                {
                    targetState = state;
                    break;
                }
            }

            if (targetState.HasValue)
            {
                PlayerState state = targetState.Value;

                // 恢复状态
                Player.position = state.Position;
                Player.statLife = state.Life;
                Player.statMana = state.Mana;
                Player.velocity = state.Velocity;

                // 额外恢复生命值（15%最大生命）
                int healAmount = (int)(Player.statLifeMax2 * 0.15f);
                Player.statLife = Math.Min(Player.statLife + healAmount, Player.statLifeMax2);
                Player.HealEffect(healAmount);

                // 视觉效果
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDustPerfect(
                        Player.Center,
                        DustID.SandstormInABottle,
                        Main.rand.NextVector2Circular(6f, 6f),
                        150,
                        Color.SandyBrown,
                        1.5f
                    );
                }

                SoundEngine.PlaySound(SoundID.Item8, Player.Center);
            }
        }

        // 获取出生点
        public Vector2 GetSpawnPoint()
        {
            // 获取玩家出生点
            Vector2 spawnPoint = new Vector2(
                Player.SpawnX * 16f + 8f,
                Player.SpawnY * 16f - 16f
            );

            // 如果设置了床，使用床的位置
            if (Player.SpawnX >= 0 && Player.SpawnY >= 0)
            {
                return spawnPoint;
            }

            // 否则使用世界出生点
            return new Vector2(Main.spawnTileX * 16f, Main.spawnTileY * 16f);
        }

        // ====== 核心应用加成方法 ======
        private void ApplyBonuses()
        {
            if (!HasActiveBuff) return;

            // 创生三泰坦
            if (hasFuShi)
            {
                Player.GetDamage(DamageClass.Generic) += allDamageBonus;
            }

            if (hasLangMan)
            {
                // 应用基础移速加成
                Player.moveSpeed += langManBaseMoveSpeedBonus;

                // 应用层数移速加成
                float speedBonus = langManSpeedStacks * langManSpeedBonusPerStack;
                Player.moveSpeed += speedBonus;

                // 应用层数伤害加成
                float damageBonus = langManSpeedStacks * langManDamageBonusPerStack;
                Player.GetDamage(DamageClass.Generic) += damageBonus;
            }

            if (hasLiXing)
            {
                Player.GetAttackSpeed(DamageClass.Ranged) += rangedSpeedBonus;
                Player.GetCritChance(DamageClass.Ranged) += rangedCritBonus;
            }

            // 命运三泰坦
            if (hasMenJing)
            {
                // 计算所有召唤物数量（包括技能召唤的）
                int minionCount = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.owner == Player.whoAmI &&
                        (proj.minion || proj.sentry) && proj.minionSlots > 0)
                    {
                        minionCount++;
                    }
                }

                // 每个召唤物增加1%伤害，上限1000%
                float damageBonus = Math.Min(minionCount * 0.01f, 10.0f);
                Player.GetDamage(DamageClass.Summon) += damageBonus;

                // 临时增加召唤栏
                if (menJingSkillActive)
                {
                    int targetMinions = originalMinionSlots + menJingExtraSlots;
                    if (targetMinions > 1000) targetMinions = 1000;
                    Player.maxMinions = targetMinions;
                }
            }

            if (hasLvFa)
            {
                Player.manaCost -= manaCostReduction;
            }

            if (hasSuiYue)
            {
                int minutesAlive = survivalTime / 3600;
                float bonus = Math.Min(minutesAlive * survivalDamagePerMinute, 0.15f);
                Player.GetDamage(DamageClass.Generic) += bonus;

                // 低生命伤害加成
                float lifePercent = (float)Player.statLife / Player.statLifeMax2;
                float damageBonus = (1f - lifePercent) * 0.8f;
                damageBonus = Math.Min(damageBonus, 0.5f);
                Player.GetDamage(DamageClass.Generic) += damageBonus;
            }

            // 支柱三泰坦
            if (hasDaDi)
            {
                ApplyDaDiBonuses();
            }

            if (hasHaiYang)
            {
                ApplyHaiYangBonuses();
            }

            if (hasTianKong)
            {
                ApplyTianKongBonuses();
            }

            // 灾厄三泰坦
            if (hasFenZheng)
            {
                ApplyFenZhengBonuses();
            }

            if (hasGuiJi)
            {
                ApplyGuiJiBonuses();
            }

            if (hasSiWang)
            {
                Player.GetDamage(DamageClass.Generic) += damageBonusFromLifeLoss;
            }
        }

        // ====== 大地之泰坦专属方法 ======
        private void ApplyDaDiBonuses()
        {
            if (Player.velocity.Y == 0)
            {
                Player.moveSpeed += groundMoveSpeedBonus;
            }

            if (immuneToKnockback)
            {
                Player.noKnockback = true;
            }

            if (immuneToLava)
            {
                Player.lavaImmune = true;
                Player.lavaMax = 99999;
            }

            if (immuneToFireBlocks && Player.hurtCooldowns[0] <= 0)
            {
                Point tilePos = Player.Center.ToTileCoordinates();
                Tile tile = Main.tile[tilePos.X, tilePos.Y];
                if (tile != null && tile.HasTile && Main.tileSolid[tile.TileType])
                {
                    if (tile.TileType == TileID.Hellstone ||
                        tile.TileType == TileID.ObsidianBrick ||
                        tile.TileType == TileID.HellstoneBrick)
                    {
                        Player.immune = true;
                        Player.immuneTime = 2;
                    }
                }
            }
            // 应用护盾加成效果
            if (damageBonusFromShield > 0)
            {
                Player.GetDamage(DamageClass.Generic) += damageBonusFromShield;
            }
        }

        // ====== 海洋之泰坦专属方法 ======
        private void ApplyHaiYangBonuses()
        {
            bool inWater = Player.wet;
            bool inRain = Main.raining;

            if (inWater)
            {
                Player.moveSpeed += waterMoveSpeedBonus;
                Player.lifeRegen += waterLifeRegenBonus;
                Player.maxRunSpeed *= 1.5f;
                Player.runAcceleration *= 1.5f;
                Player.gills = true;
                Player.ignoreWater = true;
            }

            if (inRain)
            {
                Player.moveSpeed += rainMoveSpeedBonus;
                Player.lifeRegen += waterLifeRegenBonus / 2;
            }

            if (inWater || inRain)
            {
                Player.GetDamage(DamageClass.Generic) += 0.5f;
            }
        }

        private void HandleWaterCircle()
        {
            waterCircleTimer++;

            if (waterCircleTimer >= WaterCircleInterval)
            {
                CreateWaterCircle();
                waterCircleTimer = 0;
            }
        }

        private void CreateWaterCircle()
        {
            for (int i = 0; i < 30; i++)
            {
                Dust.NewDustPerfect(
                    Player.Center,
                    DustID.Water,
                    Main.rand.NextVector2Circular(WaterCircleRadius, WaterCircleRadius),
                    100,
                    Color.Cyan,
                    1.5f
                );
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player otherPlayer = Main.player[i];
                if (otherPlayer.active && !otherPlayer.dead &&
                    Vector2.Distance(Player.Center, otherPlayer.Center) < WaterCircleRadius)
                {
                    int healAmount = otherPlayer.statLifeMax2 / 20;
                    healAmount = Math.Max(healAmount, 1);

                    otherPlayer.statLife += healAmount;
                    if (otherPlayer.statLife > otherPlayer.statLifeMax2)
                        otherPlayer.statLife = otherPlayer.statLifeMax2;

                    otherPlayer.HealEffect(healAmount);

                    if (i == Player.whoAmI)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            Dust.NewDustPerfect(
                                otherPlayer.Center,
                                DustID.HealingPlus,
                                Main.rand.NextVector2Circular(2f, 2f),
                                100,
                                Color.LightBlue,
                                0.8f
                            );
                        }
                    }
                }
            }

            SoundEngine.PlaySound(SoundID.Item21, Player.Center);
        }

        // ====== 天空之泰坦专属方法 ======
        private void ApplyTianKongBonuses()
        {
            Player.wingTimeMax = (int)(Player.wingTimeMax * (1 + wingHoverBonus));
            Player.wingTime = Player.wingTimeMax;

            if (Player.position.Y < Main.worldSurface * 16 - 300 * 16)
            {
                Player.GetDamage(DamageClass.Generic) += highAltitudeDamageBonus;
            }

            AddSkyFieldEffects();
        }

        private void HandleTianKongHover()
        {
            bool spacePressed = Player.controlJump;
            bool downPressed = Player.controlDown;

            if (spacePressed && downPressed && !Player.mount.Active)
            {
                isHovering = true;
                hoverTimer++;
                Player.velocity.Y = Math.Min(Player.velocity.Y, 0.5f);
                Player.gravity = 0.1f;

                if (Main.rand.NextBool(5))
                {
                    Dust.NewDustPerfect(
                        Player.Center + new Vector2(Main.rand.Next(-20, 21), 20),
                        DustID.Cloud,
                        new Vector2(0, -0.5f),
                        100,
                        Color.White,
                        0.8f
                    );
                }

                if (hoverTimer >= MaxHoverTime)
                {
                    StopHovering();
                }
            }
            else if (isHovering)
            {
                StopHovering();
            }
        }

        private void StopHovering()
        {
            isHovering = false;
            hoverTimer = 0;
            Player.gravity = Player.defaultGravity;
        }

        private void HandleThunderStrikes()
        {
            if (!Main.raining) return;

            thunderStrikeTimer++;

            if (thunderStrikeTimer >= ThunderStrikeInterval)
            {
                LockRandomTargets(3);
                thunderStrikeTimer = 0;
            }

            UpdateLockedTargets();
        }

        private void LockRandomTargets(int count)
        {
            lockedTargets.RemoveAll(id => !Main.npc[id].active);

            List<NPC> availableTargets = new List<NPC>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.lifeMax > 5 &&
                    !npc.dontTakeDamage && npc.type != NPCID.TargetDummy &&
                    Vector2.Distance(Player.Center, npc.Center) < 600f &&
                    !lockedTargets.Contains(i))
                {
                    availableTargets.Add(npc);
                }
            }

            int targetsToLock = Math.Min(count, availableTargets.Count);
            for (int i = 0; i < targetsToLock; i++)
            {
                NPC target = availableTargets[Main.rand.Next(availableTargets.Count)];
                lockedTargets.Add(target.whoAmI);
                targetLockTimers[target.whoAmI] = 0;

                for (int j = 0; j < 5; j++)
                {
                    Dust.NewDustPerfect(
                        target.Center + new Vector2(0, -30),
                        DustID.Electric,
                        Main.rand.NextVector2Circular(2f, 2f),
                        100,
                        Color.Yellow,
                        1f
                    );
                }

                availableTargets.Remove(target);
            }
        }

        private void UpdateLockedTargets()
        {
            List<int> targetsToRemove = new List<int>();

            foreach (int targetId in lockedTargets)
            {
                if (targetLockTimers.ContainsKey(targetId))
                {
                    targetLockTimers[targetId]++;

                    NPC target = Main.npc[targetId];
                    if (target.active && Main.rand.NextBool(15))
                    {
                        Dust.NewDustPerfect(
                            target.Center + new Vector2(0, -20),
                            DustID.Electric,
                            Vector2.Zero,
                            100,
                            Color.Yellow,
                            0.6f
                        );
                    }

                    if (targetLockTimers[targetId] >= LockDuration)
                    {
                        targetsToRemove.Add(targetId);
                    }
                }
            }

            foreach (int targetId in targetsToRemove)
            {
                lockedTargets.Remove(targetId);
                targetLockTimers.Remove(targetId);
            }
        }

        private void AddSkyFieldEffects()
        {
            if (Main.raining)
            {
                Player.GetCritChance(DamageClass.Generic) += 10;

                if (Main.rand.NextBool(100))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Dust.NewDustPerfect(
                            Player.Center + Main.rand.NextVector2Circular(100f, 50f),
                            DustID.Electric,
                            Vector2.Zero,
                            100,
                            Color.Yellow,
                            0.5f
                        );
                    }
                }
            }
            else
            {
                Player.GetDamage(DamageClass.Generic) += 0.20f;
                Player.lifeRegen += 5;

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player otherPlayer = Main.player[i];
                    if (otherPlayer.active && !otherPlayer.dead && otherPlayer.whoAmI != Player.whoAmI &&
                        Player.team == otherPlayer.team && Player.team != 0 &&
                        Vector2.Distance(Player.Center, otherPlayer.Center) < 800f)
                    {
                        otherPlayer.lifeRegen += 3;
                    }
                }

                if (Main.rand.NextBool(100))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Dust.NewDustPerfect(
                            Player.Center + Main.rand.NextVector2Circular(100f, 100f),
                            DustID.GoldFlame,
                            new Vector2(0, -0.5f),
                            100,
                            Color.Gold,
                            0.5f
                        );
                    }
                }
            }
        }

        // ====== 纷争之泰坦专属方法 ======
        private void ApplyFenZhengBonuses()
        {
            Player.GetDamage(DamageClass.Melee) += meleeDamageBonus;
            Player.GetAttackSpeed(DamageClass.Melee) += meleeSpeedBonus;

            int bonusLife = Player.statDefense * defenseToLifeConversion;
            Player.statLifeMax2 += bonusLife;
            Player.lifeRegen += Player.statDefense * defenseToRegenConversion;

            if (tauntEnemies)
            {
                Player.aggro += 1000;
            }
        }

        private void UpdateFenZhengDamage()
        {
            if (!hasFenZheng || !HasActiveBuff) return;

            fenZhengDamageTimer++;

            if (fenZhengDamageTimer >= 60)
            {
                if (Player.statLifeMax2 > 0)
                {
                    fenZhengDamagePercent = (float)fenZhengDamageDealt / Player.statLifeMax2;
                    if (fenZhengDamagePercent >= 2.0f)
                    {
                        fenZhengCanTriggerDeathImmune = true;
                    }
                }

                fenZhengDamageDealt = 0;
                fenZhengDamageTimer = 0;
            }
        }

        // ====== 诡计之泰坦专属方法 ======
        private void ApplyGuiJiBonuses()
        {
            Player.moveSpeed += moveSpeedBonus;

            if (isInvisible)
            {
                Player.invis = true;
                Player.immuneAlpha = Math.Max(Player.immuneAlpha, 150);

                if (!IsBossNearby())
                {
                    Player.aggro -= 1000;
                }
            }
        }

        private void HandleGuiJiInvisibility()
        {
            if (!hasGuiJi || !HasActiveBuff) return;

            int currentTime = (int)Main.GameUpdateCount;
            bool isControlDown = Player.controlDown;

            if (isControlDown && !guiJiWasControlDown)
            {
                if (currentTime - guiJiLastDownPressTime < DoublePressWindow)
                {
                    guiJiDownPressCount++;

                    if (guiJiDownPressCount >= 2)
                    {
                        isInvisible = !isInvisible;

                        if (isInvisible)
                        {
                            for (int i = 0; i < 15; i++)
                            {
                                Dust.NewDustPerfect(
                                    Player.Center,
                                    DustID.Shadowflame,
                                    Main.rand.NextVector2Circular(3f, 3f),
                                    150,
                                    default,
                                    1.5f
                                );
                            }

                            SoundEngine.PlaySound(SoundID.Item8, Player.Center);
                        }
                        else
                        {
                            Player.invis = false;

                            for (int i = 0; i < 10; i++)
                            {
                                Dust.NewDustPerfect(
                                    Player.Center,
                                    DustID.Shadowflame,
                                    Main.rand.NextVector2Circular(3f, 3f),
                                    150,
                                    default,
                                    1.2f
                                );
                            }

                            SoundEngine.PlaySound(SoundID.Item8, Player.Center);
                        }

                        guiJiDownPressCount = 0;
                    }
                }
                else
                {
                    guiJiDownPressCount = 1;
                }

                guiJiLastDownPressTime = currentTime;
            }

            guiJiWasControlDown = isControlDown;

            if (isInvisible && Main.rand.NextBool(15))
            {
                Dust.NewDustPerfect(
                    Player.Center + Main.rand.NextVector2Circular(25f, 25f),
                    DustID.Shadowflame,
                    Vector2.Zero,
                    150,
                    default,
                    0.6f
                );
            }
        }

        private void InitializeCoinDrops()
        {
            coinDrops.Clear();

            coinDrops.Add(new CoinDrop
            {
                CoinType = ItemID.SilverCoin,
                CoinValue = 100,
                DropChance = 0.60f
            });

            coinDrops.Add(new CoinDrop
            {
                CoinType = ItemID.GoldCoin,
                CoinValue = 10000,
                DropChance = 0.30f
            });

            coinDrops.Add(new CoinDrop
            {
                CoinType = ItemID.PlatinumCoin,
                CoinValue = 1000000,
                DropChance = 0.10f
            });
        }

        // ====== 死亡之泰坦专属方法 ======
        private void HandleSiWangEffects()
        {
            UpdateLifeTracking();
            UpdateDeathImmunityStatus();
        }

        private void UpdateLifeTracking()
        {
            int currentLife = Player.statLife;

            if (lastLifeValue > 0)
            {
                int lifeChange = currentLife - lastLifeValue;

                if (lifeChange < 0)
                {
                    lifeLossThisFrame = -lifeChange;
                    AddXinRuiFromLifeLoss(lifeLossThisFrame);
                }
                else if (lifeChange > 0)
                {
                    lifeGainThisFrame = lifeChange;
                    AddXinRuiFromLifeGain(lifeGainThisFrame);
                }
            }

            lastLifeValue = currentLife;
            lifeLossThisFrame = 0;
            lifeGainThisFrame = 0;
        }

        private void AddXinRuiFromLifeLoss(int lifeLoss)
        {
            if (lifeLoss <= 0) return;

            float xinRuiGain = lifeLoss / 50f;
            xinRui += xinRuiGain;
            xinRui = Math.Min(xinRui, XinRuiMax);
        }

        private void AddXinRuiFromLifeGain(int lifeGain)
        {
            if (lifeGain <= 0) return;

            float xinRuiGain = lifeGain / 25f;
            xinRui += xinRuiGain;
            xinRui = Math.Min(xinRui, XinRuiMax);
        }

        private void UpdateDeathImmunityStatus()
        {
            int currentTime = (int)(Main.GameUpdateCount / 60);

            if (deathImmunityUsed && currentTime - lastDeathImmunityTime > deathImmunityCooldown)
            {
                deathImmunityUsed = false;
            }
        }

        private void UpdateGhostHandTimers()
        {
            if (attackGhostHandTimer > 0)
            {
                attackGhostHandTimer--;
            }

            if (hurtGhostHandTimer > 0)
            {
                hurtGhostHandTimer--;
            }

            if (autoGhostHandTimer > 0)
            {
                autoGhostHandTimer--;
            }
            else
            {
                SpawnRandomGhostHands();
                autoGhostHandTimer = GhostHandInterval;
            }
        }

        private void SpawnRandomGhostHands()
        {
            NPC target = FindRandomEnemy();
            if (target == null) return;

            int count = Main.rand.Next(3, 7);

            for (int i = 0; i < count; i++)
            {
                SpawnGhostHand(target);
            }

            for (int i = 0; i < 5; i++)
            {
                Dust.NewDustPerfect(
                    Player.Center,
                    DustID.Shadowflame,
                    Main.rand.NextVector2Circular(3f, 3f),
                    150,
                    Color.Purple,
                    1.2f
                );
            }

            SoundEngine.PlaySound(SoundID.Item8, Player.Center);
        }

        private void SpawnGhostHand(NPC target)
        {
            Vector2 spawnPosition;
            float distanceVariation = Main.rand.NextFloat();

            if (distanceVariation < 0.5f)
            {
                spawnPosition = Player.Center + Main.rand.NextVector2Circular(100f, 100f);
            }
            else
            {
                spawnPosition = target.Center + Main.rand.NextVector2Circular(150f, 150f);
            }

            Vector2 baseDirection = (target.Center - spawnPosition).SafeNormalize(Vector2.Zero);

            int mode = Main.rand.Next(3);
            Vector2 velocity = Vector2.Zero;

            switch (mode)
            {
                case 0:
                    velocity = baseDirection * Main.rand.NextFloat(10f, 15f);
                    break;

                case 1:
                    velocity = baseDirection.RotatedBy(Main.rand.NextFloat(-0.3f, 0.3f)) * Main.rand.NextFloat(8f, 12f);
                    break;

                case 2:
                    Vector2 randomDir = Main.rand.NextVector2Unit();
                    velocity = randomDir * Main.rand.NextFloat(5f, 8f);
                    break;
            }

            int baseDamage = 18;
            int damage = (int)(baseDamage * Player.GetDamage(DamageClass.Generic).Multiplicative);

            var source = Player.GetSource_FromThis("Shenyupai_SiWang_GhostHand");

            int proj = Projectile.NewProjectile(
                source,
                spawnPosition,
                velocity,
                964,
                damage,
                2f,
                Player.whoAmI
            );

            if (proj >= 0 && proj < Main.maxProjectiles)
            {
                Projectile projectile = Main.projectile[proj];
                projectile.ArmorPenetration = 20;
                projectile.penetrate = 3;
                projectile.usesLocalNPCImmunity = true;
                projectile.localNPCHitCooldown = 10;

                if (mode == 2)
                {
                    projectile.ai[0] = 1;
                }

                projectile.ai[1] = damage;
            }
        }

        // ====== 其他泰坦通用方法 ======
        private void HandleSpecialEffects()
        {
            if (hasSuiYue && HasActiveBuff)
            {
                HandleSuiYueTimeControl();
            }
        }

        private void HandleSuiYueTimeControl()
        {
            if (Player.controlHook && Main.GameUpdateCount - lastTimeControlUse > timeControlCooldown * 60)
            {
                lastTimeControlUse = (int)Main.GameUpdateCount;

                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDustPerfect(
                        Player.Center,
                        DustID.GoldFlame,
                        Main.rand.NextVector2Circular(5f, 5f),
                        100,
                        default,
                        1.5f
                    );
                }

                SoundEngine.PlaySound(SoundID.Item8, Player.Center);
            }
        }

        // ====== 辅助方法 ======
        private bool IsEnemyNearby(float distance)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.lifeMax > 5 &&
                    Vector2.Distance(Player.Center, npc.Center) < distance)
                {
                    return true;
                }
            }
            return false;
        }

        private NPC FindRandomEnemy()
        {
            List<NPC> validEnemies = new List<NPC>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.lifeMax > 5 &&
                    !npc.dontTakeDamage && npc.type != NPCID.TargetDummy &&
                    Vector2.Distance(Player.Center, npc.Center) < 600f)
                {
                    validEnemies.Add(npc);
                }
            }

            if (validEnemies.Count > 0)
            {
                return validEnemies[Main.rand.Next(validEnemies.Count)];
            }

            return null;
        }

        private bool IsBossNearby()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.boss && Vector2.Distance(Player.Center, npc.Center) < 1000f)
                {
                    return true;
                }
            }
            return false;
        }

        // ====== 伤害处理钩子 ======
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            // 浪漫牌：闪避系统
            if (hasLangMan && HasActiveBuff && langManDodgeAvailable)
            {
                if (ActivateLangManDodge())
                {
                    modifiers.FinalDamage *= 0f;
                    return;
                }
            }

            // 注意：大地护盾的逻辑现在在 OnHurt 中处理
            // 因为 ModifyHurt 是修改伤害值，而我们需要在实际伤害发生前处理护盾

            // 大地护盾：减伤效果（在伤害计算阶段应用减伤）
            if (hasDaDi && HasActiveBuff && damageReductionFromShield > 0 && currentShield > 0)
            {
                // 应用护盾减伤
                float reduction = damageReductionFromShield;
                modifiers.FinalDamage *= (1f - reduction);

                // 显示减伤效果
                if (Main.rand.NextBool(4))
                {
                    CombatText.NewText(
                        Player.getRect(),
                        Color.Orange,
                        $"-{(reduction * 100):F0}%伤害"
                    );
                }
            }


            if (hasSiWang && HasActiveBuff)
            {
                HandleSiWangDamageModifiers(ref modifiers);
            }

            if (hasFenZheng && HasActiveBuff)
            {
                HandleFenZhengDamageModifiers(ref modifiers);
            }

            // 岁月：按伤害比例加速时间流逝
            if (hasSuiYue && HasActiveBuff)
            {
                float damagePercent = (float)modifiers.FinalDamage.Flat / Player.statLifeMax2;

                // 增加时间流逝速率
                timeFlowRate += damagePercent * 0.5f;
                timeFlowRate = Math.Min(timeFlowRate, 0.02f);

                // 重置时间流逝计时器，立即触发一次流逝
                timeFlowTimer = TimeFlowInterval - 10;
            }

            // 门径：15秒后受击立即恢复召唤栏
            if (hasMenJing && HasActiveBuff && menJingSkillActive &&
                menJingSkillTimer <= MenJingSkillDuration - MenJingSkillVulnerableTime)
            {
                DeactivateMenJingSkill();
            }
        }

        // ====== 伤害实际发生时的处理 ======
        public override void OnHurt(Player.HurtInfo info)
        {
            // 大地护盾：在实际伤害发生时处理护盾吸收
            if (hasDaDi && HasActiveBuff && currentShield > 0)
            {
                int incomingDamage = info.Damage;
                int originalDamage = incomingDamage;

                // 计算护盾能吸收的伤害（不能超过护盾当前值）
                int shieldAbsorption = (int)Math.Min(currentShield, incomingDamage);

                // 减少护盾值
                currentShield -= shieldAbsorption;

                // 计算玩家实际承受的伤害
                int actualDamage = incomingDamage - shieldAbsorption;

                // 显示护盾吸收信息
                if (shieldAbsorption > 0)
                {
                    CombatText.NewText(
                        Player.getRect(),
                        new Color(255, 215, 0), // 金色
                        $"护盾吸收: {shieldAbsorption}"
                    );
                }

                // 护盾受损特效
                for (int i = 0; i < Math.Min(5, shieldAbsorption / 10); i++)
                {
                    Dust.NewDustPerfect(
                        Player.Center,
                        DustID.GoldFlame,
                        Main.rand.NextVector2Circular(2f, 2f),
                        100,
                        Color.Orange,
                        0.8f
                    );
                }

                // 如果护盾耗尽
                if (currentShield <= 0)
                {
                    currentShield = 0;
                    isShieldBroken = true;
                    shieldBreakTimer = 0;

                    // 显示溢出伤害（如果有）
                    if (actualDamage > 0)
                    {
                        CombatText.NewText(Player.getRect(), Color.Red, $"溢出伤害: {actualDamage}");
                    }

                    // 护盾破碎特效
                    for (int i = 0; i < 10; i++)
                    {
                        Dust.NewDustPerfect(
                            Player.Center,
                            DustID.Stone,
                            Main.rand.NextVector2Circular(4f, 4f),
                            100,
                            Color.Gray,
                            1.2f
                        );
                    }

                    CombatText.NewText(Player.getRect(), Color.Red, "护盾已破裂！");
                    SoundEngine.PlaySound(SoundID.Item27, Player.Center);

                    // 护盾破碎后，重置加成效果
                    damageBonusFromShield = 0f;
                    damageReductionFromShield = 0f;
                }

                // 如果护盾完全吸收了伤害，恢复玩家生命值并显示效果
                if (actualDamage <= 0)
                {
                    Player.statLife += originalDamage; // 恢复被减去的生命值
                    Player.HealEffect(originalDamage); // 显示治疗效果
                    SoundEngine.PlaySound(SoundID.Item35, Player.Center);
                }
            }

            // 死亡牌：受伤触发鬼手
            if (hasSiWang && HasActiveBuff && hurtGhostHandTimer <= 0)
            {
                SpawnRandomGhostHands();
                hurtGhostHandTimer = GhostHandInterval;
            }
        }

        private void HandleSiWangDamageModifiers(ref Player.HurtModifiers modifiers)
        {
            float baseVulnerability = 1.25f;
            float damageReduction = xinRui * DamageReductionPerXinRui;
            damageReduction = Math.Min(damageReduction, 0.5f);

            float healthPercent = (float)Player.statLife / Player.statLifeMax2;
            if (healthPercent < 0.35f)
            {
                damageReduction *= 2f;
                damageReduction = Math.Min(damageReduction, 0.75f);
            }

            float finalMultiplier = baseVulnerability * (1f - damageReduction);
            finalMultiplier = Math.Max(finalMultiplier, 0.1f);

            modifiers.FinalDamage *= finalMultiplier;

            float damagePercent = (float)modifiers.FinalDamage.Flat / Player.statLifeMax2;
            float xinRuiGain = damagePercent * 50f;
            xinRuiGain = Math.Min(xinRuiGain, 10f);
            xinRui += xinRuiGain;
            xinRui = Math.Min(xinRui, XinRuiMax);
        }

        private void HandleFenZhengDamageModifiers(ref Player.HurtModifiers modifiers)
        {
            float originalDamage = modifiers.FinalDamage.Flat;
            float maxDamage = Player.statLifeMax2 * damageTakenCap;

            if (originalDamage > maxDamage)
            {
                modifiers.FinalDamage *= maxDamage / originalDamage;
            }
        }

        // ====== 理性牌伤害加成 ======
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // 理性牌：基于debuff数量的伤害加成
            if (hasLiXing && HasActiveBuff)
            {
                float damageBonus = CalculateLiXingDamageBonus(target);
                if (damageBonus > 0)
                {
                    modifiers.FinalDamage *= (1f + damageBonus);
                }
            }

            // 律法牌：双倍伤害
            if (hasLvFa && HasActiveBuff && isDoubleDamageActive)
            {
                modifiers.FinalDamage *= 2.0f;

                // 双倍伤害特效
                if (Main.rand.NextBool(5))
                {
                    Dust.NewDustPerfect(
                        target.Center,
                        DustID.GoldFlame,
                        Main.rand.NextVector2Circular(3f, 3f),
                        100,
                        Color.Gold,
                        1f
                    );
                }
            }

            // 天空之泰坦的效果
            if (hasTianKong && HasActiveBuff && Main.raining && lockedTargets.Contains(target.whoAmI))
            {
                modifiers.FinalDamage *= 2.0f;

                for (int i = 0; i < 5; i++)
                {
                    Dust.NewDustPerfect(
                        target.Center,
                        DustID.Electric,
                        Main.rand.NextVector2Circular(3f, 3f),
                        100,
                        Color.Yellow,
                        1f
                    );
                }

                SoundEngine.PlaySound(SoundID.Thunder, target.Center);
                lockedTargets.Remove(target.whoAmI);
                targetLockTimers.Remove(target.whoAmI);
            }
        }

        // ====== 理性牌的弹幕伤害加成 ======
        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            // 理性牌：基于debuff数量的伤害加成
            if (hasLiXing && HasActiveBuff && proj.CountsAsClass(DamageClass.Ranged))
            {
                float damageBonus = CalculateLiXingDamageBonus(target);
                if (damageBonus > 0)
                {
                    modifiers.FinalDamage *= (1f + damageBonus);
                }
            }

            // 律法牌：双倍伤害
            if (hasLvFa && HasActiveBuff && isDoubleDamageActive)
            {
                modifiers.FinalDamage *= 2.0f;

                // 双倍伤害特效
                if (Main.rand.NextBool(5))
                {
                    Dust.NewDustPerfect(
                        target.Center,
                        DustID.GoldFlame,
                        Main.rand.NextVector2Circular(3f, 3f),
                        100,
                        Color.Gold,
                        1f
                    );
                }
            }
        }

        // ====== 理性牌伤害计算 ======
        private float CalculateLiXingDamageBonus(NPC target)
        {
            if (!hasLiXing || !HasActiveBuff) return 0f;

            // 计算目标身上的debuff数量
            int debuffCount = CountDebuffsOnNPC(target);

            // 计算伤害加成
            float damageBonus = debuffCount * liXingDamageBonusPerDebuff;

            // 限制最大伤害加成
            damageBonus = Math.Min(damageBonus, liXingMaxDamageBonus);

            return damageBonus;
        }

        // 辅助方法：计算NPC身上的debuff数量
        private int CountDebuffsOnNPC(NPC npc)
        {
            int count = 0;

            for (int i = 0; i < NPC.maxBuffs; i++)
            {
                int buffType = npc.buffType[i];
                if (buffType > 0 && npc.buffTime[i] > 0)
                {
                    // 检查是否为负面debuff（排除增益buff）
                    if (IsDebuffType(buffType))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        // 辅助方法：判断是否为debuff类型
        private bool IsDebuffType(int buffType)
        {
            // 常见增益buff（排除）
            int[] buffBuffs = new int[]
            {
                BuffID.Regeneration,
                BuffID.Swiftness,
                BuffID.Ironskin,
                BuffID.ManaRegeneration,
                BuffID.MagicPower,
                BuffID.Featherfall,
                BuffID.Gills,
                BuffID.Shine,
                BuffID.NightOwl,
                BuffID.Hunter,
                BuffID.Dangersense,
                BuffID.Calm,
                BuffID.Heartreach,
                BuffID.Endurance,
                BuffID.Rage,
                BuffID.Inferno,
                BuffID.Wrath,
                BuffID.MinecartLeft,
                BuffID.Lifeforce,
                BuffID.Sunflower,
                BuffID.Campfire,
                BuffID.Honey,
                BuffID.DryadsWard,
                BuffID.AmmoReservation,
                BuffID.AmmoBox,
                BuffID.Warmth,
                BuffID.PeaceCandle,
                BuffID.WaterCandle,
                BuffID.StarInBottle,
                BuffID.Archery,
                BuffID.Titan,
                BuffID.Flipper,
                BuffID.Summoning,
                BuffID.Bewitched,
                BuffID.WeaponImbueVenom,
                BuffID.WeaponImbueCursedFlames,
                BuffID.WeaponImbueFire,
                BuffID.WeaponImbueGold,
                BuffID.WeaponImbueIchor,
                BuffID.WeaponImbueNanites,
                BuffID.WeaponImbueConfetti,
                BuffID.WeaponImbuePoison,
                BuffID.Clairvoyance,
                BuffID.Panic,
                BuffID.HeartLamp,
            };

            // 如果buff在白名单中，说明是增益buff，返回false
            foreach (int buff in buffBuffs)
            {
                if (buffType == buff)
                    return false;
            }

            // 其他buff默认为debuff
            return true;
        }

        // 辅助方法：检查debuff是否有效
        private bool IsDebuffValid(int buffType, NPC npc)
        {
            // 跳过一些可能无效的debuff
            if (buffType == BuffID.Confused && npc.boss)
                return false;

            if (buffType == BuffID.Stoned)
                return false;

            return true;
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (hasSiWang && HasActiveBuff)
            {
                return HandleSiWangDeathImmunity();
            }

            if (hasFenZheng && HasActiveBuff)
            {
                return HandleFenZhengDeathImmunity();
            }

            return true;
        }

        private bool HandleSiWangDeathImmunity()
        {
            int currentTime = (int)(Main.GameUpdateCount / 60);

            if (!deathImmunityUsed && currentTime - lastDeathImmunityTime > deathImmunityCooldown)
            {
                TriggerDeathImmunity();
                deathImmunityUsed = true;
                lastDeathImmunityTime = currentTime;
                return false;
            }

            if (XinRuiResurrectionAvailable)
            {
                TriggerXinRuiResurrection();
                lastXinRuiResurrectionTime = currentTime;
                xinRui -= 100f;
                xinRui = Math.Max(xinRui, 0f);
                return false;
            }

            return true;
        }

        private bool HandleFenZhengDeathImmunity()
        {
            int currentTime = (int)(Main.GameUpdateCount / 60);

            if (fenZhengCanTriggerDeathImmune && currentTime - lastFenZhengDeathImmuneTime > fenZhengDeathImmuneCooldown)
            {
                Player.statLife = Player.statLifeMax2 / 2;
                lastFenZhengDeathImmuneTime = currentTime;
                fenZhengDamageDealt = 0;
                fenZhengDamagePercent = 0f;
                fenZhengCanTriggerDeathImmune = false;

                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDustPerfect(
                        Player.Center,
                        DustID.Blood,
                        Main.rand.NextVector2Circular(5f, 5f),
                        100,
                        default,
                        1.8f
                    );
                }

                SoundEngine.PlaySound(SoundID.Item8, Player.Center);

                return false;
            }

            return true;
        }

        private void TriggerDeathImmunity()
        {
            Player.statLife = Player.statLifeMax2 / 2;
            Player.immune = true;
            Player.immuneTime = 180;

            for (int i = 0; i < 30; i++)
            {
                Dust.NewDustPerfect(
                    Player.Center,
                    DustID.Shadowflame,
                    Main.rand.NextVector2Circular(6f, 6f),
                    100,
                    Color.Purple,
                    2f
                );
            }

            SoundEngine.PlaySound(SoundID.Item8, Player.Center);
        }

        private void TriggerXinRuiResurrection()
        {
            Player.statLife = Player.statLifeMax2;
            Player.immune = true;
            Player.immuneTime = 300;
            ClearNegativeBuffs();

            for (int i = 0; i < 40; i++)
            {
                Dust.NewDustPerfect(
                    Player.Center,
                    DustID.PurpleTorch,
                    Main.rand.NextVector2Circular(8f, 8f),
                    100,
                    Color.Magenta,
                    3f
                );
            }

            SoundEngine.PlaySound(SoundID.Item8, Player.Center);
        }

        private void ClearNegativeBuffs()
        {
            int[] negativeBuffs = new int[]
            {
                BuffID.Poisoned,
                BuffID.OnFire,
                BuffID.CursedInferno,
                BuffID.Frostburn,
                BuffID.Chilled,
                BuffID.Frozen,
                BuffID.Slow,
                BuffID.Weak,
                BuffID.BrokenArmor,
                BuffID.Horrified,
                BuffID.TheTongue,
                BuffID.Cursed,
                BuffID.Darkness,
                BuffID.Silenced,
                BuffID.Confused,
                BuffID.Bleeding,
                BuffID.Venom,
                BuffID.Ichor,
                BuffID.BetsysCurse,
                BuffID.WitheredArmor,
                BuffID.WitheredWeapon,
                BuffID.Suffocation,
                BuffID.Stoned
            };

            foreach (int buffType in negativeBuffs)
            {
                if (Player.HasBuff(buffType))
                {
                    Player.ClearBuff(buffType);
                }
            }
        }

        // ====== 攻击处理钩子 ======
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (hasFenZheng && HasActiveBuff)
            {
                fenZhengDamageDealt += damageDone;
            }

            // 浪漫牌：记录攻击命中
            if (hasLangMan && HasActiveBuff)
            {
                RecordLangManHit();
            }

            // 理性牌：100%附加随机原版debuff
            if (hasLiXing && HasActiveBuff && debuffChance > 0 && vanillaDebuffTypes.Count > 0)
            {
                // 随机选择一个原版debuff
                int debuffType = vanillaDebuffTypes[Main.rand.Next(vanillaDebuffTypes.Count)];
                int debuffTime = 180 + Main.rand.Next(120);

                // 检查这个debuff是否可以被添加
                if (IsDebuffValid(debuffType, target))
                {
                    target.AddBuff(debuffType, debuffTime);

                    // 特效
                    for (int i = 0; i < 3; i++)
                    {
                        Dust.NewDustPerfect(
                            target.Center,
                            DustID.PurpleTorch,
                            Main.rand.NextVector2Circular(3f, 3f),
                            100,
                            Color.Purple,
                            0.8f
                        );
                    }
                }
            }

            if (hasSiWang && HasActiveBuff && attackGhostHandTimer <= 0)
            {
                SpawnRandomGhostHands();
                attackGhostHandTimer = GhostHandInterval;
            }

            if (hasGuiJi && HasActiveBuff && target.boss && target.type != NPCID.TargetDummy)
            {
                Buffs.SFeir_Buffs.BaoJinBi_Buff.OnNPCGotHit(target, Player, damageDone);
                int buffDuration = 600;
                target.AddBuff(ModContent.BuffType<Buffs.SFeir_Buffs.BaoJinBi_Buff>(), buffDuration);

                for (int i = 0; i < 3; i++)
                {
                    Dust.NewDustPerfect(
                        target.Center,
                        DustID.GoldFlame,
                        Main.rand.NextVector2Circular(2f, 2f),
                        100,
                        Color.Gold,
                        1.2f
                    );
                }
            }
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 浪漫牌：记录攻击命中（包括远程攻击）
            if (hasLangMan && HasActiveBuff)
            {
                RecordLangManHit();
            }

            // 理性牌：远程攻击也触发效果
            if (hasLiXing && HasActiveBuff && debuffChance > 0 && proj.CountsAsClass(DamageClass.Ranged) && vanillaDebuffTypes.Count > 0)
            {
                // 随机选择一个原版debuff
                int debuffType = vanillaDebuffTypes[Main.rand.Next(vanillaDebuffTypes.Count)];
                int debuffTime = 180 + Main.rand.Next(120);

                if (IsDebuffValid(debuffType, target))
                {
                    target.AddBuff(debuffType, debuffTime);

                    for (int i = 0; i < 3; i++)
                    {
                        Dust.NewDustPerfect(
                            target.Center,
                            DustID.PurpleTorch,
                            Main.rand.NextVector2Circular(3f, 3f),
                            100,
                            Color.Purple,
                            0.8f
                        );
                    }
                }
            }

            if (hasSiWang && HasActiveBuff && proj.type == 964 && proj.owner == Player.whoAmI)
            {
                HandleGhostHandHit(proj, target, damageDone);
            }

            if (hasGuiJi && HasActiveBuff && target.boss && target.type != NPCID.TargetDummy)
            {
                Buffs.SFeir_Buffs.BaoJinBi_Buff.OnNPCGotHit(target, Player, damageDone);
                int buffDuration = 600;
                target.AddBuff(ModContent.BuffType<Buffs.SFeir_Buffs.BaoJinBi_Buff>(), buffDuration);

                for (int i = 0; i < 5; i++)
                {
                    Dust.NewDustPerfect(
                        target.Center,
                        DustID.Gold,
                        Main.rand.NextVector2Circular(3f, 3f),
                        100,
                        Color.Gold,
                        1f
                    );
                }
            }
        }

        private void HandleGhostHandHit(Projectile proj, NPC target, int damageDone)
        {
            int remainingPenetrate = proj.penetrate;
            int hitsMade = 3 - remainingPenetrate;

            float damageMultiplier = 1f;
            if (hitsMade > 1)
            {
                damageMultiplier -= 0.15f * (hitsMade - 1);
                damageMultiplier = Math.Max(damageMultiplier, 0.1f);
            }

            int healAmount = Player.statLifeMax2 / 100;
            healAmount = Math.Max(healAmount, 1);

            int currentLife = Player.statLife;
            int maxLife = Player.statLifeMax2;
            int healableAmount = maxLife - currentLife;

            if (healableAmount > 0)
            {
                int actualHeal = Math.Min(healAmount, healableAmount);
                Player.statLife += actualHeal;
                Player.HealEffect(actualHeal);
            }
            else
            {
                float xinRuiGain = healAmount * 0.5f;
                xinRui += xinRuiGain;
                xinRui = Math.Min(xinRui, XinRuiMax);
            }

            for (int i = 0; i < 3; i++)
            {
                Dust.NewDustPerfect(
                    target.Center,
                    DustID.LifeDrain,
                    Main.rand.NextVector2Circular(2f, 2f),
                    100,
                    Color.Magenta,
                    0.7f
                );
            }

            if (remainingPenetrate > 0)
            {
                float nextDamageMultiplier = 1f - (0.15f * hitsMade);
                nextDamageMultiplier = Math.Max(nextDamageMultiplier, 0.1f);
                int initialDamage = (int)proj.ai[1];
                proj.damage = (int)(initialDamage * nextDamageMultiplier);
            }
        }


        // ====== 公共访问方法 ======
        public void PublicRemoveCurrentCardEffect()
        {
            RemoveCurrentCardEffect();
        }

        public void PublicResetAllBonuses()
        {
            ResetAllBonuses();
        }


        // ====== 卡牌操作方法 ======
        public void DrawCard()
        {
            RemoveCurrentCardEffect();
            ResetAllBonuses();

            CurrentCard = Shenyupai_System.DrawRandomCard(Player);

            if (CurrentCard != null)
            {
                BuffStartTime = Main.GameUpdateCount;
                CurrentCard.ApplyEffect?.Invoke(Player);

                if (CurrentCard.BuffType > 0)
                {
                    Player.AddBuff(CurrentCard.BuffType, BuffDuration);
                }

                Main.NewText($"抽到了：【{CurrentCard.Name}】", Color.Gold);
            }
        }

        public void RemoveCurrentCardEffect()
        {
            if (CurrentCard != null)
            {
                CurrentCard.RemoveEffect?.Invoke(Player);

                if (CurrentCard.BuffType > 0)
                {
                    Player.ClearBuff(CurrentCard.BuffType);
                }

                CurrentCard = null;
            }

            BuffStartTime = -1;
            ResetAllBonuses();
        }

        private void ResetAllBonuses()
        {
            // 创生三泰坦
            hasFuShi = false;
            allDamageBonus = 0f;

            // 浪漫牌重置
            hasLangMan = false;
            langManBaseMoveSpeedBonus = 0.30f;
            langManAttackTimer = 0;
            langManHitsInWindow = 0;
            langManSpeedStacks = 0;
            langManStackTimers.Clear();
            langManSpeedBonusPerStack = 0.05f;
            langManDamageBonusPerStack = 0.02f;
            langManDodgeAvailable = false;
            langManDodgeCooldown = 0;
            langManDodgeActive = false;
            langManDodgeActiveTimer = 0;
            langManVisualTimer = 0;

            hasLiXing = false;
            rangedSpeedBonus = 0f;
            rangedCritBonus = 0;
            debuffChance = 0f;
            liXingDamageBonusPerDebuff = 0f;
            liXingMaxDamageBonus = 0f;

            // 命运三泰坦
            hasMenJing = false;
            minionSlotsBonus = 0;
            summonDamageBonus = 0f;
            perMinionBonus = 0f;

            // 门径技能重置
            originalMinionSlots = 0;
            menJingExtraSlots = 0;
            menJingSkillActive = false;
            menJingSkillTimer = 0;

            hasLvFa = false;
            manaCostReduction = 0f;
            doubleDamageTimer = 0;
            isDoubleDamageActive = false;
            doubleDamageActiveTimer = 0;

            hasSuiYue = false;
            timeControlCooldown = 0;
            survivalDamagePerMinute = 0f;
            survivalTime = 0;
            timeFlowRate = 0.005f;
            timeFlowTimer = 0;
            timeRecordQueue.Clear();

            // 支柱三泰坦

            // 大地护盾系统重置
            hasDaDi = false;
            groundMoveSpeedBonus = 0f;
            currentShield = 0f;
            maxShield = 0f;
            shieldRefreshTimer = 0;
            isShieldActive = false;
            isShieldBroken = false;
            shieldBreakTimer = 0;

            // 护盾加成重置
            damageBonusFromShield = 0f;
            damageReductionFromShield = 0f;

            // 免疫系统重置
            immuneToLava = false;
            immuneToPetrify = false;
            immuneToKnockback = false;
            immuneToFireBlocks = false;
            immuneToWeakness = false;
            immuneToBrokenArmor = false;

            hasHaiYang = false;
            waterMoveSpeedBonus = 0f;
            waterLifeRegenBonus = 0;

            hasTianKong = false;
            flightSpeedBonus = 0f;
            wingHoverBonus = 0f;
            highAltitudeDamageBonus = 0f;

            // 灾厄三泰坦
            hasFenZheng = false;
            meleeDamageBonus = 0f;
            meleeSpeedBonus = 0f;
            defenseToLifeConversion = 0;
            defenseToRegenConversion = 0;
            fenZhengDeathImmuneCooldown = 0;
            tauntEnemies = false;
            damageTakenCap = 1f;
            fenZhengDamageDealt = 0;
            fenZhengDamageTimer = 0;
            fenZhengDamagePercent = 0f;
            fenZhengCanTriggerDeathImmune = false;

            hasGuiJi = false;
            moveSpeedBonus = 0f;
            itemMagnetRangeBonus = 0f;
            isInvisible = false;

            hasSiWang = false;
            deathImmunityCooldown = 0;
            ghostHandChance = 0f;
            lifeStealAmount = 0f;
            teamDamageTransfer = false;
            deathImmunityUsed = false;

            // 海洋之泰坦专属字段重置
            waterCircleTimer = 0;

            // 天空之泰坦专属字段重置
            isHovering = false;
            hoverTimer = 0;
            lockedTargets.Clear();
            targetLockTimers.Clear();
            thunderStrikeTimer = 0;

            // 诡计之泰坦专属字段重置
            guiJiDownPressCount = 0;
            guiJiLastDownPressTime = 0;
            guiJiWasControlDown = false;
            coinDrops.Clear();
            totalCoinsDropped = 0;

            // 死亡之泰坦专属字段重置
            xinRui = 0f;
            damageBonusFromLifeLoss = 0f;
            lastLifeValue = 0;
            lifeLossThisFrame = 0;
            lifeGainThisFrame = 0;
            overflowHealing = 0f;
            currentDamageReductionFromOverflow = 0f;
            currentRegenBonusFromOverflow = 0f;
            attackGhostHandTimer = 0;
            hurtGhostHandTimer = 0;
            autoGhostHandTimer = 0;
            lastXinRuiResurrectionTime = -999;

            // 技能系统重置
            skillQReady = true;
            skillQCooldown = 0;
        }

        // ====== 数据保存 ======
        public override void SaveData(TagCompound tag)
        {
            if (Shenyupai_System.UnlockedCards.Count > 0)
            {
                tag["UnlockedCards"] = Shenyupai_System.UnlockedCards;
            }
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("UnlockedCards"))
            {
                Shenyupai_System.UnlockedCards = tag.Get<List<string>>("UnlockedCards");
            }
        }

        // ====== 卡牌切换方法 ======
        public void SwitchToNewCard()
        {
            DrawCard();
        }

        public void SwitchToSpecificCard(string cardName)
        {
            var allCards = Shenyupai_System.GetAllCards();
            Shenyupai_System.ShenyupaiBuff targetCard = null;

            foreach (var card in allCards)
            {
                if (card.Name == cardName)
                {
                    targetCard = card;
                    break;
                }
            }

            if (targetCard != null)
            {
                RemoveCurrentCardEffect();
                ResetAllBonuses();

                CurrentCard = targetCard;
                BuffStartTime = Main.GameUpdateCount;

                // 在调用 ApplyEffect 之前，先保存卡牌名称
                string cardNameToDisplay = targetCard.Name;

                targetCard.ApplyEffect?.Invoke(Player);

                if (targetCard.BuffType > 0)
                {
                    Player.AddBuff(targetCard.BuffType, BuffDuration);
                }

                // 修复：使用保存的卡牌名称，而不是 CurrentCard.Name
                Main.NewText($"已切换到：【{cardNameToDisplay}】", Color.Gold);
            }
            else
            {
                Main.NewText($"未找到名为【{cardName}】的神谕牌", Color.Red);
            }
        }
    }
}