using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor
{
    // public class FavoriteConfigPopup: PopupWindowContent
    public class FavoriteConfigPopup: PopupWindowContent
    {
        private const float Width = 200f;
        private float _height = FavoriteConfigPanel.DefaultHeight;

        public override Vector2 GetWindowSize() => new Vector2(Width, _height);

        private readonly GameObjectFavorite _favoriteConfig;

        public FavoriteConfigPopup(GameObjectFavorite favoriteConfig)
        {
            _favoriteConfig = favoriteConfig;
        }

        public override void OnGUI(Rect rect)
        {
            // Intentionally left empty
        }

        // public override Vector2 GetWindowSize()
        // {
        //     return new Vector2(200, 100);
        // }

        public readonly UnityEvent<GameObjectFavorite> DeletedEvent = new UnityEvent<GameObjectFavorite>();
        public readonly UnityEvent<GameObjectFavorite> UpdatedEvent = new UnityEvent<GameObjectFavorite>();

        private FavoriteConfigPanel _favoriteConfigPanel;

        // private IVisualElementScheduledItem _task;

        public override void OnOpen()
        {
            _favoriteConfigPanel = new FavoriteConfigPanel(_favoriteConfig)
            {
                // style =
                // {
                //     height = Length.Percent(100),
                // }
            };
            _favoriteConfigPanel.DeletedEvent.AddListener(r =>
            {
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_RENDER_FAV
                Debug.Log($"delete button up pass {r.globalObjectIdString}");
#endif
                DeletedEvent.Invoke(r);
            });
            _favoriteConfigPanel.UpdatedEvent.AddListener(UpdatedEvent.Invoke);
            _favoriteConfigPanel.OnHeightChanged.AddListener(OnHeightChanged);

            editorWindow.rootVisualElement.Add(_favoriteConfigPanel);
            _favoriteConfigPanel.NeedCloseEvent.AddListener(hasChange =>
            {
                if(hasChange)
                {
                    EditorApplication.RepaintHierarchyWindow();
                }
                editorWindow.Close();
            });

            // _task = _favoriteConfigPanel.schedule.Execute(CheckFirstHeight).Every(150);

            // element.RegisterCallback<GeometryChangedEvent>(_ =>
            // {
            //     _height = element.resolvedStyle.height;
            //     editorWindow.Repaint();
            // });
        }

        // private void CheckFirstHeight()
        // {
        //     if (double.IsNaN(_favoriteConfigPanel.resolvedStyle.height))
        //     {
        //         return;
        //     }
        //     OnHeightChanged();
        //     _task.Pause();
        // }

        private void OnHeightChanged()
        {
            _height = _favoriteConfigPanel.Height;
            // Debug.Log($"set height to {_height}");

#if !UNITY_6000_0_OR_NEWER
            editorWindow.Repaint();
#endif
        }
    }
}
