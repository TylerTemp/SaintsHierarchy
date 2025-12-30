namespace SaintsHierarchy
{
    public interface IHierarchyIconPath
    {
#if UNITY_EDITOR
        string HierarchyIconPath { get; }
#endif
    }
}
