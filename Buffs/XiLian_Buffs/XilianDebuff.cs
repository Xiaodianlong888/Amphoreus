using Microsoft.Xna.Framework;  // 添加这行：提供 Color 和 Vector2
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Buffs.XiLian_Buffs
{
    public class XilianDebuff : ModBuff
    {

        public override string Texture => "Amphoreus/Buffs/XiLian_Buffs/XilianDebuff";
        public override void Update(NPC npc, ref int buffIndex)
        {
            // 1. 基础效果：减速 + 降防（契合冰+记忆）
            npc.velocity *= 0.85f; // 减速15%
            npc.defense -= 8;      // 降防8点

            // 2. 视觉粒子：每帧生成星星尘埃
            if (Main.rand.NextBool(3))
            {
                // 随机选择星星颜色
                Color starColor = Main.rand.Next(3) switch
                {
                    0 => new Color(135, 206, 235), // 冰蓝
                    1 => new Color(255, 182, 193), // 樱粉
                    _ => Color.White               // 纯白
                };

                Dust dust = Dust.NewDustPerfect(
                    npc.Center + Main.rand.NextVector2Circular(npc.width / 2, npc.height / 2),
                    DustID.SilverFlame,
                    Vector2.Zero,
                    100,
                    starColor,
                    1.2f
                );
                dust.noGravity = true;
                dust.fadeIn = 1.5f;
            }
        }
    }
}