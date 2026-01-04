using System;
using System.Diagnostics;

namespace SaintsHierarchy
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Method)]
    public class HierarchyGhostLeftButtonAttribute: HierarchyButtonAttribute
    {
        public override bool IsGhost => true;
        public override bool IsLeft => true;

        public HierarchyGhostLeftButtonAttribute(string label = null, string tooltip = null): base(label, tooltip){}
    }
}
