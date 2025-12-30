using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Samples.Scripts
{
    public class HierarchyIconTexture2DExample: MonoBehaviour, IHierarchyIconTexture2D
    {
#if UNITY_EDITOR
        public Texture2D HierarchyIconTexture2D => AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Packages/today.comes.saintshierarchy/Editor/Editor Default Resources/SaintsHierarchy/d_eyeDropper.Large.png");
#endif
    }
}
