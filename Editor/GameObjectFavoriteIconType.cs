using System;
using UnityEngine;

namespace SaintsHierarchy.Editor
{
    [Serializable]
    public enum GameObjectFavoriteIconType
    {
        [InspectorName("Default (Not Supported Yet)")]
        Default,
        UnityDefault,
        None,
        Custom,
    }
}
