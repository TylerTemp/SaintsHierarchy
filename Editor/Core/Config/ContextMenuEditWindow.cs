using System;
using System.Collections.Generic;
using System.Linq;
using SaintsHierarchy.Editor.Core.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.Core.Config
{
    public class ContextMenuEditWindow : EditorWindow
    {
        private const string DragKey = "SaintsHierarchy.ContextMenuItem";
        private IConfig _target;
        private ContextMenuTree _resolved;
        private List<HierarchyContextUtil.MenuItem> _source;
        private TreeView _tree;
        private ScrollView _details;
        private Label _configLabel;
        private HelpBox _error;
        private Foldout _issues;
        private bool _refreshing;
        [SerializeField]
        private string _selectedId = "Hierarchy";
        private readonly Dictionary<string, int> _itemIds = new Dictionary<string, int>(StringComparer.Ordinal);
        private int _nextItemId = 1;
        private string _pressedId;
        private Vector2 _pressPosition;

        private sealed class MenuDrag
        {
            internal ContextMenuEditWindow Owner;
            internal string Id;
        }

        internal static void Open(IEnumerable<HierarchyContextUtil.MenuItem> source)
        {
            // Capture the entire invocation before opening a window changes focus.
            List<HierarchyContextUtil.MenuItem> snapshot = source.ToList();
            ContextMenuEditWindow window = GetWindow<ContextMenuEditWindow>("Saints Hierarchy Context Menu");
            window._source = snapshot;
            window.Refresh();
            window.Show();
        }

        private void OnEnable() => HierarchyContextConfigStore.Changed += Refresh;

        private void OnDisable()
        {
            HierarchyContextConfigStore.Changed -= Refresh;
            if (DragAndDrop.GetGenericData(DragKey) is MenuDrag drag && drag.Owner == this)
            {
                DragAndDrop.SetGenericData(DragKey, null);
            }
        }

        public void CreateGUI()
        {
            minSize = new Vector2(660, 360);
            rootVisualElement.Clear();
            _resolved = null;
            Toolbar toolbar = new Toolbar();
            _configLabel = new Label
            {
                style =
                {
                    flexGrow = 1,
                    alignSelf = Align.Center,
                },
            };
            toolbar.Add(_configLabel);
            toolbar.Add(new ToolbarButton(() => NewGroup(SelectedGroup()))
            {
                text = "New Group",
                name = "new-group",
            });
            toolbar.Add(new ToolbarButton(Refresh)
            {
                text = "Refresh",
            });
            rootVisualElement.Add(toolbar);
            _error = new HelpBox("", HelpBoxMessageType.Error)
            {
                name = "edit-error",
                style =
                {
                    display = DisplayStyle.None
                }
            };
            rootVisualElement.Add(_error);
            _issues = new Foldout
            {
                text = "Configuration issues",
                style =
                {
                    display = DisplayStyle.None,
                    maxHeight = 140
                }
            };
            rootVisualElement.Add(_issues);
            TwoPaneSplitView split = new TwoPaneSplitView(0, 340, TwoPaneSplitViewOrientation.Horizontal)
            {
                style =
                {
                    flexGrow = 1
                },
            };
            _tree = new TreeView
            {
                name = "context-menu-tree",
                fixedItemHeight = 26,
                selectionType = SelectionType.Single,
                reorderable = false,
                makeItem = MakeRow,
                bindItem = BindRow,
                style =
                {
                    flexGrow = 1,
                    minWidth = 0
                },
            };
            _tree.selectionChanged += selection =>
            {
                if (_refreshing)
                {
                    return;
                }
                _selectedId = selection.OfType<ContextMenuNode>().FirstOrDefault()?.Id;
                ShowDetails();
            };
            _tree.RegisterCallback<MouseMoveEvent>(StartDrag);
            _tree.RegisterCallback<MouseUpEvent>(_ => _pressedId = null);
            // TreeView's default flex-basis is zero, which would override the
            // split view's explicit fixed-pane width. Let a plain pane own sizing.
            VisualElement treePane = new VisualElement
            {
                name = "menu-tree-pane",
                style =
                {
                    minWidth = 230
                }
            };
            treePane.Add(_tree);
            split.Add(treePane);
            _details = new ScrollView
            {
                name = "item-settings",
                style =
                {
                    minWidth = 270,
                    paddingLeft = 8,
                    paddingRight = 8
                }
            };
            split.Add(_details);
            rootVisualElement.Add(split);
            Refresh();
        }

        private void Refresh()
        {
            if (_refreshing || _tree == null)
            {
                return;
            }
            _refreshing = true;
            HashSet<string> expanded = new HashSet<string>(_itemIds.Where(pair => _tree.IsExpanded(pair.Value)).Select(pair => pair.Key));
            _target = HierarchyContextConfigStore.ContextMenu;
            _source ??= new List<HierarchyContextUtil.MenuItem>();
            _resolved = ContextMenuResolver.Resolve(_source, _target.ContextPathOverrides, _target.ContextCustomGroups,
                HierarchyContextConfigStore.Active.DefaultContextMenuIcons);
            _configLabel.text = ReferenceEquals(_target, PersonalHierarchyConfig.instance) ? "Editing Personal Config" : "Editing Project Config";
            _tree.SetRootItems(_resolved.Root.Children.Select(TreeItem).ToList());
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (string id in expanded)
            {
                if (_resolved.ById.ContainsKey(id))
                {

                    _tree.ExpandItem(ItemId(id), false
#if UNITY_6000_0_OR_NEWER
                        , false
#endif
                    );

                }
            }
            if (string.IsNullOrEmpty(_selectedId) || !_resolved.ById.TryGetValue(_selectedId, out ContextMenuNode selected) || selected.Kind == ContextMenuNodeKind.Root)
            {
                selected = _resolved.Root.Children.FirstOrDefault();
                _selectedId = selected?.Id;
            }
            for (ContextMenuNode parent = selected?.Parent; parent != null && parent.Kind != ContextMenuNodeKind.Root; parent = parent.Parent)
            {
                _tree.ExpandItem(ItemId(parent.Id), false
#if UNITY_6000_0_OR_NEWER
                    , false
#endif
                );
            }
            _tree.Rebuild();
            if (selected != null)
            {
                _tree.SetSelectionById(ItemId(_selectedId));
                _tree.ScrollToItemById(ItemId(_selectedId));
            }
            else
            {
                _tree.ClearSelection();
            }
            ShowDetails();
            ShowIssues();
            _refreshing = false;
        }

        private int ItemId(string id)
        {
            if (!_itemIds.TryGetValue(id, out int value))
            {
                _itemIds.Add(id, value = _nextItemId++);
            }
            return value;
        }

        private TreeViewItemData<ContextMenuNode> TreeItem(ContextMenuNode node) => new TreeViewItemData<ContextMenuNode>(ItemId(node.Id), node, node.Children.Select(TreeItem).ToList());

        private VisualElement MakeRow()
        {
            VisualElement row = Util.LoadResource<VisualTreeAsset>("UIToolkit/ContextMenuEditRow.uxml").CloneTree();
            row.AddToClassList("context-menu-edit-row");
            row.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                {
                    return;
                }
                _pressedId = row.userData as string;
                _pressPosition = evt.mousePosition;
            });
            row.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (!OwnDrag(out MenuDrag drag))
                {
                    return;
                }
                bool valid = TryMove(drag.Id, row.userData as string, out _, out _);
                DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
                row.style.backgroundColor = valid ? new Color(.2f, .5f, .8f, .25f) : StyleKeyword.Null;
                evt.StopPropagation();
            });
            row.RegisterCallback<DragLeaveEvent>(_ => row.style.backgroundColor = StyleKeyword.Null);
            row.RegisterCallback<DragExitedEvent>(_ => row.style.backgroundColor = StyleKeyword.Null);
            row.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (!OwnDrag(out MenuDrag drag))
                {
                    return;
                }
                row.style.backgroundColor = StyleKeyword.Null;
                if (TryMove(drag.Id, row.userData as string, out List<ContextPathOverride> proposal, out string error))
                {
                    DragAndDrop.AcceptDrag();
                    _selectedId = drag.Id;
                    Commit(proposal, _target.ContextCustomGroups.ToList(), "Move Context Menu Item");
                }
                else
                {
                    ShowError(error);
                }
                DragAndDrop.SetGenericData(DragKey, null);
                evt.StopPropagation();
            });
            row.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                if (!(row.userData is string id) || !_resolved.ById.TryGetValue(id, out ContextMenuNode node))
                {
                    return;
                }
                if (ContextMenuEdits.CanRestoreLocation(node))
                {
                    evt.menu.AppendAction("Restore original location", _ => Restore(node));
                }
                if (node.Kind != ContextMenuNodeKind.Command)
                {
                    evt.menu.AppendAction("New group", _ => NewGroup(node));
                }
                if (node.SourcePath == null)
                {
                    evt.menu.AppendAction("Delete group", _ => DeleteGroup(node), ContextMenuEdits.CanDeleteGroup(node, _target.ContextPathOverrides, _target.ContextCustomGroups) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }
            }));
            return row;
        }

        private void BindRow(VisualElement row, int index)
        {
            ContextMenuNode node = _tree.GetItemDataForIndex<ContextMenuNode>(index);
            row.userData = node.Id;
            row.tooltip = node.DisplayPath + (node.SourcePath == null ? "\nCustom group" : "\nOriginal: " + node.SourcePath);
            row.style.opacity = node.HiddenInHierarchy ? .5f : 1;
            row.style.backgroundColor = StyleKeyword.Null;
            row.EnableInClassList("context-menu-edit-row--separator", node.SeparatorBefore);
            row.Q<Label>("name").text = node.Label;
            List<string> status = new List<string>();
            if (node.SourcePath == null)
            {
                status.Add("Custom");
            }
            if (node.HasPlacementOverride)
            {
                status.Add("Moved / renamed");
            }
            if (node.HiddenInHierarchy)
            {
                status.Add(node.HiddenSelf ? "Hidden" : "Parent hidden");
            }
            else if (!node.IsVisible)
            {
                status.Add("Empty");
            }
            row.Q<Label>("status").text = string.Join(" · ", status);
            Texture2D icon = node.Prefix.Type == ContextPathPrefixType.TextureObject ? node.Prefix.TextureObject : null;
            if (node.Prefix.Type == ContextPathPrefixType.TexturePath && !string.IsNullOrEmpty(node.Prefix.TexturePath))
            {
                icon = HierarchyContextUtil.LoadIcon(node.Prefix.TexturePath);
            }
            VisualElement iconContainer = row.Q<VisualElement>("icon");
            Label prefix = iconContainer.Q<Label>("iconText");
            prefix.text = node.Prefix.Type == ContextPathPrefixType.Text ? node.Prefix.Text : "";
            bool hasText = !string.IsNullOrEmpty(prefix.text);
            prefix.style.display = hasText ? DisplayStyle.Flex : DisplayStyle.None;
            Image image = iconContainer.Q<Image>("iconImage");
            image.image = icon;
            image.style.display = !hasText && image.image != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void StartDrag(MouseMoveEvent evt)
        {
            if (string.IsNullOrEmpty(_pressedId) || (evt.pressedButtons & 1) == 0 || (evt.mousePosition - _pressPosition).sqrMagnitude < 36)
            {
                return;
            }
            if (!_resolved.ById.TryGetValue(_pressedId, out ContextMenuNode node) || node.Kind == ContextMenuNodeKind.Root)
            {
                return;
            }
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
            DragAndDrop.SetGenericData(DragKey, new MenuDrag
            {
                Owner = this,
                Id = node.Id
            });
            DragAndDrop.StartDrag(node.Label);
            _pressedId = null;
            evt.StopPropagation();
        }

        private bool OwnDrag(out MenuDrag drag)
        {
            drag = DragAndDrop.GetGenericData(DragKey) as MenuDrag;
            return drag != null && drag.Owner == this;
        }

        private bool TryMove(string id, string destination, out List<ContextPathOverride> proposal, out string error)
        {
            proposal = null;
            error = "Drop the item onto a different group.";
            if (id == null || destination == null || !_resolved.ById.TryGetValue(id, out ContextMenuNode node) || !_resolved.ById.TryGetValue(destination, out ContextMenuNode parent) || node.Kind == ContextMenuNodeKind.Root || parent.Kind == ContextMenuNodeKind.Command || node.Parent == parent)
            {
                return false;
            }
            proposal = ContextMenuEdits.Move(_target.ContextPathOverrides, node, parent);
            return ContextMenuEdits.TryResolve(_source, _resolved, proposal, _target.ContextCustomGroups, out _, out error, id);
        }

        private ContextMenuNode SelectedGroup()
        {
            if (_resolved.ById.TryGetValue(_selectedId ?? "", out ContextMenuNode node))
            {
                return node.Kind == ContextMenuNodeKind.Command ? node.Parent : node;
            }
            return _resolved.Root;
        }

        private void NewGroup(ContextMenuNode parent)
        {
            ContextCustomGroup group = ContextMenuEdits.CreateGroup(_resolved, parent);
            List<ContextCustomGroup> groups = _target.ContextCustomGroups.ToList();
            groups.Add(group);
            string previousSelection = _selectedId;
            _selectedId = group.id;
            if (!Commit(_target.ContextPathOverrides.ToList(), groups, "Create Context Menu Group"))
            {
                _selectedId = previousSelection;
            }
        }

        private void DeleteGroup(ContextMenuNode node)
        {
            if (!ContextMenuEdits.DeleteGroup(node, _target.ContextPathOverrides, _target.ContextCustomGroups, out List<ContextPathOverride> overrides, out List<ContextCustomGroup> groups))
            {
                return;
            }
            string previousSelection = _selectedId;
            _selectedId = node.Parent.Id;
            if (!Commit(overrides, groups, "Delete Context Menu Group"))
            {
                _selectedId = previousSelection;
            }
        }

        private void Restore(ContextMenuNode node)
        {
            if (!ContextMenuEdits.CanRestoreLocation(node))
            {
                return;
            }
            ContextPathOverride value = _target.ContextPathOverrides.LastOrDefault(item => item.targetId == node.Id);
            value.targetId = node.Id;
            value.RestoreLocation();
            SaveOverride(value, "Restore Context Menu Location");
        }

        private void SaveOverride(ContextPathOverride value, string undoName = "Edit Context Menu Item")
        {
            List<ContextPathOverride> proposal = ContextMenuEdits.WithOverride(_target.ContextPathOverrides, value);
            if (!ContextMenuEdits.TryResolve(_source, _resolved, proposal, _target.ContextCustomGroups, out _, out string error, value.targetId))
            {
                ShowError(error);
                return;
            }
            Commit(proposal, _target.ContextCustomGroups.ToList(), undoName);
        }

        private bool Commit(List<ContextPathOverride> overrides, List<ContextCustomGroup> groups, string undoName)
        {
            if (!ContextMenuEdits.TryResolve(_source, _resolved, overrides, groups, out _, out string error))
            {
                ShowError(error);
                return false;
            }
            _error.style.display = DisplayStyle.None;
            HierarchyContextConfigStore.Edit(_target, undoName, () =>
            {
                _target.ContextPathOverrides.Clear();
                _target.ContextPathOverrides.AddRange(overrides);
                _target.ContextCustomGroups.Clear();
                _target.ContextCustomGroups.AddRange(groups);
            });
            return true;
        }

        private void ShowDetails()
        {
            _details.Clear();
            if (_resolved == null || !_resolved.ById.TryGetValue(_selectedId ?? "", out ContextMenuNode node))
            {
                return;
            }
            _details.Add(new Label(node.Label)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 8
                }
            });
            _details.Add(new Label(node.DisplayPath)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 6
                }
            });
            if (node.Kind == ContextMenuNodeKind.Root)
            {
                _details.Add(new HelpBox("Select a command or group to edit it. Hidden items and empty groups stay in this tree so you can restore them.", HelpBoxMessageType.Info));
                return;
            }
            _details.Add(new Label(node.SourcePath == null ? "Custom group" : "Original: " + node.SourcePath)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 6
                }
            });
            ContextPathOverride value = node.AppliedOverride ?? new ContextPathOverride
            {
                targetId = node.Id
            };
            TextField nameField = new TextField("Name")
            {
                name = "item-name",
                value = node.Name,
                isDelayed = true
            };
            nameField.RegisterValueChangedCallback(evt =>
            {
                if (node.SourcePath == null)
                {
                    if (ContextMenuEdits.RenameGroup(node, evt.newValue, _target.ContextPathOverrides, _target.ContextCustomGroups, out List<ContextPathOverride> overrides, out List<ContextCustomGroup> groups))
                    {
                        Commit(overrides, groups, "Rename Context Menu Group");
                    }
                }
                else
                {
                    Change(config =>
                    {
                        config.newName = evt.newValue == node.OriginalName ? null : evt.newValue;
                        return config;
                    });
                }
            });
            _details.Add(nameField);
            Toggle hidden = new Toggle("Hidden")
            {
                name = "item-hidden",
                value = value.hidden
            };
            hidden.RegisterValueChangedCallback(evt => Change(config =>
            {
                config.hidden = evt.newValue;
                return config;
            }));
            _details.Add(hidden);
            if (node.HiddenInHierarchy && !node.HiddenSelf)
            {
                _details.Add(new HelpBox("Hidden by an original ancestor or destination group.", HelpBoxMessageType.Info));
            }
            Toggle priorityOverride = new Toggle("Override priority")
            {
                name = "override-priority",
                value = value.overridePriority
            };
            priorityOverride.RegisterValueChangedCallback(evt => Change(config =>
            {
                config.overridePriority = evt.newValue;
                if (evt.newValue)
                {
                    config.priority = node.Priority;
                }
                return config;
            }));
            _details.Add(priorityOverride);
            IntegerField priority = new IntegerField("Priority")
            {
                name = "item-priority",
                value = node.Priority,
                isDelayed = true
            };
            priority.SetEnabled(value.overridePriority);
            priority.RegisterValueChangedCallback(evt => Change(config =>
            {
                config.priority = evt.newValue;
                return config;
            }));
            _details.Add(priority);
            EnumField separator = new EnumField("Separator before", value.separatorBefore)
            {
                name = "item-separator"
            };
            separator.RegisterValueChangedCallback(evt => Change(config =>
            {
                config.separatorBefore = (ContextSeparatorBefore)evt.newValue;
                return config;
            }));
            _details.Add(separator);
            EnumField prefix = new EnumField("Prefix", value.prefixType)
            {
                name = "item-prefix",
                tooltip = "Default uses the SaintsHierarchy icon for the original menu path, if available. None shows no prefix."
            };
            prefix.RegisterValueChangedCallback(evt => Change(config =>
            {
                config.prefixType = (ContextPathPrefixType)evt.newValue;
                return config;
            }));
            _details.Add(prefix);
            switch (value.prefixType)
            {
                case ContextPathPrefixType.Default:
                case ContextPathPrefixType.None:
                    break;
                case ContextPathPrefixType.TextureObject:
                    ObjectField texture = new ObjectField("Icon")
                    {
                        objectType = typeof(Texture2D),
                        allowSceneObjects = false,
                        value = value.prefixTextureObject
                    };
                    texture.RegisterValueChangedCallback(evt => Change(config =>
                    {
                        config.prefixTextureObject = evt.newValue as Texture2D;
                        return config;
                    }));
                    _details.Add(texture);
                    break;
                case ContextPathPrefixType.TexturePath:
                    TextField path = new TextField("Icon path")
                    {
                        value = value.prefixTexturePath,
                        isDelayed = true
                    };
                    path.RegisterValueChangedCallback(evt => Change(config =>
                    {
                        config.prefixTexturePath = evt.newValue;
                        return config;
                    }));
                    _details.Add(path);
                    break;
                case ContextPathPrefixType.Text:
                    TextField text = new TextField("Prefix text")
                    {
                        value = value.prefixText,
                        isDelayed = true
                    };
                    text.RegisterValueChangedCallback(evt => Change(config =>
                    {
                        config.prefixText = evt.newValue;
                        return config;
                    }));
                    _details.Add(text);
                    break;
            }
            if (ContextMenuEdits.CanRestoreLocation(node))
            {
                _details.Add(new Button(() => Restore(node))
                {
                    name = "restore-location",
                    text = "Restore original location"
                });
            }
            if (node.SourcePath == null)
            {
                Button delete = new Button(() => DeleteGroup(node))
                {
                    name = "delete-group",
                    text = "Delete group",
                    tooltip = "Restores the locations of items inside this group and removes its nested custom containers. Other item settings are kept."
                };
                delete.SetEnabled(ContextMenuEdits.CanDeleteGroup(node, _target.ContextPathOverrides, _target.ContextCustomGroups));
                _details.Add(delete);
            }
            void Change(Func<ContextPathOverride, ContextPathOverride> change)
            {
                // Read the current record, not the snapshot captured when a field was built.
                ContextPathOverride current = _target.ContextPathOverrides.LastOrDefault(item => item.targetId == node.Id);
                current.targetId = node.Id;
                SaveOverride(change(current));
            }
        }

        private void ShowIssues()
        {
            _issues.Clear();
            _issues.text = "Configuration issues (" + _resolved.Diagnostics.Count + ")";
            _issues.style.display = _resolved.Diagnostics.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            ScrollView list = new ScrollView();
            _issues.Add(list);
            foreach (ContextMenuDiagnostic issue in _resolved.Diagnostics)
            {
                VisualElement row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                    },
                };
                if (_target.ContextPathOverrides.Any(item => item.targetId == issue.TargetId))
                {
                    row.Add(new Button(() => Commit(_target.ContextPathOverrides.Where(item => item.targetId != issue.TargetId).ToList(), _target.ContextCustomGroups.ToList(), "Remove Invalid Context Menu Override"))
                    {
                        text = "Remove",
                    });
                }

                row.Add(new Label(issue.TargetId + ": " + issue.Message)
                {
                    style =
                    {
                        whiteSpace = WhiteSpace.Normal,
                    },
                });

                list.Add(row);
            }
        }

        private void ShowError(string message)
        {
            _error.text = message;
            _error.style.display = DisplayStyle.Flex;
        }
    }
}
