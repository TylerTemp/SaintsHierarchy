using System;
using UnityEngine;

namespace SaintsHierarchy.Editor.Core
{

    [Serializable]
    public struct GameObjectConfig
    {
        public string globalObjectIdString;
        public string icon;
        public bool hasColor;
        public Color color;
    }
}
