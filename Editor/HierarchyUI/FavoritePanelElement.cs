using System.Collections.Generic;
using System.Linq;
using SaintsHierarchy.Editor.Core;
using SaintsHierarchy.Editor.Core.Utils;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public class FavoritePanelElement : VisualElement
    {
        private static VisualTreeAsset _template;
        private readonly VisualElement _buttons;
        private readonly List<(GameObject go, GameObjectFavorite config, FavoriteButton button)> _favorites =
            new List<(GameObject go, GameObjectFavorite config, FavoriteButton button)>();
        private readonly List<(GameObject go, GameObjectFavorite config)> _dragging =
            new List<(GameObject go, GameObjectFavorite config)>();
        private readonly List<FavoriteButton> _ghosts = new List<FavoriteButton>();
        private Rect[] _dragBounds;
        private int _dropIndex = -1;
        private Button _pressedButton;
        private Vector2 _pressPosition;
        private int _pointerId;
        private VisualElement _dragEventRoot;
        private VisualElement _hierarchyDragSource;

        public FavoritePanelElement()
        {
            _template ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/Favorite/FavoritePanel.uxml");
            _template.CloneTree(this);
            _buttons = this.Q<VisualElement>("favorite-buttons");
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                // The hierarchy collection captures the pointer, so drag events can still target
                // that sibling while the pointer is over favorites. Handle them before the view.
                _dragEventRoot = panel.visualTree;
                _dragEventRoot.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
                _dragEventRoot.RegisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
                _dragEventRoot.RegisterCallback<DragExitedEvent>(OnDragExited, TrickleDown.TrickleDown);
                // Captured events can be delivered only to the collection itself, bypassing ancestors.
                _hierarchyDragSource = _dragEventRoot.Q("unity-tree-view__list-view");
                if(_hierarchyDragSource != null)
                {
                    _hierarchyDragSource.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
                    _hierarchyDragSource.RegisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
                    _hierarchyDragSource.RegisterCallback<DragExitedEvent>(OnDragExited, TrickleDown.TrickleDown);
                }
                EditorApplication.hierarchyChanged += Refresh;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                EditorSceneManager.sceneSaved += OnSceneSaved;
                HierarchyEditorEvents.ReloadAllScenesRequested += Refresh;
                HierarchyEditorEvents.InitializeRequested += Refresh;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                _dragEventRoot.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
                _dragEventRoot.UnregisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
                _dragEventRoot.UnregisterCallback<DragExitedEvent>(OnDragExited, TrickleDown.TrickleDown);
                _dragEventRoot = null;
                if(_hierarchyDragSource != null)
                {
                    _hierarchyDragSource.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
                    _hierarchyDragSource.UnregisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
                    _hierarchyDragSource.UnregisterCallback<DragExitedEvent>(OnDragExited, TrickleDown.TrickleDown);
                }
                _hierarchyDragSource = null;

                EditorApplication.hierarchyChanged -= Refresh;
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorSceneManager.sceneSaved -= OnSceneSaved;
                HierarchyEditorEvents.ReloadAllScenesRequested -= Refresh;
                HierarchyEditorEvents.InitializeRequested -= Refresh;
                ResetPress();
                ClearDrag();
            });
            RegisterCallback<DragEnterEvent>(evt => UpdateDragFeedback(evt.mousePosition));
            RegisterCallback<DragLeaveEvent>(evt =>
            {
                if (evt.target == this)
                {
                    ClearDrag();
                }
            });
        }

        private void OnDragExited(DragExitedEvent evt) => ClearDrag();

        private void OnPlayModeChanged(PlayModeStateChange state) => Refresh();
        private void OnSceneSaved(Scene scene) => Refresh();

        private void Refresh()
        {
            ResetPress();
            ClearDrag();
            IConfig usingConfig = Util.GetUsingConfig();
            style.display = usingConfig.disabled || usingConfig.disableFavorites
                ? DisplayStyle.None : DisplayStyle.Flex;
            // Keep resolved objects through play mode, where their global IDs can change.
            Dictionary<string, GameObject> loaded = _favorites.ToDictionary(each => each.config.globalObjectIdString, each => each.go);
            _favorites.Clear();
            _buttons.Clear();
            HashSet<string> scenes = new HashSet<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path))
                {
                    scenes.Add(AssetDatabase.AssetPathToGUID(scene.path));
                }
            }
            HashSet<string> added = new HashSet<string>();
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (GameObjectFavorite favorite in Util.GetFavoriteConfig().favorites)
            {
                if (!scenes.Contains(favorite.sceneGuid) || !added.Add(favorite.globalObjectIdString))
                {
                    continue;
                }
                loaded.TryGetValue(favorite.globalObjectIdString, out GameObject go);
                if (go == null && GlobalObjectId.TryParse(favorite.globalObjectIdString, out GlobalObjectId id))
                {
                    go = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                }
                if (go == null)
                {
                    continue;
                }
                FavoriteButton button = CreateButton(go, favorite, false);
                _favorites.Add((go, favorite, button));
                _buttons.Add(button);
            }
        }

        private FavoriteButton CreateButton(GameObject go, GameObjectFavorite favorite, bool ghost)
        {
            FavoriteButton button = new FavoriteButton(go, favorite)
            {
                IsGhost = ghost,
                AnimateReordering = !ghost,
            };
            if (ghost)
            {
                return button;
            }
            button.clicked += () => Activate(go);
            button.RegisterCallback<PointerDownEvent>(evt =>
            {
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (evt.button == 1 || evt.button == 0 && evt.altKey)
                {
                    ShowConfig(button, favorite);
                    evt.StopImmediatePropagation();
                }
                else if (evt.button == 0)
                {
                    ResetPress();
                    _pressedButton = button;
                    _pressPosition = evt.position;
                    _pointerId = evt.pointerId;
                    button.CapturePointer(evt.pointerId);
                    evt.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);
            button.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (_pressedButton != button || !button.HasPointerCapture(evt.pointerId) ||
                    ((Vector2)evt.position - _pressPosition).sqrMagnitude <= 25 || go == null)
                {
                    return;
                }
                ResetPress();
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.entityIds = new[] { go.GetEntityId() };
                DragAndDrop.StartDrag(go.name);
                evt.StopPropagation();
            });
            button.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (_pressedButton != button || evt.button != 0)
                {
                    return;
                }
                ResetPress();
                if (button.worldBound.Contains(evt.position))
                {
                    Activate(go);
                }
                evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
            button.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (_pressedButton == button)
                {
                    _pressedButton = null;
                }
            });
            return button;
        }

        private static void Activate(GameObject go)
        {
            if (go == null)
            {
                return;
            }
            EditorGUIUtility.PingObject(go);
            if (Util.GetUsingConfig().FavoriteClickToInspect)
            {
                Selection.activeGameObject = go;
            }
        }

        private static void ShowConfig(Button button, GameObjectFavorite favorite)
        {
            foreach (HierarchyWindow window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
            {
                if (window.rootVisualElement.panel != button.panel)
                {
                    continue;
                }
                FavoriteConfigPopup popup = new FavoriteConfigPopup(favorite);
                popup.UpdatedEvent.AddListener(_ => HierarchyEditorEvents.RequestReloadAllScenes());
                popup.DeletedEvent.AddListener(_ => HierarchyEditorEvents.RequestReloadAllScenes());
                Vector2 position = window.position.position + new Vector2(button.worldBound.xMin, button.worldBound.yMax);
                UnityEditor.PopupWindow.Show(GUIUtility.ScreenToGUIRect(new Rect(position, Vector2.zero)), popup);
                break;
            }
        }

        private void ResetPress()
        {
            Button button = _pressedButton;
            _pressedButton = null;
            if (button != null && button.HasPointerCapture(_pointerId))
            {
                button.ReleasePointer(_pointerId);
            }
        }

        private bool UpdateDrag(Vector2 position)
        {
            if (!worldBound.Contains(position))
            {
                ClearDrag();
                return false;
            }

            GameObject[] objects = DragAndDrop.entityIds
                .Select(EditorUtility.EntityIdToObject)
                .OfType<GameObject>()
                .Where(go => go != null
                             && !EditorUtility.IsPersistent(go)
                             && go.scene.IsValid()
                             && go.scene.isLoaded
                             && !string.IsNullOrEmpty(go.scene.path))
                .Distinct()
                .ToArray();

            if (objects.Length == 0)
            {
                ClearDrag();
                return false;
            }

            if (!_dragging.Select(each => each.go).SequenceEqual(objects))
            {
                ClearDrag();
                // Freeze hit targets before inserting ghosts so reflow cannot make the drop slot oscillate.
                _dragBounds = _favorites
                    .Select(each => _buttons.LocalToWorld(each.button.layout))
                    .ToArray();
                foreach (GameObject go in objects)
                {
                    int existing = _favorites.FindIndex(each => each.go == go);
                    GameObjectFavorite favorite = existing >= 0
                        ? _favorites[existing].config
                        : new GameObjectFavorite
                        {
                            globalObjectIdString = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString(),
                            sceneGuid = AssetDatabase.AssetPathToGUID(go.scene.path),
                        };
                    _dragging.Add((go, favorite));
                    _ghosts.Add(CreateButton(go, favorite, true));
                }
            }
            int index = 0;
            for (int i = 0; i < _favorites.Count; i++)
            {
                Rect bounds = _dragBounds[i];
                if (position.y < bounds.yMin || position.y <= bounds.yMax && position.x < bounds.center.x)
                {
                    break;
                }
                if (_dragging.All(each => each.go != _favorites[i].go))
                {
                    index++;
                }
            }

            // ReSharper disable once InvertIf
            if (index != _dropIndex)
            {
                _dropIndex = index;
                foreach (FavoriteButton ghost in _ghosts)
                {
                    ghost.RemoveFromHierarchy();
                }

                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach ((GameObject go, GameObjectFavorite config, FavoriteButton button) favorite in _favorites)
                {
                    if (_dragging.Any(each => each.go == favorite.go))
                    {
                        favorite.button.RemoveFromHierarchy();
                    }
                }
                for (int i = 0; i < _ghosts.Count; i++)
                {
                    _buttons.Insert(index + i, _ghosts[i]);
                }
            }
            return true;
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (!UpdateDragFeedback(evt.mousePosition))
            {
                return;
            }
            evt.StopImmediatePropagation();
        }

        private bool UpdateDragFeedback(Vector2 position)
        {
            if (!enabledInHierarchy || resolvedStyle.display == DisplayStyle.None || !worldBound.Contains(position))
            {
                ClearDrag();
                return false;
            }
            bool accepted = UpdateDrag(position);
            if (!accepted)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
            else if (_dragging.All(drag => _favorites.Any(each => each.go == drag.go)))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }

            return true;
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (!UpdateDragFeedback(evt.mousePosition) || _dragging.Count == 0)
            {
                return;
            }

            IConfig config = Util.GetFavoriteConfig();
            HashSet<string> ids = _dragging
                .Select(each => each.config.globalObjectIdString)
                .ToHashSet();

            List<(GameObject go, GameObjectFavorite config, FavoriteButton button)> remaining =
                _favorites
                    .Where(each => !ids.Contains(each.config.globalObjectIdString))
                    .ToList();
            // Anchor in the full saved list, retaining favorites belonging to closed scenes.
            config.favorites.RemoveAll(each => ids.Contains(each.globalObjectIdString));
            int insertIndex = _dropIndex == 0
                ? 0
                : config.favorites.IndexOf(remaining[_dropIndex - 1].config) + 1;

            config.favorites.InsertRange(insertIndex, _dragging.Select(each => each.config));
            EditorUtility.SetDirty((Object)config);
            config.SaveToDisk();
            DragAndDrop.AcceptDrag();
            if (panel.GetCapturingElement(PointerId.mousePointerId) is VisualElement captured)
            {
                captured.ReleasePointer(PointerId.mousePointerId);
            }
            ClearDrag();
            HierarchyEditorEvents.RequestReloadAllScenes();
            evt.StopImmediatePropagation();
        }

        private void ClearDrag()
        {
            if (_dragging.Count == 0)
            {
                return;
            }
            foreach (FavoriteButton ghost in _ghosts)
            {
                ghost.RemoveFromHierarchy();
            }
            for (int i = 0; i < _favorites.Count; i++)
            {
                _buttons.Insert(i, _favorites[i].button);
            }
            _ghosts.Clear();
            _dragging.Clear();
            _dragBounds = null;
            _dropIndex = -1;
        }
    }
}
