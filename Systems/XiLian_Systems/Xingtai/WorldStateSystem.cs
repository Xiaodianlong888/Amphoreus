using Amphoreus.Systems.Ruwosuoshu;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;

namespace Amphoreus.Systems.XiLian_Systems.Xingtai
{
    public class WorldStateSystem : ModSystem
    {
        // === NPC形态状态 ===
        public enum NpcForm { None, Mimi, Xilian }
        public static NpcForm CurrentWorldForm = NpcForm.None;
        public static Vector2 LastKnownPosition = Vector2.Zero;

        // === 迷迷模式 ===
        public enum MimiMode { Daily, Battle, BattleEndDelay }
        public static MimiMode CurrentMimiMode = MimiMode.Daily;

        // === 肉山击败状态 ===
        public static bool IsWallOfFleshDefeated = false;

        // === 网络同步 ===
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write((byte)CurrentWorldForm);
            writer.Write(LastKnownPosition.X);
            writer.Write(LastKnownPosition.Y);
            writer.Write((byte)CurrentMimiMode);
            writer.Write(IsWallOfFleshDefeated);
        }

        public override void NetReceive(BinaryReader reader)
        {
            CurrentWorldForm = (NpcForm)reader.ReadByte();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            LastKnownPosition = new Vector2(x, y);
            CurrentMimiMode = (MimiMode)reader.ReadByte();
            IsWallOfFleshDefeated = reader.ReadBoolean();

            // 修复：客户端接收到网络同步时也检查Boss状态
            CheckAndResetBattleMode();
        }

        // === 数据保存/加载 ===
        public override void SaveWorldData(TagCompound tag)
        {
            tag["CurrentWorldForm"] = (int)CurrentWorldForm;
            tag["LastX"] = LastKnownPosition.X;
            tag["LastY"] = LastKnownPosition.Y;
            tag["CurrentMimiMode"] = (int)CurrentMimiMode;
            tag["IsWallOfFleshDefeated"] = IsWallOfFleshDefeated;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            CurrentWorldForm = (NpcForm)tag.GetInt("CurrentWorldForm");
            float x = tag.GetFloat("LastX");
            float y = tag.GetFloat("LastY");
            LastKnownPosition = new Vector2(x, y);
            CurrentMimiMode = (MimiMode)tag.GetInt("CurrentMimiMode");
            IsWallOfFleshDefeated = tag.GetBool("IsWallOfFleshDefeated");

            // === 修复：加载世界数据时检查是否需要重置战斗模式 ===
            CheckAndResetBattleMode();
        }

        // === 检查是否有BOSS在场 ===
        public static bool IsAnyBossActive()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].boss && !Main.npc[i].friendly)
                {
                    // 检查是否为肉山
                    int type = Main.npc[i].type;
                    if (type == NPCID.WallofFlesh || type == NPCID.WallofFleshEye)
                    {
                        continue; // 跳过肉山，由特殊逻辑处理
                    }
                    return true;
                }
            }
            return false;
        }

        // === 新增：检查和重置战斗模式 ===
        private void CheckAndResetBattleMode()
        {
            // 只有在战斗模式下才需要检查
            if (CurrentMimiMode == MimiMode.Battle || CurrentMimiMode == MimiMode.BattleEndDelay)
            {
                bool hasActiveBoss = IsAnyBossActive();

                // 没有活跃的Boss，强制恢复日常模式
                if (!hasActiveBoss)
                {
                    CurrentMimiMode = MimiMode.Daily;

                    // 网络同步
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.WorldData);
                    }
                }
            }
        }

        // === 更新状态方法 ===
        public static void UpdateFormState(NpcForm form, Vector2 position)
        {
            CurrentWorldForm = form;
            LastKnownPosition = position;
        }

        public static void SetMimiMode(MimiMode mode)
        {
            CurrentMimiMode = mode;
        }

        public static void SetWallOfFleshDefeated(bool defeated)
        {
            IsWallOfFleshDefeated = defeated;
        }

        // === 保存位置的方法 ===
        public static void SaveLastMimiPosition(Vector2 position)
        {
            LastKnownPosition = position;
            CurrentWorldForm = NpcForm.Mimi;
        }

        public static void SaveLastXilianPosition(Vector2 position)
        {
            LastKnownPosition = position;
            CurrentWorldForm = NpcForm.Xilian;
        }

        // === 新增：世界加载后检查 ===
        public override void OnWorldLoad()
        {
            // 修复：世界加载时强制检查一次Boss状态
            CheckAndResetBattleMode();
        }
    }
}