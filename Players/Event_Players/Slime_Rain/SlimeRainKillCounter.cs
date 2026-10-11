using Amphoreus.Players.Event_Players.Slime_Rain;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Players.Event_Players.Slime_Rain
{
    /// <summary>
    /// 监听 NPC 击杀，在史莱姆雨期间统计史莱姆击杀数。
    /// </summary>
    public class SlimeRainKillCounter : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            // 只在史莱姆雨事件期间统计
            if (!Main.slimeRain)
                return;

            // 排除雕像生成的假史莱姆
            if (npc.SpawnedFromStatue)
                return;

            // 排除非史莱姆类敌怪
            if (!IsCountableSlime(npc))
                return;

            SlimeRainTracker.SlimesKilled++;
        }

        /// <summary>
        /// 判断该 NPC 是否为计入进度的史莱姆。
        /// </summary>
        private static bool IsCountableSlime(NPC npc)
        {
            // 史莱姆王本身不计入
            if (npc.type == NPCID.KingSlime)
                return false;

            // 绝大多数史莱姆使用 aiStyle = Slime (1)
            if (npc.aiStyle == NPCAIStyleID.Slime)
                return true;

            // 如需补充特殊类型，可在此扩展，例如：
            // if (npc.type == NPCID.RainbowSlime) return true;

            return false;
        }
    }
}