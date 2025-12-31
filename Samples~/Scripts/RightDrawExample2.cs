using UnityEngine;

namespace SaintsHierarchy.Samples.Scripts
{
    public class RightDrawExample2 : MonoBehaviour, IHierarchyDraw
    {
        public Texture2D icon;
        public HierarchyUsed HierarchyDraw(HierarchyArea hierarchyArea)
        {
            if (icon == null)
            {
                return new HierarchyUsed(hierarchyArea.MakeXWidthRect(hierarchyArea.SpaceEndX, 0));
            }

            float width = icon.width;

            Rect useRect = hierarchyArea.MakeXWidthRect(hierarchyArea.SpaceEndX, -width);

            GUI.DrawTexture(useRect, icon, ScaleMode.ScaleToFit, true);

            return new HierarchyUsed(useRect);
        }
    }
}
