using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace SaintsHierarchy.Editor
{
    [FilePath("Assets/Editor Default Resources/SaintsHierarchy/SaintsHierarchyConfig.asset", FilePathAttribute.Location.ProjectFolder)]
#if SAINTSHIERARCHY_DEBUG
    [CreateAssetMenu(fileName = "SaintsHierarchyConfig", menuName = "Debug/Saints Hierarchy Config")]
#endif

    public class SaintsHierarchyConfig: ScriptableSingleton<SaintsHierarchyConfig>, IConfig
    {
        private void OnEnable()
        {
            hideFlags &= ~HideFlags.NotEditable;
            hideFlags &= ~HideFlags.HideInInspector;
        }

        // ReSharper disable InconsistentNaming
        [field: SerializeField, FormerlySerializedAs("disabled")] public bool disabled { get; set; }
        [field: SerializeField, FormerlySerializedAs("backgroundStrip")] public bool backgroundStrip { get; set; }
        [field: SerializeField, FormerlySerializedAs("gameObjectEnabledChecker")] public bool gameObjectEnabledChecker { get; set; }
        [field: SerializeField, FormerlySerializedAs("gameObjectEnabledCheckerEveryRow")] public bool gameObjectEnabledCheckerEveryRow { get; set; }
        [field: SerializeField, FormerlySerializedAs("componentIcons")] public bool componentIcons { get; set; }
        [field: SerializeField, FormerlySerializedAs("noDefaultIcon")] public bool noDefaultIcon { get; set; }
        [field: SerializeField, FormerlySerializedAs("transparentDefaultIcon")] public bool transparentDefaultIcon { get; set; }

        [field: SerializeField, FormerlySerializedAs("sceneGuidToGoConfigsList")] public List<SceneGuidToGoConfigs> sceneGuidToGoConfigsList { get; set; } = new List<SceneGuidToGoConfigs>();
        [field: SerializeField] public List<SceneGuidToGoFavorites> sceneGuidToGoFavoritesList { get; set; } = new List<SceneGuidToGoFavorites>();
        // ReSharper restore InconsistentNaming

        public void SaveToDisk() => Save(true);
    }
}
