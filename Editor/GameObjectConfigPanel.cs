using System;
using System.Collections.Generic;
using System.Linq;
using SaintsHierarchy.Editor.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace SaintsHierarchy.Editor
{
    public class GameObjectConfigPanel : VisualElement
    {
        public readonly UnityEvent<bool> NeedCloseEvent = new UnityEvent<bool>();
        private static VisualTreeAsset _gameObjectConfigTemplate;

        private readonly struct IconInfo : IEquatable<IconInfo>
        {
            public readonly string Path;
            public readonly Button Button;

            public IconInfo(string path, Button button)
            {
                Path = path;
                Button = button;
            }

            public bool Equals(IconInfo other)
            {
                return Path == other.Path;
            }

            public override bool Equals(object obj)
            {
                return obj is IconInfo other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (Path != null ? Path.GetHashCode() : 0);
            }
        }

        private readonly List<IconInfo> _iconInfos = new List<IconInfo>();

        private static readonly Color[] Colors = {
            new Color(0.16f, 0.16f, 0.16f),
            new Color(0.609f, 0.231f, 0.23100014f),
            new Color(0.55825f, 0.471625f, 0.21175f),
            new Color(0.34999996f, 0.5075f, 0.1925f),
            new Color(0.1925f, 0.5075f, 0.27124998f),
            new Color(0.1925f, 0.50750005f, 0.5075f),
            new Color(0.259875f, 0.36618757f, 0.685125f),
            new Color(0.4550001f, 0.25024998f, 0.65975f),
            new Color(0.53287494f, 0.20212498f, 0.4501876f),
        };

        private static Texture2D _closeIcon;

        public GameObjectConfigPanel(GameObject go, GameObjectConfig goConfig)
        {
            _gameObjectConfigTemplate ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/GameObjectConfig.uxml");
            TemplateContainer root = _gameObjectConfigTemplate.CloneTree();
            Add(root);

            VisualElement colorRow = root.Q<VisualElement>(name: "ColorContainer");

            ItemButtonElement noColorButton = MakeIconButton(_closeIcon ??= Util.LoadResource<Texture2D>("close.png"));
            colorRow.Insert(1, noColorButton);
            noColorButton.Button.tooltip = "Remove Color Config";
            noColorButton.Button.clicked += () => SetColor(go, false, default, true);

            List<ItemButtonElement> colorButtons = new List<ItemButtonElement>(Colors.Length);
            foreach (Color color in Colors)
            {
                ItemButtonElement colorButton = MakeColorButton(color);
                colorRow.Add(colorButton);

                if (goConfig.hasColor && goConfig.color == color)
                {
                    colorButton.SetSelected(true);
                    colorButton.Button.clicked += () => SetColor(go, false, default, true);
                }
                else
                {
                    colorButton.Button.clicked += () => SetColor(go, true, color, true);
                }
                colorButtons.Add(colorButton);
            }

            ColorField colorField = colorRow.Q<ColorField>(name: "CustomColor");
            colorField.tooltip = "Custom Color";
            colorField.value = goConfig.hasColor ? goConfig.color : Color.black;
            colorField.RegisterValueChangedCallback(evt =>
            {
                Color newColor = evt.newValue;
                SetColor(go, true, newColor, false);
                foreach (ItemButtonElement presetColorButton in colorButtons)
                {
                    presetColorButton.SetSelected(false);
                }
                EditorApplication.RepaintHierarchyWindow();
            });

#if !UNITY_6000_3_OR_NEWER
            colorField.style.width = 46;
            colorRow.Q<VisualElement>(name: "CustomColorIcon").style.display = DisplayStyle.None;
#endif

            IconPickerElement iconPickerElement = root.Q<IconPickerElement>();
            iconPickerElement.value = goConfig.icon;
            iconPickerElement.RegisterValueChangedCallback(evt => SetIcon(go, evt.newValue));
            RegisterCallback<AttachToPanelEvent>(_ => iconPickerElement.Search.Focus());
        }

        private void SetIcon(GameObject go, string iconPath)
        {
            bool needRemoveIcon = string.IsNullOrEmpty(iconPath);
            // SaintsHierarchyConfig config = Util.EnsureConfig();

            string scenePath = go.scene.path;
            if (string.IsNullOrEmpty(scenePath))
            {
                scenePath = AssetDatabase.GetAssetPath(go);
            }
            // Debug.Log($"scenePath={scenePath}");
            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            // Debug.Log($"path={scenePath}; guid={sceneGuid}");
            GlobalObjectId goId = GlobalObjectId.GetGlobalObjectIdSlow(go);
            string goIdString = Util.GlobalObjectIdNormString(goId);

            int foundSceneIndex = -1;
            int sceneIndex = 0;

            bool personalDisabled = !PersonalHierarchyConfig.instance.personalEnabled;
            List<SceneGuidToGoConfigs> sceneGuidToGoConfigsList = (PersonalHierarchyConfig.instance.personalEnabled
                ? PersonalHierarchyConfig.instance.sceneGuidToGoConfigsList
                : SaintsHierarchyConfig.instance.sceneGuidToGoConfigsList);

            foreach (SceneGuidToGoConfigs sceneGuidToGoConfig in sceneGuidToGoConfigsList)
            {
                if (sceneGuidToGoConfig.sceneGuid == sceneGuid)
                {
                    foundSceneIndex = sceneIndex;
                    int gameObjectIndex = 0;
                    foreach (GameObjectConfig gameObjectConfig in sceneGuidToGoConfig.configs)
                    {
                        if (gameObjectConfig.globalObjectIdString == goIdString)
                        {
                            if (!gameObjectConfig.hasColor && needRemoveIcon)
                            {
                                sceneGuidToGoConfig.configs.RemoveAt(gameObjectIndex);
                            }
                            else
                            {
                                sceneGuidToGoConfig.configs[gameObjectIndex] =
                                    MakeGameObjectConfig(gameObjectConfig, iconPath);
                            }

                            EditorUtility.SetDirty(personalDisabled? SaintsHierarchyConfig.instance: PersonalHierarchyConfig.instance);
                            NeedCloseEvent.Invoke(true);

                            if (personalDisabled)
                            {
                                SaintsHierarchyConfig.instance.SaveToDisk();
                            }
                            else
                            {
                                PersonalHierarchyConfig.instance.SaveToDisk();
                            }
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

            GameObjectConfig newConfig = MakeGameObjectConfig(new GameObjectConfig
            {
                globalObjectIdString = goIdString,
            }, iconPath);

            EditorUtility.SetDirty(personalDisabled? SaintsHierarchyConfig.instance: PersonalHierarchyConfig.instance);
            if (foundSceneIndex == -1)
            {

                sceneGuidToGoConfigsList.Add(new SceneGuidToGoConfigs
                {
                    sceneGuid = sceneGuid,
                    configs = new List<GameObjectConfig>
                    {
                        newConfig,
                    },
                });
            }

            else
            {
                SceneGuidToGoConfigs targetList = sceneGuidToGoConfigsList[foundSceneIndex];
                targetList.configs.Add(newConfig);
            }

            if (personalDisabled)
            {
                SaintsHierarchyConfig.instance.SaveToDisk();
            }
            else
            {
                PersonalHierarchyConfig.instance.SaveToDisk();
            }

            NeedCloseEvent.Invoke(true);
        }

        private void SetColor(GameObject go, bool hasColor, Color color, bool needClose)
        {
            bool personalDisabled = !PersonalHierarchyConfig.instance.personalEnabled;
            List<SceneGuidToGoConfigs> sceneGuidToGoConfigsList = personalDisabled
                ? SaintsHierarchyConfig.instance.sceneGuidToGoConfigsList
                : PersonalHierarchyConfig.instance.sceneGuidToGoConfigsList;

            string scenePath = go.scene.path;
            if (string.IsNullOrEmpty(scenePath))
            {
                scenePath = AssetDatabase.GetAssetPath(go);
            }
            // Debug.Log($"scenePath={scenePath}");
            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            // Debug.Log($"path={scenePath}; guid={sceneGuid}");
            GlobalObjectId goId = GlobalObjectId.GetGlobalObjectIdSlow(go);
            string goIdString = Util.GlobalObjectIdNormString(goId);

            int foundSceneIndex = -1;
            int sceneIndex = 0;
            foreach (SceneGuidToGoConfigs sceneGuidToGoConfig in sceneGuidToGoConfigsList)
            {
                if (sceneGuidToGoConfig.sceneGuid == sceneGuid)
                {
                    foundSceneIndex = sceneIndex;
                    int gameObjectIndex = 0;
                    foreach (GameObjectConfig gameObjectConfig in sceneGuidToGoConfig.configs)
                    {
                        if (gameObjectConfig.globalObjectIdString == goIdString)
                        {
                            if (string.IsNullOrEmpty(gameObjectConfig.icon) && !hasColor)
                            {
                                sceneGuidToGoConfig.configs.RemoveAt(gameObjectIndex);
                            }
                            else
                            {
                                sceneGuidToGoConfig.configs[gameObjectIndex] =
                                    MakeGameObjectColorConfig(gameObjectConfig, hasColor, color);
                            }
                            EditorUtility.SetDirty(personalDisabled? SaintsHierarchyConfig.instance: PersonalHierarchyConfig.instance);
                            if(needClose)
                            {
                                NeedCloseEvent.Invoke(true);
                            }
                            return;
                        }

                        gameObjectIndex++;
                    }

                    break;
                }

                sceneIndex++;
            }

            if (!hasColor)
            {
                return;
            }

            GameObjectConfig newConfig = MakeGameObjectColorConfig(new GameObjectConfig
            {
                globalObjectIdString = goIdString,
            }, true, color);
            if (foundSceneIndex == -1)
            {
                EditorUtility.SetDirty(personalDisabled? SaintsHierarchyConfig.instance: PersonalHierarchyConfig.instance);
                sceneGuidToGoConfigsList.Add(new SceneGuidToGoConfigs
                {
                    sceneGuid = sceneGuid,
                    configs = new List<GameObjectConfig>
                    {
                        newConfig,
                    },
                });
            }
            else
            {
                SceneGuidToGoConfigs targetList = sceneGuidToGoConfigsList[foundSceneIndex];
                EditorUtility.SetDirty(personalDisabled? SaintsHierarchyConfig.instance: PersonalHierarchyConfig.instance);
                targetList.configs.Add(newConfig);
            }

            if (personalDisabled)
            {
                SaintsHierarchyConfig.instance.SaveToDisk();
            }
            else
            {
                PersonalHierarchyConfig.instance.SaveToDisk();
            }

            if(needClose)
            {
                NeedCloseEvent.Invoke(true);
            }
        }

        private static GameObjectConfig MakeGameObjectColorConfig(GameObjectConfig gameObjectConfig, bool hasColor, Color color)
        {
            return new GameObjectConfig
            {
                globalObjectIdString = gameObjectConfig.globalObjectIdString,
                icon = gameObjectConfig.icon,
                hasColor = hasColor,
                color = color,
            };
        }


        private static GameObjectConfig MakeGameObjectConfig(GameObjectConfig gameObjectConfig, string iconPath)
        {
            return new GameObjectConfig
            {
                globalObjectIdString = gameObjectConfig.globalObjectIdString,
                icon = iconPath,
                hasColor =  gameObjectConfig.hasColor,
                color = gameObjectConfig.color,
            };
        }

        private static ItemButtonElement MakeIconButton(Texture2D icon)
        {
            ItemButtonElement itemButtonElement = new ItemButtonElement();
            itemButtonElement.Button.style.backgroundImage = icon;
            return itemButtonElement;
        }

        private static Texture2D _whiteRectTexture;

        private static ItemButtonElement MakeColorButton(Color color)
        {
            _whiteRectTexture ??= Util.LoadResource<Texture2D>("rect.png");

            ItemButtonElement itemButtonElement = new ItemButtonElement();
            itemButtonElement.Button.style.backgroundImage = _whiteRectTexture;
            itemButtonElement.Button.style.unityBackgroundImageTintColor = color;
            return itemButtonElement;
        }
    }
}
