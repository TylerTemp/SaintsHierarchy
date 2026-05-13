using System;
using System.Collections.Generic;
using SaintsHierarchy.Editor.Utils;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.UIElement
{
    public readonly struct ColorPickerResult: IEquatable<ColorPickerResult>
    {
        public readonly bool HasColor;
        public readonly bool IsCustomColor;
        public readonly Color Color;

        public ColorPickerResult(bool hasColor, bool isCustomColor, Color color)
        {
            HasColor = hasColor;
            IsCustomColor = isCustomColor;
            Color = color;
        }

        public bool Equals(ColorPickerResult other)
        {
            return HasColor == other.HasColor && IsCustomColor == other.IsCustomColor && Color.Equals(other.Color);
        }

        public static bool operator ==(ColorPickerResult left, ColorPickerResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ColorPickerResult left, ColorPickerResult right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return obj is ColorPickerResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(HasColor, IsCustomColor, Color);
        }

        public bool Equals(ColorPickerResult x, ColorPickerResult y)
        {
            return x.HasColor == y.HasColor && x.IsCustomColor == y.IsCustomColor && x.Color.Equals(y.Color);
        }

        public int GetHashCode(ColorPickerResult obj)
        {
            return HashCode.Combine(obj.HasColor, obj.IsCustomColor, obj.Color);
        }

        public int CompareTo(ColorPickerResult other)
        {
            int hasColorComparison = HasColor.CompareTo(other.HasColor);
            if (hasColorComparison != 0) return hasColorComparison;
            return IsCustomColor.CompareTo(other.IsCustomColor);
        }
    }

#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class ColorPickerElement : BindableElement, INotifyValueChanged<ColorPickerResult>
    {
#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<ColorPickerElement, UxmlTraits> { }
#endif
        // [UxmlAttribute]
        // // ReSharper disable once UnassignedField.Global
        // // ReSharper disable once MemberCanBePrivate.Global
        // public bool NoDeleteButton;

        // public readonly UnityEvent<bool> NeedCloseEvent = new UnityEvent<bool>();
        private static VisualTreeAsset _gameObjectConfigTemplate;

        private static readonly Color[] Colors = {
            new Color(0.16f, 0.16f, 0.16f),
            new Color(0.609f, 0.231f, 0.23100014f),
            new Color(0.55825f, 0.471625f, 0.21175f),
            new Color(0.34999996f, 0.5075f, 0.1925f),
            new Color(0.1925f, 0.5075f, 0.27124998f),
            new Color(0.1925f, 0.50750005f, 0.5075f),
            new Color(0.259875f, 0.36618757f, 0.685125f),
            new Color(0.4550001f, 0.25024998f, 0.65975f),
            new Color(0.53287494f, 0.20212498f, 0.4501876f),
        };

        private static Texture2D _closeIcon;

        private ColorPickerResult _curValue;

        private readonly IReadOnlyList<(ItemButtonElement, Color)> _presentColorButtons;
        private readonly ColorField _colorField;

        public ColorPickerElement()
        {
            _gameObjectConfigTemplate ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/ColorPicker.uxml");
            TemplateContainer root = _gameObjectConfigTemplate.CloneTree();
            Add(root);

            VisualElement colorRow = root.Q<VisualElement>(name: "ColorContainer");

            // Debug.Log($"NoDeleteButton={NoDeleteButton}");

            VisualElement noColorButtonTemplate = colorRow.Q<VisualElement>(name: "CloseButtonTemplate");
            // if (NoDeleteButton)
            // {
            //     Debug.Log($"NoDeleteButton!");
            //     noColorButtonTemplate.RemoveFromHierarchy();
            // }
            // else
            // {
            Button noColorButton = noColorButtonTemplate.Q<Button>();
            noColorButton.tooltip = "Remove Color Config";
            noColorButton.clicked += () => value = new ColorPickerResult(false, true, default);
            // }

            List<(ItemButtonElement, Color)> colorButtons = new List<(ItemButtonElement, Color)>(Colors.Length);
            foreach (Color presetColor in Colors)
            {
                ItemButtonElement colorButton = MakeColorButton(presetColor);
                colorRow.Add(colorButton);

                // colorButton.Button.clicked += () => SetColor(true, false, presetColor);
                colorButton.Button.clicked += () => value = new ColorPickerResult(true, false, presetColor);
                colorButtons.Add((colorButton, presetColor));
            }

            _presentColorButtons = colorButtons;

            _colorField = colorRow.Q<ColorField>(name: "CustomColor");
            _colorField.tooltip = "Custom Color";
            // colorField.value = color ?? default;
            _colorField.RegisterValueChangedCallback(evt =>
            {
                Color newColor = evt.newValue;
                // SetColor(true, true, newColor);
                value = new ColorPickerResult(true, true, newColor);
            });

#if !UNITY_6000_3_OR_NEWER
            _colorField.style.width = 46;
            colorRow.Q<VisualElement>(name: "CustomColorIcon").style.display = DisplayStyle.None;
#endif
        }

        public void NoDeleteButton()
        {
            this.Q<VisualElement>(name: "CloseButtonTemplate").RemoveFromHierarchy();
        }


        private static Texture2D _whiteRectTexture;

        private static ItemButtonElement MakeColorButton(Color color)
        {
            _whiteRectTexture ??= Util.LoadResource<Texture2D>("rect.png");

            ItemButtonElement itemButtonElement = new ItemButtonElement();
            itemButtonElement.Button.style.backgroundImage = _whiteRectTexture;
            itemButtonElement.Button.style.unityBackgroundImageTintColor = color;
            return itemButtonElement;
        }

        public void SetValueWithoutNotify(ColorPickerResult newValue)
        {
            _curValue = newValue;
            bool foundExists = false;
            foreach ((ItemButtonElement buttonElement, Color presentColor)  in _presentColorButtons)
            {
                bool isPresent = presentColor == _curValue.Color;
                buttonElement.SetSelected(isPresent);
                if (isPresent)
                {
                    foundExists = true;
                }
            }

            if (!foundExists && _colorField.value != newValue.Color)
            {
                _colorField.SetValueWithoutNotify(_curValue.Color);
            }
        }


        public ColorPickerResult value
        {
            get => _curValue;
            set
            {
                if (_curValue == value)
                {
                    // Debug.Log($"no changes");
                    return;
                }

                ColorPickerResult previous = this.value;
                SetValueWithoutNotify(value);

                using ChangeEvent<ColorPickerResult> evt = ChangeEvent<ColorPickerResult>.GetPooled(previous, value);
                evt.target = this;
                SendEvent(evt);
            }
        }
    }
}
