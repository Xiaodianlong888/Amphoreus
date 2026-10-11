using Amphoreus.Systems.Ruwosuoshu_Systems;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Players.XilLian_Players.Mimi
{
    public class MimiBuffPlayer : ModPlayer
    {

        public bool hasDodgeAbility = false;

        // === 网络同步字段 ===
        public bool hasMimiBuff = false;
        public int dodgeCooldown = 0;

        // === 固定参数 ===
        private const int DodgeCooldownMax = 60 * 60; // 60秒冷却
        private const float DodgeChance = 1.0f; // 100%闪避概率

        // === 网络同步 ===
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            // 创建数据包
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)1); // 包ID：1 = MimiBuff数据
            packet.Write((byte)Player.whoAmI);
            packet.Write(hasMimiBuff);
            packet.Write(dodgeCooldown);
            packet.Send(toWho, fromWho);
        }

        public override void CopyClientState(ModPlayer targetCopy)
        {
            MimiBuffPlayer clone = (MimiBuffPlayer)targetCopy;
            clone.hasMimiBuff = hasMimiBuff;
            clone.dodgeCooldown = dodgeCooldown;
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            MimiBuffPlayer client = (MimiBuffPlayer)clientPlayer;

            // 如果状态不同步，发送更新
            if (client.hasMimiBuff != hasMimiBuff || client.dodgeCooldown != dodgeCooldown)
            {
                SyncPlayer(-1, -1, false);
            }
        }

        public override void ResetEffects()
        {
            // Buff会自然过期，不需要手动重置
            // 冷却减少逻辑
            if (dodgeCooldown > 0)
                dodgeCooldown--;
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            // 只有拥有迷迷的buff并且解锁了闪避能力时才检查闪避
            if (hasMimiBuff && dodgeCooldown == 0 && BookDataSystem.HasUnlockedDodgeAbility)
            {
                // 100%概率触发闪避
                if (Main.rand.NextFloat() < 1.0f)
                {
                    modifiers.FinalDamage *= 0; // 完全闪避
                    dodgeCooldown = DodgeCooldownMax;

                    // 无敌帧
                    Player.immune = true;
                    Player.immuneTime = 90;

                    // 粉色爱心特效
                    SpawnHeartParticles();

                    // 粉色音效
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.8f }, Player.Center);

                    // 粉色字幕提示
                    ShowMimiText();
                }
            }
        }

        // === 修复：使用正确的粒子ID ===
        private void SpawnHeartParticles()
        {
            Vector2 center = Player.Center;

            // 1. 核心心形粒子 - 使用正确的DustID
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    center,
                    0, 0,
                    DustID.HeartCrystal, // 修正：使用正确的爱心粒子ID
                    0f, 0f,
                    150,
                    new Color(255, 100, 255), // 粉色覆盖原红色
                    1.6f
                );

                float angle = MathHelper.TwoPi * (i / 8f);
                float speed = Main.rand.NextFloat(2f, 3.5f);
                dust.velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed
                );
                dust.noGravity = true;
                dust.fadeIn = 1.2f;
            }

            // 2. 粉色精灵环绕粒子
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    center,
                    0, 0,
                    DustID.PinkFairy, // 粉色精灵粒子
                    0f, 0f,
                    100,
                    new Color(255, 150, 255), // 浅粉色
                    1.2f
                );

                dust.velocity = new Vector2(
                    Main.rand.NextFloat(-1.5f, 1.5f),
                    Main.rand.NextFloat(-2.5f, -1f) // 向上飘
                );
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.8f, 1.1f);
            }

            // 3. 粉色火焰粒子
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    center + new Vector2(Main.rand.Next(-30, 31), Main.rand.Next(-30, 31)),
                    0, 0,
                    DustID.PinkTorch, // 粉色火把粒子
                    0f, 0f,
                    80,
                    new Color(255, 100, 255, 150), // 半透明粉色
                    1.4f
                );
                dust.noGravity = true;
                dust.velocity *= 0.5f;
            }
        }

        // === 显示粉色字幕 ===
        private void ShowMimiText()
        {
            // 计算屏幕位置（玩家头顶）
            Vector2 textPosition = Player.Center - new Vector2(0, 50);

            // 创建粉色字幕
            CombatText.NewText(
                new Rectangle((int)textPosition.X, (int)textPosition.Y, 1, 1),
                new Color(255, 100, 255), // 粉色
                "迷迷~",
                true,  // 让文字更明显
                false
            );

            // 添加心形装饰
            if (Main.rand.NextBool(2))
            {
                CombatText.NewText(
                    new Rectangle((int)textPosition.X + Main.rand.Next(-20, 21),
                                 (int)textPosition.Y - Main.rand.Next(20, 40), 1, 1),
                    new Color(255, 150, 255, 200), // 半透明粉色
                    "❤",
                    false,
                    false
                );
            }
        }

        // === 处理网络数据包 ===
        public static void HandlePacket(BinaryReader reader, int fromWho)
        {
            byte packetId = reader.ReadByte();

            if (packetId == 1) // MimiBuff数据包
            {
                int playerWho = reader.ReadByte();
                bool hasBuff = reader.ReadBoolean();
                int cooldown = reader.ReadInt32();

                if (playerWho < Main.maxPlayers)
                {
                    Player player = Main.player[playerWho];
                    if (player.active)
                    {
                        MimiBuffPlayer modPlayer = player.GetModPlayer<MimiBuffPlayer>();
                        modPlayer.hasMimiBuff = hasBuff;
                        modPlayer.dodgeCooldown = cooldown;
                    }
                }
            }
        }
    }
}