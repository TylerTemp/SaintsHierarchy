using UnityEngine;

namespace SaintsHierarchy.Samples.Scripts
{
    public class DrawButtonExample : MonoBehaviour
    {
        public string c;

        [HierarchyGhostButton("$" + nameof(c), "Click Me!")]
        private void OnBtnClick()
        {
            Debug.Log($"click {c}");
        }

        [HierarchyLeftButton("C <color=Burlywood>Left")]
        private void LeftClick()
        {
            Debug.Log("Left Click");
        }
    }
}
