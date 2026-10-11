using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Projectiles.SFeir_Projectiles
{
    public class FateCoin : ModProjectile
    {
        private int result = 0; // 0=旋转中, 1=正面, 2=反面
        private int spinTimer = 0;
        private const int SPIN_DURATION = 180; // 旋转3秒
        private const int RESULT_DURATION = 120; // 显示结果2秒

        public override void SetStaticDefaults()
        {
            //DisplayName.SetDefault("命运硬币");
            Main.projFrames[Projectile.type] = 2; // 2帧：正面和反面
        }

        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 51;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = SPIN_DURATION + RESULT_DURATION;
            Projectile.scale = 1f;
        }

        public override void AI()
        {
            spinTimer++;

            // 旋转阶段
            if (spinTimer < SPIN_DURATION)
            {
                // 加速旋转
                float spinSpeed = 0.3f + (spinTimer / (float)SPIN_DURATION) * 0.7f;
                Projectile.rotation += spinSpeed;

                // 上下浮动
                Projectile.Center += new Vector2(0, (float)Math.Sin(spinTimer * 0.1f) * 0.5f);

                // 跟随玩家
                int playerIndex = (int)Projectile.ai[1];
                if (playerIndex >= 0 && playerIndex < Main.maxPlayers)
                {
                    Player player = Main.player[playerIndex];
                    if (player.active && !player.dead)
                    {
                        Vector2 targetPos = player.Center + new Vector2(0, -120);
                        Projectile.Center = Vector2.Lerp(Projectile.Center, targetPos, 0.05f);
                    }
                }
            }
            // 显示结果阶段
            else if (spinTimer == SPIN_DURATION)
            {
                // 决定结果
                result = Main.rand.Next(1, 3); // 1=正面, 2=反面
                Projectile.rotation = 0;
                Projectile.frame = result - 1;

                // 音效
                SoundEngine.PlaySound(result == 1 ? SoundID.Item35 : SoundID.Item36, Projectile.Center);

                // 通知Boss
                int bossIndex = (int)Projectile.ai[0];
                if (bossIndex >= 0 && bossIndex < Main.maxNPCs)
                {
                    NPC boss = Main.npc[bossIndex];
                    if (boss.active && boss.ModNPC is NPCs.Boss.SFeir_Boss.SFeir sfeir)
                    {
                        sfeir.LieManager?.OnCoinResult(result);
                    }
                }

                // 显示结果给玩家
                int playerIndex = (int)Projectile.ai[1];
                if (playerIndex >= 0 && playerIndex < Main.maxPlayers)
                {
                    Player player = Main.player[playerIndex];
                    if (player.active)
                    {
                        string resultText = result == 1 ? "正面！" : "反面~";
                        Color color = result == 1 ? Color.Gold : Color.Silver;
                        CombatText.NewText(player.getRect(), color, resultText, true);
                    }
                }
            }
            // 结果展示阶段
            else
            {
                // 轻微抖动
                if (result == 1) // 正面：兴奋抖动
                {
                    Projectile.position += new Vector2(
                        Main.rand.NextFloat(-0.3f, 0.3f),
                        Main.rand.NextFloat(-0.3f, 0.3f)
                    );
                }
                else // 反面：沉稳抖动
                {
                    Projectile.position += new Vector2(
                        Main.rand.NextFloat(-0.1f, 0.1f),
                        Main.rand.NextFloat(-0.1f, 0.1f)
                    );
                }

                // 逐渐透明
                if (Projectile.timeLeft < 60)
                {
                    Projectile.alpha = (int)((60 - Projectile.timeLeft) * 4.25f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 使用你的翻飞之币贴图
            Texture2D texture = ModContent.Request<Texture2D>("Amphoreus/Projectiles/SFeir_Projectiles/FateCoin").Value;

            // 如果还在旋转，使用完整的贴图
            // 如果已出结果，根据帧选择部分
            Rectangle? sourceRect = null;
            if (result > 0 && Main.projFrames[Projectile.type] > 1)
            {
                int frameHeight = texture.Height / Main.projFrames[Projectile.type];
                sourceRect = new Rectangle(0, (result - 1) * frameHeight, texture.Width, frameHeight);
            }

            Vector2 origin = sourceRect.HasValue ?
                new Vector2(sourceRect.Value.Width / 2, sourceRect.Value.Height / 2) :
                texture.Size() / 2f;

            Color drawColor = lightColor * (1f - Projectile.alpha / 255f);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRect,
                drawColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}