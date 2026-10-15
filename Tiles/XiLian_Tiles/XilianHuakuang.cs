using Amphoreus.Items.XiLian_Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Amphoreus.Tiles.XiLian_Tiles
{
    public class XilianHuakuang : ModTile
    {
        // 缓存贴图，避免每帧重复加载
        private static Texture2D _portraitTexture;

        public override void SetStaticDefaults()
        {
            // 基本设置
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.FramesOnKillWall[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            // 加载自定义贴图（98x100 头像框）
            _portraitTexture = ModContent.Request<Texture2D>("Amphoreus/Items/XiLian_Items/XiLianTouxiang").Value;

            // 配置瓦片数据：7 格宽 × 7 格高
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall); // 借用模板
            TileObjectData.newTile.Width = 7;
            TileObjectData.newTile.Height = 7;
            // 每格高度均为 16 像素（坐标高度数组长度 = Height）
            TileObjectData.newTile.CoordinateHeights = new int[7];
            for (int k = 0; k < 7; k++)
                TileObjectData.newTile.CoordinateHeights[k] = 16;
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.Origin = new Point16(0, 0); // 左上角为放置原点
            TileObjectData.newTile.AnchorWall = true;          // 必须挂在墙上
            TileObjectData.addTile(Type);

            // 地图显示名称与颜色
        }

        // 摧毁时掉落物品（掉落区域为 7×7 格，即 112×112 像素）
        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 112, 112,
                ModContent.ItemType<XiLianTouxiang>());
        }

        // 完全自定义绘制：只在画作左上角绘制一次贴图
        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            // 仅当此格子是画作区域的左上角（TileFrameX == 0 && TileFrameY == 0）时绘制
            if (tile.TileFrameX == 0 && tile.TileFrameY == 0)
            {
                // 计算屏幕坐标：瓦片左上角
                Vector2 position = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y);
                // 按原始尺寸 98×100 绘制（无缩放）
                spriteBatch.Draw(_portraitTexture, position, Color.White);
            }
        }
    }
}