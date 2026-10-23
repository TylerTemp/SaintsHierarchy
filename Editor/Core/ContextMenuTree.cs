using System;
using System.Collections.Generic;
using SaintsHierarchy.Editor.Core.Config;
using UnityEngine;

namespace SaintsHierarchy.Editor.Core
{
    internal enum ContextMenuNodeKind
    {
        Root,

        Group,

        Command,
    }

    internal struct ContextMenuPrefix
    {
        public ContextPathPrefixType Type;
        public string TexturePath;
        public Texture2D TextureObject;
        public string Text;
    }

    // Transient data shared by the config editor and popup. Execution callbacks,
    // selection-dependent state, and UI geometry belong to the popup adapter.
    internal sealed class ContextMenuNode
    {
        public readonly string Id;
        public readonly ContextMenuNodeKind Kind;

        // Original command/implicit group path; null for custom groups.
        public readonly string SourcePath;

        // Defaults before overrides. Custom groups get these from their definition.
        public string OriginalParentId;
        public string OriginalName;
        public string OriginalLabel;
        public int OriginalPriority;
        public int SourceOrder;

        // Explicit Unity boundary associated with this item, excluding priority gaps.
        public bool ExplicitSeparatorBefore;

        // Final values populated by the resolver, before any UI is constructed.
        public string DisplayPath;
        public string Name;
        public string Label;
        public string Shortcut;
        public ContextMenuPrefix Prefix;
        public int Priority;
        public bool HiddenSelf;

        // Inherited hiding follows original ancestry, including descendants moved out.
        // A hidden destination also hides items moved into it.
        public bool HiddenInHierarchy;

        // Empty groups stay editable but do not produce popup rows.
        public bool IsVisible;

        // Calculated among visible siblings; hidden rows do not consume boundaries.
        public bool SeparatorBefore;

        public ContextMenuNode Parent;
        public readonly List<ContextMenuNode> Children = new List<ContextMenuNode>();
        public ContextPathOverride? AppliedOverride;

        // Children following a moved ancestor do not have a placement override.
        public bool HasPlacementOverride => AppliedOverride?.HasPlacementOverride ?? false;

        public ContextMenuNode(string id, ContextMenuNodeKind kind, string sourcePath)
        {
            Id = id;
            Kind = kind;
            SourcePath = sourcePath;
        }
    }

    internal sealed class ContextMenuTree
    {
        public readonly ContextMenuNode Root;

        // Original nodes use their exact Unity path as ID; custom groups use custom:GUID.
        // Hidden nodes stay indexed so the editor can select and restore them.
        public readonly Dictionary<string, ContextMenuNode> ById =
            new Dictionary<string, ContextMenuNode>(StringComparer.Ordinal);

        public readonly List<ContextMenuDiagnostic> Diagnostics = new List<ContextMenuDiagnostic>();

        public ContextMenuTree(ContextMenuNode root)
        {
            Root = root;
            ById.Add(root.Id, root);
        }
    }

    internal readonly struct ContextMenuDiagnostic
    {
        public readonly string TargetId;
        public readonly string Message;

        public ContextMenuDiagnostic(string targetId, string message)
        {
            TargetId = targetId;
            Message = message;
        }
    }
}
