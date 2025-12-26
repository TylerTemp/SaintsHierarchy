using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor
{
    public class SaintsHierarchyConfig: ScriptableObject
    {
        [Serializable]
        public struct GameObjectConfig
        {
            public string globalObjectIdString;
            public string icon;
        }

        [Serializable]
        public struct SceneGuidToGoConfigs
        {
            public string sceneGuid;
            public List<GameObjectConfig> configs;
        }

        public List<SceneGuidToGoConfigs> sceneGuidToGoConfigsList;
    }
}
