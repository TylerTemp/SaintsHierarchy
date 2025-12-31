using System.Collections.Generic;
using UnityEngine;

namespace SaintsHierarchy
{
    public readonly struct HierarchyArea
    {
        /// <summary>
        /// Rect.y for drawing
        /// </summary>
        public readonly float Y;
        /// <summary>
        /// Rect.height for drawing
        /// </summary>
        public readonly float Height;
        /// <summary>
        /// the x value where the title (component name) started
        /// </summary>
        public readonly float TitleStartX;
        /// <summary>
        /// the x value where the title (component name) ended
        /// </summary>
        public readonly float TitleEndX;
        /// <summary>
        /// the x value where the empty space start. You may want to start draw here
        /// </summary>
        public readonly float SpaceStartX;
        /// <summary>
        /// the x value where the empty space ends. When drawing from right, you need to backward drawing starts here
        /// </summary>
        public readonly float SpaceEndX;

        public float TitleWidth => TitleEndX - TitleStartX;
        public float SpaceWidth => SpaceEndX - SpaceStartX;

        /// <summary>
        /// A quick way to make a rect
        /// </summary>
        /// <param name="x">where to start</param>
        /// <param name="width">width of the rect</param>
        /// <returns>rect space you want to draw</returns>
        public Rect MakeXWidthRect(float x, float width)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if(width >= 0)
            {
                return new Rect(x, Y, width, Height);
            }
            return new Rect(x + width, Y, -width, Height);
        }

        public HierarchyArea(float y, float height, float titleStartX, float titleEndX, float spaceStartX, float spaceEndX)
        {
            Y = y;
            Height = height;
            TitleStartX = titleStartX;
            TitleEndX = titleEndX;
            SpaceStartX = spaceStartX;
            SpaceEndX = spaceEndX;
        }

        public HierarchyArea EditorWrapX(float startX, float endX) =>
            new HierarchyArea(Y, Height, TitleStartX, TitleEndX, startX, endX);
    }
}
