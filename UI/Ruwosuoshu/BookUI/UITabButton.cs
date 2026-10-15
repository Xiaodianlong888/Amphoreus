using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Amphoreus.UI.Ruwosuoshu.BookUI
{
    public class UITabButton : UIElement
    {
        private string text;
        private bool isHovered;
        private bool isSelected;
        private float scale;

        public string Text => text;

        public bool IsSelected
        {
            get => isSelected;
            set => isSelected = value;
        }

        public System.Action OnClickCallback;

        public UITabButton(string text, float scale = 1f)
        {
            this.text = text;
            this.scale = scale;

            // 设置点击事件
            this.OnLeftClick += (evt, element) => {
                OnClickCallback?.Invoke();
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
            };
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var dimensions = GetDimensions();

            // 绘制背景
            Color backgroundColor;
            if (isSelected)
            {
                backgroundColor = new Color(240, 230, 200);
            }
            else if (isHovered)
            {
                backgroundColor = new Color(220, 210, 180);
            }
            else
            {
                backgroundColor = new Color(200, 190, 160);
            }

            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)dimensions.X, (int)dimensions.Y,
                             (int)dimensions.Width, (int)dimensions.Height),
                backgroundColor);

            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)dimensions.X, (int)dimensions.Y,
                             (int)dimensions.Width, 2),
                isSelected ? new Color(180, 150, 100) : new Color(150, 120, 80));

            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)dimensions.X, (int)dimensions.Y + (int)dimensions.Height - 2,
                             (int)dimensions.Width, 2),
                isSelected ? new Color(180, 150, 100) : new Color(150, 120, 80));

            // 绘制文本
            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * scale;
            Vector2 textPos = new Vector2(
                dimensions.X + dimensions.Width / 2 - textSize.X / 2,
                dimensions.Y + dimensions.Height / 2 - textSize.Y / 2);

            Color textColor = isSelected ?
                Color.White * 0.9f : Color.White * 0.7f;

            // 使用缩放绘制文本
            Utils.DrawBorderString(spriteBatch, text, textPos, textColor, scale);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // 检查鼠标悬停
            isHovered = ContainsPoint(Main.MouseScreen);
        }
    }
}