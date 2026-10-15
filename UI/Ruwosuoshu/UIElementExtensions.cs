using System.Linq;
using Terraria.UI;

namespace Amphoreus.UI.Ruwosuoshu
{
    public static class UIElementExtensions
    {
        public static void RemoveAllChildren(this UIElement element)
        {
            if (element != null && element.Children != null)
            {
                // 使用ToList()创建副本，因为我们在遍历时修改集合
                var childrenList = element.Children.ToList();
                foreach (var child in childrenList)
                {
                    element.RemoveChild(child);
                }
            }
        }
    }
}