using System;
using System.Collections.Generic;

namespace SaintsHierarchy.Editor
{
    [Serializable]
    public struct SceneGuidToGoConfigs
    {
        public string sceneGuid;
        public List<GameObjectConfig> configs;
    }
}
