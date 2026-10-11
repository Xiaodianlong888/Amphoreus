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
        private float alpha = 1f;
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
            Projectile.timeLeft = 93 * 60; // 93秒
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.alpha = 128;
            Projectile.scale = 0.5f;
            Projectile.light = 0.5f;
        }

        public override void AI()
        {
            // 缓慢脉动效果
            float pulse = (float)Math.Sin(Main.GameUpdateCount * 0.1f) * 0.15f + 0.85f;
            scale = 0.5f * pulse;
            Projectile.scale = scale;

            // 紫色发光效果
            Lighting.AddLight(Projectile.Center, 0.6f, 0.2f, 0.8f);

            // 持续生成紫色粉色发光粒子
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

                // 添加淡粉色粒子混合
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

            // 轻微的上下浮动
            Projectile.position.Y += (float)Math.Sin(Main.GameUpdateCount * 0.05f + Projectile.whoAmI) * 0.1f;
            Projectile.position.X += (float)Math.Cos(Main.GameUpdateCount * 0.03f + Projectile.whoAmI) * 0.05f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 使用紫色
            Color color = new Color(180, 50, 255, 150);

            // 主纹理 - 使用星星形状
            Texture2D texture = ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + ProjectileID.HallowStar).Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() * 0.5f;

            // 绘制主投影
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, color,
                Projectile.rotation, origin, Projectile.scale * 0.8f, SpriteEffects.None, 0);

            return false;
        }

        public override void Kill(int timeLeft)
        {
            // 消失时的粒子爆发
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