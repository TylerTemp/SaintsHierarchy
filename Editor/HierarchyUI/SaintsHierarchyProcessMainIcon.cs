using System;
using SaintsHierarchy.Editor.Core;
using SaintsHierarchy.Editor.Core.Utils;
using Unity.Hierarchy;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public static class SaintsHierarchyProcessMainIcon
    {
        private static readonly string[] DefaultIcons =
        {
            "d_Transform Icon",
            "d_cs Script Icon",
        };

        public static void ProcessMainIcon(
            HierarchyView view,
            HierarchyViewItem item,
            IConfig usingConfig)
        {
            // Debug.Log(item.Icon.style.backgroundImage.value.texture);
            bool isDefaultScriptIcon = Array.IndexOf(DefaultIcons, item.Icon.style.backgroundImage.value.texture?.name) >= 0;

            EntityId entityId = view.Source.GetEntityIdFromNode(item.Node);
            GameObject go = EditorUtility.EntityIdToObject(entityId) as GameObject;
            Texture2D customIcon = go != null
                ? Util.GetIconByComponent(go.GetComponents<Component>())
                : null;

            if (customIcon is not null)
            {
                isDefaultScriptIcon = false;
                item.Icon.style.backgroundImage = customIcon;
            }

            if (usingConfig.noDefaultIcon && isDefaultScriptIcon)
            {
                item.Icon.style.display = DisplayStyle.None;
            }
            else if (usingConfig.transparentDefaultIcon && isDefaultScriptIcon)
            {
                item.Icon.style.backgroundImage = StyleKeyword.None;
            }

            // Preserve prefab identity when a component supplies the main icon.
            // Missing prefab assets always receive a warning, as in the legacy renderer.
            Util.PrefabIconInfo prefabInfo = Util.GetPrefabIconInfo(go);
            Texture2D prefabOverlay = prefabInfo.IsMissingPrefab || customIcon != null
                ? prefabInfo.Icon
                : null;

            // ReSharper disable once InvertIf
            if (prefabOverlay != null)
            {
                VisualElement rightBottomOverlay = new VisualElement
                {
                    pickingMode = PickingMode.Ignore,
                    userData = item.OverlayIcon.style.display,
                    style =
                    {
                        width = Length.Percent(50),
                        height = Length.Percent(50),
                        position = Position.Absolute,
                        right = 0,
                        bottom = 0,
                        backgroundImage = prefabOverlay,
                        backgroundSize = new BackgroundSize(BackgroundSizeType.Contain),
                    },
                };
                rightBottomOverlay.AddToClassList(SaintsHierarchyEntrance.ExtraAddedClass);
                item.OverlayIcon.Add(rightBottomOverlay);
            }
        }
    }
}
