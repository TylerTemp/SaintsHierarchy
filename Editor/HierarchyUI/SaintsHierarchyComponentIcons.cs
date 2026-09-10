using System.Collections.Generic;
using SaintsHierarchy.Editor.Core;
using SaintsHierarchy.Editor.Core.Utils;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public static class SaintsHierarchyComponentIcons
    {
        private const string ColumnId = "saints-hierarchy-component-icons";
        private const float IconSize = 16;

        [HierarchyViewColumnDescriptor(ColumnId)]
        private static void DescribeColumn(HierarchyViewColumnDescriptor descriptor)
        {
            descriptor.Title = "Components(SH)";

            descriptor.MakeHeader = () => new Label("Components");
            descriptor.BindHeader = (_, _) => { };
            descriptor.UnbindHeader = (_, _) => { };
            descriptor.DestroyHeader = (_, _) => { };

            descriptor.DefaultWidth = 96;
            descriptor.DefaultVisibility = false;
        }

        [HierarchyViewCellDescriptor(ColumnId, typeof(HierarchyGameObjectHandler))]
        private static void DescribeCell(HierarchyViewCellDescriptor descriptor)
        {
            descriptor.ClearCellContent = true;
            descriptor.BindCell = BindCell;
        }

        private static void BindCell(HierarchyViewCell cell)
        {
            // Cells are recycled: never retain buttons bound to the previous GameObject.
            cell.Clear();
            cell.IsDefaultValue = false;
            IConfig config = Util.GetUsingConfig();
            if (config.disabled || !config.componentIcons)
            {
                return;
            }

            GameObject go = EditorUtility.EntityIdToObject(
                cell.View.Source.GetEntityIdFromNode(cell.Node)) as GameObject;
            if (go == null)
            {
                return;
            }

            VisualElement row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    alignItems = Align.Center,
                    flexGrow = 1,
                    overflow = Overflow.Hidden,
                },
            };
            foreach ((Component component, Texture2D icon) in GetComponentIcons(
                         go.GetComponents<Component>(), config.componentIconsForGeneralScripts,
                         config.componentIconsForTransform))
            {
                Button button = new Button
                {
                    tooltip = ObjectNames.NicifyVariableName(component.GetType().Name),
                    style =
                    {
                        width = IconSize,
                        height = IconSize,
                        minWidth = IconSize,
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
                        width = IconSize,
                        height = IconSize,
                    },
                });
                button.clicked += () => ComponentPropertiesPopup.Show(component, button);
                row.Add(button);
            }
            cell.Add(row);
        }

        // Keep legacy filtering, but emit icons in GetComponents order.
        private static List<(Component component, Texture2D icon)> GetComponentIcons(
            Component[] allComponents, bool componentIconsForGeneralScripts, bool componentIconsForTransform)
        {
            List<(Component component, Texture2D icon)> componentAndIcon = new List<(Component, Texture2D)>(allComponents.Length);
            bool hasCanvas = false;
            // Canvas may follow RectTransform, so determine its filtering effect first.
            foreach (Component component in allComponents)
            {
                if (component is Canvas)
                {
                    hasCanvas = true;
                    break;
                }
            }
            foreach (Component component in allComponents)
            {
                switch (component)
                {

#if SAINTSHIERARCHY_UNITY_RENDER_PIPELINES_CORE
                    case UnityEngine.Rendering.Volume:
                    {
                        if (componentIconsForGeneralScripts)
                        {
                            componentAndIcon.Add((component, Util.GetCachedIcon("d_Volume Icon.asset")));
                        }
                    }
                        break;
#endif
#if SAINTSHIERARCHY_WWISE
                    case AkInitializer:
                    {
                        if (componentIconsForGeneralScripts)
                        {
                            componentAndIcon.Add((component, Util.GetCachedIcon("wwise-logo.png")));
                        }
                    }
                        break;
                    case AkAudioListener:
                    {
                        if (componentIconsForGeneralScripts)
                        {
                            componentAndIcon.Add((component, Util.GetCachedIcon("wwise-audio-listener.psd")));
                        }
                    }
                        break;
                    case AkGameObj:
                    {
                        if (componentIconsForGeneralScripts)
                        {
                            componentAndIcon.Add((component, Util.GetCachedIcon("wwise-game-object.psd")));
                        }
                    }
                        break;
#endif

                    case Camera:
                    case Light:
                    case EventSystem:
                    case CanvasRenderer:
                        if (componentIconsForGeneralScripts)
                        {
                            goto default;
                        }
                        break;
                    case RectTransform rt:
                        if (componentIconsForTransform && (!hasCanvas || componentIconsForGeneralScripts))
                        {
                            componentAndIcon.Add((rt,
                                EditorGUIUtility.IconContent("d_RectTransform Icon").image as Texture2D));
                        }
                        break;
                    case Transform trans:
                        if (componentIconsForTransform && componentIconsForGeneralScripts)
                        {
                            componentAndIcon.Add((trans,
                                EditorGUIUtility.IconContent("Transform Icon").image as Texture2D));
                        }
                        break;
                    case Canvas:
                        if (componentIconsForGeneralScripts)
                        {
                            goto default;
                        }
                        break;
                    case ParticleSystemRenderer:  // Ignored
                        break;
#if SAINTSHIERARCHY_SPINE_UNITY
                    case Spine.Unity.SkeletonAnimation:
                    {
                        if (componentIconsForGeneralScripts)
                        {
                            componentAndIcon.Add((component, Util.GetCachedIcon("spine-icon.png")));
                        }
                    }
                        break;
                    case Spine.Unity.SkeletonGraphic:
                    {
                        if (componentIconsForGeneralScripts)
                        {
                            componentAndIcon.Add((component, Util.GetCachedIcon("spine-skeleton-graphic.psd")));
                        }
                    }
                        break;
#endif
                    default:
                    {
                        if (component == null)
                        {
                            break;
                        }

                        Texture2D icon = EditorGUIUtility.GetIconForObject(component);
                        if (icon == null)
                        {
                            using(new DisableUnityLogScoop())
                            {
                                icon =
                                    EditorGUIUtility.IconContent($"d_{component.GetType().Name} Icon")
                                        ?.image as Texture2D;
                            }


                            if(icon == null)
                            {
                                using(new DisableUnityLogScoop())
                                {
                                    icon =
                                        EditorGUIUtility.IconContent($"{component.GetType().Name} Icon")
                                            ?.image as Texture2D;
                                }
                            }
                        }

                        MonoBehaviour monoBehaviour = component as MonoBehaviour;
                        bool isMonoBehaviour = monoBehaviour != null;
                        if (!componentIconsForGeneralScripts && isMonoBehaviour && IsGeneralScriptIcon(icon))
                        {
                            icon = null;
                        }

                        if (icon == null && isMonoBehaviour)
                        {
                            MonoScript script = MonoScript.FromMonoBehaviour(monoBehaviour);
                            if(script != null)
                            {
                                Texture2D scriptIcon = AssetPreview.GetMiniThumbnail(script);
                                if(scriptIcon != null)
                                {
                                    bool isGeneralScriptIcon = IsGeneralScriptIcon(scriptIcon);
                                    if (!isGeneralScriptIcon || componentIconsForGeneralScripts)
                                    {
                                        icon = scriptIcon;
                                    }
                                }
                            }
                        }

                        if (icon == null && componentIconsForGeneralScripts)
                        {
                            icon = Util.GetCachedIcon("question-mark-grey-20.png");
                        }
                        if (icon != null)
                        {
                            componentAndIcon.Add((component, icon));
                        }
                    }
                        break;
                }
            }

            return componentAndIcon;
        }

        private static bool IsGeneralScriptIcon(Texture2D icon)
        {
            return icon != null && (icon.name == "d_cs Script Icon" || icon.name == "cs Script Icon");
        }
    }
}
