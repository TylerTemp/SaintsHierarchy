using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor.Utils
{
    public static class SaintsMenu
    {
        private const string MenuRoot =
#if SAINTSHIERARCHY_DEBUG
                "Saints/"
#else
                "Window/Saints/Hierarchy/"
#endif
            ;

        private const string DisablePath = MenuRoot + "Disable Saints Hierarchy";

        [MenuItem(DisablePath)]
        public static void DisableSaintsHierarchy()
        {
            SaintsHierarchyConfig config = Util.EnsureConfig();
            if (config != null)
            {
                EditorUtility.SetDirty(config);
                config.disabled = !config.disabled;
            }
            else
            {
                Debug.LogWarning("SaintsHierarchy config not found");
            }

            Refresh();
        }

        private const string BackgroundStripPath = MenuRoot + "Background Strip";

        [MenuItem(BackgroundStripPath)]
        public static void BackgroundStrip()
        {
            SaintsHierarchyConfig config = Util.EnsureConfig();
            if (config != null)
            {
                EditorUtility.SetDirty(config);
                config.backgroundStrip = !config.backgroundStrip;
            }
            else
            {
                Debug.LogWarning("SaintsHierarchy config not found");
            }

            Refresh();
        }

        private const string GameObjectEnabledCheckerPath = MenuRoot + "GameObject Enabled Checker";

        [MenuItem(GameObjectEnabledCheckerPath)]
        public static void GameObjectEnabledChecker()
        {
            SaintsHierarchyConfig config = Util.EnsureConfig();
            if (config != null)
            {
                EditorUtility.SetDirty(config);
                config.gameObjectEnabledChecker = !config.gameObjectEnabledChecker;
            }
            else
            {
                Debug.LogWarning("SaintsHierarchy config not found");
            }

            Refresh();
        }

        private const string ComponentIconsPath = MenuRoot + "Component Icons";

        [MenuItem(ComponentIconsPath)]
        public static void ComponentIcons()
        {
            SaintsHierarchyConfig config = Util.EnsureConfig();
            if (config != null)
            {
                EditorUtility.SetDirty(config);
                config.componentIcons = !config.componentIcons;
            }
            else
            {
                Debug.LogWarning("SaintsHierarchy config not found");
            }

            Refresh();
        }

        private static void Refresh()
        {
            EditorApplication.RepaintHierarchyWindow();
            Checkmark();
        }

        [InitializeOnLoadMethod]
        private static void Checkmark()
        {
            SaintsHierarchyConfig config = Util.EnsureConfig();
            bool disabled = config == null || config.disabled;
            Menu.SetChecked(DisablePath, disabled);

            bool backgroundStrip = config != null && config.backgroundStrip;
            Menu.SetChecked(BackgroundStripPath, backgroundStrip);

            bool gameObjectEnabledChecker = config != null && config.gameObjectEnabledChecker;
            Menu.SetChecked(GameObjectEnabledCheckerPath, gameObjectEnabledChecker);

            bool componentIcons = config != null && config.componentIcons;
            Menu.SetChecked(ComponentIconsPath, componentIcons);
        }
    }
}
