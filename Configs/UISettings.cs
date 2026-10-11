using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Amphoreus.Configs
{
    public class UISettings : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("$Mods.Amphoreus.Configs.UISettings.Headers.Dialogue")]

        [DefaultValue(false)]   // ⬅️ 改为 false，默认使用原版UI
        [LabelKey("$Mods.Amphoreus.Configs.UISettings.UseCustomDialogue.Label")]
        [TooltipKey("$Mods.Amphoreus.Configs.UISettings.UseCustomDialogue.Tooltip")]
        public bool UseCustomDialogue { get; set; } = false;
    }
}