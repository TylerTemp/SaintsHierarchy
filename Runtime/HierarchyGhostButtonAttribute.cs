using System;
using System.Diagnostics;

namespace SaintsHierarchy
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Method)]
    public class HierarchyGhostButtonAttribute: HierarchyButtonAttribute
    {
        public override bool IsGhost => true;

        public HierarchyGhostButtonAttribute(string label = null, string tooltip = null): base(label, tooltip){}
    }
}
