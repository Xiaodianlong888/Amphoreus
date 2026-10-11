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

namespace Amphoreus.UI.MimiDialogueUI
{
    public class MimiDialogueUI : UIState
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

        private NPC _currentNPC;
        private Mimi _mimi;

        public override void OnInitialize()
        {
            // ----- 主面板：尺寸600x280，垂直位置0.75f（更靠下）-----
            _mainPanel = new UIPanel();
            _mainPanel.Width.Set(600, 0);
            _mainPanel.Height.Set(280, 0);
            _mainPanel.HAlign = 0.5f;
            _mainPanel.VAlign = 0.85f;          // ⬅️ 改为 0.75f，不遮挡屏幕中心
            _mainPanel.BackgroundColor = new Color(63, 82, 151) * 0.9f;
            _mainPanel.BorderColor = Color.Black * 0.8f;
            _mainPanel.SetPadding(0);
            Append(_mainPanel);

            // ----- 头像框（98×100）-----
            _portraitPanel = new UIPanel();
            _portraitPanel.Width.Set(108, 0);
            _portraitPanel.Height.Set(110, 0);
            _portraitPanel.Left.Set(20, 0);
            _portraitPanel.Top.Set(20, 0);
            _portraitPanel.BackgroundColor = new Color(30, 40, 70);
            _portraitPanel.BorderColor = Color.Gold * 0.8f;
            _portraitPanel.SetPadding(5);
            _mainPanel.Append(_portraitPanel);

            _portrait = new UIImage(ModContent.Request<Texture2D>("Amphoreus/NPCs/XiLian/Mimi_Touxiang"));
            _portrait.Width.Set(98, 0);
            _portrait.Height.Set(100, 0);
            _portrait.HAlign = 0.5f;
            _portrait.VAlign = 0.5f;
            _portraitPanel.Append(_portrait);

            // ----- 文本区域（自动换行）-----
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

            // ----- 底部按钮区域（统一背景）-----
            UIPanel buttonArea = new UIPanel();
            buttonArea.Width.Set(560, 0);
            buttonArea.Height.Set(50, 0);
            buttonArea.Left.Set(20, 0);
            buttonArea.Top.Set(200, 0);
            buttonArea.BackgroundColor = new Color(40, 50, 80) * 0.8f;
            buttonArea.BorderColor = Color.Gray * 0.5f;
            buttonArea.SetPadding(10);
            _mainPanel.Append(buttonArea);

            // ----- 第一个按钮：换话题（固定功能）-----
            var switchBtn = CreateStyledButton("");
            _buttonSwitchPanel = switchBtn.panel;
            _buttonSwitchText = switchBtn.text;
            _buttonSwitchPanel.Left.Set(10, 0);
            _buttonSwitchPanel.Top.Set(10, 0);
            _buttonSwitchPanel.OnLeftClick += (evt, el) =>
            {
                if (_mimi != null)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    _mimi.SwitchFunctionMode();   // 切换功能模式
                    RefreshUI();                   // 立即刷新UI
                }
            };
            buttonArea.Append(_buttonSwitchPanel);

            // ----- 第二个按钮：当前模式的功能按钮（动态文本）-----
            var actionBtn = CreateStyledButton("");
            _buttonActionPanel = actionBtn.panel;
            _buttonActionText = actionBtn.text;
            _buttonActionPanel.Left.Set(-110, 1f); // 右对齐
            _buttonActionPanel.Top.Set(10, 0);
            _buttonActionPanel.OnLeftClick += (evt, el) =>
            {
                if (_mimi != null)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    _mimi.HandleSecondButton();    // 执行当前模式功能
                    RefreshUI();                    // 刷新UI（可能更新文本）
                }
            };
            buttonArea.Append(_buttonActionPanel);

            //----- 第三个按钮：关闭按钮（居中）-----
            var closeBtn = CreateStyledButton("关闭");
            UIPanel closePanel = closeBtn.panel;
            closePanel.HAlign = 0.5f;          // 水平居中
            closePanel.Top.Set(10, 0);
            closePanel.OnLeftClick += (evt, el) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                HideUI();
            };
            buttonArea.Append(closePanel);
        }

        // 统一创建带悬停效果的按钮，返回面板和文本的元组
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
            _mimi = npc?.ModNPC as Mimi;
            if (_mimi != null)
            {
                _text.SetText(text);
                RefreshUI();   // 初始显示时也刷新按钮文本
            }
        }

        // 刷新所有动态文本（按钮文本和对话内容）
        private void RefreshUI()
        {
            if (_currentNPC == null || !_currentNPC.active)
            {
                HideUI();
                return;
            }
            _mimi = _currentNPC.ModNPC as Mimi;
            if (_mimi == null) return;

            _buttonSwitchText.SetText(_mimi.GetLocalizedText("NPCs.Mimi.Buttons.ChangeTopic"));
            _buttonActionText.SetText(_mimi.GetCurrentFunctionButtonText());
            _text.SetText(_mimi.GetCurrentModeDialogueText());
        }

        private void HideUI()
        {
            _currentNPC = null;
            _mimi = null;
            ModContent.GetInstance<MimiDialogueSystem>().HideUI();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            // 如果主面板存在且鼠标在其范围内，阻止物品使用
            if (_mainPanel != null && _mainPanel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }
    }
}