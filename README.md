# Saints Hierarchy #

Unity Hierarchy enhancement (WIP). Use `Alt`+`Left Mouse Button` to select.

![](https://github.com/user-attachments/assets/35037962-3488-426a-98ca-25c9dae696a0)

## Features ##

1.  Draw indent guild
2.  Allow set icons (pre-set & custom)
3.  Auto set icons for camera, cavans, light, eventSystem & Wwise Initializer
4.  Prefab footer icon if you have custom icon set

## Usage ##

### Color ###

1.  select a color to change
2.  use `x` button to clean the color config
3.  use the color picker (second button) to manually change the color you want

![](https://github.com/user-attachments/assets/c1d3ebdf-0ff8-419a-afdb-ecda45895eb2)

### Icon ###

1.  select an icon to change
2.  to use your custom icon, first right click on you icon - copy path, then paste it into the search field. The icon will appear as the first item on the result
3.  select the same icon to remove icon config

### Scripted Icon ###

```csharp
public class HierarchyIconPathExample : MonoBehaviour, IHierarchyIconPath
{
    public string HierarchyIconPath => "CollabChanges Icon";  // return a path or name of the icon
}
```

Or

```csharp
public class HierarchyIconTexture2DExample: MonoBehaviour, IHierarchyIconTexture2D
{
    public Texture2D texture2D;
#if UNITY_EDITOR
    public Texture2D HierarchyIconTexture2D => texture2D;  // return a texture2D object; null to use default behavor
#endif
}
```

![](https://github.com/user-attachments/assets/e62907fe-818e-4666-9b68-e724f2fbb387)

### Scripted Draw ###

Draw on left example:

```csharp
public class LeftDrawExample2 : MonoBehaviour, IHierarchyLeftDraw
{
    public string c;
#if UNITY_EDITOR
    public HierarchyUsed HierarchyLeftDraw(HierarchyArea hierarchyArea)
    {
        GUIContent content = new GUIContent(c);
        float width = new GUIStyle("button").CalcSize(content).x;
        Rect useRect = hierarchyArea.MakeXWidthRect(hierarchyArea.SpaceStartX, width);

        if (GUI.Button(useRect, content))
        {
            Debug.Log($"click {c}");
        }

        return new HierarchyUsed(useRect);
    }
#endif
}
```

On right:

```csharp
public class RightDrawExample2 : MonoBehaviour, IHierarchyDraw
{
    public Texture2D icon;
    public HierarchyUsed HierarchyDraw(HierarchyArea hierarchyArea)
    {
        if (icon == null)
        {
            return new HierarchyUsed(hierarchyArea.MakeXWidthRect(hierarchyArea.SpaceEndX, 0));
        }

        float width = icon.width;

        Rect useRect = hierarchyArea.MakeXWidthRect(hierarchyArea.SpaceEndX, -width);

        GUI.DrawTexture(useRect, icon, ScaleMode.ScaleToFit, true);

        return new HierarchyUsed(useRect);
    }
}
```

![](https://github.com/user-attachments/assets/07720af8-9c61-4332-84fd-b2cfce2f8799)

## TODO ##

- [x] Background Color
- [ ] Background Strip Color
- [x] Component Icon Support
- [x] Script controll of what to draw
