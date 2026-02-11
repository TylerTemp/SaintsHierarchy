using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor.Utils
{
    public static class SaintsMenu
    {
        private const string MenuRoot =
#if SAINTSHIERARCHY_DEBUG
                "SaintsHierarchy/"
#else
                "Window/Saints/Hierarchy/"
#endif
            ;

        private const string PersonalEnabledPath = MenuRoot + "Enable Personal Config";

        [MenuItem(PersonalEnabledPath)]
        public static void PersonalEnabled()
        {
            if (!PersonalHierarchyConfig.instance.personalEnabled)
            {
                if (EditorUtility.DisplayDialog("Personal Config", "Copy project config to personal config?", "Yes",
                        "No"))
                {
                    EditorUtility.SetDirty(PersonalHierarchyConfig.instance);
                    PersonalHierarchyConfig.instance.disabled = SaintsHierarchyConfig.instance.disabled;
                    PersonalHierarchyConfig.instance.backgroundStrip = SaintsHierarchyConfig.instance.backgroundStrip;
                    PersonalHierarchyConfig.instance.componentIcons = SaintsHierarchyConfig.instance.componentIcons;
                    PersonalHierarchyConfig.instance.gameObjectEnabledChecker = SaintsHierarchyConfig.instance.gameObjectEnabledChecker;
                    PersonalHierarchyConfig.instance.sceneGuidToGoConfigsList = SaintsHierarchyConfig.instance.sceneGuidToGoConfigsList.ToList();
                    PersonalHierarchyConfig.instance.SaveToDisk();
                }
            }
            EditorUtility.SetDirty(PersonalHierarchyConfig.instance);
            PersonalHierarchyConfig.instance.personalEnabled = !PersonalHierarchyConfig.instance.personalEnabled;
            PersonalHierarchyConfig.instance.SaveToDisk();
            Refresh();
        }

        private const string DisablePath = MenuRoot + "Disable Saints Hierarchy";

        [MenuItem(DisablePath)]
        public static void DisableSaintsHierarchy()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            if (personalEnabled)
            {
                EditorUtility.SetDirty(PersonalHierarchyConfig.instance);
                PersonalHierarchyConfig.instance.disabled = !PersonalHierarchyConfig.instance.disabled;
                PersonalHierarchyConfig.instance.SaveToDisk();
            }
            else
            {
                EditorUtility.SetDirty(SaintsHierarchyConfig.instance);
                SaintsHierarchyConfig.instance.disabled = !SaintsHierarchyConfig.instance.disabled;
                SaintsHierarchyConfig.instance.SaveToDisk();
            }

            Refresh();
        }

        private const string BackgroundStripPath = MenuRoot + "Background Strip";

        [MenuItem(BackgroundStripPath)]
        public static void BackgroundStrip()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            if (personalEnabled)
            {
                EditorUtility.SetDirty(PersonalHierarchyConfig.instance);
                PersonalHierarchyConfig.instance.backgroundStrip = !PersonalHierarchyConfig.instance.backgroundStrip;
                PersonalHierarchyConfig.instance.SaveToDisk();
            }
            else
            {
                EditorUtility.SetDirty(SaintsHierarchyConfig.instance);
                SaintsHierarchyConfig.instance.backgroundStrip = !SaintsHierarchyConfig.instance.backgroundStrip;
                SaintsHierarchyConfig.instance.SaveToDisk();
            }

            Refresh();
        }

        private const string GameObjectEnabledCheckerPath = MenuRoot + "GameObject Enabled Checker";

        [MenuItem(GameObjectEnabledCheckerPath)]
        public static void GameObjectEnabledChecker()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            if (personalEnabled)
            {
                EditorUtility.SetDirty(PersonalHierarchyConfig.instance);
                PersonalHierarchyConfig.instance.gameObjectEnabledChecker = !PersonalHierarchyConfig.instance.gameObjectEnabledChecker;
                PersonalHierarchyConfig.instance.SaveToDisk();
            }
            else
            {
                EditorUtility.SetDirty(SaintsHierarchyConfig.instance);
                SaintsHierarchyConfig.instance.gameObjectEnabledChecker = !SaintsHierarchyConfig.instance.gameObjectEnabledChecker;
                SaintsHierarchyConfig.instance.SaveToDisk();
            }

            Refresh();
        }

        private const string ComponentIconsPath = MenuRoot + "Component Icons";

        [MenuItem(ComponentIconsPath)]
        public static void ComponentIcons()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            if (personalEnabled)
            {
                EditorUtility.SetDirty(PersonalHierarchyConfig.instance);
                PersonalHierarchyConfig.instance.componentIcons = !PersonalHierarchyConfig.instance.componentIcons;
                PersonalHierarchyConfig.instance.SaveToDisk();
            }
            else
            {
                EditorUtility.SetDirty(SaintsHierarchyConfig.instance);
                SaintsHierarchyConfig.instance.componentIcons = !SaintsHierarchyConfig.instance.componentIcons;
                SaintsHierarchyConfig.instance.SaveToDisk();
            }

            Refresh();
        }

        private const string NoDefaultIconPath = MenuRoot + "No Default Icon";

        [MenuItem(NoDefaultIconPath)]
        public static void NoDefaultIcon()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            if (personalEnabled)
            {
                EditorUtility.SetDirty(PersonalHierarchyConfig.instance);
                PersonalHierarchyConfig.instance.noDefaultIcon = !PersonalHierarchyConfig.instance.noDefaultIcon;
                PersonalHierarchyConfig.instance.SaveToDisk();
            }
            else
            {
                EditorUtility.SetDirty(SaintsHierarchyConfig.instance);
                SaintsHierarchyConfig.instance.noDefaultIcon = !SaintsHierarchyConfig.instance.noDefaultIcon;
                SaintsHierarchyConfig.instance.SaveToDisk();
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
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            Menu.SetChecked(PersonalEnabledPath, personalEnabled);

            bool disabled = personalEnabled? PersonalHierarchyConfig.instance.disabled : SaintsHierarchyConfig.instance.disabled;
            Menu.SetChecked(DisablePath, disabled);

            bool backgroundStrip = personalEnabled?  PersonalHierarchyConfig.instance.backgroundStrip : SaintsHierarchyConfig.instance.backgroundStrip;
            Menu.SetChecked(BackgroundStripPath, backgroundStrip);

            bool gameObjectEnabledChecker = personalEnabled? PersonalHierarchyConfig.instance.gameObjectEnabledChecker : SaintsHierarchyConfig.instance.gameObjectEnabledChecker;
            Menu.SetChecked(GameObjectEnabledCheckerPath, gameObjectEnabledChecker);

            bool componentIcons = personalEnabled? PersonalHierarchyConfig.instance.componentIcons : SaintsHierarchyConfig.instance.componentIcons;
            Menu.SetChecked(ComponentIconsPath, componentIcons);

            bool noDefaultIcon = personalEnabled? PersonalHierarchyConfig.instance.noDefaultIcon : SaintsHierarchyConfig.instance.noDefaultIcon;
            Menu.SetChecked(NoDefaultIconPath, noDefaultIcon);
        }
    }
}
