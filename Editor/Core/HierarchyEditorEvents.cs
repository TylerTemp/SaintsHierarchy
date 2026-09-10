using System;

namespace SaintsHierarchy.Editor.Core
{
    public static class HierarchyEditorEvents
    {
        public static event Action InitializeRequested;
        public static event Action ReloadAllScenesRequested;

        public static void RequestInitialize() => InitializeRequested?.Invoke();
        public static void RequestReloadAllScenes() => ReloadAllScenesRequested?.Invoke();
    }
}
