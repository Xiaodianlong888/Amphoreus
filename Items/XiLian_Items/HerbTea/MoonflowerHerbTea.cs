using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Items.XiLian_Items.HerbTea
{
    /// <summary>花茶：月光草花茶（生命上限+50，魔力上限+80，缓慢恢复魔力）</summary>
    public class MoonflowerHerbTea : ModItem
    {
        // 显示名与描述走本地化键 Mods.Amphoreus.Items.MoonflowerHerbTea.DisplayName / .Tooltip
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.BottledWater);
            Item.width = 20;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.consumable = true;
            Item.maxStack = 30;
            Item.value = 0;
            Item.rare = ItemRarityID.Blue;
            Item.buffType = ModContent.BuffType<Buffs.XiLian_Buffs.HerbTea.MoonflowerHerbTeaBuff>();
            Item.buffTime = 54000; // 15 分钟
        }
    }
}
