using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.Editor_Default_Resources
{
    public class ItemButtonElement: VisualElement
    {
        private static VisualTreeAsset _template;
        public readonly Button Button;

        public ItemButtonElement()
        {
            _template ??= Utils.LoadResource<VisualTreeAsset>("UIToolkit/ItemButton.uxml");
            TemplateContainer root = _template.CloneTree();
            Add(root);
            Button = root.Q<Button>();
        }
    }
}
