using Terraria;
using Terraria.ModLoader;

namespace Amphoreus.Buffs.SFeir_Buffs
{

    public class SpeedLieBuff : ModBuff
    {

        public override string Texture => "Amphoreus/Buffs/SFeir_Buffs/SpeedLieBuff";
        public override void SetStaticDefaults()
        {
            //DisplayName.SetDefault("速度谎言");
            //Description.SetDefault("你感觉移动更快了...但好像不对劲");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.moveSpeed *= 1.5f; // 增加移动速度
        }
    }
}
