using SaintsHierarchy.Editor.Core.Utils;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.Core.UIElement
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class DropdownButtonElement : VisualElement
    {
#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<DropdownButtonElement, UxmlTraits> { }
#endif

        private static VisualTreeAsset _template;
        public readonly Button Button;
        private readonly Label Label;

        public DropdownButtonElement() : this("")
        {
        }

        public DropdownButtonElement(string label)
        {
            _template ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/DropdownButton.uxml");
            _template.CloneTree(this);
            Button = this.Q<Button>("dropdown-button");
            Label = Button.Q<Label>("dropdown-label");
            SetLabel(label);
        }

        public void SetLabel(string label)
        {
            Label.text = label;
        }
    }
}
