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
    public class Mimi_zhua_big : ModProjectile
    {
        private const float InitialSpeed = 2.0f;
        private const float SizeGrowthRate = 0.008f;
        private const float MaxSize = 8.0f;
        private const int TrailDustInterval = 1;
        private const int DamageCooldownFrames = 11;
        private const float GlowIntensity = 1.0f;
        private const int TrailParticleLifetime = 180;

        private static readonly Color PinkColor = new Color(255, 182, 193, 255);
        private static readonly Color PurpleColor = new Color(186, 85, 211, 255);
        private static readonly Color GlowPinkColor = new Color(255, 230, 240, 220);
        private static readonly Color GlowPurpleColor = new Color(230, 140, 255, 220);

        private int trailTimer = 0;
        private int[] npcHitCooldown = new int[Main.maxNPCs];
        private float currentSizeMultiplier = 2.0f;
        private Vector2 originalDirection;


        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 50 * 2;
            Projectile.height = 42 * 2;

            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 780;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.aiStyle = -1;

            Projectile.damage = 93;
            Projectile.CritChance = 4;
            Projectile.knockBack = 30f;
            Projectile.ArmorPenetration = 9999;

            for (int i = 0; i < npcHitCooldown.Length; i++)
            {
                npcHitCooldown[i] = 0;
            }
        }

        public override void AI()
        {
            if (Projectile.timeLeft == 780)
            {
                originalDirection = Projectile.velocity;
                originalDirection.Normalize();
            }

            if (currentSizeMultiplier < MaxSize)
            {
                currentSizeMultiplier += SizeGrowthRate;
                if (currentSizeMultiplier > MaxSize)
                    currentSizeMultiplier = MaxSize;

                Projectile.width = (int)(50 * currentSizeMultiplier);
                Projectile.height = (int)(42 * currentSizeMultiplier);
                Projectile.scale = currentSizeMultiplier;

                float speedMultiplier = 1.0f / currentSizeMultiplier;
                float currentSpeed = InitialSpeed * speedMultiplier * 2.0f;

                if (currentSpeed < 0.2f) currentSpeed = 0.2f;

                Projectile.velocity = originalDirection * currentSpeed;
            }

            UpdateDamageCooldowns();

            trailTimer++;
            if (trailTimer >= TrailDustInterval)
            {
                CreateTrailEffect();
                trailTimer = 0;
            }

            Lighting.AddLight(Projectile.Center,
                Color.Lerp(GlowPinkColor, GlowPurpleColor,
                (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f).ToVector3() * GlowIntensity);
        }

        private void UpdateDamageCooldowns()
        {
            for (int i = 0; i < npcHitCooldown.Length; i++)
            {
                if (npcHitCooldown[i] > 0)
                {
                    npcHitCooldown[i]--;
                }
            }
        }

        private bool IsNPCOnCooldown(NPC npc)
        {
            return npcHitCooldown[npc.whoAmI] > 0;
        }

        private void SetNPCCooldown(NPC npc)
        {
            npcHitCooldown[npc.whoAmI] = DamageCooldownFrames;
        }

        private void CreateTrailEffect()
        {
            int particleCount = Main.rand.Next(2, 4) + (int)(currentSizeMultiplier * 0.5f);

            for (int p = 0; p < particleCount; p++)
            {
                Vector2 spawnPosition = Projectile.Center +
                    new Vector2(
                        Main.rand.NextFloat(-Projectile.width * 0.3f, Projectile.width * 0.3f),
                        Main.rand.NextFloat(-Projectile.height * 0.3f, Projectile.height * 0.3f)
                    );

                bool usePink = Main.rand.NextBool();
                Color starColor = usePink ? GlowPinkColor : GlowPurpleColor;
                starColor.A = (byte)(180 + Main.rand.Next(40));

                int dustType = usePink ? DustID.PinkFairy : DustID.PurpleTorch;

                Dust trailDust = Dust.NewDustDirect(
                    spawnPosition,
                    0, 0,
                    dustType,
                    0f, 0f,
                    150,
                    starColor,
                    1.2f + currentSizeMultiplier * 0.1f
                );

                trailDust.noGravity = true;
                trailDust.scale = 0.3f + currentSizeMultiplier * 0.05f + Main.rand.NextFloat(0.3f);

                float slowSpeed = Main.rand.NextFloat(0.1f, 0.3f);
                Vector2 randomDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
                trailDust.velocity = randomDirection * slowSpeed;

                trailDust.customData = TrailParticleLifetime;
                trailDust.alpha = 150;
                trailDust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);

                Lighting.AddLight(trailDust.position, starColor.ToVector3() * 0.4f);

                if (Main.rand.NextFloat() < 0.05f)
                {
                    CreateGiantTrailStar(spawnPosition);
                }
            }
        }

        private void CreateGiantTrailStar(Vector2 position)
        {
            Color giantColor = Color.Lerp(GlowPinkColor, Color.White, 0.3f);
            giantColor.A = 200;

            Dust giantStar = Dust.NewDustDirect(
                position,
                0, 0,
                57,
                0f, 0f,
                180,
                giantColor,
                1.8f
            );

            giantStar.noGravity = true;
            giantStar.scale = 0.5f + currentSizeMultiplier * 0.08f;

            float slowSpeed = Main.rand.NextFloat(0.05f, 0.15f);
            Vector2 randomDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
            giantStar.velocity = randomDirection * slowSpeed;

            giantStar.customData = TrailParticleLifetime + 30;
            giantStar.alpha = 120;
            giantStar.rotation = Main.rand.NextFloat(MathHelper.TwoPi);

            Lighting.AddLight(giantStar.position, giantColor.ToVector3() * 0.6f);
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

                CreateHitEffect(target.Center);

                // +++ 修改：击中音效改为更普通的击中声 +++
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 1.2f, Pitch = -0.1f }, Projectile.Center);

                hit.Damage = 93;
                hit.Knockback = 30f * currentSizeMultiplier * 0.5f;
                hit.Crit = Main.rand.NextFloat(1f) < (Projectile.CritChance / 100f);
            }
        }

        private void CreateHitEffect(Vector2 position)
        {
            int circleCount = (int)(3 + currentSizeMultiplier * 0.5f);

            for (int circle = 0; circle < circleCount; circle++)
            {
                int starCount = (int)(8 + circle * 4 + currentSizeMultiplier);
                float radius = 20f + circle * 15f + currentSizeMultiplier * 5f;

                for (int i = 0; i < starCount; i++)
                {
                    float angle = MathHelper.TwoPi * (i / (float)starCount);
                    Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

                    Color starColor = (i % 2 == 0) ? GlowPinkColor : GlowPurpleColor;

                    Dust hitDust = Dust.NewDustPerfect(
                        position + direction * radius,
                        DustID.PinkFairy,
                        direction * Main.rand.NextFloat(2f, 5f) * (1.0f + currentSizeMultiplier * 0.1f),
                        200,
                        starColor,
                        1.0f + currentSizeMultiplier * 0.05f
                    );

                    hitDust.noGravity = true;
                    hitDust.scale = 0.5f + currentSizeMultiplier * 0.05f + Main.rand.NextFloat(0.3f);
                    hitDust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);

                    hitDust.velocity *= 0.3f;
                    hitDust.alpha = 150;
                }
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (target.friendly || target.type == NPCID.TargetDummy)
                return false;

            if (IsNPCOnCooldown(target))
                return false;

            return base.CanHitNPC(target);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;

            float sizeFactor = currentSizeMultiplier / MaxSize;
            Color baseColor = Color.Lerp(PinkColor, GlowPinkColor, sizeFactor);

            float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.2f + 0.8f;
            Color glowColor = Color.Lerp(GlowPinkColor, GlowPurpleColor,
                (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);

            Color drawColor = Color.Lerp(baseColor, glowColor, 0.8f * pulse);
            drawColor *= ((255 - Projectile.alpha) / 255f);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                drawColor,
                0f,
                new Vector2(texture.Width * 0.5f, texture.Height * 0.5f),
                currentSizeMultiplier,
                SpriteEffects.None,
                0
            );

            Color glowLayerColor = glowColor * 0.6f;
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                glowLayerColor,
                0f,
                new Vector2(texture.Width * 0.5f, texture.Height * 0.5f),
                currentSizeMultiplier * 1.15f,
                SpriteEffects.None,
                0
            );

            if (currentSizeMultiplier > 4.0f)
            {
                Color outerGlow = Color.Lerp(glowColor, Color.White, 0.3f) * 0.3f;
                Main.EntitySpriteDraw(
                    texture,
                    Projectile.Center - Main.screenPosition,
                    null,
                    outerGlow,
                    0f,
                    new Vector2(texture.Width * 0.5f, texture.Height * 0.5f),
                    currentSizeMultiplier * 1.3f,
                    SpriteEffects.None,
                    0
                );
            }

            return false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // +++ 修改：发射音效改为普通法杖射出的弹幕声音 (SoundID.Item9 - "fu~"声) +++
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 1.3f, Pitch = 0.1f }, Projectile.Center);

            for (int i = 0; i < 15; i++)
            {
                CreateSpawnStar();
            }
        }

        private void CreateSpawnStar()
        {
            bool usePink = Main.rand.NextBool();
            Color starColor = usePink ? GlowPinkColor : GlowPurpleColor;

            Dust spawnStar = Dust.NewDustDirect(
                Projectile.Center,
                0, 0,
                usePink ? DustID.PinkFairy : DustID.PurpleTorch,
                Main.rand.NextFloat(-2f, 2f),
                Main.rand.NextFloat(-2f, 2f),
                200,
                starColor,
                1.0f
            );

            spawnStar.noGravity = true;
            spawnStar.scale = 0.5f + Main.rand.NextFloat(0.3f);
            spawnStar.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void Kill(int timeLeft)
        {
            CreateDeathEffect();
            // +++ 修改：消失音效改为SoundID.Item8 - "懂"声 +++
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 1.0f, Pitch = 0.0f }, Projectile.Center);
        }

        private void CreateDeathEffect()
        {
            for (int layer = 0; layer < 3; layer++)
            {
                int starCount = (int)(12 + layer * 6 + currentSizeMultiplier * 2);
                float speed = 1.0f + layer * 0.8f;

                for (int i = 0; i < starCount; i++)
                {
                    float angle = MathHelper.TwoPi * (i / (float)starCount);
                    Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

                    Color rainbowColor = Main.hslToRgb((i / (float)starCount + layer * 0.25f) % 1f, 0.8f, 0.7f);
                    rainbowColor.A = 150;

                    Dust deathStar = Dust.NewDustPerfect(
                        Projectile.Center,
                        57,
                        direction * speed * Main.rand.NextFloat(0.6f, 1.0f),
                        120,
                        rainbowColor,
                        0.7f + currentSizeMultiplier * 0.05f
                    );

                    deathStar.noGravity = true;
                    deathStar.scale = 0.4f + currentSizeMultiplier * 0.05f + Main.rand.NextFloat(0.2f);
                    deathStar.rotation = Main.rand.NextFloat(MathHelper.TwoPi);

                    deathStar.velocity *= 0.4f;
                    deathStar.alpha = 120;
                }
            }
        }
    }
}