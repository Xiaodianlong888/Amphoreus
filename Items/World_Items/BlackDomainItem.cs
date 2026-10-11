using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace Amphoreus.Items.World_Items
{
    public class BlackDomainItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            //DisplayName.SetDefault("黑域");
            //Tooltip.SetDefault("具有感染任何物块的力量");
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<Tiles.World_Tiles.BlackDomainTile>();
        }
    }
}