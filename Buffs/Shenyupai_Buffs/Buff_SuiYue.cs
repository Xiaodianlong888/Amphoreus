using Amphoreus.Players.Shenyupai_Players;
using Terraria;
using Terraria.ModLoader;

namespace Amphoreus.Buffs.Shenyupai_Buffs
{
    public class Buff_SuiYue : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var cardPlayer = player.GetModPlayer<Shenyupai_Player>();

            if (cardPlayer.CurrentCard == null ||
                cardPlayer.CurrentCard.Name != "永夜之帷" ||
                !cardPlayer.HasActiveBuff)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            player.buffTime[buffIndex] = cardPlayer.BuffTimeLeft * 60;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            var player = Main.LocalPlayer;
            var cardPlayer = player.GetModPlayer<Shenyupai_Player>();

            if (cardPlayer.CurrentCard != null && cardPlayer.CurrentCard.Name == "永夜之帷")
            {
                // 使用本地化描述并添加剩余时间
                tip = this.GetLocalization("Description").Value + $"\n剩余时间: {cardPlayer.BuffTimeLeftFormatted}";
            }
        }
    }
}