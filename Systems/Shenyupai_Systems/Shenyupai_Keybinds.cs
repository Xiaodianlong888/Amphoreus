// 文件：Shenyupai_Keybinds.cs
using Terraria.ModLoader;

namespace Amphoreus.Systems.Shenyupai
{
    public class Shenyupai_Keybinds : ModSystem
    {
        // 公开静态属性，供其他类访问
        public static ModKeybind SkillQ { get; private set; }
        public static ModKeybind SkillE { get; private set; }
        public static ModKeybind SpecialAbility { get; private set; }

        public override void Load()
        {
            // 注册键位 - 第一个参数是Mod实例，第二个是显示名称，第三个是默认键位
            SkillQ = KeybindLoader.RegisterKeybind(Mod, "神谕牌", "Q");
            SpecialAbility = KeybindLoader.RegisterKeybind(Mod, "神谕牌特殊能力", "LeftControl");

            //// 可选：在控制台输出加载信息
            //Mod.Logger.Info("神谕牌键位绑定已加载");
        }

        public override void Unload()
        {
            // 卸载时设为 null
            SkillQ = null;
            SkillE = null;
            SpecialAbility = null;

            Mod.Logger.Info("神谕牌键位绑定已卸载");
        }
    }
}