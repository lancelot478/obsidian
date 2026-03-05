using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;


public partial class GenerateGuildAnimation : EditorWindow
{
    private const string UxmlPath = "Assets/Editor/GenerateGuildAnimation/GenerateGuildAnimation.uxml";
    private const string USSPath = "Assets/Editor/GenerateGuildAnimation/GenerateGuildAnimation.uss";

    [MenuItem("Tools/公会人物动画")]
    public static void Start()
    {
        var wnd = GetWindow<GenerateGuildAnimation>();
        wnd.titleContent = new GUIContent("GenerateGuildAnimation");
    }

    public void CreateGUI()
    {
        var root = rootVisualElement;

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        visualTree.CloneTree(root);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(USSPath);
        root.styleSheets.Add(styleSheet);

        root.Q<Button>("generate_controller_button").clicked += OnClickGenerateController;
        root.Q<Button>("generate_sexual_controller_button").clicked += OnClickGenerateSexualController;

        root.Q<Button>("move_clip_button").clicked += OnClickMoveClip;

        root.Q<Button>("delete_old_anim_button").clicked += OnClickRemoveOld;

        // root.Q<Button>("mark_controller_button").clicked += OnClickMarkController;
        root.Q<Button>("mark_controller_button").SetEnabled(false);
    }
}