using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Amphoreus.Players.XiLian_Players.XiLian;

namespace Amphoreus.Systems
{
    /// <summary>
    /// 花茶的攻击附加效果：火焰花使目标着火，寒颤棘使目标减速。
    /// </summary>
    public class HerbTeaAttackEffects : GlobalNPC
    {
        private const int DebuffDuration = 180; // 3 秒

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            ApplyHerbTeaDebuffs(npc, player);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            {
                return;
            }

            Player player = Main.player[projectile.owner];
            if (player != null && player.active)
            {
                ApplyHerbTeaDebuffs(npc, player);
            }
        }

        private static void ApplyHerbTeaDebuffs(NPC npc, Player player)
        {
            XiLianHerbTeaPlayer teaPlayer = player.GetModPlayer<XiLianHerbTeaPlayer>();
            if (teaPlayer == null)
            {
                return;
            }

            // 火焰花：着火
            if (teaPlayer.fireblossomActive)
            {
                npc.AddBuff(BuffID.OnFire, DebuffDuration);
            }

            // 寒颤棘：减速
            if (teaPlayer.chillthornActive)
            {
                npc.AddBuff(BuffID.Slow, DebuffDuration);
            }
        }
    }
}
