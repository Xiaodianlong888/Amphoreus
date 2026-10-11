using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace Amphoreus.Biomes
{
    public class BlackDomainBiome : ModBiome
    {
        public override void SetStaticDefaults()
        {
            //DisplayName.SetDefault("黑域");
        }

        // 判断玩家是否处于黑域中（统计周围黑域方块数量）
        public override bool IsBiomeActive(Player player)
        {
            int tileCount = 0;
            int scanArea = 50;
            int playerX = (int)(player.Center.X / 16f);
            int playerY = (int)(player.Center.Y / 16f);

            for (int i = playerX - scanArea; i < playerX + scanArea; i++)
            {
                for (int j = playerY - scanArea; j < playerY + scanArea; j++)
                {
                    if (i < 0 || i >= Main.maxTilesX || j < 0 || j >= Main.maxTilesY)
                        continue;
                    Tile tile = Main.tile[i, j];
                    if (tile.HasTile && tile.TileType == ModContent.TileType<Tiles.World_Tiles.BlackDomainTile>())
                        tileCount++;
                }
            }
            return tileCount >= 100;
        }

        // 背景颜色（可选项，设为深色）
        public override Color? BackgroundColor => new Color(10, 10, 10);

        // 优先级较高，确保覆盖其他生物群系
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;
    }
}