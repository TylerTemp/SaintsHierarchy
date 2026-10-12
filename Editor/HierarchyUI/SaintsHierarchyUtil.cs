using SaintsHierarchy.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public static class SaintsHierarchyUtil
    {
        public static Button CreateComponentIconButton(Component component, Texture2D icon)
        {
            const int iconSize = 16;
            Button button = new Button
            {
                tooltip = ObjectNames.NicifyVariableName(component.GetType().Name),
                style =
                {
                    width = iconSize,
                    height = iconSize,
                    minWidth = iconSize,
                    flexShrink = 0,
                    marginLeft = 0,
                    marginRight = 0,
                    marginTop = 0,
                    marginBottom = 0,
                    paddingLeft = 0,
                    paddingRight = 0,
                    paddingTop = 0,
                    paddingBottom = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    borderTopWidth = 0,
                    borderBottomWidth = 0,
                    backgroundColor = Color.clear,
                    opacity = EditorUtility.GetObjectEnabled(component) == 0 ? 0.7f : 1f,
                },
            };
            button.Add(new Image
            {
                image = icon,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
                style =
                {
                    width = iconSize,
                    height = iconSize,
                },
            });
            button.clicked += () => ComponentPropertiesPopup.Show(component, button);
            return button;
        }
    }
}
