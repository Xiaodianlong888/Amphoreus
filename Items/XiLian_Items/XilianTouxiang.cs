using Amphoreus.Tiles.XiLian_Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Items.XiLian_Items
{
    public class XiLianTouxiang : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 99;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<XilianHuakuang>();
            Item.placeStyle = 0;
        }
    }
}