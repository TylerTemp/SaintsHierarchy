using UnityEngine;

namespace SaintsHierarchy
{
    public struct HierarchyUsed
    {
        public bool Used;
        public readonly Rect UsedRect;

        public HierarchyUsed(Rect usedRect)
        {
            UsedRect = usedRect;
            Used = true;
        }
    }
}
