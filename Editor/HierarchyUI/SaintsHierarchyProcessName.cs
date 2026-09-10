using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaintsHierarchy.Editor.Core.Draw;
using SaintsHierarchy.Editor.HierarchyUI.Renderer;
using UnityEngine.SceneManagement;
using SaintsHierarchy.Editor.Core;
using SaintsHierarchy.Editor.Core.Utils;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public static class SaintsHierarchyProcessName
    {
        public static void ProcessName(HierarchyView view, HierarchyViewItem item, IConfig usingConfig)
        {
            if (item.Handler is HierarchySceneHandler)
            {
                if (!usingConfig.disableSceneSelector)
                {
                    ProcessSceneSelector(item);
                }
                return;
            }
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

        private static void ProcessSceneSelector(HierarchyViewItem item)
        {
            Scene scene = ((HierarchySceneHandler)item.Handler).GetScene(item.Node);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            Button arrow = new Button(() => ShowSceneSelector(item))
            {
                tooltip = "Switch scene",
                style =
                {
                    width = 16,
                    height = 16,
                    flexShrink = 0,
                    marginLeft = 0,
                    marginRight = 0,
                    marginTop = 0,
                    marginBottom = 0,
                    paddingLeft = 0,
                    paddingRight = 0,
                    paddingTop = 0,
                    paddingBottom = 0,
                    borderLeftWidth = 1,
                    borderLeftColor = Color.gray,
                    borderRightWidth = 0,
                    borderTopWidth = 0,
                    borderBottomWidth = 0,
                    backgroundImage = EditorGUIUtility.IconContent("d_icon dropdown").image as Texture2D,
                    backgroundSize = new BackgroundSize(BackgroundSizeType.Contain),
                    backgroundColor = Color.clear,
                },
            };
            arrow.AddToClassList(SaintsHierarchyEntrance.ExtraAddedClass);
            item.LeftCustomContainer.Add(arrow);
        }

        private static void ShowSceneSelector(HierarchyViewItem item)
        {
            IConfig config = Util.GetUsingConfig();
            if (config.disabled || config.disableSceneSelector || item.panel == null ||
                item.Handler is not HierarchySceneHandler handler)
            {
                return;
            }

            Scene scene = handler.GetScene(item.Node);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            foreach (HierarchyWindow window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
            {
                if (window.rootVisualElement.panel != item.panel)
                {
                    continue;
                }

                Rect anchor = item.Name.worldBound;
                anchor.xMax = item.worldBound.xMax;
                anchor.position += window.position.position;
                SceneSelector.Show(scene, GUIUtility.ScreenToGUIRect(anchor));
                return;
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
                    container.AddToClassList(SaintsHierarchyEntrance.ExtraAddedClass);
                    parent.Add(container);
                    groups[area.IsLeft] = group = (groupBy, container);
                }
                parent = group.container;
            }

            area.Element.AddToClassList(SaintsHierarchyEntrance.ExtraAddedClass);
            parent.Add(area.Element);
        }

        private static VisualElement CreateNameElement(Component target, RenderTargetInfo info) =>
            info.Attribute is HierarchyButtonAttribute
                ? new ButtonRenderer(target, info)
                : new LabelRenderer(target, info);

        private static object[] GetDefaultArguments(MethodInfo method) =>
            method.GetParameters().Select(parameter => parameter.DefaultValue).ToArray();
    }
}
