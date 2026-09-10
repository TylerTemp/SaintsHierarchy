using SaintsHierarchy.Editor.Core;
using SaintsHierarchy.Editor.Core.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    [UxmlElement]
    public partial class FavoriteButton : Button
    {
        private static VisualTreeAsset _template;
        private readonly Image _icon;
        private readonly Label _label;
        private bool _isGhost;
        private ValueAnimation<Vector2> _reorderAnimation;
        private Vector2 _reorderOffset;

        public bool AnimateReordering { get; set; }

        [UxmlAttribute]
        public bool IsGhost
        {
            get => _isGhost;
            set
            {
                _isGhost = value;
                if (value)
                {
                    StopReorderAnimation();
                }
                EnableInClassList("favorite-ghost", value);
                pickingMode = value ? PickingMode.Ignore : PickingMode.Position;
                focusable = !value;
                SetEnabled(!value);
            }
        }

        public FavoriteButton()
        {
            _template ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/Favorite/FavoriteButton.uxml");
            _template.CloneTree(this);
            _icon = this.Q<Image>("favorite-icon");
            _label = this.Q<Label>("favorite-label");
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<DetachFromPanelEvent>(_ => StopReorderAnimation());
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!AnimateReordering || IsGhost || evt.oldRect.width <= 0 || evt.oldRect.height <= 0 ||
                evt.oldRect.position == evt.newRect.position)
            {
                return;
            }

            // Layout moves immediately; translate back to the current visual position, then slide
            // to the new slot. Include the active offset to avoid jumps during rapid reordering.
            Vector2 offset = evt.oldRect.position - evt.newRect.position + _reorderOffset;
            _reorderAnimation?.Stop();
            _reorderOffset = offset;
            style.translate = new Translate(offset.x, offset.y);
            _reorderAnimation = experimental.animation.Start(offset, Vector2.zero, 150, (element, value) =>
            {
                _reorderOffset = value;
                element.style.translate = new Translate(value.x, value.y);
            }).Ease(Easing.OutCubic).OnCompleted(() => _reorderAnimation = null);
        }

        private void StopReorderAnimation()
        {
            _reorderAnimation?.Stop();
            _reorderAnimation = null;
            _reorderOffset = Vector2.zero;
            style.translate = StyleKeyword.Null;
        }

        public FavoriteButton(GameObject go, GameObjectFavorite favorite) : this()
        {
            SetFavorite(go, favorite);
        }

        public void SetFavorite(GameObject go, GameObjectFavorite favorite)
        {
            tooltip = go == null ? string.Empty : go.name;
            _label.text = string.IsNullOrEmpty(favorite.alias) ? tooltip : favorite.alias;
            _icon.image = null;
            _icon.style.display = DisplayStyle.None;
            _icon.style.backgroundColor = StyleKeyword.Null;
            _label.style.borderBottomWidth = 0;
            _label.style.borderBottomColor = StyleKeyword.Null;
            if (go == null)
            {
                return;
            }

            GameObjectConfig goConfig = Util.GetGameObjectConfig(go).config;
            Texture2D unityIcon = EditorGUIUtility.GetIconForObject(go);
            (bool underline, Color underlineColor) = unityIcon == null ? (false, default) : Util.GetUnderline(unityIcon.name);
            Texture2D defaultIcon = unityIcon;
            Util.PrefabIconInfo prefab = Util.GetPrefabIconInfo(go);
            if (defaultIcon == null)
            {
                IConfig config = Util.GetUsingConfig();
                defaultIcon = prefab.IsInstanceRoot ? prefab.Icon
                    : config.noDefaultIcon || config.transparentDefaultIcon ? null
                    : EditorGUIUtility.IconContent("d_GameObject Icon").image as Texture2D;
            }
            Texture2D icon = favorite.iconType switch
            {
                GameObjectFavoriteIconType.None => null,
                GameObjectFavoriteIconType.Custom => Util.LoadResource<Texture2D>(favorite.icon),
                GameObjectFavoriteIconType.UnityDefault => underline ? null : defaultIcon,
                _ => !string.IsNullOrEmpty(goConfig.icon) ? Util.LoadResource<Texture2D>(goConfig.icon)
                    : prefab.IsMissingPrefab ? prefab.Icon
                    : Util.GetIconByComponent(go.GetComponents<Component>()) ?? (underline ? null : defaultIcon),
            };
            bool hasColor = favorite.colorType == GameObjectFavoriteColorType.CustomColor ||
                            favorite.colorType == GameObjectFavoriteColorType.Default && goConfig.hasColor;
            _icon.image = icon;
            _icon.style.display = icon != null || hasColor ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasColor)
            {
                _icon.style.backgroundColor = favorite.colorType == GameObjectFavoriteColorType.CustomColor
                    ? favorite.color : goConfig.color;
            }
            if (underline)
            {
                _label.style.borderBottomWidth = 1;
                _label.style.borderBottomColor = underlineColor;
            }
        }
    }
}
