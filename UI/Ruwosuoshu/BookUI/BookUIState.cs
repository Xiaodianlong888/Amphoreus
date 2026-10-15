using Amphoreus.Systems.Ruwosuoshu;
using Amphoreus.Systems.XiLian_Systems.Xinyuan;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Amphoreus.Items.XiLian_Items;

namespace Amphoreus.UI.Ruwosuoshu.BookUI
{
    public class BookUIState : UIState
    {
        private UIPanel mainPanel;
        private UIText titleText;
        private UIImageButton closeButton;
        private UIPanel tabPanel;

        private UIPanel contentPanel;
        private UIPanel leftPagePanel;
        private UIPanel rightPagePanel;

        private UIText prevPageText;
        private UIText nextPageText;
        private UIText pageIndicator;

        private UITabButton catalogTabBtn;
        private UITabButton storiesTabBtn;
        private UITabButton wishesTabBtn;

        private enum BookSection { Catalog, Stories, Wishes }
        private BookSection currentSection = BookSection.Catalog;
        private int currentPage = 0;
        private int totalPages = 0;

        public override void OnInitialize()
        {
            mainPanel = new UIPanel();
            mainPanel.Width.Set(1000, 0f);
            mainPanel.Height.Set(700, 0f);
            mainPanel.HAlign = 0.5f;
            mainPanel.VAlign = 0.5f;
            mainPanel.BackgroundColor = new Color(240, 230, 210);
            mainPanel.BorderColor = new Color(180, 150, 100);
            mainPanel.SetPadding(0);
            Append(mainPanel);

            titleText = new UIText("如我所书", 1.3f);
            titleText.HAlign = 0.5f;
            titleText.Top.Set(15, 0f);
            titleText.TextColor = Color.White * 0.9f;
            mainPanel.Append(titleText);

            closeButton = new UIImageButton(ModContent.Request<Texture2D>("Terraria/Images/UI/ButtonDelete"));
            closeButton.Width.Set(32, 0f);
            closeButton.Height.Set(32, 0f);
            closeButton.Left.Set(-40, 1f);
            closeButton.Top.Set(10, 0f);
            closeButton.OnLeftClick += (evt, element) => {
                UISystem.CloseBookUI();
            };
            mainPanel.Append(closeButton);

            CreateTabPanel();
            CreateContentArea();
            CreatePageControls();

            SwitchSection(BookSection.Catalog);

            // 初始化后自动刷新一次
            DisplayCurrentPage();
        }

        // 添加刷新方法
        public void RefreshAllPages()
        {
            SwitchSection(currentSection); // 这会重置当前页面并刷新显示
        }

        private void CreateTabPanel()
        {
            tabPanel = new UIPanel();
            tabPanel.Width.Set(900, 0f);
            tabPanel.Height.Set(50, 0f);
            tabPanel.HAlign = 0.5f;
            tabPanel.Top.Set(60, 0f);
            tabPanel.BackgroundColor = new Color(220, 200, 180);
            tabPanel.BorderColor = new Color(180, 150, 100);
            mainPanel.Append(tabPanel);

            catalogTabBtn = new UITabButton("📖 目录");
            catalogTabBtn.Width.Set(180, 0f);
            catalogTabBtn.Height.Set(40, 0f);
            catalogTabBtn.Left.Set(30, 0f);
            catalogTabBtn.VAlign = 0.5f;
            catalogTabBtn.IsSelected = true;
            catalogTabBtn.OnClickCallback = () => {
                if (currentSection != BookSection.Catalog)
                {
                    SwitchSection(BookSection.Catalog);
                }
            };
            tabPanel.Append(catalogTabBtn);

            storiesTabBtn = new UITabButton("📚 故事");
            storiesTabBtn.Width.Set(180, 0f);
            storiesTabBtn.Height.Set(40, 0f);
            storiesTabBtn.Left.Set(260, 0f);
            storiesTabBtn.VAlign = 0.5f;
            storiesTabBtn.OnClickCallback = () => {
                if (currentSection != BookSection.Stories)
                {
                    SwitchSection(BookSection.Stories);
                }
            };
            tabPanel.Append(storiesTabBtn);

            wishesTabBtn = new UITabButton("💖 心愿单");
            wishesTabBtn.Width.Set(180, 0f);
            wishesTabBtn.Height.Set(40, 0f);
            wishesTabBtn.Left.Set(490, 0f);
            wishesTabBtn.VAlign = 0.5f;
            wishesTabBtn.OnClickCallback = () => {
                if (currentSection != BookSection.Wishes)
                {
                    SwitchSection(BookSection.Wishes);
                }
            };
            tabPanel.Append(wishesTabBtn);
        }

        private void CreateContentArea()
        {
            contentPanel = new UIPanel();
            contentPanel.Width.Set(900, 0f);
            contentPanel.Height.Set(500, 0f);
            contentPanel.HAlign = 0.5f;
            contentPanel.Top.Set(120, 0f);
            contentPanel.BackgroundColor = new Color(250, 245, 235);
            contentPanel.BorderColor = new Color(180, 150, 100);
            contentPanel.SetPadding(0);
            mainPanel.Append(contentPanel);

            leftPagePanel = new UIPanel();
            leftPagePanel.Width.Set(435, 0f);
            leftPagePanel.Height.Set(470, 0f);
            leftPagePanel.Left.Set(10, 0f);
            leftPagePanel.Top.Set(10, 0f);
            leftPagePanel.BackgroundColor = new Color(255, 252, 245);
            leftPagePanel.BorderColor = new Color(200, 180, 140);
            leftPagePanel.SetPadding(15);
            contentPanel.Append(leftPagePanel);

            rightPagePanel = new UIPanel();
            rightPagePanel.Width.Set(435, 0f);
            rightPagePanel.Height.Set(470, 0f);
            rightPagePanel.Left.Set(455, 0f);
            rightPagePanel.Top.Set(10, 0f);
            rightPagePanel.BackgroundColor = new Color(255, 252, 245);
            rightPagePanel.BorderColor = new Color(200, 180, 140);
            rightPagePanel.SetPadding(15);
            contentPanel.Append(rightPagePanel);
        }

        private void CreatePageControls()
        {
            var controlPanel = new UIPanel();
            controlPanel.Width.Set(900, 0f);
            controlPanel.Height.Set(50, 0f);
            controlPanel.HAlign = 0.5f;
            controlPanel.Top.Set(630, 0f);
            controlPanel.BackgroundColor = Color.Transparent;
            controlPanel.BorderColor = Color.Transparent;
            mainPanel.Append(controlPanel);

            prevPageText = new UIText("上一页", 1f);
            prevPageText.Left.Set(50, 0f);
            prevPageText.VAlign = 0.5f;
            prevPageText.TextColor = Color.White * 0.8f;
            prevPageText.OnMouseOver += (evt, element) => {
                ((UIText)element).TextColor = Color.White * 1.0f;
            };
            prevPageText.OnMouseOut += (evt, element) => {
                ((UIText)element).TextColor = Color.White * 0.8f;
            };
            prevPageText.OnLeftClick += PrevPageClick;
            controlPanel.Append(prevPageText);

            pageIndicator = new UIText("第 1 页 / 共 1 页", 1f);
            pageIndicator.HAlign = 0.5f;
            pageIndicator.VAlign = 0.5f;
            pageIndicator.TextColor = Color.White * 0.9f;
            controlPanel.Append(pageIndicator);

            nextPageText = new UIText("下一页", 1f);
            nextPageText.Left.Set(-50, 1f);
            nextPageText.VAlign = 0.5f;
            nextPageText.TextColor = Color.White * 0.8f;
            nextPageText.OnMouseOver += (evt, element) => {
                ((UIText)element).TextColor = Color.White * 1.0f;
            };
            nextPageText.OnMouseOut += (evt, element) => {
                ((UIText)element).TextColor = Color.White * 0.8f;
            };
            nextPageText.OnLeftClick += NextPageClick;
            controlPanel.Append(nextPageText);
        }

        private void SwitchSection(BookSection section)
        {
            catalogTabBtn.IsSelected = (section == BookSection.Catalog);
            storiesTabBtn.IsSelected = (section == BookSection.Stories);
            wishesTabBtn.IsSelected = (section == BookSection.Wishes);

            currentSection = section;
            currentPage = 0;
            DisplayCurrentPage();
        }

        public void DisplayCurrentPage()
        {
            leftPagePanel.RemoveAllChildren();
            rightPagePanel.RemoveAllChildren();

            switch (currentSection)
            {
                case BookSection.Catalog:
                    DisplayCatalogPage();
                    totalPages = 1;
                    break;

                case BookSection.Stories:
                    DisplayStoriesPage();
                    totalPages = 5;
                    break;

                case BookSection.Wishes:
                    DisplayWishesPage();
                    totalPages = 1;
                    break;
            }

            UpdatePageIndicator();
            UpdatePageButtons();
        }

        private void DisplayCatalogPage()
        {
            var leftTitle = new UIText("目 录", 1.2f);
            leftTitle.HAlign = 0.5f;
            leftTitle.Top.Set(10, 0f);
            leftTitle.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(leftTitle);

            float top = 60f;
            string[] chapters = {
                "第一章：《陌生的情节，叠上熟悉的侧脸》",
                "第二章：《破碎的棱镜》",
                "第三章：《重逢，是故事最确信的注脚》",
                "第四章：《重拾破碎的棱镜》",
                "未完待续......"
            };

            foreach (var chapter in chapters)
            {
                var chapterText = new UIText(chapter, 0.9f);
                chapterText.Top.Set(top, 0f);
                chapterText.Left.Set(20, 0f);
                chapterText.TextColor = Color.White * 0.8f;
                leftPagePanel.Append(chapterText);
                top += 40f;
            }

            var rightTitle = new UIText("点击左侧书签", 1f);
            rightTitle.HAlign = 0.5f;
            rightTitle.Top.Set(100, 0f);
            rightTitle.TextColor = Color.White * 0.9f;
            rightPagePanel.Append(rightTitle);

            var rightSubtitle = new UIText("可快速跳转至对应章节", 0.9f);
            rightSubtitle.HAlign = 0.5f;
            rightSubtitle.Top.Set(140, 0f);
            rightSubtitle.TextColor = Color.White * 0.7f;
            rightPagePanel.Append(rightSubtitle);
        }

        private void DisplayStoriesPage()
        {
            switch (currentPage)
            {
                case 0:
                    DisplayStoryBefore();
                    break;
                case 1:
                    DisplayChapter1();
                    break;
                case 2:
                    DisplayChapter2();
                    break;
                case 3:
                    DisplayChapter3();
                    break;
                case 4:
                    DisplayChapter4();
                    break;
            }
        }

        private void DisplayStoryBefore()
        {
            var title1 = new UIText("故事之前：", 1f);
            title1.Top.Set(10, 0f);
            title1.Left.Set(10, 0f);
            title1.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(title1);

            var subtitle1 = new UIText("《续写于扉页的起点》", 0.9f);
            subtitle1.Top.Set(40, 0f);
            subtitle1.Left.Set(30, 0f);
            subtitle1.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(subtitle1);

            float top = 80f;
            string[] lines1 = {
                "从前 有一朵含苞待放的水晶花",
                "它透明 纯净 能折射光的十二种色彩",
                "一只小妖精好奇 如此绚丽的花朵",
                "绽放时该有多美？",
                "于是呀 它守在水晶旁 等呀等...",
                "直到有一天 或许是风儿的恶作剧吧",
                "水晶花掉在地上 碎成了无数片",
                "小妖精一定很难过吧？",
                "可当它低下头 却惊讶的发现",
                "碎片里有一团光",
                "这团光 映射出世界的光彩",
                "它依旧绚丽又夺目"
            };

            foreach (var line in lines1)
            {
                var lineText = new UIText(line, 0.85f);
                lineText.Top.Set(top, 0f);
                lineText.Left.Set(20, 0f);
                lineText.TextColor = Color.White * 0.8f;
                leftPagePanel.Append(lineText);
                top += 30f;
            }

            var title1Right = new UIText("「爱」是一朵清白的花", 1f);
            title1Right.HAlign = 0.5f;
            title1Right.Top.Set(10, 0f);
            title1Right.TextColor = Color.White * 0.9f;
            rightPagePanel.Append(title1Right);

            top = 50f;
            string[] lines1Right = {
                "它悄无声息的长大，",
                "让每一位路过的人都心生憧憬。",
                "",
                "可是，花朵是无法自己走向天地的......",
                "",
                "除非有人将「爱」栽种于心，",
                "带上它前往明天"
            };

            foreach (var line in lines1Right)
            {
                if (string.IsNullOrEmpty(line))
                {
                    top += 15f;
                    continue;
                }
                var lineText = new UIText(line, 0.9f);
                lineText.Top.Set(top, 0f);
                lineText.HAlign = 0.5f;
                lineText.TextColor = Color.White * 0.8f;
                rightPagePanel.Append(lineText);
                top += 35f;
            }
        }

        private void DisplayChapter1()
        {
            var title2 = new UIText("第一章：", 1f);
            title2.Top.Set(10, 0f);
            title2.Left.Set(10, 0f);
            title2.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(title2);

            var subtitle2 = new UIText("《陌生的情节，叠上熟悉的侧脸》", 0.9f);
            subtitle2.Top.Set(40, 0f);
            subtitle2.Left.Set(30, 0f);
            subtitle2.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(subtitle2);

            var storyText1 = new UIText("在与第一个大怪物遭遇时，", 0.9f);
            storyText1.Top.Set(100, 0f);
            storyText1.Left.Set(20, 0f);
            storyText1.TextColor = Color.White * 0.8f;
            leftPagePanel.Append(storyText1);

            var storyText2 = new UIText("有一个小小的粉色的身影出现了。", 0.9f);
            storyText2.Top.Set(135, 0f);
            storyText2.Left.Set(20, 0f);
            storyText2.TextColor = Color.White * 0.8f;
            leftPagePanel.Append(storyText2);

            var quote1 = new UIText("\"memi！memi！", 0.95f);
            quote1.Top.Set(185, 0f);
            quote1.HAlign = 0.5f;
            quote1.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(quote1);

            var quote2 = new UIText("（伙伴！一起！）\"", 0.95f);
            quote2.Top.Set(220, 0f);
            quote2.HAlign = 0.5f;
            quote2.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(quote2);
        }

        private void DisplayChapter2()
        {
            var title = new UIText("第二章：", 1f);
            title.Top.Set(10, 0f);
            title.Left.Set(10, 0f);
            title.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(title);

            var subtitle = new UIText("《破碎的棱镜》", 0.9f);
            subtitle.Top.Set(40, 0f);
            subtitle.Left.Set(30, 0f);
            subtitle.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(subtitle);
        }

        private void DisplayChapter3()
        {
            var title = new UIText("第三章：", 1f);
            title.Top.Set(10, 0f);
            title.Left.Set(10, 0f);
            title.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(title);

            var subtitle = new UIText("《重逢，是故事最确信的注脚》", 0.9f);
            subtitle.Top.Set(40, 0f);
            subtitle.Left.Set(30, 0f);
            subtitle.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(subtitle);
        }

        private void DisplayChapter4()
        {
            var title = new UIText("第四章：", 1f);
            title.Top.Set(10, 0f);
            title.Left.Set(10, 0f);
            title.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(title);

            var subtitle = new UIText("《重拾破碎的棱镜》", 0.9f);
            subtitle.Top.Set(40, 0f);
            subtitle.Left.Set(30, 0f);
            subtitle.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(subtitle);
        }

        private void DisplayWishesPage()
        {
            leftPagePanel.RemoveAllChildren();
            rightPagePanel.RemoveAllChildren();

            // 获取当前玩家
            Player player = Main.LocalPlayer;
            var wishDataPlayer = player.GetModPlayer<PlayerWishData>();

            // 左侧页面标题
            var leftTitle = new UIText("迷迷的心愿单", 1.2f);
            leftTitle.HAlign = 0.5f;
            leftTitle.Top.Set(10, 0f);
            leftTitle.TextColor = Color.White * 0.9f;
            leftPagePanel.Append(leftTitle);

            // 获取所有世界模板
            var allTemplates = BookDataSystem.WishTemplates;

            // 显示已解锁的心愿
            float topPosition = 60f;
            bool hasUnlockedWish = false;

            foreach (var template in allTemplates)
            {
                // 检查是否对当前世界解锁
                if (!BookDataSystem.IsWishUnlockedForPlayer(template.WishId))
                    continue; // 未解锁，跳过

                hasUnlockedWish = true;

                // 获取玩家记录
                var playerRecord = wishDataPlayer.GetOrCreateRecord(template.WishId);

                // 创建容器
                var wishContainer = new UIPanel();
                wishContainer.Width.Set(380, 0f);
                wishContainer.Height.Set(32, 0f);
                wishContainer.Left.Set(10, 0f);
                wishContainer.Top.Set(topPosition, 0f);
                wishContainer.BackgroundColor = Color.Transparent;
                wishContainer.BorderColor = Color.Transparent;
                wishContainer.SetPadding(5);

                // 根据玩家记录决定显示
                string displayText = template.Title;
                Color textColor = Color.Gray; // 虚体（灰色）

                if (playerRecord.IsWritten && !playerRecord.IsCompleted)
                {
                    displayText = "○ " + template.Title; // 已写下（蓝紫色+○）
                    textColor = new Color(180, 130, 255);
                    wishContainer.BackgroundColor = new Color(50, 50, 50, 30);
                }
                else if (playerRecord.IsCompleted)
                {
                    displayText = "✓ " + template.Title; // 已完成（蓝紫色+✓）
                    textColor = new Color(180, 130, 255);
                    wishContainer.BackgroundColor = new Color(50, 50, 50, 30);
                }

                // 文本显示
                var wishText = new UIText(displayText, 0.9f);
                wishText.TextColor = textColor;
                wishText.VAlign = 0.5f;
                wishText.Left.Set(10, 0f);
                wishContainer.Append(wishText);

                // 交互逻辑
                if (!playerRecord.IsWritten)
                {
                    // 虚体 -> 需要手持记忆之笔才能写下
                    wishContainer.OnMouseOver += (evt, element) => {
                        ((UIPanel)element).BackgroundColor = new Color(100, 100, 100, 80);
                        wishText.TextColor = Color.LightBlue;
                    };
                    wishContainer.OnMouseOut += (evt, element) => {
                        ((UIPanel)element).BackgroundColor = Color.Transparent;
                        wishText.TextColor = Color.Gray;
                    };
                    wishContainer.OnLeftClick += (evt, element) => {
                        if (IsHoldingQuill())
                        {
                            if (BookDataSystem.WriteWish(player, template.WishId))
                            {
                                SoundEngine.PlaySound(SoundID.MenuTick);
                                Main.NewText($"记录成功: {template.Title}", Color.LightBlue);
                                DisplayWishesPage(); // 刷新
                            }
                            else
                            {
                                // 如果无法写下（可能已经写下过了）
                                Main.NewText("这份心愿已经记下了", Color.Yellow);
                            }
                        }
                        else
                        {
                            // 提示需要手持记忆之笔
                            Main.NewText("需要手持记忆之笔才能记录心愿", Color.Yellow);
                        }
                    };
                }
                else if (playerRecord.IsWritten && !playerRecord.IsCompleted)
                {
                    // 已写下 -> 检查任务是否完成，然后可点击完成
                    var quest = MimiQuestSystem.GetQuest(template.RequiredQuest);
                    if (quest != null && quest.IsCompleted)
                    {
                        // 任务已完成，可点击完成
                        wishContainer.OnMouseOver += (evt, element) => {
                            ((UIPanel)element).BackgroundColor = new Color(100, 100, 50, 80);
                            wishText.TextColor = Color.Yellow;
                        };
                        wishContainer.OnMouseOut += (evt, element) => {
                            ((UIPanel)element).BackgroundColor = new Color(50, 50, 50, 30);
                            wishText.TextColor = new Color(180, 130, 255);
                        };
                        wishContainer.OnLeftClick += (evt, element) => {
                            if (BookDataSystem.CompleteWish(player, template.WishId))
                            {
                                SoundEngine.PlaySound(SoundID.Item29);
                                DisplayWishesPage(); // 刷新
                            }
                            else
                            {
                                // 如果无法完成（可能任务未完成）
                                Main.NewText("这份心愿尚未完成...", Color.LightBlue);
                            }
                        };
                    }
                }
                else if (playerRecord.IsCompleted)
                {
                    // 已完成 -> 轻微闪烁效果
                    wishContainer.OnUpdate += (element) => {
                        float glow = (float)(Math.Sin(Main.GameUpdateCount * 0.1f) * 0.2f + 0.8f);
                        wishText.TextColor = new Color(180, 130, 255) * glow;
                    };
                }

                leftPagePanel.Append(wishContainer);
                topPosition += 38f;
            }

            // 如果没有解锁任何心愿
            if (!hasUnlockedWish)
            {
                var emptyText = new UIText("（完成迷迷的任务，这里会出现她的心愿...）", 0.9f);
                emptyText.HAlign = 0.5f;
                emptyText.VAlign = 0.5f;
                emptyText.Top.Set(200, 0f);
                emptyText.TextColor = Color.Gray;
                leftPagePanel.Append(emptyText);
            }

            // === 修改右侧页面：移除生硬说明，改为诗意的提示 ===

            // 检查是否有已解锁的心愿
            bool hasAnyWish = false;
            foreach (var template in BookDataSystem.WishTemplates)
            {
                if (BookDataSystem.IsWishUnlockedForPlayer(template.WishId))
                {
                    hasAnyWish = true;
                    break;
                }
            }

            if (hasAnyWish)
            {
                // 如果有解锁的心愿，显示简短的说明
                var rightTitle = new UIText("用心铭记", 1.0f);
                rightTitle.HAlign = 0.5f;
                rightTitle.Top.Set(100, 0f);
                rightTitle.TextColor = Color.White * 0.8f;
                rightPagePanel.Append(rightTitle);

                var rightSubtitle = new UIText("每一份心愿都值得被珍视", 0.85f);
                rightSubtitle.HAlign = 0.5f;
                rightSubtitle.Top.Set(140, 0f);
                rightSubtitle.TextColor = Color.White * 0.6f;
                rightPagePanel.Append(rightSubtitle);
            }
            else
            {
                // 如果没有解锁任何心愿，显示诗意的等待提示
                var rightTitle = new UIText("时间的书页", 1.0f);
                rightTitle.HAlign = 0.5f;
                rightTitle.Top.Set(80, 0f);
                rightTitle.TextColor = Color.White * 0.8f;
                rightPagePanel.Append(rightTitle);

                float top = 120f;
                string[] lines = {
                    "旅途尚未开始，",
                    "故事仍在沉睡。",
                    "",
                    "完成迷迷的委托，",
                    "这里会浮现她的心愿...",
                    "",
                    "每一份记录都是",
                    "时光的馈赠。"
                };

                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        top += 10f;
                        continue;
                    }
                    var lineText = new UIText(line, 0.85f);
                    lineText.HAlign = 0.5f;
                    lineText.Top.Set(top, 0f);
                    lineText.TextColor = Color.White * (string.IsNullOrEmpty(line) ? 0.5f : 0.6f);
                    rightPagePanel.Append(lineText);
                    top += 30f;
                }
            }
        }

        // 检查是否手持记忆之笔
        private bool IsHoldingQuill()
        {
            Player player = Main.LocalPlayer;

            try
            {
                // 检查是否手持记忆之笔
                // 假设记忆之笔的 ModItem 类型名为 "RemembrancersQuill"
                // 需要确保路径正确
                int quillType = ModContent.ItemType<RemembrancersQuill>();

                if (quillType > 0 && player.HeldItem.type == quillType)
                {
                    return true;
                }
            }
            catch
            {
                // 如果找不到记忆之笔物品，返回 true 允许测试
                return true;
            }

            return false;
        }

        private void UpdatePageIndicator()
        {
            string sectionName = currentSection switch
            {
                BookSection.Catalog => "目录",
                BookSection.Stories => "故事",
                BookSection.Wishes => "心愿单",
                _ => "未知"
            };

            pageIndicator?.SetText($"{sectionName} - 第 {currentPage + 1} 页 / 共 {totalPages} 页");
        }

        private void UpdatePageButtons()
        {
            if (prevPageText == null || nextPageText == null)
                return;

            prevPageText.TextColor = currentPage > 0 ? Color.White * 0.8f : Color.Gray * 0.3f;
            nextPageText.TextColor = currentPage < totalPages - 1 ? Color.White * 0.8f : Color.Gray * 0.3f;
        }

        private void PrevPageClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (currentPage > 0)
            {
                currentPage--;
                DisplayCurrentPage();
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        }

        private void NextPageClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                DisplayCurrentPage();
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // ESC键退出
            if (Main.keyState.IsKeyDown(Keys.Escape) &&
                !Main.oldKeyState.IsKeyDown(Keys.Escape))
            {
                UISystem.CloseBookUI();
            }

            if (ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }
    }
}