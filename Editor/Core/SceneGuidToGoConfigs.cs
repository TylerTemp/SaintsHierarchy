using System;
using System.Collections.Generic;

namespace SaintsHierarchy.Editor.Core
{
    [Serializable]
    public struct SceneGuidToGoConfigs
    {
        public string sceneGuid;
        public List<GameObjectConfig> configs;
    }
}
