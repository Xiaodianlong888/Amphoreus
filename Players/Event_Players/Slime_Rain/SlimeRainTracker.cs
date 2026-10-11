using Amphoreus.Players.Event_Players.Slime_Rain;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Players.Event_Players.Slime_Rain
{
    /// <summary>
    /// 追踪史莱姆雨进度，并在 25% / 50% / 75% 时触发抖动与提示。
    /// </summary>
    public class SlimeRainTracker : ModPlayer
    {
        /// <summary>本次事件中累计击杀的史莱姆数量（全局共享）。</summary>
        public static int SlimesKilled;

        // 上一次事件是否处于激活状态
        private bool _wasSlimeRainActive;

        // 阈值触发标记，防止重复触发
        private bool _triggered25;
        private bool _triggered50;
        private bool _triggered75;

        public override void PostUpdate()
        {
            bool isActive = Main.slimeRain;

            // 事件开始：重置进度
            if (isActive && !_wasSlimeRainActive)
            {
                ResetProgress();
            }
            // 事件结束：重置进度
            else if (!isActive && _wasSlimeRainActive)
            {
                ResetProgress();
            }

            // 事件进行中：检查阈值
            if (isActive)
            {
                CheckThresholds();
            }

            _wasSlimeRainActive = isActive;
        }

        private void ResetProgress()
        {
            SlimesKilled = 0;
            _triggered25 = false;
            _triggered50 = false;
            _triggered75 = false;
        }

        private void CheckThresholds()
        {
            // 已经击败过史莱姆王只需 75 只，否则需要 150 只
            int target = NPC.downedSlimeKing ? 75 : 150;
            float progress = (float)SlimesKilled / target;

            var shake = Player.GetModPlayer<ScreenshakePlayer>();

            // ---- 25% ----
            if (!_triggered25 && progress >= 0.25f)
            {
                _triggered25 = true;
                shake.Shake(4, 20);
                Notify("有什么东西在接近……", Color.Yellow);
            }

            // ---- 50% ----
            if (!_triggered50 && progress >= 0.50f)
            {
                _triggered50 = true;
                shake.Shake(7, 30);
                Notify("某种强大的存在正在逼近……", new Color(255, 165, 0));
            }

            // ---- 75% ----
            if (!_triggered75 && progress >= 0.75f)
            {
                _triggered75 = true;
                shake.Shake(11, 45);
                Notify("它就要来了！", Color.Red);
            }
        }

        /// <summary>
        /// 发送聊天提示。注意：服务器端不能调用 Main.NewText，会崩溃。
        /// </summary>
        private static void Notify(string text, Color color)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText(text, color);
            }
        }
    }
}