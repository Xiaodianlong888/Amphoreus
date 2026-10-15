using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Amphoreus.Systems
{
    /// <summary>
    /// 太阳花花茶的刷怪速率修正：附近有该增益时刷怪更慢、上限更低。
    /// </summary>
    public class HerbTeaGlobalNPC : GlobalNPC
    {
        private const float EffectRadius = 800f;

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            bool hasSunflowerBuff = false;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player pl = Main.player[i];
                if (!pl.active || pl.dead)
                {
                    continue;
                }

                if (Vector2.Distance(pl.Center, player.Center) >= EffectRadius)
                {
                    continue;
                }

                if (pl.HasBuff(ModContent.BuffType<Buffs.XiLian_Buffs.HerbTea.SunflowerHerbTeaBuff>()))
                {
                    hasSunflowerBuff = true;
                    break;
                }
            }

            if (hasSunflowerBuff)
            {
                spawnRate *= 2;                          // 值越大生成越慢
                maxSpawns = System.Math.Max(1, maxSpawns / 2);
            }
        }
    }
}
