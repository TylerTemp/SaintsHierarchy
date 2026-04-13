using System;
using System.Collections.Generic;
using System.Reflection;
using SaintsHierarchy.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor
{
    public class SaintsHierarchyWindow
    {
        private static Type _sceneHierarchyWindowType;
        private static FieldInfo _sLastInteractedHierarchy;

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

        private static readonly Dictionary<EditorWindow, Delegate> Wrapped = new Dictionary<EditorWindow, Delegate>();
        // public static readonly Dictionary<EditorWindow, Delegate> OriginDelegate = new Dictionary<EditorWindow, Delegate>();
        // private readonly Delegate _onGUI;

        // public SaintsHierarchyWindow(Delegate onGUI)
        // {
        //     _onGUI = onGUI;
        // }

        private static void SetupWrap(EditorWindow window)
        {
            if (Wrapped.ContainsKey(window))
            {
                return;
            }

            // Debug.Log($"start wrap {window}");
            Delegate result = CreateNewWrap(window);
            if (result == null)
            {
                Debug.Log($"failed to wrap {window}");
                return;
            }

            // Debug.Log($"done wrap {window}");
            Wrapped[window] = result;
            window.Repaint();
        }

        private static FieldInfo _fieldMParent;
        private static MethodInfo _methodCreateDelegate;
        private static Type _hostViewType;
        private static FieldInfo _fieldMOnGUI;

        private static Delegate CreateNewWrap(EditorWindow window)
        {
            object hostViewParent = _fieldMParent.GetValue(window);
            // EditorWindow.m_Parent;
            // UnityEditor.DockArea;
            // Debug.Log(hostViewParent.GetType());
            if (_methodCreateDelegate == null)
            {
                Type type = hostViewParent.GetType();
                while (type != null)
                {
                    // Debug.Log(type);
                    _methodCreateDelegate = type.GetMethod(
                        "CreateDelegate",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                        null,
                        new[] { typeof(string) },
                        null
                    );

                    if (_methodCreateDelegate != null)
                    {
                        _hostViewType = type;
                        break;
                    }

                    type = type.BaseType;
                }

            }

            Debug.Assert(_methodCreateDelegate != null, "No longer works in this version of Unity");

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
                return null;
            }

            _fieldMOnGUI.SetValue(hostViewParent, wrappedDelegate);

            // SceneHierarchyWindow.m_Parent;
            // OriginDelegate[window] = wrappedDelegate;
            // window.Repaint();

            return onGuiDelegate;
        }

        private static FieldInfo _fieldMPos;

        private static void OnGUIWrapper(EditorWindow window)
        {

            // Debug.Log("called");
            if (!Wrapped.TryGetValue(window, out Delegate originOnGUI))
            {
                throw new Exception("This version of Unity is not supported");
            }

            bool personalDisabled = !PersonalHierarchyConfig.instance.personalEnabled;
            if (personalDisabled
                    ? SaintsHierarchyConfig.instance.disabled
                    : PersonalHierarchyConfig.instance.disabled)
            {
                originOnGUI.DynamicInvoke();
                return;
            }

            if (_sceneHierarchyWindowType == null)
            {
                return;
            }

            _fieldMPos ??= typeof(EditorWindow).GetField("m_Pos", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_fieldMPos == null)
            {
                throw new Exception("m_Pos is not found in this version of Unity");
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

                originOnGUI.DynamicInvoke();

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

                    }
                }

            }

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
            enumerator.MoveNext();
            T first = enumerator.Current;
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (first == null)
            {
                return (false, Array.Empty<T>());
            }

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
            string targetGoIdStr = Util.GlobalObjectIdNormString(targetId);
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
    }
}
