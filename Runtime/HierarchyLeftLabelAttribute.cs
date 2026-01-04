using System;
using System.Diagnostics;

namespace SaintsHierarchy
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
    public class HierarchyLeftLabelAttribute: HierarchyLabelAttribute
    {
        public override bool IsLeft => true;

        public HierarchyLeftLabelAttribute(string label = null, string tooltip = null): base(label, tooltip){}
    }
}
