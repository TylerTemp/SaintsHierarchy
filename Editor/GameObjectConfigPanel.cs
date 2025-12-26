using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor
{
    public class GameObjectConfigPanel: VisualElement
    {
        public readonly UnityEvent<bool> NeedCloseEvent = new UnityEvent<bool>();

        public GameObjectConfigPanel(GameObject go, bool hasCustomIcon)
        {
            VisualElement iconRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                },
            };
            Add(iconRow);

            if (hasCustomIcon)
            {
                Button btn = MakeIcon("trash.png");
                btn.style.backgroundColor = new Color(0.9411765f, 0.5019608f, 0.5019608f);
                btn.clicked += () => SetIcon(go, "");
                iconRow.Add(btn);
            }

            foreach (string iconPath in new[]
                     {
                         "Transform Icon",
                         "d_lightOff",
                         "d_greenLight",
                         "d_redLight",
                     })
            {
                Button btn = MakeIcon(iconPath);
                btn.clicked += () => SetIcon(go, iconPath);
                iconRow.Add(btn);
            }
        }

        private void SetIcon(GameObject go, string iconPath)
        {
            bool needRemoveIcon = string.IsNullOrEmpty(iconPath);
            SaintsHierarchyConfig config = Utils.EnsureConfig();

            string scenePath = go.scene.path;
            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            // Debug.Log($"path={scenePath}; guid={sceneGuid}");
            GlobalObjectId goId = GlobalObjectId.GetGlobalObjectIdSlow(go);
            string goIdString = Utils.GlobalObjectIdNormString(goId);

            int foundSceneIndex = -1;
            int sceneIndex = 0;
            foreach (SaintsHierarchyConfig.SceneGuidToGoConfigs sceneGuidToGoConfig in config.sceneGuidToGoConfigsList)
            {
                if (sceneGuidToGoConfig.sceneGuid == sceneGuid)
                {
                    foundSceneIndex = sceneIndex;
                    int gameObjectIndex = 0;
                    foreach (SaintsHierarchyConfig.GameObjectConfig gameObjectConfig in sceneGuidToGoConfig.configs)
                    {
                        if (gameObjectConfig.globalObjectIdString == goIdString)
                        {
                            if (needRemoveIcon)
                            {
                                sceneGuidToGoConfig.configs.RemoveAt(gameObjectIndex);
                            }
                            else
                            {
                                sceneGuidToGoConfig.configs[gameObjectIndex] =
                                    MakeGameObjectConfig(goIdString, iconPath);
                            }
                            EditorUtility.SetDirty(config);
                            NeedCloseEvent.Invoke(true);
                            return;
                        }

                        gameObjectIndex++;
                    }

                    break;
                }

                sceneIndex++;
            }

            if (needRemoveIcon)
            {
                return;
            }

            SaintsHierarchyConfig.GameObjectConfig newConfig = MakeGameObjectConfig(goIdString, iconPath);
            if (foundSceneIndex == -1)
            {
                EditorUtility.SetDirty(config);
                config.sceneGuidToGoConfigsList.Add(new SaintsHierarchyConfig.SceneGuidToGoConfigs
                {
                    sceneGuid = sceneGuid,
                    configs = new List<SaintsHierarchyConfig.GameObjectConfig>
                    {
                        newConfig,
                    },
                });
            }

            else
            {
                SaintsHierarchyConfig.SceneGuidToGoConfigs targetList = config.sceneGuidToGoConfigsList[foundSceneIndex];
                EditorUtility.SetDirty(config);
                targetList.configs.Add(newConfig);
            }

            NeedCloseEvent.Invoke(true);
        }

        private SaintsHierarchyConfig.GameObjectConfig MakeGameObjectConfig(string goIdString, string iconPath)
        {
            return new SaintsHierarchyConfig.GameObjectConfig
            {
                globalObjectIdString = goIdString,
                icon = iconPath,
            };
        }

        private Button MakeIcon(string iconPath)
        {
            return new Button()
            {
                style =
                {
                    backgroundImage = Utils.LoadResource<Texture2D>(iconPath),
                    width = 20,
                    height = 20,

#if UNITY_2022_2_OR_NEWER
                    backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center),
                    backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center),
                    backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat),
                    backgroundSize = new BackgroundSize(16, 16),
#else
                    unityBackgroundScaleMode = ScaleMode.ScaleToFit,
#endif
                },
            };
        }
    }
}
