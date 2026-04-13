using System.Collections.Generic;

namespace SaintsHierarchy.Editor
{
    public interface IConfig
    {
        // ReSharper disable InconsistentNaming
        public bool disabled { get; }
        public bool backgroundStrip { get; }
        public bool gameObjectEnabledChecker { get; }
        public bool gameObjectEnabledCheckerEveryRow { get; }
        public bool componentIcons { get; }
        public bool noDefaultIcon { get; }
        public bool transparentDefaultIcon { get; }

        public List<SceneGuidToGoConfigs> sceneGuidToGoConfigsList { get; }
        public List<SceneGuidToGoFavorites> sceneGuidToGoFavoritesList { get; }
        // ReSharper restore InconsistentNaming

        void SaveToDisk();
    }
}
