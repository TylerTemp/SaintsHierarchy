using System.Collections.Generic;
using System.Linq;
using SaintsHierarchy.Editor.Core.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.Core.UIElement.TreeDropdown
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class TreeRowValueElement: TreeRowAbsElement
    {
        public readonly UnityEvent OnClickedEvent = new UnityEvent();

        public bool IsOn { get; private set; }
        private static VisualTreeAsset _treeRowTemplate;
        // private static VisualTreeAsset _treeRowIndentIconTemplate;

        // public VisualElement MainButton;
        private readonly VisualElement _selectionIcon;

        // private readonly string _labelLow;
        private readonly HashSet<string> _searches = new HashSet<string>();

        // ReSharper disable once MemberCanBePrivate.Global
        public TreeRowValueElement(): this(null, 0)
        {
        }

        private static Texture2D _checkedIcon;


        public TreeRowValueElement(string label, int indent)
        {
            _treeRowTemplate ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/TreeDropdown/TreeRow.uxml");
            VisualElement treeRow = _treeRowTemplate.CloneTree();

            treeRow.Q<VisualElement>("saintsfield-tree-row-foldout").RemoveFromHierarchy();

            VisualElement mainButton = treeRow.Q<VisualElement>("saintsfield-tree-row");

            mainButton.AddManipulator(new Clickable(_ => OnClickedEvent.Invoke()));

            _selectionIcon = treeRow.Q<VisualElement>("saintsfield-tree-row-selection");
            if (!_checkedIcon)
            {
                _checkedIcon = Util.LoadResource<Texture2D>("check.png");
            }
            _selectionIcon.style.backgroundImage = _checkedIcon;

            // VisualElement root = treeRow.Q<VisualElement>("saintsfield-tree-row");
            if (indent > 0)
            {
                // _treeRowIndentIconTemplate ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/TreeDropdown/TreeRowIndentIcon.uxml");
                VisualElement indentContainer = treeRow.Q<VisualElement>("saintsfield-tree-row-indent");
                for (int indentIndex = 0; indentIndex < indent; indentIndex++)
                {
                    TemplateContainer clone = TreeIndentUtil.MakeIndentElement(indentIndex);
                    indentContainer.Add(clone);
                }
            }

            Label labelElement = treeRow.Q<Label>("saintsfield-tree-row-label");
            if (!string.IsNullOrEmpty(label))
            {
                // _labelLow = label.ToLower();
                _searches.Add(label.ToLower());
                labelElement.text = label;
                foreach (Label subLabel in labelElement.Query<Label>().ToList())
                {
                    subLabel.style.whiteSpace = WhiteSpace.NoWrap;
                }
                // labelElement.text = label;
            }
            RefreshIcon();

            Add(treeRow);
        }

        public override int HasValueCount => IsOn? 1: 0;

        public void SetValueOn(bool isOn)
        {
            if (IsOn == isOn)
            {
                return;
            }

            IsOn = isOn;
            OnHasValueCountChanged.Invoke(HasValueCount);
            SetHighlight(IsOn);

            RefreshIcon();
        }

        private void RefreshIcon()
        {
            _selectionIcon.style.visibility = IsOn ? Visibility.Visible : Visibility.Hidden;
        }

        private bool _shown = true;
        private bool _shownAsChild = true;

        public void AddSearches(ICollection<string> searches)
        {
            _searches.UnionWith(searches);
        }

        public override bool OnSearch(IReadOnlyList<string> searchTokens)
        {
            if (searchTokens.Count == 0)
            {
                SetDisplay(DisplayStyle.Flex);
                _shown = true;
                return true;
            }

            if (_searches.Count == 0)
            {
                SetDisplay(DisplayStyle.None);
                _shown = false;
                return false;
            }

            foreach (string toSearchLower in searchTokens)
            {
                bool anyMatched = _searches.Any(each => each.Contains(toSearchLower));
                if (!anyMatched)
                {
                    SetDisplay(DisplayStyle.None);
                    _shown = false;
                    return false;
                }
            }

            SetDisplay(DisplayStyle.Flex);
            _shown = true;
            return true;
        }

        public override bool Navigateable
        {
            get => _shown && _shownAsChild && enabledSelf;
            set => _shownAsChild = value;
        }

        public override string ToString()
        {
            return $"<TreeRowValue search={string.Join("/", _searches)} nav={Navigateable}/>";
        }
    }
}
