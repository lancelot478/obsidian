#Unity 

### AssetModificationProcessor
AssetModificationProcessor lets you hook into saving of serialized assets and scenes which are edited inside Unity.
```Csharp
    public class UIBindingPrefabSaveHelper : UnityEditor.AssetModificationProcessor  
    {  
        static UIBindingPrefabSaveHelper()  
        {  
#if UNITY_2019_1_OR_NEWER  
            PrefabStage.prefabSaving += OnPrefabStageSaving;  
#endif  
        }

```
### Check t is in this prefab
```Csharp
private bool IsInCurrentPrefab(Transform t)  
{  
    do  
    {  
        if (t == transform)  
            return true;  
        t = t.parent;  
    } while (t != null);  
    return false;  
}
```

```Csharp
public class Example
{
    [MenuItem("Examples/Double Scale")]
    static void DoubleScale()
    {
        GameObject gameObject = Selection.activeGameObject;
        Undo.RecordObject(gameObject.transform, "Double scale");
        gameObject.transform.localScale *= 2;

        // Notice that if the call to RecordPrefabInstancePropertyModifications is not present,
        // all changes to scale will be lost when saving the Scene, and reopening the Scene
        // would revert the scale back to its previous value.
	        PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject.transform);
        // Optional step in order to save the Scene changes permanently.
        //EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    // Disable the menu item if there is no Hierarchy GameObject selection.
    [MenuItem("Examples/Double Scale", true)]
    static bool ValidateDoubleScale()
    {
        return Selection.activeGameObject != null && !EditorUtility.IsPersistent(Selection.activeGameObject);
    }

```Csharp
List<UIViewController> list = ListPool<UIViewController>.Get();  
while (openedViews.Count > 0)  
{  
    var view = openedViews.Pop();  
    if (view != closedUI)  
    {  
        list.Add(view);  
    }  
    else  
    {  
        break;  
    }  
}  
for (int i = list.Count - 1; i >= 0; i--)  
{  
    openedViews.Push(list[i]);  
}
```

```Csharp
var layers = Enum.GetValues(typeof(UILayer));
viewType = GetType($"{typeof(UIConfig).Namespace}.{config.uiType}");
Editor CopyBoard : GUIUtility.systemCopyBuffer
Editor里不能调用 PrefabUtility.SaveAsPrefabAsset 应该使用EditorUtility.SetDirty(gameObject);
```

### 