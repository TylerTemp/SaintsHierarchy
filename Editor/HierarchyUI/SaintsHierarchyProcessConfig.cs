using System.Collections.Generic;
using SaintsHierarchy.Editor.Core;
using SaintsHierarchy.Editor.Core.Utils;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public static class SaintsHierarchyProcessConfig
    {
        private static Texture2D _colorStripTex;
        private static readonly Dictionary<HierarchyViewItem, (StyleBackground image, StyleColor tint, StyleBackgroundSize size)>
            OriginalBackgrounds = new Dictionary<HierarchyViewItem, (StyleBackground, StyleColor, StyleBackgroundSize)>();


        public static void ProcessConfig(HierarchyViewItem item)
        {
            if (item.Handler is not HierarchyGameObjectHandler)
            {
                return;
            }

            item.RegisterCallback<PointerDownEvent>(OnConfigPointerDown, TrickleDown.TrickleDown);
            ProcessBackground(item);
        }

        private static void OnConfigPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || !evt.altKey || Util.GetUsingConfig().disabled ||
                evt.currentTarget is not HierarchyViewItem item)
            {
                return;
            }

            GameObject go = EditorUtility.EntityIdToObject(item.View.Source.GetEntityIdFromNode(item.Node)) as GameObject;
            if (go == null)
            {
                return;
            }

            foreach (HierarchyWindow window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
            {
                if (window.rootVisualElement.panel != item.panel)
                {
                    continue;
                }

                evt.StopImmediatePropagation();
                Vector2 screenPosition = window.position.position + (Vector2)evt.position;
                Util.PopupConfig(GUIUtility.ScreenToGUIRect(new Rect(screenPosition, Vector2.zero)),
                    go, Util.GetGameObjectConfig(go).config);
                return;
            }
        }

        private static void ProcessBackground(HierarchyViewItem item)
        {
            RestoreBackground(item);
            if (Util.GetUsingConfig().disabled || item.Handler is not HierarchyGameObjectHandler)
            {
                return;
            }

            GameObject go = EditorUtility.EntityIdToObject(item.View.Source.GetEntityIdFromNode(item.Node)) as GameObject;
            if (go == null)
            {
                return;
            }

            GameObjectConfig config = Util.GetGameObjectConfig(go).config;
            if (!config.hasColor)
            {
                return;
            }

            OriginalBackgrounds[item] = (item.style.backgroundImage, item.style.unityBackgroundImageTintColor,
                item.style.backgroundSize);
            _colorStripTex ??= Util.LoadResource<Texture2D>("color-strip.psd");
            item.style.backgroundImage = _colorStripTex;
            item.style.unityBackgroundImageTintColor = config.color;
            item.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
        }

        private static void RestoreBackground(HierarchyViewItem item)
        {
            if (OriginalBackgrounds.TryGetValue(item, out var original))
            {
                item.style.backgroundImage = original.image;
                item.style.unityBackgroundImageTintColor = original.tint;
                item.style.backgroundSize = original.size;
                OriginalBackgrounds.Remove(item);
            }
        }

        public static void Clear(HierarchyViewItem item)
        {
            item.UnregisterCallback<PointerDownEvent>(OnConfigPointerDown, TrickleDown.TrickleDown);
            RestoreBackground(item);
        }
    }
}
