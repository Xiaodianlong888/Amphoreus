using Amphoreus.Players.Shenyupai_Players;
using Amphoreus.Systems.XiLian_Systems.Touxiang;
using Amphoreus.Systems.XiLian_Systems.Xingtai;
using Amphoreus.Systems.XiLian_Systems.Xinyuan;
using Amphoreus.Systems.Ruwosuoshu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Amphoreus.NPCs.XiLian
{
    [AutoloadHead]
    public class Xilian : ModNPC
    {
        // === 枚举：功能模式 ===
        public enum FunctionMode
        {
            Chat,
            OracleCard,
            Shop,
            Transform,
            Quest
        }

        // === 对话头像贴图 ===
        private static Texture2D _portraitTexture;
        public static Texture2D PortraitTexture => _portraitTexture;
        public virtual string PortraitTexturePath => "Amphoreus/NPCs/XiLian/Xilian_Touxiang";
        public override string Texture => "Amphoreus/NPCs/XiLian/Xilian";

        // === 核心状态 ===
        public bool isFollowingPlayer = false;
        public int followingPlayerIndex = -1;

        // === 攻击系统 ===
        private bool isAttacking = false;
        private int attackTimer = 0;
        private int attackTarget = -1;
        private const int FramesPerSecond = 60;
        private const int TotalAttackTime = 4 * FramesPerSecond;
        private const int ProjectileSpawnTime = 3 * FramesPerSecond;
        private const float MaxAttackDistance = 500f;
        private const int AttackFrameStartIndex = 19;
        private const int AttackCooldown = 180;
        private int attackCooldownTimer = 0;

        // === 传送脱离系统 ===
        /// <summary>敌人进入该距离即视为「过于接近」，触发传送脱离。</summary>
        private const float TeleportTriggerDistance = 150f;
        /// <summary>传送后与敌人保持的距离。</summary>
        private const float TeleportSafeDistance = 420f;

        /// <summary>寻找落点时向下搜索的最大格数。</summary>
        private const int DownwardSearchTiles = 20;
        /// <summary>传送冷却（3 秒）。</summary>
        private const int TeleportCooldown = 180;
        private int teleportCooldownTimer = 0;

        // === 对话模式相关 ===
        public FunctionMode CurrentFunctionMode => _currentFunctionMode;
        private FunctionMode _currentFunctionMode = FunctionMode.Chat;
        private FunctionMode[] _functionModeOrder = new FunctionMode[]
        {
            FunctionMode.Chat,
            FunctionMode.OracleCard,
            FunctionMode.Shop,
            FunctionMode.Transform,
            FunctionMode.Quest
        };
        private int _functionModeIndex = 0;
        private string _currentChatText = ""; // 存储当前显示的聊天文本

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 23;
            NPCID.Sets.ExtraFramesCount[Type] = 9;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.AttackType[Type] = 1;
            NPCID.Sets.HatOffsetY[Type] = 4;
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers(0) { Velocity = 1f });

            NPC.Happiness
        // 生物群系偏好
        .SetBiomeAffection<ForestBiome>(AffectionLevel.Like)      // 森林
        .SetBiomeAffection<OceanBiome>(AffectionLevel.Like)       // 海洋
        .SetBiomeAffection<HallowBiome>(AffectionLevel.Like)      // 神圣之地
        .SetBiomeAffection<CorruptionBiome>(AffectionLevel.Dislike)  // 腐化之地（不喜欢）
        .SetBiomeAffection<CrimsonBiome>(AffectionLevel.Dislike)  // 猩红之地（不喜欢）
        .SetBiomeAffection<DungeonBiome>(AffectionLevel.Dislike)  // 地牢（不喜欢）
                                                                  // NPC偏好
        .SetNPCAffection(NPCID.Guide, AffectionLevel.Like)        // 向导
        .SetNPCAffection(NPCID.Dryad, AffectionLevel.Like)        // 树妖
        .SetNPCAffection(NPCID.Princess, AffectionLevel.Like);    // 公主

            if (_portraitTexture == null)
                _portraitTexture = ModContent.Request<Texture2D>(PortraitTexturePath).Value;
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 40;
            NPC.height = 54;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.damage = 111;
            NPC.defense = 15;
            NPC.lifeMax = 1111;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            AnimationType = NPCID.Dryad;
            NPC.knockBackResist = 0f;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement(Language.GetTextValue("Mods.Amphoreus.NPCs.Xilian.Bestiary")),
            });
        }

        // === 生成条件 ===
        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            return WorldStateSystem.CurrentWorldForm == WorldStateSystem.NpcForm.Xilian;
        }

        public override bool CheckConditions(int left, int right, int top, int bottom)
        {
            return WorldStateSystem.CurrentWorldForm == WorldStateSystem.NpcForm.Xilian;
        }

        // === 核心AI ===
        public override void AI()
        {
            if (attackCooldownTimer > 0)
                attackCooldownTimer--;

            if (teleportCooldownTimer > 0)
                teleportCooldownTimer--;

            // 攻击中：不处理走位与脱离
            if (isAttacking)
            {
                HandleAttackState();
                return;
            }

            // 昔涟不恐惧敌人：敌人靠近时她不移动、不保持距离、也不传送脱离。
            // 因此这里不再调用 TryAvoidByTeleport()。

            // 敌人进入攻击范围就立刻攻击
            if (attackCooldownTimer <= 0)
            {
                FindAttackTarget();
                if (attackTarget != -1)
                {
                    StartAttack();
                    return;
                }
            }

            base.AI();
        }

        /// <summary>
        /// 已停用：原「敌人靠近则传送脱离」的行为。
        /// 昔涟不再恐惧敌人，敌人靠近时保持原地不动。
        /// </summary>
        private bool TryAvoidByTeleport()
        {
            return false;
        }

        /// <summary>
        /// 在威胁点周围一圈寻找可站立的物块。
        /// 用 <see cref="Collision.SolidCollision"/> 按 NPC 真实碰撞箱（40x54）判定，
        /// 并把落点吸附到地面上，避免传送后卡进方块里。
        /// </summary>
        private bool TryFindTeleportSpot(Vector2 threatCenter, out Vector2 result)
        {
            result = NPC.Center;

            for (int attempt = 0; attempt < 40; attempt++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * TeleportSafeDistance;
                Vector2 candidate = threatCenter + offset;

                if (!TryResolveStandingPosition(candidate, out Vector2 standing))
                {
                    continue;
                }

                result = standing;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 把候选位置（NPC 中心）吸附到一个「脚下实体、身体无阻挡」的站位。
        /// 从候选点向下最多找 DownwardSearchTiles 格，成功返回该处中心坐标。
        /// </summary>
        private bool TryResolveStandingPosition(Vector2 candidateCenter, out Vector2 standingCenter)
        {
            standingCenter = candidateCenter;

            int halfWidth = NPC.width / 2;
            int halfHeight = NPC.height / 2;

            int startTileY = (int)(candidateCenter.Y / 16f);

            for (int dy = 0; dy <= DownwardSearchTiles; dy++)
            {
                int tileY = startTileY + dy;

                if (tileY < 10 || tileY > Main.maxTilesY - 10)
                {
                    return false;
                }

                if ((int)(candidateCenter.X / 16f) < 10 ||
                    (int)(candidateCenter.X / 16f) > Main.maxTilesX - 10)
                {
                    return false;
                }

                // 站立行的顶边 Y：脚正好贴在该行上沿
                float top = tileY * 16f - NPC.height;
                Vector2 pos = new Vector2(candidateCenter.X - halfWidth, top);

                // 脚下必须是实体方块
                int footTileY = (int)((top + NPC.height + 2f) / 16f);
                int leftTileX = (int)((pos.X + 2f) / 16f);
                int rightTileX = (int)((pos.X + NPC.width - 2f) / 16f);

                Tile leftFoot = Framing.GetTileSafely(leftTileX, footTileY);
                Tile rightFoot = Framing.GetTileSafely(rightTileX, footTileY);

                bool leftSolid = leftFoot != null && leftFoot.HasUnactuatedTile && Main.tileSolid[leftFoot.TileType];
                bool rightSolid = rightFoot != null && rightFoot.HasUnactuatedTile && Main.tileSolid[rightFoot.TileType];

                if (!leftSolid || !rightSolid)
                {
                    continue;
                }

                // 整个碰撞箱不能压到实体方块
                if (Collision.SolidCollision(pos, NPC.width, NPC.height))
                {
                    continue;
                }

                // 不能落在液体里（微光/岩浆会伤到）
                if (Collision.LavaCollision(pos, NPC.width, NPC.height))
                {
                    continue;
                }

                standingCenter = new Vector2(candidateCenter.X, top + halfHeight);
                return true;
            }

            return false;
        }

        /// <summary>传送：位移 + 尘埃 + 音效，并在服务端同步位置。</summary>
        private void TeleportTo(Vector2 destination, Vector2 threatCenter)
        {
            for (int i = 0; i < 20; i++)
            {
                Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.PinkFairy, 0f, 0f, 100, default, 1.2f)
                    .noGravity = true;
            }

            NPC.Center = destination;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.Item8, destination);
            }

            for (int i = 0; i < 20; i++)
            {
                Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.PinkFairy, 0f, 0f, 100, default, 1.2f)
                    .noGravity = true;
            }
        }

        // === 攻击系统方法 ===
        private void FindAttackTarget()
        {
            float closestDistance = MaxAttackDistance;
            attackTarget = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.lifeMax > 5 && !npc.dontTakeDamage && npc.type != NPCID.TargetDummy && !npc.townNPC)
                {
                    float distance = Vector2.Distance(NPC.Center, npc.Center);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        attackTarget = i;
                    }
                }
            }
        }

        private void StartAttack()
        {
            isAttacking = true;
            attackTimer = 0;
            NPC.netUpdate = true;
        }

        private void HandleAttackState()
        {
            attackTimer++;

            if (!IsTargetValid())
            {
                CancelAttack();
                return;
            }

            NPC.velocity = Vector2.Zero;
            NPC target = Main.npc[attackTarget];
            NPC.direction = target.Center.X > NPC.Center.X ? 1 : -1;
            NPC.spriteDirection = NPC.direction;

            // ========== 添加光效与粒子（开始） ==========
            // 光芒随攻击进度增强
            float progress = (float)attackTimer / TotalAttackTime;
            float lightStrength = 0.5f + 1.0f * progress;
            Lighting.AddLight(NPC.Center, 1.0f * lightStrength, 0.5f * lightStrength, 0.8f * lightStrength);

            // 粒子：在半径 50 的圆内随机位置零星生成，整体密度大幅降低
            int dustCount = (int)Math.Ceiling(2f + 4f * progress);
            for (int i = 0; i < dustCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = 50f * (float)Math.Sqrt(Main.rand.NextFloat());
                Vector2 dustPos = NPC.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;

                Dust dust = Dust.NewDustPerfect(
                    dustPos, DustID.PinkFairy, Vector2.Zero,
                    100, default, 0.8f + progress * 0.6f);
                dust.noGravity = true;
                dust.velocity *= 0.3f;
            }
            // ========== 添加结束 ==========

            if (attackTimer == ProjectileSpawnTime)
                SpawnC8XProjectile(target);

            if (attackTimer >= TotalAttackTime)
            {
                CancelAttack();
            }
        }

        private bool IsTargetValid()
        {
            if (attackTarget == -1 || attackTarget >= Main.maxNPCs) return false;
            NPC target = Main.npc[attackTarget];
            if (!target.active || target.friendly || target.life <= 0) return false;
            float distance = Vector2.Distance(NPC.Center, target.Center);
            return distance <= MaxAttackDistance;
        }

        private void CancelAttack(bool startCooldown = true)
        {
            isAttacking = false;
            attackTimer = 0;
            attackTarget = -1;

            // 结束攻击一律进入冷却，避免敌人进出范围时反复起手抽搐
            if (startCooldown)
                attackCooldownTimer = AttackCooldown;

            NPC.netUpdate = true;
        }

        private void SpawnC8XProjectile(NPC target)
        {
            Vector2 direction = target.Center - NPC.Center;
            direction.Normalize();
            Vector2 velocity = direction * 12f;
            int damage = (int)(NPC.damage * 2.5f);

            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                velocity,
                ProjectileID.RainbowRodBullet,
                damage,
                5f,
                Main.myPlayer
            );

            SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
        }

        // === 动画帧 ===
        public override void FindFrame(int frameHeight)
        {
            if (isAttacking)
            {
                NPC.frameCounter = 0;
                int currentAttackFrame = attackTimer / FramesPerSecond;
                currentAttackFrame = Math.Clamp(currentAttackFrame, 0, 3);
                NPC.frame.Y = (AttackFrameStartIndex + currentAttackFrame) * frameHeight;
                NPC.spriteDirection = NPC.direction;
                return;
            }
            base.FindFrame(frameHeight);
        }

        // ========== 统一的本地化调用方法 ==========
        public string GetLocalizedText(string key, params object[] args)
        {
            string fullKey = $"Mods.Amphoreus.{key}";
            string text = Language.GetTextValue(fullKey);
            if (text == fullKey)
            {
                return $"[{key}]";
            }
            if (args != null && args.Length > 0)
            {
                try { return string.Format(text, args); } catch { return text; }
            }
            return text;
        }

        // === 条件对话键检测（仅环境和事件）===
        private string GetConditionalChatKey()
        {
            Player player = Main.LocalPlayer;
            if (player == null) return null;

            if (Main.bloodMoon) return "BloodMoon";
            if (Main.eclipse) return "Eclipse";
            if (Main.slimeRain) return "SlimeRain";
            if (Terraria.GameContent.Events.LanternNight.LanternsUp) return "LanternNight";   // 灯笼夜
            if (player.ZoneGraveyard) return "Graveyard";
            if (Main.raining) return "Rain";
            if (Terraria.GameContent.Events.BirthdayParty.PartyIsUp) return "Party";          // 派对
            if (Main.windSpeedCurrent > 0.4f) return "Windy";
            if (Main.dayTime)
            {
                if (Main.time < 27000) return "Morning";
            }
            else return "Night";

            if (player.ZoneForest) return !Main.dayTime ? "ForestNight" : "Forest";
            if (player.ZoneHallow) return "Hallow";
            if (player.ZoneCorrupt) return "Corruption";
            if (player.ZoneCrimson) return "Crimson";
            if (player.ZoneJungle) return "Jungle";
            if (player.ZoneSnow) return "Snow";
            if (player.ZoneDesert) return "Desert";
            if (player.ZoneBeach) return "Ocean";
            if (player.ZoneGlowshroom) return "Mushroom";
            if (player.ZoneUnderworldHeight) return "Hell";

            return null;
        }

        // === 获取随机日常问候（不含NPC评价）===
        private string GetRandomDailyChat()
        {
            string key = "NPCs.Xilian.Dialogue.Daily";
            string text = GetLocalizedText(key);
            if (!string.IsNullOrEmpty(text) && text != $"[{key}]")
            {
                string[] lines = text.Split('\n');
                if (lines.Length > 0)
                    return lines[Main.rand.Next(lines.Length)].Trim();
            }
            return "meme~";
        }

        // === 获取随机NPC评价（聊聊天专用）===
        private string GetRandomNPCTalk()
        {
            string key = "NPCs.Xilian.ChatMode.NPCTalk";
            string text = GetLocalizedText(key);
            if (!string.IsNullOrEmpty(text) && text != $"[{key}]")
            {
                string[] lines = text.Split('\n');
                if (lines.Length > 0)
                    return lines[Main.rand.Next(lines.Length)].Trim();
            }
            return "meme~";
        }

        // === 获取当前功能模式对应的第二个按钮文本 ===
        public string GetCurrentFunctionButtonText()
        {
            switch (_currentFunctionMode)
            {
                case FunctionMode.Chat:
                    return GetLocalizedText("NPCs.Xilian.Buttons.Chat");
                case FunctionMode.OracleCard:
                    return GetLocalizedText("NPCs.Xilian.Buttons.OracleCard");
                case FunctionMode.Shop:
                    return GetLocalizedText("NPCs.Xilian.Buttons.Shop");
                case FunctionMode.Transform:
                    return GetLocalizedText("NPCs.Xilian.Buttons.Transform");
                case FunctionMode.Quest:
                    return GetLocalizedText("NPCs.Xilian.Buttons.Quest");
                default:
                    return "";
            }
        }

        // === 获取当前功能模式对应的对话文本（用于模式切换显示）===
        public string GetCurrentModeDialogueText()
        {
            switch (_currentFunctionMode)
            {
                case FunctionMode.Chat:
                    return _currentChatText;  // 已在 GetChat 中初始化
                case FunctionMode.OracleCard:
                    return GetLocalizedText("NPCs.Xilian.FunctionModes.OracleCard.Dialogue");
                case FunctionMode.Shop:
                    return GetLocalizedText("NPCs.Xilian.FunctionModes.Shop.Dialogue");
                case FunctionMode.Transform:
                    return Main.hardMode
                        ? GetLocalizedText("NPCs.Xilian.FunctionModes.Transform.Enabled")
                        : GetLocalizedText("NPCs.Xilian.FunctionModes.Transform.Disabled");
                case FunctionMode.Quest:
                    return GetQuestDialogueText();
                default:
                    // 兜底：不应出现。若出现说明功能模式状态异常，给出可读文本而不是内部占位符
                    return GetLocalizedText("NPCs.Xilian.FunctionModes.Unknown");
            }
        }

        // === 任务模式：根据当前任务状态给出对应文本 ===
        private string GetQuestDialogueText()
        {
            var available = XiLianQuestSystem.GetFirstAvailableQuest();

            if (available == null)
            {
                if (XiLianQuestSystem.CurrentQuest != null && XiLianQuestSystem.CurrentQuest.IsActive)
                {
                    return XiLianQuestSystem.CanCompleteCurrentQuest(Main.LocalPlayer)
                        ? XiLianQuestSystem.CurrentQuest.CompletionText
                        : GetQuestRequirementHint(XiLianQuestSystem.CurrentQuest);
                }

                return GetLocalizedText("NPCs.Xilian.FunctionModes.Quest.NoQuest");
            }

            return available.Description;
        }

        // === 任务模式：列出还缺哪些材料 ===
        private string GetQuestRequirementHint(XiLianQuestData quest)
        {
            var player = Main.LocalPlayer;
            var lines = new List<string> { quest.Description };

            foreach (var req in quest.RequiredItems)
            {
                int have = XiLianQuestSystem.CountItemsInInventory(player, req.Key);
                string itemName = Lang.GetItemNameValue(req.Key);
                lines.Add($"{itemName}: {have}/{req.Value}");
            }

            return string.Join("\n", lines);
        }

        // === 切换功能模式 ===
        public void SwitchFunctionMode()
        {
            _functionModeIndex++;
            if (_functionModeIndex >= _functionModeOrder.Length)
                _functionModeIndex = 0;
            _currentFunctionMode = _functionModeOrder[_functionModeIndex];
            NPC.netUpdate = true;
            SoundEngine.PlaySound(SoundID.MenuTick);

            string newText;
            if (_currentFunctionMode == FunctionMode.Chat)
            {
                // 切换到聊天模式时，直接生成一句日常问候（不含评价）
                _currentChatText = GetRandomDailyChat();
                newText = _currentChatText;
            }
            else
            {
                newText = GetCurrentModeDialogueText();
            }
            SetChatText(newText);
        }

        /// <summary>
        /// 统一的对话文本刷新入口。
        /// 走自定义对话框时把文本交给 XiLianDialogueSystem 显示，
        /// 否则写原版的 Main.npcChatText。
        /// </summary>
        private void SetChatText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (Config.UseCustomDialogue)
            {
                ModContent.GetInstance<XiLianDialogueSystem>().ShowDialogue(text, NPC);
            }
            else
            {
                Main.npcChatText = text;
            }
        }

        // === 处理第二个按钮点击 ===
        public void HandleSecondButton()
        {
            var player = Main.LocalPlayer;
            var cardPlayer = player.GetModPlayer<Shenyupai_Player>();

            switch (_currentFunctionMode)
            {
                case FunctionMode.Chat:
                    // 聊聊天：从NPC评价池中随机获取一句
                    _currentChatText = GetRandomNPCTalk();
                    break;

                case FunctionMode.OracleCard:
                    cardPlayer.DrawCard();
                    if (cardPlayer.CurrentCard != null)
                    {
                        _currentChatText = $"你抽到了：【{cardPlayer.CurrentCard.Name}】\n" +
                                          $"{cardPlayer.CurrentCard.Description}\n\n" +
                                          $"效果将持续{cardPlayer.BuffTimeLeftFormatted}（24分钟游戏时间）";
                    }
                    else
                    {
                        _currentChatText = GetLocalizedText("NPCs.Xilian.OracleCard.Failed");
                    }
                    for (int i = 0; i < 20; i++)
                        Dust.NewDustDirect(NPC.Center, 30, 30, DustID.PurpleTorch, 0f, 0f, 150, default, 1.5f);
                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                    break;

                case FunctionMode.Shop:
                    // 已在 OnChatButtonClicked 中处理
                    break;

                case FunctionMode.Transform:
                    if (Main.hardMode)
                    {
                        Main.npcChatText = "";
                        ModContent.GetInstance<XiLianDialogueSystem>().HideUI();
                        TransformXilianToMimi();
                    }
                    else
                    {
                        _currentChatText = GetLocalizedText("NPCs.Xilian.FunctionModes.Transform.Disabled");
                    }
                    break;

                case FunctionMode.Quest:
                    HandleQuestButton(player);
                    break;
            }
        }

        // === 任务模式按钮：接取 / 提示 / 提交 ===
        private void HandleQuestButton(Player player)
        {
            var current = XiLianQuestSystem.CurrentQuest;

            // 已有进行中的任务
            if (current != null && current.IsActive)
            {
                if (XiLianQuestSystem.CanCompleteCurrentQuest(player))
                {
                    XiLianQuestSystem.CompleteCurrentQuest(player);
                    _currentChatText = GetLocalizedText("NPCs.Xilian.FunctionModes.Quest.Complete");
                }
                else
                {
                    _currentChatText = GetQuestRequirementHint(current);
                }
                return;
            }

            // 没有进行中的任务：尝试接取
            var available = XiLianQuestSystem.GetFirstAvailableQuest();
            if (available != null)
            {
                XiLianQuestSystem.AcceptQuest(available.QuestType);
                _currentChatText = GetLocalizedText("NPCs.Xilian.FunctionModes.Quest.Accepted");
            }
            else
            {
                _currentChatText = GetLocalizedText("NPCs.Xilian.FunctionModes.Quest.NoQuest");
            }
        }

        // ========== 商店实现 ==========
        public override void AddShops()
        {
            var shop = new NPCShop(Type, "Shop");
            shop.Add(4961); // 七彩草蛉
            shop.Add(1326); // 混沌传送杖
            shop.Add(75);   // 坠落之星
            shop.Add(501);  // 妖精尘
            shop.Add(4293); // 桃子
            shop.Add(4618); // 桃子果酒
            shop.Add(502);  // 水晶碎块
            shop.Add(495);  // 彩虹魔杖
            shop.Add(2310); // 七彩矿鱼
            shop.Add(3209); // 水晶蛇

            // === 花茶：完成对应任务章节后才上架 ===
            shop.Add(ModContent.ItemType<Items.XiLian_Items.HerbTea.SunflowerHerbTea>(),
                new Condition("Mods.Amphoreus.NPCs.Xilian.ShopConditions.Chapter1",
                    () => XiLianQuestSystem.Chapter1Completed));
            shop.Add(ModContent.ItemType<Items.XiLian_Items.HerbTea.MoonflowerHerbTea>(),
                new Condition("Mods.Amphoreus.NPCs.Xilian.ShopConditions.Chapter1",
                    () => XiLianQuestSystem.Chapter1Completed));
            shop.Add(ModContent.ItemType<Items.XiLian_Items.HerbTea.ShiningRootHerbTea>(),
                new Condition("Mods.Amphoreus.NPCs.Xilian.ShopConditions.Chapter2",
                    () => XiLianQuestSystem.Chapter2Completed));
            shop.Add(ModContent.ItemType<Items.XiLian_Items.HerbTea.FireblossomHerbTea>(),
                new Condition("Mods.Amphoreus.NPCs.Xilian.ShopConditions.Chapter3",
                    () => XiLianQuestSystem.Chapter3Completed));
            shop.Add(ModContent.ItemType<Items.XiLian_Items.HerbTea.ChillthornHerbTea>(),
                new Condition("Mods.Amphoreus.NPCs.Xilian.ShopConditions.Chapter3",
                    () => XiLianQuestSystem.Chapter3Completed));

            shop.Register();
        }

        // ========== 原版UI的按钮设置 ==========
        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = GetLocalizedText("NPCs.Xilian.Buttons.ChangeTopic");
            button2 = GetCurrentFunctionButtonText();
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                // 点击“换话题”
                SwitchFunctionMode();
            }
            else
            {
                if (_currentFunctionMode == FunctionMode.Shop)
                {
                    Main.npcChatText = "";
                    ModContent.GetInstance<XiLianDialogueSystem>().HideUI();
                    shopName = "Shop";
                    return;
                }

                // 处理第二个按钮（聊聊天/神谕牌/变身/任务）
                HandleSecondButton();

                // HandleSecondButton 已把结果写进 _currentChatText 的模式，优先显示它，
                // 否则会被通用文案覆盖（聊聊天拿到的 NPC 评价就会丢）。
                if (_currentFunctionMode != FunctionMode.Shop)
                {
                    bool handledByButton =
                        _currentFunctionMode == FunctionMode.Chat ||
                        _currentFunctionMode == FunctionMode.OracleCard ||
                        _currentFunctionMode == FunctionMode.Quest ||
                        (_currentFunctionMode == FunctionMode.Transform && !Main.hardMode);

                    bool hasResult = handledByButton && !string.IsNullOrEmpty(_currentChatText);

                    SetChatText(hasResult ? _currentChatText : GetCurrentModeDialogueText());
                }
            }
        }

        // ========== 自定义UI控制 ==========
        private Configs.UISettings Config => ModContent.GetInstance<Configs.UISettings>();

        public override bool CanChat()
        {
            if (Config.UseCustomDialogue && ModContent.GetInstance<XiLianDialogueSystem>().IsActive)
                return false;
            return base.CanChat();
        }

        // === 主对话方法（右键点击时调用）===
        public override string GetChat()
        {
            string chatText = "";

            // 与昔涟对话即视为已结识，用于解锁花茶任务第一章
            BookDataSystem.OnXiLianDialogue();
            XiLianQuestSystem.CheckQuestUnlockStatus();

            if (_currentFunctionMode == FunctionMode.Chat)
            {
                // 日常触发：75%环境对话，25%日常问候（不含NPC评价）
                if (Main.rand.NextFloat() < 0.75f)
                {
                    string conditionKey = GetConditionalChatKey();
                    if (!string.IsNullOrEmpty(conditionKey))
                    {
                        // GetLocalizedText 在键缺失时返回 "[键名]"，据此判断是否需要回退
                        string envKey = $"NPCs.Xilian.EnvironmentChat.{conditionKey}";
                        string conditionText = GetLocalizedText(envKey);
                        if (conditionText != $"[{envKey}]")
                        {
                            string[] lines = conditionText.Split('\n');
                            if (lines.Length > 0)
                                chatText = lines[Main.rand.Next(lines.Length)].Trim();
                        }
                    }
                }
                if (string.IsNullOrEmpty(chatText))
                {
                    chatText = GetRandomDailyChat();
                }
            }
            else
            {
                chatText = GetCurrentModeDialogueText();
            }

            _currentChatText = chatText;

            if (Config.UseCustomDialogue)
            {
                ModContent.GetInstance<XiLianDialogueSystem>().ShowDialogue(chatText, NPC);
                return "";
            }
            return chatText;
        }

        // === 形态切换 ===
        public void TransformXilianToMimi()
        {
            Vector2 position = NPC.Center;

            if (!NPC.homeless)
            {
                NPC.homeless = true;
                NPC.homeTileX = 0;
                NPC.homeTileY = 0;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (i != NPC.whoAmI && Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Xilian>())
                {
                    if (Main.netMode == NetmodeID.MultiplayerClient || Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                    Main.npc[i].active = false;
                }
            }
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Mimi>())
                {
                    if (Main.netMode == NetmodeID.MultiplayerClient || Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                    Main.npc[i].active = false;
                }
            }

            NPC.active = false;
            int mimiIndex = NPC.NewNPC(NPC.GetSource_NaturalSpawn(),
                (int)position.X, (int)position.Y, ModContent.NPCType<Mimi>());
            NPC mimi = Main.npc[mimiIndex];
            mimi.homeless = true;

            WorldStateSystem.UpdateFormState(WorldStateSystem.NpcForm.Mimi, position);
            WorldStateSystem.SetMimiMode(WorldStateSystem.MimiMode.Daily);

            for (int i = 0; i < 15; i++)
                Dust.NewDustDirect(position, 20, 20, DustID.PurpleTorch, 0f, 0f, 150, default, 1.5f);
            SoundEngine.PlaySound(SoundID.Item8, position);

            if (mimi.ModNPC is Mimi mimiNPC)
            {
                string chatText = mimiNPC.GetChat();
                if (!string.IsNullOrEmpty(chatText))
                {
                    SetChatText(chatText);
                }
            }
        }

        // === 数据保存 ===
        public override void SaveData(TagCompound tag)
        {
            tag["isFollowingPlayer"] = isFollowingPlayer;
            tag["followingPlayerIndex"] = followingPlayerIndex;
            tag["functionModeIndex"] = _functionModeIndex;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("isFollowingPlayer"))
                isFollowingPlayer = tag.GetBool("isFollowingPlayer");
            if (tag.ContainsKey("followingPlayerIndex"))
                followingPlayerIndex = tag.GetInt("followingPlayerIndex");
            if (tag.ContainsKey("functionModeIndex"))
                _functionModeIndex = tag.GetInt("functionModeIndex");
            _currentFunctionMode = _functionModeOrder[_functionModeIndex];
        }

        // === 网络同步 ===
        public override void SendExtraAI(System.IO.BinaryWriter writer)
        {
            writer.Write(isFollowingPlayer);
            writer.Write(followingPlayerIndex);
            writer.Write(isAttacking);
            writer.Write(attackTimer);
            writer.Write(attackTarget);
            writer.Write(attackCooldownTimer);
            writer.Write(_functionModeIndex);
        }

        public override void ReceiveExtraAI(System.IO.BinaryReader reader)
        {
            isFollowingPlayer = reader.ReadBoolean();
            followingPlayerIndex = reader.ReadInt32();
            isAttacking = reader.ReadBoolean();
            attackTimer = reader.ReadInt32();
            attackTarget = reader.ReadInt32();
            attackCooldownTimer = reader.ReadInt32();
            _functionModeIndex = reader.ReadInt32();
            _currentFunctionMode = _functionModeOrder[_functionModeIndex];
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (NPC.townNPC)
                WorldStateSystem.UpdateFormState(WorldStateSystem.NpcForm.Xilian, NPC.Center);
        }
    }
}