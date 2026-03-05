using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CommonButtonAnim
{
    [MenuItem("Assets/Anim/Instance")]
    [MenuItem("GameObject/Anim/Instance")]
    public static void ExecutePrefab()
    {
        var objects = Selection.objects;
        if (objects == null)
        {
            return;
        }

        foreach (var obj in objects)
        {
            Change(obj as GameObject);
            if (obj != null)
            {
                EditorUtility.SetDirty(obj);
            }
        }
    }

    private static void Change(GameObject root)
    {
        if (!root)
        {
            return;
        }

        var button = GetOrAddComponent<Button>(root);
        button.transition = Selectable.Transition.Animation;

        var animator = GetOrAddComponent<Animator>(root);
        animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Art/Effect/UI/Common/Animation/UIM_Common_Btn.controller");

        GetOrAddComponent<CanvasGroup>(root);
    }

    private static T GetOrAddComponent<T>(GameObject root) where T : Component
    {
        if (!root)
        {
            return default;
        }

        var comp = root.GetComponent<T>();
        if (comp == null)
        {
            comp = root.AddComponent<T>();
        }

        return comp;
    }
}