#if SAINTSHIERARCHY_SAINTSFIELD
using SaintsField.Editor;
using UnityEditor;

namespace SaintsHierarchy.Editor
{
    [CustomEditor(typeof(SaintsHierarchyConfig), true)]
    public class HierarchyConfigEditor: SaintsEditor
    {

    }

    [CustomEditor(typeof(PersonalHierarchyConfig), true)]
    public class PersonalConfigEditor: SaintsEditor
    {

    }
}
#endif
