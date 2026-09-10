using System;
using UnityEngine;

namespace SaintsHierarchy.Editor.Utils
{
    public class BoldLabelScoop: IDisposable
    {
        private readonly bool _isBold;
        private readonly FontStyle _oldStyle;

        public BoldLabelScoop(bool bold)
        {
            _isBold = bold;
            // ReSharper disable once InvertIf
            if (bold)
            {
                _oldStyle = GUI.skin.label.fontStyle;
                GUI.skin.label.fontStyle = FontStyle.Bold;
            }
        }

        public void Dispose()
        {
            if (_isBold)
            {
                GUI.skin.label.fontStyle = _oldStyle;
            }
        }
    }
}
