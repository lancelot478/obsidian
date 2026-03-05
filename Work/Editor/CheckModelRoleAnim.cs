using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CheckModelRoleAnim : Editor
{
   /// <summary>
   /// 需要核对loop的动画列表
   /// </summary>
    public static Dictionary<string, int> m_loopAnimNameList = new Dictionary<string, int>()
    {
         {"wait", 1},
         {"walk", 1},
         {"attack_sprint", 1},
         {"attack_wait", 1},
         {"attack_wait2", 1},
         {"collectionlumber", 1},
         {"collectionmine", 1},
         {"collectionplant", 1},
         {"stun", 1},
         {"wait_withEye", 1},
         {"xuanjue_1", 1},
         {"xuanjue_2", 1},
         {"ready_2", 1},
         {"use_skill10_pose2", 1},
         {"use_assault1_2", 1},
         {"use_skill1_2", 1},
         {"use_skill1_5", 1},
         {"use_skill2_2", 1},
         {"use_skill2_5", 1},
         {"use_skill3_2", 1},
         {"use_skill3_5", 1},
         {"use_skill4_2", 1},
         {"use_skill4_5", 1},
         {"use_skill5_2", 1},
         {"use_skill5_5", 1},
         {"use_skill6_2", 1},
         {"use_skill6_5", 1},
         {"use_skill7_2", 1},
         {"use_skill7_5", 1},
         {"use_skill8_2", 1},
         {"use_skill8_5", 1},
         {"use_skill9_2", 1},
         {"use_skill9_5", 1},
         {"use_skill10_2", 1},
         {"use_skill10_5", 1},
         {"use_skill11_2", 1},
         {"use_skill11_5", 1},
         {"use_skill12_2", 1},
         {"use_skill12_5", 1},
         {"use_skill20_2", 1},
         {"use_skill20_5", 1},
         {"use_skill21_2", 1},
         {"use_skill21_5", 1},
    };
    /// <summary>
    /// 角色文件夹
    /// </summary>
    public static Dictionary<string, string> m_roleDirList = new Dictionary<string, string>()
    {
         {"Novice_F", "_Novice"},
         {"Novice_M", "_Novice"},
         {"Swordman_F", "_Sword"},
         {"Swordman_M", "_Sword"},
         {"Ranger_F", "_Bow"},
         {"Ranger_M", "_Bow"},
         {"Magician_F", "_Ball"},
         {"Magician_M", "_Ball"},
         {"Assassin_F", "_Katar"},
         {"Assassin_M", "_Katar"},
         {"Reverend_F", "_Staff"},
         {"Reverend_M", "_Staff"},
    };
    /// <summary>
    /// 角色文件夹无后缀列表
    /// </summary>
    public static Dictionary<string, int> m_roleNotSuffixList = new Dictionary<string, int>()
    {
         {"wait", 1},
         {"BattleShow", 1},
         {"BattleShow1", 1},
         {"BattleShow2", 1},
         {"BattleShow3", 1},
         {"collectionshow", 1},
         {"collectionlumber", 1},
         {"collectionmine", 1},
         {"collectionplant", 1},
         {"weaponswitch1", 1},
         {"weaponswitch2", 1},
         {"xuanjue_1", 1},
         {"xuanjue_2", 1},
         {"empty", 1},
         {"wait_withEye", 1},
         {"hit", 1},
         {"repel", 1},
    };

    public static string GetFileAssetPath(string namePath)
    {
        string path = namePath.Replace("\\", "/");
        path = path.Replace(Application.dataPath, "Assets");
        return path;
    }
    [MenuItem("Assets/CheckRoleModel/1核对角色动画")]  
    static void CheckRoleModelAnimFormat()
    {
        Object[] folderObj = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
        foreach (Object folder in folderObj)
        {
            string assetPath = AssetDatabase.GetAssetPath(folder) + "/";
            string[] folderNameArr = folder.name.Split('@');
            string folderPath = assetPath;
            for (int i = 0; i < folderNameArr.Length; i++)
            {
                string path = assetPath.Replace(folder.name, folderNameArr[i]);
                CheckFormat(path);
            }
        }
    }

    static string GetAnimNmae(AnimationClip clip, string directoryName)
    {
        string animName = clip.name;
        m_roleDirList.TryGetValue(directoryName, out string suffix);
        bool isRole = !string.IsNullOrEmpty(suffix);
        if (isRole)
        {
            // todo 是否多此一举
            m_roleNotSuffixList.TryGetValue(animName, out int result);
            if (result > 0)
            {
                return animName;
            }
            animName = animName.Replace(suffix, "");
        }
        return animName;
    }

    static void CheckFormat(string assetPath)
    {
        Dictionary<string, AnimationClip> animDic = new Dictionary<string, AnimationClip>() { };

        string fullPath = Application.dataPath.Replace("Assets", string.Empty) + assetPath;
        DirectoryInfo direction = new DirectoryInfo(fullPath);
        FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            FileInfo file = files[i];
            if (file.Extension == ".meta" || file.Extension == ".DS_Store")
            {
                continue;
            }
            string path = GetFileAssetPath(file.FullName);
            string extension = file.Extension.ToLower();
            switch (extension)
            {
                case ".anim":
                    AnimationClip animClip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
                    if (!path.Contains("Test") && !path.Contains("Root"))
                    {
                        animDic.Add(animClip.name, animClip);
                        string animName = GetAnimNmae(animClip, direction.Name);
                        m_loopAnimNameList.TryGetValue(animName, out int isLoop);
                        if (isLoop == 1)
                        {
                            if (!animClip.isLooping)
                            {
                                Debug.LogError($"{animClip}{animClip.name} 此动画忘记勾选 Loop ", animClip);
                            }
                        }
                        else
                        {
                            if (animClip.isLooping)
                            {
                                //Debug.LogError($"{animClip}{animClip.name} 此动画不必勾选 Loop", animClip);
                            }
                        }
                    }
                    break;
            }
        }
        string animControlPath = fullPath + direction.Name + ".controller";
        animControlPath = GetFileAssetPath(animControlPath);
        AnimatorOverrideController animControl = AssetDatabase.LoadAssetAtPath(animControlPath, typeof(AnimatorOverrideController)) as AnimatorOverrideController;
        if (animControl == null)
        {
            Debug.Log($"不存在动画控制器  请先CreateRolePrefab ！！！！");
            return;
        }
        AnimationClip[] clips = animControl.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            animDic.TryGetValue(clips[i].name, out AnimationClip cli);
            if (cli != null && !cli.empty && clips[i].empty)
            {
                Debug.LogError($"此动画文件 :{cli.name} =没有映射到==> AnimatorController !!!", cli);
            }
        }
    }
}
