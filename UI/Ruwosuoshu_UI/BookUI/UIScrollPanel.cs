using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Amphoreus.UI.Ruwosuoshu.BookUI
{
    public class UIScrollPanel : UIPanel
    {
        private UIScrollbar scrollbar;
        private UIElement container;
        private float viewPosition = 0f;

        public UIScrollPanel()
        {
            BorderColor = Color.Transparent;
            BackgroundColor = Color.Transparent;

            scrollbar = new UIScrollbar();
            scrollbar.Width.Set(20, 0f);
            scrollbar.Height.Set(0, 1f);
            scrollbar.Left.Set(-25, 1f);
            scrollbar.SetView(100f, 1000f);
            scrollbar.ViewPosition = 0f;

            base.Append(scrollbar);

            container = new UIElement();
            container.Width.Set(0, 1f);
            container.Height.Set(0, 1f);
            container.SetPadding(0);
            base.Append(container);
        }

        public void AddToContainer(UIElement element)
        {
            container.Append(element);
        }

        public List<UIElement> GetContainerChildren()
        {
            var children = new List<UIElement>();
            foreach (var child in container.Children)
            {
                children.Add(child);
            }
            return children;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var dimensions = GetDimensions();
            var innerDimensions = GetInnerDimensions();

            RasterizerState rasterizerState = new RasterizerState
            {
                CullMode = CullMode.None,
                ScissorTestEnable = true
            };

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None, rasterizerState, null, Main.UIScaleMatrix);

            Rectangle oldRect = spriteBatch.GraphicsDevice.ScissorRectangle;
            spriteBatch.GraphicsDevice.ScissorRectangle = new Rectangle(
                (int)innerDimensions.X, (int)innerDimensions.Y,
                (int)innerDimensions.Width, (int)innerDimensions.Height);

            container.Top.Set(-viewPosition, 0f);
            container.Recalculate();

            foreach (var element in container.Children)
            {
                element.Draw(spriteBatch);
            }

            spriteBatch.GraphicsDevice.ScissorRectangle = oldRect;
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, null, null, Main.UIScaleMatrix);
        }

        public override void Recalculate()
        {
            base.Recalculate();

            float totalHeight = 0f;
            foreach (var element in container.Children)
            {
                totalHeight += element.GetOuterDimensions().Height + 10f;
            }

            scrollbar.SetView(GetInnerDimensions().Height, Math.Max(GetInnerDimensions().Height, totalHeight));
        }
    }
}