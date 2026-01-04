using System;
using System.Diagnostics;

namespace SaintsHierarchy
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Method)]
    public class HierarchyLeftDrawAttribute: HierarchyDrawAttribute
    {
        public override bool IsLeft => false;

        public HierarchyLeftDrawAttribute(string groupBy = null): base(groupBy){}
    }
}
