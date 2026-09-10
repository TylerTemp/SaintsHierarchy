using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using SaintsHierarchy.Editor.Core.Draw;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI.Renderer
{
    public class ButtonRenderer : Button
    {
        private readonly Component _target;
        private readonly MethodInfo _method;
        private IEnumerator _coroutine;
        private IVisualElementScheduledItem _coroutineSchedule;

        public ButtonRenderer(Component target, RenderTargetInfo info)
        {
            _target = target;
            _method = (MethodInfo)info.MemberInfo;
            HierarchyButtonAttribute attribute = (HierarchyButtonAttribute)info.Attribute;
            tooltip = attribute.Tooltip;
            if (attribute.IsGhost)
            {
                style.backgroundColor = Color.clear;
                style.borderLeftWidth = style.borderRightWidth = 0;
                style.borderTopWidth = style.borderBottomWidth = 0;
            }
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.flexShrink = 0;
            style.marginLeft = style.marginRight = 2;
            style.marginTop = style.marginBottom = 0;
            style.paddingLeft = style.paddingRight = 2;
            style.paddingTop = style.paddingBottom = 0;

            LabelRenderer label = new LabelRenderer(target, info)
            {
                pickingMode = PickingMode.Ignore,
                style = { marginLeft = 0, marginRight = 0, paddingLeft = 0, paddingRight = 0 },
            };
            style.display = label.style.display;
            label.OnVisibilityChanged.AddListener(OnVisibilityChanged);
            Add(label);
            clicked += Invoke;

        }

        private void OnVisibilityChanged(bool show)
        {
            if (show)
            {
                style.display = DisplayStyle.Flex;
            }
            else
            {
                style.display = DisplayStyle.None;
                _coroutineSchedule?.Pause();
            }
        }

        private void Invoke()
        {
            if (_target == null)
            {
                Debug.LogError("Target not found for button");
                return;
            }
            try
            {
                if (_method.Invoke(_target, _method.GetParameters().Select(parameter => parameter.DefaultValue).ToArray())
                    is IEnumerator coroutine)
                {
                    IEnumerator previous = _coroutine;
                    _coroutine = coroutine;
                    if (_coroutineSchedule == null)
                    {
                        _coroutineSchedule = schedule.Execute(UpdateCoroutine).Every(1);
                    }
                    else
                    {
                        _coroutineSchedule.Resume();
                    }
                    if (!ReferenceEquals(previous, coroutine))
                    {
                        (previous as IDisposable)?.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e.InnerException ?? e);
            }
        }

        private void UpdateCoroutine()
        {
            IEnumerator coroutine = _coroutine;
            if (coroutine == null)
            {
                return;
            }
            bool running = false;
            try
            {
                running = _target != null && coroutine.MoveNext();
            }
            catch (Exception e)
            {
                Debug.LogException(e.InnerException ?? e);
            }
            if (!running && ReferenceEquals(_coroutine, coroutine))
            {
                _coroutine = null;
                _coroutineSchedule.Pause();
                try
                {
                    (coroutine as IDisposable)?.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogException(e.InnerException ?? e);
                }
            }
        }
    }
}
