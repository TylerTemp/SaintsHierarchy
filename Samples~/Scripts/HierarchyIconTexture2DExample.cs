using UnityEngine;

namespace SaintsHierarchy.Samples.Scripts
{
    public class HierarchyIconTexture2DExample: MonoBehaviour, IHierarchyIconTexture2D
    {
        public Texture2D texture2D;
#if UNITY_EDITOR
        public Texture2D HierarchyIconTexture2D => texture2D;
#endif
    }
}
