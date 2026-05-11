## 1.4.1 ##

Fix: Search input for icon will close the popup

## 1.4.0 ##

1.  Add: Alt + left click a component icon to open a resizable component properties popup near the hierarchy item
2.  Add: Configs for showing component icons for general scripts and Transform
3.  Add: Component properties popup can toggle `Behaviour.enabled`
4.  Add: Disabled Behaviour component icons are dimmed

## 1.3.2 ##

1.  Fix: `sv` color didn't paint an underline color
2.  Add: Favorite GameObjects now can paint the correct underline by config

## 1.3.1 ##

1.  Fix: Scene selector was very slow if you use addressable
2.  Fix: Accidently drag the scene if you try drag the mouse while scene selector is already open   

## 1.3.0 ##

Add: Now you can click the scene name to switch scene in your project

## 1.2.3 ##

1.  Add: Favorite GameObjects can now read hierarchy config and async the appearance (icon, color) 
2.  Add: You can now override icon and color for Favorite GameObjects
3.  Add: Favorite GameObjects now can read the default icon config and hide the icon

## 1.2.2 ##

1.  Add: You can now change alias and icon for a favorite gameObject
2.  Improve: Better dragging detection for favorite gameObject

## 1.2.0 ##

Add: You can now drag & drop GameObjects to Favorite area at the top of the hierarchy.

Note: 
1.  ATM the config is saved under personal config (so won't be saved by git), and can not be saved to team-shared config
2.  ATM it can not show custom icons etc

## 1.1.7 ##

Add: Config for "GameObject Enabled Checker Every Row" [#1](https://github.com/TylerTemp/SaintsHierarchy/issues/1)

## 1.1.6 ##

1.  Fix: new added gameObject under a prefab instance now shows a `+` icon like Unity's default behavoir
2.  Add: `Transparent Default Icon` config so label can be aligned
3.  Change: Move menu to `Tools/SaintsHierarchy`

## 1.1.5 ##

Add "No Default Icon" config to remove the default white box icon for gameObject.

## 1.1.2 ##

Disabled item color fix

## 1.1.1 ##

1.  Fix: Unable to enable "Component Icons"
2.  Fix: "Component Icons" no longer overflow to label if there are many
3.  Fix: drawn content will try to not overlap (can not grarrenteed because of the limit of IMGUI)
4.  Fix: Custom icon not working
5.  Add: Search Icon now list all possible results from [UnityEditorIcons](https://github.com/nukadelic/UnityEditorIcons)

## 1.1.0 ##

1.  Fix: Error if the target component is missing for component icons
2.  Add: Allow setup a personal config which does not get mixed with team config. Saved under `Library/PersonalHierarchyConfig.asset`
3.  Change: GameObject Enabled Checker now only displays when any parent gameObjects is disabled

## 1.0.0 ##

1.  Better background color to not conflict with default one
2.  Component icons supported
3.  Background strip supported
4.  Config tweaks supported
5.  GameObject self enabled checker supported
6.  GameObject icon supported

## 0.0.8 ##

1.  Use decoration instead of interface for easier label, button, and custom drawing
2.  Add `Window`-`Saints`-`Disable Saints Hierarchy` to enable/disable this plugin

## 0.0.7 ##

Fix selection color when drag element

## 0.0.6 ##

1.  Fix children of `prefab` label is not blue
2.  Fix prefab editor always displayed as disabled gameObject

## 0.0.5 ##

1.  Component Icon Support
2.  Script controll of what to draw
3.  Fix an error when a path is not found inside a prefab

## 0.0.4 ##

Fix disabled object

## 0.0.3 ##

Fix error when importing fbx type

## 0.0.2 ##

1.  Add background color custom support
2.  Improve preformance
3.  Indent color now changes with background color
