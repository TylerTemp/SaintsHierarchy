using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor.UIElement.TreeDropdown
{
    public class SaintsTreeDropdownUIToolkit : PopupWindowContent
    {
        private readonly float _width;
        private readonly AdvancedDropdownMetaInfo _metaInfo;
        private readonly Func<object, bool, IReadOnlyList<object>> _setValue;

        private readonly float _maxHeight;
        private readonly bool _allowUnSelect;

        public SaintsTreeDropdownUIToolkit(AdvancedDropdownMetaInfo metaInfo, float width, float maxHeight, bool allowUnSelect, Func<object, bool, IReadOnlyList<object>> setValue)
        {
            _width = width;
            _metaInfo = metaInfo;
            _setValue = setValue;
            _maxHeight = maxHeight;
            _allowUnSelect = allowUnSelect;
        }

        public override void OnGUI(Rect rect)
        {
            // Intentionally left empty
        }

        //Set the window size
        public override Vector2 GetWindowSize()
        {
            return new Vector2(_width, Mathf.Min(_maxHeight, _treeDropdownElement.GetMaxHeight()));
        }

        private SaintsTreeDropdownElement _treeDropdownElement;

        public override void OnOpen()
        {
            _treeDropdownElement = new SaintsTreeDropdownElement(_metaInfo, _allowUnSelect);

            _treeDropdownElement.OnClickedEvent.AddListener(OnClicked);
            // _treeDropdownElement.RegisterCallback<GeometryChangedEvent>(GeoUpdateWindowSize);
            // editorWindow.rootVisualElement.Add(_treeDropdownElement);

            // ScrollView scrollView = new ScrollView();
            // scrollView.Add(_treeDropdownElement);

            // _treeDropdownElement.ScrollToElementEvent.AddListener(scrollView.ScrollTo);

            // scrollView.RegisterCallback<AttachToPanelEvent>(_ =>
            // {
            //     scrollView.schedule.Execute(() =>
            //     {
            //         if (_treeDropdownElement.CurrentFocus != null)
            //         {
            //             scrollView.ScrollTo(_treeDropdownElement.CurrentFocus);
            //         }
            //         // The delay is required for functional
            //     }).StartingIn(100);
            // });

            editorWindow.rootVisualElement.Add(_treeDropdownElement);
        }

        // public void RefreshValues(IReadOnlyList<object> curValues) => _treeDropdownElement.RefreshValues(curValues);

        private void OnClicked(object value, bool isOn, bool isPrimary)
        {
            IReadOnlyList<object> r = _setValue(value, isOn);
            if (!_allowUnSelect || isPrimary || r == null)
            {
                editorWindow.Close();
            }
            else
            {
                _treeDropdownElement.RefreshValues(r);
            }
        }

        public void SetSearch(string search)
        {
            _treeDropdownElement.SetSearch(search);
        }


// #if SAINTSHIERARCHY_DEBUG
//         // ReSharper disable once UnusedMember.Global
//         public SaintsTreeDropdownElement DebugGetElement()
//         {
//             return new SaintsTreeDropdownElement(_metaInfo, _allowUnSelect);
//         }
// #endif

#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_ADVANCED_DROPDOWN
        public override void OnClose()
        {
            Debug.Log("Popup closed: " + this);
        }
#endif
        public static (Rect worldBound, float maxHeight) GetProperPos(Rect rootWorldBound)
        {
            int screenHeight = Screen.currentResolution.height;

            const float edgeHeight = 150;

            float maxHeight = screenHeight - rootWorldBound.yMax - edgeHeight;
            Rect worldBound = new Rect(rootWorldBound);
            // Debug.Log(worldBound);
            // ReSharper disable once InvertIf
            if (maxHeight < edgeHeight)
            {
                // worldBound.x -= 400;
                worldBound.y -= edgeHeight + worldBound.height;
                // Debug.Log(worldBound);
                maxHeight = Mathf.Max(edgeHeight, screenHeight - edgeHeight);
            }

            return (worldBound, maxHeight);
        }
    }
}
