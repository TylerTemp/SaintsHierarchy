using System;
using System.Collections.Generic;
using SaintsHierarchy.Editor.Core;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    // New Hierarchy adapter. Resolver, popup, and customization use the same root snapshot as Legacy.
    [InitializeOnLoad]
    internal static class HierarchyContextMenuAdapter
    {
        private static readonly Dictionary<HierarchyView, Binding> Views = new Dictionary<HierarchyView, Binding>();

        static HierarchyContextMenuAdapter()
        {
            HierarchyWindow.BindView += Bind;
            HierarchyWindow.UnbindView += Unbind;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
            EditorApplication.delayCall += Attach;
        }

        private static void Attach()
        {
            foreach (HierarchyWindow window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
            {
                if (window.View != null) Bind(window, window.View);
            }
        }

        private static void Bind(HierarchyWindow window, HierarchyView view)
        {
            Unbind(window, view);
            Views[view] = new Binding(window, view);
        }

        private static void Unbind(HierarchyWindow window, HierarchyView view)
        {
            if (!Views.TryGetValue(view, out Binding binding)) return;
            binding.Dispose();
            Views.Remove(view);
        }

        private static void Populate(HierarchyWindow window, HierarchyView view, HierarchyViewItem item, DropdownMenu menu)
        {
            if (!HierarchyContextMenu.Enabled || !Views.TryGetValue(view, out Binding binding) || binding.EventMenu != menu) return;
            // Scene headers keep Unity's menu. Null item is the supported blank-space context.
            if (item != null && EditorUtility.EntityIdToObject(view.Source.GetEntityIdFromNode(item.Node)) is not GameObject) return;
            try
            {
                List<HierarchyContextUtil.MenuItem> snapshot = Read(menu, binding.EventInfo);
                if (binding.Menu.Show(snapshot, binding.ScreenPosition))
                {
                    // Empty native menu suppresses display after the population event finishes.
                    menu.MenuItems().Clear();
                }
            }
            catch (Exception exception)
            {
                binding.Menu.ReportFailure(exception);
            }
        }

        private static List<HierarchyContextUtil.MenuItem> Read(DropdownMenu menu, DropdownMenuEventInfo eventInfo)
        {
            List<HierarchyContextUtil.MenuItem> source = new List<HierarchyContextUtil.MenuItem>();
            foreach (DropdownMenuItem item in menu.MenuItems())
            {
                if (item is DropdownMenuSeparator separator)
                {
                    source.Add(HierarchyContextUtil.Item(separator.subMenuPath, true, false, false, null));
                }
                else if (item is DropdownMenuAction action)
                {
                    action.UpdateActionStatus(eventInfo);
                    if (action.status == DropdownMenuAction.Status.None
                        || (action.status & DropdownMenuAction.Status.Hidden) != 0)
                    {
                        continue;
                    }
                    source.Add(HierarchyContextUtil.Item(action.name, false,
                        (action.status & DropdownMenuAction.Status.Disabled) == 0,
                        (action.status & DropdownMenuAction.Status.Checked) != 0, action.Execute));
                }
            }
            return source;
        }

        private static void Dispose()
        {
            foreach (Binding binding in Views.Values) binding.Dispose();
            Views.Clear();
        }

        private sealed class Binding
        {
            internal readonly HierarchyContextMenu Menu;
            internal DropdownMenu EventMenu;
            internal DropdownMenuEventInfo EventInfo;
            internal Vector2 ScreenPosition;
            private readonly HierarchyWindow _window;
            private readonly HierarchyView _view;

            internal Binding(HierarchyWindow window, HierarchyView view)
            {
                _window = window;
                _view = view;
                Menu = new HierarchyContextMenu(window);
                view.RegisterCallback<ContextualMenuPopulateEvent>(Capture, TrickleDown.TrickleDown);
                // Run after HierarchyWindow has invoked all its public population subscribers.
                view.PopulateContextMenu += PopulateMenu;
            }

            private void PopulateMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu)
                => Populate(_window, view, item, menu);

            private void Capture(ContextualMenuPopulateEvent evt)
            {
                EventMenu = evt.target == _view ? evt.menu : null;
                EventInfo = new DropdownMenuEventInfo(evt);
                ScreenPosition = _window.position.position + evt.mousePosition;
            }

            internal void Dispose()
            {
                _view.UnregisterCallback<ContextualMenuPopulateEvent>(Capture, TrickleDown.TrickleDown);
                _view.PopulateContextMenu -= PopulateMenu;
                Menu.Dispose();
                EventMenu = null;
                EventInfo = null;
            }
        }
    }
}
