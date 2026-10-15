using Amphoreus.NPCs.XiLian;
using Amphoreus.Systems.XiLian_Systems.Touxiang;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using Terraria.ModLoader;

namespace Amphoreus.UI.Dialogue
{
    public class XiLianDialogueUI : UIState
    {
        private UIPanel _mainPanel;
        private UIPanel _portraitPanel;
        private UIImage _portrait;
        private UIPanel _textPanel;
        private UIText _text;
        private UIPanel _buttonSwitchPanel;
        private UIText _buttonSwitchText;
        private UIPanel _buttonActionPanel;
        private UIText _buttonActionText;
        private UIPanel _buttonClosePanel;
        private UIText _buttonCloseText;

        private NPC _currentNPC;
        private Xilian _xilian;

        public override void OnInitialize()
        {
            // 主面板
            _mainPanel = new UIPanel();
            _mainPanel.Width.Set(600, 0);
            _mainPanel.Height.Set(280, 0);
            _mainPanel.HAlign = 0.5f;
            _mainPanel.VAlign = 0.75f;
            _mainPanel.BackgroundColor = new Color(63, 82, 151) * 0.9f;
            _mainPanel.BorderColor = Color.Black * 0.8f;
            _mainPanel.SetPadding(0);
            Append(_mainPanel);

            // 头像框
            _portraitPanel = new UIPanel();
            _portraitPanel.Width.Set(108, 0);
            _portraitPanel.Height.Set(110, 0);
            _portraitPanel.Left.Set(20, 0);
            _portraitPanel.Top.Set(20, 0);
            _portraitPanel.BackgroundColor = new Color(30, 40, 70);
            _portraitPanel.BorderColor = Color.Gold * 0.8f;
            _portraitPanel.SetPadding(5);
            _mainPanel.Append(_portraitPanel);

            _portrait = new UIImage(ModContent.Request<Texture2D>("Amphoreus/NPCs/XiLian/Xilian_Touxiang"));
            _portrait.Width.Set(98, 0);
            _portrait.Height.Set(100, 0);
            _portrait.HAlign = 0.5f;
            _portrait.VAlign = 0.5f;
            _portraitPanel.Append(_portrait);

            // 文本区域
            _textPanel = new UIPanel();
            _textPanel.Width.Set(420, 0);
            _textPanel.Height.Set(110, 0);
            _textPanel.Left.Set(150, 0);
            _textPanel.Top.Set(20, 0);
            _textPanel.BackgroundColor = new Color(20, 30, 50);
            _textPanel.BorderColor = Color.Silver * 0.6f;
            _textPanel.SetPadding(10);
            _mainPanel.Append(_textPanel);

            _text = new UIText("");
            _text.Width.Set(400, 0);
            _text.Height.Set(90, 0);
            _text.IsWrapped = true;
            _text.TextColor = Color.White;
            _textPanel.Append(_text);

            // 底部按钮区域
            UIPanel buttonArea = new UIPanel();
            buttonArea.Width.Set(560, 0);
            buttonArea.Height.Set(50, 0);
            buttonArea.Left.Set(20, 0);
            buttonArea.Top.Set(200, 0);
            buttonArea.BackgroundColor = new Color(40, 50, 80) * 0.8f;
            buttonArea.BorderColor = Color.Gray * 0.5f;
            buttonArea.SetPadding(10);
            _mainPanel.Append(buttonArea);

            // 换话题按钮（左）
            var switchBtn = CreateStyledButton("");
            _buttonSwitchPanel = switchBtn.panel;
            _buttonSwitchText = switchBtn.text;
            _buttonSwitchPanel.Left.Set(10, 0);
            _buttonSwitchPanel.Top.Set(10, 0);
            _buttonSwitchPanel.OnLeftClick += (evt, el) =>
            {
                if (_xilian != null)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    _xilian.SwitchFunctionMode();
                    RefreshUI();
                }
            };
            buttonArea.Append(_buttonSwitchPanel);

            // 功能按钮（右）
            var actionBtn = CreateStyledButton("");
            _buttonActionPanel = actionBtn.panel;
            _buttonActionText = actionBtn.text;
            _buttonActionPanel.Left.Set(-110, 1f);
            _buttonActionPanel.Top.Set(10, 0);
            _buttonActionPanel.OnLeftClick += (evt, el) =>
            {
                if (_xilian != null)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    _xilian.HandleSecondButton();
                    // 如果点击商店或变身，UI会隐藏，否则刷新
                    if (_xilian.CurrentFunctionMode != Xilian.FunctionMode.Shop &&
                        _xilian.CurrentFunctionMode != Xilian.FunctionMode.Transform)
                    {
                        RefreshUI();
                    }
                }
            };
            buttonArea.Append(_buttonActionPanel);

            // 关闭按钮（居中）
            var closeBtn = CreateStyledButton("关闭");
            _buttonClosePanel = closeBtn.panel;
            _buttonCloseText = closeBtn.text;
            _buttonClosePanel.HAlign = 0.5f;
            _buttonClosePanel.Top.Set(10, 0);
            _buttonClosePanel.OnLeftClick += (evt, el) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                HideUI();
            };
            buttonArea.Append(_buttonClosePanel);
        }

        private (UIPanel panel, UIText text) CreateStyledButton(string text)
        {
            UIPanel panel = new UIPanel();
            panel.Width.Set(100, 0);
            panel.Height.Set(30, 0);
            panel.BackgroundColor = new Color(70, 90, 140);
            panel.BorderColor = Color.White * 0.4f;
            panel.SetPadding(5);

            UIText uiText = new UIText(text);
            uiText.HAlign = 0.5f;
            uiText.VAlign = 0.5f;
            uiText.TextColor = Color.White;
            panel.Append(uiText);

            panel.OnMouseOver += (evt, el) =>
            {
                panel.BackgroundColor = new Color(100, 130, 200);
                panel.BorderColor = Color.White;
            };
            panel.OnMouseOut += (evt, el) =>
            {
                panel.BackgroundColor = new Color(70, 90, 140);
                panel.BorderColor = Color.White * 0.4f;
            };

            return (panel, uiText);
        }

        public void SetDialogue(string text, NPC npc)
        {
            _currentNPC = npc;
            _xilian = npc?.ModNPC as Xilian;
            if (_xilian != null)
            {
                _text.SetText(text);
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            if (_currentNPC == null || !_currentNPC.active)
            {
                HideUI();
                return;
            }
            _xilian = _currentNPC.ModNPC as Xilian;
            if (_xilian == null) return;

            _buttonSwitchText.SetText(_xilian.GetLocalizedText("NPCs.Xilian.Buttons.ChangeTopic"));
            _buttonActionText.SetText(_xilian.GetCurrentFunctionButtonText());
            _text.SetText(_xilian.GetCurrentModeDialogueText());
        }

        private void HideUI()
        {
            _currentNPC = null;
            _xilian = null;
            ModContent.GetInstance<XiLianDialogueSystem>().HideUI();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_mainPanel != null && _mainPanel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }
    }
}