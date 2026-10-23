using System;
using UnityEngine;

namespace SaintsHierarchy.Editor.Core.Config
{
    [Serializable]
    public struct ContextPathOverride
    {
        // Original Unity path (including implicit groups), or a custom group's ID.
        // Identity is independent of the resolved display path.
        public string targetId;

        // Unset means follow the original parent, even when that parent moves.
        public bool overrideParent;
        public string parentId;

        // Empty means retain the original command/group name.
        public string newName;

        public bool hidden;

        public bool overridePriority;
        public int priority;

        public ContextSeparatorBefore separatorBefore;

        public ContextPathPrefixType prefixType;
        public string prefixTexturePath;
        public Texture2D prefixTextureObject;
        public string prefixText;

        public bool HasPlacementOverride => overrideParent || !string.IsNullOrEmpty(newName);

        // Restore location without discarding visibility, priority, or decoration.
        public void RestoreLocation()
        {
            overrideParent = false;
            parentId = null;
            newName = null;
        }
    }
}
