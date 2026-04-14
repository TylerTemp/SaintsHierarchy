using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SaintsHierarchy.Editor
{
    public class SaintsHierarchyWindow
    {
        private static Type _sceneHierarchyWindowType;
        private static FieldInfo _sLastInteractedHierarchy;
        private static FieldInfo _fieldMSceneHierarchy;
        private static PropertyInfo _propertyTreeViewRect;

        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            _sceneHierarchyWindowType ??= typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            if (_sceneHierarchyWindowType == null)
            {
                return;
            }

            _sLastInteractedHierarchy ??= _sceneHierarchyWindowType.GetField("s_LastInteractedHierarchy", BindingFlags.NonPublic | BindingFlags.Static);
            if (_sLastInteractedHierarchy == null)
            {
                return;
            }

            _fieldMParent ??= typeof(EditorWindow).GetField("m_Parent", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_fieldMParent == null)
            {
                Debug.Log("m_Parent is null");
                return;
            }

            _fieldMSceneHierarchy ??= _sceneHierarchyWindowType.GetField("m_SceneHierarchy", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_fieldMSceneHierarchy == null)
            {
                return;
            }

            _propertyTreeViewRect ??= _sceneHierarchyWindowType.GetProperty("treeViewRect", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_propertyTreeViewRect == null)
            {
                return;
            }

            _fieldMPos ??= typeof(EditorWindow).GetField("m_Pos", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_fieldMPos == null)
            {
                return;
            }

            EditorApplication.delayCall += CheckWindowAll;
            EditorWindow.windowFocusChanged -= CheckWindowFocused;
            EditorWindow.windowFocusChanged += CheckWindowFocused;
        }

        private static bool IsDisabled()
        {
            bool personalDisabled = !PersonalHierarchyConfig.instance.personalEnabled;
            return personalDisabled
                ? SaintsHierarchyConfig.instance.disabled
                : PersonalHierarchyConfig.instance.disabled;
        }

        private static void CheckWindowAll()
        {
            if (IsDisabled())
            {
                return;
            }
            EditorWindow[] allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();

            foreach (EditorWindow window in allWindows)
            {
                if (window.GetType() == _sceneHierarchyWindowType)
                {
                    SetupWrap(window);
                }
            }
        }

        private static void CheckWindowFocused()
        {
            if (IsDisabled())
            {
                return;
            }

            EditorWindow fWindow = EditorWindow.focusedWindow;
            if (fWindow == null)
            {
                return;
            }

            if (fWindow.GetType() == _sceneHierarchyWindowType)
            {
                SetupWrap(fWindow);
            }
        }

        private static readonly Dictionary<EditorWindow, WrapInfo> Wrapped = new Dictionary<EditorWindow, WrapInfo>();
        // public static readonly Dictionary<EditorWindow, Delegate> OriginDelegate = new Dictionary<EditorWindow, Delegate>();
        // private readonly Delegate _onGUI;

        // public SaintsHierarchyWindow(Delegate onGUI)
        // {
        //     _onGUI = onGUI;
        // }

        private readonly struct WrapInfo
        {
            public readonly Delegate OriginalOnGUI;

            public readonly Func<
#if UNITY_6000_3_OR_NEWER
                EntityId
#else
                int
#endif
                , bool, bool> SetExpand;
            public readonly object TreeViewData;
            public readonly PropertyInfo PropertyRowCount;
            private readonly PropertyInfo PropertyTreeViewRect;

            public readonly Func<
#if UNITY_6000_3_OR_NEWER
                EntityId
#else
                int
#endif
                , int
            > GetRow;

            public readonly TreeViewState
#if UNITY_6000_3_OR_NEWER
                <EntityId>
#endif
                TreeViewState;

            public WrapInfo(Delegate originalOnGUI, Func<
#if UNITY_6000_3_OR_NEWER
                EntityId
#else
                int
#endif
                , bool, bool> setExpand,
                object treeViewData,
                PropertyInfo propertyRowCount,
                Func<
#if UNITY_6000_3_OR_NEWER
                    EntityId
#else
                    int
#endif
                    , int
                > getRow,
                PropertyInfo propertyTreeViewRect,
                TreeViewState
#if UNITY_6000_3_OR_NEWER
                    <EntityId>
#endif
                treeViewState
                )
            {
                OriginalOnGUI = originalOnGUI;
                SetExpand = setExpand;

                TreeViewData = treeViewData;
                PropertyRowCount = propertyRowCount;
                GetRow = getRow;
                PropertyTreeViewRect = propertyTreeViewRect;
                TreeViewState = treeViewState;
            }

            public int GetRowCount() => (int)PropertyRowCount.GetValue(TreeViewData);
            public Rect GetTreeViewRect(EditorWindow window) => (Rect)PropertyTreeViewRect.GetValue(window);
        }

        private static void SetupWrap(EditorWindow window)
        {
            if (Wrapped.ContainsKey(window))
            {
                return;
            }

            // Debug.Log($"start wrap {window}");
            WrapInfo result = CreateNewWrap(window);
            if (result.OriginalOnGUI == null)
            {
                Debug.Log($"failed to wrap {window}");
                return;
            }

            // Debug.Log($"done wrap {window}");
            Wrapped[window] = result;
            window.Repaint();
        }

        private static FieldInfo _fieldMTreeView;
        private static FieldInfo _fieldMTreeViewState;
        private static PropertyInfo _propertyMTreeViewData;
        private static MethodInfo _methodSetExpand;
        private static PropertyInfo _propertyRowCount;
        private static MethodInfo _methodGetRow;

        private static FieldInfo _fieldMParent;
        private static MethodInfo _methodCreateDelegate;
        private static Type _hostViewType;
        private static FieldInfo _fieldMOnGUI;

        private static WrapInfo CreateNewWrap(EditorWindow window)
        {
            // UnityEditor.SceneHierarchyWindow.treeViewRect

            object sceneHierarchy = _fieldMSceneHierarchy.GetValue(window);
            if (sceneHierarchy == null)
            {
                return default;
            }

            // UnityEditor.SceneHierarchyWindow.m_SceneHierarchy.m_TreeViewState;
            _fieldMTreeViewState ??= sceneHierarchy.GetType().GetField("m_TreeViewState", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_fieldMTreeViewState == null)
            {
                return default;
            }

            TreeViewState
#if UNITY_6000_3_OR_NEWER
            <EntityId>
#endif

                treeViewState = (TreeViewState
#if UNITY_6000_3_OR_NEWER
                    <EntityId>
#endif
                )_fieldMTreeViewState.GetValue(sceneHierarchy);


                    // UnityEditor.SceneHierarchyWindow.m_SceneHierarchy.m_TreeView;
                    _fieldMTreeView ??= sceneHierarchy.GetType().GetField("m_TreeView", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_fieldMTreeView == null)
            {
                return default;
            }
            object treeViewController = _fieldMTreeView.GetValue(sceneHierarchy);

            // UnityEditor.SceneHierarchyWindow.m_SceneHierarchy.m_TreeView.data;
            // Debug.Log(treeViewController.GetType());
            _propertyMTreeViewData ??= treeViewController.GetType().GetProperty("data",  BindingFlags.Public | BindingFlags.Instance);
            if (_propertyMTreeViewData == null)
            {
                return default;
            }
            object treeViewData = _propertyMTreeViewData.GetValue(treeViewController);

            Type itemType =
#if UNITY_6000_3_OR_NEWER
                    typeof(EntityId)
#else
                    typeof(int)
#endif
                ;


            // UnityEditor.SceneHierarchyWindow.m_SceneHierarchy.m_TreeView.data.SetExpanded();
            // UnityEditor.IMGUI.Controls.TreeViewDataSource<EntityId>.SetExpanded()
            // UnityEditor.GameObjectTreeViewDataSource
            // Debug.Log(treeViewData.GetType());
            if(_methodSetExpand == null)
            {
                (Type foundType, MethodInfo methodInfo) setExpandedResult = RecGetMethodInfo(treeViewData.GetType(),
                    "SetExpanded",
                    BindingFlags.Public | BindingFlags.Instance, new[] { itemType, typeof(bool) });
                _methodSetExpand = setExpandedResult.methodInfo;
            }
            // Debug.Log(_methodSetExpand);

            Func<
#if UNITY_6000_3_OR_NEWER
                EntityId
#else
                int
#endif
                , bool, bool> setExpand = (Func<
#if UNITY_6000_3_OR_NEWER
                EntityId
#else
                int
#endif
                , bool, bool>)_methodSetExpand.CreateDelegate(typeof(Func<
#if UNITY_6000_3_OR_NEWER
                EntityId
#else
                int
#endif
                , bool, bool>), treeViewData);

            // Debug.Log(treeViewData);


            // treeViewController = sceneHierarchy.GetType().GetFieldValue("m_TreeView");
            // treeViewControllerData = treeViewController.GetMemberValue("data");

            // UnityEditor.SceneHierarchyWindow.m_SceneHierarchy.m_TreeView.data.rowCount;
            // UnityEditor.GameObjectTreeViewDataSource
            if (_propertyRowCount == null)
            {
                _propertyRowCount = treeViewData.GetType()
                    .GetProperty("rowCount", BindingFlags.Public | BindingFlags.Instance);
            }

            if (_propertyRowCount == null)
            {
                return default;
            }

            _methodGetRow ??= treeViewData.GetType()
                    .GetMethod("GetRow", BindingFlags.Public | BindingFlags.Instance, null, new[]{itemType}, null);

            if (_methodGetRow == null)
            {
                return default;
            }

            Func<
#if UNITY_6000_3_OR_NEWER
                EntityId
#else
                int
#endif
                , int
            > getRow = (Func<
#if UNITY_6000_3_OR_NEWER
                EntityId
#else
                int
#endif
                , int
            >)_methodGetRow.CreateDelegate(typeof(Func<
#if UNITY_6000_3_OR_NEWER
                EntityId
#else
                int
#endif
                , int
            >), treeViewData);

            // Func<int> getRowCount = () => (int)_propertyRowCount.GetValue(treeViewData);

            object hostViewParent = _fieldMParent.GetValue(window);
            // EditorWindow.m_Parent;
            // UnityEditor.DockArea;
            // Debug.Log(hostViewParent.GetType());
            if (_methodCreateDelegate == null)
            {
                (_hostViewType, _methodCreateDelegate) = RecGetMethodInfo(hostViewParent.GetType(), "CreateDelegate",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    new[] { typeof(string) });
            }

            if (_methodCreateDelegate == null)
            {
                return default;
            }
            // Debug.Assert(_methodCreateDelegate != null, "No longer works in this version of Unity");

            Delegate onGuiDelegate = (Delegate)_methodCreateDelegate.Invoke(hostViewParent, new object[] { "OnGUI" });

            // Type hostViewType = hostViewParent.GetType();
            // Debug.Log(_hostViewType.FullName);
            // UnityEditor.HostView.EditorWindowDelegate;
            Type hostViewEditorWindowDelegateType = _hostViewType.GetNestedType(
                "EditorWindowDelegate",
                BindingFlags.NonPublic
            );
            // Debug.Log(hostViewEditorWindowDelegateType);

            MethodInfo methodOnGUIWrapper = typeof(SaintsHierarchyWindow)
                .GetMethod(nameof(OnGUIWrapper), BindingFlags.NonPublic | BindingFlags.Static);

            Debug.Assert(methodOnGUIWrapper != null);
            Delegate wrappedDelegate = methodOnGUIWrapper.CreateDelegate(hostViewEditorWindowDelegateType, window);

            // UnityEditor.SceneHierarchyWindow

            _fieldMOnGUI ??= _hostViewType.GetField("m_OnGUI", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_fieldMOnGUI == null)
            {
                Debug.Log("m_OnGUI is null");
                return default;
            }

            _fieldMOnGUI.SetValue(hostViewParent, wrappedDelegate);

            // SceneHierarchyWindow.m_Parent;
            // OriginDelegate[window] = wrappedDelegate;
            // window.Repaint();

            return new WrapInfo(onGuiDelegate, setExpand, treeViewData, _propertyRowCount, getRow, _propertyTreeViewRect, treeViewState);
        }

        private static FieldInfo _fieldMPos;

        private static void OnGUIWrapper(EditorWindow window)
        {

            // Debug.Log("called");
            if (!Wrapped.TryGetValue(window, out WrapInfo wrapInfo))
            {
                throw new Exception("This version of Unity is not supported");
            }

            Delegate originalOnGUI = wrapInfo.OriginalOnGUI;

            bool personalDisabled = !PersonalHierarchyConfig.instance.personalEnabled;
            if (personalDisabled
                    ? SaintsHierarchyConfig.instance.disabled
                    : PersonalHierarchyConfig.instance.disabled)
            {
                originalOnGUI.DynamicInvoke();
                return;
            }

            if (_sceneHierarchyWindowType == null)
            {
                return;
            }

            // GUILayout.Button("OK", GUILayout.Height(80));

            float windowWidth = window.position.width;
            const float toolHeight = 40;

            Rect originMPos = (Rect)_fieldMPos.GetValue(window);
            Rect offsetMPos = new Rect(originMPos)
            {
                y = originMPos.y + toolHeight,
                height = originMPos.height - toolHeight,
            };
            using(new GUI.GroupScope(new Rect(originMPos)
                  {
                      x = 0,
                      y = toolHeight,
                      height = originMPos.height - toolHeight,
                  }))
            {
                _fieldMPos.SetValue(window, offsetMPos);  // need this for scroll

                originalOnGUI.DynamicInvoke();

                _fieldMPos.SetValue(window, originMPos);
            }

            Rect toolbarRect = new Rect(0, 0, windowWidth, toolHeight);

            Event evt = Event.current;
            (bool hasAny, IEnumerable<GameObject> allGo) = ContainAnyAndFull(CanDropGos(evt, toolbarRect));
            if (hasAny)
            {
                if (evt.type == EventType.DragPerform)
                {
                    foreach (GameObject favGo in allGo)
                    {
                        // Debug.Log(favGo);
                        AddToConfig(favGo);
                    }
                    DragAndDrop.AcceptDrag();
                }
                evt.Use();
            }


            IConfig config = SaintsHierarchyConfig.instance;
            if (config.sceneGuidToGoFavoritesList.Count > 0)
            {
                List<GameObjectFavorite> favorites = config.sceneGuidToGoFavoritesList[0].favorites;
                if (favorites.Count > 0)
                {
                    if (GUI.Button(toolbarRect, favorites[0].globalObjectIdString))
                    {
                        GlobalObjectId.TryParse(favorites[0].globalObjectIdString, out GlobalObjectId r);
                        GameObject g = (GameObject)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(r);
                        if(g != null)
                        {
                            ExpandInTree(g, wrapInfo, window, 20);
                        }
                    }
                }

            }

        }

        private static void ExpandInTree(GameObject gameObject, WrapInfo wrapInfo, EditorWindow window, float margin)
        {
            Transform parent = gameObject.transform.parent;
            while (parent != null)
            {
                wrapInfo.SetExpand(
                    parent.gameObject.
#if UNITY_6000_3_OR_NEWER
                        GetEntityId()
#else
                        GetInstanceID()
#endif
                    , true
                );

                parent = parent.parent;
            }

            int rowCount = wrapInfo.GetRowCount();
            float maxScrollPos = rowCount * 16 - window.position.height + 26.9f;

            int rowIndex = wrapInfo.GetRow(gameObject.
#if UNITY_6000_3_OR_NEWER
                    GetEntityId()
#else
                    GetInstanceID()
#endif
                );

            float rowPos = rowIndex * 16f + 8;
            // float scrollAreaHeight = wrapInfo.GetTreeViewRect(window).height;

            float targetScrollPos = Mathf.Clamp(rowPos - margin, 0, maxScrollPos);

            if (targetScrollPos < 25)
            {
                targetScrollPos = 0;
            }

            wrapInfo.TreeViewState.scrollPos = Vector2.up * targetScrollPos;
            Selection.activeGameObject = gameObject;
            // window.GetMemberValue("m_SceneHierarchy").GetMemberValue<TreeViewState>("m_TreeViewState").scrollPos = Vector2.up * targetScrollPos;
        }

        private static IEnumerable<T> AsEnumerablePrepend<T>(T first, IEnumerator<T> enumerator)
        {
            yield return first;
            while (enumerator.MoveNext()) {
                yield return enumerator.Current;
            }
        }

        private static (bool, IEnumerable<T>) ContainAnyAndFull<T>(IEnumerable<T> iter)
        {
            IEnumerator<T> enumerator = iter.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return (false, Array.Empty<T>());
            }
            T first = enumerator.Current;
            // ReSharper disable once ConvertIfStatementToReturnStatement
            // if (first == null)
            // {
            //     return (false, Array.Empty<T>());
            // }

            return (true, AsEnumerablePrepend(first, enumerator));
        }

        private static IEnumerable<GameObject> CanDropGos(Event evt, Rect toolbarRect)
        {
            // Event evt = Event.current;

            if (evt.type is not (EventType.DragUpdated or EventType.DragPerform))
            {
                yield break;
            }

            if (!toolbarRect.Contains(evt.mousePosition))
            {
                yield break;
            }

            bool any = false;

            foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
            {
                // ReSharper disable once InvertIf
                if (draggedObject is GameObject go && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)))
                {
                    any = true;
                    // evt.Use();
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    yield return go;
                    // Debug.Log("Dropped: " + go.name);
                    // Debug.Log(AssetDatabase.GetAssetPath(go));
                }
            }

            if (!any)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }

            // if (evt.type == EventType.DragPerform)
            // {
            //     // Necessary to officially "accept" the data
            //     DragAndDrop.AcceptDrag();
            //
            //     // Access the dropped objects
            //
            // }
        }


        private static void AddToConfig(GameObject go)
        {
            // GameObject targetGo = go;
            // string curScenePath = go.scene.path;
            // Debug.Log(curScenePath);
            // if (!string.IsNullOrEmpty(curScenePath) && curScenePath.EndsWith(".prefab"))
            // {
            //     targetGo = Util.GetPrefabSubGameObject(curScenePath, go);
            //     if (targetGo == null)
            //     {
            //         return;
            //     }
            // }
            string scenePath = go.scene.path;
            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);

            GlobalObjectId targetId = GlobalObjectId.GetGlobalObjectIdSlow(go);
            // string targetGoIdStr = Util.GlobalObjectIdNormString(targetId);
            string targetGoIdStr = targetId.ToString();
            IConfig config = SaintsHierarchyConfig.instance;
            List<GameObjectFavorite> favorites = null;
            foreach (SceneGuidToGoFavorites sceneGuidToGoFavorites in config.sceneGuidToGoFavoritesList)
            {
                if (sceneGuidToGoFavorites.sceneGuid == sceneGuid)
                {
                    favorites = sceneGuidToGoFavorites.favorites;
                    foreach (GameObjectFavorite gameObjectFavorite in favorites)
                    {
                        if (gameObjectFavorite.globalObjectIdString == targetGoIdStr)
                        {
                            Debug.Log($"exists, skip {targetGoIdStr}");
                            return;
                        }
                    }
                }
            }

            GameObjectFavorite item = new GameObjectFavorite
            {
                globalObjectIdString = targetGoIdStr,
                alias = string.Empty,
                color = default,
                hasColor = false,
                icon = string.Empty,
            };
            if (favorites != null)
            {
                Debug.Log($"add {targetGoIdStr} in {sceneGuid}");
                favorites.Add(item);
            }
            else
            {
                Debug.Log($"add {targetGoIdStr} created {sceneGuid}");
                config.sceneGuidToGoFavoritesList.Add(new SceneGuidToGoFavorites
                {
                    sceneGuid = sceneGuid,
                    favorites = new List<GameObjectFavorite>
                    {
                        item,
                    },
                });
            }

            config.SaveToDisk();
        }

        private static (Type foundType, MethodInfo methodInfo) RecGetMethodInfo(Type type, string name, BindingFlags flags, Type[] types)
        {
            while (type != null)
            {
                // Debug.Log(type);
                MethodInfo method = type.GetMethod(
                    name,
                    flags,
                    null,
                    types,
                    null
                );

                if (method != null)
                {
                    return (type, method);
                    break;
                }

                type = type.BaseType;
            }
            return (null, null);
        }
    }
}
