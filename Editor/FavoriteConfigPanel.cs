using SaintsHierarchy.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor
{
    public class FavoriteConfigPanel : VisualElement
    {
        public readonly UnityEvent<bool> NeedCloseEvent = new UnityEvent<bool>();
        public readonly UnityEvent<GameObjectFavorite> DeletedEvent = new UnityEvent<GameObjectFavorite>();
        private static VisualTreeAsset _gameObjectConfigTemplate;

        private readonly GameObjectFavorite _favorite;

        public FavoriteConfigPanel(GameObjectFavorite favoriteConfig)
        {
            _favorite = favoriteConfig;

            _gameObjectConfigTemplate ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/FavoriteConfig.uxml");
            TemplateContainer root = _gameObjectConfigTemplate.CloneTree();
            root.style.height = Length.Percent(100);
            Add(root);

            root.Q<Button>(name: "deleteButton").clicked += OnDeleteButton;
        }

        private void OnDeleteButton()
        {
            IConfig config = Util.GetFavoriteConfig();
            EditorUtility.SetDirty((Object) config);
            config.favorites.RemoveAll(each => each.globalObjectIdString == _favorite.globalObjectIdString);
            config.SaveToDisk();

#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_RENDER_FAV
            Debug.Log($"delete button processed {_favorite.globalObjectIdString}");
#endif
            DeletedEvent.Invoke(_favorite);
            NeedCloseEvent.Invoke(true);
        }
    }
}
