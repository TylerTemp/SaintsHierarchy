using System;
using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor.Core
{
    public static class HierarchyContextUtil
    {
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

        public static MenuItem Item(string text, bool separator, bool enabled, bool isChecked, Action execute)
        {
            string path = text ?? "";
            string shortcut = null;
            // Unity appends raw hotkey tokens to GenericMenu/DropdownMenu labels.
            // Remove them from identity so changing key bindings does not orphan overrides.
            int space = path.LastIndexOf(' ');
            if (!separator && space >= 0 && space + 1 < path.Length
                && "%^#&_".IndexOf(path[space + 1]) >= 0)
            {
                shortcut = path[(space + 1)..];
                path = path[..space].TrimEnd();
            }
            path = path.TrimEnd('/');
            return new MenuItem("Hierarchy" + (path.Length == 0 ? "" : "/" + path),
                path[(path.LastIndexOf('/') + 1)..], shortcut, 0, separator, enabled, isChecked, execute);
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
    }
}
