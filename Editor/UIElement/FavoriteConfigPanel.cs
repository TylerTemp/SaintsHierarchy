using SaintsHierarchy.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.UIElement
{
    public class FavoriteConfigPanel : VisualElement
    {
        public readonly UnityEvent<bool> NeedCloseEvent = new UnityEvent<bool>();
        public readonly UnityEvent<GameObjectFavorite> DeletedEvent = new UnityEvent<GameObjectFavorite>();
        public readonly UnityEvent<GameObjectFavorite> UpdatedEvent = new UnityEvent<GameObjectFavorite>();
        private static VisualTreeAsset _gameObjectConfigTemplate;

        private readonly GameObjectFavorite _favorite;
        private readonly TextField _aliasField;

        private readonly EnumField _colorTypeField;
        private readonly ColorPickerElement _colorPickerElement;

        private readonly EnumField _iconTypeField;
        private readonly IconPickerElement _iconPickerElement;

        public const float DefaultHeight = 600f;
        public float Height { get; private set; } = DefaultHeight;
        public readonly UnityEvent OnHeightChanged = new UnityEvent();
        private readonly VisualElement _root;

        public FavoriteConfigPanel(GameObjectFavorite favoriteConfig)
        {
            _favorite = favoriteConfig;

            _gameObjectConfigTemplate ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/FavoriteConfig.uxml");
            TemplateContainer root = _gameObjectConfigTemplate.CloneTree();
            // root.style.height = Length.Percent(100);

            _aliasField = root.Q<TextField>("aliasInput");
            _aliasField.value = _favorite.alias ?? string.Empty;
            _aliasField.RegisterCallback<KeyDownEvent>(OnAliasKeyDown, TrickleDown.TrickleDown);

            _colorPickerElement = root.Q<ColorPickerElement>();
            _colorPickerElement.NoDeleteButton();
            _colorPickerElement.value = new ColorPickerResult(favoriteConfig.colorType == GameObjectFavoriteColorType.CustomColor, false, favoriteConfig.color);

            _colorTypeField = root.Q<EnumField>("colorType");
            _colorTypeField.RegisterValueChangedCallback(evt =>
                GameObjectFavoriteColorTypeChanged((GameObjectFavoriteColorType)evt.newValue));
            _colorTypeField.value = _favorite.colorType;

            _iconPickerElement = root.Q<IconPickerElement>();
            _iconPickerElement.value = favoriteConfig.icon;

            _iconTypeField = root.Q<EnumField>("iconType");
            // _iconTypeField.Init(_favorite.iconType);
            _iconTypeField.RegisterValueChangedCallback(evt =>
                GameObjectFavoriteIconTypeChanged((GameObjectFavoriteIconType)evt.newValue));
            _iconTypeField.value = _favorite.iconType;

            root.Q<Button>(name: "saveButton").clicked += Save;
            root.Q<Button>(name: "deleteButton").clicked += OnDeleteButton;

            ScrollView scrollView = new ScrollView();
            scrollView.Add(root);
            Add(scrollView);

            _root = root;

            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
            RegisterCallback<GeometryChangedEvent>(GeometryChanged);

            GameObjectFavoriteColorTypeChanged(_favorite.colorType);
            GameObjectFavoriteIconTypeChanged(_favorite.iconType);
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
            float curHeight = _root.resolvedStyle.height;
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
            schedule.Execute(RefreshHeight).StartingIn(150);
        }

        private void GameObjectFavoriteColorTypeChanged(GameObjectFavoriteColorType colorType)
        {
            bool display = colorType == GameObjectFavoriteColorType.CustomColor;
            _colorPickerElement.style.display = display ? DisplayStyle.Flex : DisplayStyle.None;
            schedule.Execute(RefreshHeight).StartingIn(150);
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

            updatedFavorite.colorType = _colorTypeField.value is GameObjectFavoriteColorType colorType
                ? colorType
                : updatedFavorite.colorType;
            updatedFavorite.color = _colorPickerElement.value.Color;

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
