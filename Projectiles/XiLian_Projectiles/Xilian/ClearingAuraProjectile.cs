using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace Amphoreus.Projectiles.XiLian_Projectiles.XiLian
{
    public class ClearingAuraProjectile : ModProjectile
    {
        private float currentRadius = 10f;
        private const float MAX_RADIUS = 600f;
        private const float EXPANSION_SPEED = 50f;
        private const float DURATION = 13f;

        // 淡粉色颜色值 #f8e0f4
        private static readonly Color LightPink = new Color(248, 224, 244);

        // 使用原版贴图
        public override string Texture => "Terraria/Images/Extra_89";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = (int)(DURATION * 60);
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.alpha = 100;
            Projectile.light = 0.8f;
            Projectile.aiStyle = -1;

            // 关键设置：确保弹幕不会移动
            Projectile.velocity = Vector2.Zero;
            Projectile.position = Main.LocalPlayer.Center - new Vector2(10, 10);
        }

        public override void AI()
        {
            // 缓慢扩展半径（由内向外）
            currentRadius += EXPANSION_SPEED / 60f;

            if (currentRadius > MAX_RADIUS)
                currentRadius = MAX_RADIUS;

            // 淡粉色发光效果
            float lightIntensity = Math.Min(1f, currentRadius / 100f) * 0.8f;
            Lighting.AddLight(Projectile.Center,
                LightPink.R / 255f * lightIntensity,
                LightPink.G / 255f * lightIntensity,
                LightPink.B / 255f * lightIntensity);

            // 生成淡粉色粒子效果
            GenerateParticles();

            // 清除范围内所有弹幕（不分敌友）
            ClearAllProjectiles();
        }

        private void GenerateParticles()
        {
            // 在光圈边缘生成淡粉色粒子
            int particleCount = (int)(currentRadius / 15f);

            for (int i = 0; i < particleCount; i++)
            {
                if (Main.rand.NextBool(2))
                {
                    // 计算粒子位置（在光圈边缘）
                    float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * currentRadius;
                    Vector2 dustPos = Projectile.Center + offset;

                    // 淡粉色粒子
                    Dust dust = Dust.NewDustPerfect(dustPos, DustID.PinkFairy, Vector2.Zero, 0, default, 1.2f);
                    dust.velocity = -offset.SafeNormalize(Vector2.Zero) * 1.5f; // 向内移动
                    dust.noGravity = true;
                    dust.color = LightPink;

                    // 额外的小粒子
                    if (Main.rand.NextBool(3))
                    {
                        Dust smallDust = Dust.NewDustPerfect(dustPos, DustID.PinkTorch, Vector2.Zero, 0, default, 0.8f);
                        smallDust.velocity = Main.rand.NextVector2Circular(0.8f, 0.8f);
                        smallDust.noGravity = true;
                        smallDust.color = LightPink;
                    }
                }
            }

            // 在光圈内部生成随机粒子（模拟向外扩散效果）
            if (Main.rand.NextBool(4))
            {
                // 随机位置，但靠近中心
                float randomRadius = Main.rand.NextFloat(0, currentRadius * 0.7f);
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * randomRadius;
                Vector2 dustPos = Projectile.Center + offset;

                Dust innerDust = Dust.NewDustPerfect(dustPos, DustID.PinkFairy, Vector2.Zero, 0, default, 0.9f);
                // 向外移动
                innerDust.velocity = offset.SafeNormalize(Vector2.Zero) * 2f;
                innerDust.noGravity = true;
                innerDust.color = LightPink;
            }
        }

        // 修改：清除所有弹幕（包括敌方、友方、中立，但排除自身）
        private void ClearAllProjectiles()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];

                // 排除自身
                if (proj == Projectile)
                    continue;

                // 检查弹幕是否激活
                if (!proj.active)
                    continue;

                // 计算弹幕与光圈中心的距离
                float distance = Vector2.Distance(proj.Center, Projectile.Center);

                // 如果弹幕在光圈范围内（考虑弹幕自身尺寸）
                if (distance < currentRadius + proj.width / 2f)
                {
                    // 创建淡粉色清除特效
                    for (int j = 0; j < 5; j++)
                    {
                        Dust dust = Dust.NewDustDirect(proj.position, proj.width, proj.height,
                            DustID.PinkTorch, 0f, 0f, 100, default, 1.3f);
                        dust.velocity = (proj.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 2.5f;
                        dust.noGravity = true;
                        dust.color = LightPink;
                    }

                    // 播放清除音效（随机播放，避免噪音过大）
                    if (Main.rand.NextBool(3))
                    {
                        SoundEngine.PlaySound(SoundID.Item10.WithVolumeScale(0.3f), proj.position);
                    }

                    // 清除弹幕
                    proj.Kill();
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 绘制淡粉色光圈
            Texture2D circleTexture = Terraria.GameContent.TextureAssets.Projectile[ProjectileID.LostSoulFriendly].Value;

            // 计算光圈透明度（随时间渐变）
            float fade = 1f;
            if (Projectile.timeLeft < 60) // 最后1秒开始淡出
                fade = Projectile.timeLeft / 60f;

            // 淡粉色 #f8e0f4 带透明度
            Color color = new Color(248, 224, 244, (int)(180 * fade));

            // 绘制主光圈 - 使用固定的中心点
            float scale = currentRadius / 50f;

            // 直接使用Projectile.Center，不进行任何偏移
            Main.EntitySpriteDraw(circleTexture, Projectile.Center - Main.screenPosition, null, color,
                0f, circleTexture.Size() * 0.5f, scale, SpriteEffects.None, 0);

            return false;
        }

        public override void Kill(int timeLeft)
        {
            // 光圈消失时的特效
            for (int i = 0; i < 40; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * currentRadius;
                Vector2 dustPos = Projectile.Center + offset;

                Dust dust = Dust.NewDustPerfect(dustPos, DustID.PinkFairy, Vector2.Zero, 0, default, 2f);
                dust.velocity = offset.SafeNormalize(Vector2.Zero) * 6f;
                dust.noGravity = true;
                dust.color = LightPink;
            }

            // 播放消失音效
            SoundEngine.PlaySound(SoundID.Item27.WithVolumeScale(0.5f), Projectile.Center);
        }
    }
}