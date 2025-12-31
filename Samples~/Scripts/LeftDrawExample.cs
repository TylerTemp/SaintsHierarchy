using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SaintsHierarchy.Samples.Scripts
{
    public class LeftDrawExample : MonoBehaviour, IHierarchyLeftDraw
    {
#if UNITY_EDITOR
        public HierarchyUsed HierarchyLeftDraw(HierarchyArea hierarchyArea)
        {
            GUIContent content = new GUIContent("LEFT");
            float width = EditorStyles.label.CalcSize(content).x;
            Rect useRect = hierarchyArea.MakeXWidthRect(hierarchyArea.SpaceStartX, width);

            // EditorGUI.DrawRect(useRect, Color.blue);
            EditorGUI.LabelField(useRect, content);

            return new HierarchyUsed(useRect);
        }
#endif
    }
}
