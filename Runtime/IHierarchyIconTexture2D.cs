using UnityEngine;

namespace SaintsHierarchy
{
    public interface IHierarchyIconTexture2D
    {
#if UNITY_EDITOR
        Texture2D HierarchyIconTexture2D { get; }
#endif
    }
}
