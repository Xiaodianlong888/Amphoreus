// Shenyupai_World.cs
using Amphoreus.Players.Shenyupai_Players;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Amphoreus.Systems.Shenyupai
{
    public class Shenyupai_World : ModSystem
    {
        public static bool HasFuShiActive { get; set; } = false;
        public static int FuShiOwner { get; set; } = -1;
        public static List<string> ActiveCardsInWorld { get; set; } = new List<string>();

        public override void Load()
        {
            ActiveCardsInWorld = new List<string>();
        }

        public override void Unload()
        {
            ActiveCardsInWorld.Clear();
            HasFuShiActive = false;
            FuShiOwner = -1;
        }

        // 检查世界是否已有某张牌
        public static bool IsCardActiveInWorld(string cardName)
        {
            return ActiveCardsInWorld.Contains(cardName);
        }

        // 添加活跃卡牌
        public static void AddActiveCard(string cardName)
        {
            if (!ActiveCardsInWorld.Contains(cardName))
                ActiveCardsInWorld.Add(cardName);
        }

        // 移除活跃卡牌
        public static void RemoveActiveCard(string cardName)
        {
            ActiveCardsInWorld.Remove(cardName);
        }

        // 检查是否可以抽取某张牌
        public static bool CanDrawCard(string cardName, Player player)
        {
            // 负世之牌特殊检查
            if (cardName == "全世之座")
            {
                // 需要已解锁所有其他牌
                var cardPlayer = player.GetModPlayer<Shenyupai_Player>();
                // 这里需要检查解锁进度
                return false; // 简化处理
            }

            // 普通牌检查：世界唯一性
            if (IsCardActiveInWorld(cardName))
                return false;

            return true;
        }
    }
}