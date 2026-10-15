using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Items.XiLian_Items
{
    /// <summary>
    /// C8 武器物品类 - 直接使用原版彩虹魔杖弹幕
    /// </summary>
    public class C8 : ModItem
    {
        public override string Texture => "Amphoreus/Items/XiLian_Items/C8";

        public override void SetDefaults()
        {
            Item.SetWeaponValues(33550336, 1, 8); // 伤害，暴击率1%（仅显示），击退
            Item.DamageType = DamageClass.Magic;
            Item.mana = 1;
            Item.value = -1;          // 无价
            Item.width = 50;
            Item.height = 50;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.UseSound = SoundID.Item9;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ProjectileID.RainbowRodBullet; // 原版彩虹魔杖弹幕
            Item.shootSpeed = 13f;
            Item.noUseGraphic = false;
        }

        public override Vector2? HoldoutOffset() => Vector2.Zero;

        public override void HoldItem(Player player) => player.itemLocation = player.Center;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 shootPos = player.Center;
            int proj = Projectile.NewProjectile(source, shootPos, velocity, ProjectileID.RainbowRodBullet, 33550336, knockback, player.whoAmI);
            Projectile p = Main.projectile[proj];
            p.penetrate = -1;                // 无限穿透
            p.timeLeft = 1111 * 60;          // 1111秒
            p.tileCollide = false;            // 可穿透物块
            p.extraUpdates = 1;               // 更平滑

            // 标记为C8发射的弹幕
            p.GetGlobalProjectile<C8GlobalProjectile>().IsC8Projectile = true;
            return false;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            Vector2 mouse = Main.MouseWorld;
            Vector2 dir = mouse - player.Center;
            dir.Normalize();
            player.itemRotation = dir.ToRotation();
            if (player.direction < 0) player.itemRotation += MathHelper.Pi;
            player.itemLocation = player.Center;
        }
    }

    /// <summary>
    /// 全局弹幕类：处理伤害固定、护甲穿透、环境净化、追踪敌对弹幕
    /// </summary>
    public class C8GlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool IsC8Projectile = false;
        private int purifyCooldown = 0;

        // 允许切割/转化物块
        public override bool? CanCutTiles(Projectile projectile) => IsC8Projectile ? true : null;

        // 净化物块与墙壁（环境改造枪式区域净化）
        public override void CutTiles(Projectile projectile)
        {
            if (!IsC8Projectile) return;
            if (++purifyCooldown < 3) return; // 每3帧一次
            purifyCooldown = 0;

            int tileRange = 6;
            Vector2 center = projectile.Center;
            int left = (int)(center.X / 16f - tileRange);
            int right = (int)(center.X / 16f + tileRange);
            int top = (int)(center.Y / 16f - tileRange);
            int bottom = (int)(center.Y / 16f + tileRange);

            left = Utils.Clamp(left, 2, Main.maxTilesX - 2);
            right = Utils.Clamp(right, 2, Main.maxTilesX - 2);
            top = Utils.Clamp(top, 2, Main.maxTilesY - 2);
            bottom = Utils.Clamp(bottom, 2, Main.maxTilesY - 2);

            for (int i = left; i <= right; i++)
            {
                for (int j = top; j <= bottom; j++)
                {
                    Tile tile = Main.tile[i, j];
                    if (tile.HasTile)
                        WorldGen.Convert(i, j, BiomeConversionID.PurificationPowder, 0);

                    if (tile.WallType > 0)
                    {
                        // 精准转化邪恶墙壁（仅转化特定类型，其他墙壁不变）
                        switch (tile.WallType)
                        {
                            case WallID.EbonstoneUnsafe:
                            case WallID.CrimstoneUnsafe:
                            case WallID.PearlstoneBrickUnsafe: // 修正为 PearlstoneBrickUnsafe
                                tile.WallType = WallID.Stone;
                                WorldGen.SquareWallFrame(i, j);
                                NetMessage.SendTileSquare(-1, i, j, 1);
                                break;
                            case WallID.CorruptSandstone:
                            case WallID.CrimsonSandstone:
                            case WallID.HallowSandstone:
                                tile.WallType = WallID.Sandstone;
                                WorldGen.SquareWallFrame(i, j);
                                NetMessage.SendTileSquare(-1, i, j, 1);
                                break;
                            case WallID.CorruptHardenedSand:
                            case WallID.CrimsonHardenedSand:
                            case WallID.HallowHardenedSand:
                                tile.WallType = WallID.HardenedSand;
                                WorldGen.SquareWallFrame(i, j);
                                NetMessage.SendTileSquare(-1, i, j, 1);
                                break;
                        }
                    }
                }
            }
        }

        // 固定伤害与护甲穿透
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (IsC8Projectile)
            {
                modifiers.SourceDamage.Base = 33550336f;
                modifiers.FinalDamage.Base = 33550336f;
                modifiers.ArmorPenetration += 33550336f;
                // 不强制暴击，保留武器显示的1%暴击率（但实际暴击时伤害也是固定值，不影响）
            }
        }

        // 保持自定义属性（防止原版AI重置）
        public override bool PreAI(Projectile projectile)
        {
            if (IsC8Projectile)
            {
                if (projectile.penetrate != -1) projectile.penetrate = -1;
                projectile.tileCollide = false;
            }
            return true;
        }

        // 在 PostAI 中实现追踪并消除敌对弹幕
        public override void PostAI(Projectile projectile)
        {
            if (!IsC8Projectile) return;

            // 追踪敌对弹幕
            TrackHostileProjectiles(projectile);

            // 消除检测（每2帧一次）
            if (projectile.localAI[1]++ > 1)
            {
                projectile.localAI[1] = 0;
                EliminateHostileProjectiles(projectile);
            }
        }

        private void TrackHostileProjectiles(Projectile projectile)
        {
            float maxDetectRange = 600f;
            float turnSpeed = 0.1f;          // 转向平滑度
            Projectile target = null;
            float minDistSq = maxDetectRange * maxDetectRange;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active) continue;
                if (other.whoAmI == projectile.whoAmI) continue;
                if (!other.hostile) continue; // 只追踪敌对弹幕

                float distSq = Vector2.DistanceSquared(projectile.Center, other.Center);
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    target = other;
                }
            }

            if (target != null)
            {
                Vector2 dir = target.Center - projectile.Center;
                dir.Normalize();
                float currentSpeed = projectile.velocity.Length();
                Vector2 desiredVelocity = dir * currentSpeed;
                projectile.velocity = Vector2.Lerp(projectile.velocity, desiredVelocity, turnSpeed);

                // 限制最大速度（可选）
                float maxSpeed = 20f;
                if (projectile.velocity.Length() > maxSpeed)
                    projectile.velocity = Vector2.Normalize(projectile.velocity) * maxSpeed;
            }
        }

        private void EliminateHostileProjectiles(Projectile projectile)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active) continue;
                if (other.whoAmI == projectile.whoAmI) continue;
                if (!other.hostile) continue; // 只消除敌对弹幕

                // 碰撞阈值
                float collisionThreshold = (projectile.width + other.width) * 0.4f;
                if (Vector2.Distance(projectile.Center, other.Center) > collisionThreshold)
                    continue;

                other.active = false;
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, other.whoAmI);
                }

                // 可选：播放消除音效或粒子
                // Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4, other.Center);
            }
        }
    }
}