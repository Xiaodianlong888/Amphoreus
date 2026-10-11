using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus
{
    public class Amphoreus : Mod
    {
        public static Amphoreus Instance { get; private set; }

        /// <summary>自定义网络消息类型。</summary>
        public enum MessageType : byte
        {
            RequestSlimeRain
        }

        public override void Load()
        {
            Instance = this;

            // 如果准备了音乐文件，取消下面这行的注释
            // MusicLoader.AddMusic(this, "Assets/Sounds/Music/BlackDomainTheme");
        }

        public override void Unload()
        {
            Instance = null;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            MessageType msgType = (MessageType)reader.ReadByte();

            switch (msgType)
            {
                case MessageType.RequestSlimeRain:
                    // 只在服务器端处理
                    if (Main.netMode == NetmodeID.Server && !Main.slimeRain)
                    {
                        // 原版方法内部会自动向所有客户端广播事件开始提示
                        Main.StartSlimeRain();
                    }
                    break;
            }
        }
    }
}