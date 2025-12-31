namespace SaintsHierarchy
{
    public interface IHierarchyLeftDraw
    {
#if UNITY_EDITOR
        HierarchyUsed HierarchyLeftDraw(HierarchyArea hierarchyArea);
#endif
    }
}
