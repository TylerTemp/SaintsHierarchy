using SaintsHierarchy.Editor.Core.UIElement;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public static class SaintsHierarchyTag
    {
        private const string ColumnId = "saints-hierarchy-tag";

        [HierarchyViewColumnDescriptor(ColumnId)]
        private static void DescribeColumn(HierarchyViewColumnDescriptor descriptor)
        {
            descriptor.Title = "Tag(SH)";

            descriptor.MakeHeader = () => new Label("Tag")
            {
                style =
                {
                    height = Length.Percent(100),
                    unityTextAlign = TextAnchor.MiddleLeft,
                    paddingLeft = 4,
                },
            };
            descriptor.BindHeader = (_, _) => { };
            descriptor.UnbindHeader = (_, _) => { };
            descriptor.DestroyHeader = (_, _) => { };

            descriptor.DefaultWidth = 96;
            descriptor.DefaultVisibility = false;
        }

        [HierarchyViewCellDescriptor(ColumnId, typeof(HierarchyGameObjectHandler))]
        private static void DescribeCell(HierarchyViewCellDescriptor descriptor)
        {
            descriptor.ClearCellContent = true;
            descriptor.BindCell = BindCell;
            descriptor.UnbindCell = cell =>
            {
                cell.Unbind();
                if (cell.BoundObject is SerializedObject serializedObject)
                {
                    serializedObject.Dispose();
                    cell.BoundObject = null;
                }
            };
        }

        private static void BindCell(HierarchyViewCell cell)
        {
            cell.Clear();
            cell.IsDefaultValue = false;
            GameObject go = EditorUtility.EntityIdToObject(
                cell.View.Source.GetEntityIdFromNode(cell.Node)) as GameObject;
            if (go == null || (go.hideFlags & HideFlags.NotEditable) != 0)
            {
                return;
            }

            DropdownButtonElement dropdown = new DropdownButtonElement(go.tag);
            cell.Add(dropdown);

            Refresh();
            SerializedObject serializedObject = new SerializedObject(go);
            cell.BoundObject = serializedObject;
            dropdown.TrackPropertyValue(serializedObject.FindProperty("m_TagString"), _ => Refresh());
            dropdown.Button.clicked += () =>
            {
                if (go == null || dropdown.parent != cell)
                {
                    return;
                }

                string currentTag = go.tag;
                GenericDropdownMenu menu = new GenericDropdownMenu();

                foreach (string tag in InternalEditorUtility.tags)
                {
                    menu.AddItem(tag, tag == currentTag, () =>
                    {
                        // An open menu must not edit a different object after its cell is recycled.
                        if (go == null || dropdown.parent != cell)
                        {
                            return;
                        }
                        ApplyTag(cell, tag);
                        Refresh();
                    });
                }
                menu.AddSeparator("");

                menu.AddItem("Add Tag...", false, () =>
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
                menu.AddItem($"Search \"{currentTag}\"", false, () =>
                {
                    foreach (HierarchyWindow window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
                    {
                        // ReSharper disable once InvertIf
                        if (window.View == cell.View)
                        {
                            string escapedTag = currentTag.Replace("\\", "\\\\").Replace("\"", "\\\"");
                            window.SetSearchText($"tag=\"{escapedTag}\"");
                            break;
                        }
                    }
                });
                menu.DropDown(dropdown.Button.worldBound, dropdown.Button, DropdownMenuSizeMode.Content);
            };
            return;

            void Refresh()
            {
                if (go == null || dropdown.parent != cell)
                {
                    return;
                }
                dropdown.SetLabel(go.tag);
                dropdown.Button.tooltip = go.tag;
                cell.IsDefaultValue = string.IsNullOrEmpty(go.tag) || go.CompareTag("Untagged");
            }
        }

        private static void ApplyTag(HierarchyViewCell cell, string tag)
        {
            List<GameObject> targets = new List<GameObject>();
            if (cell.View.IsSelected(cell.Node))
            {
                foreach (HierarchyNode node in cell.View.ViewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
                {
                    AddTarget(node);
                }
            }
            else
            {
                AddTarget(cell.Node);
            }

            if (targets.Count == 0)
            {
                return;
            }

            Undo.RecordObjects(targets.ToArray(), "Change Tag");
            foreach (GameObject target in targets)
            {
                target.tag = tag;
            }
            return;

            void AddTarget(HierarchyNode node)
            {
                GameObject target = EditorUtility.EntityIdToObject(
                    cell.View.Source.GetEntityIdFromNode(node)) as GameObject;
                if (target != null && (target.hideFlags & HideFlags.NotEditable) == 0 && !target.CompareTag(tag))
                {
                    targets.Add(target);
                }
            }
        }
    }
}
