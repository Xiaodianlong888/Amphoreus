using Amphoreus.Systems.Ruwosuoshu_Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace Amphoreus.UI.Ruwosuoshu
{
    public class QuillUI : UIState
    {
        private UIPanel mainPanel;
        private UIText titleText;
        private UIText wishText;
        private UIImageButton writeButton;
        private UIImageButton cancelButton;

        private string currentWishId = "";
        private string currentWishText = "";

        public override void OnInitialize()
        {
            mainPanel = new UIPanel();
            mainPanel.Width.Set(400, 0f);
            mainPanel.Height.Set(200, 0f);
            mainPanel.HAlign = 0.5f;
            mainPanel.VAlign = 0.5f;
            mainPanel.BackgroundColor = new Color(240, 230, 210);
            mainPanel.BorderColor = new Color(180, 150, 100);
            mainPanel.SetPadding(15);
            Append(mainPanel);

            titleText = new UIText("记录心愿", 1.2f);
            titleText.HAlign = 0.5f;
            titleText.Top.Set(10, 0f);
            titleText.TextColor = Color.White * 0.9f;
            mainPanel.Append(titleText);

            wishText = new UIText("", 1f);
            wishText.HAlign = 0.5f;
            wishText.VAlign = 0.5f;
            wishText.TextColor = Color.White * 0.8f;
            mainPanel.Append(wishText);

            writeButton = new UIImageButton(ModContent.Request<Texture2D>("Terraria/Images/UI/ButtonPlay"));
            writeButton.Width.Set(80, 0f);
            writeButton.Height.Set(40, 0f);
            writeButton.Left.Set(60, 0f);
            writeButton.Top.Set(-40, 1f);
            writeButton.OnLeftClick += WriteButtonClick;
            mainPanel.Append(writeButton);

            var writeText = new UIText("写下", 0.9f);
            writeText.HAlign = 0.5f;
            writeText.VAlign = 0.5f;
            writeText.TextColor = Color.White * 0.9f;
            writeButton.Append(writeText);

            cancelButton = new UIImageButton(ModContent.Request<Texture2D>("Terraria/Images/UI/ButtonDelete"));
            cancelButton.Width.Set(80, 0f);
            cancelButton.Height.Set(40, 0f);
            cancelButton.Left.Set(-60, 1f);
            cancelButton.Top.Set(-40, 1f);
            cancelButton.OnLeftClick += CancelButtonClick;
            mainPanel.Append(cancelButton);

            var cancelText = new UIText("取消", 0.9f);
            cancelText.HAlign = 0.5f;
            cancelText.VAlign = 0.5f;
            cancelText.TextColor = Color.White * 0.9f;
            cancelButton.Append(cancelText);

            // 默认隐藏
            mainPanel.Remove();
        }

        public void ShowWish(string wishId, string text)
        {
            currentWishId = wishId;
            currentWishText = text;
            wishText.SetText(text);

            // 显示UI
            mainPanel.Parent?.Append(mainPanel);
        }

        // 修改 WriteButtonClick 方法：
        private void WriteButtonClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (!string.IsNullOrEmpty(currentWishId))
            {
                Player player = Main.LocalPlayer;

                // 调用 BookDataSystem 记录心愿
                if (BookDataSystem.WriteWish(player, currentWishId))
                {
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                    Main.NewText($"记录成功: {currentWishText}", Color.LightBlue);
                }
                else
                {
                    Main.NewText("这份心愿已经记录过了", Color.Yellow);
                }

                Hide();
            }
        }


        private void CancelButtonClick(UIMouseEvent evt, UIElement listeningElement)
        {
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
            Hide();
        }

        public void Hide()
        {
            mainPanel.Remove();
        }

        public bool IsVisible => mainPanel.Parent != null;
    }
}