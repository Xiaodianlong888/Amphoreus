using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Projectiles.XiLian_Projectiles.Xilian
{
    public class QuillInkProjectile : ModProjectile
    {
        // 到达判定阈值
        private const float ArriveThreshold = 6f;

        // 目标位置（由发射时写入 ai[0], ai[1]）
        private Vector2 TargetPosition => new Vector2(Projectile.ai[0], Projectile.ai[1]);

        private bool hasArrived = false;

        // 脉动缩放
        private float scale = 0.5f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 93 * 60;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;     // 穿墙
            Projectile.DamageType = DamageClass.Magic;
            Projectile.alpha = 128;
            Projectile.scale = 0.5f;
            Projectile.light = 0f;              // 关闭自带光照，避免光柱
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override void AI()
        {
            // ─────────── 移动逻辑 ───────────
            if (!hasArrived)
            {
                Vector2 toTarget = TargetPosition - Projectile.Center;
                float distance = toTarget.Length();

                if (distance <= ArriveThreshold)
                {
                    // 到达：精确对齐，停下
                    Projectile.Center = TargetPosition;
                    Projectile.velocity = Vector2.Zero;
                    hasArrived = true;

                    // 到达时的短暂爆发
                    for (int i = 0; i < 8; i++)
                    {
                        Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                            DustID.PurpleTorch, 0f, 0f, 100, default, 1.5f);
                        d.velocity = Main.rand.NextVector2Circular(2f, 2f);
                        d.noGravity = true;
                    }
                }
                else
                {
                    // 保持朝目标飞行
                    Vector2 dir = toTarget / distance;
                    float speed = Projectile.velocity.Length();
                    if (speed < 1f) speed = 15f;
                    Projectile.velocity = dir * speed;

                    // 朝向飞行方向
                    Projectile.rotation = Projectile.velocity.ToRotation();
                }
            }
            else
            {
                // 已到达：维持静止
                Projectile.velocity = Vector2.Zero;

                // 到位后轻微自转
                Projectile.rotation += 0.03f;

                // 轻微上下浮动（叠加在 Center 上，不做位置累加，避免漂移）
                float bob = (float)Math.Sin(Main.GameUpdateCount * 0.05f + Projectile.whoAmI) * 1.5f;
                Projectile.Center = TargetPosition + new Vector2(0f, bob);
            }

            // ─────────── 视觉逻辑（保留原效果） ───────────
            float pulse = (float)Math.Sin(Main.GameUpdateCount * 0.1f) * 0.15f + 0.85f;
            scale = 0.5f * pulse;
            Projectile.scale = scale;

            // 紫色发光（轻量，不会形成光柱）
            Lighting.AddLight(Projectile.Center, 0.35f, 0.12f, 0.45f);

            // 持续生成紫色/粉色粒子
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.PurpleTorch,
                    0f, 0f, 100, default, 1.2f * scale);
                dust.velocity = Main.rand.NextVector2Circular(0.5f, 0.5f);
                dust.noGravity = true;

                if (Main.rand.NextBool(5))
                {
                    Dust pinkDust = Dust.NewDustDirect(
                        Projectile.position,
                        Projectile.width,
                        Projectile.height,
                        DustID.PinkTorch,
                        0f, 0f, 100, default, 0.8f * scale);
                    pinkDust.velocity = Main.rand.NextVector2Circular(0.3f, 0.3f);
                    pinkDust.noGravity = true;
                }

                if (Main.rand.NextBool(10))
                {
                    Dust largeDust = Dust.NewDustDirect(
                        Projectile.position,
                        Projectile.width,
                        Projectile.height,
                        DustID.GemAmethyst,
                        0f, 0f, 0, default, 1.5f * scale);
                    largeDust.velocity = Main.rand.NextVector2Circular(1f, 1f);
                    largeDust.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 紫色
            Color color = new Color(180, 50, 255, 150);

            Texture2D texture = ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + ProjectileID.HallowStar).Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() * 0.5f;

            // 外发光（紫色，叠加绘制，稍大）
            Color glowColor = new Color(140, 40, 220, 80);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, glowColor,
                Projectile.rotation, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0);

            // 主体
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, color,
                Projectile.rotation, origin, Projectile.scale * 0.8f, SpriteEffects.None, 0);

            return false;
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    DustID.PurpleTorch, 0f, 0f, 100, default, 1.2f);
                dust.velocity = Main.rand.NextVector2Circular(2f, 2f);
                dust.noGravity = true;
            }
        }
    }
}