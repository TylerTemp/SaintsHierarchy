using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SaintsHierarchy.Samples.Scripts
{
    public class DrawManually : MonoBehaviour
    {
        public bool play;
        [Range(0f, 1f)] public float range1;
        [Range(0f, 1f)] public float range2;

        private string ButtonLabel => play ? "Pause" : "Play";

#if UNITY_EDITOR
        [HierarchyLeftButton("$" + nameof(ButtonLabel))]
        private IEnumerator LeftBtn()
        {
            play = !play;
            // ReSharper disable once InvertIf
            if (play)
            {
                while (play)
                {
                    range1 = (range1 + 0.0005f) % 1;
                    range2 = (range2 + 0.0009f) % 1;
                    EditorApplication.RepaintHierarchyWindow();
                    yield return null;
                }
            }
        }



        [HierarchyDraw("my progress bar")]
        private HierarchyUsed DrawRight1G1(HierarchyArea headerArea)
        {
            Rect useRect = new Rect(headerArea.MakeXWidthRect(headerArea.GroupStartX - 40, 40))
            {
                height = headerArea.Height / 2,
            };
            Rect progressRect = new Rect(useRect)
            {
                width = range1 * useRect.width,
            };

            EditorGUI.DrawRect(useRect, Color.gray);
            EditorGUI.DrawRect(progressRect, Color.red);

            return new HierarchyUsed(useRect);
        }
        [HierarchyDraw("my progress bar")]
        private HierarchyUsed DrawRight1G2(HierarchyArea headerArea)
        {
            Rect useRect = new Rect(headerArea.MakeXWidthRect(headerArea.GroupStartX - 40, 40))
            {
                y = headerArea.Y + headerArea.Height / 2,
                height = headerArea.Height / 2,
            };
            Rect progressRect = new Rect(useRect)
            {
                width = range2 * useRect.width,
            };

            EditorGUI.DrawRect(useRect, Color.gray);
            EditorGUI.DrawRect(progressRect, Color.yellow);

            return new HierarchyUsed(useRect);
        }
#endif
    }
}
