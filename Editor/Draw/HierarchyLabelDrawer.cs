using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaintsHierarchy.Editor.Utils;
using SaintsHierarchy.Utils;
using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor.Draw
{
    public static class HierarchyLabelDrawer
    {
        private static readonly Dictionary<string, IReadOnlyList<RichTextChunk>> ParsedXmlCache = new Dictionary<string, IReadOnlyList<RichTextChunk>>();

        private static string GetLabelName(object target, RenderTargetInfo renderTargetInfo)
        {
            switch (renderTargetInfo.MemberType)
            {
                case MemberType.Method:
                {
                    MethodInfo method = (MethodInfo)renderTargetInfo.MemberInfo;
                    return ObjectNames.NicifyVariableName(method.Name);
                }
                case MemberType.Field:
                {
                    FieldInfo field = (FieldInfo)renderTargetInfo.MemberInfo;
                    return ObjectNames.NicifyVariableName(field.Name);
                }
                case MemberType.Property:
                {
                    PropertyInfo property = (PropertyInfo)renderTargetInfo.MemberInfo;
                    return ObjectNames.NicifyVariableName(property.Name);
                }
                default:
                    return "";
            }
        }

        public static (bool used, HierarchyUsed headerUsed) Draw(object target, HierarchyArea headerArea, HierarchyLabelAttribute headerLabelAttribute, RenderTargetInfo renderTargetInfo)
        {
            string rawLabel;
            string labelName = GetLabelName(target, renderTargetInfo);

            if (string.IsNullOrEmpty(headerLabelAttribute.Label))
            {
                switch (renderTargetInfo.MemberType)
                {
                    case MemberType.Method:
                    {
                        MethodInfo method = (MethodInfo)renderTargetInfo.MemberInfo;
                        ParameterInfo[] methodParams = method.GetParameters();
                        object[] methodPass = new object[methodParams.Length];
                        // methodPass[0] = headerArea;
                        for (int index = 0; index < methodParams.Length; index++)
                        {
                            ParameterInfo param = methodParams[index];
                            object defaultValue = param.DefaultValue;
                            methodPass[index] = defaultValue;
                        }

                        object returnValue;
                        try
                        {
                            returnValue = method.Invoke(target, methodPass);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e.InnerException ?? e);
                            return (false, default);
                        }

                        rawLabel = GetStringFromResult(returnValue);

                    }
                        break;
                    case MemberType.Field:
                    {
                        FieldInfo field = (FieldInfo)renderTargetInfo.MemberInfo;
                        object returnResult = field.GetValue(target);
                        rawLabel = GetStringFromResult(returnResult);

                    }
                        break;
                    case MemberType.Property:
                    {
                        PropertyInfo property = (PropertyInfo)renderTargetInfo.MemberInfo;
                        object returnResult;
                        try
                        {
                            returnResult = property.GetValue(target);
                        }
                        catch (Exception e)
                        {
    #if SAINTSFIELD_DEBUG
                            Debug.LogException(e.InnerException ?? e);
    #endif
                            return (false, default);
                        }

                        rawLabel = GetStringFromResult(returnResult);

                    }
                        break;
                    default:
                        return (false, default);
                }
            }
            else if(headerLabelAttribute.IsCallback)
            {
                (string error, object result) = Util.GetOf<object>(headerLabelAttribute.Label, null, null, renderTargetInfo.MemberInfo, target, null);
                if (error != "")
                {
#if SAINTSFIELD_DEBUG
                    Debug.LogError(error);
#endif
                    return (false, default);
                }
                rawLabel = GetStringFromResult(result);
            }
            else
            {
                rawLabel = headerLabelAttribute.Label;
            }

            if (string.IsNullOrEmpty(rawLabel))
            {
                return (false, default);
            }

            if(rawLabel.Contains("<field") || !ParsedXmlCache.TryGetValue(rawLabel, out IReadOnlyList<RichTextChunk> labelChunks))
            {
                ParsedXmlCache[rawLabel] = labelChunks =
                    RichTextDrawer.ParseRichXmlWithProvider(rawLabel, renderTargetInfo).ToArray();
            }

            RichTextDrawer richTextDrawer = CacheAndUtil.GetCachedRichTextDrawer();
            GUIContent oldLabel = new GUIContent(labelName) { tooltip = headerLabelAttribute.Tooltip };
            float drawNeedWidth = richTextDrawer.GetWidth(oldLabel, headerArea.Height, labelChunks);

            float labelWidth = drawNeedWidth + 4;

            Rect usedRect = headerLabelAttribute.IsLeft
                ? headerArea.MakeXWidthRect(headerArea.GroupStartX, labelWidth)
                : headerArea.MakeXWidthRect(headerArea.GroupStartX - labelWidth, labelWidth);
            Rect labelRect = new Rect(usedRect)
            {
                x = usedRect.x + 2,
                width = usedRect.width - 4,
            };

            if (labelChunks.Count == 0)
            {
                return (false, default);
            }

            richTextDrawer.DrawChunks(labelRect, oldLabel, labelChunks);

            return (true, new HierarchyUsed(usedRect));
        }

        private static string GetStringFromResult(object returnValue)
        {
            if (RuntimeUtil.IsNull(returnValue))
            {
                return null;
            }

            if (returnValue is string stringValue)
            {
                return stringValue;
            }

            return returnValue.ToString();
        }
    }
}
