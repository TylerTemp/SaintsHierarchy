using System;
using System.Diagnostics;

namespace SaintsHierarchy
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Method)]
    public class HierarchyDrawAttribute: Attribute, IHierarchyAttribute
    {
        public string GroupBy { get; }
        public virtual bool IsLeft => false;

        public HierarchyDrawAttribute(string groupBy = null)
        {
            GroupBy = groupBy;
        }
    }
}
