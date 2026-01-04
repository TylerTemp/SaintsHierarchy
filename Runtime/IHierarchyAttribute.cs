namespace SaintsHierarchy
{
    public interface IHierarchyAttribute
    {
        string GroupBy { get; }
        bool IsLeft { get; }
    }
}
