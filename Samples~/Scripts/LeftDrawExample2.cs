using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SaintsHierarchy.Samples.Scripts
{
    public class LeftDrawExample2 : MonoBehaviour, IHierarchyLeftDraw
    {
        public string c;
#if UNITY_EDITOR
        public HierarchyUsed HierarchyLeftDraw(HierarchyArea hierarchyArea)
        {
            GUIContent content = new GUIContent(c);
            float width = new GUIStyle("button").CalcSize(content).x;
            Rect useRect = hierarchyArea.MakeXWidthRect(hierarchyArea.SpaceStartX, width);

            if (GUI.Button(useRect, content))
            {
                Debug.Log($"click {c}");
            }

            return new HierarchyUsed(useRect);
        }
#endif
    }
}
