namespace SaintsHierarchy
{
    public interface IHierarchyDraw
    {
#if UNITY_EDITOR
        HierarchyUsed HierarchyDraw(HierarchyArea hierarchyArea);
#endif
    }
}
