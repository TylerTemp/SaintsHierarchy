using System.Linq;
using SaintsHierarchy.Editor.Core;
using SaintsHierarchy.Editor.Core.Utils;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public static class SaintsHierarchyEntrance
    {
        public const string ExtraAddedClass = "saints-hierarchy-extra";
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            HierarchyWindow.BindView -= OnBindView;
            HierarchyWindow.BindView += OnBindView;
            HierarchyWindow.UnbindView -= OnUnbindView;
            HierarchyWindow.UnbindView += OnUnbindView;
            EditorApplication.delayCall += AttachFavoritePanels;
            HierarchyWindow.BindViewItem -= OnBindViewItem;
            HierarchyWindow.BindViewItem += OnBindViewItem;
            HierarchyWindow.UnbindViewItem -= OnUnbindViewItem;
            HierarchyWindow.UnbindViewItem += OnUnbindViewItem;
            HierarchyEditorEvents.ReloadAllScenesRequested -= RefreshVisibleItems;
            HierarchyEditorEvents.ReloadAllScenesRequested += RefreshVisibleItems;
            HierarchyEditorEvents.InitializeRequested -= RefreshVisibleItems;
            HierarchyEditorEvents.InitializeRequested += RefreshVisibleItems;
            EditorApplication.hierarchyChanged -= RefreshVisibleItems;
            EditorApplication.hierarchyChanged += RefreshVisibleItems;
        }

        private static void AttachFavoritePanels()
        {
            foreach (HierarchyWindow window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
            {
                OnBindView(window, window.View);
            }
        }

        private static void OnBindView(HierarchyWindow window, HierarchyView view)
        {
            // ReSharper disable once InvertIf
            if (!Util.GetUsingConfig().disableFavorites && window.rootVisualElement.Q<FavoritePanelElement>() == null)
            {
                VisualElement parent = window.View.parent;
                parent.Insert(parent.IndexOf(window.View), new FavoritePanelElement());
            }
        }

        private static void OnUnbindView(HierarchyWindow window, HierarchyView view)
        {
            window.rootVisualElement.Q<FavoritePanelElement>()?.RemoveFromHierarchy();
        }

        private static void OnBindViewItem(
            HierarchyWindow window,
            HierarchyView view,
            HierarchyViewItem item)
        {
            ClearExtraAdded(item);
            IConfig usingConfig = Util.GetUsingConfig();
            if (usingConfig.disabled)
            {
                return;
            }

            SaintsHierarchyProcessMainIcon.ProcessMainIcon(view, item, usingConfig);
            SaintsHierarchyProcessName.ProcessName(view, item, usingConfig);
            SaintsHierarchyProcessConfig.ProcessConfig(item);
            SaintsHierarchyProcessIndent.ProcessIndent(item);
        }

        private static void RefreshVisibleItems()
        {
            foreach (HierarchyWindow window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
            {
                foreach (HierarchyViewItem item in window.rootVisualElement.Query<HierarchyViewItem>().ToList())
                {
                    SaintsHierarchyProcessConfig.ProcessConfig(item);
                    SaintsHierarchyProcessIndent.ProcessIndent(item);
                }
            }
        }

        private static void OnUnbindViewItem(
            HierarchyWindow window,
            HierarchyView view,
            HierarchyViewItem item)
        {
            ClearExtraAdded(item);
        }

        private static void ClearExtraAdded(HierarchyViewItem item)
        {
            SaintsHierarchyProcessIndent.Clear(item);
            SaintsHierarchyProcessConfig.Clear(item);
            foreach (VisualElement extra in item.Query<VisualElement>(className: ExtraAddedClass).ToList())
            {
                extra.RemoveFromHierarchy();
            }
        }
    }
}
