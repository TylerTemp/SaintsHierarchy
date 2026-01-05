using System;
using UnityEngine;

namespace SaintsHierarchy.Editor.Utils
{
    public class GUIColorScoop: IDisposable
    {
        private readonly Color _oldColor;

        public GUIColorScoop(Color color)
        {
            _oldColor = GUI.color;
            GUI.color = color;
        }


        public void Dispose()
        {
            GUI.color = _oldColor;
        }
    }
}
