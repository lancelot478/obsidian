using System.IO;
using System.Linq;
using SAGA.Editor;
using UnityEditor;
using UnityEngine;

public partial class GenerateGuildAnimation
{
    private string[] AnimName = new string[]
    {
        "Lean",
        "Patrol",
        "StandChat_A", "StandChat_B",
        "StandChat_1_A", "StandChat_1_B",
        "StandIdle", "StandWatch",
        "SitIdle", "SitRead",
        "SitWatch1", "SitWatch2",
        "SitWatch1", "SitWatch2",
        "SitChat_1_L", "SitChat_1_R", "SitChat_2_L", "SitChat_2_R", "SitChat_3_L", "SitChat_3_R",
    };

    private const string SourceModelDir = "Assets/Models/ModelRole";
    private const string TargetModelDir = "Assets/Models/Guild/Player/ModelRoleAnim";
    private const string TargetSexualModelHairDir = "Assets/Models/Guild/Player/ModelHair";
    private const string TargetSexualModelHeadDir = "Assets/Models/Guild/Player/ModelHead";
    private const string TargetSexualModelDir = "Assets/Models/Guild/Player/ModelRole";
    private const string ControllerBundleName = "guild/anim";

    private void GenerateController(string path)
    {
        tModelEditor.LoadAsset();
        var parentName = Path.GetFileName(path);
        var sexualDirectories = Directory.GetDirectories(path, "*.*", SearchOption.TopDirectoryOnly);
        foreach (var sexualDirectory in sexualDirectories)
        {
            var animController = new AnimatorOverrideController
            {
                runtimeAnimatorController = tModelEditor.baseAnim
            };

            var animClipPathList = Directory.GetFiles(sexualDirectory, "*.anim");
            foreach (var animClipPath in animClipPathList)
            {
                var animClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animClipPath);
                var clipName = Path.GetFileNameWithoutExtension(animClipPath);
                animController[clipName] = animClip;
            }

            var dirName = Path.GetRelativePath(path, sexualDirectory);
            var controllerPath =
                Path.ChangeExtension(Path.Combine(sexualDirectory, $"{parentName}_{dirName}"), ".controller");
            var homeBuildPath=
                Path.ChangeExtension(Path.Combine(sexualDirectory, $"{parentName}_{dirName}_HomeBuild"), ".controller");
            AssetDatabase.CreateAsset(animController, controllerPath);
            AssetDatabase.CreateAsset(animController, homeBuildPath);
        }

        AssetDatabase.Refresh();

        ShowNotification(new GUIContent("生成控制器完成！"));
    }

    private void OnClickGenerateSexualController()
    {
        GenerateController(TargetSexualModelHairDir);
        GenerateController(TargetSexualModelHeadDir);
        GenerateController(TargetSexualModelDir);
    }

    private void OnClickGenerateController()
    {
        tModelEditor.LoadAsset();
        var careerDirectories = Directory.GetDirectories(TargetModelDir, "*.*", SearchOption.TopDirectoryOnly);
        foreach (var careerDirectory in careerDirectories)
        {
            var animController = new AnimatorOverrideController
            {
                runtimeAnimatorController = tModelEditor.baseAnim
            };

            var animClipPathList = Directory.GetFiles(careerDirectory, "*.anim");
            foreach (var animClipPath in animClipPathList)
            {
                var animClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animClipPath);
                var clipName = Path.GetFileNameWithoutExtension(animClipPath);
                animController[clipName] = animClip;
            }

            var dirName = Path.GetRelativePath(TargetModelDir, careerDirectory);
            var controllerPath = Path.ChangeExtension(Path.Combine(careerDirectory, dirName), ".controller");
            AssetDatabase.CreateAsset(animController, controllerPath);
        }

        AssetDatabase.Refresh();

        ShowNotification(new GUIContent("生成控制器完成！"));
    }

    private void OnClickMoveClip()
    {
        var careerDirectories = Directory.GetDirectories(SourceModelDir, "*.*", SearchOption.TopDirectoryOnly);
        foreach (var careerDirectory in careerDirectories)
        {
            var animClips = Directory.GetFiles(careerDirectory, "*.anim");
            foreach (var animClip in animClips)
            {
                var relativePath = Path.GetRelativePath(SourceModelDir, animClip);
                if (!AnimName.Any(item => relativePath.Contains(item)))
                {
                    continue;
                }

                var targetPath = Path.Combine(TargetModelDir, relativePath);
                var targetDir = Path.GetDirectoryName(targetPath);


                if (!Directory.Exists(targetDir))
                {
                    if (targetDir != null) Directory.CreateDirectory(targetDir);
                }

                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }

                FileUtil.CopyFileOrDirectory(animClip, targetPath);
            }
        }

        AssetDatabase.Refresh();

        ShowNotification(new GUIContent("公会动画片段移动完成！"));
    }

    private void OnClickRemoveOld()
    {
        var careerDirectories = Directory.GetDirectories(SourceModelDir, "*.*", SearchOption.TopDirectoryOnly);
        foreach (var careerDirectory in careerDirectories)
        {
            var animClips = Directory.GetFiles(careerDirectory, "*.anim");
            foreach (var animClip in animClips)
            {
                var relativePath = Path.GetRelativePath(SourceModelDir, animClip);
                if (!AnimName.Any(item => relativePath.Contains(item)))
                {
                    continue;
                }

                if (File.Exists(animClip))
                {
                    File.Delete(animClip);
                }
            }
        }

        AssetDatabase.Refresh();

        ShowNotification(new GUIContent("老的公会动画片段删除完成！"));
    }

    private void OnClickMarkController()
    {
        var allController = Directory.GetFiles(TargetModelDir, "*.controller", SearchOption.AllDirectories);
        foreach (var controller in allController)
        {
            MarkAssetBundleNames.SetPathAssetBundleName(controller, ControllerBundleName);
        }

        AssetDatabase.Refresh();

        ShowNotification(new GUIContent("公会动画标记完成！"));
    }
}