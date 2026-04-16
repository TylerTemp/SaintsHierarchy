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
        private float _height = 100f;

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

        public override void OnOpen()
        {
            FavoriteConfigPanel element = new FavoriteConfigPanel(_favoriteConfig)
            {
                // style =
                // {
                //     height = Length.Percent(100),
                // }
            };
            element.DeletedEvent.AddListener(r =>
            {
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_RENDER_FAV
                Debug.Log($"delete button up pass {r.globalObjectIdString}");
#endif
                DeletedEvent.Invoke(r);
            });
            element.UpdatedEvent.AddListener(UpdatedEvent.Invoke);

            editorWindow.rootVisualElement.Add(element);
            element.NeedCloseEvent.AddListener(hasChange =>
            {
                if(hasChange)
                {
                    EditorApplication.RepaintHierarchyWindow();
                }
                editorWindow.Close();
            });

            element.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                _height = element.resolvedStyle.height;
                editorWindow.Repaint();
            });
        }

    }
}
