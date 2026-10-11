using Amphoreus.Players.Shenyupai_Players;
using Amphoreus.Systems.XiLian_Systems.Touxiang;
using Amphoreus.Systems.XiLian_Systems.Xingtai;
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
            Transform
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

        // === 对话模式相关 ===
        public FunctionMode CurrentFunctionMode => _currentFunctionMode;
        private FunctionMode _currentFunctionMode = FunctionMode.Chat;
        private FunctionMode[] _functionModeOrder = new FunctionMode[]
        {
            FunctionMode.Chat,
            FunctionMode.OracleCard,
            FunctionMode.Shop,
            FunctionMode.Transform
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
            NPC.immortal = true;
            NPC.dontTakeDamage = true;
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

            if (isAttacking)
            {
                HandleAttackState();
                return;
            }

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

            if (attackTimer == ProjectileSpawnTime)
                SpawnC8XProjectile(target);

            if (attackTimer >= TotalAttackTime)
            {
                if (IsTargetValid())
                {
                    CancelAttack();
                    attackCooldownTimer = AttackCooldown;
                }
                else
                {
                    CancelAttack();
                }
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

        private void CancelAttack()
        {
            isAttacking = false;
            attackTimer = 0;
            attackTarget = -1;
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
            if (player.ZoneGraveyard) return "Graveyard";
            if (Main.raining) return "Rain";
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
                default:
                    return "meme~";
            }
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
            Main.npcChatText = newText;
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
                        ModContent.GetInstance<XilianDialogueSystem>().HideUI();
                        TransformXilianToMimi();
                    }
                    else
                    {
                        _currentChatText = GetLocalizedText("NPCs.Xilian.FunctionModes.Transform.Disabled");
                    }
                    break;
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
                    ModContent.GetInstance<XilianDialogueSystem>().HideUI();
                    shopName = "Shop";
                    return;
                }

                // 处理第二个按钮（聊聊天/神谕牌/变身）
                HandleSecondButton();

                // 非商店模式，更新对话文本
                if (_currentFunctionMode != FunctionMode.Shop)
                {
                    Main.npcChatText = GetCurrentModeDialogueText();
                }
            }
        }

        // ========== 自定义UI控制 ==========
        private Configs.UISettings Config => ModContent.GetInstance<Configs.UISettings>();

        public override bool CanChat()
        {
            if (Config.UseCustomDialogue && ModContent.GetInstance<XilianDialogueSystem>().IsActive)
                return false;
            return base.CanChat();
        }

        // === 主对话方法（右键点击时调用）===
        public override string GetChat()
        {
            string chatText = "";

            if (_currentFunctionMode == FunctionMode.Chat)
            {
                // 日常触发：75%环境对话，25%日常问候（不含NPC评价）
                if (Main.rand.NextFloat() < 0.75f)
                {
                    string conditionKey = GetConditionalChatKey();
                    if (!string.IsNullOrEmpty(conditionKey))
                    {
                        string conditionText = GetLocalizedText($"NPCs.Xilian.EnvironmentChat.{conditionKey}");
                        if (conditionText != $"[NPCs.Xilian.ConditionalChat.{conditionKey}]")
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
                ModContent.GetInstance<XilianDialogueSystem>().ShowDialogue(chatText, NPC);
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
                    Main.npcChatText = chatText;
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