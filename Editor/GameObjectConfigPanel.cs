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

            VisualElement iconRow = root.Q<ScrollView>(name: "IconContainer").contentContainer;
            ToolbarSearchField search = root.Q<ToolbarSearchField>();

            RefreshIconRows(search, iconRow, go, goConfig);
            search.RegisterValueChangedCallback(_ => RefreshIconRows(search, iconRow, go, goConfig));

            // searchButton.Button.clicked += () =>
            // {
            //     string searchData = search.value;
            //     if(!string.IsNullOrEmpty(searchData))
            //     {
            //         SetIcon(go, searchData);
            //     }
            // };


            RegisterCallback<AttachToPanelEvent>(_ => search.Focus());
        }

        private void RefreshIconRows(ToolbarSearchField search, VisualElement iconRow, GameObject go, GameObjectConfig goConfig)
        {
            _iconInfos.Clear();
            iconRow.Clear();

            ItemButtonElement customButton = MakeIconButton(null);
            iconRow.Add(customButton);
            customButton.Button.tooltip = "Current Custom Icon";
            customButton.Button.clicked += () => SetIcon(go, "");

            IReadOnlyList<string> presetIcons;
            if (string.IsNullOrWhiteSpace(search.value))
            {
                presetIcons = DefaultIcons;
            }
            else
            {
                string searchText = search.value.Trim();
                string[] searchLowParts = searchText.ToLower().Split();
                presetIcons = FullIcons.Where(each => TextSearch(each.ToLower(), searchLowParts)).ToArray();

                Texture2D searchedIcon = Util.LoadResource<Texture2D>(searchText);
                if(searchedIcon != null)
                {
                    ItemButtonElement searchInputDirectButton = MakeIconButton(searchedIcon);
                    iconRow.Add(searchInputDirectButton);
                    searchInputDirectButton.Button.tooltip = "Searched Icon";
                    // searchInputDirectButton.style.display = DisplayStyle.None;

                    if(!string.IsNullOrEmpty(goConfig.icon) && goConfig.icon == searchText)
                    {
                        searchInputDirectButton.SetSelected(true);
                    }

                    searchInputDirectButton.Button.clicked += () =>
                    {
                        string searchData = search.value;
                        if (!string.IsNullOrEmpty(searchData))
                        {
                            SetIcon(go, searchData);
                        }
                    };
                }
            }

            if(!string.IsNullOrEmpty(goConfig.icon) && !presetIcons.Contains(goConfig.icon))
            {
                customButton.Button.style.backgroundImage = Util.LoadResource<Texture2D>(goConfig.icon);
            }
            else
            {
                customButton.style.display = DisplayStyle.None;
            }

            if (!string.IsNullOrEmpty(goConfig.icon))  // has icon
            {
                ItemButtonElement noIconButton = MakeIconButton(_closeIcon ??= Util.LoadResource<Texture2D>("close.png"));
                noIconButton.Button.tooltip = "Delete Icon Config";
                noIconButton.Button.clicked += () => SetIcon(go, "");
                iconRow.Add(noIconButton);
            }

            foreach (string iconPath in presetIcons)
            {
                ItemButtonElement btn = MakeIconButton(Util.LoadResource<Texture2D>(iconPath));
                bool isCurrent = iconPath == goConfig.icon;
                if (isCurrent)
                {
                    btn.SetSelected(true);
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


        }

        private static bool TextSearch(string target, string[] searchLowParts)
        {
            return searchLowParts.All(target.Contains);
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

        private static readonly string[] DefaultIcons =
        {
            "transparent.png",
            "d_Folder Icon",
            "d_FolderFavorite Icon",
            "d_Canvas Icon",
            "d_AvatarMask On Icon",
            "d_cs Script Icon",
            "d_StandaloneInputModule Icon",
            "d_EventSystem Icon",
            "d_Terrain Icon",
            "d_ScriptableObject Icon",

            "d_Camera Icon",
            "d_ParticleSystem Icon",
            "d_LineRenderer Icon",
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
            "d_redLight",

            "d_lightOff",
            "d_lightRim",
        };

        // See: https://github.com/nukadelic/UnityEditorIcons/blob/master/EditorIcons.cs
        private static readonly string[] FullIcons =
        {
            "_Help","_Popup","aboutwindow.mainheader","ageialogo","AlphabeticalSorting","Animation.AddEvent",
            "Animation.AddKeyframe","Animation.EventMarker","Animation.FirstKey","Animation.LastKey",
            "Animation.NextKey","Animation.Play","Animation.PrevKey","Animation.Record","Animation.SequencerLink",
            "animationanimated","animationdopesheetkeyframe","animationkeyframe","animationnocurve",
            "animationvisibilitytoggleoff","animationvisibilitytoggleon","AnimationWrapModeMenu","AssemblyLock",
            "Asset Store","Audio Mixer","AvatarCompass","AvatarController.Layer","AvatarController.LayerHover",
            "AvatarController.LayerSelected","BodyPartPicker","BodySilhouette","DotFill","DotFrame","DotFrameDotted",
            "DotSelection","Head","HeadIk","HeadZoom","HeadZoomSilhouette","LeftArm","LeftFeetIk","LeftFingers",
            "LeftFingersIk","LeftHandZoom","LeftHandZoomSilhouette","LeftLeg","MaskEditor_Root","RightArm","RightFeetIk",
            "RightFingers","RightFingersIk","RightHandZoom","RightHandZoomSilhouette","RightLeg","Torso","AvatarPivot",
            "back","back@2x","beginButton-On","beginButton","blendKey","blendKeyOverlay","blendKeySelected",
            "blendSampler","blueGroove","BuildSettings.Android","BuildSettings.Android.Small","BuildSettings.Broadcom",
            "BuildSettings.Editor","BuildSettings.Editor.Small","BuildSettings.Facebook",
            "BuildSettings.Facebook.Small","BuildSettings.FlashPlayer","BuildSettings.FlashPlayer.Small",
            "BuildSettings.iPhone","BuildSettings.iPhone.Small","BuildSettings.Lumin","BuildSettings.Lumin.small",
            "BuildSettings.Metro","BuildSettings.Metro.Small","BuildSettings.N3DS","BuildSettings.N3DS.Small",
            "BuildSettings.PS4","BuildSettings.PS4.Small","BuildSettings.PSM","BuildSettings.PSM.Small",
            "BuildSettings.PSP2","BuildSettings.PSP2.Small","BuildSettings.SelectedIcon","BuildSettings.Standalone",
            "BuildSettings.Standalone.Small","BuildSettings.StandaloneBroadcom.Small",
            "BuildSettings.StandaloneGLES20Emu.Small","BuildSettings.StandaloneGLESEmu",
            "BuildSettings.StandaloneGLESEmu.Small","BuildSettings.Switch","BuildSettings.Switch.Small",
            "BuildSettings.tvOS","BuildSettings.tvOS.Small","BuildSettings.Web","BuildSettings.Web.Small",
            "BuildSettings.WebGL","BuildSettings.WebGL.Small","BuildSettings.WP8","BuildSettings.WP8.Small",
            "BuildSettings.Xbox360","BuildSettings.Xbox360.Small","BuildSettings.XboxOne",
            "BuildSettings.XboxOne.Small","BuildSettings.Xiaomi","Camera Gizmo","CheckerFloor","Clipboard",
            "ClothInspector.PaintTool","ClothInspector.PaintValue","ClothInspector.SelectTool",
            "ClothInspector.SettingsTool","ClothInspector.ViewValue","CloudConnect","Collab.Build",
            "Collab.BuildFailed","Collab.BuildSucceeded","Collab.FileAdded","Collab.FileConflict","Collab.FileDeleted",
            "Collab.FileIgnored","Collab.FileMoved","Collab.FileUpdated","Collab.FolderAdded","Collab.FolderConflict",
            "Collab.FolderDeleted","Collab.FolderIgnored","Collab.FolderMoved","Collab.FolderUpdated",
            "Collab.NoInternet","Collab","Collab.Warning","CollabConflict","CollabError","CollabNew","CollabOffline",
            "CollabProgress","CollabPull","CollabPush","ColorPicker.ColorCycle","ColorPicker.CycleColor",
            "ColorPicker.CycleSlider","ColorPicker.SliderCycle","console.erroricon.inactive.sml","console.erroricon",
            "console.erroricon.sml","console.infoicon","console.infoicon.sml","console.warnicon.inactive.sml",
            "console.warnicon","console.warnicon.sml","curvekeyframe","curvekeyframeselected",
            "curvekeyframeselectedoverlay","curvekeyframesemiselectedoverlay","curvekeyframeweighted","CustomSorting",
            "d__Popup","d_aboutwindow.mainheader","d_ageialogo","d_AlphabeticalSorting","d_Animation.AddEvent",
            "d_Animation.AddKeyframe","d_Animation.EventMarker","d_Animation.FirstKey","d_Animation.LastKey",
            "d_Animation.NextKey","d_Animation.Play","d_Animation.PrevKey","d_Animation.Record",
            "d_Animation.SequencerLink","d_animationanimated","d_animationkeyframe","d_animationnocurve",
            "d_animationvisibilitytoggleoff","d_animationvisibilitytoggleon","d_AnimationWrapModeMenu",
            "d_AS Badge Delete","d_AS Badge New","d_AssemblyLock","d_Asset Store","d_Audio Mixer",
            "d_AvatarBlendBackground","d_AvatarBlendLeft","d_AvatarBlendLeftA","d_AvatarBlendRight",
            "d_AvatarBlendRightA","d_AvatarCompass","d_AvatarPivot","d_back","d_back@2x","d_beginButton-On",
            "d_beginButton","d_blueGroove","d_BuildSettings.Android","d_BuildSettings.Android.Small",
            "d_BuildSettings.Broadcom","d_BuildSettings.FlashPlayer","d_BuildSettings.FlashPlayer.Small",
            "d_BuildSettings.iPhone","d_BuildSettings.iPhone.Small","d_BuildSettings.Lumin",
            "d_BuildSettings.Lumin.small","d_BuildSettings.PS4","d_BuildSettings.PS4.Small","d_BuildSettings.PSP2",
            "d_BuildSettings.PSP2.Small","d_BuildSettings.SelectedIcon","d_BuildSettings.Standalone",
            "d_BuildSettings.Standalone.Small","d_BuildSettings.tvOS","d_BuildSettings.tvOS.Small",
            "d_BuildSettings.Web","d_BuildSettings.Web.Small","d_BuildSettings.WebGL","d_BuildSettings.WebGL.Small",
            "d_BuildSettings.Xbox360","d_BuildSettings.Xbox360.Small","d_BuildSettings.XboxOne",
            "d_BuildSettings.XboxOne.Small","d_CheckerFloor","d_CloudConnect","d_Collab.FileAdded",
            "d_Collab.FileConflict","d_Collab.FileDeleted","d_Collab.FileIgnored","d_Collab.FileMoved",
            "d_Collab.FileUpdated","d_Collab.FolderAdded","d_Collab.FolderConflict","d_Collab.FolderDeleted",
            "d_Collab.FolderIgnored","d_Collab.FolderMoved","d_Collab.FolderUpdated","d_ColorPicker.CycleColor",
            "d_ColorPicker.CycleSlider","d_console.erroricon","d_console.erroricon.sml","d_console.infoicon",
            "d_console.infoicon.sml","d_console.warnicon","d_console.warnicon.sml","d_curvekeyframe",
            "d_curvekeyframeselected","d_curvekeyframeselectedoverlay","d_curvekeyframesemiselectedoverlay",
            "d_curvekeyframeweighted","d_CustomSorting","d_DefaultSorting","d_EditCollider","d_editcollision_16",
            "d_editconstraints_16","d_editicon.sml","d_endButton-On","d_endButton","d_eyeDropper.Large",
            "d_eyeDropper.sml","d_Favorite","d_FilterByLabel","d_FilterByType","d_FilterSelectedOnly",
            "d_FilterSelectedOnly@2x","d_forward","d_forward@2x","d_GEAR","d_Groove","d_HorizontalSplit",
            "d_icon dropdown","d_InspectorLock","d_JointAngularLimits","d_leftBracket","d_Lighting",
            "d_LightmapEditor.WindowTitle","d_LookDevCenterLight","d_LookDevCenterLight@2x","d_LookDevClose",
            "d_LookDevClose@2x","d_LookDevEnvRotation","d_LookDevEnvRotation@2x","d_LookDevMirrorViews",
            "d_LookDevMirrorViews@2x","d_LookDevMirrorViewsActive","d_LookDevMirrorViewsActive@2x",
            "d_LookDevMirrorViewsInactive","d_LookDevMirrorViewsInactive@2x","d_LookDevObjRotation",
            "d_LookDevObjRotation@2x","d_LookDevPaneOption","d_LookDevPaneOption@2x","d_LookDevResetEnv",
            "d_LookDevResetEnv@2x","d_LookDevShadow","d_LookDevShadow@2x","d_LookDevSideBySide",
            "d_LookDevSideBySide@2x","d_LookDevSingle1","d_LookDevSingle1@2x","d_LookDevSingle2",
            "d_LookDevSingle2@2x","d_LookDevSplit","d_LookDevSplit@2x","d_LookDevZone","d_LookDevZone@2x",
            "d_Mirror","d_model large","d_monologo","d_MoveTool on","d_MoveTool","d_Navigation","d_Occlusion",
            "d_P4_AddedLocal","d_P4_AddedRemote","d_P4_CheckOutLocal","d_P4_CheckOutRemote","d_P4_Conflicted",
            "d_P4_DeletedLocal","d_P4_DeletedRemote","d_P4_Local","d_P4_LockedLocal","d_P4_LockedRemote",
            "d_P4_OutOfSync","d_Particle Effect","d_PauseButton On","d_PauseButton","d_PlayButton On","d_PlayButton",
            "d_PlayButtonProfile On","d_PlayButtonProfile","d_playLoopOff","d_playLoopOn","d_preAudioAutoPlayOff",
            "d_preAudioAutoPlayOn","d_preAudioLoopOff","d_preAudioLoopOn","d_preAudioPlayOff","d_preAudioPlayOn",
            "d_PreMatCube","d_PreMatCylinder","d_PreMatLight0","d_PreMatLight1","d_PreMatSphere","d_PreMatTorus",
            "d_Preset.Context","d_PreTextureAlpha","d_PreTextureMipMapHigh","d_PreTextureMipMapLow","d_PreTextureRGB",
            "d_Profiler.Audio","d_Profiler.CPU","d_Profiler.FirstFrame","d_Profiler.GPU","d_Profiler.LastFrame",
            "d_Profiler.Memory","d_Profiler.Network","d_Profiler.NextFrame","d_Profiler.Physics","d_Profiler.PrevFrame",
            "d_Profiler.Record","d_Profiler.Rendering","d_Profiler.Video","d_ProfilerColumn.WarningCount","d_Project",
            "d_RectTool On","d_RectTool","d_RectTransformBlueprint","d_RectTransformRaw","d_redGroove","d_Refresh",
            "d_renderdoc","d_rightBracket","d_RotateTool On","d_RotateTool","d_ScaleTool On","d_ScaleTool",
            "d_SceneViewAlpha","d_SceneViewAudio","d_SceneViewFx","d_SceneViewLighting","d_SceneViewOrtho",
            "d_SceneViewRGB","d_ScrollShadow","d_Settings","d_SettingsIcon","d_SocialNetworks.FacebookShare",
            "d_SocialNetworks.LinkedInShare","d_SocialNetworks.Tweet","d_SocialNetworks.UDNOpen","d_SpeedScale",
            "d_StepButton On","d_StepButton","d_StepLeftButton-On","d_StepLeftButton","d_SVN_AddedLocal",
            "d_SVN_Conflicted","d_SVN_DeletedLocal","d_SVN_Local","d_SVN_LockedLocal","d_SVN_OutOfSync","d_tab_next",
            "d_tab_next@2x","d_tab_prev","d_tab_prev@2x","d_TerrainInspector.TerrainToolLower On",
            "d_TerrainInspector.TerrainToolLowerAlt","d_TerrainInspector.TerrainToolPlants On",
            "d_TerrainInspector.TerrainToolPlants","d_TerrainInspector.TerrainToolPlantsAlt On",
            "d_TerrainInspector.TerrainToolPlantsAlt","d_TerrainInspector.TerrainToolRaise On",
            "d_TerrainInspector.TerrainToolRaise","d_TerrainInspector.TerrainToolSetheight On",
            "d_TerrainInspector.TerrainToolSetheight","d_TerrainInspector.TerrainToolSetheightAlt On",
            "d_TerrainInspector.TerrainToolSetheightAlt","d_TerrainInspector.TerrainToolSettings On",
            "d_TerrainInspector.TerrainToolSettings","d_TerrainInspector.TerrainToolSmoothHeight On",
            "d_TerrainInspector.TerrainToolSmoothHeight","d_TerrainInspector.TerrainToolSplat On",
            "d_TerrainInspector.TerrainToolSplat","d_TerrainInspector.TerrainToolSplatAlt On",
            "d_TerrainInspector.TerrainToolSplatAlt","d_TerrainInspector.TerrainToolTrees On",
            "d_TerrainInspector.TerrainToolTrees","d_TerrainInspector.TerrainToolTreesAlt On",
            "d_TerrainInspector.TerrainToolTreesAlt","d_TimelineDigIn","d_TimelineEditModeMixOFF",
            "d_TimelineEditModeMixON","d_TimelineEditModeReplaceOFF","d_TimelineEditModeReplaceON",
            "d_TimelineEditModeRippleOFF","d_TimelineEditModeRippleON","d_TimelineSelector","d_Toolbar Minus",
            "d_Toolbar Plus More","d_Toolbar Plus","d_ToolHandleCenter","d_ToolHandleGlobal","d_ToolHandleLocal",
            "d_ToolHandlePivot","d_tranp","d_TransformTool On","d_TransformTool","d_tree_icon","d_tree_icon_branch",
            "d_tree_icon_branch_frond","d_tree_icon_frond","d_tree_icon_leaf","d_TreeEditor.AddBranches",
            "d_TreeEditor.AddLeaves","d_TreeEditor.Branch On","d_TreeEditor.Branch","d_TreeEditor.BranchFreeHand On",
            "d_TreeEditor.BranchFreeHand","d_TreeEditor.BranchRotate On","d_TreeEditor.BranchRotate",
            "d_TreeEditor.BranchScale On","d_TreeEditor.BranchScale","d_TreeEditor.BranchTranslate On",
            "d_TreeEditor.BranchTranslate","d_TreeEditor.Distribution On","d_TreeEditor.Distribution",
            "d_TreeEditor.Duplicate","d_TreeEditor.Geometry On","d_TreeEditor.Geometry","d_TreeEditor.Leaf On",
            "d_TreeEditor.Leaf","d_TreeEditor.LeafFreeHand On","d_TreeEditor.LeafFreeHand","d_TreeEditor.LeafRotate On",
            "d_TreeEditor.LeafRotate","d_TreeEditor.LeafScale On","d_TreeEditor.LeafScale",
            "d_TreeEditor.LeafTranslate On","d_TreeEditor.LeafTranslate","d_TreeEditor.Material On",
            "d_TreeEditor.Material","d_TreeEditor.Refresh","d_TreeEditor.Trash","d_TreeEditor.Wind On",
            "d_TreeEditor.Wind","d_UnityEditor.AnimationWindow","d_UnityEditor.ConsoleWindow",
            "d_UnityEditor.DebugInspectorWindow","d_UnityEditor.FindDependencies","d_UnityEditor.GameView",
            "d_UnityEditor.HierarchyWindow","d_UnityEditor.InspectorWindow","d_UnityEditor.LookDevView",
            "d_UnityEditor.ProfilerWindow","d_UnityEditor.SceneHierarchyWindow","d_UnityEditor.SceneView",
            "d_UnityEditor.Timeline.TimelineWindow","d_UnityEditor.VersionControl","d_UnityLogo","d_VerticalSplit",
            "d_ViewToolMove On","d_ViewToolMove","d_ViewToolOrbit On","d_ViewToolOrbit","d_ViewToolZoom On",
            "d_ViewToolZoom","d_VisibilityOff","d_VisibilityOn","d_VUMeterTextureHorizontal","d_VUMeterTextureVertical",
            "d_WaitSpin00","d_WaitSpin01","d_WaitSpin02","d_WaitSpin03","d_WaitSpin04","d_WaitSpin05","d_WaitSpin06",
            "d_WaitSpin07","d_WaitSpin08","d_WaitSpin09","d_WaitSpin10","d_WaitSpin11","d_WelcomeScreen.AssetStoreLogo",
            "d_winbtn_graph","d_winbtn_graph_close_h","d_winbtn_graph_max_h","d_winbtn_graph_min_h",
            "d_winbtn_mac_close","d_winbtn_mac_close_a","d_winbtn_mac_close_h","d_winbtn_mac_inact","d_winbtn_mac_max",
            "d_winbtn_mac_max_a","d_winbtn_mac_max_h","d_winbtn_mac_min","d_winbtn_mac_min_a","d_winbtn_mac_min_h",
            "d_winbtn_win_close","d_winbtn_win_close_a","d_winbtn_win_close_h","d_winbtn_win_max","d_winbtn_win_max_a",
            "d_winbtn_win_max_h","d_winbtn_win_min","d_winbtn_win_min_a","d_winbtn_win_min_h","d_winbtn_win_rest",
            "d_winbtn_win_rest_a","d_winbtn_win_rest_h","DefaultSorting","EditCollider","editcollision_16",
            "editconstraints_16","editicon.sml","endButton-On","endButton","eyeDropper.Large","eyeDropper.sml",
            "Favorite","FilterByLabel","FilterByType","FilterSelectedOnly","FilterSelectedOnly@2x","forward",
            "forward@2x","GEAR","Grid.BoxTool","Grid.Default","Grid.EraserTool","Grid.FillTool","Grid.MoveTool",
            "Grid.PaintTool","Grid.PickingTool","Grid.SelectTool","Groove","align_horizontally",
            "align_horizontally_center","align_horizontally_center_active","align_horizontally_left",
            "align_horizontally_left_active","align_horizontally_right","align_horizontally_right_active",
            "align_vertically","align_vertically_bottom","align_vertically_bottom_active","align_vertically_center",
            "align_vertically_center_active","align_vertically_top","align_vertically_top_active",
            "d_align_horizontally","d_align_horizontally_center","d_align_horizontally_center_active",
            "d_align_horizontally_left","d_align_horizontally_left_active","d_align_horizontally_right",
            "d_align_horizontally_right_active","d_align_vertically","d_align_vertically_bottom",
            "d_align_vertically_bottom_active","d_align_vertically_center","d_align_vertically_center_active",
            "d_align_vertically_top","d_align_vertically_top_active","HorizontalSplit","icon dropdown",
            "InspectorLock","JointAngularLimits","KnobCShape","KnobCShapeMini","leftBracket","Lighting",
            "LightmapEditor.WindowTitle","Lightmapping","d_greenLight","d_lightOff","d_lightRim","d_orangeLight",
            "d_redLight","greenLight","lightOff","lightRim","orangeLight","redLight","LockIcon-On","LockIcon",
            "LookDevCenterLight","LookDevCenterLightl@2x","LookDevClose","LookDevClose@2x","LookDevEnvRotation",
            "LookDevEnvRotation@2x","LookDevEyedrop","LookDevLight","LookDevLight@2x","LookDevMirrorViewsActive",
            "LookDevMirrorViewsActive@2x","LookDevMirrorViewsInactive","LookDevMirrorViewsInactive@2x",
            "LookDevObjRotation","LookDevObjRotation@2x","LookDevPaneOption","LookDevPaneOption@2x","LookDevResetEnv",
            "LookDevResetEnv@2x","LookDevShadow","LookDevShadow@2x","LookDevShadowFrame","LookDevShadowFrame@2x",
            "LookDevSideBySide","LookDevSideBySide@2x","LookDevSingle1","LookDevSingle1@2x","LookDevSingle2",
            "LookDevSingle2@2x","LookDevSplit","LookDevSplit@2x","LookDevZone","LookDevZone@2x","loop","Mirror",
            "monologo","MoveTool on","MoveTool","Navigation","Occlusion","P4_AddedLocal","P4_AddedRemote",
            "P4_BlueLeftParenthesis","P4_BlueRightParenthesis","P4_CheckOutLocal","P4_CheckOutRemote","P4_Conflicted",
            "P4_DeletedLocal","P4_DeletedRemote","P4_Local","P4_LockedLocal","P4_LockedRemote","P4_OutOfSync",
            "P4_RedLeftParenthesis","P4_RedRightParenthesis","P4_Updating","PackageBadgeDelete","PackageBadgeNew",
            "Particle Effect","PauseButton On","PauseButton","PlayButton On","PlayButton","PlayButtonProfile On",
            "PlayButtonProfile","playLoopOff","playLoopOn","playSpeed","preAudioAutoPlayOff","preAudioAutoPlayOn",
            "preAudioLoopOff","preAudioLoopOn","preAudioPlayOff","preAudioPlayOn","PreMatCube","PreMatCylinder",
            "PreMatLight0","PreMatLight1","PreMatQuad","PreMatSphere","PreMatTorus","Preset.Context","PreTextureAlpha",
            "PreTextureArrayFirstSlice","PreTextureArrayLastSlice","PreTextureMipMapHigh","PreTextureMipMapLow",
            "PreTextureRGB","AreaLight Gizmo","AreaLight Icon","Assembly Icon","AssetStore Icon","AudioMixerView Icon",
            "AudioSource Gizmo","Camera Gizmo","CGProgram Icon","ChorusFilter Icon","CollabChanges Icon",
            "CollabChangesConflict Icon","CollabChangesDeleted Icon","CollabConflict Icon","CollabCreate Icon",
            "CollabDeleted Icon","CollabEdit Icon","CollabExclude Icon","CollabMoved Icon","cs Script Icon",
            "d_AudioMixerView Icon","d_CollabChanges Icon","d_CollabChangesConflict Icon","d_CollabChangesDeleted Icon",
            "d_CollabConflict Icon","d_CollabCreate Icon","d_CollabDeleted Icon","d_CollabEdit Icon",
            "d_CollabExclude Icon","d_CollabMoved Icon","d_GridLayoutGroup Icon","d_HorizontalLayoutGroup Icon",
            "d_Prefab Icon","d_PrefabModel Icon","d_PrefabVariant Icon","d_VerticalLayoutGroup Icon",
            "DefaultSlate Icon","DirectionalLight Gizmo","DirectionalLight Icon","DiscLight Gizmo","DiscLight Icon",
            "dll Script Icon","EchoFilter Icon","Favorite Icon","Folder Icon","FolderEmpty Icon",
            "FolderFavorite Icon","GameManager Icon","GridBrush Icon","HighPassFilter Icon",
            "HorizontalLayoutGroup Icon","LensFlare Gizmo","LightingDataAssetParent Icon","LightProbeGroup Gizmo",
            "LightProbeProxyVolume Gizmo","LowPassFilter Icon","Main Light Gizmo","MetaFile Icon",
            "Microphone Icon","MuscleClip Icon","ParticleSystem Gizmo","PointLight Gizmo","Prefab Icon",
            "PrefabModel Icon","PrefabOverlayAdded Icon","PrefabOverlayModified Icon","PrefabOverlayRemoved Icon",
            "PrefabVariant Icon","Projector Gizmo","RaycastCollider Icon","ReflectionProbe Gizmo",
            "ReverbFilter Icon","SceneSet Icon","Search Icon","SoftlockProjectBrowser Icon","SpeedTreeModel Icon",
            "SpotLight Gizmo","Spotlight Icon","SpriteCollider Icon","sv_icon_dot0_pix16_gizmo",
            "sv_icon_dot10_pix16_gizmo","sv_icon_dot11_pix16_gizmo","sv_icon_dot12_pix16_gizmo",
            "sv_icon_dot13_pix16_gizmo","sv_icon_dot14_pix16_gizmo","sv_icon_dot15_pix16_gizmo",
            "sv_icon_dot1_pix16_gizmo","sv_icon_dot2_pix16_gizmo","sv_icon_dot3_pix16_gizmo",
            "sv_icon_dot4_pix16_gizmo","sv_icon_dot5_pix16_gizmo","sv_icon_dot6_pix16_gizmo",
            "sv_icon_dot7_pix16_gizmo","sv_icon_dot8_pix16_gizmo","sv_icon_dot9_pix16_gizmo",
            "AnimatorController Icon","AnimatorState Icon","AnimatorStateMachine Icon",
            "AnimatorStateTransition Icon","BlendTree Icon","AnimationWindowEvent Icon","AudioMixerController Icon",
            "DefaultAsset Icon","EditorSettings Icon","AnyStateNode Icon","HumanTemplate Icon",
            "LightingDataAsset Icon","LightmapParameters Icon","Preset Icon","SceneAsset Icon",
            "SubstanceArchive Icon","AssemblyDefinitionAsset Icon","NavMeshAgent Icon","NavMeshData Icon",
            "NavMeshObstacle Icon","OffMeshLink Icon","AnalyticsTracker Icon","Animation Icon",
            "AnimationClip Icon","AimConstraint Icon","d_AimConstraint Icon","d_LookAtConstraint Icon",
            "d_ParentConstraint Icon","d_PositionConstraint Icon","d_RotationConstraint Icon",
            "d_ScaleConstraint Icon","LookAtConstraint Icon","ParentConstraint Icon","PositionConstraint Icon",
            "RotationConstraint Icon","ScaleConstraint Icon","Animator Icon","AnimatorOverrideController Icon",
            "AreaEffector2D Icon","AudioMixerGroup Icon","AudioMixerSnapshot Icon","AudioSpatializerMicrosoft Icon",
            "AudioChorusFilter Icon","AudioClip Icon","AudioDistortionFilter Icon","AudioEchoFilter Icon",
            "AudioHighPassFilter Icon","AudioListener Icon","AudioLowPassFilter Icon","AudioReverbFilter Icon",
            "AudioReverbZone Icon","AudioSource Icon","Avatar Icon","AvatarMask Icon","BillboardAsset Icon",
            "BillboardRenderer Icon","BoxCollider Icon","BoxCollider2D Icon","BuoyancyEffector2D Icon","Camera Icon",
            "Canvas Icon","CanvasGroup Icon","CanvasRenderer Icon","CapsuleCollider Icon","CapsuleCollider2D Icon",
            "CharacterController Icon","CharacterJoint Icon","CircleCollider2D Icon","Cloth Icon",
            "CompositeCollider2D Icon","ComputeShader Icon","ConfigurableJoint Icon","ConstantForce Icon",
            "ConstantForce2D Icon","Cubemap Icon","d_Canvas Icon","d_CanvasGroup Icon","d_CanvasRenderer Icon",
            "d_GameObject Icon","d_LightProbeProxyVolume Icon","d_ParticleSystem Icon","d_ParticleSystemForceField Icon",
            "d_RectTransform Icon","d_StreamingController Icon","DistanceJoint2D Icon","EdgeCollider2D Icon",
            "d_EventSystem Icon","d_EventTrigger Icon","d_Physics2DRaycaster Icon","d_PhysicsRaycaster Icon",
            "d_StandaloneInputModule Icon","d_TouchInputModule Icon","EventSystem Icon","EventTrigger Icon",
            "HoloLensInputModule Icon","Physics2DRaycaster Icon","PhysicsRaycaster Icon","StandaloneInputModule Icon",
            "TouchInputModule Icon","SpriteShapeRenderer Icon","VisualTreeAsset Icon","d_VisualEffect Icon",
            "d_VisualEffectAsset Icon","VisualEffect Icon","VisualEffectAsset Icon","FixedJoint Icon",
            "FixedJoint2D Icon","Flare Icon","FlareLayer Icon","Font Icon","FrictionJoint2D Icon",
            "GameObject Icon","Grid Icon","GUILayer Icon","GUISkin Icon","GUIText Icon","GUITexture Icon",
            "Halo Icon","HingeJoint Icon","HingeJoint2D Icon","LensFlare Icon","Light Icon","LightProbeGroup Icon",
            "LightProbeProxyVolume Icon","LightProbes Icon","LineRenderer Icon","LODGroup Icon","Material Icon",
            "Mesh Icon","MeshCollider Icon","MeshFilter Icon","MeshRenderer Icon","Motion Icon","MovieTexture Icon",
            "NetworkAnimator Icon","NetworkDiscovery Icon","NetworkIdentity Icon","NetworkLobbyManager Icon",
            "NetworkLobbyPlayer Icon","NetworkManager Icon","NetworkManagerHUD Icon","NetworkMigrationManager Icon",
            "NetworkProximityChecker Icon","NetworkStartPosition Icon","NetworkTransform Icon",
            "NetworkTransformChild Icon","NetworkTransformVisualizer Icon","NetworkView Icon","OcclusionArea Icon",
            "OcclusionPortal Icon","ParticleSystem Icon","ParticleSystemForceField Icon","PhysicMaterial Icon",
            "PhysicsMaterial2D Icon","PlatformEffector2D Icon","d_PlayableDirector Icon","PlayableDirector Icon",
            "PointEffector2D Icon","PolygonCollider2D Icon","ProceduralMaterial Icon","Projector Icon",
            "RectTransform Icon","ReflectionProbe Icon","RelativeJoint2D Icon","d_SortingGroup Icon",
            "SortingGroup Icon","RenderTexture Icon","Rigidbody Icon","Rigidbody2D Icon","ScriptableObject Icon",
            "Shader Icon","ShaderVariantCollection Icon","SkinnedMeshRenderer Icon","Skybox Icon","SliderJoint2D Icon",
            "TrackedPoseDriver Icon","SphereCollider Icon","SpringJoint Icon","SpringJoint2D Icon","Sprite Icon",
            "SpriteMask Icon","SpriteRenderer Icon","StreamingController Icon","StyleSheet Icon","SurfaceEffector2D Icon",
            "TargetJoint2D Icon","Terrain Icon","TerrainCollider Icon","TerrainData Icon","TextAsset Icon",
            "TextMesh Icon","Texture Icon","Texture2D Icon","Tile Icon","Tilemap Icon","TilemapCollider2D Icon",
            "TilemapRenderer Icon","d_TimelineAsset Icon","TimelineAsset Icon","TrailRenderer Icon","Transform Icon",
            "SpriteAtlas Icon","AspectRatioFitter Icon","Button Icon","CanvasScaler Icon","ContentSizeFitter Icon",
            "d_AspectRatioFitter Icon","d_CanvasScaler Icon","d_ContentSizeFitter Icon","d_FreeformLayoutGroup Icon",
            "d_GraphicRaycaster Icon","d_GridLayoutGroup Icon","d_HorizontalLayoutGroup Icon","d_LayoutElement Icon",
            "d_PhysicalResolution Icon","d_ScrollViewArea Icon","d_SelectionList Icon","d_SelectionListItem Icon",
            "d_SelectionListTemplate Icon","d_VerticalLayoutGroup Icon","Dropdown Icon","FreeformLayoutGroup Icon",
            "GraphicRaycaster Icon","GridLayoutGroup Icon","HorizontalLayoutGroup Icon","Image Icon","InputField Icon",
            "LayoutElement Icon","Mask Icon","Outline Icon","PositionAsUV1 Icon","RawImage Icon","RectMask2D Icon",
            "Scrollbar Icon","ScrollRect Icon","Selectable Icon","Shadow Icon","Slider Icon","Text Icon","Toggle Icon",
            "ToggleGroup Icon","VerticalLayoutGroup Icon","VideoClip Icon","VideoPlayer Icon","VisualEffect Icon",
            "VisualEffectAsset Icon","WheelCollider Icon","WheelJoint2D Icon","WindZone Icon",
            "SpatialMappingCollider Icon","SpatialMappingRenderer Icon","WorldAnchor Icon","UssScript Icon",
            "UxmlScript Icon","VerticalLayoutGroup Icon","VideoEffect Icon","VisualEffect Gizmo",
            "VisualEffectAsset Icon","AnchorBehaviour Icon","AnchorInputListenerBehaviour Icon",
            "AnchorStageBehaviour Icon","CloudRecoBehaviour Icon","ContentPlacementBehaviour Icon",
            "ContentPositioningBehaviour Icon","CylinderTargetBehaviour Icon","d_AnchorBehaviour Icon",
            "d_AnchorInputListenerBehaviour Icon","d_AnchorStageBehaviour Icon","d_CloudRecoBehaviour Icon",
            "d_ContentPlacementBehaviour Icon","d_ContentPositioningBehaviour Icon","d_CylinderTargetBehaviour Icon",
            "d_ImageTargetBehaviour Icon","d_MidAirPositionerBehaviour Icon","d_ModelTargetBehaviour Icon",
            "d_MultiTargetBehaviour Icon","d_ObjectTargetBehaviour Icon","d_PlaneFinderBehaviour Icon",
            "d_UserDefinedTargetBuildingBehaviour Icon","d_VirtualButtonBehaviour Icon","d_VuforiaBehaviour Icon",
            "d_VuMarkBehaviour Icon","d_WireframeBehaviour Icon","ImageTargetBehaviour Icon",
            "MidAirPositionerBehaviour Icon","ModelTargetBehaviour Icon","MultiTargetBehaviour Icon",
            "ObjectTargetBehaviour Icon","PlaneFinderBehaviour Icon","UserDefinedTargetBuildingBehaviour Icon",
            "VirtualButtonBehaviour Icon","VuforiaBehaviour Icon","VuMarkBehaviour Icon","WireframeBehaviour Icon",
            "WindZone Gizmo","Profiler.Audio","Profiler.CPU","Profiler.FirstFrame","Profiler.GlobalIllumination",
            "Profiler.GPU","Profiler.Instrumentation","Profiler.LastFrame","Profiler.Memory","Profiler.NetworkMessages",
            "Profiler.NetworkOperations","Profiler.NextFrame","Profiler.Physics","Profiler.Physics2D",
            "Profiler.PrevFrame","Profiler.Record","Profiler.Rendering","Profiler.UI","Profiler.UIDetails",
            "Profiler.Video","ProfilerColumn.WarningCount","Project","RectTool On","RectTool","RectTransformBlueprint",
            "RectTransformRaw","redGroove","Refresh","renderdoc","rightBracket","RotateTool On","RotateTool",
            "SaveActive","SaveFromPlay","SavePassive","ScaleTool On","ScaleTool","SceneLoadIn","SceneLoadOut",
            "SceneSave","SceneSaveGrey","SceneViewAlpha","SceneViewAudio","SceneViewFx","SceneViewLighting",
            "SceneViewOrtho","SceneViewRGB","ScrollShadow","Settings","SettingsIcon","SocialNetworks.FacebookShare",
            "SocialNetworks.LinkedInShare","SocialNetworks.Tweet","SocialNetworks.UDNLogo","SocialNetworks.UDNOpen",
            "SoftlockInline","SpeedScale","StateMachineEditor.ArrowTip","StateMachineEditor.ArrowTipSelected",
            "StateMachineEditor.Background","StateMachineEditor.State","StateMachineEditor.StateHover",
            "StateMachineEditor.StateSelected","StateMachineEditor.StateSub","StateMachineEditor.StateSubHover",
            "StateMachineEditor.StateSubSelected","StateMachineEditor.UpButton","StateMachineEditor.UpButtonHover",
            "StepButton On","StepButton","StepLeftButton-On","StepLeftButton","sticky_arrow","sticky_p4","sticky_skin",
            "sv_icon_dot0_sml","sv_icon_dot10_sml","sv_icon_dot11_sml","sv_icon_dot12_sml","sv_icon_dot13_sml",
            "sv_icon_dot14_sml","sv_icon_dot15_sml","sv_icon_dot1_sml","sv_icon_dot2_sml","sv_icon_dot3_sml",
            "sv_icon_dot4_sml","sv_icon_dot5_sml","sv_icon_dot6_sml","sv_icon_dot7_sml","sv_icon_dot8_sml",
            "sv_icon_dot9_sml","sv_icon_name0","sv_icon_name1","sv_icon_name2","sv_icon_name3","sv_icon_name4",
            "sv_icon_name5","sv_icon_name6","sv_icon_name7","sv_icon_none","sv_label_0","sv_label_1","sv_label_2",
            "sv_label_3","sv_label_4","sv_label_5","sv_label_6","sv_label_7","SVN_AddedLocal","SVN_Conflicted",
            "SVN_DeletedLocal","SVN_Local","SVN_LockedLocal","SVN_OutOfSync","tab_next","tab_next@2x","tab_prev",
            "tab_prev@2x","TerrainInspector.TerrainToolLower On","TerrainInspector.TerrainToolLower",
            "TerrainInspector.TerrainToolLowerAlt","TerrainInspector.TerrainToolPlants On",
            "TerrainInspector.TerrainToolPlants","TerrainInspector.TerrainToolPlantsAlt On",
            "TerrainInspector.TerrainToolPlantsAlt","TerrainInspector.TerrainToolRaise On",
            "TerrainInspector.TerrainToolRaise","TerrainInspector.TerrainToolSculpt On",
            "TerrainInspector.TerrainToolSculpt","TerrainInspector.TerrainToolSetheight On",
            "TerrainInspector.TerrainToolSetheight","TerrainInspector.TerrainToolSetheightAlt On",
            "TerrainInspector.TerrainToolSetheightAlt","TerrainInspector.TerrainToolSettings On",
            "TerrainInspector.TerrainToolSettings","TerrainInspector.TerrainToolSmoothHeight On",
            "TerrainInspector.TerrainToolSmoothHeight","TerrainInspector.TerrainToolSplat On",
            "TerrainInspector.TerrainToolSplat","TerrainInspector.TerrainToolSplatAlt On",
            "TerrainInspector.TerrainToolSplatAlt","TerrainInspector.TerrainToolTrees On",
            "TerrainInspector.TerrainToolTrees","TerrainInspector.TerrainToolTreesAlt On",
            "TerrainInspector.TerrainToolTreesAlt","TestFailed","TestIgnored","TestInconclusive","TestNormal",
            "TestPassed","TestStopwatch","TimelineClipBG","TimelineClipFG","TimelineDigIn","TimelineEditModeMixOFF",
            "TimelineEditModeMixON","TimelineEditModeReplaceOFF","TimelineEditModeReplaceON","TimelineEditModeRippleOFF",
            "TimelineEditModeRippleON","TimelineSelector","Toolbar Minus","Toolbar Plus More","Toolbar Plus",
            "ToolHandleCenter","ToolHandleGlobal","ToolHandleLocal","ToolHandlePivot","tranp","TransformTool On",
            "TransformTool","tree_icon","tree_icon_branch","tree_icon_branch_frond","tree_icon_frond","tree_icon_leaf",
            "TreeEditor.AddBranches","TreeEditor.AddLeaves","TreeEditor.Branch On","TreeEditor.Branch",
            "TreeEditor.BranchFreeHand On","TreeEditor.BranchFreeHand","TreeEditor.BranchRotate On",
            "TreeEditor.BranchRotate","TreeEditor.BranchScale On","TreeEditor.BranchScale",
            "TreeEditor.BranchTranslate On","TreeEditor.BranchTranslate","TreeEditor.Distribution On",
            "TreeEditor.Distribution","TreeEditor.Duplicate","TreeEditor.Geometry On","TreeEditor.Geometry",
            "TreeEditor.Leaf On","TreeEditor.Leaf","TreeEditor.LeafFreeHand On","TreeEditor.LeafFreeHand",
            "TreeEditor.LeafRotate On","TreeEditor.LeafRotate","TreeEditor.LeafScale On","TreeEditor.LeafScale",
            "TreeEditor.LeafTranslate On","TreeEditor.LeafTranslate","TreeEditor.Material On","TreeEditor.Material",
            "TreeEditor.Refresh","TreeEditor.Trash","TreeEditor.Wind On","TreeEditor.Wind","UnityEditor.AnimationWindow",
            "UnityEditor.ConsoleWindow","UnityEditor.DebugInspectorWindow","UnityEditor.FindDependencies",
            "UnityEditor.GameView","UnityEditor.Graphs.AnimatorControllerTool","UnityEditor.HierarchyWindow",
            "UnityEditor.InspectorWindow","UnityEditor.LookDevView","UnityEditor.ProfilerWindow",
            "UnityEditor.SceneHierarchyWindow","UnityEditor.SceneView","UnityEditor.Timeline.TimelineWindow",
            "UnityEditor.VersionControl","UnityLogo","UnityLogoLarge","UpArrow","vcs_add","vcs_branch","vcs_change",
            "vcs_check","vcs_delete","vcs_document","vcs_edit","vcs_incoming","vcs_integrate","vcs_local","vcs_lock",
            "vcs_refresh","vcs_sync","vcs_unresolved","vcs_update","VerticalSplit","ViewToolMove On","ViewToolMove",
            "ViewToolOrbit On","ViewToolOrbit","ViewToolZoom On","ViewToolZoom","VisibilityOff","VisibilityOn",
            "VisualEffect Gizmo","VUMeterTextureHorizontal","VUMeterTextureVertical","WaitSpin00","WaitSpin01",
            "WaitSpin02","WaitSpin03","WaitSpin04","WaitSpin05","WaitSpin06","WaitSpin07","WaitSpin08","WaitSpin09",
            "WaitSpin10","WaitSpin11","WelcomeScreen.AssetStoreLogo","winbtn_graph","winbtn_graph_close_h",
            "winbtn_graph_max_h","winbtn_graph_min_h","winbtn_mac_close","winbtn_mac_close_a","winbtn_mac_close_h",
            "winbtn_mac_inact","winbtn_mac_max","winbtn_mac_max_a","winbtn_mac_max_h","winbtn_mac_min",
            "winbtn_mac_min_a","winbtn_mac_min_h","winbtn_win_close","winbtn_win_close_a","winbtn_win_close_h",
            "winbtn_win_max","winbtn_win_max_a","winbtn_win_max_h","winbtn_win_min","winbtn_win_min_a",
            "winbtn_win_min_h","winbtn_win_rest","winbtn_win_rest_a","winbtn_win_rest_h",
            "AvatarInspector/RightFingersIk","AvatarInspector/LeftFingersIk","AvatarInspector/RightFeetIk",
            "AvatarInspector/LeftFeetIk","AvatarInspector/RightFingers","AvatarInspector/LeftFingers",
            "AvatarInspector/RightArm","AvatarInspector/LeftArm","AvatarInspector/RightLeg","AvatarInspector/LeftLeg",
            "AvatarInspector/Head","AvatarInspector/Torso","AvatarInspector/MaskEditor_Root",
            "AvatarInspector/BodyPartPicker","AvatarInspector/BodySIlhouette","boo Script Icon","js Script Icon",
            "EyeDropper.Large","AboutWindow.MainHeader","AgeiaLogo","MonoLogo","PlayButtonProfile Anim",
            "StepButton Anim","PauseButton Anim","PlayButton Anim","MoveTool On","Icon Dropdown",
            "AvatarInspector/DotSelection","AvatarInspector/DotFrameDotted","AvatarInspector/DotFrame",
            "AvatarInspector/DotFill","AvatarInspector/RightHandZoom","AvatarInspector/LeftHandZoom",
            "AvatarInspector/HeadZoom","AvatarInspector/RightLeg","AvatarInspector/LeftLeg",
            "AvatarInspector/RightFingers","AvatarInspector/RightArm","AvatarInspector/LeftFingers",
            "AvatarInspector/LeftArm","AvatarInspector/Head","AvatarInspector/Torso",
            "AvatarInspector/RightHandZoomSilhouette","AvatarInspector/LeftHandZoomSilhouette",
            "AvatarInspector/HeadZoomSilhouette","AvatarInspector/BodySilhouette","lightMeter/redLight",
            "lightMeter/orangeLight","lightMeter/lightRim","lightMeter/greenLight","SceneviewAudio",
            "SceneviewLighting","TerrainInspector.TerrainToolSetHeight","AS Badge New","AS Badge Move",
            "AS Badge Delete","WelcomeScreen.UnityAnswersLogo","WelcomeScreen.UnityForumLogo",
            "WelcomeScreen.UnityBasicsLogo","WelcomeScreen.VideoTutLogo","WelcomeScreen.MainHeader","Icon Dropdown",
            "PrefabNormal Icon","PrefabNormal Icon","BuildSettings.BlackBerry.Small","BuildSettings.Tizen.Small",
            "BuildSettings.XBox360.Small","BuildSettings.PS3.Small","BuildSettings.SamsungTV.Small",
            "BuildSettings.BlackBerry","BuildSettings.Tizen","BuildSettings.XBox360","BuildSettings.PS3",
            "BuildSettings.SamsungTV"
        };
    }
}
