using System;
using System.Diagnostics;

namespace SaintsHierarchy
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Method)]
    public class HierarchyLeftButtonAttribute: HierarchyButtonAttribute
    {
        public override bool IsLeft => true;

        public HierarchyLeftButtonAttribute(string label = null, string tooltip = null): base(label, tooltip){}
    }
}
