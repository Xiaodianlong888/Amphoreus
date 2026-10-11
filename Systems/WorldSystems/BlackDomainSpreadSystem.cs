using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Amphoreus.Systems.WorldSystems
{
    public class BlackDomainSpreadSystem : ModSystem
    {
        internal static List<Point> _blackDomainPositions = new List<Point>();

        private const int SpreadAttemptsPerTick = 5;

        // 供 GlobalTile 调用的添加方法
        internal static void AddBlackDomainPosition(int x, int y)
        {
            Point newPos = new Point(x, y);
            if (!_blackDomainPositions.Contains(newPos))
                _blackDomainPositions.Add(newPos);
        }

        // 供 GlobalTile 调用的移除方法
        internal static void RemoveBlackDomainPosition(int x, int y)
        {
            _blackDomainPositions.RemoveAll(p => p.X == x && p.Y == y);
        }

        // 世界加载时扫描全图，初始化列表
        public override void OnWorldLoad()
        {
            _blackDomainPositions.Clear();
            for (int x = 0; x < Main.maxTilesX; x++)
            {
                for (int y = 0; y < Main.maxTilesY; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile && tile.TileType == ModContent.TileType<Tiles.World_Tiles.BlackDomainTile>())
                        _blackDomainPositions.Add(new Point(x, y));
                }
            }
        }

        public override void PreUpdateWorld()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (_blackDomainPositions.Count == 0)
                return;

            for (int attempt = 0; attempt < SpreadAttemptsPerTick; attempt++)
            {
                TrySpreadFromRandomSource();
            }
        }

        private void TrySpreadFromRandomSource()
        {
            Point source = _blackDomainPositions[WorldGen.genRand.Next(_blackDomainPositions.Count)];
            int sourceX = source.X;
            int sourceY = source.Y;

            int targetX = sourceX + WorldGen.genRand.Next(-1, 2);
            int targetY = sourceY + WorldGen.genRand.Next(-1, 2);

            if (targetX < 0 || targetX >= Main.maxTilesX || targetY < 0 || targetY >= Main.maxTilesY)
                return;

            Tile targetTile = Main.tile[targetX, targetY];

            // 感染所有有图格的方块（除了黑域本身）
            if (targetTile.HasTile &&
                targetTile.TileType != ModContent.TileType<Tiles.World_Tiles.BlackDomainTile>())
            {
                targetTile.TileType = (ushort)ModContent.TileType<Tiles.World_Tiles.BlackDomainTile>();
                targetTile.TileFrameX = 0;
                targetTile.TileFrameY = 0;

                _blackDomainPositions.Add(new Point(targetX, targetY));

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendTileSquare(-1, targetX, targetY, 1);
            }
        }
    }
}