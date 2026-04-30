using SaintsHierarchy.Editor.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.UIElement
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class ItemButtonElement: VisualElement
    {
#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<ItemButtonElement, UxmlTraits> { }
#endif

        private static VisualTreeAsset _template;
        public readonly Button Button;

        [UxmlAttribute] public Texture2D Icon;

        public ItemButtonElement()
        {
            _template ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/ItemButton.uxml");
            TemplateContainer root = _template.CloneTree();
            Add(root);
            Button = root.Q<Button>();
            Button.style.backgroundImage = Icon;
        }

        public void SetSelected(bool selected)
        {
            const string className = "ItemButtonSelected";
            if (selected)
            {
                Button.AddToClassList(className);
            }
            else
            {
                Button.RemoveFromClassList(className);
            }
        }
    }
}
