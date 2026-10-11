// 新建一个文件：HouseFixSystem.cs
using Terraria;
using Terraria.ModLoader;

namespace Amphoreus.Systems.XiLian_Systems.Xingtai
{
    public class HouseFixSystem : ModSystem
    {
        private int fixTimer = 0;
        private const int FixInterval = 60 * 60; // 每60秒检查一次

        public override void PostUpdateWorld()
        {
            fixTimer++;
            if (fixTimer >= FixInterval)
            {
                fixTimer = 0;
                FixInvalidHouseOccupations();
            }
        }

        private void FixInvalidHouseOccupations()
        {
            // 检查所有NPC
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || !npc.townNPC)
                    continue;

                // 如果NPC有家但实际不在房屋中，释放房屋
                if (!npc.homeless && npc.homeTileX == 0 && npc.homeTileY == 0)
                {
                    npc.homeless = true;
                    npc.netUpdate = true;
                }
            }

            // 重置所有未分配的房屋标志
            for (int i = 0; i < Main.maxTilesX; i++)
            {
                for (int j = 0; j < Main.maxTilesY; j++)
                {
                    // 检查房屋标记是否有效
                    // 这部分逻辑需要根据实际情况调整
                }
            }
        }
    }
}