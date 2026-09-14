using SaintsHierarchy.Editor.Core.UIElement;
using System.Collections.Generic;
using System.Reflection;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public static class SaintsHierarchyLayer
    {
        private const string ColumnId = "saints-hierarchy-layer";
        // Keep Unity's selection, child confirmation, and Undo behavior without its IMGUI menu.
        private static readonly MethodInfo SetGameObjectsLayer = typeof(HierarchyWindow).Assembly
            .GetType("Unity.Hierarchy.Editor.HierarchyWindowColumnLayer")
            ?.GetMethod("SetGameObjectsLayer", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(HierarchyViewCell), typeof(int) }, null);

        [HierarchyViewColumnDescriptor(ColumnId)]
        private static void DescribeColumn(HierarchyViewColumnDescriptor descriptor)
        {
            descriptor.Title = "Layer(SH)";

            descriptor.MakeHeader = () => new Label("Layer")
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

            DropdownButtonElement dropdown = new DropdownButtonElement(InternalEditorUtility.GetLayerName(go.layer));
            cell.Add(dropdown);

            Refresh();
            SerializedObject serializedObject = new SerializedObject(go);
            cell.BoundObject = serializedObject;
            dropdown.TrackPropertyValue(serializedObject.FindProperty("m_Layer"), _ => Refresh());
            dropdown.Button.clicked += () =>
            {
                if (go == null || dropdown.parent != cell)
                {
                    return;
                }

                int currentLayer = go.layer;
                GenericDropdownMenu menu = new GenericDropdownMenu();
                foreach ((string name, int value) in GetAllLayers())
                {
                    menu.AddItem(name, value == currentLayer, () =>
                    {
                        // An open menu must not edit a different object after its cell is recycled.
                        if (go == null || dropdown.parent != cell || go.layer == value)
                        {
                            return;
                        }
                        if (SetGameObjectsLayer == null)
                        {
                            Debug.LogError("Unity's Hierarchy layer setter is unavailable.");
                            return;
                        }
                        SetGameObjectsLayer.Invoke(null, new object[] { cell, value });
                        Refresh();
                    });
                }
                menu.AddSeparator("");

                menu.AddItem("Add Layer...", false, () =>
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
                menu.AddItem($"Search \"{InternalEditorUtility.GetLayerName(currentLayer)}\"", false, () =>
                {
                    foreach (HierarchyWindow window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
                    {
                        // ReSharper disable once InvertIf
                        if (window.View == cell.View)
                        {
                            window.SetSearchText($"layer={currentLayer}");
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
                string layerName = InternalEditorUtility.GetLayerName(go.layer);
                dropdown.SetLabel(layerName);
                dropdown.Button.tooltip = layerName;
                cell.IsDefaultValue = go.layer == 0;
            }
        }

        private static IReadOnlyList<(string name, int value)> GetAllLayers()
        {
            List<(string name, int value)> resultList = new List<(string name, int value)>();
            for (int layer = 0; layer < 32; ++layer)
            {
                string layerName = InternalEditorUtility.GetLayerName(layer);
                if (layerName.Length != 0)
                {
                    resultList.Add((layerName, layer));
                }
            }
            return resultList;
        }
    }
}
