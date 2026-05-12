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
        // private static Texture2D _colorStripTex;
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
#if UNITY_6000_3_OR_NEWER
            EditorWindow.windowFocusChanged -= CheckWindowFocused;
            EditorWindow.windowFocusChanged += CheckWindowFocused;
#endif

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

        public static void ReloadAllScene()
        {
            // LoadedScenes.Clear();
            CurrentFavoriteGameObjects.Clear();
            OnSceneCheck();
        }

        // private static readonly HashSet<Scene> LoadedScenes = new HashSet<Scene>();

        private static void OnSceneCheck()
        {
            // int count = SceneManager.sceneCount;
            // HashSet<Scene> leftOutScenes = new HashSet<Scene>(LoadedScenes);

            // for (int i = 0; i < count; i++)
            // {
            //     Scene scene = SceneManager.GetSceneAt(i);
            //
            //     if (LoadedScenes.Add(scene))
            //     {
            //         leftOutScenes.Remove(scene);
            //         ReloadSceneFav(scene);
            //     }
            //     // Debug.Log(
            //     //     $"[{i}] " +
            //     //     $"name={scene.name}, " +
            //     //     $"path={scene.path}, " +
            //     //     $"loaded={scene.isLoaded}, " +
            //     //     $"dirty={scene.isDirty}"
            //     // );
            // }
            //
            // foreach (Scene leftOutScene in leftOutScenes)
            // {
            //     RemoveSceneFav(leftOutScene);
            // }

            int count = SceneManager.sceneCount;
            HashSet<string> openSceneGuids = new HashSet<string>();

            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                openSceneGuids.Add(AssetDatabase.GUIDFromAssetPath(scene.path).ToString());
                // if (LoadedScenes.Add(scene))
                // {
                //     leftOutScenes.Remove(scene);
                //     ReloadSceneFav(scene);
                // }
                // Debug.Log(
                //     $"[{i}] " +
                //     $"name={scene.name}, " +
                //     $"path={scene.path}, " +
                //     $"loaded={scene.isLoaded}, " +
                //     $"dirty={scene.isDirty}"
                // );
            }

            IConfig config = Util.GetFavoriteConfig();
// #if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_RENDER_FAV
//             Debug.Log($"scene fav count {config.favorites.Count}");
// #endif
            foreach (GameObjectFavorite sceneGuidToGoFavorites in config.favorites)
            {
// #if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_RENDER_FAV
//                 Debug.Log($"checking {sceneGuidToGoFavorites.DebugGetObject()} {sceneGuidToGoFavorites.sceneGuid}->{guidStr}");
// #endif
                if (openSceneGuids.Contains(sceneGuidToGoFavorites.sceneGuid))
                {
                    // List<RuntimeFavoriteGameObject> fav = new List<RuntimeFavoriteGameObject>();
                    string gameIdStr = sceneGuidToGoFavorites.globalObjectIdString;
                    // Debug.Log($"parsing {gameIdStr}");
                    if (GlobalObjectId.TryParse(gameIdStr, out GlobalObjectId id))
                    {
                        GameObject go;
                        try
                        {
                            go = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                        }
#pragma warning disable CS0168 // Variable is declared but never used
                        catch (Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
                        {
#if SAINTSHIERARCHY_DEBUG
                            Debug.LogException(e);
#endif
                            return;
                        }
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_RENDER_FAV
                        Debug.Log($"get {go}");
#endif
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

//         private static void ReloadSceneFav(Scene scene) => ReloadSceneFav(scene.path);
//         private static void ReloadSceneFav(string scenePath)
//         {
//             GUID guid = AssetDatabase.GUIDFromAssetPath(scenePath);
//             string guidStr = guid.ToString();
//             CurrentFavoriteGameObjects.RemoveAll(each => each.FavoriteConfig.sceneGuid == guidStr);
//
//             IConfig config = Util.GetFavoriteConfig();
// // #if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_RENDER_FAV
// //             Debug.Log($"scene fav count {config.favorites.Count}");
// // #endif
//             foreach (GameObjectFavorite sceneGuidToGoFavorites in config.favorites)
//             {
// #if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_RENDER_FAV
//                 Debug.Log($"checking {sceneGuidToGoFavorites.DebugGetObject()} {sceneGuidToGoFavorites.sceneGuid}->{guidStr}");
// #endif
//                 if (sceneGuidToGoFavorites.sceneGuid == guidStr)
//                 {
//                     // List<RuntimeFavoriteGameObject> fav = new List<RuntimeFavoriteGameObject>();
//                     string gameIdStr = sceneGuidToGoFavorites.globalObjectIdString;
//                     // Debug.Log($"parsing {gameIdStr}");
//                     if (GlobalObjectId.TryParse(gameIdStr, out GlobalObjectId id))
//                     {
//                         GameObject go = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
// #if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_RENDER_FAV
//                         Debug.Log($"get {go}");
// #endif
//                         if (go != null)
//                         {
//                             // Debug.Log($"add {go}");
//                             CurrentFavoriteGameObjects.Add(new RuntimeFavoriteGameObject(go, sceneGuidToGoFavorites));
//                         }
//                     }
//                     // else
//                     // {
//                     //     Debug.Log($"parsing failed");
//                     // }
//
//                     // return;
//                     // CurrentFavoriteGameObjects.RemoveAll(static each => each.SceneGuid == sceneGuidToGoFavorites.sceneGuid);
//                 }
//             }
//         }
//
//         private static void RemoveSceneFav(Scene scene)
//         {
//             string scenePath = scene.path;
//             GUID guid = AssetDatabase.GUIDFromAssetPath(scenePath);
//             string guidStr = guid.ToString();
//             CurrentFavoriteGameObjects.RemoveAll(each => each.FavoriteConfig.sceneGuid == guidStr);
//         }
//
         private static void OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
         {
             ReloadAllScene();
             // ReloadSceneFav(scene);
             // Debug.Log($"created {scene.name}");
         }
//
//         // private static void OnSceneClosed(Scene scene)
//         // {
//         //     Debug.Log($"closed {scene.name}");
//         // }
//
         private static void OnSceneClosing(Scene scene, bool removingScene)
         {
             ReloadAllScene();
             // RemoveSceneFav(scene);
             // Debug.Log($"closing {scene.name}");
         }

         private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
         {
             ReloadAllScene();
             // ReloadSceneFav(scene);
             // Debug.Log($"opened {scene.name}");
         }

        private static IConfig GetUsingConfig()
        {
            // return SaintsHierarchyConfig.instance.en;
            return PersonalHierarchyConfig.instance.personalEnabled? PersonalHierarchyConfig.instance: SaintsHierarchyConfig.instance;
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

#if UNITY_6000_3_OR_NEWER
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
#endif

        private static readonly Dictionary<EditorWindow, WrapInfo> Wrapped = new Dictionary<EditorWindow, WrapInfo>();
        // public static readonly Dictionary<EditorWindow, Delegate> OriginDelegate = new Dictionary<EditorWindow, Delegate>();
        // private readonly Delegate _onGUI;

        // public SaintsHierarchyWindow(Delegate onGUI)
        // {
        //     _onGUI = onGUI;
        // }

        private readonly struct WrapInfo
        {
            public readonly object HostViewParent;
            public readonly Delegate OriginalOnGUI;
            public readonly Delegate WrappedOnGUI;

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

            public WrapInfo(object hostViewParent, Delegate originalOnGUI, Delegate wrappedOnGUI, Func<
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
                HostViewParent = hostViewParent;
                OriginalOnGUI = originalOnGUI;
                WrappedOnGUI = wrappedOnGUI;
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
            if (Wrapped.TryGetValue(window, out WrapInfo wrappedInfo) && IsWrapCurrent(window, wrappedInfo))
            {
                return;
            }

            Wrapped.Remove(window);

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

        private static bool IsWrapCurrent(EditorWindow window, WrapInfo wrapInfo)
        {
            if (_fieldMOnGUI == null)
            {
                return false;
            }

            object hostViewParent = _fieldMParent.GetValue(window);
            if (hostViewParent == null || !ReferenceEquals(hostViewParent, wrapInfo.HostViewParent))
            {
                return false;
            }

            Delegate currentOnGui = _fieldMOnGUI.GetValue(hostViewParent) as Delegate;
            return currentOnGui == wrapInfo.WrappedOnGUI;
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
            if (treeViewController == null)
            {
                return default;
            }

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

            return new WrapInfo(hostViewParent, onGuiDelegate, wrappedDelegate, setExpand, treeViewData, _propertyRowCount, getRow, treeViewState);
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

            public readonly bool HasColor;
            public readonly Color Color;

            public readonly bool HasUnderline;
            public readonly Color UnderlineColor;

            public FavoriteDrawingInfo(RuntimeFavoriteGameObject runtimeConfig,
                RuntimeFavoriteStatus status,
                string text,
                Texture2D icon,
                float width,
                bool hasColor,
                Color color,
                bool hasUnderline,
                Color underlineColor
            )
            {
                RuntimeConfig = runtimeConfig;
                Status = status;
                Width = width;

                Text = text;
                Icon = icon;

                HasColor = hasColor;
                Color = color;

                HasUnderline = hasUnderline;
                UnderlineColor = underlineColor;
            }

            public static string HelperGetDisplayText(RuntimeFavoriteGameObject config)
            {
                string alias = config.FavoriteConfig.alias;
                return string.IsNullOrEmpty(alias)? config.LoadedGameObject.name: alias;
            }

            // public string GetDisplayText() => RuntimeConfig.LoadedGameObject.name;
            private static Texture2D _defaultIcon;

            // public Texture2D GetDisplayIcon() => EditorGUIUtility.GetIconForObject(RuntimeConfig.LoadedGameObject);


        }

        // private static bool _inDrag;

        private readonly struct MergedConfig
        {
            public readonly bool HasColor;
            public readonly Color Color;
            public readonly bool HasUnderline;
            public readonly Color UnderlineColor;
            public readonly Texture2D Icon;

            public MergedConfig(bool hasColor, Color color, bool hasUnderline, Color underlineColor, Texture2D icon)
            {
                HasColor = hasColor;
                Color = color;
                HasUnderline = hasUnderline;
                UnderlineColor = underlineColor;
                Icon = icon;
            }
        }

        private static MergedConfig GetMergedConfig(GameObject go, RuntimeFavoriteGameObject favoriteConfig)
        {
            GameObjectConfig goConfig = default;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                (bool runtimeFound, GameObjectConfig runtimeConfig) =
                    RuntimeCacheConfig.instance.Search(go.
#if UNITY_6000_4_OR_NEWER
                        GetEntityId
#else
                        GetInstanceID
#endif
                            ());
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (runtimeFound)
                {
                    goConfig = runtimeConfig;
                }
                else
                {
                    (bool found, GameObjectConfig config) c = Util.GetGameObjectConfig(go);
                    goConfig = c.config;
                }
            }
            else
            {
                (bool found, GameObjectConfig goConfigResult) = Util.GetGameObjectConfig(go);
                if (found)
                {
                    RuntimeCacheConfig.instance.Upsert(go.
#if UNITY_6000_4_OR_NEWER
                        GetEntityId
#else
                        GetInstanceID
#endif
                            (), goConfigResult);
                    goConfig = goConfigResult;
                }
            }

            Texture2D icon = GetMergedIcon(go, favoriteConfig, goConfig);
            (bool hasColor, Color color) = GetMergedColor(favoriteConfig, goConfig);
            (bool hasUnderline, Color underlineColor) = GetUnderline(go);
            if (hasColor && icon is null)
            {
                icon = Util.GetCachedIcon("transparent_square.png");
            }

            return new MergedConfig(hasColor, color, hasUnderline, underlineColor, icon);
        }

        private static Texture2D GetMergedIcon(GameObject go, RuntimeFavoriteGameObject favoriteConfig, GameObjectConfig goConfig)
        {
            switch (favoriteConfig.FavoriteConfig.iconType)
            {
                case GameObjectFavoriteIconType.Default:
                {
                    if (!string.IsNullOrEmpty(goConfig.icon))
                    {
                        return Util.LoadResource<Texture2D>(goConfig.icon);
                    }

                    bool isMissingPrefab = IsMissingPrefab(go);
                    if (isMissingPrefab)
                    {
                        return Util.LoadResource<Texture2D>("prefab_warning.png");
                    }
                    Texture2D compIcon = Util.GetIconByComponent(go.GetComponents<Component>());
                    // ReSharper disable once ConvertIfStatementToReturnStatement
                    if (compIcon is not null)
                    {
                        return compIcon;
                    }

                    goto case GameObjectFavoriteIconType.UnityDefault;
                }
                case GameObjectFavoriteIconType.UnityDefault:
                {
                    Texture2D unityIcon = GetUnityDefaultIcon(go);
                    // ReSharper disable once InvertIf
                    if (unityIcon is not null)
                    {
                        (bool hasUnderline, Color _) = Util.GetUnderline(unityIcon.name);
                        if (hasUnderline)
                        {
                            return null;
                        }
                    }

                    return unityIcon;
                }
                case GameObjectFavoriteIconType.None:
                {
                    return null;
                }
                case GameObjectFavoriteIconType.Custom:
                {
                    return Util.LoadResource<Texture2D>(favoriteConfig.FavoriteConfig.icon);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(favoriteConfig.FavoriteConfig.iconType), favoriteConfig.FavoriteConfig.iconType, null);
            }
        }

        private static (bool hasColor, Color color) GetMergedColor(RuntimeFavoriteGameObject favoriteConfig, GameObjectConfig goConfig)
        {
            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            switch (favoriteConfig.FavoriteConfig.colorType)
            {
                case GameObjectFavoriteColorType.Default:
                {
                    return (goConfig.hasColor, goConfig.color);
                }
                case GameObjectFavoriteColorType.NoColor:
                {
                    return (false, default);
                }
                case GameObjectFavoriteColorType.CustomColor:
                {
                    return (true, favoriteConfig.FavoriteConfig.color);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(favoriteConfig.FavoriteConfig.colorType), favoriteConfig.FavoriteConfig.colorType, null);
            }
        }

        private static (bool hasUnderline, Color underlineColor) GetUnderline(GameObject go)
        {
            Texture2D icon = EditorGUIUtility.GetIconForObject(go);
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (icon is null)
            {
                return (false, default);
            }

            return Util.GetUnderline(icon.name);
        }

        private static Texture2D GetUnityDefaultIcon(GameObject go)
        {
            Texture2D icon = EditorGUIUtility.GetIconForObject(go);
            if (icon is not null)
            {
                return icon;
            }

            if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                IConfig config = Util.GetUsingConfig();
                if(config.noDefaultIcon || config.transparentDefaultIcon)
                {
                    return null;
                }
                return EditorGUIUtility.IconContent("d_GameObject Icon").image as Texture2D;
            }

            PrefabInstanceStatus instanceStatus = PrefabUtility.GetPrefabInstanceStatus(go);

            if (instanceStatus == PrefabInstanceStatus.MissingAsset)
            {
                return Util.LoadResource<Texture2D>("prefab_warning.png");
            }

            PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(go);

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (assetType)
            {
                case PrefabAssetType.Regular:
                    return EditorGUIUtility.IconContent("d_Prefab Icon").image as Texture2D;
                case PrefabAssetType.Variant:
                    return EditorGUIUtility.IconContent("d_PrefabVariant Icon").image as Texture2D;
                case PrefabAssetType.Model:
                    return EditorGUIUtility.IconContent("d_PrefabModel Icon").image as Texture2D;
            }

            return null;
        }

        private static bool IsMissingPrefab(GameObject go)
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                return false;
            }
            PrefabInstanceStatus instanceStatus = PrefabUtility.GetPrefabInstanceStatus(go);

            return instanceStatus == PrefabInstanceStatus.MissingAsset;
        }

        private class EditorWindowStatus
        {
            // public bool InDrag;
            public readonly List<GameObject> Dragging = new List<GameObject>();

            // public bool selfDragging = false;
            // public Vector2 selfDraggingStart;

            public GameObject PrepareDragGo;
            public Vector2 PrepareDragPos;
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
            Dictionary<GameObject, RuntimeFavoriteGameObject> existedDragging = new Dictionary<GameObject, RuntimeFavoriteGameObject>();
            foreach (RuntimeFavoriteGameObject runtimeFavoriteGameObject in CurrentFavoriteGameObjects)
            {
                GameObject go = runtimeFavoriteGameObject.LoadedGameObject;
                MergedConfig mergedConfig = GetMergedConfig(go, runtimeFavoriteGameObject);

                string text = FavoriteDrawingInfo.HelperGetDisplayText(runtimeFavoriteGameObject);
                Texture2D icon = mergedConfig.Icon;

                float textWidth = GUI.skin.button.CalcSize(new GUIContent(text)).x;
                float iconWidth = icon is null? 0: EditorGUIUtility.singleLineHeight;
                float totalWidth = textWidth + iconWidth + 6;
                // float totalWidth = new GUIStyle("Button").CalcSize(new GUIContent(text, )) + iconWidth + gap * 2;

                FavoriteDrawingInfo info = new FavoriteDrawingInfo(runtimeFavoriteGameObject, RuntimeFavoriteStatus.Default, text,
                    icon, totalWidth, mergedConfig.HasColor, mergedConfig.Color, mergedConfig.HasUnderline, mergedConfig.UnderlineColor);

                if (windowStatus.Dragging.Contains(runtimeFavoriteGameObject.LoadedGameObject))
                {
                    // info.Status = RuntimeFavoriteStatus.DragExisted;
                    existedDragging[runtimeFavoriteGameObject.LoadedGameObject] = runtimeFavoriteGameObject;
                    continue;
                }
                existsDrawingInfos.Add(info);
            }

            // add dragging to display it properly
            List<FavoriteDrawingInfo> draggingDrawingInfos = new List<FavoriteDrawingInfo>();
            foreach (GameObject dragging in windowStatus.Dragging)
            {
                bool exists = existedDragging.TryGetValue(dragging, out RuntimeFavoriteGameObject runtimeFavoriteGameObject);

                MergedConfig mergedConfig = GetMergedConfig(dragging, runtimeFavoriteGameObject);

                Texture2D icon = mergedConfig.Icon;
                bool hasColor = mergedConfig.HasColor;
                Color color = mergedConfig.Color;

                string text = exists
                    ? FavoriteDrawingInfo.HelperGetDisplayText(runtimeFavoriteGameObject)
                    : dragging.name;

                float textWidth = GUI.skin.button.CalcSize(new GUIContent(text)).x;
                float iconWidth = icon is null? 0: EditorGUIUtility.singleLineHeight;
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
                    exists? RuntimeFavoriteStatus.DragExisted: RuntimeFavoriteStatus.DragNew,
                    text,
                    icon,
                    totalWidth,
                    hasColor,
                    color,
                    mergedConfig.HasUnderline,
                    mergedConfig.UnderlineColor);
                draggingDrawingInfos.Add(info);
            }

            // Calc
            float toolHeight = CalcRelativePos(existsDrawingInfos.Concat(draggingDrawingInfos), rowHeight, windowWidth);

            // fav icon
            Texture2D favIcon = Util.GetCachedIcon("fav.png");
            Rect iconRect = new Rect(windowWidth - 18, 0, 18, 18);
            GUI.DrawTexture(iconRect, favIcon, ScaleMode.StretchToFill, true);

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
                    if (favoriteDrawingInfo.HasColor)
                    {
                        GUI.Box(drawRect, "", GUI.skin.button);  // bg

                        // GUI.backgroundColor = favoriteDrawingInfo.Color;
                        using(new GUIColorScoop(favoriteDrawingInfo.Color))
                        {
                            // color bg box
                            GUI.DrawTexture(drawRect, Util.GetCachedIcon("color-strip-boxed.png"), ScaleMode.StretchToFill, true);
                            // color square
                            EditorGUI.DrawRect(new Rect(drawRect)
                            {
                                width = drawRect.height,
                            }, favoriteDrawingInfo.Color);
                        }
                        // Debug.Log($"{content.text}/{content.image}");
                        GUI.Box(drawRect, content, GUI.skin.label);
                    }
                    else
                    {
                        GUI.Box(drawRect, content, GUI.skin.button);
                    }
                }

                if (favoriteDrawingInfo.HasUnderline)
                {
                    Rect underlineRect;
                    if (favoriteDrawingInfo.Icon is null)
                    {
                        underlineRect = new Rect(drawRect.x + 1, drawRect.yMax - 2, drawRect.width - 2, 1);
                    }
                    else
                    {
                        underlineRect = new Rect(drawRect.x + drawRect.height, drawRect.yMax - 2,
                            drawRect.width - 1 - drawRect.height, 1);
                    }
                    EditorGUI.DrawRect(underlineRect, favoriteDrawingInfo.UnderlineColor);
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
                            windowStatus.PrepareDragPos = eventCurrent.mousePosition;
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
                            && windowStatus.PrepareDragGo == favoriteDrawingInfo.RuntimeConfig.LoadedGameObject
                            && (windowStatus.PrepareDragPos - eventCurrent.mousePosition).sqrMagnitude > 5*5)
                        {
                            // Debug.Log((windowStatus.PrepareDragPos -  eventCurrent.mousePosition).sqrMagnitude);
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
                        // new Rect(eventCurrent.mousePosition.x, eventCurrent.mousePosition.y, 0, 0),
                        new Rect(eventCurrent.mousePosition.x, drawRect.yMax, 0, 0),
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
            // ReSharper disable once UnusedParameter.Local
            pop.DeletedEvent.AddListener(target =>
            {
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_RENDER_FAV
                Debug.Log($"fav deleted {target.globalObjectIdString}");
#endif
                ReloadAllScene();
                window.Repaint();
            });
            pop.UpdatedEvent.AddListener(_ =>
            {
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
                        Debug.Log($"found now drag item {savedConf.DebugGetObject()}");
#endif
                        dragSavedConfigs.Add(savedConf);
                    }
                    else
                    {
                        if (dragSavedConfigs.Count == 0)
                        {
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_APPLY_FAV
                            Debug.Log($"found before drag item {savedConf.DebugGetObject()}");
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
                    Debug.Log($"dragSavedConfigIds: {string.Join(",", dragSavedConfigs.Select(each => each.DebugGetObject()))}");
#endif
                    config.favorites.RemoveAll(each => dragSavedConfigIds.Contains(each.globalObjectIdString));
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_APPLY_FAV
                    Debug.Log($"removed now: {string.Join(",", config.favorites.Select(each => each.DebugGetObject()))}");
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
                        Debug.Log($"insert index to {dragToIndex}: {drag.DebugGetObject()}");
#endif
                        config.favorites.Insert(dragToIndex, drag);
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_APPLY_FAV
                        Debug.Log($"insert result: {string.Join(",", config.favorites.Select(each => each.DebugGetObject()))}");
#endif
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

            // int rowCount = wrapInfo.GetRowCount();
//             float maxScrollPos = rowCount * 16 - window.position.height + 26.9f;
//
//             int rowIndex = wrapInfo.GetRow(gameObject.
// #if UNITY_6000_3_OR_NEWER
//                     GetEntityId()
// #else
//                     GetInstanceID()
// #endif
//                 );
//
//             float rowPos = rowIndex * 16f + 8;
            // float scrollAreaHeight = wrapInfo.GetTreeViewRect(window).height;

            // float targetScrollPos = Mathf.Clamp(rowPos - margin, 0, maxScrollPos);
            //
            // if (targetScrollPos < 25)
            // {
            //     targetScrollPos = 0;
            // }

            // wrapInfo.TreeViewState.scrollPos = Vector2.up * targetScrollPos;
            // Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
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
