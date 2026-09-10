using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaintsHierarchy.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor.Draw
{
    public static class HierarchyButtonDrawer
    {

        private static readonly Dictionary<RenderTargetInfo, IEnumerator> CachedCoroutines = new Dictionary<RenderTargetInfo, IEnumerator>();

        public static (bool buttonUsed, HierarchyUsed buttonHeaderUsed) Draw(Component target, HierarchyArea hierarchyArea, HierarchyButtonAttribute hierarchyButtonAttribute, RenderTargetInfo renderTargetInfo)
        {
            if (CachedCoroutines.TryGetValue(renderTargetInfo, out IEnumerator coroutine))
            {
                if (!coroutine.MoveNext())
                {
                    CachedCoroutines.Remove(renderTargetInfo);
                }
            }

            MethodInfo method = (MethodInfo)renderTargetInfo.MemberInfo;
            string friendlyName = ObjectNames.NicifyVariableName(method.Name);
            // string title;
            IReadOnlyList<RichTextChunk> titleChunks;
            if (string.IsNullOrEmpty(hierarchyButtonAttribute.Label))
            {
                titleChunks = new[]
                {
                    new RichTextChunk(content: friendlyName, iconColor: null, isIcon: false, rawContent: friendlyName),
                };
            }
            else
            {
                string rawTitle = hierarchyButtonAttribute.Label;

                if (hierarchyButtonAttribute.IsCallback)
                {
                    (string error, string result) = Util.GetOf<string>(rawTitle, null,
                        null, method, target, null);
                    if (error != "")
                    {
#if SAINTSFIELD_DEBUG
                        Debug.LogError(error);
#endif
                        return (false, default);
                    }

                    if (string.IsNullOrEmpty(result))
                    {
                        return (false, default);
                    }

                    rawTitle = result;
                }

                if(rawTitle.Contains("<field") || !CacheAndUtil.ParsedXmlCache.TryGetValue(rawTitle, out  titleChunks))
                {
                    // RichTextDrawer richTextDrawer = CacheAndUtil.GetCachedRichTextDrawer();
                    CacheAndUtil.ParsedXmlCache[rawTitle] = titleChunks =
                        RichTextDrawer.ParseRichXmlWithProvider(rawTitle, renderTargetInfo).ToArray();
                }
            }
            GUIContent oldLabel = new GUIContent(friendlyName);

            RichTextDrawer richTextDrawer = CacheAndUtil.GetCachedRichTextDrawer();

            float drawNeedWidth = richTextDrawer.GetWidth(oldLabel, hierarchyArea.Height, titleChunks);

            // GUIContent content = new GUIContent(title);
            // Vector2 size = GUI.skin.button.CalcSize(content);
            // float buttonWidth = size.x;
            Rect usedRect = hierarchyButtonAttribute.IsLeft
                ? hierarchyArea.MakeXWidthRect(hierarchyArea.GroupStartX, drawNeedWidth + 8)
                : hierarchyArea.MakeXWidthRect(hierarchyArea.GroupStartX - drawNeedWidth  - 8, drawNeedWidth + 8);

            Rect buttonRect = new Rect(usedRect)
            {
                x = usedRect.x + 2,
                width = usedRect.width - 4,
            };
            Rect labelRect = new Rect(buttonRect)
            {
                x = buttonRect.x + 2,
                width = buttonRect.width - 4,
            };

            GUIContent buttonContent = string.IsNullOrEmpty(hierarchyButtonAttribute.Tooltip)
                ? GUIContent.none
                : new GUIContent("", hierarchyButtonAttribute.Tooltip);

            GUIStyle style =
                    hierarchyButtonAttribute.IsGhost
                    ? EditorStyles.iconButton
                    : EditorStyles.miniButton;

            // ReSharper disable once InvertIf
            if (GUI.Button(buttonRect, buttonContent, style))
            {
                ParameterInfo[] methodParams = method.GetParameters();
                object[] methodPass = new object[methodParams.Length];
                // methodPass[0] = headerArea;
                for (int index = 0; index < methodParams.Length; index++)
                {
                    ParameterInfo param = methodParams[index];
                    object defaultValue = param.DefaultValue;
                    methodPass[index] = defaultValue;
                }

                // HeaderUsed methodReturn = default;
                object methodReturn = null;
                try
                {
                    methodReturn = method.Invoke(target, methodPass);
                }
                catch (Exception e)
                {
                    Debug.LogException(e.InnerException ?? e);
                }

                // Debug.Log($"methodReturn={methodReturn}");

                if (methodReturn is IEnumerator ie)
                {
                    CachedCoroutines[renderTargetInfo] = ie;
                }
            }

            richTextDrawer.DrawChunks(labelRect, oldLabel, titleChunks);

            return (true, new HierarchyUsed(usedRect));
        }

        public static void Update()
        {
            List<RenderTargetInfo> deleteKeys =
                new List<RenderTargetInfo>(CachedCoroutines.Count);
            foreach (KeyValuePair<RenderTargetInfo, IEnumerator> kv in CachedCoroutines)
            {
                if (!kv.Value.MoveNext())
                {
                    deleteKeys.Add(kv.Key);
                }
            }

            foreach (RenderTargetInfo deleteKey in deleteKeys)
            {
                CachedCoroutines.Remove(deleteKey);
            }
        }
    }
}
