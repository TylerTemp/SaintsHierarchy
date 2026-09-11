using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.Core.UIElement.TreeDropdown
{
    public class SaintsTreeDropdownElement: VisualElement
    {
        public readonly UnityEvent<object> OnClickedEvent = new UnityEvent<object>();

        // public readonly UnityEvent<TreeRowAbsElement> ScrollToElementEvent = new UnityEvent<TreeRowAbsElement>();

        private TreeRowAbsElement CurrentFocus { get; set; }

        private readonly IReadOnlyList<TreeRowAbsElement> _flatList;
        private readonly ToolbarSearchField _toolbarSearchField;
        private readonly ScrollView _scrollView;

        public SaintsTreeDropdownElement(AdvancedDropdownMetaInfo metaInfo)
        {
            // VisualElement root = new VisualElement();

            // CleanableTextInputFullWidth cleanableTextInput = new CleanableTextInputFullWidth(null);
            // Add(cleanableTextInput);
            _toolbarSearchField = new ToolbarSearchField
            {
                style =
                {
                    flexGrow = 1,
                    width = StyleKeyword.None,
                },
            };
            Add(_toolbarSearchField);

            TreeRowAbsElement[] treeRowElements = MakeNestedTreeRow(0,
                metaInfo.DropdownListValue,
                metaInfo.CurValue)
                .ToArray();

            ScrollView treeContainer = new ScrollView
            {
                focusable = true,
            };

            List<TreeRowAbsElement> flatList = new List<TreeRowAbsElement>();
            foreach (TreeRowAbsElement treeRow in treeRowElements)
            {
                treeContainer.Add(treeRow);
                foreach (TreeRowAbsElement rowAbsElement in FlatTreeRow(treeRow))
                {
                    flatList.Add(rowAbsElement);
                    switch (rowAbsElement)
                    {
                        case TreeRowValueElement tr:
                            tr.OnClickedEvent.AddListener(() => CurrentFocus = tr);
                            break;
                        case TreeRowFoldoutElement tf:
                            tf.RegisterValueChangedCallback(_ => CurrentFocus = tf);
                            break;
                    }
                }
            }

            _flatList = flatList;

            Add(treeContainer);
            _scrollView = treeContainer;
            treeContainer.RegisterCallback<GeometryChangedEvent>(OnTreeGeometryChanged);

#if UNITY_6000_0_OR_NEWER
            _toolbarSearchField.placeholderText = "Search";
#endif
            _toolbarSearchField.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                if (evt.direction == NavigationMoveEvent.Direction.Down)
                {
                    treeContainer.Focus();
                }
            });

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                _toolbarSearchField.Q<TextField>().Q("unity-text-input").Focus();
                if(CurrentFocus != null)
                {
                    treeContainer.schedule
                        .Execute(() => treeContainer.ScrollTo(CurrentFocus))
                        // This delay is required for no good reason...
                        .StartingIn(100);
                }
            });

            _toolbarSearchField.RegisterValueChangedCallback(evt =>
            {
                string searchText = evt.newValue;

                string[] searchTokens = string.IsNullOrWhiteSpace(searchText)
                    ? Array.Empty<string>()
                    : searchText.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (TreeRowAbsElement treeRowAbsElement in treeRowElements)
                {
                    treeRowAbsElement.OnSearch(searchTokens);
                }
            });

            // navigation
            RegisterCallback<NavigationMoveEvent>(e =>
            {
                // Debug.Log(e.direction);
                bool isUp;
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (e.direction)
                {
                    case NavigationMoveEvent.Direction.Up:
                        isUp = true;
                        break;
                    case NavigationMoveEvent.Direction.Down:
                        isUp = false;
                        break;
                    case NavigationMoveEvent.Direction.Left:
                    {
                        switch (CurrentFocus)
                        {
                            case TreeRowFoldoutElement { value: true } foldoutElement:
                                foldoutElement.value = false;
                                break;
                            case { Parent: not null }:
                            {
                                CurrentFocus = CurrentFocus.Parent;
                                // Debug.Log($"currentFocus={_currentFocus}");
                                foreach (TreeRowAbsElement treeRowAbsElement in _flatList)
                                {
                                    treeRowAbsElement.SetNavigateHighlight(CurrentFocus == treeRowAbsElement);
                                }

                                break;
                            }
                        }

                        return;
                    }
                    case NavigationMoveEvent.Direction.Right:
                    {
                        if (CurrentFocus is TreeRowFoldoutElement { value: false } foldoutElement)
                        {
                            foldoutElement.value = true;
                        }
                        return;
                    }
                    default:
                        return;
                }

                TreeRowAbsElement toFocus = null;
                if (CurrentFocus != null)
                {
                    List<TreeRowAbsElement> prevList = new List<TreeRowAbsElement>(_flatList.Count);
                    for (int index = 0; index < _flatList.Count; index++)
                    {
                        TreeRowAbsElement current = _flatList[index];
                        if (current == CurrentFocus)
                        {
                            if (isUp)
                            {
                                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                                if (prevList.Count > 0)
                                {
                                    // Debug.Log(prevList.Count);
                                    // Debug.Log($"pres: {string.Join(", ", prevList)}");
                                    toFocus = prevList.LastOrDefault(each => each.Navigateable);
                                }
                                else
                                {
                                    toFocus = _flatList.LastOrDefault(each => each.Navigateable);
                                }

                                // Debug.Log($"up to {toFocus}");
                            }
                            else
                            {
                                toFocus = _flatList.Skip(index + 1).FirstOrDefault(each => each.Navigateable)
                                          ?? _flatList.FirstOrDefault(each => each.Navigateable);
                            }

                            break;
                        }

                        // Debug.Log($"{current} -> {currentFocus}");
                        prevList.Add(current);
                    }
                }

                if (CurrentFocus == null)
                {
                    toFocus = isUp
                        ? _flatList.LastOrDefault(each => each.Navigateable)
                        : _flatList.FirstOrDefault(each => each.Navigateable);
                }

                if (toFocus != null)
                {
                    CurrentFocus = toFocus;

                    // Debug.Log($"currentFocus={_currentFocus}");

                    foreach (TreeRowAbsElement treeRowAbsElement in _flatList)
                    {
                        treeRowAbsElement.SetNavigateHighlight(toFocus == treeRowAbsElement);
                    }

                    // ScrollToElementEvent.Invoke(CurrentFocus);
                    treeContainer.ScrollTo(CurrentFocus);
                }
            }, TrickleDown.TrickleDown);
            RegisterCallback<KeyUpEvent>(e =>
            {

                if (CurrentFocus is null)
                {
                    return;
                }

                // ReSharper disable once InvertIf
                if (e.keyCode is
                    // KeyCode.Space
                    KeyCode.Return
                    or KeyCode.KeypadEnter
                )
                {
                    switch (CurrentFocus)
                    {
                        case TreeRowFoldoutElement foldoutElement:
                            foldoutElement.value = !foldoutElement.value;
                            break;
                        case TreeRowValueElement valueElement:
                            valueElement.OnClickedEvent.Invoke();
                            break;
                    }
                }

            });
        }

        private bool _hasHorizontalScrollerOnce;

        private void OnTreeGeometryChanged(GeometryChangedEvent evt)
        {
            // Debug.Log(_scrollView.horizontalScroller);
            float width = _scrollView.horizontalScroller.resolvedStyle.width;
            if (double.IsNaN(width) || width <= 0)
            {
                _hasHorizontalScrollerOnce = false;
            }
            else
            {
                // Debug.Log(width);
                _hasHorizontalScrollerOnce = true;
            }
        }

        public int GetMaxHeight()
        {
            // int result = SaintsPropertyDrawer.SingleLineHeight + 2 + 18;  // search bar height + border + scroller
            int result = 20 + 2;  // search bar height + border
            if (_hasHorizontalScrollerOnce)
            {
                result += 18;
            }

            foreach (TreeRowAbsElement treeRowAbsElement in _flatList)
            {
                if (treeRowAbsElement is TreeRowSepElement)
                {
                    result += 2;
                }
                else
                {
                    result += 20;
                }
            }

            return result;
        }

        private static IEnumerable<TreeRowAbsElement> FlatTreeRow(TreeRowAbsElement treeRow)
        {
            if (treeRow is TreeRowSepElement)
            {
                yield break;
            }

            yield return treeRow;

            // ReSharper disable once InvertIf
            if (treeRow is TreeRowFoldoutElement treeRowFoldoutElement)
            {
                foreach (TreeRowAbsElement subRow in treeRowFoldoutElement.ContentChildren)
                {
                    foreach (TreeRowAbsElement flatSub in FlatTreeRow(subRow))
                    {
                        yield return flatSub;
                    }
                }
            }
        }

        private IReadOnlyList<TreeRowAbsElement> MakeNestedTreeRow(int indent, IAdvancedDropdownList dropdownLis, object curValue)
        {
            List<TreeRowAbsElement> result = new List<TreeRowAbsElement>(dropdownLis.Count);

            bool hasMeaningfulChild = false;
            // bool hasSelect = false;
            // int incrId = accId;
            // bool isEmptyNode = true;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (IAdvancedDropdownList dropdownItem in dropdownLis)
            {
                if (dropdownItem.isSeparator)
                {
                    result.Add(new TreeRowSepElement(indent));
                    continue;
                }

                if (dropdownItem.ChildCount() == 0)  // value node
                {
                    hasMeaningfulChild = true;
                    TreeRowValueElement valueElement = new TreeRowValueElement(string.IsNullOrEmpty(dropdownItem.icon)? dropdownItem.displayName: $"<icon={dropdownItem.icon}/>{dropdownItem.displayName}", indent);
                    if (dropdownItem.ExtraSearches.Count > 0)
                    {
                        valueElement.AddSearches(dropdownItem.ExtraSearches);
                    }
                    if (Equals(curValue, dropdownItem.value))
                    {
                        valueElement.SetValueOn(true);
                        CurrentFocus ??= valueElement;
                    }

                    if (dropdownItem.disabled)
                    {
                        valueElement.SetEnabled(false);
                    }

                    object value = dropdownItem.value;
                    valueElement.OnClickedEvent.AddListener(() => OnClickedEvent.Invoke(value));
                    result.Add(valueElement);

                    continue;
                }

                // (List<TreeViewItemData<IAdvancedDropdownList>> children, int resultId, bool childSelect) = MakeNestedItems(dropdownItem, curValues, incrId, selectedNestedIds, selectedValueIds);
                IReadOnlyList<TreeRowAbsElement> tailResult = MakeNestedTreeRow(indent + 1, dropdownItem, curValue);

                if (dropdownItem.ChildCount() > 0 && tailResult.Count == 0)
                {
                    continue;
                }

                hasMeaningfulChild = true;

                TreeRowFoldoutElement thisNode = new TreeRowFoldoutElement(dropdownItem.displayName, indent, true);
                foreach (TreeRowAbsElement childElement in tailResult)
                {
                    thisNode.AddContent(childElement);
                }

                result.Add(thisNode);
            }

            return hasMeaningfulChild ? result : Array.Empty<TreeRowAbsElement>();
        }

        public void SetSearch(string search)
        {
            _toolbarSearchField.value = search;
        }
    }
}
