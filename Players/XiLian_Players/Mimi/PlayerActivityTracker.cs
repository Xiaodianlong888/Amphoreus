using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace Amphoreus.Players.XiLian_Players.Mimi
{
    public class PlayerActivityTracker : ModPlayer
    {
        // 网络同步字段
        public bool IsIdle { get; private set; }
        public int IdleTime { get; private set; }

        private Vector2 lastPosition;
        private int checkTimer;

        // 固定参数
        private const int CheckInterval = 60 * 30; // 30秒检测一次
        private const float MoveThreshold = 10f; // 10像素内视为没动

        public override void OnEnterWorld()
        {
            // 玩家进入世界时初始化
            lastPosition = Player.Center;
            IsIdle = false;
            IdleTime = 0;
            checkTimer = 0;
        }

        public override void PostUpdate()
        {
            // 使用计时器，而不是每帧计算
            checkTimer++;

            if (checkTimer >= CheckInterval)
            {
                checkTimer = 0;

                // 计算移动距离
                float distance = Vector2.Distance(lastPosition, Player.Center);

                if (distance < MoveThreshold)
                {
                    // 玩家没动
                    IdleTime += CheckInterval;

                    // 30秒没动标记为挂机
                    if (IdleTime >= 60 * 30)
                    {
                        IsIdle = true;
                    }
                }
                else
                {
                    // 玩家移动了
                    if (IsIdle)
                    {
                        // 刚从挂机状态恢复
                        OnPlayerWakeUp();
                    }

                    IdleTime = 0;
                    IsIdle = false;
                }

                // 更新最后位置
                lastPosition = Player.Center;
            }
        }

        private void OnPlayerWakeUp()
        {
            // 玩家从挂机状态醒来
            // 这里可以触发一些效果
        }

        // 网络同步
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)2); // 包ID：2 = 活动状态数据
            packet.Write((byte)Player.whoAmI);
            packet.Write(IsIdle);
            packet.Write(IdleTime);
            packet.Send(toWho, fromWho);
        }

        public static void HandlePacket(BinaryReader reader)
        {
            byte packetId = reader.ReadByte();
            if (packetId == 2)
            {
                int playerWho = reader.ReadByte();
                bool isIdle = reader.ReadBoolean();
                int idleTime = reader.ReadInt32();

                if (playerWho < Main.maxPlayers)
                {
                    Player player = Main.player[playerWho];
                    if (player.active)
                    {
                        var tracker = player.GetModPlayer<PlayerActivityTracker>();
                        tracker.IsIdle = isIdle;
                        tracker.IdleTime = idleTime;
                    }
                }
            }
        }
    }
}