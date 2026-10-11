using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Amphoreus.Configs
{
    public class Mimi_Config : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // 跟随距离模式枚举
        public enum FollowDistanceMode
        {
            Original,    // 原来的跟随距离（侧前方）
            Behind       // 新增跟随在身后
        }

        [Header("MimiFollowSettings")] // 本地化键：Configs.Mimi_Config.Headers.MimiFollowSettings
        [Label("FollowDistanceMode")] // 本地化键：Configs.Mimi_Config.FollowDistanceMode.Label
        [Tooltip("FollowDistanceModeTooltip")] // 本地化键：Configs.Mimi_Config.FollowDistanceMode.Tooltip
        [DefaultValue(FollowDistanceMode.Original)]
        public FollowDistanceMode MimiFollowDistanceMode { get; set; }

        [Label("BehindFollowDistance")] // 本地化键：Configs.Mimi_Config.BehindFollowDistance.Label
        [Tooltip("BehindFollowDistanceTooltip")] // 本地化键：Configs.Mimi_Config.BehindFollowDistance.Tooltip
        [Range(20, 200)]
        [DefaultValue(80)]
        [Slider]
        public int BehindFollowDistance { get; set; }

        [Label("OriginalFollowOffsetX")] // 本地化键：Configs.Mimi_Config.OriginalFollowOffsetX.Label
        [Tooltip("OriginalFollowOffsetXTooltip")] // 本地化键：Configs.Mimi_Config.OriginalFollowOffsetX.Tooltip
        [Range(-100, 100)]
        [DefaultValue(-50)]
        [Slider]
        public int OriginalFollowOffsetX { get; set; }

        [Label("FollowOffsetY")] // 本地化键：Configs.Mimi_Config.FollowOffsetY.Label
        [Tooltip("FollowOffsetYTooltip")] // 本地化键：Configs.Mimi_Config.FollowOffsetY.Tooltip
        [Range(-100, 50)]
        [DefaultValue(-30)]
        [Slider]
        public int FollowOffsetY { get; set; }
    }
}