using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.NPCs.Boss.SFeir_Boss
{
    [AutoloadBossHead]
    public class SFeir : ModNPC
    {
        // === 继承猪鲨的AI系统 ===
        // 猪鲨的AI Style是39，我们将直接使用它
        // 但保留我们自己的状态管理

        // 我们自己的状态（叠加在猪鲨AI之上）
        private int customTimer = 0;
        private int coinCooldown = 0;
        private const int CoinInterval = 480; // 每8秒抛一次硬币
        private int chatCooldown = 0;
        private const int ChatInterval = 300; // 每5秒可说话

        // 当前阶段
        public int CurrentPhase = 1;
        private bool phase2Triggered = false;
        private bool phase3Triggered = false;

        // 谎言系统
        public LieManager LieManager;

        // 外观颜色
        private static readonly Color ChatColor = new Color(180, 200, 230); // 蓝灰色

        public override void SetStaticDefaults()
        {
            //DisplayName.SetDefault("赛飞儿，诡计猫盗");

            // 使用猪鲨的帧数
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.DukeFishron];

            // Boss设置
            NPCID.Sets.MustAlwaysDraw[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.MPAllowedEnemies[Type] = true;

            // 确保不隐藏UI
            //NPCID.Sets.HideBossHealthBar[Type] = false;
        }

        public override void SetDefaults()
        {
            // === 使用猪鲨的基础属性 ===
            NPC.width = 120;  // 猪鲨尺寸
            NPC.height = 120;
            NPC.lifeMax = 50000;
            NPC.life = NPC.lifeMax;
            NPC.damage = 60;
            NPC.defense = 25;
            NPC.knockBackResist = 0f;
            NPC.value = Item.buyPrice(5, 0, 0, 0);

            // === 关键：使用猪鲨的AI Style ===
            NPC.aiStyle = 39; // 猪鲨的AI Style

            // Boss设置（猪鲨的配置）
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;

            // 初始化谎言系统
            LieManager = new LieManager(NPC);
        }

        // === 使用猪鲨的贴图 ===
        public override string Texture => "Amphoreus/NPCs/Boss/SFeir_Boss/SFeir";

        // === 生成时初始化 ===
        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);

            // 初始化计时器
            customTimer = 0;
            coinCooldown = 120; // 2秒后第一次抛硬币
            chatCooldown = 60;  // 1秒后说第一句话

            // 寻找目标
            NPC.TargetClosest(true);

            // 发送欢迎消息
            SendChatMessage("借用一下这位鱼先生的外形~游戏开始咯！");
        }

        // === 核心AI：在猪鲨AI基础上叠加我们的逻辑 ===
        public override void AI()
        {
            // === 1. 先让猪鲨的AI执行（自动通过aiStyle=39处理）===
            // 猪鲨AI会处理：移动、冲撞、龙卷风等攻击

            // === 2. 我们的脱战检测 ===
            if (!CheckValidTarget())
            {
                HandleDespawn();
                return;
            }

            // === 3. 更新计时器 ===
            customTimer++;
            if (coinCooldown > 0) coinCooldown--;
            if (chatCooldown > 0) chatCooldown--;

            // === 4. 阶段检测 ===
            CheckPhaseTransition();

            // === 5. 我们的自定义逻辑 ===
            ExecuteCustomLogic();

            // === 6. 更新子系统 ===
            if (LieManager != null)
            {
                LieManager.Update();
            }
        }

        // === 脱战检测 ===
        private bool CheckValidTarget()
        {
            // 检查是否有任何活着的玩家在合理距离内
            float maxDistance = 5000f;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                {
                    float distance = Vector2.Distance(player.Center, NPC.Center);
                    if (distance < maxDistance)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void HandleDespawn()
        {
            // 没有有效目标，逐渐消失
            NPC.velocity *= 0.95f;
            NPC.alpha += 3;

            if (NPC.alpha >= 255)
            {
                NPC.active = false;
                if (Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("赛飞儿：客人都走了，我也收工啦~", ChatColor);
                }
            }
        }

        // === 阶段转换 ===
        private void CheckPhaseTransition()
        {
            // 第一阶段 -> 第二阶段：66%生命
            if (!phase2Triggered && NPC.life <= NPC.lifeMax * 0.66f)
            {
                phase2Triggered = true;
                CurrentPhase = 2;
                OnEnterPhaseTwo();
            }

            // 第二阶段 -> 第三阶段：33%生命
            if (!phase3Triggered && NPC.life <= NPC.lifeMax * 0.33f)
            {
                phase3Triggered = true;
                CurrentPhase = 3;
                OnEnterPhaseThree();
            }
        }

        private void OnEnterPhaseTwo()
        {
            SendChatMessage("热身结束，现在要认真了哦！");
            CombatText.NewText(NPC.getRect(), Color.Orange, "第二阶段！", true);

            // 可以在这里增强猪鲨AI的参数
            // 例如让冲撞更快更频繁
        }

        private void OnEnterPhaseThree()
        {
            SendChatMessage("嘻嘻，看来得动真格了~借你的东西用用！");
            CombatText.NewText(NPC.getRect(), Color.Red, "最终阶段！", true);

            // 预留偷UI接口
            // StealUISystem.Activate();
        }

        // === 我们的自定义逻辑 ===
        private void ExecuteCustomLogic()
        {
            // 1. 抛硬币系统
            if (coinCooldown <= 0 && CurrentPhase >= 2) // 第二阶段开始抛硬币
            {
                TriggerCoinToss();
                coinCooldown = CoinInterval / CurrentPhase; // 阶段越高频率越快
            }

            // 2. 随机聊天
            if (chatCooldown <= 0 && customTimer % 120 == 0)
            {
                MaybeSaySomething();
                chatCooldown = ChatInterval;
            }

            // 3. 移动时掉落金币
            if (NPC.velocity.Length() > 5f && customTimer % 20 == 0)
            {
                DropGoldFromMovement();
            }
        }

        private void TriggerCoinToss()
        {
            // 找到当前目标玩家
            Player target = Main.player[NPC.target];
            if (target != null && target.active && !target.dead)
            {
                // 播放音效
                SoundEngine.PlaySound(SoundID.Item4, NPC.Center);

                // 生成硬币弹幕
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    target.Center + new Vector2(0, -150),
                    Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.SFeir_Projectiles.FateCoin>(),
                    0,
                    0f,
                    Main.myPlayer,
                    NPC.whoAmI,
                    target.whoAmI
                );

                // 显示提示
                string[] coinLines = {
                    "命运硬币，转起来~",
                    "猜猜是正面还是反面？",
                    "让运气决定接下来的游戏吧~"
                };
                SendChatMessage(coinLines[Main.rand.Next(coinLines.Length)]);
            }
        }

        private void MaybeSaySomething()
        {
            if (Main.rand.NextBool(3)) // 33%概率说话
            {
                string[] idleLines = CurrentPhase switch
                {
                    1 => new[] {
                        "这位鱼先生游得真快呢~",
                        "你的钱包在召唤我哦~",
                        "要玩点刺激的吗？"
                    },
                    2 => new[] {
                        "谎言和真相，你分得清吗？",
                        "速度与欺骗，我最擅长了~",
                        "猜猜我下一步要做什么？"
                    },
                    _ => new[] {
                        "一切都将属于我~",
                        "你的犹豫就是我的机会！",
                        "游戏快到高潮了~"
                    }
                };

                SendChatMessage(idleLines[Main.rand.Next(idleLines.Length)]);
            }
        }

        private void DropGoldFromMovement()
        {
            if (Main.netMode == NetmodeID.Server) return;

            // 只在移动速度快时掉落
            Vector2 spawnPos = NPC.Center + new Vector2(
                Main.rand.Next(-NPC.width / 2, NPC.width / 2),
                Main.rand.Next(-NPC.height / 2, NPC.height / 2)
            );

            Vector2 velocity = new Vector2(
                Main.rand.NextFloat(-1f, 1f),
                Main.rand.NextFloat(-2f, -1f)
            );

            // 掉落银币或金币
            int coinType = Main.rand.Next(5) == 0 ? ItemID.GoldCoin : ItemID.SilverCoin;
            Item.NewItem(
                NPC.GetSource_FromAI(),
                spawnPos,
                velocity,
                coinType,
                Main.rand.Next(1, 3)
            );
        }

        // === 伤害处理 ===
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            // 爆金币效果
            if (hurtInfo.Damage > 0 && Main.netMode != NetmodeID.Server)
            {
                int coinCount = Main.rand.Next(3, 8);
                for (int i = 0; i < coinCount; i++)
                {
                    Vector2 velocity = new Vector2(
                        Main.rand.NextFloat(-3f, 3f),
                        Main.rand.NextFloat(-5f, -2f)
                    );

                    Item.NewItem(
                        NPC.GetSource_OnHit(target),
                        target.Center,
                        velocity,
                        ItemID.SilverCoin,
                        1
                    );
                }

                // 显示战斗台词
                string[] hitLines = {
                    "金币拿来~",
                    "这是惩罚哦~",
                    "别想逃！",
                    "讨价咯~"
                };
                if (Main.rand.NextBool(3))
                {
                    CombatText.NewText(target.getRect(), Color.Gold, hitLines[Main.rand.Next(hitLines.Length)]);
                }
            }
        }

        // === 聊天方法 ===
        private void SendChatMessage(string text)
        {
            if (Main.netMode != NetmodeID.Server && chatCooldown <= 0)
            {
                Main.NewText($"[赛飞儿] {text}", ChatColor);
                chatCooldown = 60; // 简短冷却防止刷屏
            }
        }

        // === 被击杀时 ===
        public override void OnKill()
        {
            // 掉落大量金币
            if (Main.netMode != NetmodeID.Server)
            {
                int goldAmount = 30 + CurrentPhase * 10;
                for (int i = 0; i < goldAmount; i++)
                {
                    Vector2 velocity = new Vector2(
                        Main.rand.NextFloat(-5f, 5f),
                        Main.rand.NextFloat(-8f, -3f)
                    );

                    Item.NewItem(
                        NPC.GetSource_Death(),
                        NPC.Center,
                        velocity,
                        ItemID.GoldCoin,
                        1
                    );
                }

                SendChatMessage("这次算你赢了...但我会回来的！");
            }
        }

        // === 绘制调整（可选）===
        // 如果你想让猪鲨贴图有点赛飞儿的风格，可以添加一点着色
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // 调用默认绘制（猪鲨贴图）
            // 可以添加一些颜色叠加来让它看起来更像赛飞儿
            return true;
        }
    }
}