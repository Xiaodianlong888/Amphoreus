using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Amphoreus.Systems.Ruwosuoshu
{
    public class PlayerWishData : ModPlayer
    {
        // 玩家的心愿记录
        public Dictionary<string, WishRecord> WishRecords { get; private set; } = new Dictionary<string, WishRecord>();

        public override void Initialize()
        {
            // 每个新角色初始化时清空记录
            WishRecords.Clear();
        }

        public override void OnEnterWorld()
        {
            // 进入世界时，为所有世界模板创建默认记录
            var worldTemplates = BookDataSystem.WishTemplates;
            foreach (var template in worldTemplates)
            {
                if (!WishRecords.ContainsKey(template.WishId))
                {
                    WishRecords[template.WishId] = new WishRecord();
                }
            }
        }

        // 获取或创建记录
        public WishRecord GetOrCreateRecord(string wishId)
        {
            if (!WishRecords.TryGetValue(wishId, out var record))
            {
                record = new WishRecord();
                WishRecords[wishId] = record;
            }
            return record;
        }

        // 写入心愿
        public bool WriteWish(string wishId)
        {
            var record = GetOrCreateRecord(wishId);
            if (!record.IsWritten)
            {
                record.IsWritten = true;
                return true;
            }
            return false;
        }

        // 完成心愿
        public bool CompleteWish(string wishId)
        {
            if (WishRecords.TryGetValue(wishId, out var record) &&
                record.IsWritten && !record.IsCompleted)
            {
                record.IsCompleted = true;
                return true;
            }
            return false;
        }

        // 保存玩家数据
        public override void SaveData(TagCompound tag)
        {
            var wishSaveList = new List<TagCompound>();
            foreach (var kvp in WishRecords)
            {
                var recordTag = new TagCompound
                {
                    ["WishId"] = kvp.Key,
                    ["IsWritten"] = kvp.Value.IsWritten,
                    ["IsCompleted"] = kvp.Value.IsCompleted
                };
                wishSaveList.Add(recordTag);
            }
            tag["PlayerWishRecords"] = wishSaveList;
        }

        // 加载玩家数据
        public override void LoadData(TagCompound tag)
        {
            WishRecords.Clear();
            if (tag.ContainsKey("PlayerWishRecords"))
            {
                var wishLoadList = tag.GetList<TagCompound>("PlayerWishRecords");
                foreach (var recordTag in wishLoadList)
                {
                    string wishId = recordTag.GetString("WishId");
                    var record = new WishRecord
                    {
                        IsWritten = recordTag.GetBool("IsWritten"),
                        IsCompleted = recordTag.GetBool("IsCompleted")
                    };
                    WishRecords[wishId] = record;
                }
            }
        }
    }
}