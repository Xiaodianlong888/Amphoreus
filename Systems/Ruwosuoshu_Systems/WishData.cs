using Amphoreus.Systems.XiLian_Systems.Xinyuan;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Amphoreus.Systems.Ruwosuoshu_Systems
{
    // 心愿模板（世界数据）
    public class WishTemplate
    {
        public string WishId { get; set; }
        public string Title { get; set; }
        public MimiQuestType RequiredQuest { get; set; }
    }

    // 玩家心愿记录（玩家数据）
    public class WishRecord
    {
        public bool IsWritten { get; set; } = false;
        public bool IsCompleted { get; set; } = false;
    }
}