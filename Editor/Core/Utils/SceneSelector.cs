#if SAINTSHIERARCHY_ADDRESSABLE && !SAINTSHIERARCHY_ADDRESSABLE_DISABLE
#define USE_ADDRESSABLE
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SaintsHierarchy.Editor.Core.UIElement.TreeDropdown;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if USE_ADDRESSABLE
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace SaintsHierarchy.Editor.Core.Utils
{
    public static class SceneSelector
    {
        public static void Show(Scene scene, Rect position, Action onSelected = null)
        {
            AdvancedDropdownMetaInfo meta = new AdvancedDropdownMetaInfo
            {
                CurValue = scene.path,
                DropdownListValue = GetScenePaths(),
            };
            (Rect worldBound, float maxHeight) = SaintsTreeDropdownUIToolkit.GetProperPos(position);
            PopupWindow.Show(worldBound, new SaintsTreeDropdownUIToolkit(
                meta, worldBound.width, maxHeight,
                curItem =>
                {
                    OpenAScene(scene, (string)curItem);
                    onSelected?.Invoke();
                }));
        }

        private static AdvancedDropdownList<string> GetScenePaths()
        {
            AdvancedDropdownList<string> scenePaths = new AdvancedDropdownList<string>();

#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_SCENE_SELECTOR
            Debug.Log("getting Scene in build");
#endif
            EditorBuildSettingsScene[] inBuildScenes = EditorBuildSettings.scenes;
            bool hasInBuildScenes = inBuildScenes.Length > 0;
            HashSet<string> addedScenePaths = new HashSet<string>();
            bool needSeparator = hasInBuildScenes;
            if(hasInBuildScenes)
            {
                // AdvancedDropdownList<string> buildScenes = new AdvancedDropdownList<string>("Builds");
                foreach (EditorBuildSettingsScene editorBuildSettingsScene in inBuildScenes)
                {
                    string assetPath = editorBuildSettingsScene.path;
                    if (!File.Exists(assetPath))  // invalid
                    {
                        continue;
                    }
                    addedScenePaths.Add(assetPath);
                    string dropPath = assetPath[..^".unity".Length];
                    // Debug.Log($"build {editorBuildSettingsScene.path}");
                    scenePaths.Add($"[Build]/{dropPath}", assetPath);
                }
                // scenePaths.Add(buildScenes);
            }
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_SCENE_SELECTOR
            Debug.Log("done getting Scene in build");
#endif

#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_SCENE_SELECTOR
            Debug.Log("getting Scene in addressable");
#endif
            bool hasAddressableScenes = false;
            foreach (string assetPath in GetAddressableScenes())
            {
                if (!addedScenePaths.Add(assetPath))
                {
                    continue;
                }

                if (needSeparator)
                {
                    scenePaths.AddSeparator();
                    needSeparator = false;
                }
                hasAddressableScenes = true;

                // Debug.Log($"address: {addressableScene.name}");
                string dropPath = assetPath[..^".unity".Length];
                scenePaths.Add($"[Addressable]/{dropPath}", assetPath);
            }

#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_SCENE_SELECTOR
            Debug.Log("done getting Scene in addressable");
#endif

            if (hasAddressableScenes)
            {
                needSeparator = true;
            }

#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_SCENE_SELECTOR
            Debug.Log("getting Scene in assets");
#endif
            // bool hasAssetScene = false;
            if(!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                foreach (string sceneGuid in AssetDatabase.FindAssets("t:scene"))
                {
                    if (!GUID.TryParse(sceneGuid, out GUID guid))
                    {
                        continue;
                    }

                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!assetPath.EndsWith(".unity"))
                    {
                        continue;
                    }

                    if (!addedScenePaths.Add(assetPath))
                    {
                        continue;
                    }

                    bool editable = AssetDatabase.IsOpenForEdit(
                        assetPath,
                        out string _,
                        StatusQueryOptions.ForceUpdate
                    );

                    if (!editable)
                    {
                        continue;
                    }

                    if (needSeparator)
                    {
                        scenePaths.AddSeparator();
                        needSeparator = false;
                    }

                    string dropPath = assetPath[..^".unity".Length];
                    scenePaths.Add(dropPath, assetPath);
                }
            }
#if SAINTSHIERARCHY_DEBUG && SAINTSHIERARCHY_DEBUG_SCENE_SELECTOR
            Debug.Log("done getting Scene in assets");
#endif

            scenePaths.SelfCompact();
            return scenePaths;
        }

        private static IReadOnlyList<string> _runtimeAdditiveScenesToRestore;

        private static void OpenAScene(Scene toReplaceScene, string toOpenScene)
        {
            if (!Application.isPlaying && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            bool replacingActiveScene = SceneManager.GetActiveScene() == toReplaceScene;

            if (SceneManager.sceneCount == 1)
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorSceneManager.OpenScene(toOpenScene, OpenSceneMode.Single);
                }
                else
                {
                    SceneManager.LoadScene(toOpenScene, LoadSceneMode.Single);
                }
            }
            else
            {
                if (EditorApplication.isPlaying)
                {
                    if (replacingActiveScene)
                    {
                        _runtimeAdditiveScenesToRestore = Enumerable.Range(0, SceneManager.sceneCount)
                            .Select(SceneManager.GetSceneAt)
                            .Where(each => each.isLoaded && each != toReplaceScene && each.path != "" && each.path != toOpenScene)
                            .Select(each => each.path)
                            .ToList();
                        SceneManager.sceneLoaded += OnRuntimeSceneLoaded;
                        SceneManager.LoadScene(toOpenScene, LoadSceneMode.Single);
                    }
                    else
                    {
                        SceneManager.sceneLoaded += OnRuntimeSceneLoaded;

                        SceneManager.LoadScene(toOpenScene, LoadSceneMode.Additive);
                        SceneManager.UnloadSceneAsync(toReplaceScene);
                    }
                }
                else
                {
                    Scene openedScene = EditorSceneManager.OpenScene(toOpenScene, OpenSceneMode.Additive);
                    EditorSceneManager.MoveSceneAfter(openedScene, toReplaceScene);
                    if (replacingActiveScene)
                    {
                        SceneManager.SetActiveScene(openedScene);
                    }

                    EditorSceneManager.CloseScene(toReplaceScene, true);
                }
            }
        }

        private static void OnRuntimeSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnRuntimeSceneLoaded;
            SceneManager.SetActiveScene(scene);

            if (_runtimeAdditiveScenesToRestore == null)
            {
                return;
            }

            foreach (string scenePath in _runtimeAdditiveScenesToRestore)
            {
                SceneManager.LoadScene(scenePath, LoadSceneMode.Additive);
            }

            _runtimeAdditiveScenesToRestore = null;
        }


        private static IEnumerable<string> GetAddressableScenes()
        {
#if USE_ADDRESSABLE
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (!settings)
            {
                yield break;
            }

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (AddressableAssetGroup addressableAssetGroup in settings.groups)
            {
                foreach (AddressableAssetEntry addressableAssetEntry in addressableAssetGroup.entries)
                {
                    if (!addressableAssetEntry.IsScene)
                    {
                        continue;
                    }

                    yield return addressableAssetEntry.AssetPath;
                }
            }
#else
            yield break;
#endif
        }


    }
}
