using System;
using System.Linq;
using System.Reflection;
using SaintsHierarchy.Editor.Core.Draw;
using SaintsHierarchy.Editor.Core.Utils;
using SaintsHierarchy.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI.Renderer
{
    public class LabelRenderer : VisualElement
    {
        private static readonly RichTextDrawer TextDrawer = new RichTextDrawer();
        private readonly Component _target;
        private readonly RenderTargetInfo _info;
        public readonly UnityEvent<bool> OnVisibilityChanged = new UnityEvent<bool>();
        private RichTextChunk[] _previous;

        public LabelRenderer(Component target, RenderTargetInfo info)
        {
            _target = target;
            _info = info;
            tooltip = info.Attribute is HierarchyButtonAttribute button
                ? button.Tooltip
                : ((HierarchyLabelAttribute)info.Attribute).Tooltip;
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.flexShrink = 0;
            style.marginLeft = style.marginRight = 2;
            style.marginTop = style.marginBottom = 0;
            style.paddingLeft = style.paddingRight = 2;
            style.paddingTop = style.paddingBottom = 0;

            Refresh();
            // Element schedules pause automatically while detached.
            schedule.Execute(Refresh).Every(100);
        }

        private void Refresh()
        {
            RichTextChunk[] chunks;
            try
            {
                string text = _target == null ? null : GetText();
                chunks = string.IsNullOrEmpty(text)
                    ? Array.Empty<RichTextChunk>()
                    : RichTextDrawer.ParseRichXmlWithProvider(text, _info).ToArray();
            }
            catch (Exception)
            {
                // Match legacy's failed callbacks without breaking the hierarchy row.
                chunks = Array.Empty<RichTextChunk>();
            }
            if (_previous != null && _previous.SequenceEqual(chunks))
            {
                return;
            }
            _previous = chunks;
            Clear();
            bool show = chunks.Length != 0;
            style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            OnVisibilityChanged.Invoke(show);
            foreach (VisualElement chunk in TextDrawer.DrawChunksUIToolKit(chunks))
            {
                if (chunk is Label label)
                {
                    label.enableRichText = true;
                    label.style.whiteSpace = WhiteSpace.NoWrap;
                    label.style.marginTop = label.style.marginBottom = 0;
                }
                Add(chunk);
            }
        }

        private string GetText()
        {
            string raw;
            bool callback;
            if (_info.Attribute is HierarchyButtonAttribute button)
            {
                if (string.IsNullOrEmpty(button.Label))
                {
                    return ObjectNames.NicifyVariableName(_info.MemberInfo.Name);
                }
                raw = button.Label;
                callback = button.IsCallback;
            }
            else
            {
                HierarchyLabelAttribute label = (HierarchyLabelAttribute)_info.Attribute;
                if (string.IsNullOrEmpty(label.Label))
                {
                    object value = _info.MemberInfo switch
                    {
                        FieldInfo field => field.GetValue(_target),
                        PropertyInfo property => property.GetValue(_target),
                        MethodInfo method => method.Invoke(_target,
                            method.GetParameters().Select(parameter => parameter.DefaultValue).ToArray()),
                        _ => null,
                    };
                    // ReSharper disable once PossibleNullReferenceException
                    return RuntimeUtil.IsNull(value) ? null : value.ToString();
                }
                raw = label.Label;
                callback = label.IsCallback;
            }

            if (!callback)
            {
                return raw;
            }
            (string error, object result) = Util.GetOf<object>(raw, null, null, _info.MemberInfo, _target, null);
            if (error != "" || RuntimeUtil.IsNull(result))
            {
                return null;
            }

            return result.ToString();
        }
    }
}
