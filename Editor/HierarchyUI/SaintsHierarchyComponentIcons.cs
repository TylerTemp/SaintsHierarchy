using SaintsHierarchy.Editor.Core;
using SaintsHierarchy.Editor.Core.Config;
using SaintsHierarchy.Editor.Core.Utils;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor.HierarchyUI
{
    public static class SaintsHierarchyComponentIcons
    {
        private const string ColumnId = "saints-hierarchy-component-icons";
        private const float IconSize = 16;

        [HierarchyViewColumnDescriptor(ColumnId)]
        private static void DescribeColumn(HierarchyViewColumnDescriptor descriptor)
        {
            descriptor.Title = "Components(SH)";

            descriptor.MakeHeader = () => new Label("Components")
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
        }

        private static void BindCell(HierarchyViewCell cell)
        {
            cell.AddToClassList(ColumnId);
            // Cells are recycled: never retain buttons bound to the previous GameObject.
            cell.Clear();
            cell.IsDefaultValue = false;
            IConfig config = Util.GetUsingConfig();

            // dont care. User can toggle it in Unity's menu
            // if (config.disabled || !config.componentIcons)
            // {
            //     return;
            // }

            GameObject go = EditorUtility.EntityIdToObject(
                cell.View.Source.GetEntityIdFromNode(cell.Node)) as GameObject;
            if (go == null)
            {
                return;
            }

            VisualElement row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    alignItems = Align.Center,
                    flexGrow = 1,
                    overflow = Overflow.Hidden,
                },
            };
            foreach ((Component component, Texture2D icon) in Util.GetComponentIcons(
                         go.GetComponents<Component>(), config.componentIconsForGeneralScripts,
                         config.componentIconsForTransform))
            {
                Button button = SaintsHierarchyUtil.CreateComponentIconButton(component, icon);
                button.clicked += () => ComponentPropertiesPopup.Show(component, button);
                row.Add(button);
            }
            cell.Add(row);
        }

        public static void RefreshVisibleCells(VisualElement root)
        {
            foreach (HierarchyViewCell cell in root.Query<HierarchyViewCell>(className: ColumnId).ToList())
            {
                BindCell(cell);
            }
        }
    }
}
