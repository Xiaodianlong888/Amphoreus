using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Amphoreus.Players.Event_Players.Slime_Rain
{
    /// <summary>
    /// 负责屏幕抖动效果。通过 ModifyScreenPosition 在渲染时偏移画面。
    /// </summary>
    public class ScreenshakePlayer : ModPlayer
    {
        /// <summary>剩余抖动帧数（60 帧 = 1 秒）。</summary>
        public int ShakeTimer;

        /// <summary>抖动幅度（像素）。数值越大抖动越强。</summary>
        public int ShakeMagnitude;

        public override void ModifyScreenPosition()
        {
            if (ShakeTimer <= 0)
                return;

            ShakeTimer--;

            // 每帧在屏幕位置上叠加一个随机偏移
            Main.screenPosition += new Vector2(
                Main.rand.Next(-ShakeMagnitude, ShakeMagnitude + 1),
                Main.rand.Next(-ShakeMagnitude, ShakeMagnitude + 1)
            );
        }

        /// <summary>
        /// 触发一次屏幕抖动。
        /// 若当前正在进行的抖动更强，则不会被较弱的抖动覆盖。
        /// </summary>
        /// <param name="magnitude">抖动幅度（像素），推荐 4 / 7 / 11</param>
        /// <param name="duration">持续时间（帧），推荐 20 / 30 / 45</param>
        public void Shake(int magnitude, int duration)
        {
            if (ShakeTimer <= 0 || magnitude >= ShakeMagnitude)
            {
                ShakeMagnitude = magnitude;
                ShakeTimer = duration;
            }
        }
    }
}