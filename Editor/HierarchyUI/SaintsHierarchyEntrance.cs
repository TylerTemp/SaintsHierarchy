using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaintsHierarchy.Editor.Core;
using SaintsHierarchy.Editor.Core.Draw;
using SaintsHierarchy.Editor.Core.Utils;
using SaintsHierarchy.Editor.HierarchyUI.Renderer;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public static class SaintsHierarchyEntrance
    {
        private const string ExtraAddedClass = "saints-hierarchy-extra";
        private static Texture2D _colorStripTex;
        private static readonly Dictionary<HierarchyViewItem, (StyleBackground image, StyleColor tint, StyleBackgroundSize size)>
            OriginalBackgrounds = new Dictionary<HierarchyViewItem, (StyleBackground, StyleColor, StyleBackgroundSize)>();


        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            HierarchyWindow.BindView -= OnBindView;
            HierarchyWindow.BindView += OnBindView;
            HierarchyWindow.UnbindView -= OnUnbindView;
            HierarchyWindow.UnbindView += OnUnbindView;
            EditorApplication.delayCall += AttachFavoritePanels;
            HierarchyWindow.BindViewItem -= OnBindViewItem;
            HierarchyWindow.BindViewItem += OnBindViewItem;
            HierarchyWindow.UnbindViewItem -= OnUnbindViewItem;
            HierarchyWindow.UnbindViewItem += OnUnbindViewItem;
            HierarchyEditorEvents.ReloadAllScenesRequested -= RefreshBackgrounds;
            HierarchyEditorEvents.ReloadAllScenesRequested += RefreshBackgrounds;
            HierarchyEditorEvents.InitializeRequested -= RefreshBackgrounds;
            HierarchyEditorEvents.InitializeRequested += RefreshBackgrounds;
        }

        private static void AttachFavoritePanels()
        {
            foreach (HierarchyWindow window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
            {
                OnBindView(window, window.View);
            }
        }

        private static void OnBindView(HierarchyWindow window, HierarchyView view)
        {
            // ReSharper disable once InvertIf
            if (!Util.GetUsingConfig().disableFavorites && window.rootVisualElement.Q<FavoritePanelElement>() == null)
            {
                VisualElement parent = window.View.parent;
                parent.Insert(parent.IndexOf(window.View), new FavoritePanelElement());
            }
        }

        private static void OnUnbindView(HierarchyWindow window, HierarchyView view)
        {
            window.rootVisualElement.Q<FavoritePanelElement>()?.RemoveFromHierarchy();
        }

        private static void OnBindViewItem(
            HierarchyWindow window,
            HierarchyView view,
            HierarchyViewItem item)
        {
            ClearExtraAdded(item);
            IConfig usingConfig = Util.GetUsingConfig();
            if (usingConfig.disabled)
            {
                return;
            }

            ProcessMainIcon(view, item, usingConfig);
            ProcessName(view, item);
            ProcessConfig(item);
        }

        private static void ProcessConfig(HierarchyViewItem item)
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

        private static void RefreshBackgrounds()
        {
            foreach (HierarchyWindow window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
            {
                foreach (HierarchyViewItem item in window.rootVisualElement.Query<HierarchyViewItem>().ToList())
                {
                    ProcessConfig(item);
                }
            }
        }

        private static readonly string[] DefaultIcons =
        {
            "d_Transform Icon",
            "d_cs Script Icon",
        };

        private static void ProcessMainIcon(
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
                rightBottomOverlay.AddToClassList(ExtraAddedClass);
                item.OverlayIcon.Add(rightBottomOverlay);
            }
        }

        private static void ProcessName(HierarchyView view, HierarchyViewItem item)
        {
            if (item.Handler is not HierarchyGameObjectHandler)
            {
                return;
            }

            GameObject go = EditorUtility.EntityIdToObject(view.Source.GetEntityIdFromNode(item.Node)) as GameObject;
            if (go == null)
            {
                return;
            }

            Dictionary<bool, (string name, VisualElement container)> groups = new Dictionary<bool, (string name, VisualElement container)>();
            foreach (Component component in go.GetComponents<Component>())
            {
                foreach (RenderTargetInfo cached in Util.GetRenderTargetInfos(component))
                {
                    // Metadata is cached by type, but tag providers must use this row's component.
                    RenderTargetInfo info = new RenderTargetInfo(component, cached.Attribute,
                        cached.MemberType, cached.MemberInfo, cached.SortOrder);
                    HierarchyArea area;
                    if (info.MemberInfo is MethodInfo method &&
                        (typeof(VisualElement).IsAssignableFrom(method.ReturnType) || method.ReturnType == typeof(HierarchyArea)))
                    {
                        area = CreateCustomArea(component, method);
                    }
                    else if (info.Attribute is HierarchyButtonAttribute || info.Attribute is HierarchyLabelAttribute)
                    {
                        area = new HierarchyArea(info.Attribute.IsLeft, CreateNameElement(component, info));
                    }
                    else
                    {
                        // IMGUI-only custom drawers cannot run in the new hierarchy.
                        continue;
                    }

                    AddNameDecoration(area, info.Attribute.GroupBy,
                        item.LeftCustomContainer, item.RightCustomContainer, groups);
                }
            }
        }

        private static HierarchyArea CreateCustomArea(Component target, MethodInfo method)
        {
            try
            {
                return method.Invoke(target, GetDefaultArguments(method)) switch
                {
                    HierarchyArea area => area,
                    VisualElement element => new HierarchyArea(element),
                    _ => default,
                };
            }
            catch (Exception e)
            {
                Debug.LogException(e.InnerException ?? e);
                return default;
            }
        }

        private static void AddNameDecoration(HierarchyArea area, string groupBy,
            VisualElement left, VisualElement right,
            Dictionary<bool, (string name, VisualElement container)> groups)
        {
            if (area.Element == null)
            {
                return;
            }

            VisualElement parent = area.IsLeft ? left : right;
            if (string.IsNullOrEmpty(groupBy))
            {
                groups.Remove(area.IsLeft);
            }
            else
            {
                // Consecutive members of a group share vertical space on their chosen side.
                if (!groups.TryGetValue(area.IsLeft, out (string name, VisualElement container) group) || group.name != groupBy)
                {
                    VisualElement container = new VisualElement
                    {
                        pickingMode = PickingMode.Ignore,
                        style = { flexDirection = FlexDirection.Column, flexShrink = 0 },
                    };
                    container.AddToClassList(ExtraAddedClass);
                    parent.Add(container);
                    groups[area.IsLeft] = group = (groupBy, container);
                }
                parent = group.container;
            }

            area.Element.AddToClassList(ExtraAddedClass);
            parent.Add(area.Element);
        }

        private static VisualElement CreateNameElement(Component target, RenderTargetInfo info) =>
            info.Attribute is HierarchyButtonAttribute
                ? new ButtonRenderer(target, info)
                : new LabelRenderer(target, info);

        private static object[] GetDefaultArguments(MethodInfo method) =>
            method.GetParameters().Select(parameter => parameter.DefaultValue).ToArray();

        private static void OnUnbindViewItem(
            HierarchyWindow window,
            HierarchyView view,
            HierarchyViewItem item)
        {
            ClearExtraAdded(item);
        }

        private static void ClearExtraAdded(HierarchyViewItem item)
        {
            item.UnregisterCallback<PointerDownEvent>(OnConfigPointerDown, TrickleDown.TrickleDown);
            RestoreBackground(item);
            foreach (VisualElement extra in item.Query<VisualElement>(className: ExtraAddedClass).ToList())
            {
                extra.RemoveFromHierarchy();
            }
        }
    }
}
