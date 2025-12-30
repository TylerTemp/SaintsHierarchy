using UnityEngine;

namespace SaintsHierarchy
{
    public struct HierarchyUsed
    {
        public readonly Rect UsedRect;

        public HierarchyUsed(Rect usedRect)
        {
            UsedRect = usedRect;
        }
    }
}
