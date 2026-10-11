using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Projectiles.XiLian_Projectiles.Mimi
{
    public class Mimi_zhua_NoWall : ModProjectile
    {
        private const float MaxSpeed = 5f;
        private const float Acceleration = 0.2f;
        private const float TrackingRange = 1200f;
        private const int TrailDustInterval = 4;
        private const int ProjectileLifetime = 540;
        private const int FadeOutDuration = 60;
        private const int DamageCooldownFrames = 11;

        private static readonly Color PinkColor = new Color(255, 182, 193, 200);
        private static readonly Color PurpleColor = new Color(186, 85, 211, 200);
        private static readonly Color LightPinkColor = new Color(255, 209, 220, 180);
        private static readonly Color LightPurpleColor = new Color(206, 130, 255, 180);

        private int trailTimer = 0;
        private NPC targetNPC = null;
        private bool hasHitEnemy = false;
        private int fadeOutTimer = 0;
        private bool isFadingOut = false;
        private Vector2 originalVelocity = Vector2.Zero;
        private int damageTimer = 0;
        private int[] npcHitCooldown = new int[Main.maxNPCs];
        private int delayTrackingTimer = 0;
        private float trackingStrength = 0.4f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = (int)(50 * 0.67f);
            Projectile.height = (int)(42 * 0.67f);

            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;           // 碰撞一次后消失
            Projectile.timeLeft = ProjectileLifetime;
            Projectile.tileCollide = true;       // 碰撞墙体
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.aiStyle = -1;

            Projectile.damage = 11;
            Projectile.CritChance = 4;
            Projectile.knockBack = 20f;
            Projectile.ArmorPenetration = 9999;

            for (int i = 0; i < npcHitCooldown.Length; i++)
            {
                npcHitCooldown[i] = 0;
            }
        }

        public override void AI()
        {
            if (delayTrackingTimer == 0)
            {
                delayTrackingTimer = (int)Projectile.ai[0];
                trackingStrength = Projectile.ai[1];
                if (delayTrackingTimer <= 0) delayTrackingTimer = 60;
                if (trackingStrength <= 0) trackingStrength = 0.4f;
                originalVelocity = Projectile.velocity;
            }

            if (isFadingOut)
            {
                ExecuteFadeOut();
                return;
            }

            UpdateDamageCooldowns();

            if (hasHitEnemy)
            {
                Projectile.velocity = originalVelocity;
                if (Projectile.timeLeft < 60)
                {
                    StartFadeOut();
                }
                trailTimer++;
                if (trailTimer >= TrailDustInterval)
                {
                    CreateFadingStarTrail();
                    trailTimer = 0;
                }
                return;
            }

            trailTimer++;
            if (trailTimer >= TrailDustInterval)
            {
                CreateMagicalStarTrail();
                trailTimer = 0;
            }

            if (delayTrackingTimer > 0)
            {
                delayTrackingTimer--;
                if (originalVelocity != Vector2.Zero)
                {
                    Projectile.velocity = originalVelocity;
                }
            }
            else
            {
                if (targetNPC == null || !targetNPC.active || targetNPC.life <= 0 || targetNPC.type == NPCID.TargetDummy)
                {
                    FindTarget();
                }
                if (targetNPC != null)
                {
                    TrackTargetWeakly();
                }
            }

            if (Projectile.timeLeft > ProjectileLifetime - 10)
            {
                Projectile.alpha = (int)(255 * (1 - (ProjectileLifetime - Projectile.timeLeft) / 10f));
            }

            Lighting.AddLight(Projectile.Center,
                Color.Lerp(PinkColor, PurpleColor, 0.5f).ToVector3() * 0.3f);

            damageTimer++;
        }

        private void TrackTargetWeakly()
        {
            Vector2 direction = targetNPC.Center - Projectile.Center;
            float distance = direction.Length();
            if (distance > 0.01f)
            {
                direction.Normalize();
                Vector2 desiredVelocity = direction * MaxSpeed;
                Vector2 newVelocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, Acceleration * trackingStrength);
                if (newVelocity.Length() > MaxSpeed)
                {
                    newVelocity.Normalize();
                    newVelocity *= MaxSpeed;
                }
                Projectile.velocity = newVelocity;
            }
        }

        private void UpdateDamageCooldowns()
        {
            for (int i = 0; i < npcHitCooldown.Length; i++)
            {
                if (npcHitCooldown[i] > 0) npcHitCooldown[i]--;
            }
        }

        private bool IsNPCOnCooldown(NPC npc) => npcHitCooldown[npc.whoAmI] > 0;
        private void SetNPCCooldown(NPC npc) => npcHitCooldown[npc.whoAmI] = DamageCooldownFrames;

        private void FindTarget()
        {
            float closestDistance = TrackingRange;
            targetNPC = null;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.CanBeChasedBy() && npc.active && !npc.friendly && npc.lifeMax > 5 && npc.type != NPCID.TargetDummy)
                {
                    float distance = Vector2.Distance(Projectile.Center, npc.Center);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        targetNPC = npc;
                    }
                }
            }
        }

        private void CreateMagicalStarTrail()
        {
            if (Main.rand.NextFloat() > 0.8f) return;
            bool usePink = Main.rand.NextBool();
            Color starColor = usePink ? PinkColor : PurpleColor;
            Vector2 offset = new Vector2(
                Main.rand.NextFloat(-Projectile.width * 0.6f, Projectile.width * 0.6f),
                Main.rand.NextFloat(-Projectile.height * 0.6f, Projectile.height * 0.6f));
            Vector2 starPosition = Projectile.Center + offset;
            int dustType = usePink ? DustID.PinkFairy : DustID.PurpleTorch;
            Dust starDust = Dust.NewDustPerfect(starPosition, dustType, Vector2.Zero, 180, starColor, 1.0f);
            starDust.noGravity = true;
            starDust.scale = 0.4f + Main.rand.NextFloat(0.3f);
            Vector2 trailDirection = -Projectile.velocity * 0.05f;
            starDust.velocity = trailDirection + Main.rand.NextVector2Circular(0.8f, 0.8f);
            starDust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            if (Main.rand.NextFloat() < 0.2f) CreateMixedColorStar(starPosition);
        }

        private void CreateMixedColorStar(Vector2 position)
        {
            Color mixedColor = Color.Lerp(PinkColor, PurpleColor, Main.rand.NextFloat());
            mixedColor.A = 200;
            Dust mixedStar = Dust.NewDustPerfect(position, 57, Main.rand.NextVector2Circular(1.2f, 1.2f), 200, mixedColor, 1.2f);
            mixedStar.noGravity = true;
            mixedStar.scale = 0.5f + Main.rand.NextFloat(0.2f);
            mixedStar.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        private void CreateFadingStarTrail()
        {
            if (Main.rand.NextFloat() > 0.5f) return;
            bool usePink = Main.rand.NextBool();
            Color starColor = usePink ? LightPinkColor : LightPurpleColor;
            Vector2 offset = new Vector2(
                Main.rand.NextFloat(-Projectile.width * 0.8f, Projectile.width * 0.8f),
                Main.rand.NextFloat(-Projectile.height * 0.8f, Projectile.height * 0.8f));
            Vector2 starPosition = Projectile.Center + offset;
            int dustType = usePink ? DustID.PinkFairy : DustID.PurpleTorch;
            Dust starDust = Dust.NewDustPerfect(starPosition, dustType, Vector2.Zero, 120, starColor, 0.8f);
            starDust.noGravity = true;
            starDust.scale = 0.3f + Main.rand.NextFloat(0.2f);
            starDust.velocity = Main.rand.NextVector2Circular(1.2f, 1.2f);
            starDust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.type != NPCID.TargetDummy)
            {
                if (IsNPCOnCooldown(target))
                {
                    hit.Damage = 0;
                    hit.Knockback = 0f;
                    return;
                }
                SetNPCCooldown(target);

                if (!hasHitEnemy)
                {
                    hasHitEnemy = true;
                    SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
                    Projectile.timeLeft = FadeOutDuration + 10;
                    CreateHitStarBurst();
                }
                else
                {
                    for (int i = 0; i < 3; i++) CreateSmallStarAt(target.Center);
                }

                hit.Damage = 11;
                hit.Knockback = 20f;
                hit.Crit = Main.rand.NextFloat(1f) < (Projectile.CritChance / 100f);
            }
        }

        private void CreateHitStarBurst()
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * (i / 8f);
                Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Color starColor = (i % 2 == 0) ? LightPinkColor : LightPurpleColor;
                Dust burstStar = Dust.NewDustPerfect(Projectile.Center, DustID.PinkFairy, direction * Main.rand.NextFloat(2f, 4f), 180, starColor, 0.9f);
                burstStar.noGravity = true;
                burstStar.scale = 0.5f + Main.rand.NextFloat(0.2f);
                burstStar.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            }
        }

        private void CreateSmallStarAt(Vector2 position)
        {
            bool usePink = Main.rand.NextBool();
            Color starColor = usePink ? PinkColor : PurpleColor;
            Dust smallStar = Dust.NewDustPerfect(position, usePink ? DustID.PinkFairy : DustID.PurpleTorch, Main.rand.NextVector2Circular(2.5f, 2.5f), 150, starColor, 0.7f);
            smallStar.noGravity = true;
            smallStar.scale = 0.35f + Main.rand.NextFloat(0.15f);
        }

        private void StartFadeOut()
        {
            if (!isFadingOut)
            {
                isFadingOut = true;
                fadeOutTimer = FadeOutDuration;
                Projectile.damage = 0;
                for (int i = 0; i < 4; i++) CreateFinalStar();
            }
        }

        private void CreateFinalStar()
        {
            Color finalColor = Color.Lerp(PinkColor, Color.White, 0.3f);
            finalColor.A = 150;
            Dust finalStar = Dust.NewDustPerfect(Projectile.Center, 57, Main.rand.NextVector2Circular(2f, 2f), 150, finalColor, 0.8f);
            finalStar.noGravity = true;
            finalStar.scale = 0.4f;
            finalStar.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        private void ExecuteFadeOut()
        {
            fadeOutTimer--;
            float fadeRatio = 1f - (fadeOutTimer / (float)FadeOutDuration);
            Projectile.alpha = (int)(255 * fadeRatio);
            if (originalVelocity != Vector2.Zero)
            {
                Projectile.velocity = originalVelocity * (1f - fadeRatio * 0.5f);
            }
            if (fadeOutTimer <= 0) Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!isFadingOut)
            {
                StartFadeOut();
            }
            return false; // 阻止系统自动杀死，由淡出处理
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (target.friendly || target.type == NPCID.TargetDummy) return false;
            if (IsNPCOnCooldown(target)) return false;
            return base.CanHitNPC(target);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Color baseColor = hasHitEnemy ? Color.Lerp(PinkColor, Color.White, 0.3f) : PinkColor;
            float glowIntensity = 0.4f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.15f;
            Color glowColor = Color.Lerp(PinkColor, PurpleColor, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f);
            Color drawColor = Color.Lerp(lightColor, baseColor, 0.6f);
            drawColor = Color.Lerp(drawColor, glowColor, glowIntensity * 0.4f);
            drawColor *= ((255 - Projectile.alpha) / 255f);
            float drawScale = 0.67f * Projectile.scale;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor, 0f, new Vector2(texture.Width * 0.5f, texture.Height * 0.5f), drawScale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            for (int i = 0; i < 3; i++) CreateSpawnStar();
        }

        private void CreateSpawnStar()
        {
            bool usePink = Main.rand.NextBool();
            Color starColor = usePink ? LightPinkColor : LightPurpleColor;
            Dust spawnStar = Dust.NewDustPerfect(Projectile.Center, usePink ? DustID.PinkFairy : DustID.PurpleTorch, Main.rand.NextVector2Circular(1.5f, 1.5f), 180, starColor, 0.9f);
            spawnStar.noGravity = true;
            spawnStar.scale = 0.45f;
            spawnStar.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 5; i++) CreateDeathStar();
        }

        private void CreateDeathStar()
        {
            Color deathColor = Color.Lerp(PurpleColor, Color.White, 0.4f);
            deathColor.A = 100;
            Dust deathStar = Dust.NewDustPerfect(Projectile.Center, 57, Main.rand.NextVector2Circular(2.5f, 2.5f), 100, deathColor, 0.7f);
            deathStar.noGravity = true;
            deathStar.scale = 0.35f;
            deathStar.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }
    }
}