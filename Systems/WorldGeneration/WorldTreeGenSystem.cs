using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace Amphoreus.Systems.WorldGeneration
{
    /// <summary>
    /// 世界树：在微光湖正上方地表长出的一棵巨型生命树。
    ///
    ///   - 主干总宽 11 格（内部空心 5 格 + 两侧实心壁各 3 格），背景墙宽 9 格
    ///   - 树干自地表向上生长到太空层后封顶
    ///   - 空心内每 20 格铺一层生命木平台
    ///   - 树枝：每侧 3~4 根，离太空至少 30 格、离地表至少 30 格，
    ///     间隔不小于 20 格，长度 40~60 格，上仰 14°~32°，左右随机；
    ///     树叶沿枝干成串包裹，不只长在末端
    ///   - 树冠：封顶中心由多个不规则圆形叠成
    ///   - 树根：照搬原版红木树（MahoganyTreeBiome.cs:85-89），
    ///     4 根 ShapeRoot，角度按 k/3*2 + 0.57075 均匀铺开，逐渐收细
    ///   - 同一高度只允许有一条树枝或一条树根
    ///   - 地表以下的主干通道继续向下贯穿，直达微光环境边界
    ///
    /// 全程用原版世界生成 DSL（WorldUtils.Gen / Shapes / Actions / Modifiers），
    /// 并挂 SkipTiles 保护宝箱与神庙。
    /// </summary>
    public class WorldTreeGenSystem : ModSystem
    {
        // ---------- 主干规格 ----------
        /// <summary>主干内部空心宽度（格）。</summary>
        private const int HollowWidth = 5;
        /// <summary>单侧实心壁宽度（格）。</summary>
        private const int WallWidth = 3;
        /// <summary>主干总宽（格）= 空心 + 两侧壁。</summary>
        private const int TrunkWidth = HollowWidth + WallWidth * 2;   // 11
        /// <summary>背景墙宽度（格）。</summary>
        private const int BackgroundWallWidth = 9;

        // ---------- 横向树干（树枝）----------
        /// <summary>每侧生成几根横向树干（随机区间）。</summary>
        private const int BranchCountMin = 3;
        private const int BranchCountMax = 4;
        /// <summary>相邻横向树干之间的最小间隔（格）。</summary>
        private const int BranchSpacing = 20;
        /// <summary>横向树干长度下限（格）。</summary>
        private const int BranchLengthMin = 40;
        /// <summary>横向树干长度上限（格）。</summary>
        private const int BranchLengthMax = 61;   // 40~60 含端点
        /// <summary>离地表至少这么高才允许长横向树干（格）。</summary>
        private const int BranchMinHeightAboveSurface = 30;
        /// <summary>离太空至少这么远才允许长横向树干（格）。</summary>
        private const int BranchMinDistanceBelowSpace = 30;
        /// <summary>树枝末端球状树叶的半径下限（格）。</summary>
        private const int BranchLeafMin = 8;
        /// <summary>树枝末端球状树叶的半径上限（格）。</summary>
        private const int BranchLeafMax = 16;

        // ---------- 树根 ----------
        /// <summary>树根总宽（格）。</summary>
        private const int RootWidth = 9;
        /// <summary>树根内部空心宽度（格）。</summary>
        private const int RootHollowWidth = 5;
        /// <summary>树根长度下限（格）。</summary>
        private const int RootLengthMin = 60;
        /// <summary>树根长度上限（格）。</summary>
        private const int RootLengthMax = 81;   // 60~80 含端点
        /// <summary>相邻树根的间隔下限（格）。</summary>
        private const int RootSpacingMin = 50;
        /// <summary>相邻树根的间隔上限（格）。</summary>
        private const int RootSpacingMax = 71;   // 50~70 含端点
        /// <summary>树根离地表至少这么远（格）。</summary>
        private const int RootGapFromSurface = 30;
        /// <summary>树根离微光至少这么远（格）。</summary>
        private const int RootGapFromShimmer = 30;

        // ---------- 下方通道（手动铺，参照 GrowLivingTree_MakePassage）----------
        /// <summary>通道中心半宽（格）。</summary>
        private const int ShaftInnerHalf = 2;
        /// <summary>通道底部距微光环境边界的保留格数。</summary>
        private const int ShimmerGap = 4;
        private const int ShimmerEnvironmentTileThreshold = 600;
        private const int ShimmerProbeRadius = 60;
        /// <summary>空心内平台间隔（格）。</summary>
        private const int PlatformInterval = 20;
        /// <summary>横向隧道间隔下限（格）。</summary>
        private const int TunnelIntervalMin = 30;
        /// <summary>横向隧道间隔上限（格）。</summary>
        private const int TunnelIntervalMax = 50;

        // ---------- 树冠 ----------
        /// <summary>树冠半径（格）。</summary>
        private const int CanopyRadius = 60;
        /// <summary>生命木平台样式（生命木系列）。</summary>
        private const int LivingWoodPlatformStyle = 23;

        // ---------- 状态 ----------
        private static int ShimmerFloorY = -1;
        /// <summary>当前世界树的树干顶 Y（仅用于计算高度比例）。</summary>
        private static int _trunkTopY;
        /// <summary>当前世界树树干顶到地表的跨度（仅用于计算高度比例）。</summary>
        private static int _trunkSpan;

        /// <summary>下一条横向隧道的 Y。</summary>
        private static int _nextTunnelY;

        /// <summary>地表 Y，供 GrowRoots 做「不得高于地表」的硬性约束。</summary>
        private static int _surfaceYForGuard = int.MinValue;

        /// <summary>
        /// 已被树枝或树根占用的高度。同一 Y 只允许有一条树枝或一条树根。
        /// </summary>
        private static readonly HashSet<int> UsedStructureYs = new HashSet<int>();

        private static readonly ushort[] ProtectedTiles =
        {
            TileID.Containers,
            TileID.Containers2,
            TileID.LihzahrdBrick,
            TileID.LihzahrdAltar,
        };

        private static readonly ushort[] ProtectedWalls =
        {
            WallID.LihzahrdBrickUnsafe,
        };

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            int idx = tasks.FindIndex(p => p.Name == "Living Trees");
            if (idx == -1)
            {
                return;
            }

            tasks.Insert(idx + 1, new PassLegacy("Amphoreus: World Tree", WorldTreePass, 2.0));
        }

        private void WorldTreePass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "世界树正在生长……";

            if (GenVars.shimmerPosition.X <= 0.0 || GenVars.shimmerPosition.Y <= 0.0)
            {
                Mod.Logger.Warn("[Amphoreus/WorldTree] 跳过：本世界没有微光湖。");
                return;
            }

            int centerX = (int)GenVars.shimmerPosition.X;
            int shimmerY = (int)GenVars.shimmerPosition.Y;

            if (centerX < 200 || centerX > Main.maxTilesX - 200)
            {
                Mod.Logger.Warn($"[Amphoreus/WorldTree] 跳过：微光湖太靠边缘 centerX={centerX}");
                return;
            }

            ShimmerFloorY = FindShimmerSurfaceY(centerX, shimmerY);

            int surfaceY = FindSurfaceY(centerX, shimmerY);
            if (surfaceY < 0)
            {
                Mod.Logger.Warn($"[Amphoreus/WorldTree] 跳过：找不到地表 centerX={centerX}");
                return;
            }

            // 树干顶：向上生长直到进入太空层
            int spaceY = GetSpaceLayerY();
            int trunkTop = Math.Max(6, Math.Min(surfaceY - 20, spaceY));

            // 通道底：微光环境边界之上
            int envY = FindShimmerEnvironmentStart(centerX, surfaceY, ShimmerFloorY);
            int shaftBottom = (envY > 0 ? envY : ShimmerFloorY) - ShimmerGap;

            progress.Set(0.2);

            // 1) 主干：自封顶行向下贯穿到微光环境边界
            BuildTrunk(centerX, trunkTop, shaftBottom, surfaceY);

            progress.Set(0.6);

            // 2) 顶端封顶
            CapTrunkTop(centerX, trunkTop);

            progress.Set(0.75);

            // 3) 树冠：以封顶中心生成
            BuildCanopy(centerX, trunkTop);

            progress.Set(1.0);

            Mod.Logger.Info($"[Amphoreus/WorldTree] 完成 centerX={centerX} 地表Y={surfaceY} " +
                            $"封顶Y={trunkTop}（太空层Y={spaceY}，树干高{surfaceY - trunkTop}格） 通道底Y={shaftBottom} " +
                            $"微光环境Y={envY} 微光液面Y={ShimmerFloorY} 主干宽={TrunkWidth} 空心宽={HollowWidth} 树冠半径={CanopyRadius}");
        }

        /// <summary>
        /// 太空层起始 Y。判定与游戏一致（SceneMetrics.cs:672）：
        /// 玩家中心 Y ≤ Main.worldSurface * 0.35 即视为太空。
        /// </summary>
        private static int GetSpaceLayerY()
        {
            return (int)(Main.worldSurface * 0.35);
        }

        // ==================== 高度占用 ====================

        /// <summary>标记某个 Y 区间已被树枝或树根占用。</summary>
        private static void MarkStructureSpan(int y, int halfHeight)
        {
            for (int i = -halfHeight; i <= halfHeight; i++)
            {
                UsedStructureYs.Add(y + i);
            }
        }

        /// <summary>该 Y 区间是否与已有树枝/树根冲突。</summary>
        private static bool StructureSpanConflicts(int y, int halfHeight)
        {
            for (int i = -halfHeight; i <= halfHeight; i++)
            {
                if (UsedStructureYs.Contains(y + i))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 若该高度已被占用，则向上/向下错位到最近的空位。
        /// 上下各试探最多 maxShift 格，都找不到就返回 -1（放弃这一条）。
        /// </summary>
        private static int ShiftIfOccupied(int y, int halfHeight, int maxShift)
        {
            if (!StructureSpanConflicts(y, halfHeight))
            {
                return y;
            }

            for (int d = halfHeight + 1; d <= maxShift; d++)
            {
                if (!StructureSpanConflicts(y - d, halfHeight))
                {
                    return y - d;
                }

                if (!StructureSpanConflicts(y + d, halfHeight))
                {
                    return y + d;
                }
            }

            return -1;
        }

        // ==================== 主干 ====================

        /// <summary>
        /// 主干：总宽 11 格（空心 5 + 两侧实心壁各 3），背景墙 9 格。
        /// 自封顶向下贯穿到微光环境边界，空心内每 20 格铺一层平台。
        /// </summary>
        private static void BuildTrunk(int centerX, int trunkTop, int shaftBottom, int surfaceY)
        {
            // 总宽 11 -> center-5 .. center+5
            int outerLeft = centerX - TrunkWidth / 2;
            int outerRight = outerLeft + TrunkWidth - 1;

            // 空心 5 -> center-2 .. center+2
            int innerLeft = outerLeft + WallWidth;
            int innerRight = innerLeft + HollowWidth - 1;

            // 记录树干跨度，供树枝计算高度比例
            _trunkTopY = trunkTop;
            _trunkSpan = Math.Max(1, surfaceY - trunkTop);

            // 供 GrowRoots 做「不得高于地表」的硬性约束
            _surfaceYForGuard = surfaceY;

            // 同一高度只允许一条树枝或一条树根，因此先清空占用表
            UsedStructureYs.Clear();

            // 地表以下通道内容（平台 / 隧道 / 密室）的起点
            _nextTunnelY = surfaceY + WorldGen.genRand.Next(TunnelIntervalMin, TunnelIntervalMax);

            // 横向树干规划（均衡分布、随机高度）：
            //   - 上界离太空至少 BranchMinDistanceBelowSpace 格
            //   - 下界离地表至少 BranchMinHeightAboveSurface 格
            //   - 每侧 BranchCountMin ~ BranchCountMax 根，间隔不小于 BranchSpacing
            int branchUpperLimit = trunkTop + BranchMinDistanceBelowSpace;
            int branchLowerLimit = surfaceY - BranchMinHeightAboveSurface;

            int branchCount = WorldGen.genRand.Next(BranchCountMin, BranchCountMax + 1);

            var branchYs = new List<int>();
            if (branchLowerLimit > branchUpperLimit)
            {
                int span = branchLowerLimit - branchUpperLimit;

                // 均衡分布 + 少量抖动
                for (int i = 1; i <= branchCount; i++)
                {
                    int y = branchUpperLimit + span * i / (branchCount + 1);
                    y += WorldGen.genRand.Next(-6, 7);
                    y = Math.Clamp(y, branchUpperLimit, branchLowerLimit);

                    if (branchYs.Count > 0 && Math.Abs(y - branchYs[branchYs.Count - 1]) < BranchSpacing)
                    {
                        continue;
                    }

                    branchYs.Add(y);
                }
            }

            // 树根规划：地表以下、且离地表与微光各留 RootGap 的中间地带，
            // 每隔 RootSpacingMin~Max 格生成一条（错位分布）
            int rootTop = surfaceY + RootGapFromSurface;
            int rootBottom = shaftBottom - RootGapFromShimmer;

            var rootYs = new List<int>();
            if (rootBottom > rootTop)
            {
                int ry = rootTop + WorldGen.genRand.Next(0, 20);

                while (ry <= rootBottom)
                {
                    rootYs.Add(ry);
                    ry += WorldGen.genRand.Next(RootSpacingMin, RootSpacingMax);
                }
            }

            for (int y = trunkTop; y <= shaftBottom; y++)
            {
                if (y <= 1 || y >= Main.maxTilesY - 2)
                {
                    break;
                }

                // 不越过微光液面
                if (ShimmerFloorY > 0 && y >= ShimmerFloorY)
                {
                    break;
                }

                // ---- 背景墙：以主干中心为准，宽 9 格 ----
                int bgLeft = centerX - BackgroundWallWidth / 2;
                WorldUtils.Gen(new Point(bgLeft, y),
                    new Shapes.Rectangle(BackgroundWallWidth, 1),
                    Actions.Chain(
                        new Modifiers.SkipWalls(ProtectedWalls),
                        new Actions.PlaceWall(WallID.LivingWood)));

                // ---- 两侧实心壁（各 WallWidth 格）----
                foreach (int wallStart in new[] { outerLeft, innerRight + 1 })
                {
                    WorldUtils.Gen(new Point(wallStart, y),
                        new Shapes.Rectangle(WallWidth, 1),
                        Actions.Chain(
                            new Modifiers.SkipTiles(ProtectedTiles),
                            new Modifiers.SkipWalls(ProtectedWalls),
                            new Actions.SetTile(TileID.LivingWood, setSelfFrames: true)));
                }

                // ---- 内部空心 ----
                WorldUtils.Gen(new Point(innerLeft, y),
                    new Shapes.Rectangle(HollowWidth, 1),
                    Actions.Chain(
                        new Modifiers.SkipTiles(TileID.Containers, TileID.Containers2),
                        new Actions.ClearTile()));

                // ---- 平台：地表以上每 PlatformInterval 格一层（树干空心内）----
                if (y < surfaceY && (y - trunkTop) > 0 && (y - trunkTop) % PlatformInterval == 0)
                {
                    WorldUtils.Gen(new Point(innerLeft, y),
                        new Shapes.Rectangle(HollowWidth, 1),
                        Actions.Chain(
                            new Modifiers.SkipTiles(TileID.Containers, TileID.Containers2),
                            new Actions.PlaceTile(TileID.Platforms, LivingWoodPlatformStyle)));
                }

                // ---- 横向树干：命中预算高度就长，与已有枝干/树根错位 ----
                if (branchYs.Contains(y))
                {
                    int shifted = ShiftIfOccupied(y, 6, 12);
                    if (shifted > 0)
                    {
                        GrowHorizontalBranch(centerX, shifted, outerLeft, outerRight);
                        MarkStructureSpan(shifted, 6);
                    }
                }

                // ---- 树根：命中预算高度就长，与已有枝干/树根错位 ----
                if (rootYs.Contains(y))
                {
                    int shifted = ShiftIfOccupied(y, 6, 12);
                    if (shifted > 0)
                    {
                        GrowRoots(centerX, shifted, outerLeft, outerRight);
                        MarkStructureSpan(shifted, 6);
                    }
                }

                // ---- 地表以下：手动铺通道内容（平台 / 横向隧道 / 密室）----
                if (y > surfaceY)
                {
                    BuildLowerPassageContent(centerX, y, innerLeft, surfaceY, shaftBottom);
                }
            }
        }

        // ==================== 地表以下的通道内容 ====================

        /// <summary>
        /// 地表以下的通道内容：
        ///   - 每 <see cref="PlatformInterval"/> 行铺一层平台
        ///   - 每 <see cref="TunnelIntervalMin"/>~<see cref="TunnelIntervalMax"/> 行开一条横向隧道
        /// </summary>
        private static void BuildLowerPassageContent(int centerX, int y, int innerLeft, int surfaceY, int shaftBottom)
        {
            // 1) 平台：每 20 行一层
            if ((y - surfaceY) % PlatformInterval == 0)
            {
                WorldUtils.Gen(new Point(innerLeft, y),
                    new Shapes.Rectangle(HollowWidth, 1),
                    Actions.Chain(
                        new Modifiers.SkipTiles(TileID.Containers, TileID.Containers2),
                        new Actions.PlaceTile(TileID.Platforms, LivingWoodPlatformStyle)));
            }

            // 2) 横向隧道：每 30~50 行一条
            if (y >= _nextTunnelY && y < shaftBottom)
            {
                BuildHorizontalTunnel(centerX, y);
                _nextTunnelY = y + WorldGen.genRand.Next(TunnelIntervalMin, TunnelIntervalMax);
            }
        }

        /// <summary>横向隧道：从通道向左右各伸出一条带生命木墙的水平通道。</summary>
        private static void BuildHorizontalTunnel(int centerX, int y)
        {
            int len = WorldGen.genRand.Next(20, 45);

            foreach (int dir in new[] { -1, 1 })
            {
                int startX = dir < 0 ? centerX - ShaftInnerHalf - 1 : centerX + ShaftInnerHalf;
                int left = dir < 0 ? startX - len : startX;
                int width = len;

                if (left <= 2 || left + width >= Main.maxTilesX - 2)
                {
                    continue;
                }

                // 掏空 + 铺墙
                WorldUtils.Gen(new Point(left, y),
                    new Shapes.Rectangle(width, 3),
                    Actions.Chain(
                        new Modifiers.SkipTiles(TileID.Containers, TileID.Containers2),
                        new Actions.ClearTile(),
                        new Actions.PlaceWall(WallID.LivingWood)));

                // 隧道地板
                WorldUtils.Gen(new Point(left, y + 3),
                    new Shapes.Rectangle(width, 1),
                    Actions.Chain(
                        new Modifiers.SkipTiles(ProtectedTiles),
                        new Actions.SetTile(TileID.LivingWood, setSelfFrames: true)));
            }
        }


        /// <summary>顶端封顶：把空心那一行填实。</summary>
        private static void CapTrunkTop(int centerX, int trunkTop)
        {
            int left = centerX - TrunkWidth / 2;

            if (trunkTop <= 1)
            {
                return;
            }

            WorldUtils.Gen(new Point(left, trunkTop),
                new Shapes.Rectangle(TrunkWidth, 1),
                Actions.Chain(
                    new Modifiers.SkipTiles(ProtectedTiles),
                    new Actions.SetTile(TileID.LivingWood, setSelfFrames: true)));
        }

        // ==================== 树枝 ====================

        /// <summary>
        /// 树枝：在主干某一高度向一侧伸出一根。
        /// 左右由随机决定（因此两侧数量不必对称）；角度取上仰区间；
        /// 树叶沿枝干成串包裹，不只长在末端。
        /// </summary>
        /// <summary>
        /// 树枝：在主干某一高度向<b>一侧</b>伸出一根<b>完全水平</b>的枝干，
        /// 末端挂一个球状树叶团。
        /// </summary>
        private static void GrowHorizontalBranch(int centerX, int y, int outerLeft, int outerRight)
        {
            // 左右随机，使两侧数量不对称
            int dir = WorldGen.genRand.Next(2) == 0 ? -1 : 1;

            int len = WorldGen.genRand.Next(BranchLengthMin, BranchLengthMax);
            int startX = dir < 0 ? outerLeft : outerRight;
            int endX = startX + dir * (len - 1);

            int left = Math.Min(startX, endX);
            int width = Math.Abs(endX - startX) + 1;

            if (left <= 2 || left + width >= Main.maxTilesX - 2)
            {
                return;
            }

            // 水平枝干（与树干等高，Y 不变）
            WorldUtils.Gen(new Point(left, y),
                new Shapes.Rectangle(width, 2),
                Actions.Chain(
                    new Modifiers.SkipTiles(ProtectedTiles),
                    new Modifiers.SkipWalls(ProtectedWalls),
                    new Actions.SetTile(TileID.LivingWood, setSelfFrames: true)));

            // 末端球状树叶
            int leafRadius = WorldGen.genRand.Next(BranchLeafMin, BranchLeafMax);
            PlaceCanopyBlob(endX + dir * leafRadius / 2, y, leafRadius, 4, 0.4f);
        }

        /// <summary>
        /// 树枝叶：沿枝干生成<b>叶片形状</b>的树叶块。
        ///
        /// 叶片轮廓用 sin 曲线：宽度 = 最大宽度 × sin(π·τ)，
        /// τ=0 时宽度为 0（叶尖）、τ=0.5 时最宽（叶身）、τ=1 时为 0（叶尖），
        /// 于是每一片叶子都是两端尖、中间鼓的梭形，而不是圆球。
        /// 沿整条树枝连续铺开，叶片连成一体包裹枝干。
        /// </summary>
        private static void DecorateBranchWithLeaves(Point start, Point end)
        {
            int dx = end.X - start.X;
            int dy = end.Y - start.Y;
            double length = Math.Sqrt((double)dx * dx + (double)dy * dy);

            if (length < 3.0)
            {
                return;
            }

            // 叶片最大半宽（格）
            int maxHalfWidth = WorldGen.genRand.Next(4, 7);

            // 叶片长度（沿枝干方向占多少格）
            double leafLength = WorldGen.genRand.Next(10, 18);

            // 沿枝干方向逐格推进
            for (double t = 0.0; t <= length; t += 0.8)
            {
                double u = t / length;

                // 该处沿枝干的单位向量 + 法向量
                double nx = dx / length;
                double ny = dy / length;

                int cx = start.X + (int)Math.Round(nx * t);
                int cy = start.Y + (int)Math.Round(ny * t);

                // 叶片内的相位 τ ∈ [0,1]，做两端尖的梭形
                double tau = (t % leafLength) / leafLength;

                // 包络：整条枝干两端也收尖
                double taper = Math.Sin(Math.PI * u) * 0.6 + 0.4;

                double halfWidth = maxHalfWidth * Math.Sin(Math.PI * tau) * taper;

                if (halfWidth < 0.5)
                {
                    continue;
                }

                // 沿法向量向两侧铺叶片（垂直方向，厚度方向）
                for (int o = -(int)halfWidth; o <= (int)halfWidth; o++)
                {
                    int lx = cx + (int)Math.Round(-ny * o);
                    int ly = cy + (int)Math.Round(nx * o);

                    if (!WorldGen.InWorld(lx, ly, 20))
                    {
                        continue;
                    }

                    WorldUtils.Gen(new Point(lx, ly),
                        new Shapes.Rectangle(1, 1),
                        Actions.Chain(
                            new Modifiers.SkipTiles(TileID.Containers, TileID.Containers2),
                            new Modifiers.SkipWalls(WallID.LihzahrdBrickUnsafe),
                            new Actions.SetTile(TileID.LeafBlock, setSelfFrames: true)));
                }
            }
        }

        // ==================== 树根 ====================

        /// <summary>
        /// 树根：从主干<b>两侧</b>各伸出一条水平树根。
        /// 根体总宽 9 格、内部空心 5 格。
        ///
        /// 封闭规则：
        ///   - 上沿、下沿各留 2 层实心（9 - 5 = 4，上下各 2）
        ///   - 远离主干的那一端封实心
        ///   - <b>靠主干的一端保持敞开</b>，与主干空心连通
        /// </summary>
        private static void GrowRoots(int centerX, int y, int outerLeft, int outerRight)
        {
            // 硬性约束：树根绝不允许出现在地表以上
            if (y - RootWidth / 2 <= _surfaceYForGuard)
            {
                return;
            }

            int len = WorldGen.genRand.Next(RootLengthMin, RootLengthMax);

            // 根体总宽 9 -> y-4 .. y+4
            int solidTop = y - RootWidth / 2;

            // 上下封边厚度，(9-5)/2 = 2
            int edge = (RootWidth - RootHollowWidth) / 2;
            int hollowTop = solidTop + edge;
            int hollowHeight = RootHollowWidth;

            int innerLeft = centerX - HollowWidth / 2;
            int innerRight = innerLeft + HollowWidth - 1;

            foreach (int dir in new[] { -1, 1 })
            {
                // 起点贴主干外壁；主干侧敞口，远端封闭
                int startX = dir < 0 ? outerLeft : outerRight;
                int endX = startX + dir * (len - 1);

                int solidLeft = Math.Min(startX, endX);
                int solidWidth = Math.Abs(endX - startX) + 1;

                if (solidLeft <= 2 || solidLeft + solidWidth >= Main.maxTilesX - 2)
                {
                    continue;
                }

                // 1) 根体：宽 9 × 长 len 的实心生命木（外壳，天然形成上下封边）
                WorldUtils.Gen(new Point(solidLeft, solidTop),
                    new Shapes.Rectangle(solidWidth, RootWidth),
                    Actions.Chain(
                        new Modifiers.SkipTiles(ProtectedTiles),
                        new Modifiers.SkipWalls(ProtectedWalls),
                        new Actions.SetTile(TileID.LivingWood, setSelfFrames: true)));

                // 2) 内部空心：从「贴着主干的那一端」开始，直到远端留 seal 格封口
                int seal = edge + 1;                      // 远端封口厚度

                // 空心在根体内部沿 X 的区间（不含远端封口）
                int hollowLeft = dir < 0
                    ? solidLeft + seal                     // 左侧根：远端在左，封口在左
                    : solidLeft;                           // 右侧根：远端在右，封口在右

                int hollowSpan = solidWidth - seal;
                if (hollowSpan < 4)
                {
                    continue;
                }

                // 靠主干那一端：延伸进主干空心，实现联通
                int linkLeft = dir < 0 ? startX : innerLeft;
                int linkRight = dir < 0 ? innerRight : startX;
                int linkWidth = linkRight - linkLeft + 1;

                int carveLeft = Math.Min(hollowLeft, linkLeft);
                int carveRight = Math.Max(hollowLeft + hollowSpan - 1, linkRight);
                int carveWidth = carveRight - carveLeft + 1;

                if (carveWidth <= 0)
                {
                    continue;
                }

                WorldUtils.Gen(new Point(carveLeft, hollowTop),
                    new Shapes.Rectangle(carveWidth, hollowHeight),
                    Actions.Chain(
                        new Modifiers.SkipTiles(TileID.Containers, TileID.Containers2),
                        new Actions.ClearTile(),
                        new Actions.PlaceWall(WallID.LivingWood)));
            }
        }

        // ==================== 树冠 ====================

        /// <summary>
        /// 树冠：以封顶中心为核心，由多个不规则圆形叠在一起构成。
        /// </summary>
        private static void BuildCanopy(int centerX, int trunkTop)
        {
            if (!WorldGen.InWorld(centerX, trunkTop, CanopyRadius + 20))
            {
                return;
            }

            // 1) 主冠核：一个较大的不规则圆
            PlaceCanopyBlob(centerX, trunkTop, CanopyRadius, 5, 0.35f);

            // 2) 环绕叠加 12 个大小不一的不规则圆，形成蓬松冠幅
            const int blobs = 12;
            for (int i = 0; i < blobs; i++)
            {
                double angle = (i / (double)blobs) * Math.PI * 2.0
                               + WorldGen.genRand.NextFloat(-0.3f, 0.3f);

                // 越靠外的圆越小，整体呈球形
                float distFactor = 0.35f + 0.55f * WorldGen.genRand.NextFloat();
                int cx = centerX + (int)(Math.Cos(angle) * CanopyRadius * distFactor);
                int cy = trunkTop + (int)(Math.Sin(angle) * CanopyRadius * distFactor * 0.75);

                int radius = (int)(CanopyRadius * WorldGen.genRand.NextFloat(0.30f, 0.62f));

                PlaceCanopyBlob(cx, cy, radius, 5, 0.4f);
            }

            // 3) 冠顶再压两个小圆，收出圆头
            PlaceCanopyBlob(centerX, trunkTop - CanopyRadius / 3, CanopyRadius / 3, 5, 0.45f);
            PlaceCanopyBlob(centerX + WorldGen.genRand.Next(-14, 15),
                            trunkTop + WorldGen.genRand.Next(-6, 7),
                            CanopyRadius / 2, 5, 0.4f);
        }

        /// <summary>放一团树冠叶球（不规则圆）。</summary>
        private static void PlaceCanopyBlob(int cx, int cy, int radius, int blotchCount, float blotchStrength)
        {
            if (radius < 3 || !WorldGen.InWorld(cx, cy, radius + 20))
            {
                return;
            }

            WorldUtils.Gen(new Point(cx, cy),
                new Shapes.Circle(radius),
                Actions.Chain(
                    new Modifiers.Blotches(blotchCount, blotchStrength),
                    new Modifiers.SkipTiles(TileID.Containers, TileID.Containers2),
                    new Modifiers.SkipWalls(WallID.LihzahrdBrickUnsafe),
                    new Actions.SetTile(TileID.LeafBlock, setSelfFrames: true)));
        }

        // ==================== 定位与微光检测 ====================

        /// <summary>找地表：返回地面之上那一格的 Y。</summary>
        private static int FindSurfaceY(int x, int limitY)
        {
            for (int y = 40; y < limitY - 40 && y < Main.maxTilesY - 2; y++)
            {
                Tile tile = Framing.GetTileSafely(x, y);
                Tile below = Framing.GetTileSafely(x, y + 1);

                bool hereEmpty = tile == null || !tile.HasUnactuatedTile;
                bool belowSolid = below != null && below.HasUnactuatedTile && Main.tileSolid[below.TileType];

                if (hereEmpty && belowSolid)
                {
                    return y;
                }
            }
            return -1;
        }

        /// <summary>找微光液面上缘。</summary>
        private static int FindShimmerSurfaceY(int centerX, int shimmerY)
        {
            int x = Math.Clamp(centerX, 5, Main.maxTilesX - 6);

            for (int y = shimmerY; y > shimmerY - 120 && y > 5; y--)
            {
                Tile tile = Framing.GetTileSafely(x, y);
                if (tile != null && tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Shimmer)
                {
                    return y;
                }
            }

            return shimmerY - 2;
        }

        /// <summary>统计半径内的微光液体格数。</summary>
        private static int CountShimmerTiles(int cx, int cy, int radius)
        {
            int count = 0;

            int left = Math.Max(1, cx - radius);
            int right = Math.Min(Main.maxTilesX - 2, cx + radius);
            int top = Math.Max(1, cy - radius);
            int bottom = Math.Min(Main.maxTilesY - 2, cy + radius);

            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (tile != null && tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Shimmer)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>向下探测微光环境：60 格半径内 ≥600 格微光液体即认定到达。</summary>
        private static int FindShimmerEnvironmentStart(int x, int fromY, int limitY)
        {
            const int step = 6;

            for (int y = fromY; y <= limitY; y += step)
            {
                if (CountShimmerTiles(x, y, ShimmerProbeRadius) >= ShimmerEnvironmentTileThreshold)
                {
                    return y;
                }
            }

            return -1;
        }
    }
}
