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
        public readonly UnityEvent<GameObjectFavorite> UpdatedEvent = new UnityEvent<GameObjectFavorite>();
        private static VisualTreeAsset _gameObjectConfigTemplate;

        private readonly GameObjectFavorite _favorite;
        private readonly TextField _aliasField;
        private readonly EnumField _iconTypeField;
        private readonly IconPickerElement _iconPickerElement;

        public const float DefaultHeight = 600f;
        public float Height { get; private set; } = DefaultHeight;
        public readonly UnityEvent OnHeightChanged = new UnityEvent();

        public FavoriteConfigPanel(GameObjectFavorite favoriteConfig)
        {
            _favorite = favoriteConfig;

            _gameObjectConfigTemplate ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/FavoriteConfig.uxml");
            TemplateContainer root = _gameObjectConfigTemplate.CloneTree();
            // root.style.height = Length.Percent(100);
            Add(root);

            _aliasField = root.Q<TextField>("aliasInput");
            _aliasField.value = _favorite.alias ?? string.Empty;
            _aliasField.RegisterCallback<KeyDownEvent>(OnAliasKeyDown, TrickleDown.TrickleDown);

            _iconPickerElement = root.Q<IconPickerElement>();
            _iconTypeField = root.Q<EnumField>("iconType");
            // _iconTypeField.Init(_favorite.iconType);
            _iconTypeField.RegisterValueChangedCallback(evt =>
                GameObjectFavoriteIconTypeChanged((GameObjectFavoriteIconType)evt.newValue));
            _iconTypeField.value = _favorite.iconType;
            GameObjectFavoriteIconTypeChanged(_favorite.iconType);

            root.Q<Button>(name: "saveButton").clicked += Save;
            root.Q<Button>(name: "deleteButton").clicked += OnDeleteButton;

            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
            RegisterCallback<GeometryChangedEvent>(GeometryChanged);
        }

        private void GeometryChanged(GeometryChangedEvent evt)
        {
            RefreshHeight();
        }

        private void AttachToPanel(AttachToPanelEvent evt)
        {
            _aliasField.Focus();
            RefreshHeight();
        }

        private void RefreshHeight()
        {
            float curHeight = resolvedStyle.height;
            if (double.IsNaN(curHeight))
            {
                return;
            }
            Height = curHeight;
            OnHeightChanged.Invoke();
        }

        private void GameObjectFavoriteIconTypeChanged(GameObjectFavoriteIconType iconType)
        {
            bool display = iconType == GameObjectFavoriteIconType.Custom;
            _iconPickerElement.style.display = display ? DisplayStyle.Flex : DisplayStyle.None;
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

        private void OnAliasKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
// #if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_CONFIG_FAV
//                 Debug.Log("Key Down!");
// #endif
                Save();
                evt.StopPropagation();
            }
        }

        private void Save()
        {
            IConfig config = Util.GetFavoriteConfig();
            int foundIndex = config.favorites.FindIndex(each => each.globalObjectIdString == _favorite.globalObjectIdString);
            if (foundIndex == -1)
            {
                Debug.LogWarning($"config not found for {_favorite.globalObjectIdString}");
                NeedCloseEvent.Invoke(false);
                return;
            }

            GameObjectFavorite updatedFavorite = config.favorites[foundIndex];
            updatedFavorite.alias = _aliasField.value ?? string.Empty;
            updatedFavorite.iconType = _iconTypeField.value is GameObjectFavoriteIconType iconType
                ? iconType
                : updatedFavorite.iconType;
            updatedFavorite.icon = _iconPickerElement.value;
            config.favorites[foundIndex] = updatedFavorite;

#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_CONFIG_FAV
            Debug.Log($"updatedFavorite.alias={updatedFavorite.alias}; iconType={updatedFavorite.iconType}; icon={updatedFavorite.icon}");
#endif

            EditorUtility.SetDirty((Object) config);
            config.SaveToDisk();
            UpdatedEvent.Invoke(updatedFavorite);

            NeedCloseEvent.Invoke(true);
        }
    }
}
