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

        public bool disableFavorites { get; set; }
        public bool saveFavoritesToProjectConfig { get; set; }
        public List<GameObjectFavorite> favorites { get; }
        // ReSharper restore InconsistentNaming

        void SaveToDisk();
    }
}
