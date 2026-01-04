using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor.Utils
{
    public static class SaintsMenu
    {
        private const string MenuRoot =
#if SAINTSHIERARCHY_DEBUG
                "Saints/"
#else
                "Window/Saints/"
#endif
            ;

        [MenuItem(MenuRoot + "Disable Saints Hierarchy")]
        public static void DisableSaintsHierarchy()
        {
            SaintsHierarchyConfig config = Util.EnsureConfig();
            if (config != null)
            {
                EditorUtility.SetDirty(config);
                config.disabled = !config.disabled;
            }
            else
            {
                Debug.LogWarning("SaintsHierarchy config not found");
            }

            Checkmark();
        }

        [InitializeOnLoadMethod]
        private static void Checkmark()
        {
            SaintsHierarchyConfig config = Util.EnsureConfig();
            bool disabled = config == null || config.disabled;
            Menu.SetChecked(MenuRoot + "Disable Saints Hierarchy", disabled);
        }
    }
}
