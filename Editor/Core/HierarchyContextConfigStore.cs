using System;
using System.Collections.Generic;
using SaintsHierarchy.Editor.Core.Config;
using SaintsHierarchy.Editor.Core.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaintsHierarchy.Editor.Core
{
    [InitializeOnLoad]
    internal static class HierarchyContextConfigStore
    {
        internal static event Action Changed;
        internal static IConfig Active => Util.GetUsingConfig();
        internal static IConfig ContextMenu => Active.SaveContextMenuToProjectConfig
            ? SaintsHierarchyConfig.instance : PersonalHierarchyConfig.instance;

        private static readonly HashSet<IConfig> Modified = new HashSet<IConfig>();

        static HierarchyContextConfigStore()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            HierarchyEditorEvents.InitializeRequested += Invalidate;
        }

        private static void Invalidate()
        {
            Changed?.Invoke();
            EditorApplication.RepaintHierarchyWindow();
        }

        internal static void Edit(IConfig config, string undoName, Action action)
        {
            Undo.RecordObject((Object)config, undoName);
            action();
            EditorUtility.SetDirty((Object)config);
            Modified.Add(config);
            config.SaveToDisk();
            Invalidate();
        }

        private static void OnUndoRedo()
        {
            foreach (IConfig config in Modified)
            {
                config.SaveToDisk();
            }
            Invalidate();
        }
    }
}
