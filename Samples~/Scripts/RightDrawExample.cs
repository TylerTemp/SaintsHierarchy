using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SaintsHierarchy.Samples.Scripts
{
    public class RightDrawExample : MonoBehaviour, IHierarchyDraw
    {
        public HierarchyUsed HierarchyDraw(HierarchyArea hierarchyArea)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                richText = true,
            };

            GUIContent content = new GUIContent("<color=red>RIGHT</color>");
            float width = style.CalcSize(content).x;

            Rect useRect = hierarchyArea.MakeXWidthRect(hierarchyArea.SpaceEndX, -width);

            // EditorGUI.DrawRect(useRect, Color.blue);
            EditorGUI.LabelField(useRect, content, style);

            return new HierarchyUsed(useRect);
        }
    }
}
