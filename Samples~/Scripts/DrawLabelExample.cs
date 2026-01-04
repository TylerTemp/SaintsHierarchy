using UnityEngine;

namespace SaintsHierarchy.Samples.Scripts
{
    public class DrawLabelExample : MonoBehaviour
    {
        [HierarchyLabel("<color=CadetBlue><field/>")]
        [HierarchyLeftLabel("<color=CadetBlue>|LEFT|")]
        public string content;
    }
}
