using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Chat;
using Terraria.Localization;

namespace Amphoreus.Systems.XiLian_Systems.Xingtai
{
    public class BossEventSystem : ModSystem
    {
        // === 状态 ===
        private bool isInBattle = false;
        private int checkTimer = 0;
        private int delayTimer = 0;

        // === 新增：提示信息记录 ===
        private bool hasShownIdleMessage = false;
        private int lastBossType = -1;

        // === 计时器常量 ===
        private const int CheckInterval = 60 * 3;  // 3秒检测一次
        private const int DelayTime = 60 * 10;     // 10秒延迟

        // === 网络同步 ===
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(isInBattle);
            writer.Write(checkTimer);
            writer.Write(delayTimer);
            writer.Write(hasShownIdleMessage);
            writer.Write(lastBossType);
        }

        public override void NetReceive(BinaryReader reader)
        {
            isInBattle = reader.ReadBoolean();
            checkTimer = reader.ReadInt32();
            delayTimer = reader.ReadInt32();
            hasShownIdleMessage = reader.ReadBoolean();
            lastBossType = reader.ReadInt32();
        }

        // === 每帧更新 ===
        public override void PostUpdateNPCs()
        {
            // === 新增：检查是否有迷迷处于闲置模式 ===
            bool anyMimiInIdleMode = CheckIfAnyMimiInIdleMode();
            if (anyMimiInIdleMode)
            {
                // 如果有迷迷处于闲置模式，不执行战斗检测逻辑
                return;
            }

            // 3秒检测计时
            checkTimer++;
            if (checkTimer >= CheckInterval)
            {
                checkTimer = 0;
                CheckBossStatus();
            }

            // 如果在延迟状态，处理延迟计时
            if (delayTimer > 0)
            {
                delayTimer--;
                if (delayTimer <= 0)
                {
                    // 延迟结束，BOSS战结束，切换回日常模式
                    EndBattle();
                    isInBattle = false;
                }
            }
        }

        // === 核心检测逻辑 ===
        private void CheckBossStatus()
        {
            // === 新增：再次检查是否有迷迷处于闲置模式 ===
            bool anyMimiInIdleMode = CheckIfAnyMimiInIdleMode();
            if (anyMimiInIdleMode)
            {
                // 如果有迷迷处于闲置模式，不执行战斗检测逻辑
                // 但需要确保战斗状态被正确重置
                if (isInBattle)
                {
                    isInBattle = false;
                    delayTimer = 0;

                    // 设置日常模式
                    Systems.XiLian_Systems.Xingtai.WorldStateSystem.SetMimiMode(
                        Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Daily);
                }
                return;
            }

            // 检查当前是否有迷迷存在
            bool hasMimi = CheckIfMimiExists();
            bool hasXilian = CheckIfXilianExists();
            bool hasMimiOrXilian = hasMimi || hasXilian;

            // 检测是否有BOSS
            bool hasBoss = false;
            int currentBossType = -1;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.boss && !npc.friendly)
                {
                    hasBoss = true;
                    currentBossType = npc.type;
                    break;
                }
            }

            // === 修复：延迟期间BOSS出现立即取消延迟 ===
            if (delayTimer > 0 && hasBoss)
            {
                CancelDelay();
                return; // 直接返回，等待下一次检测处理战斗逻辑
            }

            // === 核心逻辑：有BOSS就必须有迷迷 ===
            if (hasBoss && !hasMimiOrXilian)
            {
                // 有BOSS但世界上没有迷迷或昔涟 → 立即生成一个迷迷并进入战斗
                SpawnMimiForBattle();
                StartBattle();
            }
            else if (hasBoss && hasMimiOrXilian)
            {
                // 有BOSS也有迷迷或昔涟
                bool anyMimiInIdle = CheckIfAnyMimiInIdleMode();

                if (!isInBattle && !anyMimiInIdle)
                {
                    // 没有闲置的迷迷 → 进入战斗
                    StartBattle();
                }
                else if (!isInBattle && anyMimiInIdle)
                {
                    // 有闲置的迷迷 → 显示提示（只显示一次）
                    if (currentBossType != lastBossType || !hasShownIdleMessage)
                    {
                        ShowMessage(GetLocalizedText("MimiResting"), Color.LightGoldenrodYellow);
                        hasShownIdleMessage = true;
                        lastBossType = currentBossType;
                    }
                }
            }
            else if (!hasBoss && isInBattle && delayTimer == 0)
            {
                // 没有BOSS但在战斗中 → 开始10秒延迟
                StartDelay();

                // 重置提示信息记录
                hasShownIdleMessage = false;
                lastBossType = -1;
            }
        }

        // === 检查迷迷是否存在 ===
        private bool CheckIfMimiExists()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == ModContent.NPCType<NPCs.XiLian.Mimi>())
                {
                    return true;
                }
            }
            return false;
        }

        // === 检查昔涟是否存在 ===
        private bool CheckIfXilianExists()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == ModContent.NPCType<NPCs.XiLian.Xilian>())
                {
                    return true;
                }
            }
            return false;
        }

        // === 检查是否有迷迷处于闲置模式 ===
        private bool CheckIfAnyMimiInIdleMode()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == ModContent.NPCType<NPCs.XiLian.Mimi>())
                {
                    // 检查是否处于闲置模式
                    if (npc.ModNPC is NPCs.XiLian.Mimi mimi)
                    {
                        if (mimi.IsIdleMode)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // === 事件处理器 ===
        public void OnBossSpawned(NPC bossNPC)
        {
            // === 新增：检查是否有迷迷处于闲置模式 ===
            bool anyMimiInIdleMode = CheckIfAnyMimiInIdleMode();
            if (anyMimiInIdleMode)
            {
                // 有闲置的迷迷 → 显示提示（只显示一次）
                if (bossNPC.type != lastBossType || !hasShownIdleMessage)
                {
                    ShowMessage(GetLocalizedText("MimiResting"), Color.LightGoldenrodYellow);
                    hasShownIdleMessage = true;
                    lastBossType = bossNPC.type;
                }
                return;
            }

            // 检查当前是否有迷迷存在
            bool hasMimi = CheckIfMimiExists();
            bool hasXilian = CheckIfXilianExists();
            bool hasMimiOrXilian = hasMimi || hasXilian;

            // === 修复：延迟期间BOSS出现立即取消延迟 ===
            if (delayTimer > 0)
            {
                CancelDelay();
                return;
            }

            if (!hasMimiOrXilian)
            {
                // 没有迷迷或昔涟 → 立即生成一个迷迷并进入战斗
                SpawnMimiForBattle();
                StartBattle();
            }
            else
            {
                if (!isInBattle)
                {
                    StartBattle();
                }
            }
        }

        // === 生成迷迷进行战斗 ===
        private void SpawnMimiForBattle()
        {
            Player player = FindNearestPlayer();
            if (player == null) return;

            Vector2 spawnPos = player.Center +
                new Vector2((player.direction > 0 ? -1 : 1) * 100, -50);

            // 确保只有一个迷迷
            RemoveAllMimiAndXilian();

            int mimiIndex = NPC.NewNPC(NPC.GetSource_NaturalSpawn(),
                (int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType<NPCs.XiLian.Mimi>());

            if (mimiIndex < Main.maxNPCs)
            {
                // 设置战斗模式
                if (Main.npc[mimiIndex].ModNPC is NPCs.XiLian.Mimi mimi)
                {
                    mimi.FollowingPlayerIndex = player.whoAmI;

                    // 更新世界状态
                    WorldStateSystem.UpdateFormState(
                        WorldStateSystem.NpcForm.Mimi,
                        spawnPos);
                }

                SpawnTransformEffect(spawnPos);

                // 显示信息
                ShowMessage(GetLocalizedText("MimiComing"), Color.LightPink);
            }
        }

        // === 战斗状态方法 ===
        private void StartBattle()
        {
            isInBattle = true;
            delayTimer = 0;

            // 设置战斗模式
            Systems.XiLian_Systems.Xingtai.WorldStateSystem.SetMimiMode(
                Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Battle);

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }

            ShowMessage(GetLocalizedText("MimiComing"), Color.LightPink);
        }

        private void StartDelay()
        {
            delayTimer = DelayTime;

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }

            ShowMessage(GetLocalizedText("BattleEndDelay"), Color.LightGoldenrodYellow);
        }

        private void CancelDelay()
        {
            delayTimer = 0;

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }

            ShowMessage(GetLocalizedText("BattleResume"), Color.Orange);
        }

        private void EndBattle()
        {
            // 设置日常模式
            Systems.XiLian_Systems.Xingtai.WorldStateSystem.SetMimiMode(
                Systems.XiLian_Systems.Xingtai.WorldStateSystem.MimiMode.Daily);

            ShowMessage(GetLocalizedText("BattleEnd"), Color.LightGreen);

            // 重置提示信息记录
            hasShownIdleMessage = false;
            lastBossType = -1;
        }

        // === 辅助方法 ===
        private void RemoveAllMimiAndXilian()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active)
                {
                    if (Main.npc[i].type == ModContent.NPCType<NPCs.XiLian.Mimi>() ||
                        Main.npc[i].type == ModContent.NPCType<NPCs.XiLian.Xilian>())
                    {
                        if (Main.netMode == NetmodeID.MultiplayerClient || Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                        }
                        Main.npc[i].active = false;
                    }
                }
            }
        }

        private Player FindNearestPlayer()
        {
            foreach (Player player in Main.player)
            {
                if (player.active && !player.dead)
                {
                    return player;
                }
            }
            return null;
        }

        private void SpawnTransformEffect(Vector2 position)
        {
            for (int i = 0; i < 15; i++)
            {
                Dust.NewDustDirect(position, 20, 20,
                    DustID.PurpleTorch, 0f, 0f, 150, default, 1.5f);
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, position);
        }

        private void ShowMessage(string text, Color color)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(text, color);
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
            }
        }

        // 添加获取本地化文本的方法
        private string GetLocalizedText(string key)
        {
            return Mod.GetLocalization($"Systems.XiLian_Systems.Xingtai.BossEventSystem.{key}").Value;
        }

        // === 查询方法 ===
        public bool IsInBattle()
        {
            return isInBattle || delayTimer > 0;
        }
    }
}