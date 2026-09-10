using SaintsHierarchy.Editor.Core.UIElement;
using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor.Core
{
    public class GameObjectConfigPopup: PopupWindowContent
    {
        private readonly GameObject _go;
        private readonly GameObjectConfig _goConfig;

        public GameObjectConfigPopup(GameObject go, GameObjectConfig goConfig)
        {
            _go = go;
            _goConfig = goConfig;
        }

        public override void OnGUI(Rect rect)
        {
            // Intentionally left empty
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(200, 200);
        }

        public override void OnOpen()
        {
            GameObjectConfigPanel element = new GameObjectConfigPanel(_go, _goConfig);
            editorWindow.rootVisualElement.Add(element);
            element.NeedCloseEvent.AddListener(hasChange =>
            {
                if(hasChange)
                {
                    HierarchyEditorEvents.RequestReloadAllScenes();
                    EditorApplication.RepaintHierarchyWindow();
                }
                editorWindow.Close();
            });
        }

    }
}
