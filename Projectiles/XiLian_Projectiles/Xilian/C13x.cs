using Amphoreus.Items.XiLian_Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Projectiles.XiLian_Projectiles.Xilian
{
    public class C13x : ModProjectile
    {
        private const int SlowTime = 60; // 1秒缓慢
        private int slowTimer = 0;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;      // 必须为true才能与敌对弹幕交互？但这里我们只做消除，不造成伤害
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;       // 无限穿透，但我们会主动消除
            Projectile.timeLeft = 1111 * 60;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.light = 0.8f;
            Projectile.scale = 0.8f;
            Projectile.damage = 0;            // 不造成伤害
            Projectile.GetGlobalProjectile<C8GlobalProjectile>().IsC8Projectile = true;
        }

        public override void AI()
        {
            if (slowTimer < SlowTime)
            {
                // 缓慢阶段：保持原速，不追踪
                slowTimer++;
            }
            else
            {
                // 加速阶段：追踪敌对弹幕
                TrackHostileProjectiles();
            }

            // 每帧检测碰撞并消除敌对弹幕（同时自身消失）
            CheckCollisionAndEliminate();

            Lighting.AddLight(Projectile.Center, Main.DiscoColor.ToVector3() * 0.3f);
            Projectile.rotation += 0.2f;
        }

        private void TrackHostileProjectiles()
        {
            float maxDetect = 400f;
            Projectile target = null;
            float minDistSq = maxDetect * maxDetect;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active) continue;
                if (other.whoAmI == Projectile.whoAmI) continue;
                if (other.type == Projectile.type) continue;
                if (!other.hostile) continue; // 只追踪敌对弹幕

                float distSq = Vector2.DistanceSquared(Projectile.Center, other.Center);
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    target = other;
                }
            }

            if (target != null)
            {
                Vector2 dir = target.Center - Projectile.Center;
                dir.Normalize();
                float targetSpeed = 12f;
                Vector2 desired = dir * targetSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.15f);
            }
        }

        private void CheckCollisionAndEliminate()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active) continue;
                if (other.whoAmI == Projectile.whoAmI) continue;
                if (!other.hostile) continue; // 只消除敌对弹幕

                float dist = Vector2.Distance(Projectile.Center, other.Center);
                float threshold = (Projectile.width + other.width) * 0.4f;
                if (dist < threshold)
                {
                    // 消除敌对弹幕
                    other.active = false;
                    // 自身消失
                    Projectile.active = false;

                    // 网络同步
                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, other.whoAmI);
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, Projectile.whoAmI);
                    }

                    // 播放音效（可选）
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4, Projectile.Center);
                    // 生成粒子
                    for (int d = 0; d < 5; d++)
                    {
                        Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
                    }
                    break; // 只处理一次碰撞
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            if (tex == null) return false;

            float hue = (Main.GlobalTimeWrappedHourly * 0.2f + Projectile.whoAmI * 0.1f) % 1f;
            Color color = Main.hslToRgb(hue, 1f, 0.7f);
            color.A = 200;

            Vector2 origin = tex.Size() / 2f;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, color * 0.3f, Projectile.rotation, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0);
            return false;
        }

        // 确保副弹幕不会伤害 NPC（已设置 damage=0，并且在 ModifyHitNPC 中也会被全局类处理）
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // 完全禁用伤害
            modifiers.FinalDamage.Base = 0f;
        }
    }
}