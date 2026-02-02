# Saints Hierarchy #

Unity Hierarchy enhancement. Use `Alt`+`Left Mouse Button` to select.

![](https://github.com/user-attachments/assets/35037962-3488-426a-98ca-25c9dae696a0)

## Features ##

1.  Draw indent guild
2.  Allow set icons (pre-set & custom)
3.  Auto set icons for camera, cavans, light, eventSystem & Wwise Initializer
4.  Prefab footer icon if you have custom icon set


## Config ##

This will automaticlly add indent tree, and icon for camera, light, canvas, event system, wwise

`Window` - `Saints` - `Hierarchy` - `Disable Saints Hierarchy` to disable this plugin

![](https://github.com/user-attachments/assets/f9b3f079-0a7e-46c6-91b2-2ef83f9feff2)

### Background Strip ###

`Window` - `Saints` - `Hierarchy` - `Background Strip`

![](https://github.com/user-attachments/assets/a4c4cf02-7bc0-4161-9592-99a0f7d3bc44)

### Component Icons ###

`Window` - `Saints` - `Hierarchy` - `Component Icons`

You can set the script icon and show the icons at the end of row

Setup:

![](https://github.com/user-attachments/assets/81ab960d-1abd-425f-858c-b79344284088)

Result:

![](https://github.com/user-attachments/assets/423da9d8-1282-4d53-8ba9-d4bb4a83bdc6)

### GameObject Enabled Checker ###

`Window` - `Saints` - `Hierarchy` - `GameObject Enabled Checker`

Add a checkbox at the end for gameObject which has any disabled parent gameObjects, to quickly toggle it back.

![](https://github.com/user-attachments/assets/22cb4180-4aa6-4dcc-8cfc-6df0ee822e90)

## Usage ##

### GameObject Icon ###

GameObject icon (including custom icons) will be used as hierarchy icon:

![](https://github.com/user-attachments/assets/08563fd4-afd5-4185-ac07-9144db4b21d2)

Result:

![](https://github.com/user-attachments/assets/2753aa05-c782-41e2-a084-caafb8f51ad5)

GameObject label will be used as hierarchy label underline:

![](https://github.com/user-attachments/assets/80643e95-1b5c-48a5-a261-ed30a0646017)

Result:

![](https://github.com/user-attachments/assets/0c30c68e-6d50-4829-a10a-628b6dc85ea6)

### Color ###

1.  select a color to change
2.  use `x` button to clean the color config
3.  use the color picker (second button) to manually change the color you want

![](https://github.com/user-attachments/assets/156b467c-b534-412f-9a79-cde1dca61ec2)

### Custom Icon ###

1.  select an icon to change
2.  to use your custom icon, first right click on you icon - copy path, then paste it into the search field. The icon will appear as the first item on the result
3.  select the same icon to remove icon config

![](https://github.com/user-attachments/assets/25a0f472-8496-4087-9e0c-b75ef87be67b)

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

### `HierarchyLabel`/`HierarchyLeftLabel` ###

Draw label. Callback & tag supported.

Parameters:

*   `string label = null`: label to draw. Use `"$" + name` to make a callback/property
*   `string tooltip = null`: tooltip to show

```csharp
using SaintsHierarchy;

[HierarchyLabel("<color=CadetBlue><field/>")]
[HierarchyLeftLabel("<color=CadetBlue>|LEFT|")]
public string content;
```
![](https://github.com/user-attachments/assets/375905ab-67b1-4e2f-a793-afd35a5f5087)

### `HierarchyButton`/`HierarchyLeftButton`/`HierarchyGhostButton`/`HierarchyGhostLeftButton` ###

Draw button. Callback & tag supported.

Parameters:

*   `string label = null`: label of the button. `null` to use function name. use `"$" + name` to use a callback label
*   `string tooltip = null`: tooltip to show

```csharp
using SaintsHierarchy;

public string c;

[HierarchyGhostButton("$" + nameof(c), "Click Me!")]  // dynamic label
private void OnBtnClick()
{
    Debug.Log($"click {c}");
}

[HierarchyLeftButton("C <color=Burlywood>Left")]
private void LeftClick()
{
    Debug.Log("Left Click");
}
```

![](https://github.com/user-attachments/assets/78728c9f-e0f3-4e6d-afa5-7d167c97d7ed)

### `HierarchyDraw`/`HierarchyLeftDraw` ###

Manually draw content

Parameters:

*   `string groupBy = null`: group the items virtically by this name. If `null`, it will not share space with anyone.

Signature:

The method must have this signaure:

```csharp
HierarchyUsed FuncName(HierarchyArea hierarchyArea)
```

`HierarchyArea` has the following fields:

```csharp
/// <summary>
/// Rect.y for drawing
/// </summary>
public readonly float Y;
/// <summary>
/// Rect.height for drawing
/// </summary>
public readonly float Height;
/// <summary>
/// the x value where the title (gameObject name) started
/// </summary>
public readonly float TitleStartX;
/// <summary>
/// the x value where the title (gameObject name) ended
/// </summary>
public readonly float TitleEndX;
/// <summary>
/// the x value where the empty space start. You may want to start draw here
/// </summary>
public readonly float SpaceStartX;
/// <summary>
/// the x value where the empty space ends. When drawing from right, you need to backward drawing starts here
/// </summary>
public readonly float SpaceEndX;

/// <summary>
/// The x drawing position. It's recommend to use this as your start drawing point, as SaintsHierarchy will
/// change this value accordingly everytime an item is drawn.
/// </summary>
public readonly float GroupStartX;
/// <summary>
/// When using `GroupBy`, you can see the vertical rect which already used by others in this group
/// </summary>
public readonly IReadOnlyList<Rect> GroupUsedRect;

public float TitleWidth => TitleEndX - TitleStartX;
public float SpaceWidth => SpaceEndX - SpaceStartX;

/// <summary>
/// A quick way to make a rect
/// </summary>
/// <param name="x">where to start</param>
/// <param name="width">width of the rect</param>
/// <returns>rect space you want to draw</returns>
public Rect MakeXWidthRect(float x, float width)
{
    if(width >= 0)
    {
        return new Rect(x, Y, width, Height);
    }
    return new Rect(x + width, Y, -width, Height);
}
```

After you draw your item, use `return new HierarchyUsed(useRect);` to tell the space you've used. Use `return default` if you don't draw this time.

```csharp
public bool play;
[Range(0f, 1f)] public float range1;
[Range(0f, 1f)] public float range2;

private string ButtonLabel => play ? "Pause" : "Play";

#if UNITY_EDITOR
[HierarchyLeftButton("$" + nameof(ButtonLabel))]
private IEnumerator LeftBtn()
{
    play = !play;
    // ReSharper disable once InvertIf
    if (play)
    {
        while (play)
        {
            range1 = (range1 + 0.0005f) % 1;
            range2 = (range2 + 0.0009f) % 1;
            EditorApplication.RepaintHierarchyWindow();
            yield return null;
        }
    }
}

[HierarchyDraw("my progress bar")]
private HierarchyUsed DrawRight1G1(HierarchyArea headerArea)
{
    Rect useRect = new Rect(headerArea.MakeXWidthRect(headerArea.GroupStartX - 40, 40))
    {
        height = headerArea.Height / 2,
    };
    Rect progressRect = new Rect(useRect)
    {
        width = range1 * useRect.width,
    };

    EditorGUI.DrawRect(useRect, Color.gray);
    EditorGUI.DrawRect(progressRect, Color.red);

    return new HierarchyUsed(useRect);
}
[HierarchyDraw("my progress bar")]
private HierarchyUsed DrawRight1G2(HierarchyArea headerArea)
{
    Rect useRect = new Rect(headerArea.MakeXWidthRect(headerArea.GroupStartX - 40, 40))
    {
        y = headerArea.Y + headerArea.Height / 2,
        height = headerArea.Height / 2,
    };
    Rect progressRect = new Rect(useRect)
    {
        width = range2 * useRect.width,
    };

    EditorGUI.DrawRect(useRect, Color.gray);
    EditorGUI.DrawRect(progressRect, Color.yellow);

    return new HierarchyUsed(useRect);
}
#endif
```

[![](https://github.com/user-attachments/assets/bd5db3ef-da03-4455-b665-1dc661901b15)](https://github.com/user-attachments/assets/260c9661-e7c2-4e0f-b666-a36287fe9eb4)
