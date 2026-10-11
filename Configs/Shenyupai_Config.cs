using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Amphoreus.Configs
{
    public class ShenyupaiConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("KeybindTips")]
        [Label("显示键位提示")]
        [Tooltip("在屏幕上显示当前技能的键位提示")]
        [DefaultValue(true)]
        public bool ShowKeybindTips { get; set; }

        [Label("键位提示位置")]
        [DrawTicks]
        [OptionStrings(new string[] { "左上角", "右上角", "左下角", "右下角" })]
        [DefaultValue("右上角")]
        public string KeybindPosition { get; set; }

        [Label("提示大小")]
        [Slider]
        [Range(0.5f, 2f)]
        [Increment(0.1f)]
        [DefaultValue(1f)]
        public float TipScale { get; set; }
    }
}