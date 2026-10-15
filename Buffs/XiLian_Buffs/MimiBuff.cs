using Amphoreus.Players.Shenyupai_Players;
using Amphoreus.Systems.Ruwosuoshu;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Amphoreus.Buffs.XiLian_Buffs
{
    public class MimiBuff : ModBuff
    {
        // === 动态贴图 ===
        public override string Texture => "Amphoreus/Buffs/XiLian_Buffs/MimiBuff";

        public override void SetStaticDefaults()
        {
            // 可选：这里可以设置一些静态属性
            // 显示名称和描述会自动从本地化文件中读取
            // 如果本地化文件中没有，可以在这里设置默认值（英文）
            // Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<Players.XiLian_Players.Mimi.MimiBuffPlayer>();
            modPlayer.hasMimiBuff = true;

            // 固定4%暴击率加成（基础奖励）
            player.GetCritChance(DamageClass.Generic) += 4;

            // 根据任务完成情况添加额外效果
            if (BookDataSystem.HasUnlockedBuffAbility)
            {
                // 第二阶段奖励：额外属性增益
                player.GetDamage(DamageClass.Generic) += 0.05f; // 5%伤害
                player.GetAttackSpeed(DamageClass.Generic) += 0.1f; // 10%攻速
                player.moveSpeed += 0.1f; // 10%移速
            }

            if (BookDataSystem.HasUnlockedDodgeAbility)
            {
                // 第三阶段奖励：闪避能力
                // 闪避逻辑在MimiBuffPlayer中处理
                modPlayer.hasDodgeAbility = true;
            }
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            var player = Main.LocalPlayer;
            var cardPlayer = player.GetModPlayer<Shenyupai_Player>();

            if (cardPlayer.CurrentCard != null && cardPlayer.CurrentCard.Name == "迷迷的声援")
            {
                // 获取本地化字符串并格式化（传入剩余时间参数）
                var localizedTip = this.GetLocalization("DetailedDescription");
                if (localizedTip != null)
                {
                    tip = string.Format(localizedTip.Value, cardPlayer.BuffTimeLeftFormatted);
                }
                else
                {
                    // 如果本地化没有找到，使用默认文本
                    tip = $"• 迷迷为你加油\n• 提供闪避能力，暴击率+4%\n剩余时间: {cardPlayer.BuffTimeLeftFormatted}";
                }
            }
            else
            {
                // 使用默认描述
                var defaultDescription = this.GetLocalization("Description");
                if (defaultDescription != null)
                {
                    tip = defaultDescription.Value;
                }
                else
                {
                    tip = "迷迷为你加油，提供闪避能力，暴击率+4%";
                }
            }
        }
    }
}