using System;
using System.Collections.Generic;
using System.Linq;

namespace SaintsHierarchy.Editor.Core.Config
{
    // Produces proposals; callers persist only after validation by the shared resolver.
    internal static class ContextMenuEdits
    {
        internal static List<ContextPathOverride> WithOverride(IEnumerable<ContextPathOverride> overrides,
            ContextPathOverride value)
        {
            List<ContextPathOverride> result = overrides.Where(item => item.targetId != value.targetId).ToList();
            if (value.HasPlacementOverride || value.hidden || value.overridePriority
                || value.separatorBefore != ContextSeparatorBefore.Default || value.prefixType != ContextPathPrefixType.Default)
            {
                result.Add(value);
            }
            return result;
        }

        internal static List<ContextPathOverride> Move(IEnumerable<ContextPathOverride> overrides,
            ContextMenuNode node, ContextMenuNode parent)
        {
            ContextPathOverride value = node.AppliedOverride ?? new ContextPathOverride
            {
                targetId = node.Id
            };
            value.overrideParent = parent.Id != node.OriginalParentId;
            value.parentId = value.overrideParent ? parent.Id : null;
            return WithOverride(overrides, value);
        }

        internal static bool CanDeleteGroup(ContextMenuNode node, IEnumerable<ContextPathOverride> overrides,
            IEnumerable<ContextCustomGroup> groups)
        {
            return node.SourcePath == null && node.Kind == ContextMenuNodeKind.Group
                && groups.Any(group => group.id == node.Id);
        }

        internal static bool CanRestoreLocation(ContextMenuNode node) =>
            node.SourcePath != null && node.Kind != ContextMenuNodeKind.Root && node.HasPlacementOverride;

        internal static bool RenameGroup(ContextMenuNode node, string name, IEnumerable<ContextPathOverride> overrides,
            IEnumerable<ContextCustomGroup> groups, out List<ContextPathOverride> updatedOverrides,
            out List<ContextCustomGroup> updatedGroups)
        {
            updatedOverrides = overrides.ToList();
            updatedGroups = groups.ToList();
            if (!CanDeleteGroup(node, updatedOverrides, updatedGroups))
            {
                return false;
            }
            updatedGroups = updatedGroups.Select(group =>
            {
                if (group.id == node.Id)
                {
                    group.name = name;
                }
                return group;
            }).ToList();
            // Replace legacy rename overrides with the custom group's actual name.
            ContextPathOverride value = updatedOverrides.LastOrDefault(item => item.targetId == node.Id);
            value.targetId = node.Id;
            value.newName = null;
            updatedOverrides = WithOverride(updatedOverrides, value);
            return true;
        }

        internal static bool DeleteGroup(ContextMenuNode node, IEnumerable<ContextPathOverride> overrides,
            IEnumerable<ContextCustomGroup> groups, out List<ContextPathOverride> remainingOverrides,
            out List<ContextCustomGroup> remainingGroups)
        {
            remainingOverrides = overrides.ToList();
            remainingGroups = groups.ToList();
            if (!CanDeleteGroup(node, remainingOverrides, remainingGroups))
            {
                return false;
            }
            HashSet<string> contents = new HashSet<string>(StringComparer.Ordinal);
            Collect(node);
            Dictionary<string, ContextCustomGroup> definitions = remainingGroups
                .GroupBy(group => group.id).ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
            HashSet<string> removed = new HashSet<string>(StringComparer.Ordinal)
            {
node.Id
            };
            bool changed;
            do
            {
                changed = false;
                foreach (ContextCustomGroup group in remainingGroups)
                {
                    if (contents.Contains(group.id) && removed.Contains(group.parentId))
                    {
                        changed |= removed.Add(group.id);
                    }
                }
            } while (changed);

            // Keep groups that were independently moved out. Their default parent
            // must bypass deleted containers so Restore remains usable later.
            remainingGroups = remainingGroups.Where(group => !removed.Contains(group.id)).Select(group =>
            {
                while (removed.Contains(group.parentId))
                {
                    group.parentId = definitions[group.parentId].parentId;
                }
                return group;
            }).ToList();

            List<ContextPathOverride> updated = new List<ContextPathOverride>();
            foreach (ContextPathOverride original in remainingOverrides)
            {
                if (removed.Contains(original.targetId))
                {
                    continue;
                }
                ContextPathOverride value = original;
                // Also clean inactive/missing commands referencing a deleted group,
                // even though those commands are absent from the resolved tree.
                if (contents.Contains(value.targetId) || value.overrideParent && removed.Contains(value.parentId))
                {
                    string customName = definitions.ContainsKey(value.targetId) ? value.newName : null;
                    value.RestoreLocation();
                    // A surviving custom group's name is user-owned, not a Unity
                    // command name that should revert when its container is deleted.
                    value.newName = customName;
                    updated = WithOverride(updated, value);
                }
                else
                {
                    updated.Add(value);
                }
            }
            remainingOverrides = updated;
            return true;

            void Collect(ContextMenuNode current)
            {
                contents.Add(current.Id);
                foreach (ContextMenuNode child in current.Children)
                {
                    Collect(child);
                }
            }
        }

        internal static ContextCustomGroup CreateGroup(ContextMenuTree tree, ContextMenuNode parent)
        {
            string name = "New Group";
            // Original names remain reserved while a group is renamed or moved.
            // Custom definitions are parsed before placement overrides are applied.
            for (int suffix = 2; tree.ById.Values.Any(node => node.Parent == parent && node.Name == name
                || node.OriginalParentId == parent.Id && node.OriginalName == name); suffix++)
            {
                name = "New Group " + suffix;
            }
            ContextCustomGroup group = ContextCustomGroup.Create(name, parent.Id);
            group.priority = parent.Children.Count == 0 ? 0 : parent.Children.Max(child => child.Priority);
            return group;
        }

        internal static bool TryResolve(IEnumerable<HierarchyContextUtil.MenuItem> source, ContextMenuTree current,
            IEnumerable<ContextPathOverride> overrides, IEnumerable<ContextCustomGroup> groups,
            out ContextMenuTree proposed, out string error, string editedTarget = null)
        {
            // Placement validation does not need to load default icon resources.
            proposed = ContextMenuResolver.Resolve(source, overrides, groups, false);
            HashSet<(string, string)> existing = new HashSet<(string, string)>(
                current.Diagnostics.Select(issue => (issue.TargetId, issue.Message)));
            List<ContextMenuDiagnostic> introduced = proposed.Diagnostics
                .Where(issue => issue.TargetId == editedTarget || !existing.Contains((issue.TargetId, issue.Message))).ToList();
            error = string.Join("\n", introduced.Select(issue => issue.TargetId + ": " + issue.Message));
            return introduced.Count == 0;
        }
    }
}
