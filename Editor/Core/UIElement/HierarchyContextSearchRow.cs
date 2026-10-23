using SaintsHierarchy.Editor.Core.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.Core.UIElement
{
    public class HierarchyContextSearchRow : VisualElement
    {
        private readonly Button _searchStartButton;
        public readonly ToolbarSearchField ToolbarSearchField;

        internal bool IsSearching
        {
            get; private set;
        }

        public HierarchyContextSearchRow()
        {
            VisualTreeAsset template = Util.LoadResource<VisualTreeAsset>("UIToolkit/HierarchyContextSearchRow.uxml");
            template.CloneTree(this);
            this.Q<Image>("icon").image = Util.LoadResource<Texture2D>("d_Search Icon");

            _searchStartButton = this.Q<Button>("search-start-button");
            _searchStartButton.EnableInClassList("project-context-row--dark", EditorGUIUtility.isProSkin);
            _searchStartButton.clicked += OnSearchStartButtonClicked;
            this.Q<Label>("shortcut").text = Application.platform == RuntimePlatform.OSXEditor ? "Cmd+F" : "Ctrl+F";

            ToolbarSearchField = this.Q<ToolbarSearchField>();
            ToolbarSearchField.style.display = DisplayStyle.None;
        }

        private void OnSearchStartButtonClicked() => SetSearchOpen(true);

        internal void ToggleSearch() => SetSearchOpen(!IsSearching);

        private void SetSearchOpen(bool open)
        {
            // Notify the popup so clearing search also restores the original menu.
            ToolbarSearchField.value = string.Empty;
            IsSearching = open;
            _searchStartButton.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;
            ToolbarSearchField.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open)
            {
                ToolbarSearchField.Focus();
            }
        }
    }
}
