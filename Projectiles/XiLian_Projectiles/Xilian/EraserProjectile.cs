using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Projectiles.XiLian_Projectiles.Xilian
{
    public class EraserProjectile : ModProjectile
    {
        // 橡皮擦半径
        public float Radius = 50f;

        // 淡出
        private const int FadeDuration = 30; // 0.5 秒
        private bool isFading = false;
        private int fadeTimer = 0;

        // 脉动缩放
        private float scale = 0.7f;

        // 粉色
        private static readonly Color PinkStarColor = new Color(255, 170, 225);
        private static readonly Color PinkGlowColor = new Color(255, 130, 210);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60 * 60;
            Projectile.tileCollide = false;     // 穿墙
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.alpha = 128;
            Projectile.scale = 0.7f;
            Projectile.light = 0f;              // 避免光柱
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // ─────────── 位置与状态控制（仅拥有者） ───────────
            if (Projectile.owner == Main.myPlayer)
            {
                if (!isFading)
                {
                    if (Main.mouseRight)
                    {
                        Projectile.Center = Main.MouseWorld;
                        Projectile.velocity = Vector2.Zero;
                        Projectile.netUpdate = true;
                    }
                    else
                    {
                        isFading = true;
                        Projectile.netUpdate = true;
                    }
                }
            }

            // 淡出计算
            float fadeAlpha = 1f;
            if (isFading)
            {
                fadeTimer++;
                fadeAlpha = 1f - (fadeTimer / (float)FadeDuration);
                if (fadeTimer >= FadeDuration)
                {
                    Projectile.Kill();
                    return;
                }
            }

            // ─────────── 未淡出时清除弹幕 ───────────
            if (!isFading)
                ClearProjectilesInRadius();

            // ─────────── 视觉（保持与墨水同风格） ───────────
            float pulse = (float)Math.Sin(Main.GameUpdateCount * 0.12f) * 0.15f + 0.85f;
            scale = 0.7f * pulse;
            Projectile.scale = scale;

            // 粉色发光（轻量，避免光柱）
            Lighting.AddLight(Projectile.Center, 0.35f, 0.15f, 0.3f);

            // 中心粉色星星粒子簇
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position, Projectile.width, Projectile.height,
                    DustID.PinkTorch, 0f, 0f, 100, default, 1.2f * scale);
                dust.velocity = Main.rand.NextVector2Circular(0.5f, 0.5f);
                dust.noGravity = true;
                dust.color = PinkStarColor;
            }

            // 外圈粒子（半径 50）
            GenerateRingParticles(fadeAlpha);
        }

        // 外圈粒子
        private void GenerateRingParticles(float fadeAlpha)
        {
            if (fadeAlpha <= 0f) return;

            for (int i = 0; i < 3; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 offset = angle.ToRotationVector2() * Radius;
                Vector2 pos = Projectile.Center + offset;

                Dust d = Dust.NewDustPerfect(
                    pos,
                    DustID.PinkFairy,
                    Vector2.Zero,
                    0, default, 1.0f * fadeAlpha + 0.3f);
                d.noGravity = true;
                d.color = PinkStarColor;

                // 轻微切向速度，让环有呼吸感
                Vector2 tangent = new Vector2(-offset.Y, offset.X).SafeNormalize(Vector2.Zero);
                d.velocity = tangent * Main.rand.NextFloat(-0.6f, 0.6f);
            }

            // 内部随机散布
            if (Main.rand.NextBool(2))
            {
                float r = Main.rand.NextFloat(0f, Radius * 0.9f);
                float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + a.ToRotationVector2() * r;

                Dust d = Dust.NewDustPerfect(
                    pos,
                    DustID.PinkFairy,
                    Vector2.Zero,
                    0, default, 0.7f * fadeAlpha + 0.2f);
                d.noGravity = true;
                d.color = PinkStarColor;
            }
        }

        // 清除弹幕：按弹幕伤害消耗魔力
        private void ClearProjectilesInRadius()
        {
            Player owner = Main.player[Projectile.owner];

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active) continue;
                if (proj == Projectile) continue;
                if (proj.type == Projectile.type) continue;

                float dist = Vector2.Distance(proj.Center, Projectile.Center);
                if (dist <= Radius + proj.width * 0.5f)
                {
                    int manaCost = Math.Max(1, proj.damage);

                    // 魔力不足则跳过
                    if (owner.statMana < manaCost)
                        continue;

                    owner.statMana -= manaCost;
                    if (owner.manaRegenDelay < 60)
                        owner.manaRegenDelay = 60;

                    // 粉色清除特效
                    for (int j = 0; j < 4; j++)
                    {
                        Dust d = Dust.NewDustDirect(proj.position, proj.width, proj.height,
                            DustID.PinkTorch, 0f, 0f, 100, default, 1.3f);
                        d.noGravity = true;
                        d.color = PinkStarColor;
                    }

                    proj.Kill();
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 当前透明度
            float fadeAlpha = 1f;
            if (isFading)
                fadeAlpha = MathHelper.Clamp(1f - (fadeTimer / (float)FadeDuration), 0f, 1f);

            // 与墨水同贴图：HallowStar
            Texture2D texture = ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + ProjectileID.HallowStar).Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() * 0.5f;

            // 缓速旋转
            float rotation = Main.GlobalTimeWrappedHourly * 1.5f;

            // 粉色主体
            Color color = PinkStarColor * fadeAlpha;

            // 外发光（粉色）
            Color glowColor = PinkGlowColor * (0.4f * fadeAlpha);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, glowColor,
                rotation, origin, Projectile.scale * 1.5f, SpriteEffects.None, 0);

            // 主体星星
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, color,
                rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void Kill(int timeLeft)
        {
            // 淡出到消失后的散射粒子
            for (int i = 0; i < 20; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 offset = angle.ToRotationVector2() * Radius;
                Dust d = Dust.NewDustPerfect(Projectile.Center + offset, DustID.PinkFairy, Vector2.Zero, 0, default, 1.2f);
                d.velocity = offset.SafeNormalize(Vector2.Zero) * 4f;
                d.noGravity = true;
                d.color = PinkStarColor;
            }
        }
    }
}