using Terraria;
using Terraria.ModLoader;
using Amphoreus.Players.XiLian_Players.XiLian;

namespace Amphoreus.Buffs.XiLian_Buffs.HerbTea
{
    /// <summary>花茶增益：向日葵花茶</summary>
    public class SunflowerHerbTeaBuff : ModBuff
    {
        // 显示名与描述走本地化键 Mods.Amphoreus.Buffs.SunflowerHerbTeaBuff.DisplayName / .Description
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<XiLianHerbTeaPlayer>().sunflowerActive = true;
        }
    }

    /// <summary>花茶增益：月光草花茶</summary>
    public class MoonflowerHerbTeaBuff : ModBuff
    {
        // 显示名与描述走本地化键 Mods.Amphoreus.Buffs.MoonflowerHerbTeaBuff.DisplayName / .Description
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<XiLianHerbTeaPlayer>().moonflowerActive = true;
        }
    }

    /// <summary>花茶增益：闪耀根花茶</summary>
    public class ShiningRootHerbTeaBuff : ModBuff
    {
        // 显示名与描述走本地化键 Mods.Amphoreus.Buffs.ShiningRootHerbTeaBuff.DisplayName / .Description
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<XiLianHerbTeaPlayer>().shiningRootActive = true;
        }
    }

    /// <summary>花茶增益：火焰花花茶</summary>
    public class FireblossomHerbTeaBuff : ModBuff
    {
        // 显示名与描述走本地化键 Mods.Amphoreus.Buffs.FireblossomHerbTeaBuff.DisplayName / .Description
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<XiLianHerbTeaPlayer>().fireblossomActive = true;
        }
    }

    /// <summary>花茶增益：寒颤棘花茶</summary>
    public class ChillthornHerbTeaBuff : ModBuff
    {
        // 显示名与描述走本地化键 Mods.Amphoreus.Buffs.ChillthornHerbTeaBuff.DisplayName / .Description
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<XiLianHerbTeaPlayer>().chillthornActive = true;
        }
    }
}
