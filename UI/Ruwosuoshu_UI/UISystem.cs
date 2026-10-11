using Amphoreus.Systems.Ruwosuoshu_Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace Amphoreus.UI.Ruwosuoshu
{
    [Autoload(Side = ModSide.Client)]
    public class UISystem : ModSystem
    {
        internal UserInterface bookInterface;
        internal BookUI.BookUIState bookUI;
        internal BookButtonUI bookButtonUI;

        public static Vector2 BookIconPosition = new Vector2(Main.screenWidth / 2, 100); // 屏幕中心上方
        public static bool IsBookIconVisible = true;

        private bool isDragging = false;
        private bool canStartDrag = false;
        private Vector2 dragOffset = Vector2.Zero;
        private float rightMouseHoldTime = 0f;
        private const float DRAG_START_DELAY = 0.1f;

        private float iconBrightness = 0.6f;
        private float iconScale = 1.0f;
        private bool isMouseOver = false;
        private bool leftMousePressedOnIcon = false;

        private float rainbowHue = 0f;
        private const float RAINBOW_SPEED = 0.5f;
        private float glowIntensity = 0f;

        // 保存背包状态
        private static bool wasInventoryOpen = false;

        internal static Texture2D BookIconTexture;
        internal static Texture2D BookBackgroundTexture;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                try
                {
                    BookIconTexture = ModContent.Request<Texture2D>("Amphoreus/UI/Ruwosuoshu_UI/BookUI/BookIcon", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }
                catch (Exception ex)
                {
                    BookIconTexture = TextureAssets.Item[ItemID.Book].Value;
                    Mod.Logger.Warn($"无法加载BookIcon贴图: {ex.Message}");
                }

                try
                {
                    BookBackgroundTexture = ModContent.Request<Texture2D>("Amphoreus/UI/Ruwosuoshu_UI/BookUI/BookBackground", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }
                catch (Exception ex)
                {
                    BookBackgroundTexture = TextureAssets.MagicPixel.Value;
                    Mod.Logger.Warn($"无法加载BookBackground贴图: {ex.Message}");
                }

                bookUI = new BookUI.BookUIState();
                bookUI.Activate();

                bookButtonUI = new BookButtonUI();
                bookButtonUI.Activate();

                bookInterface = new UserInterface();
                bookInterface.SetState(bookButtonUI);
            }
        }

        public override void Unload()
        {
            BookIconTexture = null;
            BookBackgroundTexture = null;
            bookUI = null;
            bookButtonUI = null;
            bookInterface = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (bookInterface?.CurrentState != null)
            {
                bookInterface.Update(gameTime);
            }

            UpdateRainbowEffect(gameTime);

            if (Main.playerInventory && IsBookIconVisible)
            {
                HandleIconEffects(gameTime);
                HandleIconDragging(gameTime);
            }
        }

        private void UpdateRainbowEffect(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            rainbowHue += RAINBOW_SPEED * deltaTime;
            if (rainbowHue > 1f)
                rainbowHue -= 1f;
        }

        private Color GetRainbowColor()
        {
            return Main.hslToRgb(rainbowHue, 1f, 0.5f);
        }

        private void HandleIconEffects(GameTime gameTime)
        {
            Rectangle iconRect = new Rectangle(
                (int)BookIconPosition.X - 20,
                (int)BookIconPosition.Y - 20,
                40, 40);

            isMouseOver = iconRect.Contains(Main.MouseScreen.ToPoint());

            if (!isDragging)
            {
                float targetBrightness = isMouseOver ? 1.0f : 0.6f;
                iconBrightness = MathHelper.Lerp(iconBrightness, targetBrightness, 0.2f);

                float targetScale = isMouseOver ? 1.1f : 1.0f;
                iconScale = MathHelper.Lerp(iconScale, targetScale, 0.15f);

                float targetGlow = isMouseOver ? 1.0f : 0.3f;
                glowIntensity = MathHelper.Lerp(glowIntensity, targetGlow, 0.15f);
            }
            else
            {
                iconBrightness = 1.0f;
                iconScale = 1.15f;
                glowIntensity = 1.0f;
            }

            if (isMouseOver && Main.mouseLeft && !leftMousePressedOnIcon && !isDragging)
            {
                leftMousePressedOnIcon = true;
            }

            if (leftMousePressedOnIcon && !Main.mouseLeft)
            {
                if (isMouseOver && !isDragging)
                {
                    OpenBookUI();
                    SoundEngine.PlaySound(SoundID.MenuOpen);

                    if (isDragging)
                    {
                        EndDragging();
                    }
                }
                leftMousePressedOnIcon = false;
            }

            if (!isMouseOver && leftMousePressedOnIcon)
            {
                leftMousePressedOnIcon = false;
            }
        }

        private void HandleIconDragging(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Rectangle iconRect = new Rectangle(
                (int)BookIconPosition.X - 20,
                (int)BookIconPosition.Y - 20,
                40, 40);

            if (iconRect.Contains(Main.MouseScreen.ToPoint()) && Main.mouseRight)
            {
                rightMouseHoldTime += deltaTime;

                if (rightMouseHoldTime >= DRAG_START_DELAY && !isDragging)
                {
                    isDragging = true;
                    canStartDrag = true;
                    dragOffset = Main.MouseScreen - BookIconPosition;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }
            else if (!Main.mouseRight)
            {
                rightMouseHoldTime = 0f;
            }

            if (isDragging && canStartDrag)
            {
                if (Main.mouseRight)
                {
                    BookIconPosition = Main.MouseScreen - dragOffset;
                    BookIconPosition.X = MathHelper.Clamp(BookIconPosition.X, 20, Main.screenWidth - 60);
                    BookIconPosition.Y = MathHelper.Clamp(BookIconPosition.Y, 20, Main.screenHeight - 60);
                    Main.LocalPlayer.mouseInterface = true;

                    // 立即保存位置
                    SaveIconPosition();
                }
                else
                {
                    EndDragging();
                }
            }
        }

        private void EndDragging()
        {
            isDragging = false;
            canStartDrag = false;
            rightMouseHoldTime = 0f;
            SoundEngine.PlaySound(SoundID.MenuTick);
            SaveIconPosition();
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");
            if (inventoryIndex != -1)
            {
                layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                    "Amphoreus: Book Icon",
                    delegate
                    {
                        DrawBookIcon();
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(inventoryIndex + 2, new LegacyGameInterfaceLayer(
                    "Amphoreus: Book UI",
                    delegate
                    {
                        if (bookInterface?.CurrentState != null)
                        {
                            bookInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        private void DrawBookIcon()
        {
            if (!IsBookIconVisible || !Main.playerInventory)
                return;

            Vector2 drawPos = BookIconPosition;
            Vector2 origin = new Vector2(BookIconTexture.Width, BookIconTexture.Height) * 0.5f;

            Color rainbowColor = GetRainbowColor();
            Color baseColor = Color.White * iconBrightness;

            if (leftMousePressedOnIcon && !isDragging)
            {
                baseColor *= 0.8f;
            }

            if (isDragging)
            {
                baseColor = Color.White * 0.7f;
                baseColor *= 0.7f;

                float pulse = (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 10f) * 0.3f + 0.7f);
                Color dragGlowColor = rainbowColor * pulse * 0.9f;

                float ringRadius = 28 * iconScale;
                for (int i = 0; i < 13; i++)
                {
                    float angle = MathHelper.TwoPi * i / 13f + (float)Main.GlobalTimeWrappedHourly * 3f;
                    Vector2 ringPos = drawPos + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * ringRadius;

                    float segmentPulse = (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 8f + i * 0.5f) * 0.5f + 0.5f);
                    Color segmentColor = rainbowColor * segmentPulse * 0.8f;

                    Main.spriteBatch.Draw(
                        TextureAssets.MagicPixel.Value,
                        new Rectangle((int)ringPos.X - 3, (int)ringPos.Y - 3, 6, 6),
                        segmentColor);
                }
            }

            if (glowIntensity > 0.01f)
            {
                Color glowColor = rainbowColor * glowIntensity * 0.8f;

                for (int i = 0; i < 8; i++)
                {
                    float angle = MathHelper.TwoPi * i / 8f;
                    Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 2f * iconScale;

                    Main.spriteBatch.Draw(
                        BookIconTexture,
                        drawPos + offset,
                        null,
                        glowColor,
                        0f,
                        origin,
                        iconScale,
                        SpriteEffects.None,
                        0f);
                }

                Color innerGlowColor = rainbowColor * glowIntensity * 0.5f;
                for (int i = 0; i < 4; i++)
                {
                    float angle = MathHelper.TwoPi * i / 4f;
                    Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 1f * iconScale;

                    Main.spriteBatch.Draw(
                        BookIconTexture,
                        drawPos + offset,
                        null,
                        innerGlowColor,
                        0f,
                        origin,
                        iconScale,
                        SpriteEffects.None,
                        0f);
                }
            }

            Main.spriteBatch.Draw(
                BookIconTexture,
                drawPos,
                null,
                baseColor,
                0f,
                origin,
                iconScale,
                SpriteEffects.None,
                0f);

            if (rightMouseHoldTime > 0 && rightMouseHoldTime < DRAG_START_DELAY)
            {
                float progress = rightMouseHoldTime / DRAG_START_DELAY;
                Vector2 progressPos = drawPos + new Vector2(0, 25);

                Main.spriteBatch.Draw(
                    TextureAssets.MagicPixel.Value,
                    new Rectangle((int)progressPos.X - 15, (int)progressPos.Y - 2, 30, 4),
                    Color.Black * 0.7f);

                Main.spriteBatch.Draw(
                    TextureAssets.MagicPixel.Value,
                    new Rectangle((int)progressPos.X - 14, (int)progressPos.Y - 1, (int)(28 * progress), 2),
                    rainbowColor);
            }

            Rectangle iconRect = new Rectangle(
                (int)BookIconPosition.X - 20,
                (int)BookIconPosition.Y - 20,
                40, 40);

            if (iconRect.Contains(Main.MouseScreen.ToPoint()))
            {
                Main.LocalPlayer.mouseInterface = true;

                string tooltip = isDragging ?
                    "拖拽中... 松开右键放置" :
                    "如我所书\n左键点击打开 - 右键长按拖动";
                Main.instance.MouseText(tooltip, "", 0, 0, -1, -1, -1, -1, -1);
            }
        }

        public void OpenBookUI()
        {
            if (bookInterface?.CurrentState is BookButtonUI)
            {
                wasInventoryOpen = Main.playerInventory;
                Main.playerInventory = false;
                bookInterface.SetState(bookUI);

                // 强制刷新所有页面
                if (bookUI != null)
                {
                    bookUI.RefreshAllPages();
                }

                SoundEngine.PlaySound(SoundID.MenuOpen);
            }
        }

        public static void CloseBookUI()
        {
            var uiSystem = ModContent.GetInstance<UISystem>();
            if (uiSystem.bookInterface?.CurrentState is BookUI.BookUIState)
            {
                uiSystem.bookInterface.SetState(uiSystem.bookButtonUI);
                SoundEngine.PlaySound(SoundID.MenuClose);
                Main.playerInventory = wasInventoryOpen;
            }
        }

        private void SaveIconPosition()
        {
            // 直接设置到BookDataSystem
            BookDataSystem.BookIconPosition = BookIconPosition;
        }

        private void LoadIconPosition()
        {
            var dataSystem = ModContent.GetInstance<BookDataSystem>();
            if (dataSystem != null && BookDataSystem.BookIconPosition != Vector2.Zero)
            {
                BookIconPosition = BookDataSystem.BookIconPosition;
            }
            else
            {
                // 使用屏幕高度的10%作为顶部位置
                // 这样可以适应不同分辨率
                float x = Main.screenWidth / 2;
                float y = Main.screenHeight * 0.1f; // 屏幕高度的10%处

                BookIconPosition = new Vector2(x, y);
            }
        }

        public override void PostSetupContent()
        {
            LoadIconPosition();
        }

        public static void ResetIconPosition()
        {
            BookIconPosition = new Vector2(Main.screenWidth / 2, Main.screenHeight / 2);
        }
    }
}