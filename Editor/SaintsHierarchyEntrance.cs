#if (WWISE_2024_OR_LATER || WWISE_2023_OR_LATER || WWISE_2022_OR_LATER || WWISE_2021_OR_LATER || WWISE_2020_OR_LATER || WWISE_2019_OR_LATER || WWISE_2018_OR_LATER || WWISE_2017_OR_LATER || WWISE_2016_OR_LATER || SAINTSFIELD_WWISE) && !SAINTSFIELD_WWISE_DISABLE
#define SAINTSHIERARCHY_WWISE
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
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

        private static int? _selectedInstance;

        private static bool IsSelected(int instanceID)
        {
            if (_selectedInstance != null)
            {
                return instanceID == _selectedInstance;
            }

            return Selection.entityIds.Contains(instanceID);
        }

        private static readonly Color TreeColor = new Color(0.4f, 0.4f, 0.4f);

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            // Get the object corresponding to the ID
            Object obj = EditorUtility.EntityIdToObject(instanceID);

            if (obj == null)
            {
                return;
            }

            if (obj is not GameObject go)
            {
                return;
            }

            Rect fullRect = new Rect(selectionRect)
            {
                x = LeftStartX,
                xMax = selectionRect.xMax + 16,
            };

            Vector2 mousePosition = Event.current.mousePosition;
            bool isHover = fullRect.Contains(mousePosition);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && isHover)
            {
                _selectedInstance = instanceID;
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                _selectedInstance = null;
            }



            var curScenePath = go.scene.path;
            // Debug.Log($"popup parent: {string.Join(",", parentRoots)}");
            // Debug.Log($"popup scene: {curScenePath}");
            // GameObject targetGo = go;
            if (curScenePath.EndsWith(".prefab"))
            {
                string absPath = string.Join("/", GetAbsPath(go.transform).Skip(1));
                Debug.Log($"popup absPath: {absPath} for {go.name}");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(curScenePath);
                GameObject newGo;
                if (absPath == "")
                {
                    newGo = prefab;
                }
                else
                {
                    Debug.Log(prefab.name);
                    Debug.Log(prefab.transform);
                    newGo = prefab.transform.Find(absPath).gameObject;
                }
                Debug.Assert(newGo != null, absPath);
                go = newGo;
                // Debug.Log($"popup re-id: {GlobalObjectId.GetGlobalObjectIdSlow(targetGo)}");
            }

            SaintsHierarchyConfig.GameObjectConfig goConfig = GetGameObjectConfig(go);

            Transform trans = go.transform;

            #region Tree

            bool hasFoldout = trans.childCount >= 1;

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
                int indentXLast = StartOffset + (indentLevel - 1) * IndentOffset;
                Rect drawIndent = new Rect(selectionRect)
                {
                    x = indentXLast,
                    width = IndentOffset,
                };
                // GUI.Label(drawIndent, "─");
                DrawRightThrough(drawIndent, TreeColor);
            }
            #endregion

            #region Main Icon

            bool hasCustomIcon = false;
            Texture iconTexture = null;
            if (!string.IsNullOrEmpty(goConfig.icon))
            {
                iconTexture = Utils.LoadResource<Texture2D>(goConfig.icon);
                hasCustomIcon = iconTexture != null;
            }
            else if (trans.GetComponent<Camera>())
            {
                iconTexture = EditorGUIUtility.IconContent("Camera Icon").image;
            }
            else if (trans.GetComponent<Light>())
            {
                iconTexture = EditorGUIUtility.IconContent("DirectionalLight Icon").image;
            }
            else if (trans.GetComponent<Volume>())
            {
                iconTexture = Utils.LoadResource<Texture2D>("d_Volume Icon.asset");
            }
#if SAINTSHIERARCHY_WWISE
            else if (trans.GetComponent<AkInitializer>())
            {
                iconTexture = Utils.LoadResource<Texture2D>("wwise-logo.png");
            }
#endif

            Texture prefabTexture = null;
            bool isAnyPrefabInstanceRoot = PrefabUtility.IsAnyPrefabInstanceRoot(go);
            if(isAnyPrefabInstanceRoot)
            {
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
                        prefabTexture = EditorGUIUtility.IconContent("d_PrefabModel Icon").image;
                        break;
                }

                if (instanceStatus == PrefabInstanceStatus.MissingAsset)
                {
                    prefabTexture = EditorGUIUtility.IconContent("d_PrefabVariant On Icon").image;
                }
            }

            Rect iconRect = new Rect(selectionRect)
            {
                width = IndentOffset,
            };

            if(iconTexture is not null)
            {
                Color bg = GetBgColor(selectionRect, instanceID, isHover);
                EditorGUI.DrawRect(iconRect, bg);
                GUI.DrawTexture(iconRect, iconTexture, ScaleMode.ScaleToFit, true);

                if (prefabTexture is not null)
                {
                    const float scale = 0.7f;
                    Rect footerIconRect = new Rect(iconRect.x + iconRect.width * (1 - scale) + 5, iconRect.y + iconRect.height *
                        (1 - scale),
                        iconRect.width * scale, iconRect.height * scale);
                    GUI.DrawTexture(footerIconRect, prefabTexture);
                }
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && isHover && (Event.current.modifiers & EventModifiers.Alt) != 0)
            {
                Debug.Log($"popup id: {GlobalObjectId.GetGlobalObjectIdSlow(go)}");
                // var parentRoots = GetPrefabRootTopToBottom(go);

                Utils.PopupConfig(new Rect(mousePosition.x, mousePosition.y, 0, 0), go, hasCustomIcon);
            }

            #endregion
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
                Debug.Log($"add {current.name}");
                names.Insert(0, current.name);

                if (stage != null &&
                    current.gameObject.scene == stage.scene &&
                    current.gameObject == prefabContentsRoot)
                {
                    break;
                }

                current = current.parent;

            }
            Debug.Log($"names={string.Join("/",  names)}");
            return names;
        }

        private static SaintsHierarchyConfig.GameObjectConfig GetGameObjectConfig(GameObject go)
        {
            GlobalObjectId goId = GlobalObjectId.GetGlobalObjectIdSlow(go);
            if (go.name == "PrefabInsideAPrefab")
            {
                Debug.Log($"raw: {goId}");
            }

            string scenePath = go.scene.path;
            // Debug.Log($"scenePath={scenePath}");
            if (string.IsNullOrEmpty(scenePath))
            {
                scenePath = AssetDatabase.GetAssetPath(go);
            }
            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            string norId = Utils.GlobalObjectIdNormString(goId);
            // (bool found, SaintsHierarchyConfig.GameObjectConfig config) = FindConfig(sceneGuid, Utils.GlobalObjectIdNormStringNoPrefabLink(goId));
            // if (go.name == "PrefabInsideAPrefab")
            // {
            //     Debug.Log($"nor: {norId}");
            // }
            (bool found, SaintsHierarchyConfig.GameObjectConfig config) = FindConfig(sceneGuid, norId);
            // string upkId = Utils.GlobalObjectIdNormStringNoPrefabLink(goId);
            // (bool found, SaintsHierarchyConfig.GameObjectConfig config) = FindConfig(sceneGuid, upkId);
            if (found)
            {
                return config;
            }

            // IReadOnlyList<GameObject> prefabRootTopToBottom = GetPrefabRootTopToBottom(go);
            foreach ((GameObject prefabInstanceRoot, string relativePath) in GetPrefabRootTopToBottom(go))
            {
                if (go.name == "PrefabInsideAPrefab")
                {
                    Debug.Log($"go={go.name}: {prefabInstanceRoot.name}->{relativePath}");
                }
                // GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabInstanceRoot);
                string prefabPath =
                    AssetDatabase.GetAssetPath(
                        PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabInstanceRoot));
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                GameObject prefabSubGo = relativePath == ""? prefabAsset: prefabAsset.transform.Find(relativePath).gameObject;
                GlobalObjectId prefabSubGoId = GlobalObjectId.GetGlobalObjectIdSlow(prefabSubGo);
                string prefabSubGoIdStr = Utils.GlobalObjectIdNormString(prefabSubGoId);
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefabAsset));
                if (go.name == "PrefabInsideAPrefab")
                {
                    Debug.Log($"prefab prefabSubGo = {prefabSubGo.name}");
                    Debug.Log($"prefab path = {prefabPath}");
                    Debug.Log($"prefab={guid}/goId={prefabSubGoIdStr}");
                }
                (bool found, SaintsHierarchyConfig.GameObjectConfig config) prefabConfig = FindConfig(guid, prefabSubGoIdStr);
                if (prefabConfig.found)
                {
                    return prefabConfig.config;
                }
            }

            return default;
        }

        private static (bool found, SaintsHierarchyConfig.GameObjectConfig config) FindConfig(string sceneGuid, string goIdString)
        {

            SaintsHierarchyConfig config = Utils.EnsureConfig();
            // string goIdString = goId.ToString();
            // string goIdString = Utils.GlobalObjectIdNormString(goId);

            foreach (SaintsHierarchyConfig.SceneGuidToGoConfigs sceneGuidToGoConfigs in config.sceneGuidToGoConfigsList)
            {
                if (sceneGuidToGoConfigs.sceneGuid == sceneGuid)
                {
                    foreach (SaintsHierarchyConfig.GameObjectConfig gameObjectConfig in sceneGuidToGoConfigs.configs)
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


        private static Color GetBgColor(Rect selectionRect, int instanceID, bool isHover)
        {
            if (IsSelected(instanceID))
            {
                Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
                Type sceneHierarchyWindowType =
                    editorAssembly.GetType("UnityEditor.SceneHierarchyWindow");
                FieldInfo field = sceneHierarchyWindowType.GetField(
                    "s_SceneHierarchyWindows",
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                List<EditorWindow> sceneHierarchyWindows = ((IList)field.GetValue(null)).Cast<EditorWindow>().ToList();
                EditorWindow sceneHierarchyWindow = null;
                if (sceneHierarchyWindows.Count == 1)
                {
                    sceneHierarchyWindow = sceneHierarchyWindows[0];
                }
                else
                {
                    Vector2 screenPoint = GUIUtility.GUIToScreenPoint(selectionRect.center);
                    sceneHierarchyWindow = sceneHierarchyWindows.FirstOrDefault(r =>
                        r.hasFocus && r.position.Contains(screenPoint));
                }
                bool isTreeFocused = false;
                if (EditorWindow.focusedWindow == sceneHierarchyWindow)
                {
                    const BindingFlags instanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
                    Type type = sceneHierarchyWindow.GetType();
                    FieldInfo sceneField = type.GetField("m_SceneHierarchy", instanceNonPublic);
                    object sceneHierarchy = sceneField.GetValue(sceneHierarchyWindow);   //
                    // Debug.Log(sceneHierarchy);
                    FieldInfo keyboardControlIdField =
                        sceneHierarchy.GetType().GetField(
                            "m_TreeViewKeyboardControlID", instanceNonPublic);
                    int keyboardControlId = (int)keyboardControlIdField.GetValue(sceneHierarchy);
                    if (GUIUtility.keyboardControl == keyboardControlId)
                    {
                        isTreeFocused = true;
                    }
                    // && GUIUtility.keyboardControl == sceneHierarchy?.GetMemberValue<int>("m_TreeViewKeyboardControlID");
                }


                return isTreeFocused? ColorSelectFocus: ColorSelectUnfocus;
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (isHover)
            {
                return ColorHover;
            }
            return ColorNormal;
        }

        private static readonly Color ColorSelectFocus = new Color(.17f, .365f, .535f);
        private static readonly Color ColorSelectUnfocus = new Color(.3f, .3f, .3f);
        private static readonly Color ColorNormal = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color ColorHover = new Color(.265f, .265f, .265f);

        private static void DrawIndentDepth(Rect drawIndent, int inherentDepth, Transform trans)
        {
            if (inherentDepth == 1)
            {
                bool hasNext = HasNextSibling(trans);
                if (hasNext)
                {
                    DrawDownThroughRight(drawIndent, TreeColor);
                }
                else
                {
                    DrawDownRight(drawIndent, TreeColor);
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
                    DrawDownThrough(drawIndent, TreeColor);
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
            float startX = rect.x + rect.width / 2 -  0.5f;
            Rect downThrough = new Rect(startX, rect.y, 1, rect.height);
            EditorGUI.DrawRect(downThrough, color);
        }

        private static void DrawDownRight(Rect rect, Color color)
        {
            float startX = rect.x + rect.width / 2 -  0.5f;
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
