using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace Amphoreus.Tiles.World_Tiles
{
    public class BlackDomainTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            MinPick = 0;
            MineResist = 1f;
            HitSound = SoundID.Tink;
            DustType = DustID.Blood; // 暗红色粒子

            AddMapEntry(new Color(30, 10, 40), CreateMapEntryName());
        }
    }
}
