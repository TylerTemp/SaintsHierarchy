using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.Core.UIElement
{
    internal sealed class HierarchyContextPopupWindow : EditorWindow
    {
        internal EditorWindow Owner;
        internal HierarchyContextMenu.Entry ParentEntry;

        // These collections always describe the same active view, including filtered results.
        internal List<HierarchyContextMenu.Entry> Entries
        {
            get; private set;
        }

        internal List<HierarchyContextMenuRow> Rows
        {
            get; private set;
        }

        internal int Selected = -1;
        internal int HoverGeneration;
        private HierarchyPopupMenu _menu;
        private ScrollView _scroll;

        private VisualElement _originalRoot;
        private VisualElement _searchResults;
        private List<HierarchyContextMenu.Entry> _originalEntries;
        private List<HierarchyContextMenuRow> _originalRows;
        private HierarchyContextSearchRow _searchRow;

        internal void Initialize(bool isRoot, HierarchyPopupMenu menu, EditorWindow owner, List<HierarchyContextMenu.Entry> entries)
        {
            _menu = menu;
            Owner = owner;
            Entries = _originalEntries = entries;

            rootVisualElement.focusable = true;
            rootVisualElement.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Customize Context Menu...", _ => _menu.Customize());
                evt.StopPropagation();
            }));
            rootVisualElement.style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(.22f, .22f, .22f)
                : new Color(.94f, .94f, .94f);
            Color border = EditorGUIUtility.isProSkin
                ? new Color(.1f, .1f, .1f)
                : new Color(.55f, .55f, .55f);
            rootVisualElement.style.borderTopWidth = rootVisualElement.style.borderBottomWidth = 1;
            rootVisualElement.style.borderLeftWidth = rootVisualElement.style.borderRightWidth = 1;
            rootVisualElement.style.borderTopColor = rootVisualElement.style.borderBottomColor = border;
            rootVisualElement.style.borderLeftColor = rootVisualElement.style.borderRightColor = border;
            rootVisualElement.style.paddingTop = rootVisualElement.style.paddingBottom = 2;
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<PointerEnterEvent>(_ => _menu.EnterWindow(this));
            rootVisualElement.RegisterCallback<PointerLeaveEvent>(_ => _menu.LeaveWindow(this));
            rootVisualElement.RegisterCallback<PointerMoveEvent>(evt =>
            {
    int row = Rows.FindIndex(element => element.worldBound.Contains(evt.position));
    if (row >= 0 && (!Entries[row].Enabled || Entries[row].Separator))
                {
        _menu.Hover(this, row);
                }
            }, TrickleDown.TrickleDown);
            _scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1,
                },
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
            };
            rootVisualElement.Add(_scroll);

            if (isRoot)
            {
                _searchRow = new HierarchyContextSearchRow();
                _scroll.Add(_searchRow);
                _searchRow.ToolbarSearchField.RegisterValueChangedCallback(OnSearchCallback);
                _searchRow.RegisterCallback<PointerEnterEvent>(_ => _menu.ClearSelection(this));
                _searchRow.RegisterCallback<PointerMoveEvent>(_ =>
                {
    if (Selected >= 0 || _menu.Levels.Count > 1)
                    {
        _menu.ClearSelection(this);
                    }
                });

                HierarchyContextMenuRow sep = HierarchyContextMenuRow.Separator();
                _scroll.Add(sep);

                _searchResults = new VisualElement();
                _scroll.Add(_searchResults);
            }
            _originalRoot = new VisualElement();
            _scroll.Add(_originalRoot);

            Rows = _originalRows = RenderEntries(_originalEntries, _originalRoot);
        }

        private void OnSearchCallback(ChangeEvent<string> evt)
        {
            _menu.ClearSelection(this);
            _searchResults.Clear();

            string[] searchTokens = (evt.newValue ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (searchTokens.Length == 0)
            {
                Entries = _originalEntries;
                Rows = _originalRows;
                _searchResults.style.display = DisplayStyle.None;
                _originalRoot.style.display = DisplayStyle.Flex;
                return;
            }
            _originalRoot.style.display = DisplayStyle.None;
            _searchResults.style.display = DisplayStyle.Flex;

            List<HierarchyContextMenu.Entry> pathEntries =
                new List<HierarchyContextMenu.Entry>(_originalEntries.Count);
            List<HierarchyContextMenu.Entry> endPointEntries =
                new List<HierarchyContextMenu.Entry>(_originalEntries.Count);

            foreach (HierarchyContextMenu.Entry entity in _originalEntries)
            {
                HierarchyContextMenu.Entry foundEntity = GetMatchedPathEntity(entity, searchTokens.ToList());
                if (foundEntity == null)
                {
                    continue;
                }
                if (foundEntity.Children.Count > 0)
                {
                    pathEntries.Add(foundEntity);
                }
                else
                {
                    endPointEntries.Add(foundEntity);
                }
            }
            List<HierarchyContextMenu.Entry> allEntities = new List<HierarchyContextMenu.Entry>(endPointEntries);
            if (endPointEntries.Count > 0 && pathEntries.Count > 0)
            {
                allEntities.Add(new HierarchyContextMenu.Entry
                {
                    Separator = true,
                });
            }
            allEntities.AddRange(pathEntries);
            if (allEntities.Count == 0)
            {
                allEntities.Add(new HierarchyContextMenu.Entry
                {
                    Path = "",
                    Label = "No Result",
                    Enabled = false,
                });
            }
            List<HierarchyContextMenuRow> resultRows = RenderEntries(allEntities, _searchResults);
            Entries = allEntities;
            Rows = resultRows;
        }

        private List<HierarchyContextMenuRow> RenderEntries(IReadOnlyList<HierarchyContextMenu.Entry> entries, VisualElement container)
        {
            List<HierarchyContextMenuRow> rows = new List<HierarchyContextMenuRow>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                HierarchyContextMenu.Entry entry = entries[i];
                HierarchyContextMenuRow row = entry.Separator
                    ? HierarchyContextMenuRow.Separator()
                    : new HierarchyContextMenuRow(entry.Label, HierarchyPopupMenu.DisplaySuffix(entry), entry.Checked, entry.Enabled,
                        entry.PrefixIcon, entry.PrefixText);
                rows.Add(row);
                container.Add(row);
                if (entry.Separator)
                {
                    continue;
                }
                int index = i;
                row.RegisterCallback<PointerEnterEvent>(_ =>
                {
    if (IsCurrentRow(index, row) && entry.Enabled)
                    {
        _menu.Hover(this, index);
                    }
                });
                row.RegisterCallback<PointerMoveEvent>(_ =>
                {
    if (IsCurrentRow(index, row) && entry.Enabled && Selected != index)
                    {
        _menu.Hover(this, index);
                    }
                });
                row.RegisterCallback<PointerLeaveEvent>(_ =>
                {
    if (IsCurrentRow(index, row))
                    {
        _menu.LeaveRow(this, index);
                    }
                });
                row.RegisterCallback<PointerUpEvent>(evt =>
                {
    if (evt.button != 0 || !IsCurrentRow(index, row))
                    {
        return;
                    }
    _menu.Activate(this, index);
    evt.StopPropagation();
                });
            }
            return rows;
        }

        private bool IsCurrentRow(int index, HierarchyContextMenuRow row) =>
            Rows != null && index < Rows.Count && Rows[index] == row;

        private void OnKeyDown(KeyDownEvent evt)
        {
            bool searchModifier = Application.platform == RuntimePlatform.OSXEditor
                ? evt.commandKey && !evt.ctrlKey
                : evt.ctrlKey && !evt.commandKey;
            if (_searchRow != null && evt.keyCode == KeyCode.F && searchModifier && !evt.altKey && !evt.shiftKey)
            {
                _menu.ClearSelection(this);
                _searchRow.ToggleSearch();
                if (!_searchRow.IsSearching)
                {
                    rootVisualElement.Focus();
                }
                evt.StopImmediatePropagation();
#if !UNITY_2023_2_OR_NEWER
                evt.PreventDefault();
#endif
                return;
            }
            if (_searchRow != null && evt.target is VisualElement target &&
                (target == _searchRow || _searchRow.Contains(target)))
            {
                bool inSearchField = target == _searchRow.ToolbarSearchField || _searchRow.ToolbarSearchField.Contains(target);
                bool plainKey = !evt.altKey && !evt.ctrlKey && !evt.commandKey && !evt.shiftKey;
                bool navigateResults = inSearchField && plainKey &&
                    (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.DownArrow ||
                     Selected >= 0 && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter));
                // Escape dismisses the whole menu even while editing the search field.
                if (evt.keyCode != KeyCode.Escape && !navigateResults)
                {
                    return;
                }
            }
            _menu.Key(this, evt);
        }

        internal void Select(int index)
        {
            if (Selected >= 0 && Selected < Rows.Count)
            {
                Rows[Selected].SetSelected(false);
            }
            Selected = index;
            if (index < 0 || index >= Rows.Count)
            {
                return;
            }
            Rows[index].SetSelected(true);
            _scroll.ScrollTo(Rows[index]);
        }

        private static HierarchyContextMenu.Entry GetMatchedPathEntity(HierarchyContextMenu.Entry entity, List<string> leftTokens)
        {
            if (entity.Separator)
            {
                return null;
            }
            if (leftTokens.Count == 0)
            {
                return entity;
            }
            string curSegPath = entity.DisplayPath ?? entity.Path ?? entity.Label ?? "";
            for (int indexRev = leftTokens.Count - 1; indexRev >= 0; indexRev--)
            {
                string token = leftTokens[indexRev];
                if (curSegPath.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    leftTokens.RemoveAt(indexRev);
                }
            }
            if (leftTokens.Count == 0)
            {
                return entity;
            }
            if (entity.Children.Count == 0)
            {
                return null;
            }
            List<HierarchyContextMenu.Entry> children = entity.Children
                .Select(each => GetMatchedPathEntity(each, leftTokens.ToList()))
                .Where(each => each != null)
                .ToList();

            if (children.Count == 1)
            {
                // Merge only collapsed display segments; retained parents supply their own context.
                // Keep the matched command and metadata unchanged in this presentation copy.
                HierarchyContextMenu.Entry match = children[0];
                HierarchyContextMenu.Entry result = match.NewChildren(match.Children);
                result.Label = entity.Label + "/" + match.Label;
                return result;
            }
            if (children.Count > 0)
            {
                return entity.NewChildren(children);
            }
            return null;
        }

        internal void MoveSelection(int direction)
        {
            HoverGeneration++;
            int index = Selected < 0 ? (direction > 0 ? -1 : 0) : Selected;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int n = 0; n < Entries.Count; n++)
            {
                index = (index + direction + Entries.Count) % Entries.Count;
                // ReSharper disable once InvertIf
                if (!Entries[index].Separator && Entries[index].Enabled)
                {
                    Select(index);
                    return;
                }
            }
        }

        private void OnDestroy() => _menu?.WindowDestroyed(this);
    }
}
