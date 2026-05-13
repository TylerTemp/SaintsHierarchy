using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor
{
    public class ComponentPropertiesPopup : EditorWindow
    {
        private const float Width = 420f;
        private const float FallbackHeight = 180f;
        private const float FitContentPadding = 16f;
        // private const float ObviousExtraHeight = 80f;

        [SerializeField] private Component _component;
        [SerializeField] private Vector2 _anchorPosition;
        private UnityEditor.Editor _editor;
        private bool _initialFitDone;

        public static void Show(Component component, Rect activatorRect)
        {
            if (component == null)
            {
                return;
            }

            Rect screenRect = GUIUtility.GUIToScreenRect(new Rect(activatorRect.xMax, activatorRect.yMax, activatorRect.width, activatorRect.height));

            ComponentPropertiesPopup window = CreateInstance<ComponentPropertiesPopup>();
            window._component = component;
            window.titleContent = new GUIContent($"{ObjectNames.NicifyVariableName(component.GetType().Name)} ({component.gameObject.name})");
            window.minSize = new Vector2(280f, 140f);
            Rect initialPosition = new Rect(screenRect.x, screenRect.y, Width, FallbackHeight);
            window._anchorPosition = initialPosition.position;

            window.ShowUtility();
            window.position = initialPosition;
            window.Focus();
        }


        private void CreateGUI()
        {
            if (_component == null)
            {
                Close();
                return;
            }

            _editor = UnityEditor.Editor.CreateEditor(_component);
            if (_editor == null)
            {
                Close();
                return;
            }

            rootVisualElement.Clear();

            ScrollView scrollView = new ScrollView
            {
                style =
                {
                    flexGrow = 1,
                },
            };

            InspectorElement inspectorElement = new InspectorElement(_editor)
            {
                style =
                {
                    flexShrink = 0,
                },
            };

            if (_component is Behaviour behaviour)
            {
                Toggle enabledToggle = new Toggle("Enabled")
                {
                    value = behaviour.enabled,
                    style =
                    {
                        flexShrink = 1,
                        flexGrow = 1,
                        marginTop = 0,
                        marginBottom = 0,
                        marginLeft = 0,
                        marginRight = 0,
                    },
                };
                enabledToggle.AddToClassList(Toggle.alignedFieldUssClassName);
                enabledToggle.RegisterValueChangedCallback(evt =>
                {
                    if (behaviour == null)
                    {
                        return;
                    }

                    Undo.RecordObject(behaviour, "Toggle Behaviour Enabled");
                    behaviour.enabled = evt.newValue;
                    EditorUtility.SetDirty(behaviour);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                });

                VisualElement enabledToggleContainer = new VisualElement();
                enabledToggleContainer.AddToClassList(InspectorElement.ussClassName);
                enabledToggleContainer.Add(enabledToggle);
                scrollView.Add(enabledToggleContainer);
            }

            scrollView.Add(inspectorElement);
            rootVisualElement.Add(scrollView);

            EventCallback<GeometryChangedEvent> fitInitialWindowHeight = null;
            fitInitialWindowHeight = _ =>
            {
                scrollView.contentContainer.UnregisterCallback(fitInitialWindowHeight);
                FitInitialWindowHeight(scrollView.contentContainer);
            };
            scrollView.contentContainer.RegisterCallback(fitInitialWindowHeight);
        }

        private void FitInitialWindowHeight(VisualElement contentElement)
        {
            if (_initialFitDone)
            {
                return;
            }

            float contentHeight = contentElement.resolvedStyle.height;
            if (float.IsNaN(contentHeight) || contentHeight <= 0f)
            {
                contentHeight = contentElement.layout.height;
            }

            if (float.IsNaN(contentHeight) || contentHeight <= 0f)
            {
                contentHeight = 0f;
                foreach (VisualElement child in contentElement.Children())
                {
                    float childHeight = child.resolvedStyle.height;
                    if (float.IsNaN(childHeight) || childHeight <= 0f)
                    {
                        childHeight = child.layout.height;
                    }

                    if (!float.IsNaN(childHeight) && childHeight > 0f)
                    {
                        contentHeight += childHeight;
                    }
                }
            }

            if (float.IsNaN(contentHeight) || contentHeight <= 0f)
            {
                return;
            }

            _initialFitDone = true;

            float desiredHeight = Mathf.Max(minSize.y, contentHeight + FitContentPadding);
            // Debug.Log($"minSize.y={minSize.y} contentHeight={contentHeight} FitContentPadding={FitContentPadding}; desiredHeight={desiredHeight}");
            Rect currentPosition = position;
            // if (currentPosition.height - desiredHeight < ObviousExtraHeight)
            // {
            //     Debug.Log($"no change");
            //     return;
            // }

            position = new Rect(_anchorPosition.x, _anchorPosition.y, currentPosition.width, desiredHeight);
        }

        private void OnLostFocus()
        {
            Close();
        }

        private void OnDisable()
        {
            if (_editor != null)
            {
                DestroyImmediate(_editor);
                _editor = null;
            }
        }
    }
}
