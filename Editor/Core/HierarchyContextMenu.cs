using System;
using System.Collections.Generic;
using System.Linq;
using SaintsHierarchy.Editor.Core.Config;
using SaintsHierarchy.Editor.Core.UIElement;
using SaintsHierarchy.Editor.Core.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaintsHierarchy.Editor.Core
{
    public sealed class HierarchyContextMenu
    {
        private readonly EditorWindow _window;
        private HierarchyPopupMenu _dropdown;
        private bool _reportedFailure;

        public static bool Enabled => !Util.GetUsingConfig().disabled && Util.GetUsingConfig().ReplaceContextMenu;

        internal sealed class Entry
        {
            internal string Path;
            internal string DisplayPath;
            internal string Label;
            internal string Shortcut;
            internal int Priority;
            internal bool Separator;
            internal bool Enabled;
            internal bool Checked;
            internal Texture2D PrefixIcon;
            internal string PrefixText;
            internal Action Execute;
            internal readonly List<Entry> Children = new List<Entry>();

            public Entry NewChildren(IEnumerable<Entry> children)
            {
                Entry result = new Entry
                {
                    Path = Path,
                    DisplayPath = DisplayPath,
                    Label = Label,
                    Shortcut = Shortcut,
                    Priority = Priority,
                    Separator = false,
                    Enabled = Enabled,
                    Checked = Checked,
                    PrefixIcon = PrefixIcon,
                    PrefixText = PrefixText,
                    Execute = Execute,
                };
                result.Children.AddRange(children);
                return result;
            }
        }

        public HierarchyContextMenu(EditorWindow window) => _window = window;

        public void Dispose()
        {
            _dropdown?.Close();
            _dropdown = null;
        }

        public bool Show(List<HierarchyContextUtil.MenuItem> source, Vector2 screenPosition)
        {
            if (!Enabled || !HierarchyPopupMenu.Supported) return false;
            // The original root snapshot also drives customization from every submenu.
            List<HierarchyContextUtil.MenuItem> snapshot = source.ToList();
            List<Entry> entries = ReadEntries(snapshot);
            if (entries.Count > 0) entries.Add(new Entry { Separator = true });
            entries.Add(new Entry
            {
                Label = "Customize Context Menu...",
                Enabled = true,
                Execute = () => ContextMenuEditWindow.Open(snapshot),
            });
            Dispose();
            _dropdown = new HierarchyPopupMenu(_window, entries, screenPosition,
                () => ContextMenuEditWindow.Open(snapshot));
            _dropdown.Show();
            return true;
        }

        internal List<Entry> ReadEntries(List<HierarchyContextUtil.MenuItem> source)
        {
            IConfig config = HierarchyContextConfigStore.ContextMenu;
            ContextMenuTree tree = ContextMenuResolver.Resolve(source, config.ContextPathOverrides,
                config.ContextCustomGroups, HierarchyContextConfigStore.Active.DefaultContextMenuIcons);
            Dictionary<string, HierarchyContextUtil.MenuItem> commands = source.Where(item => !item.IsSeparator)
                .GroupBy(item => item.Path).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            Object[] selection = Selection.objects;
            return CreateEntries(tree.Root);

            List<Entry> CreateEntries(ContextMenuNode parent)
            {
                List<Entry> result = new List<Entry>();
                foreach (ContextMenuNode node in parent.Children)
                {
                    if (!node.IsVisible) continue;
                    if (node.SeparatorBefore) result.Add(new Entry { Separator = true });
                    bool commandNode = node.Kind == ContextMenuNodeKind.Command;
                    commands.TryGetValue(node.SourcePath ?? "", out HierarchyContextUtil.MenuItem command);
                    Entry entry = new Entry
                    {
                        Path = node.SourcePath,
                        DisplayPath = node.DisplayPath,
                        Label = node.Label,
                        Priority = node.Priority,
                        Enabled = !commandNode || command.Enabled,
                        Checked = commandNode && command.Checked,
                        Shortcut = node.Shortcut,
                    };
                    switch (node.Prefix.Type)
                    {
                        case ContextPathPrefixType.TextureObject:
                            entry.PrefixIcon = node.Prefix.TextureObject;
                            break;
                        case ContextPathPrefixType.TexturePath:
                            if (!string.IsNullOrEmpty(node.Prefix.TexturePath)) entry.PrefixIcon = HierarchyContextUtil.LoadIcon(node.Prefix.TexturePath);
                            break;
                        case ContextPathPrefixType.Text:
                            entry.PrefixText = node.Prefix.Text;
                            break;
                    }
                    entry.Children.AddRange(CreateEntries(node));
                    result.Add(entry);
                    if (!commandNode) continue;
                    // Execute Unity's original callback, including its creation context and Undo handling.
                    entry.Execute = () =>
                    {
                        if (!_window || !selection.SequenceEqual(Selection.objects)) return;
                        _window.Focus();
                        command.Execute?.Invoke();
                    };
                }
                return result;
            }
        }

        public void ReportFailure(Exception exception)
        {
            Dispose();
            if (_reportedFailure) return;
            _reportedFailure = true;
            Debug.LogWarning("Saints Hierarchy context menu unavailable; using Unity's menu. " + exception.GetBaseException().Message);
        }
    }
}
