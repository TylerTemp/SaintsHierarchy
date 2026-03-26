using System.Collections.Generic;
using UnityEditor;

namespace SaintsHierarchy.Editor
{
    [FilePath("Library/PersonalHierarchyConfig.asset", FilePathAttribute.Location.ProjectFolder)]
    public class PersonalHierarchyConfig: ScriptableSingleton<PersonalHierarchyConfig>
    {
        public bool personalEnabled;

        public bool disabled;
        public bool backgroundStrip;
        public bool gameObjectEnabledChecker;
        public bool gameObjectEnabledCheckerEveryRow;
        public bool componentIcons;
        public bool noDefaultIcon;
        public bool transparentDefaultIcon;

        public List<SceneGuidToGoConfigs> sceneGuidToGoConfigsList = new List<SceneGuidToGoConfigs>();

        public void SaveToDisk() => Save(true);
    }
}
