using System;
using System.Diagnostics;
using SaintsHierarchy.Utils;

namespace SaintsHierarchy
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Method)]
    public class HierarchyButtonAttribute: Attribute, IHierarchyAttribute
    {
        public readonly string Label;
        public readonly bool IsCallback;
        public readonly string Tooltip;

        public virtual bool IsGhost => false;

        public string GroupBy => "";
        public virtual bool IsLeft => false;

        public HierarchyButtonAttribute(string label = null, string tooltip = null)
        {
            (Label, IsCallback) = RuntimeUtil.ParseCallback(label);
            Tooltip = tooltip;
        }
    }
}
