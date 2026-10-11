using Amphoreus.Items.XiLian_Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Projectiles.XiLian_Projectiles.Xilian
{
    public class C8X : ModProjectile
    {
        private Vector2 lastSpawnPos;
        private const float SpawnDistance = 30f;   // 每30像素生成副弹幕
        private int spawnTimer = 0;
        private const int SpawnRate = 20;          // 每秒3颗（60/3=20帧）

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1111 * 60;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.light = 1f;
            Projectile.scale = 1.5f;   // 主弹幕较大
            Projectile.GetGlobalProjectile<C8GlobalProjectile>().IsC8Projectile = true;
        }

        public override void AI()
        {
            // 追踪最近的敌人（NPC）
            float maxDetect = 600f;
            NPC target = null;
            float minDistSq = maxDetect * maxDetect;
            foreach (var npc in Main.npc)
            {
                if (npc.active && npc.CanBeChasedBy() && !npc.friendly)
                {
                    float distSq = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                    if (distSq < minDistSq)
                    {
                        minDistSq = distSq;
                        target = npc;
                    }
                }
            }
            if (target != null)
            {
                Vector2 dir = target.Center - Projectile.Center;
                dir.Normalize();
                float speed = Projectile.velocity.Length();
                Vector2 desired = dir * speed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.1f);
            }

            // 生成副弹幕：两种方式结合（沿轨迹距离 + 定时）
            // 1. 沿轨迹距离（保证轨迹上有副弹幕）
            if (lastSpawnPos == Vector2.Zero)
            {
                lastSpawnPos = Projectile.Center;
            }
            else
            {
                float dist = Vector2.Distance(Projectile.Center, lastSpawnPos);
                if (dist >= SpawnDistance)
                {
                    SpawnSubProjectile(lastSpawnPos, Projectile.Center);
                    lastSpawnPos = Projectile.Center;
                }
            }

            // 2. 定时生成（保证每秒3颗）
            spawnTimer++;
            if (spawnTimer >= SpawnRate)
            {
                spawnTimer = 0;
                // 定时生成时，也可以沿轨迹随机位置，这里简单在当前位置生成并偏移
                SpawnSubProjectile(Projectile.Center, Projectile.Center + Projectile.velocity * 5f);
            }

            // 发光和旋转
            Lighting.AddLight(Projectile.Center, Main.DiscoColor.ToVector3() * 0.5f);
            Projectile.rotation += 0.3f;
        }

        private void SpawnSubProjectile(Vector2 from, Vector2 to)
        {
            // 在两点之间插值生成，并添加横向偏移
            Vector2 dir = to - from;
            float length = dir.Length();
            if (length < 1f) return;

            // 生成位置取中点
            Vector2 basePos = (from + to) * 0.5f;
            // 横向偏移：垂直于dir的方向
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            perp.Normalize();
            float offsetAmount = Main.rand.NextFloat(-20f, 20f); // 偏移量可调
            Vector2 spawnPos = basePos + perp * offsetAmount;

            int sub = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                Projectile.velocity * 0.3f, // 初始速度较慢（滑行）
                ModContent.ProjectileType<C13x>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner
            );
            Main.projectile[sub].scale = 0.8f; // 副弹幕较小
            Main.projectile[sub].GetGlobalProjectile<C8GlobalProjectile>().IsC8Projectile = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 自定义绘制实现金色→粉色→蓝色渐变
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            if (tex == null) return false;

            // 根据弹幕的存活时间或位置计算渐变因子
            float factor = (float)Projectile.timeLeft / (1111 * 60); // 从1到0
            // 或者使用旅行距离：用Projectile.ai[0]累计距离，这里简单用时间
            // 头部金色（factor接近1），中间粉色，尾部蓝色（factor接近0）
            Color color;
            if (factor > 0.66f)
            {
                // 金色区间：黄橙色
                float t = (factor - 0.66f) / 0.34f; // 0~1
                color = Color.Lerp(new Color(255, 215, 0), new Color(255, 192, 203), t); // 金→粉
            }
            else if (factor > 0.33f)
            {
                // 粉色区间
                float t = (factor - 0.33f) / 0.33f;
                color = Color.Lerp(new Color(255, 192, 203), new Color(135, 206, 235), t); // 粉→蓝
            }
            else
            {
                // 蓝色区间
                float t = factor / 0.33f;
                color = Color.Lerp(new Color(135, 206, 235), new Color(70, 130, 180), t); // 蓝→深蓝
            }

            // 绘制主弹幕（带发光效果）
            Vector2 origin = tex.Size() / 2f;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            // 添加光晕
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, color * 0.3f, Projectile.rotation, origin, Projectile.scale * 1.3f, SpriteEffects.None, 0);
            return false;
        }
    }
}