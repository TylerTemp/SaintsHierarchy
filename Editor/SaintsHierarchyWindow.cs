using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaintsHierarchy.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Scene = UnityEngine.SceneManagement.Scene;

namespace SaintsHierarchy.Editor
{
    public class SaintsHierarchyWindow
    {
        private static Type _sceneHierarchyWindowType;
        private static FieldInfo _sLastInteractedHierarchy;
        private static FieldInfo _fieldMSceneHierarchy;
        // private static PropertyInfo _propertyTreeViewRect;

        [InitializeOnLoadMethod]
        public static void OnLoad()
        {
            if (GetUsingConfig().disableFavorites)
            {
                return;
            }

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

            // _propertyTreeViewRect ??= _sceneHierarchyWindowType.GetProperty("treeViewRect", BindingFlags.NonPublic | BindingFlags.Instance);
            // if (_propertyTreeViewRect == null)
            // {
            //     return;
            // }

            _fieldMPos ??= typeof(EditorWindow).GetField("m_Pos", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_fieldMPos == null)
            {
                return;
            }

            EditorApplication.delayCall += CheckWindowAll;
            EditorWindow.windowFocusChanged -= CheckWindowFocused;
            EditorWindow.windowFocusChanged += CheckWindowFocused;

            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorSceneManager.newSceneCreated -= OnNewSceneCreated;
            EditorSceneManager.newSceneCreated += OnNewSceneCreated;
            EditorApplication.hierarchyChanged -= ReloadAllScene;
            EditorApplication.hierarchyChanged += ReloadAllScene;
            OnSceneCheck();
        }

        private static void ReloadAllScene()
        {
            LoadedScenes.Clear();
            OnSceneCheck();
        }

        private static readonly HashSet<Scene> LoadedScenes = new HashSet<Scene>();

        private static void OnSceneCheck()
        {
            int count = SceneManager.sceneCount;
            HashSet<Scene> leftOutScenes = new HashSet<Scene>(LoadedScenes);

            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (LoadedScenes.Add(scene))
                {
                    leftOutScenes.Remove(scene);
                    ReloadSceneFav(scene);
                }
                // Debug.Log(
                //     $"[{i}] " +
                //     $"name={scene.name}, " +
                //     $"path={scene.path}, " +
                //     $"loaded={scene.isLoaded}, " +
                //     $"dirty={scene.isDirty}"
                // );
            }

            foreach (Scene leftOutScene in leftOutScenes)
            {
                RemoveSceneFav(leftOutScene);
            }
        }

        private static IConfig GetUsingConfig()
        {
            // return SaintsHierarchyConfig.instance.en;
            return PersonalHierarchyConfig.instance.personalEnabled? PersonalHierarchyConfig.instance: SaintsHierarchyConfig.instance;
        }

        private readonly struct RuntimeFavoriteGameObject
        {
            public readonly GameObjectFavorite FavoriteConfig;

            // public string SceneGuid;
            public readonly GameObject LoadedGameObject;
            // public readonly RuntimeFavoriteStatus Status;

            public RuntimeFavoriteGameObject(GameObject runtimeGo, GameObjectFavorite config)
            {
                LoadedGameObject = runtimeGo;
                FavoriteConfig = config;
                // Status = RuntimeFavoriteStatus.Default;
            }
        }

        private static readonly List<RuntimeFavoriteGameObject> CurrentFavoriteGameObjects = new List<RuntimeFavoriteGameObject>();

        private static void ReloadSceneFav(Scene scene) => ReloadSceneFav(scene.path);
        private static void ReloadSceneFav(string scenePath)
        {
            GUID guid = AssetDatabase.GUIDFromAssetPath(scenePath);
            string guidStr = guid.ToString();
            CurrentFavoriteGameObjects.RemoveAll(each => each.FavoriteConfig.sceneGuid == guidStr);

            IConfig config = Util.GetFavoriteConfig();
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_RENDER_FAV
            Debug.Log($"scene fav count {config.favorites.Count}");
#endif
            foreach (GameObjectFavorite sceneGuidToGoFavorites in config.favorites)
            {
                // Debug.Log($"checking {sceneGuidToGoFavorites.sceneGuid}->{guidStr}");
                if (sceneGuidToGoFavorites.sceneGuid == guidStr)
                {
                    // List<RuntimeFavoriteGameObject> fav = new List<RuntimeFavoriteGameObject>();
                    string gameIdStr = sceneGuidToGoFavorites.globalObjectIdString;
                    // Debug.Log($"parsing {gameIdStr}");
                    if (GlobalObjectId.TryParse(gameIdStr, out GlobalObjectId id))
                    {
                        GameObject go = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                        // Debug.Log($"get {go}");
                        if (go != null)
                        {
                            // Debug.Log($"add {go}");
                            CurrentFavoriteGameObjects.Add(new RuntimeFavoriteGameObject(go, sceneGuidToGoFavorites));
                        }
                    }
                    // else
                    // {
                    //     Debug.Log($"parsing failed");
                    // }

                    // return;
                    // CurrentFavoriteGameObjects.RemoveAll(static each => each.SceneGuid == sceneGuidToGoFavorites.sceneGuid);
                }
            }
        }

        private static void RemoveSceneFav(Scene scene)
        {
            string scenePath = scene.path;
            GUID guid = AssetDatabase.GUIDFromAssetPath(scenePath);
            string guidStr = guid.ToString();
            CurrentFavoriteGameObjects.RemoveAll(each => each.FavoriteConfig.sceneGuid == guidStr);
        }

        private static void OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            ReloadSceneFav(scene);
            // Debug.Log($"created {scene.name}");
        }

        // private static void OnSceneClosed(Scene scene)
        // {
        //     Debug.Log($"closed {scene.name}");
        // }

        private static void OnSceneClosing(Scene scene, bool removingScene)
        {
            RemoveSceneFav(scene);
            // Debug.Log($"closing {scene.name}");
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ReloadSceneFav(scene);
            // Debug.Log($"opened {scene.name}");
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

            private readonly object _treeViewData;
            private readonly PropertyInfo _propRowCount;
            // private readonly PropertyInfo _propTreeViewRect;

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
                PropertyInfo propRowCount,
                Func<
#if UNITY_6000_3_OR_NEWER
                    EntityId
#else
                    int
#endif
                    , int
                > getRow,
                TreeViewState
#if UNITY_6000_3_OR_NEWER
                    <EntityId>
#endif
                treeViewState
                )
            {
                OriginalOnGUI = originalOnGUI;
                SetExpand = setExpand;

                _treeViewData = treeViewData;
                _propRowCount = propRowCount;
                GetRow = getRow;
                // _propTreeViewRect = propTreeViewRect;
                TreeViewState = treeViewState;
            }

            public int GetRowCount() => (int)_propRowCount.GetValue(_treeViewData);
            // public Rect GetTreeViewRect(EditorWindow window) => (Rect)_propTreeViewRect.GetValue(window);
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

            return new WrapInfo(onGuiDelegate, setExpand, treeViewData, _propertyRowCount, getRow, treeViewState);
        }

        private static FieldInfo _fieldMPos;

        private enum RuntimeFavoriteStatus
        {
            Default,
            DragExisted,
            DragNew,
        }

        private class FavoriteDrawingInfo
        {
            public readonly RuntimeFavoriteGameObject RuntimeConfig;
            public readonly RuntimeFavoriteStatus Status;

            public float OriginalX;
            public float OriginalY;
            public readonly float Width;
            // public readonly float Height;

            public readonly string Text;
            public readonly Texture2D Icon;

            public static string HelperGetDisplayText(RuntimeFavoriteGameObject config) => config.LoadedGameObject.name;
            // public string GetDisplayText() => RuntimeConfig.LoadedGameObject.name;
            private static Texture2D _defaultIcon;

            public static Texture2D HelperGetDisplayIcon(RuntimeFavoriteGameObject config) =>
                HelperGetDisplayIcon(config.LoadedGameObject);
            public static Texture2D HelperGetDisplayIcon(GameObject go)
            {
                return EditorGUIUtility.GetIconForObject(go) ?? EditorGUIUtility.IconContent("d_GameObject Icon").image as Texture2D;
            }
            // public Texture2D GetDisplayIcon() => EditorGUIUtility.GetIconForObject(RuntimeConfig.LoadedGameObject);

            public FavoriteDrawingInfo(RuntimeFavoriteGameObject runtimeConfig,
                RuntimeFavoriteStatus status,
                string text,
                Texture2D icon,
                float width
            )
            {
                RuntimeConfig = runtimeConfig;
                Status = status;
                Width = width;

                Text = text;
                Icon = icon;
            }
        }

        // private static bool _inDrag;

        private class EditorWindowStatus
        {
            // public bool InDrag;
            public readonly List<GameObject> Dragging = new List<GameObject>();

            // public bool selfDragging = false;
            // public Vector2 selfDraggingStart;

            public GameObject PrepareDragGo;
            public bool IsDraggingGo;
        }

        private static readonly Dictionary<EditorWindow,EditorWindowStatus> EditorWindowStatuses = new Dictionary<EditorWindow,EditorWindowStatus>();

        private static void OnGUIWrapper(EditorWindow window)
        {
            // Debug.Log("called");
            if (!Wrapped.TryGetValue(window, out WrapInfo wrapInfo))
            {
                throw new Exception("This version of Unity is not supported");
            }

            if (!EditorWindowStatuses.TryGetValue(window, out EditorWindowStatus windowStatus))
            {
                EditorWindowStatuses[window] = windowStatus = new EditorWindowStatus();
            }

            Event eventCurrent = Event.current;
            EventType eventType = eventCurrent.type;

            // if (eventType is EventType.DragExited or EventType.DragPerform)
            // {
            //     windowStatus.InDrag = false;
            //     windowStatus.Dragging.Clear();
            // }

            Delegate originalOnGUI = wrapInfo.OriginalOnGUI;

            IConfig config = Util.GetUsingConfig();
            // bool personalDisabled = !PersonalHierarchyConfig.instance.personalEnabled;
            if (config.disabled || config.disableFavorites)
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
            // const float gap = 2;
            float rowHeight = EditorGUIUtility.singleLineHeight + 2;

            // List<FavoriteDrawingInfo> favoriteDrawingInfos = new List<FavoriteDrawingInfo>();
            List<FavoriteDrawingInfo> existsDrawingInfos = new List<FavoriteDrawingInfo>();
            HashSet<GameObject> existedDragging = new HashSet<GameObject>();
            foreach (RuntimeFavoriteGameObject runtimeFavoriteGameObject in CurrentFavoriteGameObjects)
            {
                string text = FavoriteDrawingInfo.HelperGetDisplayText(runtimeFavoriteGameObject);
                Texture2D icon = FavoriteDrawingInfo.HelperGetDisplayIcon(runtimeFavoriteGameObject);

                // float textWidth = EditorStyles.label.CalcSize(new GUIContent(text)).x;
                // float textWidth = GUI.skin.button.CalcSize(new GUIContent(text, icon)).x;
                float textWidth = GUI.skin.button.CalcSize(new GUIContent(text)).x;
                float iconWidth = EditorGUIUtility.singleLineHeight;
                float totalWidth = textWidth + iconWidth + 6;
                // float totalWidth = new GUIStyle("Button").CalcSize(new GUIContent(text, )) + iconWidth + gap * 2;

                FavoriteDrawingInfo info = new FavoriteDrawingInfo(runtimeFavoriteGameObject, RuntimeFavoriteStatus.Default, text,
                    icon, totalWidth);

                if (windowStatus.Dragging.Contains(runtimeFavoriteGameObject.LoadedGameObject))
                {
                    // info.Status = RuntimeFavoriteStatus.DragExisted;
                    existedDragging.Add(runtimeFavoriteGameObject.LoadedGameObject);
                    continue;
                }
                existsDrawingInfos.Add(info);
            }

            // add dragging to display it properly
            List<FavoriteDrawingInfo> draggingDrawingInfos = new List<FavoriteDrawingInfo>();
            foreach (GameObject dragging in windowStatus.Dragging)
            {
                string text = dragging.name;
                Texture2D icon = FavoriteDrawingInfo.HelperGetDisplayIcon(dragging);

                float textWidth = GUI.skin.button.CalcSize(new GUIContent(text)).x;
                float iconWidth = EditorGUIUtility.singleLineHeight;
                float totalWidth = textWidth + iconWidth + 6;
                // float totalWidth = new GUIStyle("Button").CalcSize(new GUIContent(text, )) + iconWidth + gap * 2;

                string scenePath = dragging.scene.path;
                string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);

                FavoriteDrawingInfo info = new FavoriteDrawingInfo(
                    new RuntimeFavoriteGameObject(
                    dragging,
                    new GameObjectFavorite
                    {
                        globalObjectIdString = GlobalObjectId.GetGlobalObjectIdSlow(dragging).ToString(),
                        sceneGuid = sceneGuid,
                    }),
                    existedDragging.Contains(dragging)? RuntimeFavoriteStatus.DragExisted: RuntimeFavoriteStatus.DragNew,
                    text,
                    icon, totalWidth);
                draggingDrawingInfos.Add(info);
            }

            // Calc
            float toolHeight = CalcRelativePos(existsDrawingInfos.Concat(draggingDrawingInfos), rowHeight, windowWidth);
            Rect toolbarRect = new Rect(0, 0, windowWidth, toolHeight);

            // re-sort
            List<FavoriteDrawingInfo> favoriteDrawingInfos;
            if (windowStatus.Dragging.Count > 0)
            {
                Vector2 mousePos = eventCurrent.mousePosition;
                favoriteDrawingInfos = new List<FavoriteDrawingInfo>(existsDrawingInfos.Count + draggingDrawingInfos.Count);
                bool inserted = false;
                foreach (FavoriteDrawingInfo favoriteDrawingInfo in existsDrawingInfos)
                {
                    Rect useRect = new Rect(favoriteDrawingInfo.OriginalX + toolbarRect.x,
                        favoriteDrawingInfo.OriginalY + toolbarRect.y, favoriteDrawingInfo.Width, rowHeight);
                    if (!inserted && useRect.Contains(mousePos))
                    {
                        bool isPre = Mathf.InverseLerp(useRect.x, useRect.xMax, mousePos.x) < 0.4f;
                        if (isPre)
                        {
                            favoriteDrawingInfos.AddRange(draggingDrawingInfos);
                            favoriteDrawingInfos.Add(favoriteDrawingInfo);
                        }
                        else
                        {
                            favoriteDrawingInfos.Add(favoriteDrawingInfo);
                            favoriteDrawingInfos.AddRange(draggingDrawingInfos);
                        }
                        inserted = true;
                    }
                    else
                    {
                        favoriteDrawingInfos.Add(favoriteDrawingInfo);
                    }
                }

                if (inserted)
                {
                    CalcRelativePos(favoriteDrawingInfos, rowHeight, windowWidth);
                }
                else
                {
                    favoriteDrawingInfos.AddRange(draggingDrawingInfos);
                }
            }
            else
            {
                favoriteDrawingInfos = existsDrawingInfos;
            }

            // Event evt = Event.current;
            if (!toolbarRect.Contains(eventCurrent.mousePosition))
            {
                // windowStatus.InDrag = false;
                windowStatus.Dragging.Clear();
            }

            CanDropGos(eventCurrent, eventType, toolbarRect, windowStatus);
            // Debug.Log(hasDrop);
            if (eventType == EventType.DragPerform && windowStatus.Dragging.Count > 0)
            {
                ApplyListToConfig(favoriteDrawingInfos);
                // foreach (GameObject favGo in windowStatus.Dragging)
                // {
                //     // Debug.Log(favGo);
                //     AddToConfig(favGo);
                // }
                DragAndDrop.AcceptDrag();
            }

            // bool repaint = false;
            // Rect repaintRect = default;

            foreach (FavoriteDrawingInfo favoriteDrawingInfo in favoriteDrawingInfos)
            {
                Rect useRect = new Rect(favoriteDrawingInfo.OriginalX + toolbarRect.x,
                    favoriteDrawingInfo.OriginalY + toolbarRect.y, favoriteDrawingInfo.Width, rowHeight);

                Rect drawRect = new Rect(useRect.x + 1, useRect.y + 1, useRect.width - 2, useRect.height - 2);
                GUIContent content = new GUIContent(favoriteDrawingInfo.Text, favoriteDrawingInfo.Icon);
                using (new GUIBackgroundColorScoopWithStatus(favoriteDrawingInfo.Status))
                {
                    GUI.Box(drawRect, content, GUI.skin.button);
                }
                bool btnClicked = false;

                switch (eventType)
                {
                    case EventType.MouseDown:
                        windowStatus.IsDraggingGo = false;
                        if (eventCurrent.button == 0
                            && useRect.Contains(eventCurrent.mousePosition))
                        {
                            windowStatus.PrepareDragGo = favoriteDrawingInfo.RuntimeConfig.LoadedGameObject;
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_DRAG
                            Debug.Log($"PrepareDragGo: {favoriteDrawingInfo.RuntimeConfig.LoadedGameObject}");
#endif

                            // windowStatus.selfDraggingStart = evt.mousePosition;

                            // Start drag operation
                            DragAndDrop.PrepareStartDrag();
                            // DragAndDrop.SetGenericData("MyDragData", "Drag");

                            // Debug.Log("prepare");
                            // EditorGUI.DrawRect(useRect, Color.red);
                            eventCurrent.Use();
                        }
                        break;

                    case EventType.MouseDrag:
                        if (useRect.Contains(eventCurrent.mousePosition)
                            && !windowStatus.IsDraggingGo
                            && windowStatus.PrepareDragGo == favoriteDrawingInfo.RuntimeConfig.LoadedGameObject)
                        {
                            DragAndDrop.objectReferences = new[] { (Object)favoriteDrawingInfo.RuntimeConfig.LoadedGameObject };
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_DRAG
                            Debug.Log($"DragGo: {favoriteDrawingInfo.RuntimeConfig.LoadedGameObject}");
#endif
                            DragAndDrop.StartDrag("Dragging Button");
                            windowStatus.IsDraggingGo = true;
                            // Debug.Log("start to drag");
                            // EditorGUI.DrawRect(useRect, Color.red);
                            eventCurrent.Use();
                        }
                        break;

                    case EventType.MouseUp:
                    {
                        // Debug.Log($"{windowStatus.IsDraggingGo}/{windowStatus.PrepareDragGo}=={favoriteDrawingInfo.RuntimeConfig.LoadedGameObject}/{useRect.Contains(eventCurrent.mousePosition)}/{eventCurrent.button}==0");
                        if (eventCurrent.button == 0
                            && useRect.Contains(eventCurrent.mousePosition)
                            && windowStatus.PrepareDragGo == favoriteDrawingInfo.RuntimeConfig.LoadedGameObject)
                        {
                            if (!windowStatus.IsDraggingGo)
                            {
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_DRAG
                                Debug.Log($"DragGoClick: {favoriteDrawingInfo.RuntimeConfig.LoadedGameObject}");
#endif
                                btnClicked = true;
                                eventCurrent.Use();
                            }
                            else
                            {
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_DRAG
                                Debug.Log($"DragGoEnd: {favoriteDrawingInfo.RuntimeConfig.LoadedGameObject}");
#endif
                            }

                            windowStatus.IsDraggingGo = false;
                            windowStatus.PrepareDragGo = null;
                            DragAndDrop.objectReferences = Array.Empty<Object>();
                        }

                        break;
                    }
                }

                bool contextClicked = eventType == EventType.MouseUp
                                    && eventCurrent.button == 1
                                    && useRect.Contains(eventCurrent.mousePosition);

                if (btnClicked)
                {
                    if (eventCurrent.alt)
                    {
                        contextClicked = true;
                    }
                    else
                    {
                        ExpandInTree(favoriteDrawingInfo.RuntimeConfig.LoadedGameObject, wrapInfo, window, 20);
                    }
                }

                if (contextClicked)
                {
                    PopConfigWindow(
                        new Rect(eventCurrent.mousePosition.x, eventCurrent.mousePosition.y, 0, 0),
                        favoriteDrawingInfo.RuntimeConfig.FavoriteConfig,
                        window);
                }
            }


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

            if (windowStatus.Dragging.Count > 0)
            {
                Vector2 mousePos = eventCurrent.mousePosition;
                GUIContent content = new GUIContent(string.Join("\n", windowStatus.Dragging.Select(each => each.name)));
                Vector2 size = GUI.skin.label.CalcSize(content);
                Rect rect = new Rect(
                    mousePos.x + 10,
                    mousePos.y + 10,
                    size.x,
                    size.y
                );

                GUI.Label(rect, content);
                if (eventType is EventType.MouseMove or EventType.DragUpdated)
                {
                    window.Repaint();
                }
            }


            if (eventType is EventType.DragExited or EventType.DragPerform)
            {
                // windowStatus.InDrag = false;
                windowStatus.Dragging.Clear();
            }
        }

        private static void PopConfigWindow(Rect worldBound, GameObjectFavorite favoriteConfig, EditorWindow window)
        {
            FavoriteConfigPopup pop = new FavoriteConfigPopup(favoriteConfig);
            pop.DeletedEvent.AddListener(target =>
            {
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_RENDER_FAV
                Debug.Log($"fav deleted {target.globalObjectIdString}");
#endif
                ReloadAllScene();
                window.Repaint();
            });
            PopupWindow.Show(worldBound, pop);
        }

        private static void ApplyListToConfig(IReadOnlyList<FavoriteDrawingInfo> favoriteDrawingInfos)
        {
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_APPLY_FAV
            foreach (FavoriteDrawingInfo favoriteDrawingInfo in favoriteDrawingInfos)
            {
                Debug.Log($"{favoriteDrawingInfo.Status}/{favoriteDrawingInfo.RuntimeConfig.LoadedGameObject.name}");
            }
#endif

            IConfig config = Util.GetFavoriteConfig();
            bool reload = false;

            // deal move
            {
                List<GameObjectFavorite> beforeDragSavedConfigsReversed = new List<GameObjectFavorite>();
                List<GameObjectFavorite> dragSavedConfigs = new List<GameObjectFavorite>();
                foreach (FavoriteDrawingInfo favoriteDrawingInfo in favoriteDrawingInfos)
                {
                    GameObjectFavorite savedConf = favoriteDrawingInfo.RuntimeConfig.FavoriteConfig;
                    if (favoriteDrawingInfo.Status == RuntimeFavoriteStatus.DragExisted)
                    {
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_APPLY_FAV
                        Debug.Log($"found now drag item {savedConf.globalObjectIdString}");
#endif
                        dragSavedConfigs.Add(savedConf);
                    }
                    else
                    {
                        if (dragSavedConfigs.Count == 0)
                        {
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_APPLY_FAV
                            Debug.Log($"found before drag item {savedConf.globalObjectIdString}");
#endif
                            beforeDragSavedConfigsReversed.Insert(0, savedConf);
                        }
                    }
                }

                if (dragSavedConfigs.Count > 0)
                {
                    HashSet<string> dragSavedConfigIds =
                        dragSavedConfigs.Select(each => each.globalObjectIdString).ToHashSet();
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_APPLY_FAV
                    Debug.Log($"dragSavedConfigIds: {string.Join(",", dragSavedConfigIds)}");
#endif
                    config.favorites.RemoveAll(each => dragSavedConfigIds.Contains(each.globalObjectIdString));
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_APPLY_FAV
                    Debug.Log($"removed now: {string.Join(",", config.favorites.Select(each => each.globalObjectIdString))}");
#endif

                    bool hasBeforeItems = beforeDragSavedConfigsReversed.Count > 0;

                    int dragToIndex;
                    if (hasBeforeItems)
                    {
                        dragToIndex = beforeDragSavedConfigsReversed.Count;
                        foreach (GameObjectFavorite savedConfig in beforeDragSavedConfigsReversed)
                        {
                            int foundIndex = config.favorites.IndexOf(savedConfig);
                            if (foundIndex >= 0)
                            {
                                dragToIndex = foundIndex + 1;
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_APPLY_FAV
                                Debug.Log($"shift index to {dragToIndex}");
#endif
                                break;
                            }
                        }
                    }
                    else
                    {
                        dragToIndex = 0;
                    }

                    dragSavedConfigs.Reverse();
                    foreach (GameObjectFavorite drag in dragSavedConfigs)
                    {
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_APPLY_FAV
                        Debug.Log($"insert index to {dragToIndex}: {drag.globalObjectIdString}");
#endif
                        config.favorites.Insert(dragToIndex, drag);
                    }

                    reload = true;
                }
            }

            // deal insert
            List<GameObjectFavorite> beforeDragNewConfigsReversed = new List<GameObjectFavorite>();
            List<GameObjectFavorite> dragNewConfigs = new List<GameObjectFavorite>();
            foreach (FavoriteDrawingInfo favoriteDrawingInfo in favoriteDrawingInfos)
            {
                GameObjectFavorite savedConf = favoriteDrawingInfo.RuntimeConfig.FavoriteConfig;
                if (favoriteDrawingInfo.Status == RuntimeFavoriteStatus.DragNew)
                {
                    dragNewConfigs.Add(savedConf);
                }
                else
                {
                    if(dragNewConfigs.Count == 0)
                    {
                        beforeDragNewConfigsReversed.Insert(0, savedConf);
                    }
                }
            }
            if (dragNewConfigs.Count > 0)
            {
                int dragToIndex = beforeDragNewConfigsReversed.Count == 0? 0: config.favorites.Count;
                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (GameObjectFavorite savedConfig in beforeDragNewConfigsReversed)
                {
                    int foundIndex = config.favorites.IndexOf(savedConfig);
                    // ReSharper disable once InvertIf
                    if (foundIndex >= 0)
                    {
                        dragToIndex = foundIndex + 1;
                        break;
                    }
                }

                foreach (GameObjectFavorite drag in dragNewConfigs)
                {
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_APPLY_FAV
                    Debug.Log($"insert @{dragToIndex}: {drag.globalObjectIdString}");
#endif
                    config.favorites.Insert(dragToIndex, drag);
                }

                reload = true;
            }

            // ReSharper disable once InvertIf
            if (reload)
            {
                EditorUtility.SetDirty((Object) config);
                config.SaveToDisk();
                ReloadAllScene();
            }
        }

        private static float CalcRelativePos(IEnumerable<FavoriteDrawingInfo> infos, float rowHeight, float windowWidth)
        {
            float toolHeight = rowHeight;
            float x = 0;
            float y = 0;
            foreach (FavoriteDrawingInfo info in infos)
            {
                // Need wrap?
                if (x + info.Width > windowWidth)
                {
                    x = 0;
                    y += rowHeight;
                }

                info.OriginalX = x;
                info.OriginalY = y;
                toolHeight = y + rowHeight;

                x += info.Width;
            }

            return toolHeight;
        }

        private class GUIBackgroundColorScoopWithStatus : IDisposable
        {
            private readonly bool _changed;
            private readonly Color _originalColor;
            public GUIBackgroundColorScoopWithStatus(RuntimeFavoriteStatus favStatus)
            {
                if (favStatus == RuntimeFavoriteStatus.Default)
                {
                    return;
                }

                _changed = true;
                _originalColor = GUI.backgroundColor;
                // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                GUI.backgroundColor = favStatus switch
                {
                    RuntimeFavoriteStatus.DragExisted => Color.cyan,
                    RuntimeFavoriteStatus.DragNew => Color.green,
                    _ => throw new ArgumentOutOfRangeException(nameof(favStatus), favStatus, null),
                };
                // Debug.Log($"color to {GUI.backgroundColor}");
            }

            public void Dispose()
            {
                if (_changed)
                {
                    GUI.backgroundColor = _originalColor;
                }
            }
        }

        private static void ExpandInTree(GameObject gameObject, WrapInfo wrapInfo, EditorWindow window, float margin)
        {
            // Debug.Log($"expand {gameObject.name}");
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

        private static void CanDropGos(Event evt, EventType evtType, Rect toolbarRect, EditorWindowStatus windowStatus)
        {
            // Event evt = Event.current;

            if (evtType is not (EventType.DragUpdated or EventType.DragPerform))
            {
                // Don't clean this!
                // windowStatus.InDrag = false;
                return;
            }

            if (!toolbarRect.Contains(evt.mousePosition))
            {
                // windowStatus.InDrag = false;
                windowStatus.Dragging.Clear();
// #if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_DRAG
//                 Debug.Log($"No longer drag as out of rect");
// #endif
                return;
            }

            // if (evtType == EventType.DragUpdated)
            // {
            //     windowStatus.InDrag = true;
            // }

            Object[] dragging = DragAndDrop.objectReferences;
            windowStatus.Dragging.Clear();

            foreach (Object draggedObject in dragging)
            {
                // ReSharper disable once InvertIf
                if (draggedObject is GameObject go && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)))
                {
                    windowStatus.Dragging.Add(go);
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_DRAG
                    Debug.Log($"Add draging {go}");
#endif
                    // yield return go;
                    // Debug.Log("Dragging: " + go.name);
                    // Debug.Log(AssetDatabase.GetAssetPath(go));
                }
            }

            if (windowStatus.Dragging.Count > 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }

            // if (!any)
            // {
            //     DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            // }
        }


        // private static void AddToConfig(GameObject go)
        // {
        //     // GameObject targetGo = go;
        //     // string curScenePath = go.scene.path;
        //     // Debug.Log(curScenePath);
        //     // if (!string.IsNullOrEmpty(curScenePath) && curScenePath.EndsWith(".prefab"))
        //     // {
        //     //     targetGo = Util.GetPrefabSubGameObject(curScenePath, go);
        //     //     if (targetGo == null)
        //     //     {
        //     //         return;
        //     //     }
        //     // }
        //     string scenePath = go.scene.path;
        //     string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
        //
        //     GlobalObjectId targetId = GlobalObjectId.GetGlobalObjectIdSlow(go);
        //     // string targetGoIdStr = Util.GlobalObjectIdNormString(targetId);
        //     string targetGoIdStr = targetId.ToString();
        //     IConfig config = SaintsHierarchyConfig.instance;
        //     List<GameObjectFavorite> favorites = null;
        //     foreach (SceneGuidToGoFavorites sceneGuidToGoFavorites in config.sceneGuidToGoFavoritesList)
        //     {
        //         if (sceneGuidToGoFavorites.sceneGuid == sceneGuid)
        //         {
        //             favorites = sceneGuidToGoFavorites.favorites;
        //             foreach (GameObjectFavorite gameObjectFavorite in favorites)
        //             {
        //                 if (gameObjectFavorite.globalObjectIdString == targetGoIdStr)
        //                 {
        //                     Debug.Log($"exists, skip {targetGoIdStr}");
        //                     return;
        //                 }
        //             }
        //         }
        //     }
        //
        //     GameObjectFavorite item = new GameObjectFavorite
        //     {
        //         globalObjectIdString = targetGoIdStr,
        //         alias = string.Empty,
        //         color = default,
        //         hasColor = false,
        //         icon = string.Empty,
        //     };
        //     if (favorites != null)
        //     {
        //         Debug.Log($"add {targetGoIdStr} in {sceneGuid}");
        //         favorites.Add(item);
        //     }
        //     else
        //     {
        //         Debug.Log($"add {targetGoIdStr} created {sceneGuid}");
        //         config.sceneGuidToGoFavoritesList.Add(new SceneGuidToGoFavorites
        //         {
        //             sceneGuid = sceneGuid,
        //             favorites = new List<GameObjectFavorite>
        //             {
        //                 item,
        //             },
        //         });
        //     }
        //
        //     ReloadSceneFav(scenePath);
        //     config.SaveToDisk();
        // }

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
                }

                type = type.BaseType;
            }
            return (null, null);
        }
    }


}
