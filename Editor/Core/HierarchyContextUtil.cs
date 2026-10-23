using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.Core
{
    public static class HierarchyContextUtil
    {
        internal const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        public readonly struct MenuItem
        {
            public readonly string Path;
            public readonly string Label;
            public readonly string Shortcut;
            public readonly int Priority;
            public readonly bool IsSeparator;
            public readonly bool Enabled;
            public readonly bool Checked;
            public readonly Action Execute;

            public MenuItem(string path, string label, string shortcut, int priority, bool isSeparator,
                bool enabled = true, bool isChecked = false, Action execute = null)
            {
                Path = path;
                Label = label;
                Shortcut = shortcut;
                Priority = priority;
                IsSeparator = isSeparator;
                Enabled = enabled;
                Checked = isChecked;
                Execute = execute;
            }
        }

        // Copy values and callbacks out of Unity's invocation-owned menu. Never retain a pooled menu or event.
        public static List<MenuItem> Read(GenericMenu menu)
        {
            IEnumerable items = Get(menu, "menuItems") as IEnumerable
                ?? Get(menu, "m_MenuItems") as IEnumerable
                ?? throw new MissingMemberException(typeof(GenericMenu).FullName, "menuItems");
            List<MenuItem> source = new List<MenuItem>();
            foreach (object item in items)
            {
                GUIContent content = (GUIContent)Get(item, "content");
                bool separator = (bool)Get(item, "separator");
                GenericMenu.MenuFunction function = Get(item, "func") as GenericMenu.MenuFunction;
                GenericMenu.MenuFunction2 function2 = Get(item, "func2") as GenericMenu.MenuFunction2;
                object data = Get(item, "userData");
                Action execute = function2 != null ? () => function2(data)
                    : function != null ? () => function() : (Action)null;
                source.Add(Item(content.text, separator, execute != null, (bool)Get(item, "on"), execute));
            }
            return source;
        }

        public static List<MenuItem> Read(DropdownMenu menu, DropdownMenuEventInfo eventInfo)
        {
            List<MenuItem> source = new List<MenuItem>();
            foreach (DropdownMenuItem item in menu.MenuItems())
            {
                if (item is DropdownMenuSeparator separator)
                {
                    source.Add(Item(separator.subMenuPath, true, false, false, null));
                }
                else if (item is DropdownMenuAction action)
                {
                    action.UpdateActionStatus(eventInfo);
                    if (action.status == DropdownMenuAction.Status.None
                        || (action.status & DropdownMenuAction.Status.Hidden) != 0)
                    {
                        continue;
                    }
                    source.Add(Item(action.name, false,
                        (action.status & DropdownMenuAction.Status.Disabled) == 0,
                        (action.status & DropdownMenuAction.Status.Checked) != 0, action.Execute));
                }
            }
            return source;
        }

        private static MenuItem Item(string text, bool separator, bool enabled, bool isChecked, Action execute)
        {
            string path = text ?? "";
            string shortcut = null;
            // Unity appends raw hotkey tokens to GenericMenu/DropdownMenu labels.
            // Remove them from identity so changing key bindings does not orphan overrides.
            int space = path.LastIndexOf(' ');
            if (!separator && space >= 0 && space + 1 < path.Length
                && "%^#&_".IndexOf(path[space + 1]) >= 0)
            {
                shortcut = path.Substring(space + 1);
                path = path.Substring(0, space).TrimEnd();
            }
            path = path.TrimEnd('/');
            return new MenuItem("Hierarchy" + (path.Length == 0 ? "" : "/" + path),
                path.Substring(path.LastIndexOf('/') + 1), shortcut, 0, separator, enabled, isChecked, execute);
        }

        internal static object Get(object target, string name)
        {
            Type type = target.GetType();
            return type.GetField(name, Flags)?.GetValue(target)
                ?? type.GetProperty(name, Flags)?.GetValue(target);
        }

        internal static Texture2D LoadIcon(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            string assetPath = AssetDatabase.GUIDToAssetPath(path);
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(string.IsNullOrEmpty(assetPath) ? path : assetPath);
            // Optional package icons must remain silent when that package is not installed.
            if (icon == null && path.StartsWith("Packages/", StringComparison.Ordinal)) return null;
            return icon != null ? icon : Utils.Util.LoadResource<Texture2D>(path);
        }

        internal static object Call(object target, string name, params object[] arguments)
        {
            MethodInfo method = Array.Find(target.GetType().GetMethods(Flags),
                candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().FullName, name);
            }
            return method.Invoke(target, arguments);
        }
    }
}
