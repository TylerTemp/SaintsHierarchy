using System;
using System.Collections.Generic;
using System.Linq;
using SaintsHierarchy.Editor.Core.Config;
using SaintsHierarchy.Editor.Core.Utils;
using UnityEngine;

namespace SaintsHierarchy.Editor.Core
{
    // No EditorWindow, selection, execution, or UI dependencies. Both the config
    // preview and popup resolve the same snapshot without modifying saved config.
    internal static class ContextMenuResolver
    {
        internal static ContextMenuTree Resolve(IEnumerable<HierarchyContextUtil.MenuItem> source,
            IEnumerable<ContextPathOverride> overrides,
            IEnumerable<ContextCustomGroup> customGroups, bool defaultContextMenuIcons)
        {
            ContextMenuNode root = new ContextMenuNode("Hierarchy", ContextMenuNodeKind.Root, "Hierarchy")
            {
                OriginalName = "Hierarchy",
                OriginalLabel = "Hierarchy",
                Name = "Hierarchy",
                Label = "Hierarchy",
            };
            ContextMenuTree tree = new ContextMenuTree(root);
            ReadSource(tree, source);
            AddCustomGroups(tree, customGroups ?? Array.Empty<ContextCustomGroup>());
            ApplyOverrides(tree, overrides ?? Array.Empty<ContextPathOverride>());
            if (defaultContextMenuIcons)
            {
                ResolveDefaultPrefixes(tree);
            }
            FinalizeTree(tree);
            return tree;
        }

        private static void ResolveDefaultPrefixes(ContextMenuTree tree)
        {
            foreach (ContextMenuNode node in tree.ById.Values)
            {
                if (node.Prefix.Type != ContextPathPrefixType.Default)
                {
                    continue;
                }
                Texture2D icon = null;
                if (node.SourcePath != null && ContextMenuDefaults.DefaultIcons.TryGetValue(node.SourcePath, out string[] paths))
                {
                    foreach (string path in paths)
                    {
                        icon = HierarchyContextUtil.LoadIcon(path);
                        if (icon != null)
                        {
                            break;
                        }
                    }
                }
                node.Prefix = new ContextMenuPrefix
                {
                    Type = icon == null ? ContextPathPrefixType.None : ContextPathPrefixType.TextureObject,
                    TextureObject = icon,
                };
            }
        }

        private static void ReadSource(ContextMenuTree tree, IEnumerable<HierarchyContextUtil.MenuItem> source)
        {
            HashSet<string> pendingSeparators = new HashSet<string>(StringComparer.Ordinal);
            int order = 0;
            foreach (HierarchyContextUtil.MenuItem item in source)
            {
                if (string.IsNullOrEmpty(item.Path))
                {
                    continue;
                }
                // Unity uses isSeparator for both group declarations and boundaries
                // inside an already-declared group.
                if (item.IsSeparator && tree.ById.TryGetValue(item.Path, out ContextMenuNode branch)
                                     && branch.Kind != ContextMenuNodeKind.Command)
                {
                    pendingSeparators.Add(branch.Id);
                    continue;
                }
                if (!item.Path.StartsWith("Hierarchy/", StringComparison.Ordinal))
                {
                    continue;
                }
                ContextMenuNode parent = EnsureSourceGroup(
                                    item.Path.Substring(0, item.Path.LastIndexOf('/')), tree, pendingSeparators, ref order);
                if (tree.ById.ContainsKey(item.Path))
                {
                    continue;
                }
                ContextMenuNode node = new ContextMenuNode(item.Path,
                                    item.IsSeparator ? ContextMenuNodeKind.Group : ContextMenuNodeKind.Command, item.Path)
                {
                    OriginalPriority = item.Priority,
                    OriginalLabel = item.Label,
                    Shortcut = item.Shortcut,
                };
                AddSourceNode(node, parent, tree, pendingSeparators, ref order);
            }
        }

        private static ContextMenuNode EnsureSourceGroup(string path, ContextMenuTree tree,
            HashSet<string> pendingSeparators, ref int order)
        {
            if (tree.ById.TryGetValue(path, out ContextMenuNode found))
            {
                return found;
            }
            ContextMenuNode parent = EnsureSourceGroup(path.Substring(0, path.LastIndexOf('/')),
                            tree, pendingSeparators, ref order);
            ContextMenuNode group = new ContextMenuNode(path, ContextMenuNodeKind.Group, path)
            {
                OriginalLabel = path.Substring(path.LastIndexOf('/') + 1),
            };
            AddSourceNode(group, parent, tree, pendingSeparators, ref order);
            return group;
        }

        private static void AddSourceNode(ContextMenuNode node, ContextMenuNode parent, ContextMenuTree tree,
            HashSet<string> pendingSeparators, ref int order)
        {
            node.OriginalName = node.SourcePath.Substring(node.SourcePath.LastIndexOf('/') + 1);
            node.OriginalParentId = parent.Id;
            node.SourceOrder = order++;
            node.ExplicitSeparatorBefore = pendingSeparators.Remove(parent.Id);
            InitializeNode(node, parent);
            tree.ById.Add(node.Id, node);
            parent.Children.Add(node);
        }

        private static void InitializeNode(ContextMenuNode node, ContextMenuNode parent)
        {
            node.Parent = parent;
            node.Name = node.OriginalName;
            node.Label = node.OriginalLabel;
            node.Priority = node.OriginalPriority;
        }

        private static void AddCustomGroups(ContextMenuTree tree, IEnumerable<ContextCustomGroup> groups)
        {
            Dictionary<string, ContextCustomGroup> definitions = new Dictionary<string, ContextCustomGroup>(StringComparer.Ordinal);
            Dictionary<string, int> orders = new Dictionary<string, int>(StringComparer.Ordinal);
            int order = tree.ById.Count;
            foreach (ContextCustomGroup group in groups)
            {
                if (string.IsNullOrEmpty(group.id) || !group.id.StartsWith("custom:", StringComparison.Ordinal)
                                                  || group.id.Length == "custom:".Length || !ValidName(group.name))
                {
                    Report(tree, group.id, "Custom groups need a custom: ID and a nonempty name without '/'.");
                    continue;
                }
                if (definitions.ContainsKey(group.id) || tree.ById.ContainsKey(group.id))
                {
                    Report(tree, group.id, "Duplicate custom group ID.");
                    continue;
                }
                definitions.Add(group.id, group);
                orders.Add(group.id, order++);
            }
            HashSet<string> visiting = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> failed = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> unavailable = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in definitions.Keys)
            {
                EnsureCustomGroup(id);
            }
            ContextMenuNode EnsureCustomGroup(string id)
            {
                if (tree.ById.TryGetValue(id, out ContextMenuNode existing))
                {
                    return existing;
                }
                if (failed.Contains(id))
                {
                    return null;
                }
                if (!definitions.TryGetValue(id, out ContextCustomGroup definition))
                {
                    return null;
                }
                if (!visiting.Add(id))
                {
                    Report(tree, id, "Custom group parent cycle.");
                    failed.Add(id);
                    return null;
                }
                ContextMenuNode parent = string.IsNullOrEmpty(definition.parentId)
                    ? tree.Root : EnsureCustomGroup(definition.parentId);
                visiting.Remove(id);
                if (parent == null || parent.Kind == ContextMenuNodeKind.Command)
                {
                    if (!string.IsNullOrEmpty(definition.parentId) && (unavailable.Contains(definition.parentId)
                        || !tree.ById.ContainsKey(definition.parentId) && !definitions.ContainsKey(definition.parentId)))
                    {
                        failed.Add(id);
                        unavailable.Add(id);
                        return null;
                    }
                    Report(tree, id, "Custom group parent is missing, invalid, or a command.");
                    failed.Add(id);
                    return null;
                }
                if (parent.Children.Any(child => child.Name == definition.name))
                {
                    Report(tree, id, "Custom group destination is already occupied.");
                    failed.Add(id);
                    return null;
                }
                ContextMenuNode group = new ContextMenuNode(id, ContextMenuNodeKind.Group, null)
                {
                    OriginalParentId = parent.Id,
                    OriginalName = definition.name,
                    OriginalLabel = definition.name,
                    OriginalPriority = definition.priority,
                    SourceOrder = orders[id],
                };
                InitializeNode(group, parent);
                tree.ById.Add(id, group);
                parent.Children.Add(group);
                return group;
            }
        }

        private static void ApplyOverrides(ContextMenuTree tree, IEnumerable<ContextPathOverride> overrides)
        {
            Dictionary<string, ContextPathOverride> indexed = new Dictionary<string, ContextPathOverride>(StringComparer.Ordinal);
            foreach (ContextPathOverride config in overrides)
            {
                // Context-dependent Unity entries may be absent from this snapshot.
                // Keep their saved overrides without reporting them as invalid.
                if (!string.IsNullOrEmpty(config.targetId) && !tree.ById.ContainsKey(config.targetId))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(config.targetId) || config.targetId == tree.Root.Id)
                {
                    Report(tree, config.targetId, "Target is missing or is root");
                    continue;
                }
                if (indexed.ContainsKey(config.targetId))
                {
                    Report(tree, config.targetId, "Duplicated; Last used");
                }
                indexed[config.targetId] = config;
            }
            HashSet<ContextMenuNode> placements = new HashSet<ContextMenuNode>();
            foreach (ContextMenuNode node in tree.ById.Values)
            {
                if (!indexed.TryGetValue(node.Id, out ContextPathOverride config))
                {
                    continue;
                }
                node.AppliedOverride = config;
                node.HiddenSelf = config.hidden;
                node.Priority = config.overridePriority ? config.priority : node.OriginalPriority;
                node.Prefix = new ContextMenuPrefix
                {
                    Type = config.prefixType,
                    TexturePath = config.prefixTexturePath,
                    TextureObject = config.prefixTextureObject,
                    Text = config.prefixText,
                };
                if (!config.HasPlacementOverride)
                {
                    continue;
                }
                ContextMenuNode parent = node.Parent;
                if (config.overrideParent && (string.IsNullOrEmpty(config.parentId)
                    || !tree.ById.TryGetValue(config.parentId, out parent) || parent.Kind == ContextMenuNodeKind.Command))
                {
                    if (!string.IsNullOrEmpty(config.parentId) && !tree.ById.ContainsKey(config.parentId))
                    {
                        continue;
                    }
                    Report(tree, node.Id, "Destination group is missing or is a command; original location retained.");
                    continue;
                }
                string name = string.IsNullOrEmpty(config.newName) ? node.OriginalName : config.newName;
                if (!ValidName(name) || IsOriginalDescendant(parent, node, tree))
                {
                    Report(tree, node.Id, "Invalid name or move into the item's own subtree; original location retained.");
                    continue;
                }
                node.Parent = parent;
                node.Name = name;
                if (!string.IsNullOrEmpty(config.newName))
                {
                    node.Label = name;
                }
                placements.Add(node);
            }
            // Validate the complete proposed hierarchy, not the order of config records.
            // Rolling back a placement can reveal another conflict, so repeat until stable.
            bool changed;
            do
            {
                changed = false;
                foreach (ContextMenuNode node in tree.ById.Values)
                {
                    List<ContextMenuNode> chain = new List<ContextMenuNode>();
                    ContextMenuNode cursor = node;
                    while (cursor != null && !chain.Contains(cursor))
                    {
                        chain.Add(cursor);
                        cursor = cursor.Parent;
                    }
                    if (cursor == null)
                    {
                        continue;
                    }
                    foreach (ContextMenuNode member in chain.Skip(chain.IndexOf(cursor)))
                    {
                        if (!placements.Remove(member))
                        {
                            continue;
                        }
                        RestorePlacement(member, tree);
                        Report(tree, member.Id, "Placement cycle; original location retained.");
                        changed = true;
                    }
                }
                var collisions = tree.ById.Values.Where(node => node.Parent != null)
                    .GroupBy(node => (node.Parent.Id, node.Name)).Where(group => group.Count() > 1).ToList();
                foreach (var collision in collisions)
                {
                    foreach (ContextMenuNode node in collision)
                    {
                        if (!placements.Remove(node))
                        {
                            continue;
                        }
                        RestorePlacement(node, tree);
                        Report(tree, node.Id, "Destination is already occupied; original location retained.");
                        changed = true;
                    }
                }
            } while (changed);

            foreach (ContextMenuNode node in tree.ById.Values)
            {
                node.Children.Clear();
            }
            foreach (ContextMenuNode node in tree.ById.Values)
            {
                if (node.Parent != null)
                {
                    node.Parent.Children.Add(node);
                }
            }
        }

        private static bool IsOriginalDescendant(ContextMenuNode destination, ContextMenuNode node, ContextMenuTree tree)
        {
            for (ContextMenuNode cursor = destination; cursor != null;
                 cursor = cursor.OriginalParentId == null ? null : tree.ById[cursor.OriginalParentId])
            {
                if (cursor == node)
                {
                    return true;
                }
            }
            return false;
        }

        private static void RestorePlacement(ContextMenuNode node, ContextMenuTree tree)
        {
            node.Parent = tree.ById[node.OriginalParentId];
            node.Name = node.OriginalName;
            node.Label = node.OriginalLabel;
        }

        private static void FinalizeTree(ContextMenuTree tree)
        {
            Dictionary<string, bool> originalHidden = new Dictionary<string, bool>(StringComparer.Ordinal);
            Visit(tree.Root, false);

            bool HiddenByOriginalAncestry(ContextMenuNode node)
            {
                if (originalHidden.TryGetValue(node.Id, out bool hidden))
                {
                    return hidden;
                }
                hidden = node.HiddenSelf || node.OriginalParentId != null
                                    && HiddenByOriginalAncestry(tree.ById[node.OriginalParentId]);
                originalHidden.Add(node.Id, hidden);
                return hidden;
            }
            void Visit(ContextMenuNode node, bool destinationHidden)
            {
                node.DisplayPath = node.Parent == null ? node.Name : node.Parent.DisplayPath + "/" + node.Name;
                node.HiddenInHierarchy = destinationHidden || HiddenByOriginalAncestry(node);

                // Preserve Unity's source order for untouched sibling lists. Only
                // customized ordering/membership requires sorting by priority.
                bool sortByPriority = node.Children.Any(child => child.SourcePath == null
                    || child.AppliedOverride is ContextPathOverride config && (config.overridePriority
                        || config.overrideParent && child.Parent.Id != child.OriginalParentId));
                node.Children.Sort((a, b) =>
                {
    int priority = sortByPriority ? a.Priority.CompareTo(b.Priority) : 0;
    return priority != 0 ? priority : a.SourceOrder.CompareTo(b.SourceOrder);
                });

                ContextMenuNode previous = null;
                bool pendingExplicit = false;
                foreach (ContextMenuNode child in node.Children)
                {
                    Visit(child, node.HiddenInHierarchy);
                    pendingExplicit |= child.ExplicitSeparatorBefore;
                    if (!child.IsVisible)
                    {
                        continue;
                    }
                    bool separator = pendingExplicit || previous != null && (long)child.Priority - previous.Priority > 10;
                    ContextSeparatorBefore setting = child.AppliedOverride?.separatorBefore ?? ContextSeparatorBefore.Default;
                    if (setting != ContextSeparatorBefore.Default)
                    {
                        separator = setting == ContextSeparatorBefore.Show;
                    }
                    child.SeparatorBefore = previous != null && separator;
                    pendingExplicit = false;
                    previous = child;
                }
                node.IsVisible = !node.HiddenInHierarchy
                    && (node.Kind == ContextMenuNodeKind.Command || previous != null);
            }
        }

        private static bool ValidName(string name) => !string.IsNullOrWhiteSpace(name) && name.IndexOf('/') < 0;

        private static void Report(ContextMenuTree tree, string id, string message) =>
            tree.Diagnostics.Add(new ContextMenuDiagnostic(id, message));
    }
}
