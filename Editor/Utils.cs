using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SaintsHierarchy.Editor
{
    public static class Utils
    {
        public static readonly string[] ResourceSearchFolder = {
            "Assets/Editor Default Resources/SaintsHierarchy",
            // this is readonly, put it to last so user can easily override it
            "Packages/today.comes.saintshierarchy/Editor/Editor Default Resources/SaintsHierarchy", // Unity UPM
        };

        public static T LoadResource<T>(string resourcePath) where T: Object
        {
            foreach (T each in ResourceSearchFolder
                         .Select(resourceFolder => AssetDatabase.LoadAssetAtPath<T>($"{resourceFolder}/{resourcePath}")))
            {
                // ReSharper disable once Unity.PerformanceCriticalCodeNullComparison
                if(each != null)
                {
                    return each;
                }
            }

            T result = EditorGUIUtility.Load(resourcePath) as T;

            if (typeof(T) == typeof(Texture2D))
            {
                if (!result)
                {
                    Texture2D r = EditorGUIUtility.IconContent(resourcePath).image as Texture2D;
                    if (r)
                    {
                        result = r as T;
                    }
                }
            }

            if (result == null)
            {
#if SAINTSHIERARCHY_DEBUG
                Debug.LogWarning($"{resourcePath} not found in {string.Join(", ", ResourceSearchFolder)}");
#endif
                return null;
            }
            // Debug.Assert(result, $"{resourcePath} not found in {string.Join(", ", ResourceSearchFolder)}");
            return result;
        }

        private static SaintsHierarchyConfig _config;

        public static SaintsHierarchyConfig EnsureConfig()
        {
            // ReSharper disable once InvertIf
            if (_config == null)
            {
                if (!Directory.Exists("Assets/Editor Default Resources"))
                {
                    Debug.Log("Create folder: Assets/Editor Default Resources");
                    AssetDatabase.CreateFolder("Assets", "Editor Default Resources");
                }
                if (!Directory.Exists("Assets/Editor Default Resources/SaintsHierarchy"))
                {
                    Debug.Log("Create folder: Assets/Editor Default Resources/SaintsHierarchy");
                    AssetDatabase.CreateFolder("Assets/Editor Default Resources", "SaintsHierarchy");
                }

                const string assetPath =
                    "Assets/Editor Default Resources/SaintsHierarchy/SaintsHierarchyConfig.asset";
                _config = AssetDatabase.LoadAssetAtPath<SaintsHierarchyConfig>(assetPath);
                // ReSharper disable once InvertIf
                if (_config == null)
                {
                    _config = ScriptableObject.CreateInstance<SaintsHierarchyConfig>();
                    Debug.Log("Create SaintsHierarchyConfig");
                    AssetDatabase.CreateAsset(_config,
                        assetPath);
                }
            }

            return _config;
        }

        public static void PopupConfig(Rect worldBound, GameObject go, SaintsHierarchyConfig.GameObjectConfig goConfig)
        {
            PopupWindow.Show(worldBound, new GameObjectConfigPopup(go, goConfig));
        }

        // public static GlobalObjectId ScenePrefabGidToUnpackedGid(GlobalObjectId id, string prefabId)
        // {
        //     string[] sourceSplit = id.ToString().Split('-');
        //     // string[] prefabSplit = prefabId.ToString().Split('-');
        //     // string prefabFileId = prefabSplit[prefabSplit.Length - 2];
        //     // sourceSplit[1] = "1";
        //     sourceSplit[2] = prefabId;
        //     sourceSplit[4] = "0";
        //     ulong fileId = (id.targetObjectId ^ id.targetPrefabId) & 0x7fffffffffffffff;
        //     // sourceSplit[3] = fileId.ToString();
        //
        //     var join = string.Join("-", sourceSplit);
        //
        //     // ReSharper disable once ConvertIfStatementToReturnStatement
        //     if (GlobalObjectId.TryParse(
        //             join,
        //             out GlobalObjectId unpackedGid))
        //     {
        //         return unpackedGid;
        //     }
        //
        //     return new GlobalObjectId();
        //     // // ulong fileId = (id.targetObjectId ^ id.targetPrefabId) & 0x7fffffffffffffff;
        //     // ulong fileId = (id.targetObjectId ^ prefabId) & 0x7fffffffffffffff;
        //     //
        //     // // ReSharper disable once ConvertIfStatementToReturnStatement
        //     // if (GlobalObjectId.TryParse(
        //     //         $"GlobalObjectId_V1-{id.identifierType}-{id.assetGUID}-{fileId}-0",
        //     //         out GlobalObjectId unpackedGid))
        //     // {
        //     //     return unpackedGid;
        //     // }
        //     //
        //     // return new GlobalObjectId();
        // }

        public static string GlobalObjectIdNormString(GlobalObjectId goId)
        {
            string goIdStringRaw = goId.ToString();
            string[] goIdSplit = goIdStringRaw.Split('-');
            goIdSplit[1] = "1";
            // goIdSplit[4] = "0";
            return string.Join('-', goIdSplit);
        }

        public static string GlobalObjectIdNormStringNoPrefabLink(GlobalObjectId goId)
        {
            string goIdStringRaw = goId.ToString();
            string[] goIdSplit = goIdStringRaw.Split('-');
            goIdSplit[1] = "1";
            goIdSplit[4] = "0";
            return string.Join('-', goIdSplit);
        }
    }
}
