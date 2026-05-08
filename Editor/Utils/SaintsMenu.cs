using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor.Utils
{
    public static class SaintsMenu
    {
        private const string MenuRoot =
#if SAINTSHIERARCHY_DEBUG
                "Saints Hierarchy/"
#else
                "Tools/Saints Hierarchy/"
#endif
            ;

        private const string DisablePath = MenuRoot + "Disable Saints Hierarchy";

        [MenuItem(DisablePath, priority=-101)]
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

        private const string PersonalEnabledPath = MenuRoot + "Enable Personal Config";

        [MenuItem(PersonalEnabledPath, priority=-100)]
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
                    PersonalHierarchyConfig.instance.componentIconsForGeneralScripts = SaintsHierarchyConfig.instance.componentIconsForGeneralScripts;
                    PersonalHierarchyConfig.instance.componentIconsForTransform = SaintsHierarchyConfig.instance.componentIconsForTransform;
                    PersonalHierarchyConfig.instance.gameObjectEnabledChecker = SaintsHierarchyConfig.instance.gameObjectEnabledChecker;
                    PersonalHierarchyConfig.instance.gameObjectEnabledCheckerEveryRow = SaintsHierarchyConfig.instance.gameObjectEnabledCheckerEveryRow;
                    PersonalHierarchyConfig.instance.sceneGuidToGoConfigsList = SaintsHierarchyConfig.instance.sceneGuidToGoConfigsList.ToList();
                    PersonalHierarchyConfig.instance.disableFavorites = SaintsHierarchyConfig.instance.disableFavorites;
                    PersonalHierarchyConfig.instance.favorites = SaintsHierarchyConfig.instance.favorites.ToList();
                    PersonalHierarchyConfig.instance.disableSceneSelector = SaintsHierarchyConfig.instance.disableSceneSelector;
                    PersonalHierarchyConfig.instance.SaveToDisk();
                }
            }
            EditorUtility.SetDirty(PersonalHierarchyConfig.instance);
            PersonalHierarchyConfig.instance.personalEnabled = !PersonalHierarchyConfig.instance.personalEnabled;
            PersonalHierarchyConfig.instance.SaveToDisk();
            Refresh();
        }

        private const string BackgroundStripPath = MenuRoot + "Background Strip";
        [MenuItem(BackgroundStripPath, priority = 0)]
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

        private const string ComponentIconsPath = MenuRoot + "Component Icons";
        [MenuItem(ComponentIconsPath, priority=1)]
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

        private const string ComponentIconsForGeneralScriptsPath = MenuRoot + "Component Icons For General Scripts";
        [MenuItem(ComponentIconsForGeneralScriptsPath, priority=2)]
        public static void ComponentIconsForGeneralScripts()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            if (personalEnabled)
            {
                EditorUtility.SetDirty(PersonalHierarchyConfig.instance);
                PersonalHierarchyConfig.instance.componentIconsForGeneralScripts = !PersonalHierarchyConfig.instance.componentIconsForGeneralScripts;
                PersonalHierarchyConfig.instance.SaveToDisk();
            }
            else
            {
                EditorUtility.SetDirty(SaintsHierarchyConfig.instance);
                SaintsHierarchyConfig.instance.componentIconsForGeneralScripts = !SaintsHierarchyConfig.instance.componentIconsForGeneralScripts;
                SaintsHierarchyConfig.instance.SaveToDisk();
            }

            Refresh();
        }

        [MenuItem(ComponentIconsForGeneralScriptsPath, true)]
        private static bool ValidateComponentIconsForGeneralScripts()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            return personalEnabled
                ? PersonalHierarchyConfig.instance.componentIcons
                : SaintsHierarchyConfig.instance.componentIcons;
        }

        private const string ComponentIconsForTransformPath = MenuRoot + "Component Icons For Transform";
        [MenuItem(ComponentIconsForTransformPath, priority=3)]
        public static void ComponentIconsForTransform()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            if (personalEnabled)
            {
                EditorUtility.SetDirty(PersonalHierarchyConfig.instance);
                PersonalHierarchyConfig.instance.componentIconsForTransform = !PersonalHierarchyConfig.instance.componentIconsForTransform;
                PersonalHierarchyConfig.instance.SaveToDisk();
            }
            else
            {
                EditorUtility.SetDirty(SaintsHierarchyConfig.instance);
                SaintsHierarchyConfig.instance.componentIconsForTransform = !SaintsHierarchyConfig.instance.componentIconsForTransform;
                SaintsHierarchyConfig.instance.SaveToDisk();
            }

            Refresh();
        }

        [MenuItem(ComponentIconsForTransformPath, true)]
        private static bool ValidateComponentIconsForTransform()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            return personalEnabled
                ? PersonalHierarchyConfig.instance.componentIcons
                : SaintsHierarchyConfig.instance.componentIcons;
        }

        #region GameObject Enabled Checker
        private const string GameObjectEnabledCheckerPath = MenuRoot + "GameObject Enabled Checker";
        [MenuItem(GameObjectEnabledCheckerPath, priority=4)]
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

        private const string GameObjectEnabledCheckerEveryRowPath = MenuRoot + "GameObject Enabled Checker Every Row";
        [MenuItem(GameObjectEnabledCheckerEveryRowPath, priority=5)]
        public static void GameObjectEnabledCheckerEveryRow()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            if (personalEnabled)
            {
                EditorUtility.SetDirty(PersonalHierarchyConfig.instance);
                PersonalHierarchyConfig.instance.gameObjectEnabledCheckerEveryRow = !PersonalHierarchyConfig.instance.gameObjectEnabledCheckerEveryRow;
                PersonalHierarchyConfig.instance.SaveToDisk();
            }
            else
            {
                EditorUtility.SetDirty(SaintsHierarchyConfig.instance);
                SaintsHierarchyConfig.instance.gameObjectEnabledCheckerEveryRow = !SaintsHierarchyConfig.instance.gameObjectEnabledCheckerEveryRow;
                SaintsHierarchyConfig.instance.SaveToDisk();
            }

            Refresh();
        }

        [MenuItem(GameObjectEnabledCheckerEveryRowPath, true)]
        private static bool ValidateGameObjectEnabledCheckerEveryRowPath()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            return personalEnabled
                ? PersonalHierarchyConfig.instance.gameObjectEnabledChecker
                : SaintsHierarchyConfig.instance.gameObjectEnabledChecker;
        }
        #endregion

        #region Default Icon
        private const string NoDefaultIconPath = MenuRoot + "No Default Icon";
        [MenuItem(NoDefaultIconPath, priority = 6)]
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

        private const string TransparentDefaultIconPath = MenuRoot + "Transparent Default Icon";
        [MenuItem(TransparentDefaultIconPath, priority = 7)]
        public static void TransparentDefaultIcon()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            if (personalEnabled)
            {
                EditorUtility.SetDirty(PersonalHierarchyConfig.instance);
                PersonalHierarchyConfig.instance.transparentDefaultIcon = !PersonalHierarchyConfig.instance.transparentDefaultIcon;
                PersonalHierarchyConfig.instance.SaveToDisk();
            }
            else
            {
                EditorUtility.SetDirty(SaintsHierarchyConfig.instance);
                SaintsHierarchyConfig.instance.transparentDefaultIcon = !SaintsHierarchyConfig.instance.transparentDefaultIcon;
                SaintsHierarchyConfig.instance.SaveToDisk();
            }

            Refresh();
        }
        [MenuItem(TransparentDefaultIconPath, true)]
        private static bool ValidateTransparentDefaultIcon()
        {
            bool personalEnabled = PersonalHierarchyConfig.instance.personalEnabled;
            if (personalEnabled)
            {
                return !PersonalHierarchyConfig.instance.noDefaultIcon;
            }

            return !SaintsHierarchyConfig.instance.noDefaultIcon;
        }
        #endregion

        private const string DisableFavoritesPath = MenuRoot + "Disable Favorites";

        [MenuItem(DisableFavoritesPath, priority = 8)]
        public static void DisableFavorites()
        {
            IConfig config = Util.GetUsingConfig();
            EditorUtility.SetDirty((Object)config);
            config.disableFavorites = !config.disableFavorites;
            config.SaveToDisk();

            if (!config.disableFavorites)
            {
                SaintsHierarchyWindow.OnLoad();
            }

            Refresh();
        }

        private const string SaveFavoritesToProjectConfigPath = MenuRoot + "Save Favorites To Project Config";

        [MenuItem(SaveFavoritesToProjectConfigPath, priority = 9)]
        public static void SaveFavoritesToProjectConfig()
        {
            IConfig config = Util.GetUsingConfig();
            EditorUtility.SetDirty((Object)config);
            config.saveFavoritesToProjectConfig = !config.saveFavoritesToProjectConfig;
            if (config.saveFavoritesToProjectConfig)
            {
                if (PersonalHierarchyConfig.instance.favorites.Count > 0 && EditorUtility.DisplayDialog("Personal Config", "Copy personal favorite config to project config?",
                        "Yes",
                        "No"))
                {
                    SaintsHierarchyConfig.instance.favorites.Clear();
                    SaintsHierarchyConfig.instance.favorites.AddRange(PersonalHierarchyConfig.instance.favorites);
                }
            }
            config.SaveToDisk();

            Refresh();
        }

        [MenuItem(SaveFavoritesToProjectConfigPath, true)]
        public static bool SaveFavoritesToProjectConfigValidate()
        {
            IConfig config = Util.GetUsingConfig();
            return !config.disableFavorites;
        }

        #region Scene Selector

            private const string DisableSceneSelectorPath = MenuRoot + "Disable Scene Selector";

        [MenuItem(DisableSceneSelectorPath, priority = 10)]
        public static void DisableSceneSelector()
        {
            IConfig config = Util.GetUsingConfig();
            EditorUtility.SetDirty((Object)config);
            config.disableSceneSelector = !config.disableSceneSelector;
            config.SaveToDisk();

            Refresh();
        }

        [MenuItem(DisableSceneSelectorPath, true)]
        public static bool DisableSceneSelectorValidate()
        {
            IConfig config = Util.GetUsingConfig();
            return !config.disableFavorites;
        }

        #endregion


        private static void Refresh()
        {
            EditorApplication.RepaintHierarchyWindow();
            Checkmark();
        }

        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            EditorApplication.delayCall += Checkmark;
        }

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

            bool gameObjectEnabledCheckerEveryRow = personalEnabled? PersonalHierarchyConfig.instance.gameObjectEnabledCheckerEveryRow : SaintsHierarchyConfig.instance.gameObjectEnabledCheckerEveryRow;
            Menu.SetChecked(GameObjectEnabledCheckerEveryRowPath, gameObjectEnabledCheckerEveryRow);

            bool componentIcons = personalEnabled? PersonalHierarchyConfig.instance.componentIcons : SaintsHierarchyConfig.instance.componentIcons;
            Menu.SetChecked(ComponentIconsPath, componentIcons);

            bool componentIconsForGeneralScripts = personalEnabled
                ? PersonalHierarchyConfig.instance.componentIconsForGeneralScripts
                : SaintsHierarchyConfig.instance.componentIconsForGeneralScripts;
            Menu.SetChecked(ComponentIconsForGeneralScriptsPath, componentIcons && componentIconsForGeneralScripts);

            bool componentIconsForTransform = personalEnabled
                ? PersonalHierarchyConfig.instance.componentIconsForTransform
                : SaintsHierarchyConfig.instance.componentIconsForTransform;
            Menu.SetChecked(ComponentIconsForTransformPath, componentIcons && componentIconsForTransform);

            bool noDefaultIcon = personalEnabled? PersonalHierarchyConfig.instance.noDefaultIcon : SaintsHierarchyConfig.instance.noDefaultIcon;
            Menu.SetChecked(NoDefaultIconPath, noDefaultIcon);

            bool transparentDefaultIcon = personalEnabled? PersonalHierarchyConfig.instance.transparentDefaultIcon : SaintsHierarchyConfig.instance.transparentDefaultIcon;
            Menu.SetChecked(TransparentDefaultIconPath, transparentDefaultIcon);

            bool disableFavorites = Util.GetUsingConfig().disableFavorites;
            Menu.SetChecked(DisableFavoritesPath, disableFavorites);

            bool saveFavoritesToProjectConfig = Util.GetUsingConfig().saveFavoritesToProjectConfig;
            Menu.SetChecked(SaveFavoritesToProjectConfigPath, saveFavoritesToProjectConfig);

            bool disableSceneSelector = Util.GetUsingConfig().disableSceneSelector;
            Menu.SetChecked(DisableSceneSelectorPath, disableSceneSelector);
        }
    }
}
