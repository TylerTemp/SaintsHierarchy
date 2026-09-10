#if (!UNITY_6000_6_OR_NEWER || SAINTSHIERARCHY_LEGACY) && !SAINTSHIERARCHY_NEW
#define SAINTSHIERARCHY_USE_LEGACY
#endif

using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#if !SAINTSHIERARCHY_USE_LEGACY
using UnityEngine.UIElements;
#endif
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
#if SAINTSHIERARCHY_USE_LEGACY
                    EditorApplication.RepaintHierarchyWindow();
#endif
                    yield return null;
                }
            }
        }

#if SAINTSHIERARCHY_USE_LEGACY
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
#else
        [HierarchyDraw("my progress bar")]
        private VisualElement DrawRight1G1() => CreateProgressBar(() => range1, Color.red);

        [HierarchyDraw("my progress bar")]
        private HierarchyArea DrawRight1G2() => new HierarchyArea(false, CreateProgressBar(() => range2, Color.yellow));

        private VisualElement CreateProgressBar(System.Func<float> getValue, Color color)
        {
            VisualElement background = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    width = 40,
                    height = EditorGUIUtility.singleLineHeight / 2,
                    flexShrink = 0,
                    backgroundColor = Color.gray,
                },
            };
            VisualElement progress = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style = { height = Length.Percent(100), backgroundColor = color },
            };
            background.Add(progress);

            void Refresh() => progress.style.width = Length.Percent(Mathf.Clamp01(getValue()) * 100);
            Refresh();
            // The schedule pauses automatically when the hierarchy row is detached.
            background.schedule.Execute(Refresh).Every(100);
            return background;
        }
#endif
#endif
    }
}
