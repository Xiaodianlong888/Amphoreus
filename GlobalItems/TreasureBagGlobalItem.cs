using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Amphoreus.Players.Shenyupai_Players;

namespace Amphoreus.GlobalItems
{
    public class TreasureBagGlobalItem : GlobalItem
    {
        public override bool ConsumeItem(Item item, Player player)
        {
            // 使用ItemID.Sets.BossBag来判断是否为宝藏袋
            // 这是原版提供的判断宝藏袋的方法，兼容所有Mod
            if (ItemID.Sets.BossBag[item.type])
            {
                var cardPlayer = player.GetModPlayer<Shenyupai_Player>();

                if (cardPlayer != null && cardPlayer.hasGuiJi && cardPlayer.HasActiveBuff)
                {
                    float chance = 0.5f + (player.luck * 0.02f);
                    chance = MathHelper.Clamp(chance, 0.5f, 0.9f);

                    if (Main.rand.NextFloat() < chance)
                    {
                        // 特效
                        for (int i = 0; i < 10; i++)
                        {
                            Dust.NewDustPerfect(
                                player.Center,
                                DustID.GoldCoin,
                                Main.rand.NextVector2Circular(4f, 4f),
                                150,
                                default,
                                1.5f
                            );
                        }

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.Center);
                        Main.NewText("扎格列斯的祝福生效！宝藏袋未消耗", Color.Gold);

                        return false;
                    }
                }
            }

            return base.ConsumeItem(item, player);
        }
    }
}