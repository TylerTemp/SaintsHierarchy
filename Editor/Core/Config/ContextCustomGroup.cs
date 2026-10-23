using System;

namespace SaintsHierarchy.Editor.Core.Config
{
    [Serializable]
    public struct ContextCustomGroup
    {
        // Persist this ID once; never derive it from the group's name or location.
        public string id;
        public string parentId;
        public string name;
        public int priority;

        // Appearance and subsequent placement changes use ContextPathOverride,
        // targeting this ID in exactly the same way as an original Unity node.
        public static ContextCustomGroup Create(string name, string parentId)
        {
            return new ContextCustomGroup
            {
                id = "custom:" + Guid.NewGuid().ToString("N"),
                parentId = parentId,
                name = name,
            };
        }
    }
}
