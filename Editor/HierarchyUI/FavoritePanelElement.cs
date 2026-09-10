using SaintsHierarchy.Editor.Core.Utils;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public class FavoritePanelElement : VisualElement
    {
        private static VisualTreeAsset _template;

        public FavoritePanelElement()
        {
            // AddToClassList("saints-hierarchy-favorite-panel");
            _template ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/Favorite/FavoritePanel.uxml");
            _template.CloneTree(this);
        }
    }
}
