using System;
using System.Diagnostics;
using SaintsHierarchy.Utils;

namespace SaintsHierarchy
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
    public class HierarchyLabelAttribute: Attribute, IHierarchyAttribute
    {
        public readonly bool IsCallback;
        public readonly string Label;
        public readonly string Tooltip;
        public string GroupBy => "";
        public virtual bool IsLeft => false;

        public HierarchyLabelAttribute(string label = null, string tooltip = null)
        {
            (Label, IsCallback) = RuntimeUtil.ParseCallback(label);
            Tooltip = tooltip;
        }
    }
}
