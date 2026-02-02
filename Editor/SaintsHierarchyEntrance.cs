#if (WWISE_2024_OR_LATER || WWISE_2023_OR_LATER || WWISE_2022_OR_LATER || WWISE_2021_OR_LATER || WWISE_2020_OR_LATER || WWISE_2019_OR_LATER || WWISE_2018_OR_LATER || WWISE_2017_OR_LATER || WWISE_2016_OR_LATER || SAINTSFIELD_WWISE) && !SAINTSFIELD_WWISE_DISABLE
#define SAINTSHIERARCHY_WWISE
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaintsHierarchy.Editor.Draw;
using SaintsHierarchy.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace SaintsHierarchy.Editor
{
    public static class SaintsHierarchyEntrance
    {
        [InitializeOnLoadMethod]
        private static void Entrance()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private const int StartOffset = 60;
        private const int IndentOffset = 14;
        private const int LeftStartX = 32;
        private const int PrefabExpandWidth = 16;
        private const int RowHeight = 16;

//         private static int? _selectedInstance;
//
//         private static bool IsSelected(int instanceID)
//         {
//             if (_selectedInstance != null)
//             {
//                 return instanceID == _selectedInstance;
//             }
//
//             return Selection.
// #if UNITY_6000_3_OR_NEWER
//                 entityIds
// #else
//                 instanceIDs
// #endif
//                 .Contains(instanceID);
//         }

        private static readonly Color TreeColor = new Color(0.4f, 0.4f, 0.4f);
        private static Texture2D _colorStripTex;

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            // Debug.Log(selectionRect.y);
            bool personalDisabled = !PersonalHierarchyConfig.instance.personalEnabled;
            if (personalDisabled
                    ? SaintsHierarchyConfig.instance.disabled
                    : PersonalHierarchyConfig.instance.disabled)
            {
                return;
            }


            // Get the object corresponding to the ID
            Object obj = EditorUtility.
#if UNITY_6000_3_OR_NEWER
                EntityIdToObject
#else
                InstanceIDToObject
#endif
                (instanceID);

            if (obj == null)
            {
                return;
            }

            if (obj is not GameObject originGo)
            {
                return;
            }

            // Debug.Log($"insId:  {instanceID} obj: {go.name}");

            int rowIndex = Mathf.RoundToInt(selectionRect.y) / RowHeight;
            Rect fullRect = new Rect(selectionRect)
            {
                x = LeftStartX,
                xMax = selectionRect.xMax + 16,
            };

            // EditorGUI.DrawRect(fullRect, Color.yellow);

            Vector2 mousePosition = Event.current.mousePosition;
            bool isHover = fullRect.Contains(mousePosition);
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            // if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && isHover)
            // {
            //     _selectedInstance = instanceID;
            // }
            // else if (Event.current.type == EventType.MouseUp)
            // {
            //     _selectedInstance = null;
            // }

            string curScenePath = originGo.scene.path;
            // Debug.Log($"popup parent: {string.Join(",", parentRoots)}");
            // Debug.Log($"popup scene: {curScenePath}");
            // GameObject targetGo = go;
            GameObject go = originGo;
            if (curScenePath.EndsWith(".prefab"))
            {
                string absPath = string.Join("/", GetAbsPath(go.transform).Skip(1));
                // Debug.Log($"popup absPath: {absPath} for {go.name}");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(curScenePath);
                GameObject newGo;
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (absPath == "")
                {
                    newGo = prefab;
                }
                else
                {
                    // Debug.Log(prefab.name);
                    // Debug.Log(prefab.transform);
                    Transform reTarget = prefab.transform.Find(absPath);
                    if (reTarget == null)
                    {
                        return;
                    }
                    newGo = reTarget.gameObject;
                }
                Debug.Assert(newGo != null, absPath);
                go = newGo;
                // Debug.Log($"popup re-id: {GlobalObjectId.GetGlobalObjectIdSlow(targetGo)}");
            }

            GameObjectConfig goConfig;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                (bool runtimeFound, GameObjectConfig runtimeConfig) = RuntimeCacheConfig.instance.Search(instanceID);
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (runtimeFound)
                {
                    goConfig = runtimeConfig;
                }
                else
                {
                    goConfig = GetGameObjectConfig(go).config;
                }
            }
            else
            {
                bool found;
                (found, goConfig) = GetGameObjectConfig(go);
                if(found)
                {
                    RuntimeCacheConfig.instance.Upsert(instanceID, goConfig);
                }
            }

            Transform trans = go.transform;

            (string sceneHierarchyError, EditorWindow sceneHierarchyWindow, object sceneHierarchy) = GetSceneHierarchyWindow(selectionRect);
            if (sceneHierarchyError != "")
            {
#if SAINTSHIERARCHY_DEBUG
                Debug.LogWarning(sceneHierarchyError);
#endif
                return;
            }

            (string bgError, SelectStatus bgStatus) = GetBgColor(sceneHierarchyWindow, sceneHierarchy, instanceID, isHover);
            if (bgError != "")
            {
#if SAINTSHIERARCHY_DEBUG
                Debug.LogWarning(bgError);
#endif
                return;
            }

            Color bgDefaultColor;
            switch (bgStatus)
            {
                case SelectStatus.Normal:
                    bgDefaultColor = ColorNormal;
                    break;
                case SelectStatus.NormalHover:
                    bgDefaultColor = ColorHover;
                    break;
                case SelectStatus.SelectFocus:
                    bgDefaultColor = ColorSelectFocus;
                    break;
                case SelectStatus.SelectUnfocus:
                    bgDefaultColor = ColorSelectUnfocus;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bgStatus), bgStatus, null);
            }
            EditorGUI.DrawRect(fullRect, bgDefaultColor);
            if(personalDisabled
                   ? SaintsHierarchyConfig.instance.backgroundStrip
                   : PersonalHierarchyConfig.instance.backgroundStrip)
            {
                bool needLight = (rowIndex + 1) % 2 == 0;
                if (needLight)
                {
                    EditorGUI.DrawRect(fullRect, ColorStripedLight);
                }
            }

            bool hasFoldout = trans.childCount >= 1;
            bool thisExpand = false;
            if (hasFoldout)
            {
                (string expandedError, int[] expandedIds) = GetExpandedIds(sceneHierarchy);
                if (expandedError != "")
                {
#if SAINTSHIERARCHY_DEBUG
                    Debug.LogWarning(expandedError);
#endif
                    return;
                }

                thisExpand = expandedIds.Contains(instanceID);
                // Debug.Log($"this expanded: {thisExpand}, {go.name}");
            }

            #region Tree

            int startX = Mathf.RoundToInt(selectionRect.x);
            int offset = startX - 60;
            // Debug.Log($"{go.name}: {offset}");
            int indentLevel = offset / IndentOffset;
            // Debug.Log($"{indentLevel}: {go.name}");
            // Debug.Log($"{trans.GetSiblingIndex()}: {go.name}");


            // int dontOverlapFoldoutIndent = hasFoldout ? indentLevel - 1 : indentLevel;

            for (int index = 0; index < indentLevel; index++)
            {
                int indentX = StartOffset + (index - 1) * IndentOffset;
                Rect drawIndent = new Rect(selectionRect)
                {
                    x = indentX,
                    width = IndentOffset,
                };

                DrawIndentDepth(drawIndent, indentLevel - index, trans);
            }


            if (!hasFoldout && indentLevel > 0)
            {
                Transform parentTrans = trans.parent;
                Color useColor = TreeColor;
                if (parentTrans != null)
                {
                    GameObjectConfig config = GetGameObjectConfig(parentTrans.gameObject).config;
                    if (config.hasColor)
                    {
                        useColor = config.color;
                    }
                }
                int indentXLast = StartOffset + (indentLevel - 1) * IndentOffset;
                Rect drawIndent = new Rect(selectionRect)
                {
                    x = indentXLast,
                    width = IndentOffset,
                };
                // GUI.Label(drawIndent, "─");
                DrawRightThrough(drawIndent, useColor);
            }
            #endregion

            #region Foldout

            if (hasFoldout)
            {
                Rect foldoutRect = new Rect(fullRect)
                {
                    x = selectionRect.x - IndentOffset,
                    width = IndentOffset,
                };
                // EditorGUI.DrawRect(foldoutRect, Color.red);
                EditorGUI.Foldout(foldoutRect, thisExpand, GUIContent.none);
            }

            #endregion

            GUIStyle textColorStyle = EditorStyles.label;

            Component[] allComponents = trans.GetComponents<Component>();
            Texture2D goIcon = EditorGUIUtility.GetIconForObject(originGo);
            if (goIcon == null)
            {
                goIcon = EditorGUIUtility.GetIconForObject(go);
            }

            #region Main Icon

            // string customIcon = null;
            Texture iconTexture;
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (!string.IsNullOrEmpty(goConfig.icon))
            {
                iconTexture = Util.LoadResource<Texture2D>(goConfig.icon);
                // customIcon = goConfig.icon;
            }
            else if (goIcon != null && !goIcon.name.StartsWith("sv_label_"))
            {
                iconTexture = goIcon;
            }
            else
            {
                iconTexture = GetIconByComponent(allComponents);
            }

            Texture prefabTexture = null;
            bool isAnyPrefabInstanceRoot = PrefabUtility.IsAnyPrefabInstanceRoot(go);
            bool isMissingPrefab = false;
            bool isModelPrefab = false;
            if(isAnyPrefabInstanceRoot)
            {
                textColorStyle = GetLabelStylePrefab();
                PrefabAssetType assetType =
                    PrefabUtility.GetPrefabAssetType(go);
                PrefabInstanceStatus instanceStatus =
                    PrefabUtility.GetPrefabInstanceStatus(go);

                // ReSharper disable once ConvertSwitchStatementToSwitchExpression
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (assetType)
                {
                    case PrefabAssetType.Regular:
                        prefabTexture = EditorGUIUtility.IconContent("d_Prefab Icon").image;
                        break;
                    case PrefabAssetType.Variant:
                        prefabTexture = EditorGUIUtility.IconContent("d_PrefabVariant Icon").image;
                        break;
                    case PrefabAssetType.Model:
                        isModelPrefab = true;
                        prefabTexture = EditorGUIUtility.IconContent("d_PrefabModel Icon").image;
                        break;
                }

                if (instanceStatus == PrefabInstanceStatus.MissingAsset)
                {
                    isMissingPrefab = true;
                    textColorStyle = GetLabelStyleMissingPrefab();
                    iconTexture = EditorGUIUtility.IconContent("d_Prefab Icon").image;
                    prefabTexture = EditorGUIUtility.IconContent("d_console.warnicon").image;
                    // prefabTexture = EditorGUIUtility.IconContent("d_PrefabVariant On Icon").image;
                }
            }
            else if(PrefabUtility.IsPartOfAnyPrefab(originGo))
            {
                textColorStyle = GetLabelStylePrefab();
            }

            Rect iconRect = new Rect(selectionRect)
            {
                width = IndentOffset,
            };

            #endregion

            #region Label

            const int labelXOffset = 3;
            Rect rawRightRect = new Rect(selectionRect)
            {
                x = iconRect.xMax + labelXOffset,
                width = selectionRect.xMax - iconRect.xMax - labelXOffset,
            };

            GUIContent content = new GUIContent(trans.name);
            float labelWidth = textColorStyle.CalcSize(content).x;

            if (goIcon != null && goIcon.name.StartsWith("sv_label_"))
            {
                Color labelColor = goIcon.name switch
                {
                    "sv_label_0" => new Color32(140, 140, 140, 255),
                    "sv_label_1" => new Color32(70, 119, 202, 255),
                    "sv_label_2" => new Color32(65, 184, 161, 255),
                    "sv_label_3" => new Color32(48, 188, 47, 255),
                    "sv_label_4" => new Color32(234, 206, 43, 255),
                    "sv_label_5" => new Color32(229, 135, 35, 255),
                    "sv_label_6" => new Color32(204, 39, 39, 255),
                    "sv_label_7" => new Color32(187, 72, 170, 255),
                    _ => Color.clear,
                };
                // Debug.Log(goIcon.name);
                EditorGUI.DrawRect(new Rect(rawRightRect)
                {
                    y = rawRightRect.yMax - 1,
                    height = 1,
                    width = labelWidth,
                }, labelColor);

                // GUI.DrawTexture(new Rect(labelRect)
                // {
                //     width = labelWidth,
                // },goIcon, ScaleMode.StretchToFill, true);

                // GUIStyle style = new GUIStyle();
                // style.normal.background = goIcon;
                // const int border = 6;
                // style.border = new RectOffset(border, border, border, border); // left, right, top, bottom
                //
                // GUI.Box(new Rect(labelRect)
                // {
                //     x = labelRect.x - 4,
                //     y = labelRect.y - 1,
                //     width = labelWidth + 8,
                //     height = labelRect.height + 2,
                // }, GUIContent.none, style);
            }

            #endregion

            #region disabled

            if (!ActiveInAnyHierarchy(go))
            {
                // if (goConfig.hasColor)
                // {
                //     // cover the alpha, to override Unity's default drawing
                //     // EditorGUI.DrawRect(fullRect, bgDefaultColor);
                //     // EditorGUI.DrawRect(fullRect, new Color(bgDefaultColor.r));
                // }
                EditorGUI.DrawRect(fullRect, new Color(bgDefaultColor.r, bgDefaultColor.g, bgDefaultColor.b, 0.5f));
            }

            #endregion

            #region Right Space

            Rect rightRect = new Rect(selectionRect)
            {
                x = rawRightRect.x + labelWidth,
                xMax = selectionRect.xMax,
            };
            // EditorGUI.DrawRect(rightRect, Color.blueViolet);
            bool gameObjectEnabledChecker = personalDisabled
                ? SaintsHierarchyConfig.instance.gameObjectEnabledChecker
                : PersonalHierarchyConfig.instance.gameObjectEnabledChecker;

            bool componentIcons = personalDisabled
                ? SaintsHierarchyConfig.instance.componentIcons
                : PersonalHierarchyConfig.instance.componentIcons;

            DrawRect(gameObjectEnabledChecker, componentIcons, originGo, allComponents, new Rect(rawRightRect)
            {
                width = labelWidth,
            }, rightRect);

            #endregion

            #region Prefab Expand

            if (isAnyPrefabInstanceRoot && !isMissingPrefab && !isModelPrefab)
            {
                Rect rightExpandRect = new Rect(selectionRect)
                {
                    x = selectionRect.xMax,
                    width = PrefabExpandWidth,
                };
                if (rightExpandRect.Contains(mousePosition))
                {
                    EditorGUI.DrawRect(rightExpandRect, new Color(1, 1, 1, 0.1f));
                }

                GUI.DrawTexture(rightExpandRect, EditorGUIUtility.IconContent("ArrowNavigationRight").image, ScaleMode.ScaleToFit, true);
            }

            #endregion

            // late draw
            // icon
            EditorGUI.DrawRect(iconRect, bgDefaultColor);
            if(personalDisabled
                   ? SaintsHierarchyConfig.instance.backgroundStrip
                   : PersonalHierarchyConfig.instance.backgroundStrip)
            {
                bool needLight = (rowIndex + 1) % 2 == 0;
                if (needLight)
                {
                    EditorGUI.DrawRect(iconRect, ColorStripedLight);
                }
            }
            if (iconTexture == null)
            {
                // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
                if (prefabTexture is null)
                {
                    GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("d_GameObject Icon").image, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    GUI.DrawTexture(iconRect, prefabTexture, ScaleMode.ScaleToFit, true);
                }
            }
            else
            {
                GUI.DrawTexture(iconRect, iconTexture, ScaleMode.ScaleToFit, true);

                if (prefabTexture is not null)
                {
                    const float scale = 0.7f;
                    Rect footerIconRect = new Rect(iconRect.x + iconRect.width * (1 - scale) + 5, iconRect.y +
                        iconRect.height *
                        (1 - scale),
                        iconRect.width * scale, iconRect.height * scale);
                    GUI.DrawTexture(footerIconRect, prefabTexture, ScaleMode.ScaleToFit, true);
                }
            }
            // label
            Rect labelRect = new Rect(rawRightRect)
            {
                width = labelWidth,
            };
            EditorGUI.DrawRect(labelRect, bgDefaultColor);
            if(personalDisabled
                   ? SaintsHierarchyConfig.instance.backgroundStrip
                   : PersonalHierarchyConfig.instance.backgroundStrip)
            {
                bool needLight = (rowIndex + 1) % 2 == 0;
                if (needLight)
                {
                    EditorGUI.DrawRect(labelRect, ColorStripedLight);
                }
            }
            if (goConfig.hasColor)
            {
                _colorStripTex ??= Util.LoadResource<Texture2D>("color-strip.psd");
                using(new GUIColorScoop(goConfig.color))
                {
                    GUI.DrawTexture(fullRect, _colorStripTex, ScaleMode.StretchToFill, true);
                }
            }
            EditorGUI.LabelField(rawRightRect, content, textColorStyle);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && isHover && (Event.current.modifiers & EventModifiers.Alt) != 0)
            {
                // Debug.Log($"popup id: {GlobalObjectId.GetGlobalObjectIdSlow(go)}");
                // var parentRoots = GetPrefabRootTopToBottom(go);

                Util.PopupConfig(new Rect(mousePosition.x, mousePosition.y, 0, 0), go, goConfig);
            }
        }

        private static bool ActiveInAnyHierarchy(GameObject go)
        {
            bool isInPrefabMode =
                PrefabStageUtility.GetCurrentPrefabStage() != null;
            if (!isInPrefabMode)
            {
                return go.activeInHierarchy;
            }

            Transform t = go.transform;
            // Transform prefabRoot = stage.prefabContentsRoot.transform;

            while (t != null)
            {
                if (!t.gameObject.activeSelf)
                    return false;

                // if (t == prefabRoot)
                //     break;

                t = t.parent;
            }

            return true;
        }

        private static Texture2D _closePng;

        private static bool AnyParentDisabled(GameObject go)
        {
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                {
                    return true;
                }
                parent = parent.parent;
            }

            return false;
        }

        private static void DrawRect(bool gameObjectEnabledChecker, bool componentIcons, GameObject originGo, Component[] allComponents, Rect labelRect, Rect rightRect)
        {
            HierarchyButtonDrawer.Update();

            float leftXLimit = rightRect.x;

            Rect useRect = new Rect(rightRect);
            if (gameObjectEnabledChecker && AnyParentDisabled(originGo))
            {
                useRect.xMax -= 18;
                Rect checkerRect = new Rect(useRect.xMax + 2,  useRect.y, 16, useRect.height);
                using EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope();
                bool goEnable = EditorGUI.Toggle(checkerRect, originGo.activeSelf);
                if (changeCheckScope.changed)
                {
                    originGo.SetActive(goEnable);
                }
            }

            if (componentIcons)
            {
                List<(Component component, bool canToggle, Texture2D icon)> componentAndIcon = new List<(Component, bool, Texture2D)>(allComponents.Length);
                bool hasCanvas = false;
                RectTransform rectTransform = null;
                foreach (Component component in allComponents)
                {
                    switch (component)
                    {
                        case Camera:
                        case Light:
                        case EventSystem:
#if SAINTSHIERARCHY_UNITY_RENDER_PIPELINES_CORE
                        case UnityEngine.Rendering.Volume:
#endif
#if SAINTSHIERARCHY_WWISE
                        case AkInitializer:
#endif
                        case CanvasRenderer:
                            break;
                        case RectTransform rt:
                            rectTransform = rt;
                            break;
                        case Transform:
                            break;
                        case Canvas:
                            hasCanvas = true;
                            break;
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

                            if (icon == null && component is MonoBehaviour mb)
                            {
                                MonoScript script = MonoScript.FromMonoBehaviour(mb);
                                if(script != null)
                                {
                                    Texture2D scriptIcon = AssetPreview.GetMiniThumbnail(script);
                                    if(scriptIcon != null && scriptIcon.name != "d_cs Script Icon")
                                    {
                                        icon = scriptIcon;
                                    }
                                }
                            }
                            if (icon != null)
                            {
                                componentAndIcon.Add((component, component is Behaviour, icon));
                            }
                        }
                            break;
                    }
                }

                // rectTransform always at end
                if (!hasCanvas && rectTransform is not null)
                {
                    componentAndIcon.Add((rectTransform, false,
                        EditorGUIUtility.IconContent("d_RectTransform Icon").image as Texture2D));
                }

                if (componentAndIcon.Count > 0)
                {
                    float startX = useRect.xMax -= RowHeight * componentAndIcon.Count;
                    for (int index = 0; index < componentAndIcon.Count; index++)
                    {
                        (Component component, bool canToggle, Texture2D icon) compInfo = componentAndIcon[index];
                        float x = startX + index * RowHeight;
                        if (x < leftXLimit)
                        {
                            continue;
                        }
                        Rect iconRect = new Rect(x, useRect.y, RowHeight, RowHeight);
                        GUI.DrawTexture(iconRect, compInfo.icon,
                            ScaleMode.ScaleToFit, true);
                        // if (compInfo.canToggle)
                        // {
                        //     Behaviour behavior = (Behaviour)compInfo.component;
                        //     if (GUI.Button(iconRect, new GUIContent(compInfo.icon), EditorStyles.iconButton))
                        //     {
                        //         behavior.enabled = !behavior.enabled;
                        //     }
                        //
                        //     if (!behavior.enabled)
                        //     {
                        //         const float rectScale = 0.7f;
                        //         Rect sideNote = new Rect(iconRect.x + iconRect.width * (1 - rectScale), iconRect.y + iconRect.height * (1 - rectScale),
                        //             iconRect.width * rectScale, iconRect.height * rectScale);
                        //         // Rect sideNote = iconRect;
                        //         using (new GUIColorScoop(Color.black))
                        //         {
                        //             GUI.DrawTexture(sideNote, _closePng ?? Util.LoadResource<Texture2D>("close.png"), ScaleMode.StretchToFill, true);
                        //         }
                        //     }
                        //
                        // }
                        // else
                        // {
                        //     GUI.DrawTexture(iconRect, compInfo.icon,
                        //         ScaleMode.ScaleToFit, true);
                        // }
                    }
                }
            }

            float xLeft = useRect.x;
            float xRight = useRect.xMax;

            HierarchyArea hierarchyArea = new HierarchyArea(
                useRect.y,
                useRect.height,
                labelRect.x, labelRect.xMax,
                useRect.x, useRect.xMax,
                useRect.x, Array.Empty<Rect>());

            string preLeftGroupBy = null;
            List<Rect> preLeftUsedRects = new List<Rect>();
            string preRightGroupBy = null;
            List<Rect> preRightUsedRects = new List<Rect>();

            foreach (Component component in allComponents)
            {
                foreach (RenderTargetInfo renderTargetInfo in Util.GetRenderTargetInfos(component))
                {
                    switch (renderTargetInfo.Attribute)
                    {
                        case HierarchyButtonAttribute hierarchyButtonAttribute:
                        {
                            if (hierarchyButtonAttribute.IsLeft)
                            {
                                preLeftGroupBy = null;
                                if(preLeftUsedRects.Count > 0)
                                {
                                    xLeft = Mathf.Max(xLeft, preLeftUsedRects.Max(each => each.xMax));
                                    preLeftUsedRects.Clear();
                                }
                            }
                            else
                            {
                                preRightGroupBy = null;
                                if(preRightUsedRects.Count > 0)
                                {
                                    xRight = Mathf.Min(xRight, preRightUsedRects.Min(each => each.x));
                                    preRightUsedRects.Clear();
                                }
                            }

                            float x = hierarchyButtonAttribute.IsLeft ? xLeft : xRight;
                            (bool buttonUsed, HierarchyUsed buttonHeaderUsed) = HierarchyButtonDrawer.Draw(
                                component,
                                hierarchyArea.EditorWrap(x, Array.Empty<Rect>()),
                                hierarchyButtonAttribute,
                                renderTargetInfo
                            );
                            if (buttonUsed)
                            {
                                if (hierarchyButtonAttribute.IsLeft)
                                {
                                    xLeft = buttonHeaderUsed.UsedRect.xMax;

                                }
                                else
                                {
                                    xRight = buttonHeaderUsed.UsedRect.x;

                                }
                            }

                            break;
                        }
                        case HierarchyDrawAttribute hierarchyDrawAttribute:
                        {
                            string groupBy = hierarchyDrawAttribute.GroupBy;
                            IReadOnlyList<Rect> usedRects = Array.Empty<Rect>();
                            if (!string.IsNullOrEmpty(groupBy))
                            {
                                // get used rect
                                usedRects = hierarchyDrawAttribute.IsLeft ? preLeftUsedRects : preRightUsedRects;

                                // check if it's new group
                                if (hierarchyDrawAttribute.IsLeft)
                                {
                                    if (preLeftGroupBy != hierarchyDrawAttribute.GroupBy && preLeftUsedRects.Count > 0)
                                    {
                                        preLeftGroupBy = hierarchyDrawAttribute.GroupBy;
                                        xLeft = Mathf.Max(xLeft, preLeftUsedRects.Max(each => each.xMax));
                                        preLeftUsedRects.Clear();
                                    }
                                }
                                else
                                {
                                    if (preRightGroupBy != hierarchyDrawAttribute.GroupBy && preRightUsedRects.Count > 0)
                                    {
                                        preRightGroupBy = hierarchyDrawAttribute.GroupBy;
                                        xRight = Mathf.Min(xRight, preRightUsedRects.Min(each => each.x));
                                        preRightUsedRects.Clear();
                                    }
                                }
                            }
                            else
                            {
                                if (hierarchyDrawAttribute.IsLeft)
                                {
                                    preLeftGroupBy = null;
                                    if(preLeftUsedRects.Count > 0)
                                    {
                                        xLeft = Mathf.Max(xLeft, preLeftUsedRects.Max(each => each.xMax));
                                        preLeftUsedRects.Clear();
                                    }
                                }
                                else
                                {
                                    preRightGroupBy = null;
                                    if(preRightUsedRects.Count > 0)
                                    {
                                        xRight = Mathf.Min(xRight, preRightUsedRects.Min(each => each.x));
                                        preRightUsedRects.Clear();
                                    }
                                }
                            }

                            (bool used, HierarchyUsed headerUsed) = HierarchyDrawDrawer.Draw(
                                component,
                                hierarchyArea.EditorWrap(hierarchyDrawAttribute.IsLeft ? xLeft : xRight, usedRects),
                                renderTargetInfo
                            );

                            if (used)
                            {
                                if (string.IsNullOrEmpty(groupBy))
                                {
                                    if (hierarchyDrawAttribute.IsLeft)
                                    {
                                        xLeft = headerUsed.UsedRect.xMax;
                                    }
                                    else
                                    {
                                        xRight = headerUsed.UsedRect.x;
                                    }
                                }
                                else
                                {
                                    if (hierarchyDrawAttribute.IsLeft)
                                    {
                                        preLeftUsedRects.Add(headerUsed.UsedRect);
                                        preLeftGroupBy = hierarchyDrawAttribute.GroupBy;
                                    }
                                    else
                                    {
                                        preRightUsedRects.Add(headerUsed.UsedRect);
                                        preRightGroupBy = hierarchyDrawAttribute.GroupBy;
                                    }
                                }
                            }
                        }
                            break;
                        case HierarchyLabelAttribute hierarchyLabelAttribute:
                        {
                            string groupBy = hierarchyLabelAttribute.GroupBy;
                            IReadOnlyList<Rect> usedRects = Array.Empty<Rect>();
                            if (!string.IsNullOrEmpty(groupBy))
                            {
                                // get used rect
                                usedRects = hierarchyLabelAttribute.IsLeft ? preLeftUsedRects : preRightUsedRects;

                                // check if it's new group
                                if (hierarchyLabelAttribute.IsLeft)
                                {
                                    if (preLeftGroupBy != hierarchyLabelAttribute.GroupBy && preLeftUsedRects.Count > 0)
                                    {
                                        preLeftGroupBy = hierarchyLabelAttribute.GroupBy;
                                        xLeft = Mathf.Max(xLeft, preLeftUsedRects.Max(each => each.xMax));
                                        preLeftUsedRects.Clear();
                                    }
                                }
                                else
                                {
                                    if (preRightGroupBy != hierarchyLabelAttribute.GroupBy && preRightUsedRects.Count > 0)
                                    {
                                        preRightGroupBy = hierarchyLabelAttribute.GroupBy;
                                        xRight = Mathf.Min(xRight, preRightUsedRects.Min(each => each.x));
                                        preRightUsedRects.Clear();
                                    }
                                }
                            }
                            else
                            {
                                if (hierarchyLabelAttribute.IsLeft)
                                {
                                    preLeftGroupBy = null;
                                    if(preLeftUsedRects.Count > 0)
                                    {
                                        xLeft = Mathf.Max(xLeft, preLeftUsedRects.Max(each => each.xMax));
                                        preLeftUsedRects.Clear();
                                    }
                                }
                                else
                                {
                                    preRightGroupBy = null;
                                    if(preRightUsedRects.Count > 0)
                                    {
                                        xRight = Mathf.Min(xRight, preRightUsedRects.Min(each => each.x));
                                        preRightUsedRects.Clear();
                                    }
                                }
                            }

                            (bool used, HierarchyUsed headerUsed) = HierarchyLabelDrawer.Draw(
                                component,
                                hierarchyArea.EditorWrap(hierarchyLabelAttribute.IsLeft ? xLeft : xRight, usedRects),
                                hierarchyLabelAttribute,
                                renderTargetInfo
                            );

                            if (used)
                            {
                                if (string.IsNullOrEmpty(groupBy))
                                {
                                    if (hierarchyLabelAttribute.IsLeft)
                                    {
                                        xLeft = headerUsed.UsedRect.xMax;
                                    }
                                    else
                                    {
                                        xRight = headerUsed.UsedRect.x;
                                    }
                                }
                                else
                                {
                                    if (hierarchyLabelAttribute.IsLeft)
                                    {
                                        preLeftUsedRects.Add(headerUsed.UsedRect);
                                        preLeftGroupBy = hierarchyLabelAttribute.GroupBy;
                                    }
                                    else
                                    {
                                        preRightUsedRects.Add(headerUsed.UsedRect);
                                        preRightGroupBy = hierarchyLabelAttribute.GroupBy;
                                    }
                                }
                            }
                        }
                            break;
                    }
                }
            }
        }

        private static GUIStyle _labelStylePrefab;

        private static GUIStyle GetLabelStylePrefab()
        {
            return _labelStylePrefab ??= new GUIStyle(EditorStyles.label)
            {
                normal =
                {
                    textColor = new Color32(115, 156, 217, 255),
                },
            };
        }

        private static GUIStyle _labelStyleMissingPrefab;

        private static GUIStyle GetLabelStyleMissingPrefab()
        {
            return _labelStyleMissingPrefab ??= new GUIStyle(EditorStyles.label)
            {
                normal =
                {
                    textColor = new Color32(211, 106, 106, 255),
                },
            };
        }
        private static Texture2D GetIconByComponent(IEnumerable<Component> components)
        {
            foreach (Component comp in components)
            {
                switch (comp)
                {
                    case IHierarchyIconPath hierarchyIconPath:
                        return Util.LoadResource<Texture2D>(hierarchyIconPath.HierarchyIconPath);
                    case IHierarchyIconTexture2D hierarchyIconTexture2D:
                        return hierarchyIconTexture2D.HierarchyIconTexture2D;
                    case Camera:
                        return (Texture2D)EditorGUIUtility.IconContent("d_Camera Icon").image;
                    case Light:
                        return (Texture2D)EditorGUIUtility.IconContent("d_DirectionalLight Icon").image;
                    case Canvas:
                        return (Texture2D)EditorGUIUtility.IconContent("d_Canvas Icon").image;
                    case EventSystem:
                        return (Texture2D)EditorGUIUtility.IconContent("d_EventSystem Icon").image;
#if SAINTSHIERARCHY_UNITY_RENDER_PIPELINES_CORE
                    case UnityEngine.Rendering.Volume:
                        return Util.LoadResource<Texture2D>("d_Volume Icon.asset");
#endif
#if SAINTSHIERARCHY_WWISE
                    case AkInitializer:
                        return Util.LoadResource<Texture2D>("wwise-logo.png");
#endif
                }
            }

            return null;
        }

        private static IReadOnlyList<(GameObject root, string path)> GetPrefabRootTopToBottom(GameObject go)
        {
            List<(GameObject, string)> result = new List<(GameObject, string)>();

            List<string> names = new List<string>();

            Transform current = go.transform;
            while (current != null)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(current.gameObject))
                {
                    string subName = string.Join("/", names);
                    result.Add((current.gameObject, subName));
                }
                names.Insert(0, current.name);
                current = current.parent;
            }

            result.Reverse();

            return result;
        }

        private static IReadOnlyList<string> GetAbsPath(Transform trans)
        {
            List<string> names = new List<string>();

            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            GameObject prefabContentsRoot =
                stage != null ? stage.prefabContentsRoot : null;


            Transform current = trans;
            while (current != null)
            {
                // Debug.Log($"add {current.name}");
                names.Insert(0, current.name);

                if (stage != null &&
                    current.gameObject.scene == stage.scene &&
                    current.gameObject == prefabContentsRoot)
                {
                    break;
                }

                current = current.parent;

            }
            // Debug.Log($"names={string.Join("/",  names)}");
            return names;
        }

        private static (bool found, GameObjectConfig config) GetGameObjectConfig(GameObject go)
        {
            GlobalObjectId goId = GlobalObjectId.GetGlobalObjectIdSlow(go);
            // if (go.name == "PrefabInsideAPrefab")
            // {
            //     Debug.Log($"raw: {goId}");
            // }

            string scenePath = go.scene.path;
            // Debug.Log($"scenePath={scenePath}");
            if (string.IsNullOrEmpty(scenePath))
            {
                scenePath = AssetDatabase.GetAssetPath(go);
            }
            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            string norId = Util.GlobalObjectIdNormString(goId);
            // (bool found, SaintsHierarchyConfig.GameObjectConfig config) = FindConfig(sceneGuid, Utils.GlobalObjectIdNormStringNoPrefabLink(goId));
            // if (go.name == "PrefabInsideAPrefab")
            // {
            //     Debug.Log($"nor: {norId}");
            // }
            (bool found, GameObjectConfig config) = FindConfig(sceneGuid, norId);
            // string upkId = Utils.GlobalObjectIdNormStringNoPrefabLink(goId);
            // (bool found, SaintsHierarchyConfig.GameObjectConfig config) = FindConfig(sceneGuid, upkId);
            if (found)
            {
                return (true, config);
            }

            // IReadOnlyList<GameObject> prefabRootTopToBottom = GetPrefabRootTopToBottom(go);
            foreach ((GameObject prefabInstanceRoot, string relativePath) in GetPrefabRootTopToBottom(go))
            {
                // if (go.name == "PrefabInsideAPrefab")
                // {
                //     Debug.Log($"go={go.name}: {prefabInstanceRoot.name}->{relativePath}");
                // }
                // GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabInstanceRoot);
                string prefabPath =
                    AssetDatabase.GetAssetPath(
                        PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabInstanceRoot));
                // Debug.Log(prefabPath);
                if (string.IsNullOrEmpty(prefabPath))  // broken prefab
                {
                    return default;
                }
                // Debug.Log(prefabPath);
                GameObject prefabAsset;
                if(prefabPath.EndsWith(".prefab"))
                {
                    prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                }
                else  // fbx etc
                {
                    // Debug.Log(AssetDatabase.LoadAssetAtPath<DefaultAsset>(prefabPath));
                    // Debug.Log(go);
                    // Debug.Log(prefabPath);
                    continue;
                    // foreach (Object o in AssetDatabase
                    //              .LoadAllAssetsAtPath(prefabPath))
                    // {
                    //     Debug.Log($"out: {o}");
                    // }
                    // prefabAsset = AssetDatabase
                    //     .LoadAllAssetsAtPath(prefabPath)
                    //     .OfType<GameObject>()
                    //     .FirstOrDefault();
                    // if (prefabAsset == null)
                    // {
                    //     Debug.LogWarning($"Failed to load file {prefabPath}. Please report this issue");
                    //     // return (false, default);
                    //     continue;
                    // }
                }

                // Debug.Log($"{prefabAsset}: {prefabPath}->{relativePath}");

                GameObject prefabSubGo;
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (relativePath == "")
                {
                    prefabSubGo = prefabAsset;
                }
                else
                {
                    Transform subTarget = prefabAsset.transform.Find(relativePath);
                    if (subTarget == null)
                    {

#if SAINTSHIERARCHY_DEBUG
                        Debug.LogWarning($"Could not find prefab asset {prefabPath} relative to {relativePath}");
#endif
                        continue;
                    }
                    prefabSubGo = subTarget.gameObject;
                }
                GlobalObjectId prefabSubGoId = GlobalObjectId.GetGlobalObjectIdSlow(prefabSubGo);
                string prefabSubGoIdStr = Util.GlobalObjectIdNormString(prefabSubGoId);
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefabAsset));
                // if (go.name == "PrefabInsideAPrefab")
                // {
                //     Debug.Log($"prefab prefabSubGo = {prefabSubGo.name}");
                //     Debug.Log($"prefab path = {prefabPath}");
                //     Debug.Log($"prefab={guid}/goId={prefabSubGoIdStr}");
                // }
                (bool found, GameObjectConfig config) prefabConfig = FindConfig(guid, prefabSubGoIdStr);
                if (prefabConfig.found)
                {
                    return (true, prefabConfig.config);
                }
            }

            return (false, default);
        }

        private static (bool found, GameObjectConfig config) FindConfig(string sceneGuid, string goIdString)
        {
            bool personalDisabled = !PersonalHierarchyConfig.instance.personalEnabled;
            List<SceneGuidToGoConfigs> sceneGuidToGoConfigsList = personalDisabled
                ? SaintsHierarchyConfig.instance.sceneGuidToGoConfigsList
                : PersonalHierarchyConfig.instance.sceneGuidToGoConfigsList;

            foreach (SceneGuidToGoConfigs sceneGuidToGoConfigs in sceneGuidToGoConfigsList)
            {
                if (sceneGuidToGoConfigs.sceneGuid == sceneGuid)
                {
                    foreach (GameObjectConfig gameObjectConfig in sceneGuidToGoConfigs.configs)
                    {
                        if (gameObjectConfig.globalObjectIdString == goIdString)
                        {
                            return (true, gameObjectConfig);
                        }
                    }
                }
            }

            return (false, default);
        }

        private static bool _sceneHierarchyWindowsFieldInit;
        private static FieldInfo _sceneHierarchyWindowsField;
        private static bool _sceneHierarchyFieldInit;
        private static FieldInfo _sceneHierarchyField;

        private static (string Error, EditorWindow sceneHierarchyWindow, object sceneHierarchy) GetSceneHierarchyWindow(Rect selectionRect)
        {
            if (!_sceneHierarchyWindowsFieldInit)
            {
                _sceneHierarchyWindowsFieldInit = true;
                _sceneHierarchyWindowsField ??= typeof(UnityEditor.Editor).Assembly
                    .GetType("UnityEditor.SceneHierarchyWindow").GetField(
                        "s_SceneHierarchyWindows",
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
            }

            if (_sceneHierarchyWindowsField == null)
            {
                return ("_sceneHierarchyWindowsField not found", null, null);
            }

            if (_sceneHierarchyFieldInit && _sceneHierarchyField == null)
            {
                return ("_sceneHierarchyField not found", null, null);
            }

            List<EditorWindow> sceneHierarchyWindows = ((IList)_sceneHierarchyWindowsField.GetValue(null)).Cast<EditorWindow>().ToList();
            EditorWindow sceneHierarchyWindow;
            if (sceneHierarchyWindows.Count == 1)
            {
                sceneHierarchyWindow = sceneHierarchyWindows[0];
            }
            else
            {
                Vector2 screenPoint = GUIUtility.GUIToScreenPoint(selectionRect.center);
                sceneHierarchyWindow =
                    sceneHierarchyWindows.FirstOrDefault(r => r.hasFocus && r.position.Contains(screenPoint));
                if (sceneHierarchyWindow == null)
                {
                    return ("sceneHierarchyWindow focused not found", null, null);
                }
            }

            const BindingFlags instanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
            // Type type = sceneHierarchyWindow.GetType();
            if (!_sceneHierarchyFieldInit)
            {
                _sceneHierarchyFieldInit = true;
                _sceneHierarchyField = sceneHierarchyWindow.GetType().GetField("m_SceneHierarchy", instanceNonPublic);
                if (_sceneHierarchyField == null)
                {
                    return ("_sceneHierarchyField not found", sceneHierarchyWindow, null);
                }
            }

            object sceneHierarchy = _sceneHierarchyField.GetValue(sceneHierarchyWindow);
            string error = sceneHierarchy == null ? "sceneHierarchy not found" : "";

            return (error, sceneHierarchyWindow, sceneHierarchy);
        }

        private static FieldInfo _keyboardControlIdField;
        private static bool _keyboardControlIdFieldInit;

        private enum SelectStatus
        {
            Normal,
            NormalHover,
            SelectFocus,
            SelectUnfocus,
        }

        private static (string error, SelectStatus selectStatus) GetBgColor(EditorWindow sceneHierarchyWindow, object sceneHierarchy, int instanceID, bool isHover)
        {
            (string selectedError, int[] selectedIds) = GetSelectedIds(sceneHierarchy);
            if (selectedError != "")
            {
                return (selectedError, default);
            }

            if (selectedIds.Contains(instanceID))
            {
                bool isTreeFocused = false;
                // ReSharper disable once InvertIf
                if (EditorWindow.focusedWindow == sceneHierarchyWindow)
                {
                    if (!_keyboardControlIdFieldInit)
                    {
                        _keyboardControlIdFieldInit = true;
                        const BindingFlags instanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
                        _keyboardControlIdField =
                            sceneHierarchy.GetType().GetField(
                                "m_TreeViewKeyboardControlID", instanceNonPublic);
                        if (_keyboardControlIdField == null)
                        {
                            return ("_keyboardControlIdFieldInit not found", default);
                        }
                    }
                    int keyboardControlId = (int)_keyboardControlIdField.GetValue(sceneHierarchy);
                    if (GUIUtility.keyboardControl == keyboardControlId)
                    {
                        isTreeFocused = true;
                    }
                    // var treeViewControllerState = treeViewController.GetPropertyValue<TreeViewState>("state");
                    // expandedIds = treeViewControllerState?.expandedIDs.ToInts() ?? new();
                    // && GUIUtility.keyboardControl == sceneHierarchy?.GetMemberValue<int>("m_TreeViewKeyboardControlID");
                }


                return ("", isTreeFocused ? SelectStatus.SelectFocus : SelectStatus.SelectUnfocus);
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (isHover)
            {
                return ("", SelectStatus.NormalHover);
            }
            return ("", SelectStatus.Normal);
        }

        private static FieldInfo _treeViewField;
        private static bool _treeViewFieldInit;
        private static PropertyInfo _treeViewControllerStateField;
        private static bool _treeViewControllerStateFieldInit;

        private static (string error, int[] expandedIds) GetExpandedIds(object sceneHierarchy)
        {
            if (!_treeViewFieldInit)
            {
                _treeViewFieldInit = true;
                _treeViewField = sceneHierarchy.GetType().GetField("m_TreeView", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (_treeViewField == null)
            {
                return ("_treeViewField not found", null);
            }
            object treeViewController = _treeViewField.GetValue(sceneHierarchy);
            // Debug.Log(treeViewController);
            if (!_treeViewControllerStateFieldInit)
            {
                _treeViewControllerStateFieldInit = true;
                _treeViewControllerStateField = treeViewController.GetType().GetProperty("state", BindingFlags.Instance | BindingFlags.Public);

                if (_treeViewControllerStateField == null)
                {
                    return ("_treeViewControllerStateField not found", null);
                }
            }

            // Debug.Log(prop);
            object treeViewControllerStateRaw = _treeViewControllerStateField.GetValue(treeViewController);
            // Debug.Log(treeViewControllerStateRaw);
            if (treeViewControllerStateRaw is not
#if UNITY_6000_3_OR_NEWER
                TreeViewState<EntityId>
#else
                TreeViewState
#endif
                treeViewControllerState)
            {
                return ("treeViewControllerState not found", null);
            }
            // Debug.Log(treeViewControllerState);
            // Debug.Log(treeViewControllerState.expandedIDs);

            int[] expandedIds = treeViewControllerState.expandedIDs.Select(each => (int)each).ToArray();
            // Debug.Log($"expanded: {string.Join(", ", treeViewControllerState.expandedIDs)}");
            return ("", expandedIds);
        }

        private static (string error, int[] selectedIds) GetSelectedIds(object sceneHierarchy)
        {
            if (!_treeViewFieldInit)
            {
                _treeViewFieldInit = true;
                _treeViewField = sceneHierarchy.GetType().GetField("m_TreeView", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (_treeViewField == null)
            {
                return ("_treeViewField not found", null);
            }
            object treeViewController = _treeViewField.GetValue(sceneHierarchy);
            // Debug.Log(treeViewController);
            if (!_treeViewControllerStateFieldInit)
            {
                _treeViewControllerStateFieldInit = true;
                _treeViewControllerStateField = treeViewController.GetType().GetProperty("state", BindingFlags.Instance | BindingFlags.Public);

                if (_treeViewControllerStateField == null)
                {
                    return ("_treeViewControllerStateField not found", null);
                }
            }

            // Debug.Log(prop);
            object treeViewControllerStateRaw = _treeViewControllerStateField.GetValue(treeViewController);
            // Debug.Log(treeViewControllerStateRaw);
            if (treeViewControllerStateRaw is not
#if UNITY_6000_3_OR_NEWER
                TreeViewState<EntityId>
#else
                TreeViewState
#endif
                treeViewControllerState)
            {
                return ("treeViewControllerState not found", null);
            }
            // Debug.Log(treeViewControllerState);
            // Debug.Log(treeViewControllerState.expandedIDs);

            int[] selectedIDs = treeViewControllerState.selectedIDs.Select(each => (int)each).ToArray();
            // Debug.Log($"expanded: {string.Join(", ", treeViewControllerState.expandedIDs)}");
            return ("", selectedIDs);
        }

        private static readonly Color ColorSelectFocus = new Color(.17f, .365f, .535f);
        private static readonly Color ColorSelectUnfocus = new Color(.3f, .3f, .3f);
        private static readonly Color ColorNormal = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color ColorHover = new Color(.265f, .265f, .265f);
        private static readonly Color ColorStripedLight = new Color(1f, 1f, 1f, 0.025f);

        private static void DrawIndentDepth(Rect drawIndent, int inherentDepth, Transform trans)
        {
            if (inherentDepth == 1)
            {
                Color useColor = TreeColor;
                Transform parent = trans.parent;
                if (parent != null)
                {
                    GameObjectConfig config = GetGameObjectConfig(parent.gameObject).config;
                    if (config.hasColor)
                    {
                        useColor = config.color;
                    }
                }

                bool hasNext = HasNextSibling(trans);
                if (hasNext)
                {
                    DrawDownThroughRight(drawIndent, useColor);
                }
                else
                {
                    DrawDownRight(drawIndent, useColor);
                }

                // EditorGUI.DrawRect(drawIndent, Color.blue);
            }
            else
            {
                Transform preParent = trans;
                Transform parent = trans;
                int accDepth = inherentDepth;
                while (accDepth >= 1)
                {
                    accDepth--;
                    preParent = parent;
                    parent = parent.parent;
                    if (parent == null)
                    {
                        return;
                    }
                }
                // GUI.Label(drawIndent, parent.gameObject.name[0].ToString());
                // GUI.Label(drawIndent, preParent.gameObject.name[0].ToString());
                bool preParentHasNext = HasNextSibling(preParent);
                if (preParentHasNext)
                {
                    Color useColor = TreeColor;
                    if (parent != null)
                    {
                        GameObjectConfig config = GetGameObjectConfig(parent.gameObject).config;
                        if (config.hasColor)
                        {
                            useColor = config.color;
                        }
                    }
                    DrawDownThrough(drawIndent, useColor);
                }
            }
        }

        private static bool HasNextSibling(Transform t)
        {
            Transform parent = t.parent;
            if (parent == null)
            {
                return false;
            }

            int index = t.GetSiblingIndex();
            return index + 1 < parent.childCount;
        }

        private static void DrawDownThroughRight(Rect rect, Color color)
        {
            float startX = rect.x + rect.width / 2 -  0.5f;
            Rect downThrough = new Rect(startX, rect.y, 1, rect.height);
            EditorGUI.DrawRect(downThrough, color);

            Rect middleRight = new Rect(startX, rect.y + rect.height / 2 - 0.5f, rect.width / 2 + 1f, 1);
            EditorGUI.DrawRect(middleRight, color);
        }

        private static void DrawDownThrough(Rect rect, Color color)
        {
            float startX = rect.x + rect.width / 2 - 0.5f;
            Rect downThrough = new Rect(startX, rect.y, 1, rect.height);
            EditorGUI.DrawRect(downThrough, color);
        }

        private static void DrawDownRight(Rect rect, Color color)
        {
            float startX = rect.x + rect.width / 2 - 0.5f;
            Rect down = new Rect(startX, rect.y, 1, rect.height / 2);
            EditorGUI.DrawRect(down, color);

            Rect middleRight = new Rect(startX, rect.y + rect.height / 2 - 0.5f, rect.width / 2 + 1f, 1);
            EditorGUI.DrawRect(middleRight, color);
        }

        private static void DrawRightThrough(Rect rect, Color color)
        {
            Rect middleRight = new Rect(rect.x, rect.y + rect.height / 2 - 0.5f, rect.width, 1);
            EditorGUI.DrawRect(middleRight, color);
        }
    }
}
