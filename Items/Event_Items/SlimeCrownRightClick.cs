using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Items.Event_Items
{
    /// <summary>
    /// 史莱姆皇冠右键功能：不消耗物品召唤史莱姆雨。
    /// 左键行为保持原版（召唤史莱姆王）。
    /// </summary>
    public class SlimeCrownRightClick : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.SlimeCrown;
        }

        /// <summary>
        /// 启用右键功能。返回 true 才会触发 AltFunctionUse 分支。
        /// </summary>
        public override bool AltFunctionUse(Item item, Player player)
        {
            return true;
        }

        /// <summary>
        /// 只有左键消耗物品，右键不消耗。
        /// </summary>
        public override bool ConsumeItem(Item item, Player player)
        {
            return player.altFunctionUse != 2;
        }

        public override bool? UseItem(Item item, Player player)
        {
            // ============================================================
            //  右键分支：完全接管使用逻辑
            //  --------------------------------------------------------
            //  返回 true 会【完全跳过】原版对史莱姆皇冠的使用逻辑，
            //  因此右键绝对不会召唤史莱姆王。
            // ============================================================
            if (player.altFunctionUse == 2)
            {
                // 已在史莱姆雨中 → 只提示，什么都不做
                if (Main.slimeRain)
                {
                    if (Main.netMode != NetmodeID.Server)
                        Main.NewText("史莱姆雨已经开始了。", new Color(255, 220, 100));
                    return true;
                }

                // 多人客户端 → 发请求给服务器
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)Amphoreus.MessageType.RequestSlimeRain);
                    packet.Send();
                }
                // 单人 / 服务器端 → 直接触发
                else
                {
                    Main.StartSlimeRain();
                }

                return true; // ← 关键：阻止原版逻辑，不召唤史莱姆王
            }

            // ============================================================
            //  左键分支：返回 null 交给原版 → 正常召唤史莱姆王
            // ============================================================
            return null;
        }
    }
}