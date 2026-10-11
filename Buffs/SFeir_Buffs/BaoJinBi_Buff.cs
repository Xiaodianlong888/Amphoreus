using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace Amphoreus.Buffs.SFeir_Buffs
{
    public class BaoJinBi_Buff : ModBuff
    {
        // 设置Buff纹理路径
        public override string Texture => "Amphoreus/Buffs/SFeir_Buffs/BaoJinBi_Buff";

        // 易伤等级
        public enum VulnerabilityLevel
        {
            None,      // 无易伤
            Silver,    // 白银易伤 (15银)
            Gold,      // 黄金易伤 (15金)
            Platinum   // 铂金易伤 (1铂金)
        }

        // 爆金币数据
        public class CoinData
        {
            public int TotalDamage = 0;          // 累计伤害（铜币）
            public VulnerabilityLevel Level = VulnerabilityLevel.None; // 当前易伤等级
            public int LastHitTime = 0;          // 上次被击中的时间（游戏刻）
            public bool HasDroppedLevelCoin = false; // 是否已掉落等级提示钱币
            public int DropTimer = 0;            // 持续掉落计时器
        }

        // 只存储BOSS的数据
        public static Dictionary<int, CoinData> bossCoinData = new Dictionary<int, CoinData>();

        // 阈值定义（铜币为单位）
        private const int SilverThreshold = 1500;    // 15银 = 1500铜币
        private const int GoldThreshold = 150000;    // 15金 = 150,000铜币
        private const int PlatinumThreshold = 1000000; // 1铂金 = 1,000,000铜币

        // 易伤效果
        private static readonly float[] VulnerabilityValues = { 0f, 0.15f, 0.25f, 0.35f };

        // 结算乘数
        private static readonly int[] SettlementMultipliers = { 1, 2, 3, 4 };

        // 持续掉落比例（铜:银:金:铂）
        private static readonly int[][] DropRatios =
        {
            new int[] { 100, 0, 0, 0 },      // 无易伤: 100%铜币
            new int[] { 55, 45, 0, 0 },      // 白银易伤: 55%铜, 45%银
            new int[] { 30, 45, 25, 0 },     // 黄金易伤: 30%铜, 45%银, 25%金
            new int[] { 15, 35, 45, 5 }      // 铂金易伤: 15%铜, 35%银, 45%金, 5%铂
        };

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false; // PVP不生效
            Main.persistentBuff[Type] = false;

            //DisplayName.SetDefault("爆金币");
            //Description.SetDefault("积累伤害掉落金币，增加易伤");

            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // 只对BOSS生效
            if (!npc.boss)
            {
                npc.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            // 获取或创建数据
            if (!bossCoinData.TryGetValue(npc.whoAmI, out CoinData data))
            {
                data = new CoinData();
                bossCoinData[npc.whoAmI] = data;
            }

            int currentTime = (int)Main.GameUpdateCount;

            // 检查连击中断（2秒无新伤害）
            if (currentTime - data.LastHitTime > 120) // 120帧 = 2秒
            {
                // 有伤害积累且达到阈值才结算
                if (data.TotalDamage >= SilverThreshold)
                {
                    // 根据易伤等级结算
                    DropSettlementCoins(npc, data);
                    ResetData(data);
                }
            }

            // 持续掉落（每秒一次）
            data.DropTimer++;
            if (data.DropTimer >= 60) // 60帧 = 1秒
            {
                DropContinuousCoins(npc, data);
                data.DropTimer = 0;
            }

            // 清理死亡BOSS的数据
            if (!npc.active || npc.life <= 0)
            {
                if (data.TotalDamage >= SilverThreshold)
                {
                    DropSettlementCoins(npc, data);
                }
                bossCoinData.Remove(npc.whoAmI);
            }
        }

        // ====== 公共方法：处理被击中 ======
        public static void OnNPCGotHit(NPC npc, Player player, int damage)
        {
            if (!npc.boss) return;

            // 获取或创建数据
            if (!bossCoinData.TryGetValue(npc.whoAmI, out CoinData data))
            {
                data = new CoinData();
                bossCoinData[npc.whoAmI] = data;
            }

            // 累计伤害
            int oldDamage = data.TotalDamage;
            data.TotalDamage += damage;
            data.LastHitTime = (int)Main.GameUpdateCount;

            // 检查易伤等级提升
            VulnerabilityLevel oldLevel = data.Level;
            UpdateVulnerabilityLevel(data);

            // 如果易伤等级提升，掉落提示钱币
            if (data.Level > oldLevel && !data.HasDroppedLevelCoin)
            {
                DropLevelUpCoin(npc, data.Level);
                data.HasDroppedLevelCoin = true;
            }

            // 如果伤害增加，重置等级提示状态
            if (data.TotalDamage > oldDamage)
            {
                data.HasDroppedLevelCoin = false;
            }

            // 确保有Buff
            if (npc.FindBuffIndex(ModContent.BuffType<BaoJinBi_Buff>()) < 0)
            {
                npc.AddBuff(ModContent.BuffType<BaoJinBi_Buff>(), 600);
            }
        }

        // ====== 更新易伤等级 ======
        private static void UpdateVulnerabilityLevel(CoinData data)
        {
            if (data.TotalDamage >= PlatinumThreshold)
            {
                data.Level = VulnerabilityLevel.Platinum;
            }
            else if (data.TotalDamage >= GoldThreshold)
            {
                data.Level = VulnerabilityLevel.Gold;
            }
            else if (data.TotalDamage >= SilverThreshold)
            {
                data.Level = VulnerabilityLevel.Silver;
            }
            else
            {
                data.Level = VulnerabilityLevel.None;
            }
        }

        // ====== 掉落等级提升提示钱币 ======
        private static void DropLevelUpCoin(NPC npc, VulnerabilityLevel level)
        {
            int coinType = ItemID.CopperCoin;
            int amount = 1;

            switch (level)
            {
                case VulnerabilityLevel.Silver:
                    coinType = ItemID.SilverCoin;
                    amount = 15; // 15银
                    break;
                case VulnerabilityLevel.Gold:
                    coinType = ItemID.GoldCoin;
                    amount = 15; // 15金
                    break;
                case VulnerabilityLevel.Platinum:
                    coinType = ItemID.PlatinumCoin;
                    amount = 1; // 1铂金
                    break;
            }

            // 掉落提示钱币
            for (int i = 0; i < amount; i++)
            {
                SpawnSingleCoin(npc.Center, coinType, 1);
            }
        }

        // ====== 连击中断结算掉落 ======
        private static void DropSettlementCoins(NPC npc, CoinData data)
        {
            if (data.TotalDamage <= 0) return;

            // 计算结算总量 = 累计伤害 × 结算乘数
            int settlementValue = data.TotalDamage * SettlementMultipliers[(int)data.Level];

            // 将价值转换为钱币掉落
            DropValueAsCoins(npc.Center, settlementValue);
        }

        // ====== 持续掉落 ======
        private static void DropContinuousCoins(NPC npc, CoinData data)
        {
            int levelIndex = (int)data.Level;
            int[] ratios = DropRatios[levelIndex];

            // 计算总概率
            int totalRatio = 0;
            foreach (int ratio in ratios) totalRatio += ratio;

            if (totalRatio <= 0) return;

            // 随机掉落1-3个钱币
            int dropCount = Main.rand.Next(1, 4);

            for (int i = 0; i < dropCount; i++)
            {
                // 根据比例选择钱币类型
                int randomValue = Main.rand.Next(totalRatio);
                int accumulated = 0;
                int coinType = ItemID.CopperCoin;

                for (int j = 0; j < ratios.Length; j++)
                {
                    accumulated += ratios[j];
                    if (randomValue < accumulated)
                    {
                        coinType = j switch
                        {
                            0 => ItemID.CopperCoin,
                            1 => ItemID.SilverCoin,
                            2 => ItemID.GoldCoin,
                            3 => ItemID.PlatinumCoin,
                            _ => ItemID.CopperCoin
                        };
                        break;
                    }
                }

                // 确定堆叠数量
                int stack = 1;
                if (coinType == ItemID.CopperCoin)
                    stack = Main.rand.Next(1, 10);
                else if (coinType == ItemID.SilverCoin)
                    stack = Main.rand.Next(1, 5);

                SpawnSingleCoin(npc.Center, coinType, stack);
            }
        }

        // ====== 将价值转换为钱币掉落 ======
        private static void DropValueAsCoins(Vector2 position, int copperValue)
        {
            // 计算各种钱币数量
            int platinumCount = copperValue / 1000000;
            copperValue %= 1000000;

            int goldCount = copperValue / 10000;
            copperValue %= 10000;

            int silverCount = copperValue / 100;
            copperValue %= 100;

            int copperCount = copperValue;

            // 掉落铂金币
            if (platinumCount > 0)
            {
                // 铂金币不堆叠
                for (int i = 0; i < platinumCount; i++)
                {
                    SpawnSingleCoin(position, ItemID.PlatinumCoin, 1);
                }
            }

            // 掉落金币
            if (goldCount > 0)
            {
                // 金币可以少量堆叠
                int stacks = (goldCount + 9) / 10; // 最多10个一堆
                for (int i = 0; i < stacks; i++)
                {
                    int stackSize = Math.Min(10, goldCount - i * 10);
                    SpawnSingleCoin(position, ItemID.GoldCoin, stackSize);
                }
            }

            // 掉落银币
            if (silverCount > 0)
            {
                // 银币可以堆叠较多
                int stacks = (silverCount + 49) / 50; // 最多50个一堆
                for (int i = 0; i < stacks; i++)
                {
                    int stackSize = Math.Min(50, silverCount - i * 50);
                    SpawnSingleCoin(position, ItemID.SilverCoin, stackSize);
                }
            }

            // 掉落铜币
            if (copperCount > 0)
            {
                // 铜币大量堆叠
                int stacks = (copperCount + 99) / 100; // 最多100个一堆
                for (int i = 0; i < stacks; i++)
                {
                    int stackSize = Math.Min(100, copperCount - i * 100);
                    SpawnSingleCoin(position, ItemID.CopperCoin, stackSize);
                }
            }
        }

        // ====== 生成单个钱币 ======
        private static void SpawnSingleCoin(Vector2 position, int coinType, int stack)
        {
            Vector2 spawnPos = position + Main.rand.NextVector2Circular(50f, 50f);
            Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f) - new Vector2(0, 2f);

            int itemIndex = Item.NewItem(
                new EntitySource_Misc("BaoJinBi"),
                (int)spawnPos.X,
                (int)spawnPos.Y,
                16,
                16,
                coinType,
                stack,
                noBroadcast: false,
                0
            );

            if (itemIndex < Main.maxItems)
            {
                Main.item[itemIndex].velocity = velocity;
            }

            // 钱币特效
            SpawnCoinDust(coinType, spawnPos);
        }

        // ====== 钱币特效 ======
        private static void SpawnCoinDust(int coinType, Vector2 position)
        {
            int dustType = DustID.Copper;
            Color color = Color.Orange;

            switch (coinType)
            {
                case ItemID.CopperCoin:
                    dustType = DustID.Copper;
                    color = Color.Orange;
                    break;
                case ItemID.SilverCoin:
                    dustType = DustID.Silver;
                    color = Color.Silver;
                    break;
                case ItemID.GoldCoin:
                    dustType = DustID.Gold;
                    color = Color.Gold;
                    break;
                case ItemID.PlatinumCoin:
                    dustType = DustID.PlatinumCoin;
                    color = Color.LightSkyBlue;
                    break;
            }

            for (int i = 0; i < 3; i++)
            {
                Dust.NewDustPerfect(
                    position,
                    dustType,
                    Main.rand.NextVector2Circular(2f, 2f),
                    100,
                    color,
                    1f
                );
            }
        }

        // ====== 重置数据 ======
        private static void ResetData(CoinData data)
        {
            data.TotalDamage = 0;
            data.Level = VulnerabilityLevel.None;
            data.HasDroppedLevelCoin = false;
            data.DropTimer = 0;
        }

        // ====== 获取当前易伤值 ======
        public static float GetVulnerability(NPC npc)
        {
            if (!npc.boss || !bossCoinData.TryGetValue(npc.whoAmI, out CoinData data))
                return 0f;

            return VulnerabilityValues[(int)data.Level];
        }

        // ====== 清理数据 ======
        public static void ClearBossData(int npcWhoAmI)
        {
            bossCoinData.Remove(npcWhoAmI);
        }
    }

    // ====== GlobalNPC类用于处理易伤效果 ======
    public class BaoJinBiGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            // 应用易伤效果
            float vulnerability = BaoJinBi_Buff.GetVulnerability(npc);
            if (vulnerability > 0)
            {
                modifiers.FinalDamage *= (1f + vulnerability);
            }
        }

        public override void OnKill(NPC npc)
        {
            // 清理数据
            BaoJinBi_Buff.ClearBossData(npc.whoAmI);
        }
    }
}