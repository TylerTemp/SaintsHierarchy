using System;
using System.Collections.Generic;
using System.Linq;
using SaintsHierarchy.Editor.Editor_Default_Resources;
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

        private static readonly string[] DefaultIcons =
        {
            "d_Folder Icon",
            "d_FolderFavorite Icon",
            "d_greenLight",
            "d_Canvas Icon",
            "d_AvatarMask On Icon",
            "d_cs Script Icon",
            "d_StandaloneInputModule Icon",
            "d_EventSystem Icon",
            "d_Terrain Icon",
            "d_ScriptableObject Icon",

            "d_Camera Icon",
            "d_ParticleSystem Icon",
            "LineRenderer Icon",
            "d_Material Icon",
            "d_ReflectionProbe Icon",

            "d_Light Icon",
            "d_DirectionalLight Icon",
            "d_LightmapParameters Icon",
            "d_LightProbes Icon",

            "d_Rigidbody2D Icon",
            "d_BoxCollider Icon",
            "d_BoxCollider2D Icon",
            "d_SphereCollider Icon",
            "d_CircleCollider2D Icon",
            "d_CapsuleCollider Icon",
            "d_WheelCollider Icon",
            "d_MeshCollider Icon",

            "d_AudioSource Icon",
            "d_AudioDistortionFilter Icon",
            "d_AudioListener Icon",
            "d_AudioEchoFilter Icon",
            "d_AudioReverbFilter Icon",

            "d_Prefab On Icon",
            "d_PreMatSphere",
            "d_PreMatCylinder",
            "d_Favorite Icon",
            "d_Settings Icon",

            "sv_icon_dot10_pix16_gizmo",
            "sv_icon_dot11_pix16_gizmo",
            "sv_icon_dot12_pix16_gizmo",
            "sv_icon_dot13_pix16_gizmo",
            "sv_icon_dot14_pix16_gizmo",
            "sv_icon_dot15_pix16_gizmo",

            "sv_icon_dot0_pix16_gizmo",
            "sv_icon_dot1_pix16_gizmo",
            "sv_icon_dot2_pix16_gizmo",
            "sv_icon_dot3_pix16_gizmo",
            "sv_icon_dot4_pix16_gizmo",
            "sv_icon_dot5_pix16_gizmo",
            "sv_icon_dot6_pix16_gizmo",
            "sv_icon_dot7_pix16_gizmo",

            "d_greenLight",
            "d_orangeLight",
            "d_lightOff",
            "d_lightRim",
        };

        public GameObjectConfigPanel(GameObject go, string customIcon)
        {
            _gameObjectConfigTemplate ??= Utils.LoadResource<VisualTreeAsset>("UIToolkit/GameObjectConfig.uxml");
            TemplateContainer root = _gameObjectConfigTemplate.CloneTree();
            Add(root);

            VisualElement iconRow = root.Q<ScrollView>(name: "IconContainer").contentContainer;

            ItemButtonElement customButton = MakeIcon(null);
            iconRow.Add(customButton);
            customButton.Button.tooltip = "Current Custom Icon";
            customButton.Button.clicked += () => SetIcon(go, "");
            if(!string.IsNullOrEmpty(customIcon) && !DefaultIcons.Contains(customIcon))
            {
                customButton.Button.style.backgroundImage = Utils.LoadResource<Texture2D>(customIcon);
            }
            else
            {
                customButton.style.display = DisplayStyle.None;
            }

            ItemButtonElement searchButton = MakeIcon(null);
            iconRow.Add(searchButton);
            searchButton.Button.tooltip = "Searched Icon";
            searchButton.style.display = DisplayStyle.None;

            foreach (string iconPath in DefaultIcons)
            {
                ItemButtonElement btn = MakeIcon(Utils.LoadResource<Texture2D>(iconPath));
                bool isCurrent = iconPath == customIcon;
                if (isCurrent)
                {
                    btn.Button.AddToClassList("ItemButtonSelected");
                    btn.Button.clicked += () => SetIcon(go, "");
                }
                else
                {
                    btn.Button.clicked += () => SetIcon(go, iconPath);
                }
                btn.Button.tooltip = iconPath;

                iconRow.Add(btn);
                _iconInfos.Add(new IconInfo(iconPath, btn.Button));
            }

            ToolbarSearchField search = root.Q<ToolbarSearchField>();
            search.RegisterValueChangedCallback(evt =>
            {
                string searchText = evt.newValue.TrimEnd();
                List<IconInfo> matchedInfos = new List<IconInfo>();
                bool isNullOrEmpty = string.IsNullOrEmpty(searchText);
                if (isNullOrEmpty)
                {
                    matchedInfos = _iconInfos;
                }
                else
                {
                    string[] searchLowParts = searchText.ToLower().Split();
                    matchedInfos.AddRange(_iconInfos.Where(each => TextSearch(each.Path.ToLower(), searchLowParts)));
                }

                foreach (IconInfo iconInfo in _iconInfos)
                {
                    DisplayStyle display = matchedInfos.Contains(iconInfo)
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                    iconInfo.Button.style.display = display;
                }

                if (isNullOrEmpty || DefaultIcons.Contains(searchText))
                {
                    searchButton.style.display = DisplayStyle.None;
                    return;
                }

                Texture2D icon = Utils.LoadResource<Texture2D>(searchText);
                if (icon == null)
                {
                    searchButton.style.display = DisplayStyle.None;
                    return;
                }

                searchButton.Button.style.backgroundImage = icon;
                // searchButton.userData = icon;
                searchButton.style.display = DisplayStyle.Flex;
            });

            searchButton.Button.clicked += () =>
            {
                string searchData = search.value;
                if(!string.IsNullOrEmpty(searchData))
                {
                    SetIcon(go, searchData);
                }
            };


            RegisterCallback<AttachToPanelEvent>(_ => search.Focus());
        }

        private static bool TextSearch(string target, string[] searchLowParts)
        {
            return searchLowParts.All(target.Contains);
        }

        private void SetIcon(GameObject go, string iconPath)
        {
            bool needRemoveIcon = string.IsNullOrEmpty(iconPath);
            SaintsHierarchyConfig config = Utils.EnsureConfig();

            string scenePath = go.scene.path;
            if (string.IsNullOrEmpty(scenePath))
            {
                scenePath = AssetDatabase.GetAssetPath(go);
            }
            // Debug.Log($"scenePath={scenePath}");
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

        private static SaintsHierarchyConfig.GameObjectConfig MakeGameObjectConfig(string goIdString, string iconPath)
        {
            return new SaintsHierarchyConfig.GameObjectConfig
            {
                globalObjectIdString = goIdString,
                icon = iconPath,
            };
        }

        private static ItemButtonElement MakeIcon(Texture2D icon)
        {
            ItemButtonElement itemButtonElement = new ItemButtonElement();
            itemButtonElement.Button.style.backgroundImage = icon;
            return itemButtonElement;
//             return new Button
//             {
//                 style =
//                 {
//                     backgroundImage = Utils.LoadResource<Texture2D>(icon),
//                     width = 20,
//                     height = 20,
//
// #if UNITY_2022_2_OR_NEWER
//                     backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center),
//                     backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center),
//                     backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat),
//                     backgroundSize = new BackgroundSize(16, 16),
// #else
//                     unityBackgroundScaleMode = ScaleMode.ScaleToFit,
// #endif
//                 },
//             };
        }
    }
}
