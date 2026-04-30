using System.Collections.Generic;

namespace SaintsHierarchy.Editor.UIElement.TreeDropdown
{
    // name, value, disabled, icon, separator
    public interface IAdvancedDropdownList: IReadOnlyList<IAdvancedDropdownList>
    {
        IReadOnlyList<string> absolutePathFragments { get; }

        string displayName { get; }
        object value { get; }
        IReadOnlyList<IAdvancedDropdownList> children { get; }
        bool disabled { get; }
        string icon { get; }
        bool isSeparator { get; }

        int ChildCount();
        int SepCount();

        void SelfCompact();

        ICollection<string> ExtraSearches { get; }
    }
}
