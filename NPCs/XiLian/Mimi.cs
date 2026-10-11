using Amphoreus.Configs;
using Amphoreus.Systems.Ruwosuoshu_Systems;
using Amphoreus.Systems.XiLian_Systems.Touxiang;
using Amphoreus.Systems.XiLian_Systems.Xinyuan;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Amphoreus.NPCs.XiLian
{
    public class Mimi : ModNPC
    {
        public enum MimiAnimationState
        {
            Idle,
            Moving,
            Wink,
            Happy,
            Angry,
            Confused,
            Sleeping,
            Serious,
            Thinking,
            MimiGag,
            NewAttacking,
            Attacking
        }

        // 语言阶段枚举
        public enum MimiLanguageStage
        {
            Early,    // 早期：简单迷迷语
            Mid,      // 中期：夹杂少量词汇，带括号说明
            Fluent    // 后期：流畅说话
        }

        private int followingPlayerIndex = -1;
        private int buffCooldownTimer = 0;
        private int attackAnimationTimer = 0;
        private bool isAttacking = false;
        private MimiAnimationState currentAnimationState = MimiAnimationState.Idle;
        private int animationStateTimer = 0;
        private int expressionCooldown = 0;
        private int dialogWinkCooldown = 0;
        private int sleepCheckTimer = 0;
        private int currentDialogOption = 0;
        private int newAttackTimer = 0;
        private bool isNewAttacking = false;
        private int attackCycleFrameTimer = 0;
        private bool isCheckingIdle = false;
        private Vector2 lastPlayerPosition = Vector2.Zero;
        private int idleCheckTimer = 0;
        private int confusionBeforeSleepTimer = 0;
        private int normalAttackCounter = 0;
        private int superAttackCooldown = 0;
        private const float EnemyMidPointDetectionRange = 300f;

        // === 闲置模式 ===
        private bool isIdleMode = false;
        private int idleModeCooldown = 0;

        // === 无玩家计时器 ===
        private int noPlayerTimer = 0;
        private const int NoPlayerIdleDelay = 60 * 60; // 60秒

        // === 呼唤系统相关 ===
        private int callResponseTimer = 0;
        private int callCooldownTimer = 0;
        private bool isCallTriggered = false;
        private const int CallResponseDelay = 180;
        private const int CallCooldown = 60 * 60;

        // === 记录上一次检测的聊天文本 ===
        private string lastChatText = "";

        private string _currentChatText = "";

        // +++ 功能模式枚举 +++
        public enum MimiFunctionMode
        {
            Chat = 0,
            IdleToggle = 1,
            Transform = 2,
            Inventory = 3,
            Expressions = 4,
            Quest = 5
        }

        private MimiFunctionMode currentFunctionMode = MimiFunctionMode.Chat;
        private MimiFunctionMode[] functionModeOrder = new MimiFunctionMode[]
        {
            MimiFunctionMode.Chat,
            MimiFunctionMode.IdleToggle,
            MimiFunctionMode.Transform,
            MimiFunctionMode.Inventory,
            MimiFunctionMode.Expressions,
            MimiFunctionMode.Quest
        };
        private int currentFunctionModeIndex = 0;

        // === 统一的本地化调用方法 ===
        public string GetLocalizedText(string key, params object[] args)
        {
            string fullKey = $"Mods.Amphoreus.{key}";
            string text = Language.GetTextValue(fullKey);
            if (text == fullKey) return $"[{key}]";
            if (args != null && args.Length > 0)
            {
                try { return string.Format(text, args); }
                catch { return text; }
            }
            return text;
        }

        private string GetFunctionModeName(MimiFunctionMode mode)
        {
            string modeKey = mode switch
            {
                MimiFunctionMode.Chat => "NPCs.Mimi.FunctionModes.Chat.DisplayName",
                MimiFunctionMode.IdleToggle => "NPCs.Mimi.FunctionModes.IdleToggle.DisplayName",
                MimiFunctionMode.Transform => "NPCs.Mimi.FunctionModes.Transform.DisplayName",
                MimiFunctionMode.Inventory => "NPCs.Mimi.FunctionModes.Inventory.DisplayName",
                MimiFunctionMode.Expressions => "NPCs.Mimi.FunctionModes.Expressions.DisplayName",
                MimiFunctionMode.Quest => "NPCs.Mimi.FunctionModes.Quest.DisplayName",
                _ => ""
            };
            return GetLocalizedText(modeKey);
        }

        public override string Texture => "Amphoreus/NPCs/XiLian/Mimi";
        private static Texture2D _portraitTexture;
        public static Texture2D PortraitTexture => _portraitTexture;
        public virtual string PortraitTexturePath => "Amphoreus/NPCs/XiLian/Mimi_Touxiang";

        public bool IsIdleMode => isIdleMode;
        public int FollowingPlayerIndex
        {
            get => followingPlayerIndex;
            set
            {
                if (followingPlayerIndex != value)
                {
                    followingPlayerIndex = value;
                    NPC.netUpdate = true;
                }
            }
        }

        // 常数定义
        private const float MaxFollowDistance = 600f;
        private const float TeleportDistance = 1200f;
        private const float FollowAcceleration = 0.2f;
        private const float FollowDeceleration = 0.4f;
        private const float MaxSpeed = 10f;
        private const int BuffCooldown = 60 * 60;
        private const int AttackDuration = 60 * 4;
        private const int NewAttackWindupFrames = 30;
        private const int NewAttackLoopFrames = 9999;
        private const int NewAttackProjectileInterval = 20;
        private const int AttackCycleFrameInterval = 10;
        private const int ScreenDetectionRange = 1200;
        private const int SuperAttackThreshold = 93;
        private const int SuperAttackCooldownDuration = 60 * 30;
        private const float SuperAttackLaunchDistance = 150f;

        // 日常模式攻击条件
        private const float DailyModeAttackHealthThreshold = 0.25f;
        private const float DailyModeDetectionRangeLowHealth = 1200f;
        private const float DailyModeDetectionRangeNormal = 300f;

        private const float DailyFollowDistance = 400f;
        private const float DailyTeleportDistance = 1000f;
        private const float DailyMaxSpeed = 8f;
        private const int ExpressionDuration = 60 * 4;
        private const int MimiGagDuration = 60 * 4;
        private const int IdleCheckInitialInterval = 60 * 60;
        private const int IdleCheckFastInterval = 60;
        private const int ConfusionDuration = 60 * 3;
        private const int HappyDuration = 60 * 3;
        private const float MoveThreshold = 10f;
        private const int MouseClickCooldown = 30;

        // === 获取当前语言阶段 ===
        private MimiLanguageStage GetMimiLanguageStage()
        {
            var quest2 = MimiQuestSystem.GetQuest(MimiQuestType.LifeCrystalStage2);
            var quest3 = MimiQuestSystem.GetQuest(MimiQuestType.LifeCrystalStage3);
            if (quest3 != null && quest3.IsUnlocked) return MimiLanguageStage.Fluent;
            else if (quest2 != null && quest2.IsUnlocked) return MimiLanguageStage.Mid;
            else return MimiLanguageStage.Early;
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 23;
            NPCID.Sets.ActsLikeTownNPC[Type] = true;
            NPCID.Sets.ImmuneToAllBuffs[Type] = true;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0) { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
            NPCID.Sets.ExtraFramesCount[Type] = 9;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.AttackType[Type] = 1;
            NPCID.Sets.HatOffsetY[Type] = 4;

            if (_portraitTexture == null)
                _portraitTexture = ModContent.Request<Texture2D>(PortraitTexturePath).Value;
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 40;
            NPC.height = 54;
            NPC.aiStyle = -1;
            NPC.lifeMax = 1111;
            NPC.immortal = true;
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.damage = 11;
            NPC.knockBackResist = 0f;
            NPC.homeless = true;
        }

        public override bool CanGoToStatue(bool toKingStatue) => false;
        public override bool CheckConditions(int left, int right, int top, int bottom) => false;

        public override bool CanChat()
        {
            bool isBattleMode = Systems.XiLian_Systems.Xingtai.WorldStateSystem.CurrentMimiMode ==
                Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Battle;
            if (ModContent.GetInstance<MimiDialogueSystem>().IsActive) return false;
            if (isBattleMode) return false;
            return base.CanChat();
        }

        // 网络同步
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(followingPlayerIndex);
            writer.Write(buffCooldownTimer);
            writer.Write(attackAnimationTimer);
            writer.Write(isAttacking);
            writer.Write((int)currentAnimationState);
            writer.Write(animationStateTimer);
            writer.Write(expressionCooldown);
            writer.Write(dialogWinkCooldown);
            writer.Write(sleepCheckTimer);
            writer.Write(currentDialogOption);
            writer.Write(idleCheckTimer);
            writer.Write(isCheckingIdle);
            writer.Write(confusionBeforeSleepTimer);
            writer.Write(lastPlayerPosition.X);
            writer.Write(lastPlayerPosition.Y);
            writer.Write(newAttackTimer);
            writer.Write(isNewAttacking);
            writer.Write(attackCycleFrameTimer);
            writer.Write(normalAttackCounter);
            writer.Write(superAttackCooldown);
            writer.Write(isIdleMode);
            writer.Write(idleModeCooldown);
            writer.Write(noPlayerTimer);
            writer.Write((int)currentFunctionMode);
            writer.Write(callResponseTimer);
            writer.Write(callCooldownTimer);
            writer.Write(isCallTriggered);
            writer.Write(lastChatText);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            followingPlayerIndex = reader.ReadInt32();
            buffCooldownTimer = reader.ReadInt32();
            attackAnimationTimer = reader.ReadInt32();
            isAttacking = reader.ReadBoolean();
            currentAnimationState = (MimiAnimationState)reader.ReadInt32();
            animationStateTimer = reader.ReadInt32();
            expressionCooldown = reader.ReadInt32();
            dialogWinkCooldown = reader.ReadInt32();
            sleepCheckTimer = reader.ReadInt32();
            currentDialogOption = reader.ReadInt32();
            idleCheckTimer = reader.ReadInt32();
            isCheckingIdle = reader.ReadBoolean();
            confusionBeforeSleepTimer = reader.ReadInt32();
            float lastX = reader.ReadSingle();
            float lastY = reader.ReadSingle();
            lastPlayerPosition = new Vector2(lastX, lastY);
            newAttackTimer = reader.ReadInt32();
            isNewAttacking = reader.ReadBoolean();
            attackCycleFrameTimer = reader.ReadInt32();
            normalAttackCounter = reader.ReadInt32();
            superAttackCooldown = reader.ReadInt32();
            isIdleMode = reader.ReadBoolean();
            idleModeCooldown = reader.ReadInt32();
            noPlayerTimer = reader.ReadInt32();
            currentFunctionMode = (MimiFunctionMode)reader.ReadInt32();
            callResponseTimer = reader.ReadInt32();
            callCooldownTimer = reader.ReadInt32();
            isCallTriggered = reader.ReadBoolean();
            lastChatText = reader.ReadString();
        }

        // === 核心AI ===
        public override void AI()
        {
            CheckBattleModeTransition();
            if (!ShouldExist()) { NPC.velocity = Vector2.Zero; return; }

            bool isBattleMode = Systems.XiLian_Systems.Xingtai.WorldStateSystem.CurrentMimiMode ==
                Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Battle;

            // 辅助攻击（声援）最高优先级
            if (isAttacking)
            {
                attackAnimationTimer++;
                if (attackAnimationTimer == AttackDuration) ApplyMimiBuffToPlayers();
                if (attackAnimationTimer >= AttackDuration)
                {
                    isAttacking = false;
                    attackAnimationTimer = 0;
                    NPC.netUpdate = true;
                }
                if (attackAnimationTimer > 0 && attackAnimationTimer < AttackDuration)
                    NPC.velocity *= 0.5f;
                currentAnimationState = MimiAnimationState.Attacking;
                ExecuteFollowing(true);
                animationStateTimer++;
                return;
            }

            if (!isIdleMode) CheckAndUpdateFollowingPlayer(isBattleMode);
            CheckPlayerCall();

            if (callResponseTimer > 0)
            {
                callResponseTimer--;
                if (callResponseTimer <= 0 && isCallTriggered) RespondToCall();
            }
            if (callCooldownTimer > 0) callCooldownTimer--;
            if (dialogWinkCooldown > 0) dialogWinkCooldown--;
            if (superAttackCooldown > 0) superAttackCooldown--;
            if (idleModeCooldown > 0) idleModeCooldown--;

            if (isNewAttacking)
            {
                ExecuteNewAttackAnimation();
                ExecuteFollowing(isBattleMode);
                animationStateTimer++;
                return;
            }

            if (!isIdleMode && !isAttacking)
                CheckAndStartNewAttack(isBattleMode);

            if (isBattleMode && !isIdleMode)
                ExecuteBattleModeAI();
            else if (!isIdleMode)
                ExecuteDailyModeAI();
            else
                ExecuteIdleModeAI();

            animationStateTimer++;
            attackCycleFrameTimer++;
        }

        // === 检查墙壁阻挡（排除平台）===
        private bool HasWallBetween(Vector2 start, Vector2 end)
        {
            float distance = Vector2.Distance(start, end);
            Vector2 direction = Vector2.Normalize(end - start);
            int steps = (int)(distance / 16f) + 1;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 checkPos = start + direction * (distance * i / steps);
                int tileX = (int)(checkPos.X / 16f);
                int tileY = (int)(checkPos.Y / 16f);
                if (tileX >= 0 && tileX < Main.maxTilesX && tileY >= 0 && tileY < Main.maxTilesY)
                {
                    Tile tile = Main.tile[tileX, tileY];
                    if (tile.HasTile && Main.tileSolid[tile.TileType])
                    {
                        if (Main.tileSolidTop[tile.TileType]) continue;
                        return true;
                    }
                }
            }
            return false;
        }

        // === 战斗模式切换 ===
        private void CheckBattleModeTransition()
        {
            bool hasActiveBoss = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.boss && !npc.friendly && npc.life > 0)
                {
                    hasActiveBoss = true;
                    break;
                }
            }
            var currentMode = Systems.XiLian_Systems.Xingtai.WorldStateSystem.CurrentMimiMode;
            if (hasActiveBoss && currentMode != Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Battle)
            {
                Systems.XiLian_Systems.Xingtai.WorldStateSystem.SetMimiMode(
                    Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Battle);
                normalAttackCounter = 0;
                superAttackCooldown = 0;
                NPC.netUpdate = true;
            }
            else if (!hasActiveBoss && currentMode == Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Battle)
            {
                Systems.XiLian_Systems.Xingtai.WorldStateSystem.SetMimiMode(
                    Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Daily);
                normalAttackCounter = 0;
                if (isNewAttacking) StopNewAttack();
                NPC.netUpdate = true;
            }
        }

        private void CheckAndUpdateFollowingPlayer(bool isBattleMode)
        {
            if (isIdleMode) return;
            if (FollowingPlayerIndex != -1)
            {
                Player targetPlayer = Main.player[FollowingPlayerIndex];
                if (targetPlayer.active && !targetPlayer.dead) { noPlayerTimer = 0; return; }
                else FollowingPlayerIndex = -1;
            }
            FindNearestActivePlayer();
            if (FollowingPlayerIndex == -1)
            {
                noPlayerTimer++;
                if (noPlayerTimer >= NoPlayerIdleDelay && !isBattleMode && !ShouldExistInBattle())
                {
                    if (!isIdleMode)
                    {
                        isIdleMode = true;
                        noPlayerTimer = 0;
                        NPC.netUpdate = true;
                        string idleModeText = GetLocalizedText("NPCs.Mimi.System.EnterIdleMode");
                        Main.NewText(idleModeText, Color.LightBlue);
                    }
                }
            }
            else noPlayerTimer = 0;
        }

        private bool ShouldExistInBattle()
        {
            var eventSystem = ModContent.GetInstance<Systems.XiLian_Systems.Xingtai.BossEventSystem>();
            return eventSystem?.IsInBattle() ?? false;
        }

        private void CheckPlayerCall()
        {
            if (Main.netMode == NetmodeID.Server) return;
            if (callCooldownTimer > 0) return;
            if (!Main.drawingPlayerChat) return;
            if (Main.LocalPlayer.talkNPC != -1 || Main.LocalPlayer.sign != -1) return;

            string currentChatText = Main.GetInputText(Main.chatText)?.ToLower() ?? "";
            bool isMatch = currentChatText == "迷迷！" || currentChatText == "mem！" ||
                          currentChatText == "迷迷" || currentChatText == "mem";
            if (isMatch && !isCallTriggered && currentChatText != lastChatText)
            {
                isCallTriggered = true;
                callResponseTimer = CallResponseDelay;
                NPC.netUpdate = true;
                string heardCallText = GetLocalizedText("NPCs.Mimi.System.HeardCall");
                Main.NewText(heardCallText, Color.LightBlue);
            }
            lastChatText = currentChatText;
        }

        private void RespondToCall()
        {
            isCallTriggered = false;
            callCooldownTimer = CallCooldown;
            MimiLanguageStage languageStage = GetMimiLanguageStage();
            string responseText = "";
            if (isIdleMode)
            {
                isIdleMode = false;
                idleModeCooldown = MouseClickCooldown;
                noPlayerTimer = 0;
                NPC.netUpdate = true;
                SoundEngine.PlaySound(SoundID.MenuTick);
                responseText = GetLocalizedText($"NPCs.Mimi.CallResponse.Idle.{languageStage}");
            }
            else
            {
                currentAnimationState = MimiAnimationState.Happy;
                animationStateTimer = 0;
                NPC.netUpdate = true;
                responseText = GetLocalizedText($"NPCs.Mimi.CallResponse.Active.{languageStage}");
            }
            Main.NewText(responseText, Color.LightBlue);
        }

        private void ExecuteIdleModeAI()
        {
            NPC.velocity = Vector2.Zero;
            if (!isNewAttacking) currentAnimationState = MimiAnimationState.Idle;
            UpdateAnimationState();
        }

        private void CheckAndStartNewAttack(bool isBattleMode)
        {
            if (isAttacking || isNewAttacking) return;

            // 检测中心为玩家（若无效则为自己）
            Vector2 centerForDetection;
            Player targetPlayer = null;
            if (FollowingPlayerIndex != -1 && FollowingPlayerIndex < Main.maxPlayers)
            {
                targetPlayer = Main.player[FollowingPlayerIndex];
                if (targetPlayer.active && !targetPlayer.dead)
                    centerForDetection = targetPlayer.Center;
                else
                    centerForDetection = NPC.Center;
            }
            else centerForDetection = NPC.Center;

            float detectionRange = 0f;
            if (isBattleMode) detectionRange = ScreenDetectionRange;
            else
            {
                if (targetPlayer == null || !targetPlayer.active || targetPlayer.dead)
                {
                    FindNearestActivePlayer();
                    if (FollowingPlayerIndex != -1 && FollowingPlayerIndex < Main.maxPlayers)
                        targetPlayer = Main.player[FollowingPlayerIndex];
                }
                if (targetPlayer != null && targetPlayer.active && !targetPlayer.dead)
                {
                    float healthPercent = (float)targetPlayer.statLife / targetPlayer.statLifeMax2;
                    if (healthPercent <= DailyModeAttackHealthThreshold)
                        detectionRange = DailyModeDetectionRangeLowHealth;
                    else
                        detectionRange = DailyModeDetectionRangeNormal;
                }
                else return;
            }

            NPC targetEnemy = null;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.life > 0 &&
                    npc.type != NPCID.TargetDummy && npc.CanBeChasedBy())
                {
                    float distance = Vector2.Distance(centerForDetection, npc.Center);
                    if (distance < detectionRange)
                    {
                        // 日常模式检查墙壁：以迷迷自身为起点
                        if (!isBattleMode && HasWallBetween(NPC.Center, npc.Center))
                            continue;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            targetEnemy = npc;
                        }
                    }
                }
            }
            if (targetEnemy != null) StartNewAttackAnimation(targetEnemy);
        }

        private void StartNewAttackAnimation(NPC target)
        {
            isNewAttacking = true;
            newAttackTimer = 0;
            attackCycleFrameTimer = 0;
            currentAnimationState = MimiAnimationState.NewAttacking;
            NPC.netUpdate = true;
        }

        private void ExecuteNewAttackAnimation()
        {
            bool isBattleMode = Systems.XiLian_Systems.Xingtai.WorldStateSystem.CurrentMimiMode ==
                Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Battle;

            // 检测中心为玩家（若无则为自己）
            Vector2 centerForDetection;
            Player targetPlayer = null;
            if (FollowingPlayerIndex != -1 && FollowingPlayerIndex < Main.maxPlayers)
            {
                targetPlayer = Main.player[FollowingPlayerIndex];
                if (targetPlayer.active && !targetPlayer.dead)
                    centerForDetection = targetPlayer.Center;
                else
                    centerForDetection = NPC.Center;
            }
            else centerForDetection = NPC.Center;

            float detectionRange = 0f;
            if (isBattleMode) detectionRange = ScreenDetectionRange;
            else
            {
                if (targetPlayer == null || !targetPlayer.active || targetPlayer.dead)
                {
                    FindNearestActivePlayer();
                    if (FollowingPlayerIndex != -1 && FollowingPlayerIndex < Main.maxPlayers)
                        targetPlayer = Main.player[FollowingPlayerIndex];
                }
                if (targetPlayer != null && targetPlayer.active && !targetPlayer.dead)
                {
                    float healthPercent = (float)targetPlayer.statLife / targetPlayer.statLifeMax2;
                    if (healthPercent <= DailyModeAttackHealthThreshold)
                        detectionRange = DailyModeDetectionRangeLowHealth;
                    else
                        detectionRange = DailyModeDetectionRangeNormal;
                }
                else detectionRange = DailyModeDetectionRangeNormal;
            }

            NPC targetEnemy = null;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.life > 0 &&
                    npc.type != NPCID.TargetDummy && npc.CanBeChasedBy())
                {
                    float distance = Vector2.Distance(centerForDetection, npc.Center);
                    if (distance < detectionRange && distance < closestDistance)
                    {
                        // 日常模式检查墙壁：以迷迷自身为起点
                        if (!isBattleMode && HasWallBetween(NPC.Center, npc.Center))
                            continue;
                        closestDistance = distance;
                        targetEnemy = npc;
                    }
                }
            }
            if (targetEnemy == null) { StopNewAttack(); return; }

            // 设置朝向
            if (Math.Abs(targetEnemy.Center.X - NPC.Center.X) > 10f)
            {
                NPC.direction = NPC.Center.X < targetEnemy.Center.X ? 1 : -1;
                NPC.spriteDirection = NPC.direction;
            }

            // 发射弹幕
            if (newAttackTimer >= NewAttackWindupFrames &&
                (newAttackTimer - NewAttackWindupFrames) % NewAttackProjectileInterval == 0)
            {
                ShootMimiZhuaProjectile(targetEnemy, isBattleMode);
            }

            if (newAttackTimer >= NewAttackWindupFrames)
            {
                NPC.damage = 11;
                NPC.knockBackResist = 0f;
            }
            else NPC.damage = 0;

            newAttackTimer++;
            if (newAttackTimer >= NewAttackWindupFrames + NewAttackLoopFrames)
                newAttackTimer = NewAttackWindupFrames + (newAttackTimer % NewAttackLoopFrames);
        }

        // === 发射弹幕（根据模式选择类型）===
        private void ShootMimiZhuaProjectile(NPC target, bool isBattleMode = false)
        {
            if (isAttacking) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int projectileCount = Main.rand.Next(3, 6);
            int direction = NPC.direction;
            if (direction == 0) direction = 1;

            int projectileType = isBattleMode
                ? ModContent.ProjectileType<Projectiles.XiLian_Projectiles.Mimi.Mimi_zhua>()
                : ModContent.ProjectileType<Projectiles.XiLian_Projectiles.Mimi.Mimi_zhua_NoWall>();

            Vector2 basePosition = NPC.Center + new Vector2(direction * 20f, 0f);
            float planeHalfLength = 30f;

            for (int i = 0; i < projectileCount; i++)
            {
                float randomY = Main.rand.NextFloat(-planeHalfLength, planeHalfLength);
                Vector2 spawnPosition = basePosition + new Vector2(0f, randomY);
                Vector2 velocity = Vector2.Zero;

                if (target != null && target.active)
                {
                    Vector2 toTarget = target.Center - spawnPosition;
                    float spreadAngle = MathHelper.ToRadians(Main.rand.NextFloat(-15f, 15f));
                    toTarget = toTarget.RotatedBy(spreadAngle);
                    toTarget.Normalize();
                    velocity = toTarget * 4f;
                    velocity += new Vector2(
                        Main.rand.NextFloat(-0.5f, 0.5f),
                        Main.rand.NextFloat(-0.5f, 0.5f)
                    );
                }
                else
                {
                    velocity = new Vector2(direction * 4f, 0f);
                    velocity.Y = Main.rand.NextFloat(-1f, 1f);
                }

                int proj = Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    spawnPosition,
                    velocity,
                    projectileType,
                    11,
                    20f,
                    Main.myPlayer
                );

                if (proj < Main.maxProjectiles)
                {
                    Main.projectile[proj].CritChance = 4;
                    Main.projectile[proj].ArmorPenetration = 9999;
                    Main.projectile[proj].scale = 0.67f;
                    Main.projectile[proj].ai[0] = 60f;
                    Main.projectile[proj].ai[1] = Main.rand.NextFloat(0.3f, 0.6f);
                }
            }

            for (int i = 0; i < 5; i++)
            {
                Dust.NewDustDirect(
                    basePosition,
                    10, 10,
                    DustID.PinkFairy,
                    direction * 0.5f,
                    Main.rand.NextFloat(-1f, 1f),
                    150,
                    default,
                    1f
                );
            }

            // 连击计数（仅战斗模式）
            if (isBattleMode)
            {
                if (MimiQuestSystem.HasUnlockedLargeClaw)
                {
                    normalAttackCounter++;
                    if (normalAttackCounter >= SuperAttackThreshold && superAttackCooldown <= 0)
                        LaunchSuperAttack();
                }
                else normalAttackCounter = 0;
            }
            else normalAttackCounter = 0;
        }

        private void LaunchSuperAttack()
        {
            bool isBattleMode = Systems.XiLian_Systems.Xingtai.WorldStateSystem.CurrentMimiMode ==
                Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Battle;
            if (!isBattleMode) { normalAttackCounter = 0; return; }
            if (!MimiQuestSystem.HasUnlockedLargeClaw) { normalAttackCounter = 0; return; }

            normalAttackCounter = 0;
            superAttackCooldown = SuperAttackCooldownDuration;

            NPC highestHealthTarget = null;
            int highestHealth = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.life > 0 &&
                    npc.type != NPCID.TargetDummy && npc.CanBeChasedBy())
                {
                    float distance = Vector2.Distance(NPC.Center, npc.Center);
                    if (distance < ScreenDetectionRange && npc.lifeMax > highestHealth)
                    {
                        highestHealth = npc.lifeMax;
                        highestHealthTarget = npc;
                    }
                }
            }

            Vector2 launchDirection;
            Vector2 spawnPosition;
            if (highestHealthTarget != null)
            {
                launchDirection = Vector2.Normalize(highestHealthTarget.Center - NPC.Center);
                spawnPosition = NPC.Center + launchDirection * SuperAttackLaunchDistance;
            }
            else
            {
                int direction = NPC.direction; if (direction == 0) direction = 1;
                launchDirection = new Vector2(direction, 0);
                spawnPosition = NPC.Center + new Vector2(direction * SuperAttackLaunchDistance, 0f);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int proj = Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    spawnPosition,
                    launchDirection * 8f,
                    ModContent.ProjectileType<Projectiles.XiLian_Projectiles.Mimi.Mimi_zhua_big>(),
                    93,
                    30f,
                    Main.myPlayer,
                    0f, 0f
                );
                if (proj < Main.maxProjectiles)
                {
                    Main.projectile[proj].CritChance = 4;
                    Main.projectile[proj].ArmorPenetration = 9999;
                    Main.projectile[proj].scale = 2.0f;
                    Main.projectile[proj].timeLeft = 780;
                }
            }

            for (int i = 0; i < 20; i++)
            {
                Dust.NewDustDirect(spawnPosition, 30, 30, DustID.PurpleTorch,
                    launchDirection.X * 2f, launchDirection.Y * 2f, 150, default, 2.5f);
            }
            SoundEngine.PlaySound(SoundID.Item8, spawnPosition);
            NPC.netUpdate = true;
        }

        private void StopNewAttack()
        {
            isNewAttacking = false;
            newAttackTimer = 0;
            NPC.damage = 0;
            NPC.netUpdate = true;
        }

        private bool ShouldExist()
        {
            var eventSystem = ModContent.GetInstance<Systems.XiLian_Systems.Xingtai.BossEventSystem>();
            bool isInBattle = eventSystem?.IsInBattle() ?? false;
            if (isInBattle) return true;
            return Systems.XiLian_Systems.Xingtai.WorldStateSystem.CurrentWorldForm ==
                Systems.XiLian_Systems.Xingtai.WorldStateSystem.NpcForm.Mimi;
        }

        private void ExecuteBattleModeAI()
        {
            if (buffCooldownTimer > 0) buffCooldownTimer--;
            bool isBattleMode = Systems.XiLian_Systems.Xingtai.WorldStateSystem.CurrentMimiMode ==
                Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Battle;
            bool canUseBuffAbility = isBattleMode && MimiQuestSystem.HasUnlockedBuffAbility;

            if (isAttacking)
            {
                attackAnimationTimer++;
                if (attackAnimationTimer == AttackDuration) ApplyMimiBuffToPlayers();
                if (attackAnimationTimer >= AttackDuration)
                {
                    isAttacking = false;
                    attackAnimationTimer = 0;
                    NPC.netUpdate = true;
                }
                if (attackAnimationTimer > 0 && attackAnimationTimer < AttackDuration)
                    NPC.velocity *= 0.5f;
                currentAnimationState = MimiAnimationState.Attacking;
                ExecuteFollowing(true);
                return;
            }
            else if (canUseBuffAbility && buffCooldownTimer <= 0 && !isNewAttacking)
            {
                StartAttackAnimation();
                return;
            }

            if (!isNewAttacking)
            {
                float speed = Math.Abs(NPC.velocity.X) + Math.Abs(NPC.velocity.Y);
                currentAnimationState = speed > 0.5f ? MimiAnimationState.Moving : MimiAnimationState.Idle;
            }
            ExecuteFollowing(true);
        }

        private void StartAttackAnimation()
        {
            bool isBattleMode = Systems.XiLian_Systems.Xingtai.WorldStateSystem.CurrentMimiMode ==
                Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Battle;
            bool canUseBuffAbility = isBattleMode && MimiQuestSystem.HasUnlockedBuffAbility;
            if (!canUseBuffAbility || isAttacking || isNewAttacking) return;
            if (isNewAttacking) StopNewAttack();
            isAttacking = true;
            attackAnimationTimer = 0;
            buffCooldownTimer = BuffCooldown;
            NPC.damage = 0;
            NPC.netUpdate = true;
            if (Main.netMode != NetmodeID.Server)
            {
                string cheerText = GetLocalizedText("NPCs.Mimi.BattleCheer");
                Main.NewText(cheerText, Color.LightPink);
            }
        }

        private void ExecuteDailyModeAI()
        {
            if (!isIdleMode)
            {
                CheckPlayerIdleState();
                ExecuteFollowing(false);
            }
            else NPC.velocity *= 0.9f;

            if (!isNewAttacking)
            {
                float speed = Math.Abs(NPC.velocity.X) + Math.Abs(NPC.velocity.Y);
                if (currentAnimationState != MimiAnimationState.Sleeping &&
                    currentAnimationState != MimiAnimationState.Confused &&
                    currentAnimationState != MimiAnimationState.Happy &&
                    currentAnimationState != MimiAnimationState.Wink &&
                    currentAnimationState != MimiAnimationState.MimiGag &&
                    currentAnimationState != MimiAnimationState.Attacking)
                {
                    currentAnimationState = speed > 0.5f ? MimiAnimationState.Moving : MimiAnimationState.Idle;
                }
            }
            UpdateAnimationState();
        }

        private void CheckPlayerIdleState()
        {
            if (isIdleMode) return;
            if (FollowingPlayerIndex == -1) return;
            Player targetPlayer = Main.player[FollowingPlayerIndex];
            if (!targetPlayer.active || targetPlayer.dead) return;

            int checkInterval = isCheckingIdle ? IdleCheckFastInterval : IdleCheckInitialInterval;
            idleCheckTimer++;
            if (idleCheckTimer >= checkInterval)
            {
                idleCheckTimer = 0;
                float distanceMoved = Vector2.Distance(lastPlayerPosition, targetPlayer.Center);
                if (currentAnimationState == MimiAnimationState.Sleeping)
                {
                    if (distanceMoved >= MoveThreshold) WakeUpFromSleep();
                }
                else
                {
                    if (distanceMoved < MoveThreshold)
                    {
                        if (!isCheckingIdle)
                        {
                            isCheckingIdle = true;
                            confusionBeforeSleepTimer = ConfusionDuration;
                            currentAnimationState = MimiAnimationState.Confused;
                            animationStateTimer = 0;
                            NPC.netUpdate = true;
                        }
                    }
                    else
                    {
                        if (isCheckingIdle)
                        {
                            isCheckingIdle = false;
                            confusionBeforeSleepTimer = 0;
                            if (currentAnimationState == MimiAnimationState.Sleeping) WakeUpFromSleep();
                        }
                    }
                }
                lastPlayerPosition = targetPlayer.Center;
            }
            if (confusionBeforeSleepTimer > 0)
            {
                confusionBeforeSleepTimer--;
                if (confusionBeforeSleepTimer <= 0 && isCheckingIdle)
                {
                    currentAnimationState = MimiAnimationState.Sleeping;
                    animationStateTimer = 0;
                    NPC.netUpdate = true;
                }
            }
        }

        private void WakeUpFromSleep()
        {
            currentAnimationState = MimiAnimationState.Happy;
            animationStateTimer = 0;
            isCheckingIdle = false;
            confusionBeforeSleepTimer = 0;
            NPC.netUpdate = true;
        }

        // ===== 对话系统 =====
        public override string GetChat()
        {
            MimiQuestSystem.CheckQuestUnlockStatus();
            var config = ModContent.GetInstance<Configs.UISettings>();
            if (!config.UseCustomDialogue) return GetInitialDialogueText();
            string text = GetInitialDialogueText();
            ModContent.GetInstance<MimiDialogueSystem>().ShowDialogue(text, NPC);
            return "";
        }

        private string GetInitialDialogueText()
        {
            MimiLanguageStage languageStage = GetMimiLanguageStage();
            if (!BookDataSystem.HasMetMimi)
            {
                BookDataSystem.OnMimiDialogue();
                MimiQuestSystem.CheckQuestUnlockStatus();
                return GetLocalizedText($"NPCs.Mimi.Dialogue.FirstMeeting.{languageStage}");
            }
            string dialogueKey = $"NPCs.Mimi.Dialogue.Daily.{languageStage}";
            string dialoguesText = GetLocalizedText(dialogueKey);
            if (!string.IsNullOrEmpty(dialoguesText) && dialoguesText != $"[{dialogueKey}]")
            {
                string[] dialogues = dialoguesText.Split('\n');
                if (dialogues.Length > 0) return dialogues[Main.rand.Next(dialogues.Length)];
            }
            return "meme~";
        }

        private string GetRandomDailyChat()
        {
            MimiLanguageStage stage = GetMimiLanguageStage();
            string key = $"NPCs.Mimi.Dialogue.Daily.{stage}";
            string text = GetLocalizedText(key);
            if (!string.IsNullOrEmpty(text) && text != $"[{key}]")
            {
                string[] lines = text.Split('\n');
                if (lines.Length > 0) return lines[Main.rand.Next(lines.Length)];
            }
            return "meme~";
        }

        private string GetRandomExpressionText()
        {
            MimiLanguageStage stage = GetMimiLanguageStage();
            int r = Main.rand.Next(100);
            MimiAnimationState exprState;
            if (r < 20) exprState = MimiAnimationState.Wink;
            else if (r < 35) exprState = MimiAnimationState.Happy;
            else if (r < 45) exprState = MimiAnimationState.Thinking;
            else if (r < 55) exprState = MimiAnimationState.Confused;
            else if (r < 60) exprState = MimiAnimationState.Angry;
            else if (r < 65) exprState = MimiAnimationState.MimiGag;
            else if (r < 70 && !Main.dayTime) exprState = MimiAnimationState.Sleeping;
            else if (r < 80) exprState = MimiAnimationState.Serious;
            else exprState = MimiAnimationState.Happy;

            string key = $"NPCs.Mimi.Expressions.{exprState}.{stage}";
            string text = GetLocalizedText(key);
            if (!string.IsNullOrEmpty(text) && text != $"[{key}]")
            {
                string[] lines = text.Split('\n');
                if (lines.Length > 0) return lines[Main.rand.Next(lines.Length)];
            }
            return "meme~";
        }

        public string GetCurrentModeDialogueText()
        {
            MimiLanguageStage stage = GetMimiLanguageStage();
            switch (currentFunctionMode)
            {
                case MimiFunctionMode.Chat: return _currentChatText;
                case MimiFunctionMode.IdleToggle:
                    string sub = isIdleMode ? "Idle" : "Active";
                    return GetLocalizedText($"NPCs.Mimi.FunctionModes.IdleToggle.{sub}.{stage}");
                case MimiFunctionMode.Transform:
                    string type = Main.hardMode ? "Transform" : "Growth";
                    return GetLocalizedText($"NPCs.Mimi.FunctionModes.Transform.{type}.{stage}");
                case MimiFunctionMode.Inventory:
                    return GetLocalizedText($"NPCs.Mimi.FunctionModes.Inventory.{stage}");
                case MimiFunctionMode.Expressions:
                    return GetRandomExpressionText();
                case MimiFunctionMode.Quest:
                    var currentQuest = MimiQuestSystem.CurrentQuest;
                    var availableQuest = MimiQuestSystem.GetFirstUnlockedIncompleteQuest();
                    if (currentQuest != null && currentQuest.IsActive)
                    {
                        if (MimiQuestSystem.CanCompleteCurrentQuest(Main.LocalPlayer))
                            return GetLocalizedText($"NPCs.Mimi.FunctionModes.Quest.CanSubmit.{stage}");
                        else
                            return GetLocalizedText($"NPCs.Mimi.FunctionModes.Quest.NeedHint.{stage}");
                    }
                    else if (availableQuest != null)
                        return GetLocalizedText($"NPCs.Mimi.FunctionModes.Quest.HasSomethingToSay.{stage}");
                    else
                        return GetLocalizedText($"NPCs.Mimi.FunctionModes.Quest.Wish.{stage}");
            }
            return "meme~";
        }

        public void HandleSecondButton()
        {
            switch (currentFunctionMode)
            {
                case MimiFunctionMode.Chat: _currentChatText = GetRandomDailyChat(); break;
                case MimiFunctionMode.IdleToggle: ToggleIdleMode(); break;
                case MimiFunctionMode.Transform:
                    if (Main.hardMode) TransformMimiToXilian();
                    else Main.npcChatText = GetGrowthChat();
                    break;
                case MimiFunctionMode.Inventory: OpenVoidBag(); break;
                case MimiFunctionMode.Expressions: TriggerRandomExpression(); break;
                case MimiFunctionMode.Quest: HandleQuestAction(); break;
            }
        }

        private string GetGrowthChat()
        {
            MimiLanguageStage languageStage = GetMimiLanguageStage();
            string growthKey = $"NPCs.Mimi.Dialogue.Growth.{languageStage}";
            string growthDialoguesText = GetLocalizedText(growthKey);
            if (!string.IsNullOrEmpty(growthDialoguesText) && growthDialoguesText != $"[{growthKey}]")
            {
                string[] growthDialogues = growthDialoguesText.Split('\n');
                if (growthDialogues.Length > 0)
                    return growthDialogues[Main.rand.Next(growthDialogues.Length)];
            }
            return "meme~";
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = GetLocalizedText("NPCs.Mimi.Buttons.ChangeTopic");
            switch (currentFunctionMode)
            {
                case MimiFunctionMode.Chat: button2 = GetLocalizedText("NPCs.Mimi.Buttons.Chat"); break;
                case MimiFunctionMode.IdleToggle:
                    button2 = isIdleMode ? GetLocalizedText("NPCs.Mimi.Buttons.AdventureTogether") : GetLocalizedText("NPCs.Mimi.Buttons.Rest");
                    break;
                case MimiFunctionMode.Transform:
                    button2 = Main.hardMode ? GetLocalizedText("NPCs.Mimi.Buttons.Transform") : GetLocalizedText("NPCs.Mimi.Buttons.Growth");
                    break;
                case MimiFunctionMode.Inventory: button2 = GetLocalizedText("NPCs.Mimi.Buttons.OpenBackpack"); break;
                case MimiFunctionMode.Expressions: button2 = GetLocalizedText("NPCs.Mimi.Buttons.MakeExpression"); break;
                case MimiFunctionMode.Quest:
                    var currentQuest = MimiQuestSystem.CurrentQuest;
                    var availableQuest = MimiQuestSystem.GetFirstUnlockedIncompleteQuest();
                    if (currentQuest != null && currentQuest.IsActive)
                    {
                        if (MimiQuestSystem.CanCompleteCurrentQuest(Main.LocalPlayer))
                            button2 = GetLocalizedText("NPCs.Mimi.Buttons.Submit");
                        else
                            button2 = GetLocalizedText("NPCs.Mimi.Buttons.Hint");
                    }
                    else if (availableQuest != null) button2 = GetLocalizedText("NPCs.Mimi.Buttons.HasSomethingToSay");
                    else button2 = GetLocalizedText("NPCs.Mimi.Buttons.Wish");
                    break;
            }
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                SwitchFunctionMode();
                ShowCurrentModeDialogue();
            }
            else
            {
                switch (currentFunctionMode)
                {
                    case MimiFunctionMode.Chat: Main.npcChatText = GetChat(); break;
                    case MimiFunctionMode.IdleToggle: ToggleIdleMode(); break;
                    case MimiFunctionMode.Transform:
                        if (Main.hardMode) TransformMimiToXilian();
                        else Main.npcChatText = GetGrowthChat();
                        break;
                    case MimiFunctionMode.Inventory: OpenVoidBag(); break;
                    case MimiFunctionMode.Expressions: TriggerRandomExpression(); break;
                    case MimiFunctionMode.Quest: HandleQuestAction(); break;
                }
            }
        }

        private void ShowCurrentModeDialogue()
        {
            MimiLanguageStage languageStage = GetMimiLanguageStage();
            switch (currentFunctionMode)
            {
                case MimiFunctionMode.Chat:
                    Main.npcChatText = GetLocalizedText($"NPCs.Mimi.FunctionModes.Chat.{languageStage}");
                    break;
                case MimiFunctionMode.IdleToggle:
                    Main.npcChatText = isIdleMode ?
                        GetLocalizedText($"NPCs.Mimi.FunctionModes.IdleToggle.Idle.{languageStage}") :
                        GetLocalizedText($"NPCs.Mimi.FunctionModes.IdleToggle.Active.{languageStage}");
                    break;
                case MimiFunctionMode.Transform:
                    if (Main.hardMode)
                        Main.npcChatText = GetLocalizedText($"NPCs.Mimi.FunctionModes.Transform.Transform.{languageStage}");
                    else
                        Main.npcChatText = GetLocalizedText($"NPCs.Mimi.FunctionModes.Transform.Growth.{languageStage}");
                    break;
                case MimiFunctionMode.Inventory:
                    Main.npcChatText = GetLocalizedText($"NPCs.Mimi.FunctionModes.Inventory.{languageStage}");
                    break;
                case MimiFunctionMode.Expressions:
                    Main.npcChatText = GetLocalizedText($"NPCs.Mimi.FunctionModes.Expressions.{languageStage}");
                    break;
                case MimiFunctionMode.Quest:
                    var currentQuest = MimiQuestSystem.CurrentQuest;
                    var availableQuest = MimiQuestSystem.GetFirstUnlockedIncompleteQuest();
                    if (currentQuest != null && currentQuest.IsActive)
                    {
                        if (MimiQuestSystem.CanCompleteCurrentQuest(Main.LocalPlayer))
                            Main.npcChatText = GetLocalizedText($"NPCs.Mimi.FunctionModes.Quest.CanSubmit.{languageStage}");
                        else
                            Main.npcChatText = GetLocalizedText($"NPCs.Mimi.FunctionModes.Quest.NeedHint.{languageStage}");
                    }
                    else if (availableQuest != null)
                        Main.npcChatText = GetLocalizedText($"NPCs.Mimi.FunctionModes.Quest.HasSomethingToSay.{languageStage}");
                    else
                        Main.npcChatText = GetLocalizedText($"NPCs.Mimi.FunctionModes.Quest.Wish.{languageStage}");
                    break;
            }
        }

        public string GetCurrentFunctionButtonText()
        {
            string key = "";
            switch (currentFunctionMode)
            {
                case MimiFunctionMode.Chat: key = "NPCs.Mimi.Buttons.Chat"; break;
                case MimiFunctionMode.IdleToggle:
                    key = isIdleMode ? "NPCs.Mimi.Buttons.AdventureTogether" : "NPCs.Mimi.Buttons.Rest";
                    break;
                case MimiFunctionMode.Transform:
                    key = Main.hardMode ? "NPCs.Mimi.Buttons.Transform" : "NPCs.Mimi.Buttons.Growth";
                    break;
                case MimiFunctionMode.Inventory: key = "NPCs.Mimi.Buttons.OpenBackpack"; break;
                case MimiFunctionMode.Expressions: key = "NPCs.Mimi.Buttons.MakeExpression"; break;
                case MimiFunctionMode.Quest:
                    var currentQuest = MimiQuestSystem.CurrentQuest;
                    var availableQuest = MimiQuestSystem.GetFirstUnlockedIncompleteQuest();
                    if (currentQuest != null && currentQuest.IsActive)
                        key = MimiQuestSystem.CanCompleteCurrentQuest(Main.LocalPlayer) ? "NPCs.Mimi.Buttons.Submit" : "NPCs.Mimi.Buttons.Hint";
                    else if (availableQuest != null) key = "NPCs.Mimi.Buttons.HasSomethingToSay";
                    else key = "NPCs.Mimi.Buttons.Wish";
                    break;
            }
            return GetLocalizedText(key);
        }

        public void SwitchFunctionMode()
        {
            currentFunctionModeIndex++;
            if (currentFunctionModeIndex >= functionModeOrder.Length) currentFunctionModeIndex = 0;
            currentFunctionMode = functionModeOrder[currentFunctionModeIndex];
            NPC.netUpdate = true;
            SoundEngine.PlaySound(SoundID.MenuTick);
            string modeName = GetFunctionModeName(currentFunctionMode);
            string switchText = GetLocalizedText("NPCs.Mimi.System.SwitchMode", modeName);
            Main.npcChatText = switchText;
        }

        private void HandleQuestAction()
        {
            MimiLanguageStage languageStage = GetMimiLanguageStage();
            var currentQuest = MimiQuestSystem.CurrentQuest;
            var availableQuest = MimiQuestSystem.GetFirstUnlockedIncompleteQuest();

            if (currentQuest != null && currentQuest.IsActive)
            {
                if (MimiQuestSystem.CanCompleteCurrentQuest(Main.LocalPlayer))
                {
                    MimiQuestSystem.CompleteCurrentQuest(Main.LocalPlayer);
                    Main.npcChatText = GetLocalizedText($"NPCs.Mimi.FunctionModes.Quest.Complete.{languageStage}");
                    currentQuest = null;
                    availableQuest = MimiQuestSystem.GetFirstUnlockedIncompleteQuest();
                }
                else
                {
                    string progressText = GetLocalizedText($"NPCs.Mimi.FunctionModes.Quest.Progress.{languageStage}");
                    foreach (var itemReq in currentQuest.RequiredItems)
                    {
                        int playerCount = MimiQuestSystem.CountItemsInInventory(Main.LocalPlayer, itemReq.Key);
                        string itemName = Lang.GetItemNameValue(itemReq.Key);
                        progressText += $"{itemName}: {playerCount}/{itemReq.Value}\n";
                    }
                    Main.npcChatText = progressText;
                }
            }
            else if (availableQuest != null)
            {
                MimiQuestSystem.AcceptQuest(availableQuest.QuestType);
                Main.npcChatText = $"{availableQuest.Description}";
            }
            else
            {
                Main.npcChatText = GetLocalizedText($"NPCs.Mimi.FunctionModes.Quest.Wish.{languageStage}");
            }
        }

        private void ToggleIdleMode()
        {
            MimiLanguageStage languageStage = GetMimiLanguageStage();
            isIdleMode = !isIdleMode;
            idleModeCooldown = MouseClickCooldown;
            if (!isIdleMode) noPlayerTimer = 0;
            NPC.netUpdate = true;
            SoundEngine.PlaySound(SoundID.MenuTick);
            string toggleText = isIdleMode ?
                GetLocalizedText($"NPCs.Mimi.System.ToggleIdle.Idle.{languageStage}") :
                GetLocalizedText($"NPCs.Mimi.System.ToggleIdle.Active.{languageStage}");
            Main.npcChatText = toggleText;
        }

        private void OpenVoidBag()
        {
            Player player = Main.player[Main.myPlayer];
            player.chest = -3;
            Main.npcChatText = "";
            Main.playerInventory = true;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.ChestUpdates, -1, -1, null, Main.myPlayer, -3);
        }

        private void TriggerRandomExpression()
        {
            if (currentAnimationState == MimiAnimationState.Sleeping ||
                currentAnimationState == MimiAnimationState.Attacking ||
                currentAnimationState == MimiAnimationState.NewAttacking) return;

            MimiLanguageStage languageStage = GetMimiLanguageStage();
            int randomChoice = Main.rand.Next(100);
            if (randomChoice < 20) currentAnimationState = MimiAnimationState.Wink;
            else if (randomChoice < 35) currentAnimationState = MimiAnimationState.Happy;
            else if (randomChoice < 45) currentAnimationState = MimiAnimationState.Thinking;
            else if (randomChoice < 55) currentAnimationState = MimiAnimationState.Confused;
            else if (randomChoice < 60) currentAnimationState = MimiAnimationState.Angry;
            else if (randomChoice < 65) currentAnimationState = MimiAnimationState.MimiGag;
            else if (randomChoice < 70 && !Main.dayTime) currentAnimationState = MimiAnimationState.Sleeping;
            else if (randomChoice < 80) currentAnimationState = MimiAnimationState.Serious;

            animationStateTimer = 0;
            string expressionKey = $"NPCs.Mimi.Expressions.{currentAnimationState}.{languageStage}";
            string expressionText = GetLocalizedText(expressionKey);
            if (!string.IsNullOrEmpty(expressionText) && expressionText != $"[{expressionKey}]")
            {
                string[] dialogueOptions = expressionText.Split('\n');
                if (dialogueOptions.Length > 0) Main.npcChatText = dialogueOptions[Main.rand.Next(dialogueOptions.Length)];
                else Main.npcChatText = "meme~";
            }
            else
            {
                string defaultKey = $"NPCs.Mimi.Expressions.Default.{languageStage}";
                string defaultText = GetLocalizedText(defaultKey);
                if (!string.IsNullOrEmpty(defaultText) && defaultText != $"[{defaultKey}]")
                {
                    string[] dialogueOptions = defaultText.Split('\n');
                    if (dialogueOptions.Length > 0) Main.npcChatText = dialogueOptions[Main.rand.Next(dialogueOptions.Length)];
                    else Main.npcChatText = "meme~";
                }
                else Main.npcChatText = "meme~";
            }
            NPC.netUpdate = true;
        }

        private void TransformMimiToXilian()
        {
            Vector2 position = NPC.Center;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (i != NPC.whoAmI && Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Mimi>())
                {
                    if (Main.netMode == NetmodeID.MultiplayerClient || Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                    Main.npc[i].active = false;
                }
            }
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Xilian>())
                {
                    if (Main.netMode == NetmodeID.MultiplayerClient || Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                    Main.npc[i].active = false;
                }
            }
            NPC.active = false;
            int xilianIndex = NPC.NewNPC(NPC.GetSource_NaturalSpawn(),
                (int)position.X, (int)position.Y, ModContent.NPCType<Xilian>());
            NPC xilian = Main.npc[xilianIndex];
            xilian.homeless = true;
            Systems.XiLian_Systems.Xingtai.WorldStateSystem.UpdateFormState(
                Systems.XiLian_Systems.Xingtai.WorldStateSystem.NpcForm.Xilian,
                position);
            for (int i = 0; i < 15; i++)
                Dust.NewDustDirect(position, 20, 20, DustID.PurpleTorch, 0f, 0f, 150, default, 1.5f);
            SoundEngine.PlaySound(SoundID.Item8, position);
            string transformedText = GetLocalizedText("NPCs.Mimi.System.Transformed");
            Main.npcChatText = transformedText;
        }

        // ===== 跟随与移动 =====
        private void ExecuteFollowing(bool isBattleMode)
        {
            if (isIdleMode && !isBattleMode) { NPC.velocity *= 0.9f; return; }
            if (FollowingPlayerIndex == -1 && !isIdleMode) FindNearestActivePlayer();
            if (FollowingPlayerIndex != -1)
            {
                Player targetPlayer = Main.player[FollowingPlayerIndex];
                if (targetPlayer.active && !targetPlayer.dead)
                    FollowPlayer(targetPlayer, isBattleMode);
            }
            else NPC.velocity *= 0.9f;
        }

        private void FindNearestActivePlayer()
        {
            if (isIdleMode) return;
            int closestPlayer = -1;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                {
                    float distance = Vector2.Distance(NPC.Center, player.Center);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPlayer = i;
                    }
                }
            }
            FollowingPlayerIndex = closestPlayer;
            if (FollowingPlayerIndex != -1) noPlayerTimer = 0;
        }

        private void FollowPlayer(Player targetPlayer, bool isBattleMode)
        {
            var config = ModContent.GetInstance<Mimi_Config>();

            // 检测敌人中点（用于防御站位）
            Vector2? midPoint = GetEnemyMidPoint(targetPlayer, EnemyMidPointDetectionRange, isBattleMode);
            Vector2 targetPos;

            if (midPoint.HasValue)
            {
                targetPos = midPoint.Value;
            }
            else
            {
                if (config == null)
                {
                    targetPos = targetPlayer.Center +
                        new Vector2((targetPlayer.direction > 0 ? -1 : 1) * -50f, -30f);
                }
                else
                {
                    switch (config.MimiFollowDistanceMode)
                    {
                        case Mimi_Config.FollowDistanceMode.Behind:
                            int behindDistance = config.BehindFollowDistance;
                            targetPos = targetPlayer.Center +
                                new Vector2((targetPlayer.direction > 0 ? -1 : 1) * behindDistance, config.FollowOffsetY);
                            break;
                        default:
                            targetPos = targetPlayer.Center +
                                new Vector2((targetPlayer.direction > 0 ? -1 : 1) * config.OriginalFollowOffsetX, config.FollowOffsetY);
                            break;
                    }
                }
            }

            float maxFollowDist = isBattleMode ? MaxFollowDistance : DailyFollowDistance;
            float teleportDist = isBattleMode ? TeleportDistance : DailyTeleportDistance;
            float maxSpeed = isBattleMode ? MaxSpeed : DailyMaxSpeed;

            Vector2 direction = targetPos - NPC.Center;
            float distance = direction.Length();
            if (distance > teleportDist) { TeleportToPlayer(targetPlayer); return; }

            if (distance > 2f)
            {
                if (distance > 0.01f) direction.Normalize();
                else direction = Vector2.Zero;
                float speedRatio = MathHelper.Clamp(distance / maxFollowDist, 0.1f, 1f);
                Vector2 desiredVelocity = direction * maxSpeed * speedRatio;
                Vector2 smoothVelocity = NPC.velocity;
                if (smoothVelocity.Length() < desiredVelocity.Length())
                    smoothVelocity = Vector2.Lerp(smoothVelocity, desiredVelocity, FollowAcceleration);
                else
                    smoothVelocity = Vector2.Lerp(smoothVelocity, desiredVelocity, FollowDeceleration);
                if (smoothVelocity.Length() > maxSpeed)
                {
                    smoothVelocity.Normalize();
                    smoothVelocity *= maxSpeed;
                }
                NPC.velocity = smoothVelocity;
            }
            else NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, FollowDeceleration);

            if (!isNewAttacking && Math.Abs(targetPlayer.Center.X - NPC.Center.X) > 10f)
            {
                NPC.direction = NPC.Center.X < targetPlayer.Center.X ? 1 : -1;
                NPC.spriteDirection = NPC.direction;
            }
            Lighting.AddLight(NPC.Center, 0.3f, 0.5f, 0.7f);
        }

        private Vector2? GetEnemyMidPoint(Player player, float range, bool isBattleMode)
        {
            NPC nearestEnemy = null;
            float minDist = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.life > 0 &&
                    npc.type != NPCID.TargetDummy && npc.CanBeChasedBy())
                {
                    float distToPlayer = Vector2.Distance(player.Center, npc.Center);
                    if (distToPlayer < range && distToPlayer < minDist)
                    {
                        // 日常模式检查墙壁：以迷迷自身为起点
                        if (!isBattleMode && HasWallBetween(NPC.Center, npc.Center))
                            continue;
                        minDist = distToPlayer;
                        nearestEnemy = npc;
                    }
                }
            }
            if (nearestEnemy != null)
                return (player.Center + nearestEnemy.Center) / 2f;
            return null;
        }

        private void ApplyMimiBuffToPlayers()
        {
            foreach (Player player in Main.player)
            {
                if (player.active && !player.dead &&
                    Vector2.Distance(NPC.Center, player.Center) < 800f)
                {
                    player.AddBuff(ModContent.BuffType<Buffs.XiLian_Buffs.MimiBuff>(), 60 * 60 * 5);
                    for (int i = 0; i < 10; i++)
                    {
                        Dust dust = Dust.NewDustDirect(player.Center, 0, 0,
                            DustID.HeartCrystal, 0f, 0f, 100, new Color(255, 100, 255), 1.5f);
                        dust.velocity *= 1.2f;
                        dust.noGravity = true;
                    }
                }
            }
        }

        private void UpdateAnimationState()
        {
            if (animationStateTimer > ExpressionDuration &&
                (currentAnimationState == MimiAnimationState.Wink ||
                 currentAnimationState == MimiAnimationState.Happy ||
                 currentAnimationState == MimiAnimationState.Angry ||
                 currentAnimationState == MimiAnimationState.Confused ||
                 currentAnimationState == MimiAnimationState.Thinking ||
                 currentAnimationState == MimiAnimationState.MimiGag))
            {
                currentAnimationState = MimiAnimationState.Idle;
                animationStateTimer = 0;
                NPC.netUpdate = true;
            }
            if (currentAnimationState == MimiAnimationState.MimiGag && animationStateTimer > MimiGagDuration)
            {
                currentAnimationState = MimiAnimationState.Idle;
                animationStateTimer = 0;
                NPC.netUpdate = true;
            }
            if (currentAnimationState == MimiAnimationState.Happy && animationStateTimer > HappyDuration)
            {
                currentAnimationState = MimiAnimationState.Idle;
                animationStateTimer = 0;
                NPC.netUpdate = true;
            }
            if (currentAnimationState == MimiAnimationState.Confused &&
                animationStateTimer > ConfusionDuration && confusionBeforeSleepTimer <= 0)
            {
                currentAnimationState = MimiAnimationState.Idle;
                animationStateTimer = 0;
                NPC.netUpdate = true;
            }
        }

        private void TeleportToPlayer(Player player)
        {
            for (int i = 0; i < 20; i++)
            {
                Vector2 teleportPos = player.Center +
                    new Vector2(Main.rand.Next(-200, 201), Main.rand.Next(-150, -50));
                if (!Collision.SolidCollision(teleportPos, NPC.width, NPC.height))
                {
                    NPC.position = teleportPos;
                    NPC.velocity = Vector2.Zero;
                    for (int j = 0; j < 20; j++)
                    {
                        Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
                            DustID.MagicMirror, 0f, 0f, 100, default, 1.5f);
                        dust.velocity *= 3f;
                        dust.noGravity = true;
                    }
                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                    break;
                }
            }
        }

        // ===== 帧动画 =====
        public override void FindFrame(int frameHeight)
        {
            int frameIndex = 0;
            if (currentAnimationState == MimiAnimationState.Attacking && isAttacking)
            {
                int attackFrame = (attackAnimationTimer / 60) % 4;
                frameIndex = 19 + attackFrame;
            }
            else if (currentAnimationState == MimiAnimationState.NewAttacking && isNewAttacking)
            {
                if (newAttackTimer < NewAttackWindupFrames)
                    frameIndex = 15 + (newAttackTimer / 15) % 2;
                else
                {
                    int loopTimer = newAttackTimer - NewAttackWindupFrames;
                    frameIndex = 17 + ((loopTimer / AttackCycleFrameInterval) % 2);
                }
            }
            else
            {
                switch (currentAnimationState)
                {
                    case MimiAnimationState.Idle:
                        frameIndex = (int)(Main.GameUpdateCount / 15) % 2;
                        break;
                    case MimiAnimationState.Moving:
                        frameIndex = 2 + (int)((Main.GameUpdateCount / 10) % 2);
                        break;
                    case MimiAnimationState.Wink:
                        int winkFrame = (animationStateTimer / 60) % 4;
                        frameIndex = 4 + winkFrame;
                        break;
                    case MimiAnimationState.Happy: frameIndex = 8; break;
                    case MimiAnimationState.Angry: frameIndex = 9; break;
                    case MimiAnimationState.Confused: frameIndex = 10; break;
                    case MimiAnimationState.Sleeping: frameIndex = 11; break;
                    case MimiAnimationState.Serious: frameIndex = 12; break;
                    case MimiAnimationState.Thinking: frameIndex = 13; break;
                    case MimiAnimationState.MimiGag: frameIndex = 14; break;
                    default: frameIndex = 0; break;
                }
            }
            NPC.frame.Y = frameIndex * frameHeight;
            NPC.spriteDirection = NPC.direction;
        }
    }
}