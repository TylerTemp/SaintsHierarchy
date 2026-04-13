using System;
using UnityEngine;

namespace SaintsHierarchy.Editor
{
    [Serializable]
    public class GameObjectFavorite
    {
        public string globalObjectIdString;
        public string alias;
        public string icon;
        public bool hasColor;
        public Color color;
    }
}
