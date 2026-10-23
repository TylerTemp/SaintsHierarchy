using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.Core.UIElement
{
    // Coordinates native windows; menu data and command context still belong to the caller.
    internal sealed class HierarchyPopupMenu
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        internal static bool Supported => Supports(typeof(EditorWindow)) && DesktopMethod != null;

        internal static bool Supports(Type type) => type.GetMethod("ShowPopup", Type.EmptyTypes) != null;

        private static MethodInfo DesktopMethod => typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.InternalEditorUtility")?.GetMethod("GetBoundsOfDesktopAtPoint", Flags);

        private readonly EditorWindow _owner;
        private readonly Action _customize;
        private readonly List<HierarchyContextMenu.Entry> _entries;
        private readonly Vector2 _position;
        private readonly List<HierarchyContextPopupWindow> _levels = new List<HierarchyContextPopupWindow>();
        private double _focusGrace;
        private bool _closed;
        private bool _applicationWasActive;
        private HierarchyContextPopupWindow _pointerWindow;

        internal IReadOnlyList<HierarchyContextPopupWindow> Levels => _levels;

        internal HierarchyPopupMenu(EditorWindow owner, List<HierarchyContextMenu.Entry> entries, Vector2 screenPosition, Action customize)
        {
            _owner = owner;
            _customize = customize;
            _entries = entries;
            _position = screenPosition;
        }

        internal void Show()
        {
            if (!Supported)
            {
                throw new NotSupportedException("Frameless popup hosting is unavailable.");
            }
            _applicationWasActive = UnityEditorInternal.InternalEditorUtility.isApplicationActive;
            _owner.rootVisualElement.RegisterCallback<PointerDownEvent>(OnOwnerPointerDown, TrickleDown.TrickleDown);
            _owner.rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnOwnerDetached);
            EditorApplication.update -= CheckWindows;
            EditorApplication.update += CheckWindows;
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
            AssemblyReloadEvents.beforeAssemblyReload += Close;
            EditorApplication.quitting -= Close;
            EditorApplication.quitting += Close;

            try
            {
                Open(true, _entries, _position, null, false);
            }
            catch
            {
                Close();
                throw;
            }
        }

        internal void Close()
        {
            if (_closed)
            {
                return;
            }
            _closed = true;
            EditorApplication.update -= CheckWindows;
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
            EditorApplication.quitting -= Close;
            if (_owner)
            {
                _owner.rootVisualElement.UnregisterCallback<PointerDownEvent>(OnOwnerPointerDown, TrickleDown.TrickleDown);
                _owner.rootVisualElement.UnregisterCallback<DetachFromPanelEvent>(OnOwnerDetached);
            }
            CloseFrom(0);
        }

        private void OnOwnerPointerDown(PointerDownEvent evt) => Close();

        private void OnOwnerDetached(DetachFromPanelEvent evt)
        {
            if (evt.target == _owner.rootVisualElement)
            {
                Close();
            }
        }

        private void CheckWindows()
        {
            if (_closed)
            {
                return;
            }
            bool applicationActive = UnityEditorInternal.InternalEditorUtility.isApplicationActive;
            if (_applicationWasActive && !applicationActive)
            {
                Close();
                return;
            }
            _applicationWasActive |= applicationActive;

            if (!_owner || _owner.rootVisualElement.panel == null || _levels.Count == 0)
            {
                Close();
                return;
            }
            foreach (HierarchyContextPopupWindow level in _levels)
            {
                if (!level)
                {
                    Close();
                    return;
                }
            }
            if (EditorApplication.timeSinceStartup < _focusGrace)
            {
                return;
            }
            // hasFocus can remain true on multiple native popup windows. Use the active window identity.
            if (!_levels.Contains(EditorWindow.focusedWindow as HierarchyContextPopupWindow))
            {
                Close();
            }
        }

        internal static Rect Desktop(Vector2 point) => (Rect)DesktopMethod.Invoke(null, new object[]
        {
point
        });

        internal static Rect Place(Vector2 point, Vector2 size, Rect desktop, Rect? parent = null)
        {
            size = Vector2.Min(size, desktop.size);
            if (parent.HasValue)
            {
                Rect p = parent.Value;
                point.x = p.xMax - 1;
                if (point.x + size.x > desktop.xMax)
                {
                    point.x = p.xMin - size.x + 1;
                }
            }
            return new Rect(new Vector2(Mathf.Clamp(point.x, desktop.xMin, desktop.xMax - size.x),
                Mathf.Clamp(point.y, desktop.yMin, desktop.yMax - size.y)), size);
        }

        private void Open(bool isRoot, List<HierarchyContextMenu.Entry> entries, Vector2 point, HierarchyContextPopupWindow parent, bool selectFirst)
        {
            if (_closed)
            {
                return;
            }
            Rect desktop = Desktop(parent ? parent.position.center : point);
            Vector2 size = Measure(isRoot, entries, desktop);
            Rect rect = Place(
                point,
                size,
                desktop,
                parent
                    ? parent.position
                    : null
            );
            HierarchyContextPopupWindow popup = ScriptableObject.CreateInstance<HierarchyContextPopupWindow>();
            _levels.Add(popup);
            try
            {
                popup.Initialize(isRoot, this, _owner, entries);
                popup.minSize = popup.maxSize = size;
                popup.position = rect;
                // Register ownership before showing: native focus callbacks may arrive synchronously.
                _focusGrace = EditorApplication.timeSinceStartup + .15;
                popup.ShowPopup();
                popup.position = rect;
#if !UNITY_6000_6_OR_NEWER
                if (popup.position.width > size.x || popup.position.height > size.y)
                {
                    // Older Unity versions enlarge short popups while fitting their position to the screen.
                    // Change the constraints to force a native resize; assigning identical bounds is a no-op.
                    popup.minSize = Vector2.zero;
                    popup.minSize = size;
                }
#endif
                popup.Focus();
                popup.rootVisualElement.Focus();
                if (selectFirst)
                {
                    popup.MoveSelection(1);
                }
            }
            catch
            {
                Close();
                throw;
            }
        }

        private static Vector2 Measure(bool isRoot, List<HierarchyContextMenu.Entry> entries, Rect desktop)
        {
            const float sepHeight = 7;
            const float itemHeight = 22;

            float width = 140;
            float height = 6;
            foreach (HierarchyContextMenu.Entry entry in entries)
            {
                height += entry.Separator ? sepHeight : itemHeight;
                if (entry.Separator)
                {
                    continue;
                }
                float text = EditorStyles.label.CalcSize(new GUIContent(entry.Label)).x;
                float suffix = EditorStyles.label.CalcSize(new GUIContent(DisplaySuffix(entry))).x;
                float prefix = entry.PrefixIcon ? 20 : string.IsNullOrEmpty(entry.PrefixText)
                    ? 0 : EditorStyles.label.CalcSize(new GUIContent(entry.PrefixText)).x + 4;
                width = Mathf.Max(width, text + suffix + prefix + 74);
            }
            if (isRoot)
            {
                height += sepHeight + itemHeight;
            }
            return new Vector2(Mathf.Min(Mathf.Ceil(width) + 2, desktop.width), Mathf.Min(height, desktop.height));
        }

        internal static string DisplaySuffix(HierarchyContextMenu.Entry entry) =>
            entry.Children.Count > 0
                ? "›"
                : FormatShortcut(entry.Shortcut, Application.platform == RuntimePlatform.OSXEditor);

        internal static string FormatShortcut(string shortcut, bool isMac)
        {
            if (string.IsNullOrEmpty(shortcut))
            {
                return shortcut;
            }
            bool action = false, control = false, alt = false, shift = false;
            int index = 0;

            while (index < shortcut.Length && "%^#&_".IndexOf(shortcut[index]) >= 0)
            {
                switch (shortcut[index++])
                {
                    case '%':
                        action = true;
                        break;
                    case '^':
                        control = true;
                        break;
                    case '&':
                        alt = true;
                        break;
                    case '#':
                        shift = true;
                        break;
                        // '_' marks a shortcut without adding a modifier.
                }
            }
            if (index == 0 || index == shortcut.Length)
            {
                return shortcut;
            }
            string token = shortcut[index..];
            string key;
            if (token.Length == 1 && !char.IsWhiteSpace(token[0]) && !char.IsControl(token[0]))
            {
                key = token.ToUpperInvariant();
            }
            else
            {
                switch (token.ToUpperInvariant())
                {
                    case "LEFT":
                        key = "Left";
                        break;
                    case "RIGHT":
                        key = "Right";
                        break;
                    case "UP":
                        key = "Up";
                        break;
                    case "DOWN":
                        key = "Down";
                        break;
                    case "PGUP":
                        key = "PageUp";
                        break;
                    case "PGDN":
                        key = "PageDown";
                        break;
                    case "HOME":
                        key = "Home";
                        break;
                    case "END":
                        key = "End";
                        break;
                    case "INS":
                        key = "Insert";
                        break;
                    case "DEL":
                    case "DELETE":
                        key = "Delete";
                        break;
                    case "SPACE":
                        key = "Space";
                        break;
                    case "TAB":
                        key = "Tab";
                        break;
                    case "BACKSPACE":
                        key = "Backspace";
                        break;
                    case "ENTER":
                    case "RETURN":
                        key = "Enter";
                        break;
                    case "ESC":
                    case "ESCAPE":
                        key = "Esc";
                        break;
                    default:
                        key = token.ToUpperInvariant();
                        if (key.Length is >= 2 and <= 3 && key[0] == 'F' && int.TryParse(key[1..], out int number)
                            && number is >= 1 and <= 24 && key == "F" + number)
                    {
                            break;
                    }
                        // ReSharper disable once InvertIf
                        if (key.Length == 2 && key[0] == 'M' && key[1] >= '0' && key[1] <= '6')
                    {
                            key = "Mouse" + key[1];
                            break;
                    }
                        return shortcut;
                }
            }
            StringBuilder text = new StringBuilder();
            if (control || action && !isMac)
            {
                text.Append("Ctrl+");
            }
            if (action && isMac)
            {
                text.Append("Cmd+");
            }
            if (alt)
            {
                text.Append(isMac ? "Opt+" : "Alt+");
            }
            if (shift)
            {
                text.Append("Shift+");
            }
            return text.Append(key).ToString();
        }

        internal void ClearSelection(HierarchyContextPopupWindow window)
        {
            if (_closed || !_levels.Contains(window))
            {
                return;
            }
            window.HoverGeneration++;
            window.Select(-1);
            CloseFrom(_levels.IndexOf(window) + 1);
        }

        internal void Hover(HierarchyContextPopupWindow window, int index)
        {
            if (_closed || !_levels.Contains(window))
            {
                return;
            }
            HierarchyContextMenu.Entry entry = window.Entries[index];
            int depth = _levels.IndexOf(window) + 1;
            // Reconcile the old branch on entry, independently of whether the new delayed request survives.
            if (depth < _levels.Count && _levels[depth].ParentEntry != entry)
            {
                CloseFrom(depth);
            }
            window.Select(entry.Enabled && !entry.Separator
                ? index
                : -1);

            int generation = ++window.HoverGeneration;
            if (!entry.Enabled || entry.Separator)
            {
                return;
            }
            window.rootVisualElement.schedule.Execute(() =>
            {
    if (_closed || !window || window.HoverGeneration != generation || !_levels.Contains(window))
                {
        return;
                }
    if (!_levels.Contains(EditorWindow.focusedWindow as HierarchyContextPopupWindow))
                {
        return;
                }
    if (entry.Children.Count > 0)
                {
        OpenChild(window, index, false);
                }
    else
                {
        CloseFrom(_levels.IndexOf(window) + 1);
                }
            }).StartingIn(150);
        }

        internal void LeaveRow(HierarchyContextPopupWindow window, int index)
        {
            // A late leave from the old row must not cancel the newly selected row's request.
            if (!_closed && window && window.Selected == index)
            {
                window.HoverGeneration++;
            }
        }

        internal void EnterWindow(HierarchyContextPopupWindow window)
        {
            if (_closed)
            {
                return;
            }
            _pointerWindow = window;
        }

        internal void LeaveWindow(HierarchyContextPopupWindow window)
        {
            if (_closed || _pointerWindow != window)
            {
                return;
            }
            _pointerWindow = null;
            // Cancel pending hover work, but preserve every already-open level on pointer-only exit.
            foreach (HierarchyContextPopupWindow level in _levels)
            {
                level.HoverGeneration++;
            }
        }

        private void OpenChild(HierarchyContextPopupWindow window, int index, bool selectFirst)
        {
            if (_closed || !window || !_levels.Contains(window))
            {
                return;
            }
            HierarchyContextMenu.Entry entry = window.Entries[index];
            if (!entry.Enabled || entry.Children.Count == 0)
            {
                return;
            }
            int depth = _levels.IndexOf(window) + 1;
            if (depth < _levels.Count && _levels[depth].ParentEntry == entry)
            {
                // ReSharper disable once InvertIf
                if (selectFirst)
                {
                    _levels[depth].Focus();
                    _levels[depth].MoveSelection(1);
                }
                return;
            }
            CloseFrom(depth);
            // Hover/keyboard events arrive after layout; position is the settled native window location.
            Rect row = window.Rows[index].worldBound;
            Vector2 point = window.position.position + new Vector2(row.xMax, row.yMin);
            try
            {
                Open(false, entry.Children, point, window, selectFirst);
                _levels[^1].ParentEntry = entry;
            }
            catch (Exception exception)
            {
                Close();
                Debug.LogException(exception);
            }
        }

        internal void Customize()
        {
            Close();
            _customize?.Invoke();
        }

        internal void Activate(HierarchyContextPopupWindow window, int index)
        {
            if (_closed || !_levels.Contains(window) || index < 0 || index >= window.Entries.Count)
            {
                return;
            }
            HierarchyContextMenu.Entry entry = window.Entries[index];
            if (!entry.Enabled || entry.Separator)
            {
                return;
            }
            if (entry.Children.Count > 0)
            {
                OpenChild(window, index, true);
                return;
            }
            Close();
            entry.Execute?.Invoke();
        }

        internal void Key(HierarchyContextPopupWindow window, KeyDownEvent evt)
        {
            if (_closed || !_levels.Contains(window))
            {
                return;
            }
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    Close();
                    break;
                case KeyCode.UpArrow:
                    window.MoveSelection(-1);
                    break;
                case KeyCode.DownArrow:
                    window.MoveSelection(1);
                    break;
                case KeyCode.Home:
                    window.Select(-1);
                    window.MoveSelection(1);
                    break;
                case KeyCode.End:
                    window.Select(-1);
                    window.MoveSelection(-1);
                    break;
                case KeyCode.LeftArrow:
                    int depth = _levels.IndexOf(window);
                    if (depth > 0)
                {
                        CloseFrom(depth);
                        _focusGrace = EditorApplication.timeSinceStartup + .15;
                        _levels[depth - 1].Focus();
                        _levels[depth - 1].rootVisualElement.Focus();
                }
                    break;
                case KeyCode.RightArrow:
                    if (window.Selected >= 0)
                {
                        OpenChild(window, window.Selected, true);
                }
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Activate(window, window.Selected);
                    break;
                default:
                    return;
            }
            evt.StopImmediatePropagation();
#if !UNITY_2023_2_OR_NEWER
            evt.PreventDefault();
#endif
        }

        internal void WindowDestroyed(HierarchyContextPopupWindow window)
        {
            int index = _levels.IndexOf(window);
            if (index < 0)
            {
                return;
            }
            _levels.RemoveAt(index);
            if (index == 0)
            {
                Close();
            }
            else
            {
                CloseFrom(index);
            }
        }

        private void CloseFrom(int depth)
        {
            bool restoreFocus = false;
            while (_levels.Count > depth)
            {
                HierarchyContextPopupWindow window = _levels[^1];
                restoreFocus |= window && EditorWindow.focusedWindow == window;
                _levels.RemoveAt(_levels.Count - 1);
                // ReSharper disable once InvertIf
                if (window)
                {
                    window.HoverGeneration++;
                    window.Close();
                }
            }
            // ReSharper disable once InvertIf
            if (!_closed && restoreFocus && _levels.Count > 0)
            {
                _focusGrace = EditorApplication.timeSinceStartup + .15;
                _levels[^1].Focus();
                _levels[^1].rootVisualElement.Focus();
            }
        }
    }
}
