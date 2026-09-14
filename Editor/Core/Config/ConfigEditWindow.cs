#if (!UNITY_6000_6_OR_NEWER || SAINTSHIERARCHY_LEGACY) && !SAINTSHIERARCHY_NEW
#define SAINTSHIERARCHY_USE_LEGACY
#endif

using SaintsHierarchy.Editor.Core.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.Core.Config
{
    public class ConfigEditWindow : EditorWindow
    {
        private Toggle _personalEnabled;
        private ScrollView _fields;
        private SerializedObject _serializedConfig;

        private void OnEnable()
        {
            HierarchyEditorEvents.InitializeRequested += RefreshTarget;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            HierarchyEditorEvents.InitializeRequested -= RefreshTarget;
            Undo.undoRedoPerformed -= OnUndoRedo;
            ReleaseTarget();
        }

        public void CreateGUI()
        {
            ReleaseTarget();
            rootVisualElement.Clear();
            titleContent = new GUIContent("Saints Hierarchy Config");
            minSize = new Vector2(360, 240);

            const string workingMode = "Working Mode: " +
#if SAINTSHIERARCHY_USE_LEGACY
                "Legacy"
#else
                "New Hierarchy"
#endif
            ;

            rootVisualElement.Add(new HelpBox(workingMode, HelpBoxMessageType.Info)
            {
                style =
                {
                    marginBottom = 5,
                },
            });

            _personalEnabled = CreateLeftToggle("Enable Personal Config");
            _personalEnabled.RegisterValueChangedCallback(evt =>
            {
                PersonalHierarchyConfig personal = PersonalHierarchyConfig.instance;
                Undo.RecordObject(personal, "Toggle Personal Config");
                personal.personalEnabled = evt.newValue;
                EditorUtility.SetDirty(personal);
                personal.SaveToDisk();
                RefreshTarget();
                SaintsMenu.Refresh();
            });
            rootVisualElement.Add(_personalEnabled);

            _fields = new ScrollView { style = { flexGrow = 1 } };
            rootVisualElement.Add(_fields);
            RefreshTarget();
        }

        private void RefreshTarget()
        {
            if (_fields == null)
            {
                return;
            }

            _personalEnabled.SetValueWithoutNotify(PersonalHierarchyConfig.instance.personalEnabled);
            Object target = (Object)Util.GetUsingConfig();
            if (_serializedConfig != null && _serializedConfig.targetObject == target)
            {
                return;
            }

            ReleaseTarget();
            _fields.Clear();
            _serializedConfig = new SerializedObject(target);
            SerializedProperty disabled = _serializedConfig.FindProperty($"<{nameof(IConfig.disabled)}>k__BackingField");
#if SAINTSHIERARCHY_USE_LEGACY
            SerializedProperty enabledChecker = _serializedConfig.FindProperty($"<{nameof(IConfig.gameObjectEnabledChecker)}>k__BackingField");
            SerializedProperty componentIcons = _serializedConfig.FindProperty($"<{nameof(IConfig.componentIcons)}>k__BackingField");
#endif
            SerializedProperty noDefaultIcon = _serializedConfig.FindProperty($"<{nameof(IConfig.noDefaultIcon)}>k__BackingField");
            SerializedProperty disableFavorites = _serializedConfig.FindProperty($"<{nameof(IConfig.disableFavorites)}>k__BackingField");

            Toggle indentGuidesToggle = AddToggle(_serializedConfig.FindProperty($"<{nameof(IConfig.indentGuides)}>k__BackingField"));
            AddToggle(disabled);
#if SAINTSHIERARCHY_USE_LEGACY
            Toggle backgroundStripToggle = AddToggle(_serializedConfig.FindProperty($"<{nameof(IConfig.backgroundStrip)}>k__BackingField"));
            Toggle enabledCheckerToggle = AddToggle(enabledChecker);
            Toggle everyRowToggle = AddToggle(_serializedConfig.FindProperty($"<{nameof(IConfig.gameObjectEnabledCheckerEveryRow)}>k__BackingField"));
            Toggle componentIconsToggle = AddToggle(componentIcons);
#endif
            Toggle generalScriptsToggle = AddToggle(_serializedConfig.FindProperty($"<{nameof(IConfig.componentIconsForGeneralScripts)}>k__BackingField"));
            Toggle transformToggle = AddToggle(_serializedConfig.FindProperty($"<{nameof(IConfig.componentIconsForTransform)}>k__BackingField"));
            Toggle noDefaultIconToggle = AddToggle(noDefaultIcon);
            Toggle transparentIconToggle = AddToggle(_serializedConfig.FindProperty($"<{nameof(IConfig.transparentDefaultIcon)}>k__BackingField"));
            Toggle disableFavoritesToggle = AddToggle(disableFavorites);
            Toggle clickToInspectToggle = AddToggle(_serializedConfig.FindProperty($"<{nameof(IConfig.FavoriteClickToInspect)}>k__BackingField"));
            Toggle saveFavoritesToggle = AddToggle(_serializedConfig.FindProperty($"<{nameof(IConfig.saveFavoritesToProjectConfig)}>k__BackingField"));
            Toggle sceneSelectorToggle = AddToggle(_serializedConfig.FindProperty($"<{nameof(IConfig.disableSceneSelector)}>k__BackingField"));
            Toggle sceneFinderToggle = AddToggle(_serializedConfig.FindProperty($"<{nameof(IConfig.disableSceneFindInProject)}>k__BackingField"));
            _fields.Bind(_serializedConfig);

            void UpdateEnabledStates(SerializedProperty _)
            {
                bool hierarchyEnabled = !disabled.boolValue;
                indentGuidesToggle.SetEnabled(hierarchyEnabled);
#if SAINTSHIERARCHY_USE_LEGACY
                backgroundStripToggle.SetEnabled(hierarchyEnabled);
                enabledCheckerToggle.SetEnabled(hierarchyEnabled);
                everyRowToggle.SetEnabled(hierarchyEnabled && enabledChecker.boolValue);
                componentIconsToggle.SetEnabled(hierarchyEnabled);
                bool showComponentOptions = hierarchyEnabled && componentIcons.boolValue;
#else
                // The new hierarchy's component column is controlled by Unity's own menu.
                const bool showComponentOptions = true;
#endif
                generalScriptsToggle.SetEnabled(showComponentOptions);
                transformToggle.SetEnabled(showComponentOptions);
                noDefaultIconToggle.SetEnabled(hierarchyEnabled);
                transparentIconToggle.SetEnabled(hierarchyEnabled && !noDefaultIcon.boolValue);
                disableFavoritesToggle.SetEnabled(hierarchyEnabled);
                bool favoritesEnabled = hierarchyEnabled && !disableFavorites.boolValue;
                clickToInspectToggle.SetEnabled(favoritesEnabled);
                saveFavoritesToggle.SetEnabled(favoritesEnabled);
                sceneSelectorToggle.SetEnabled(hierarchyEnabled);
                sceneFinderToggle.SetEnabled(hierarchyEnabled);
            }

            UpdateEnabledStates(null);
            _fields.TrackPropertyValue(disabled, UpdateEnabledStates);
#if SAINTSHIERARCHY_USE_LEGACY
            _fields.TrackPropertyValue(enabledChecker, UpdateEnabledStates);
            _fields.TrackPropertyValue(componentIcons, UpdateEnabledStates);
#endif
            _fields.TrackPropertyValue(noDefaultIcon, UpdateEnabledStates);
            _fields.TrackPropertyValue(disableFavorites, UpdateEnabledStates);
        }

        private Toggle AddToggle(SerializedProperty property)
        {
            Toggle toggle = CreateLeftToggle(property.displayName);
            toggle.bindingPath = property.propertyPath;
            toggle.tooltip = property.tooltip;
            toggle.SetValueWithoutNotify(property.boolValue);
            toggle.RegisterValueChangedCallback(evt =>
            {
                _serializedConfig.UpdateIfRequiredOrScript();
                property.boolValue = evt.newValue;
                SaveConfig();
            });
            _fields.Add(toggle);
            return toggle;
        }

        private static Toggle CreateLeftToggle(string label)
        {
            // Match SaintsField's LeftToggle.uss without requiring its editor assembly.
            Toggle toggle = new Toggle(label)
            {
                style =
                {
                    flexDirection = FlexDirection.RowReverse,
                    justifyContent = Justify.FlexEnd,
                },
            };
            VisualElement input = toggle.Q(className: Toggle.inputUssClassName);
            input.style.flexGrow = 0;
            input.style.marginRight = 2;
            return toggle;
        }

        private void SaveConfig()
        {
            if (_serializedConfig == null)
            {
                return;
            }

            _serializedConfig.ApplyModifiedProperties();
            Object target = _serializedConfig.targetObject;
            EditorUtility.SetDirty(target);
            ((IConfig)target).SaveToDisk();
            SaintsMenu.Refresh();
        }

        private void OnUndoRedo()
        {
            PersonalHierarchyConfig.instance.SaveToDisk();
            _serializedConfig?.Update();
            SaveConfig();
            RefreshTarget();
        }

        private void ReleaseTarget()
        {
            _fields?.Unbind();
            _serializedConfig?.Dispose();
            _serializedConfig = null;
        }
    }
}
