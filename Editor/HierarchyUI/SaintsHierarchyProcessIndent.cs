using System;
using SaintsHierarchy.Editor.Core;
using SaintsHierarchy.Editor.Core.Utils;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public static class SaintsHierarchyProcessIndent
    {
        public static void ProcessIndent(HierarchyViewItem item)
        {
            IndentGuideElement previous = item.Q<IndentGuideElement>();
            previous?.Dispose();
            previous?.RemoveFromHierarchy();
            IConfig config = Util.GetUsingConfig();
            if (config.disabled || !config.indentGuides || item.View == null || item.View.Filtering ||
                item.Node == HierarchyNode.Null || item.Handler is not HierarchyGameObjectHandler)
            {
                return;
            }

            IndentGuideElement guide = new IndentGuideElement(item);
            guide.AddToClassList(SaintsHierarchyEntrance.ExtraAddedClass);
            item.hierarchy.Add(guide);
        }

        private class IndentGuideElement : VisualElement, IDisposable
        {
            private readonly HierarchyViewItem _item;
            private readonly IVisualElementScheduledItem _initialRefresh;

            public IndentGuideElement(HierarchyViewItem item)
            {
                _item = item;
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = style.right = style.top = style.bottom = 0;
                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                _initialRefresh = schedule.Execute(Rebuild);
                item.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                item.Toggle.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            private void OnGeometryChanged(GeometryChangedEvent evt) => Rebuild();

            public void Dispose()
            {
                _item.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                _item.Toggle.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                _initialRefresh.Pause();
            }

            private void Rebuild()
            {
                Clear();
                HierarchyView view = _item.View;
                IConfig config = Util.GetUsingConfig();
                if (view == null || view.Filtering || config.disabled || !config.indentGuides ||
                    _item.Node == HierarchyNode.Null || !view.Source.Exists(_item.Node))
                {
                    return;
                }

                HierarchyViewModel model = view.ViewModel;
                HierarchyNode node = _item.Node;
                HierarchyNode root = model.GetRoot();
                int depth = model.GetDepth(node);
                if (root != view.Source.Root)
                {
                    depth -= model.GetDepth(root) + 1;
                }
                if (depth <= 0)
                {
                    return;
                }

                // Unity translates the foldout's container by the displayed indentation.
                // Derive the step from that translation, including a custom view root.
                float step = _item.Toggle.parent.resolvedStyle.translate.x / depth;
                if (step is <= 0 or float.NaN)
                {
                    return;
                }
                Rect toggle = _item.Toggle.ChangeCoordinatesTo(this, new Rect(Vector2.zero, _item.Toggle.layout.size));
                float x = toggle.center.x - step;

                for (int level = 0; level < depth; level++, x -= step)
                {
                    HierarchyNode modelParent = model.GetParent(node);
                    if (modelParent == HierarchyNode.Null || modelParent == root)
                    {
                        break;
                    }
                    GameObject parentGo = EditorUtility.EntityIdToObject(view.Source.GetEntityIdFromNode(modelParent)) as GameObject;
                    if (parentGo == null)
                    {
                        break;
                    }
                    GameObjectConfig parentConfig = Util.GetGameObjectConfig(parentGo).config;
                    Color color = parentConfig.hasColor
                        ? parentConfig.color
                        : new Color(0.4f, 0.4f, 0.4f);

                    bool hasNext = model.GetNextSibling(node) != HierarchyNode.Null;
                    if (level == 0 || hasNext)
                    {
                        string texture;
                        if (level == 0)
                        {
                            texture = hasNext ? "tee" : "elbow";
                        }
                        else
                        {
                            texture = "vertical";
                        }
                        // Keep the branch tile at its native width. Extend leaf connectors
                        // through the empty foldout slot with a horizontal texture.
                        float end;
                        if (level == 0)
                        {
                            end = model.HasVisibleChildren(_item.Node)
                                ? toggle.xMin
                                : toggle.xMax;
                        }
                        else
                        {
                            end = x + 8;
                        }
                        float width = Mathf.Min(8, end - x);
                        AddLine(texture, new Rect(x, contentRect.yMin, width, contentRect.height), color);
                        if (level == 0 && end > x + width)
                        {
                            AddLine("horizontal", new Rect(x + width, contentRect.yMin,
                                end - x - width, contentRect.height), color);
                        }
                    }
                    node = modelParent;
                }
            }

            private void AddLine(string texture, Rect rect, Color color)
            {
                VisualElement line = new VisualElement
                {
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        position = Position.Absolute,
                        left = rect.x,
                        top = rect.y,
                        width = rect.width,
                        height = rect.height,
                        backgroundImage = Util.LoadAndCache($"Indent/{texture}.png"),
                        backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100)),
                        unityBackgroundImageTintColor = color,
                        unitySliceLeft = texture == "horizontal" ? 0 : 1,
                    },
                };
                Add(line);
            }
        }

        public static void Clear(HierarchyViewItem item)
        {
            item.Q<IndentGuideElement>()?.Dispose();
        }
    }
}
