using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaintsHierarchy.Editor.Core.Config;
using SaintsHierarchy.Editor.Core.UIElement;
using SaintsHierarchy.Editor.Core.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaintsHierarchy.Editor.Core
{
    // Menu data belongs to one invocation. Only hook ownership survives it.
    public sealed class HierarchyContextMenu
    {
        private const BindingFlags Flags = HierarchyContextUtil.Flags;
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

        // Legacy adapter: install only for this draw, after Unity's selection handling and before its popup callback.
        public void Draw(Action original)
        {
            Event evt = Event.current;
            if (!Enabled || evt == null || evt.type != EventType.ContextClick || _window.rootVisualElement.panel == null)
            {
                original();
                return;
            }
            List<Action> restore = new List<Action>();
            try
            {
                object hierarchy = HierarchyContextUtil.Get(_window, "m_SceneHierarchy");
                object tree = hierarchy == null ? null : HierarchyContextUtil.Get(hierarchy, "m_TreeView");
                if (tree != null)
                {
                    PropertyInfo callback = tree.GetType().GetProperty("contextClickItemCallback", Flags);
                    if (callback?.GetValue(tree) is Delegate previous && previous.GetInvocationList().Length == 1
                        && previous.Method.DeclaringType == hierarchy.GetType())
                    {
                        Type idType = previous.GetType().GetGenericArguments()[0];
                        MethodInfo factory = GetType().GetMethod(nameof(ItemCallback), Flags)!.MakeGenericMethod(idType);
                        Delegate installed = (Delegate)factory.Invoke(this, new object[] { previous, hierarchy, tree });
                        callback.SetValue(tree, installed);
                        restore.Add(() =>
                        {
                            if (Equals(callback.GetValue(tree), installed)) callback.SetValue(tree, previous);
                        });
                    }
                    PropertyInfo outside = tree.GetType().GetProperty("contextClickOutsideItemsCallback", Flags);
                    if (outside?.GetValue(tree) is Action oldOutside && oldOutside.GetInvocationList().Length == 1
                        && oldOutside.Method.DeclaringType == hierarchy.GetType())
                    {
                        Action installed = () => OutsideItems(hierarchy, oldOutside);
                        outside.SetValue(tree, installed);
                        restore.Add(() =>
                        {
                            if (Equals(outside.GetValue(tree), installed)) outside.SetValue(tree, oldOutside);
                        });
                    }
                }
            }
            catch (Exception exception)
            {
                ReportFailure(exception);
                for (int i = restore.Count - 1; i >= 0; i--) restore[i]();
                restore.Clear();
            }
            try
            {
                original();
            }
            finally
            {
                for (int i = restore.Count - 1; i >= 0; i--) restore[i]();
            }
        }

        private Action<T> ItemCallback<T>(Delegate previous, object hierarchy, object tree) => id =>
        {
            bool handled = false;
            try
            {
                object item = HierarchyContextUtil.Call(tree, "FindItem", id);
                GameObject go = item == null ? null : HierarchyContextUtil.Get(item, "objectPPTR") as GameObject;
                // Scene headers and SubScenes keep Unity's original menu.
                if (go != null && !IsSubScene(go))
                {
                    handled = ShowLegacy(hierarchy,
#if UNITY_6000_4_OR_NEWER
                        go.GetEntityId()
#else
                        go.GetInstanceID()
#endif
                    );
                }
            }
            catch (Exception exception)
            {
                ReportFailure(exception);
            }
            if (!handled) previous.DynamicInvoke(id);
        };

        private void OutsideItems(object hierarchy, Action native)
        {
            Object[] selection = Selection.objects;
            Object active = Selection.activeObject;
            bool handled = false;
            try
            {
                // Match Unity: creations from blank space must not become children of the old selection.
                Selection.activeObject = null;
                handled = ShowLegacy(hierarchy, null);
            }
            catch (Exception exception)
            {
                ReportFailure(exception);
            }
            if (!handled)
            {
                Selection.objects = selection;
                Selection.activeObject = active;
                native();
            }
        }

        private static bool IsSubScene(GameObject go)
        {
            Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SubSceneGUI");
            MethodInfo method = type?.GetMethod("GetSubSceneInfo", Flags, null, new[] { typeof(GameObject) }, null);
            object info = method?.Invoke(null, new object[] { go });
            return info != null && HierarchyContextUtil.Get(info, "isValid") is true;
        }

        private bool ShowLegacy(object hierarchy, object id)
        {
            GenericMenu menu = new GenericMenu();
            MethodInfo build = hierarchy.GetType().GetMethod("CreateGameObjectContextClick", Flags)
                ?? throw new MissingMethodException(hierarchy.GetType().FullName, "CreateGameObjectContextClick");
            Type idType = build.GetParameters()[1].ParameterType;
            if (id == null)
            {
                id = Activator.CreateInstance(idType);
            }
            else if (!idType.IsInstanceOfType(id))
            {
                MethodInfo convert = Array.Find(id.GetType().GetMethods(Flags), method =>
                    method.Name == "op_Implicit" && method.ReturnType == idType
                    && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == id.GetType());
                id = convert?.Invoke(null, new[] { id }) ?? throw new InvalidOperationException("Unsupported Hierarchy identifier.");
            }
            build.Invoke(hierarchy, new object[] { menu, id });
            bool shown = Show(HierarchyContextUtil.Read(menu), GUIUtility.GUIToScreenPoint(Event.current.mousePosition));
            if (shown)
            {
                GUIUtility.hotControl = 0;
                Event.current.Use();
            }
            return shown;
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
