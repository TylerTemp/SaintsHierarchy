using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SaintsHierarchy.Editor.Draw;
using SaintsHierarchy.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.Utils
{
    public class RichTextDrawer
    {
        // cache
        private struct TextureCacheKey : IEquatable<TextureCacheKey>
        {
            public string ColorPresent;
            public string IconPath;
            // public bool IsEditorResource;

            public override bool Equals(object obj)
            {
                // ReSharper disable once UseNegatedPatternInIsExpression
                if (!(obj is TextureCacheKey other))
                {
                    return false;
                }

                return ColorPresent == other.ColorPresent && IconPath == other.IconPath;
            }

            public override int GetHashCode()
            {
                if (ColorPresent == null)
                {
                    return IconPath != null ? IconPath.GetHashCode() : 0;
                }

                return HashCode.Combine(ColorPresent, IconPath);
            }

            public bool Equals(TextureCacheKey other)
            {
                return ColorPresent == other.ColorPresent && IconPath == other.IconPath;
            }
        }

        private readonly Dictionary<TextureCacheKey, Texture2D> _textureCache = new Dictionary<TextureCacheKey, Texture2D>();

        public void Dispose()
        {
            foreach (Texture2D cacheValue in _textureCache.Values)
            {
                UnityEngine.Object.DestroyImmediate(cacheValue);
            }
            _textureCache.Clear();
        }

        public static (string error, string xml) GetLabelXml(SerializedProperty property, string richTextXml, bool isCallback, FieldInfo fieldInfo, object target)
        {
            if (!isCallback)
            {
                return ("", richTextXml);
            }

            (string error, string result) = Util.GetOf(richTextXml, "", property, fieldInfo, target, null);
            if (error != "")
            {
                string originalName;
                try
                {
                    originalName = property.displayName;
                }
                catch(InvalidOperationException e)
                {
                    return (e.Message, "");
                }

                return (error, originalName);
            }

            return ("", result);
        }

        public struct EmptyRichTextTagProvider: IRichTextTagProvider
        {
            public string GetLabel() => "";

            public string GetContainerType() => "";

            public string GetContainerTypeBaseType() => "";

            public string GetIndex(string formatter) => "";

            public string GetField(string rawContent, string tagName, string tagValue) => "";
        }

        public static IEnumerable<RichTextChunk> ParseRichXmlWithProvider(string richXml, IRichTextTagProvider provider)
        {
            if (string.IsNullOrEmpty(richXml))
            {
                yield break;
            }
            List<RuntimeUtil.RichTextParsedChunk> openTag = new List<RuntimeUtil.RichTextParsedChunk>();
            // List<RuntimeUtil.RichTextParsedChunk> acc = new List<RuntimeUtil.RichTextParsedChunk>();
            StringBuilder richText = new StringBuilder();
            foreach (RuntimeUtil.RichTextParsedChunk richTextParsedChunk in RuntimeUtil.ParseRichXml(richXml))
            {
                // Debug.Log($"get parsed chunk {richTextParsedChunk}");
                switch (richTextParsedChunk.ChunkType)
                {
                    case RuntimeUtil.ChunkType.NormalTag:
                    {
                        bool removed = false;
                        bool isStartTag = richTextParsedChunk.TagType == RuntimeUtil.TagType.StartTag;
                        if (isStartTag)
                        {
                            openTag.Add(richTextParsedChunk);
                        }
                        else
                        {
                            // ReSharper disable once UseIndexFromEndExpression
                            if (openTag.Count > 0 && openTag[openTag.Count - 1].TagName == richTextParsedChunk.TagName)
                            {
                                removed = true;
                                openTag.RemoveAt(openTag.Count - 1);
                            }
                        }

                        switch (richTextParsedChunk.TagName)
                        {
                            case "color":
                            {
                                if(isStartTag)
                                {
                                    string colorHtml =
                                        Colors.ToHtmlHexString(
                                            Colors.GetColorByStringPresent(richTextParsedChunk.TagValue));
                                    richText.Append($"<color={colorHtml}>");
                                }
                                else
                                {
                                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                                    if (removed)
                                    {
                                        richText.Append("</color>");
                                    }
                                    else // invalid
                                    {
                                        richText.Append(richTextParsedChunk.RawContent);
                                    }
                                }
                            }
                                break;
                            case "label":
                                richText.Append(provider.GetLabel());
                                break;
                            case "container.Type":
                                richText.Append(provider.GetContainerType());
                                break;
                            case "container.Type.BaseType":
                                richText.Append(provider.GetContainerTypeBaseType());
                                break;
                            case "index":
                                richText.Append(provider.GetIndex(richTextParsedChunk.TagValue));
                                break;
                            case "icon":
                                break;
                            default:
                            {
                                // Debug.Log(parsedResult.content);
                                if (richTextParsedChunk.RawContent != null && (richTextParsedChunk.TagName == "field" ||
                                                                               richTextParsedChunk.TagName.StartsWith("field.")))
                                {
                                    richText.Append(provider.GetField(richTextParsedChunk.RawContent, richTextParsedChunk.TagName,
                                        richTextParsedChunk.TagValue));
                                }
                                else
                                {
                                    richText.Append(richTextParsedChunk.RawContent);
                                }
                            }
                                break;
                        }

                        // Debug.Log($"add tag {richTextParsedChunk}");

                        // acc.Add(richTextParsedChunk);
                        break;
                    }
                    case RuntimeUtil.ChunkType.IconTag:
                    {
                        string richTextFull = richText.ToString();
                        if (richTextFull != "")
                        {
                            yield return new RichTextChunk(richTextFull, false, richTextFull);
                        }
                        richText.Clear();

                        // Debug.Log($"parse icon {richTextParsedChunk}");

                        RichTextChunk wrapIcon = new RichTextChunk(
                            richTextParsedChunk.RawContent,
                            true,
                            richTextParsedChunk.TagValue,
                            richTextParsedChunk.IconColor);
                        // Debug.Log($"add icon {wrapIcon}");
                        // acc.Add(richTextParsedChunk);
                        yield return wrapIcon;
                        break;
                    }
                    case RuntimeUtil.ChunkType.Text:
                        richText.Append(richTextParsedChunk.RawContent);
                        break;
                }
            }

            string richTextFinal = richText.ToString();
            if (richTextFinal != "")
            {
                yield return new RichTextChunk(richTextFinal, false, richTextFinal);
            }
        }

        public static string TagStringFormatter(object finalValue, string parsedResultValue)
        {
            if (RuntimeUtil.IsNull(finalValue))
            {
                // ReSharper disable once TailRecursiveCall
                return TagStringFormatter("", parsedResultValue);
            }

            if (string.IsNullOrEmpty(parsedResultValue))
            {
                return $"{finalValue}";
            }

            if (parsedResultValue.Contains("{") && parsedResultValue.Contains("}"))
            {
                try
                {
                    return string.Format(parsedResultValue, finalValue);
                }
#pragma warning disable CS0168
                catch (Exception ex)
#pragma warning restore CS0168
                {
#if SAINTSFIELD_DEBUG
                    Debug.LogWarning(ex);
#endif
                    return $"{finalValue}";
                }
            }

            if (parsedResultValue.StartsWith("B"))
            {
                string binaryFormatResult = Util.FormatBinary(parsedResultValue, finalValue);
                // Debug.Log($"{parsedResult.value}/{finalValue}/{binaryFormatResult}");
                if (binaryFormatResult != "")
                {
                    return binaryFormatResult;
                }
            }

            string formatString = $"{{0:{parsedResultValue}}}";
            try
            {
                return string.Format(formatString, finalValue);
            }
#pragma warning disable CS0168
            catch (Exception ex)
#pragma warning restore CS0168
            {
#if SAINTSFIELD_DEBUG
                // Debug.LogException(ex);
#endif
                return $"{finalValue}";
            }
        }

        public float GetWidth(GUIContent oldLabel, float height, IEnumerable<RichTextChunk> payloads)
        {
            GUIStyle textStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
            };

            float totalWidth = 0;

            foreach(RichTextChunk curChunk in payloads)
            {
                // RichTextChunk curChunk = parsedChunk[0];
                // parsedChunk.RemoveAt(0);

                // Debug.Log($"parsedChunk={curChunk}");
                if (curChunk.IsIcon)
                {
                    TextureCacheKey cacheKey = new TextureCacheKey
                    {
                        ColorPresent = curChunk.IconColor,
                        IconPath = curChunk.Content,
                    };
                    Texture texture = GetTexture2D(cacheKey, curChunk, height);
                    float curWidth = texture.height > 0
                        ? texture.width
                        : height;

                    totalWidth += curWidth;
                }
                else
                {
                    GUIContent curGUIContent = new GUIContent(oldLabel)
                    {
                        text = curChunk.Content,
                        image = null,
                    };
                    totalWidth += textStyle.CalcSize(curGUIContent).x;
                }
            }
            return totalWidth;
        }

        public void DrawChunks(Rect position, GUIContent oldLabel, IEnumerable<RichTextChunk> payloads)
        {
            Rect labelRect = position;
            // List<RichTextChunk> parsedChunk = payloads.ToList();

            // Debug.Log($"parsedChunk.Count={parsedChunk.Count}");

            GUIStyle textStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
            };

            foreach(RichTextChunk curChunk in payloads)
            {
                // RichTextChunk curChunk = parsedChunk[0];
                // parsedChunk.RemoveAt(0);

                // Debug.Log($"parsedChunk={curChunk}");
                GUIContent curGUIContent;
                float curWidth;
                if (curChunk.IsIcon)
                {
                    TextureCacheKey cacheKey = new TextureCacheKey
                    {
                        ColorPresent = curChunk.IconColor,
                        IconPath = curChunk.Content,
                    };
                    if (!_textureCache.TryGetValue(cacheKey, out Texture2D texture) || texture == null)
                    {
                        texture = Tex.TextureTo(
                            Util.LoadResource<Texture2D>(curChunk.Content),
                            Colors.GetColorByStringPresent(curChunk.IconColor),
                            -1,
                            Mathf.FloorToInt(position.height)
                        );
                        if (texture.width != 1 && texture.height != 1)
                        {
                            _textureCache[cacheKey] = texture;
                        }
                    }

                    curGUIContent = new GUIContent(oldLabel)
                    {
                        text = null,
                        image = texture,
                    };
                    curWidth = texture.width;
                }
                else
                {
                    curGUIContent = new GUIContent(oldLabel)
                    {
                        text = curChunk.Content,
                        image = null,
                    };
                    curWidth = textStyle.CalcSize(curGUIContent).x;
                }

                (Rect textRect, Rect leftRect) = RectUtils.SplitWidthRect(labelRect, curWidth);
                // GUI.Label(textRect, curGUIContent, textStyle);
                EditorGUI.LabelField(textRect, curGUIContent, textStyle);
                if (leftRect.width <= 0)
                {
                    return;
                }

                labelRect = leftRect;
            }
        }

        public const float ImageWidth = 20;
        // public const float ImageWidth = EditorGUIUtility.SingleLineHeight;

#if UNITY_2021_3_OR_NEWER
        public IEnumerable<VisualElement> DrawChunksUIToolKit(IEnumerable<RichTextChunk> payloads)
        {
            foreach(RichTextChunk curChunk in payloads)
            {
                // Debug.Log(curChunk);
                if (!curChunk.IsIcon)
                {
                    yield return new Label(curChunk.Content)
                    {
                        style =
                        {
                            flexShrink = 0,
                            unityTextAlign = TextAnchor.UpperLeft,
                            paddingLeft = 0,
                            paddingRight = 0,
                            whiteSpace = WhiteSpace.Normal,
                        },
                        pickingMode = PickingMode.Ignore,
                    };
                }
                else
                {
                    TextureCacheKey cacheKey = new TextureCacheKey
                    {
                        ColorPresent = "",
                        IconPath = curChunk.Content,
                    };

                    if (!_textureCache.TryGetValue(cacheKey, out Texture2D texture) || texture == null)
                    {
                        texture = Util.LoadResource<Texture2D>(curChunk.Content);
                        if (texture != null && texture.width != 1 && texture.height != 1)
                        {
                            _textureCache[cacheKey] = texture;
                        }
                    }

                    // Image img = new Image
                    // {
                    //     image = texture,
                    //     scaleMode = ScaleMode.ScaleToFit,
                    //     tintColor = Colors.GetColorByStringPresent(curChunk.IconColor),
                    //     pickingMode = PickingMode.Ignore,
                    //     style =
                    //     {
                    //         flexShrink = 0,
                    //         // marginTop = 2,
                    //         // marginBottom = 2,
                    //         // paddingLeft = 1,
                    //         // paddingRight = 1,
                    //         maxHeight = 15,
                    //         alignSelf = Align.Center,
                    //         width = ImageWidth,
                    //         height = SaintsPropertyDrawer.SingleLineHeight - 2,
                    //     },
                    // };
                    VisualElement img = new VisualElement
                    {
                        pickingMode = PickingMode.Ignore,
                        style =
                        {
                            backgroundImage = texture,
                            unityBackgroundImageTintColor = Colors.GetColorByStringPresent(curChunk.IconColor),
#if UNITY_2022_2_OR_NEWER
                            backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center),
                            backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center),
                            backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat),
                            backgroundSize = new BackgroundSize(BackgroundSizeType.Contain),
#else
                            unityBackgroundScaleMode = ScaleMode.ScaleToFit,
#endif

                            flexShrink = 0,
                            // marginTop = 2,
                            // marginBottom = 2,
                            // paddingLeft = 1,
                            // paddingRight = 1,
                            maxHeight = 15,
                            alignSelf = Align.Center,
                            width = ImageWidth,
                            height = 18,
                        },
                    };
                    // img.style.flexShrink = 0;

#if EXT_INSPECTOR_LOG
                    Debug.Log($"#draw# icon <{curChunk.Content} {curChunk.IconColor}/>");
#endif
                    yield return img;
                }
            }
        }
#endif

        private Texture2D GetTexture2D(TextureCacheKey cacheKey, RichTextChunk curChunk, float height)
        {
            if (_textureCache.TryGetValue(cacheKey, out Texture2D texture) && texture != null)
            {
                return texture;
            }

            texture = Tex.TextureTo(
                Util.LoadResource<Texture2D>(curChunk.Content),
                Colors.GetColorByStringPresent(curChunk.IconColor),
                -1,
                Mathf.FloorToInt(height)
            );
            if (texture.width != 1 && texture.height != 1)
            {
                _textureCache[cacheKey] = texture;
            }

            return texture;
        }

// #if UNITY_2021_3_OR_NEWER
//         public static float TextLengthUIToolkit(TextElement calculator, string origin)
//         {
//             // float spaceWidth = calculator.MeasureTextSize(" ", 0, VisualElement.MeasureMode.Undefined, 100, VisualElement.MeasureMode.Undefined).x;
//             // float textWidth = calculator.MeasureTextSize(original, 0, VisualElement.MeasureMode.Undefined, 100, VisualElement.MeasureMode.Undefined).x;
//             // int spaceCount = Mathf.CeilToInt(textWidth / spaceWidth);
//             // return new string(' ', spaceCount);
//
//             return calculator.MeasureTextSize(origin, 0, VisualElement.MeasureMode.Undefined, 100, VisualElement.MeasureMode.Undefined).x;
//
//         }
// #endif
    }
}
