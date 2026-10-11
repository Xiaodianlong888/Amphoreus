using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace Amphoreus.Systems.XiLian_Systems.Xingtai
{
    public class BossEventDetector : GlobalNPC
    {
        public override void OnSpawn(NPC npc, Terraria.DataStructures.IEntitySource source)
        {
            // 只检测BOSS标签的NPC
            if (npc.boss && !npc.friendly)
            {
                // 触发事件
                var eventSystem = ModContent.GetInstance<BossEventSystem>();
                eventSystem?.OnBossSpawned(npc);
            }
        }
    }
}