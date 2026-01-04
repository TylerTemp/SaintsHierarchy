using UnityEngine;

namespace SaintsHierarchy.Editor.Utils
{
    public static class RectUtils
    {
        public static (Rect curRect, Rect leftRect) SplitWidthRect(Rect targetRect, float width)
        {
            float totalWidth = targetRect.width;
            if (totalWidth <= 0)
            {
                Rect zeroRect = new Rect(targetRect)
                {
                    width = 0,
                };
                return (zeroRect, zeroRect);
            }

            float canUseWidth = Mathf.Min(totalWidth, width);

            Rect curRect = new Rect(targetRect)
            {
                width = canUseWidth,
            };

            Rect leftRect = new Rect(targetRect)
            {
                x = curRect.x + curRect.width,
                width = targetRect.width - canUseWidth,
            };

            return (
                curRect,
                leftRect
            );
        }
    }
}
