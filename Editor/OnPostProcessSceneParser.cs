// using UnityEditor;
// using UnityEditor.Callbacks;
// using UnityEngine;
// using UnityEngine.SceneManagement;
//
// namespace SaintsHierarchy.Editor
// {
//     public static class OnPostProcessSceneParser
//     {
//         [PostProcessScene]
//         public static void OnPostProcessScene()
//         {
//             Scene scene = SceneManager.GetActiveScene();
//             foreach (GameObject rootGameObject in scene.GetRootGameObjects())
//             {
//                 foreach (Transform trans in rootGameObject.transform.GetComponentsInChildren<Transform>())
//                 {
//                     Debug.Log($"{trans.name}: {PrefabUtility.IsAnyPrefabInstanceRoot(trans.gameObject)}");
//                 }
//             }
//         }
//     }
// }
