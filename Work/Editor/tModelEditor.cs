using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using MiniJSON2;
using UnityEngine.Rendering;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;
using GMF;
public enum BindAnimationType
{
    Default,
    Login,
    ShowIdle
}
public class tModelEditor : AssetPostprocessor
{
    const string BASE_ROLE_ANIM_PATH = "Assets/Animations/RoleAnim/anim_Role.controller";
    const string BASE_ROLE_SHOW_IDLE_ANIM_PATH = "Assets/Animations/RoleAnim/anim_Role_ShowIdle.controller";
    const string BASE_ROLE_LOGIN_ANIM_PATH = "Assets/Animations/RoleAnim/anim_Role_Login.controller";

    const string MODEL_CONFIG_PATH = "Assets/Config/ttdbl2_config/ModelInfo.json";

    const string MODEL_CONFIG_PATH_MONSTER = "Assets/Config/ttdbl2_config/MonsterModelInfo/";
     const string MODEL_INFO_CONFIG_PATH_ = "Assets/Models/_ModelInfo/";

    const string CUBEMAP_PATH = "Assets/Models/ModelRole/Novice_F/concrete_tunnel_02_1k2.hdr";
    public const int LAYER_3D = 10;
    public const string PREFAB_PATH = "Assets/Prefabs/";
    public static RuntimeAnimatorController baseAnim, baseIdleAnim, baseLoginAnim;
    public static Texture cubemapTex;
    public static Dictionary<string, AnimationClip> animClipFDic;
    public static Dictionary<string, AnimationClip> animClipMDic;
    public static Dictionary<string, AudioClip> audioClipDic;

    // public static List<string> commonRoleAnimNameList = new List<string>()
    // {
    //     //"die",
    //     "BodyCheck",
    //     "HeadCheck",
    //     "walk_ride",
    //     "walk_ride1",
    //     "walk_ride2",
    //     "walk_ride3"
    // };

    public static List<string> loopAnimNameList = new List<string>()
    {
        "wait",
        "walk",
        "slow",
        "skill_ready",
        "victory",
        "attack_wait",
        "use_skill1_2",
        "use_skill2_2",
        "use_skill3_2",
        "wait_withEye"
    };

    public static List<string> overlapWaitAnimName = new List<string>()
    {
        //fashion use
    };

    public static Dictionary<string, int> roleTexDic = new Dictionary<string, int>()
    {
        { "Wilow_bird", 1 },
    };

    //需要创建Animator的基础职业
    public static string[] baseJobAnimNameArr =
    {
        "Swordman_M", "Swordman_F", "Ranger_M", "Ranger_F", "Magician_M", "Magician_F", "Assassin_M", "Assassin_F",
        "Reverend_M", "Reverend_F"
    };

    //进阶需要用到动画的文件夹
    public static Dictionary<string, string> roleJobAnimDic = new Dictionary<string, string>()
    {
        //--- 2阶
        //身体
        { "Berserker_M", "Swordman_M" },
        { "Berserker_F", "Swordman_F" },
        { "Hunter_M", "Ranger_M" },
        { "Hunter_F", "Ranger_F" },
        { "Mage_M", "Magician_M" },
        { "Mage_F", "Magician_F" },
        { "Wanderer_M", "Assassin_M" },
        { "Wanderer_F", "Assassin_F" },
        { "Priest_M", "Reverend_M" },
        { "Priest_F", "Reverend_F" },
        //头发
        { "Hair_Berserker_M", "Hair_Swordman_M" },
        { "Hair_Berserker_F", "Hair_Swordman_F" },
        { "Hair_Hunter_M", "Hair_Ranger_M" },
        { "Hair_Hunter_F", "Hair_Ranger_F" },
        { "Hair_Mage_M", "Hair_Magician_M" },
        { "Hair_Mage_F", "Hair_Magician_F" },
        { "Hair_Wanderer_M", "Hair_Assassin_M" },
        { "Hair_Wanderer_F", "Hair_Assassin_F" },
        { "Hair_Priest_M", "Hair_Reverend_M" },
        { "Hair_Priest_F", "Hair_Reverend_F" },
    };

    public static Dictionary<string, string> pointPathDic = new Dictionary<string, string>()
    {
        { "CP_1", "hairPath" },
        { "CP_2", "assistantPath" },
        { "CP_3", "weaponPath" },
        { "CP_4", "headPath" }, // 头
        { "CP_5", "wingPath" }, //背
        { "CP_6", "facePath" }, //脸
        { "CP_7", "tailPath" }, //尾
        { "CP_8", "mountPath" }, // 坐骑
        { "CP_9", "leftArmPath" }, // 左臂
        { "CP_10", "rightArmPath" }, //右臂
        { "CP_11", "leftShoulderPath" }, //左肩
        { "CP_12", "rightShoulderPath" }, // 右肩
        { "CP_14", "leftLegPath" }, //左腿
        { "CP_13", "rightLegPath" }, // 右腿
        { "CP_15", "bigTailPath" }, //大尾巴
        { "CP_16", "mountRolePath" }, //坐骑角色挂点
        { "CP_17", "mountRoleWeaponPath" }, //坐骑角色武器挂点

        { "EP_1", "headEffectPath" },
        { "EP_2", "footEffectPath" },
        { "EP_3", "hitPath" },
        { "EP_4", "leftEffectPath" },
        { "EP_5", "rightEffectPath" },
        { "EP_6", "hitEffectPath" },
        { "EP_7", "behindEffectPath" },
        //左右脚挂点
        { "EP_8", "leftFootEffectPath" },
        { "EP_9", "rightFootEffectPath" },
        { "EP_10", "specialEffectPath" }, // 特殊挂点
        { "WP_1", "weaponEffectPath" },
        { "WP_2", "ballisticEffectPath" },
        { "WP_3", "weaponEffectPath2" },
        { "BaseBody_M_Face", "eyePath" }, //男性角色眼部  可和女性优化为BaseBody_Face统一命名
        { "BaseBody_F_Face", "eyePath" }, //女性角色眼部
        { "MP_1", "mountDecorationPath" }, //坐骑装饰
    };

    //骨骼换装骨骼路径
    public static Dictionary<string, string> skeletonPathDic = new Dictionary<string, string>()
    {
        { "Bone_Brow_L1", "Bone_Brow_L1" },
        { "Bone_Brow_L2", "Bone_Brow_L2" },
        { "Bone_Brow_R1", "Bone_Brow_R1" },
        { "Bone_Brow_R2", "Bone_Brow_R2" },
        { "Bone_Eyelid_L1", "Bone_Eyelid_L1" },
        { "Bone_Eyelid_L2", "Bone_Eyelid_L2" },
        { "Bone_Eyelid_R1", "Bone_Eyelid_R1" },
        { "Bone_Eyelid_R2", "Bone_Eyelid_R2" },
        { "Bone_pupil_L", "Bone_pupil_L" },
        { "Bone_pupil_R", "Bone_pupil_R" },
    };

    public static List<string> offReflectionProbesList = new List<string>()
    {
        "Poring",
    };

    public static Dictionary<string, string> effectPathDic = new Dictionary<string, string>()
    {
        { "71SoleWater", "ecp_1" },
        { "Horong", "CP_5" },
        { "fire_0017_order113", "BlackWitch_Weapon" },
        { "KasaKid_Left", "ecp_44" },
        { "KasaKid_Right", "ecp_45" },
        { "KAHO_Step", "ecp_43" },
        { "RedExplosion_fire", "ecp_42" },
        { "90YmirsHeart_continued2", "Bip001" },
        { "Rrandgris_Weapon1", "CP_3" },
    };

    public static Dictionary<string, Vector3[]> effectPosDic = new Dictionary<string, Vector3[]>()
    {
        {
            "Horong",
            new Vector3[]
                { new Vector3(0, -0.5f, -0.1f), new Vector3(0.35f, -0.75f, -0.1f), new Vector3(-0.35f, -0.75f, -0.1f) }
        },
        { "fire_0017_order113", new Vector3[] { new Vector3(0, 0.5f, 0) } },
        { "71SoleWater", new Vector3[] { new Vector3(0, 0, 0.05f) } },
    };

    public static void LoadAsset()
    {
        baseAnim =
            AssetDatabase.LoadAssetAtPath(BASE_ROLE_ANIM_PATH, typeof(RuntimeAnimatorController)) as
                RuntimeAnimatorController;
        baseIdleAnim =
            AssetDatabase.LoadAssetAtPath(BASE_ROLE_SHOW_IDLE_ANIM_PATH, typeof(RuntimeAnimatorController)) as
                RuntimeAnimatorController;
        baseLoginAnim =
            AssetDatabase.LoadAssetAtPath(BASE_ROLE_LOGIN_ANIM_PATH, typeof(RuntimeAnimatorController)) as
                RuntimeAnimatorController;
        cubemapTex = AssetDatabase.LoadAssetAtPath(CUBEMAP_PATH, typeof(Texture)) as Texture;
    }

    public static void CreateRolePrefabWithParams()
    {
    }

   // [MenuItem("Assets/------分离ModelInfo")]
    static void SplitModelInfoJson()
    {
        JSON modelPointJs = GetModelPointJson();
        foreach (string key in modelPointJs.fields.Keys)
        {
            string path = MODEL_INFO_CONFIG_PATH_ + key + ".json";
            JSON js1 = new JSON();
            js1.serialized = modelPointJs.ToString2(key);
                // js1.serialized = modelPointJs.Value[key].ToString();
            SaveJson(js1, path);
            //Debug.Log($"@@@@Key: {key}  ===> {path}");
        }
        AssetDatabase.Refresh();
    }
   static void SavNewModelInfoJson(string name,string info) {
       string path = MODEL_INFO_CONFIG_PATH_ + name + ".json";
        JSON js1 = new JSON();
        js1.serialized = info;
        SaveJson(js1, path);
   }

    // [MenuItem("Assets/------SplitMonsterJson")]
    // static void SplitMonsterJson()
    // {
    //     //   JSON js = new JSON();
    //     //         js.serialized = textAsset.text;

    //     JSON modelPointJs = GetModelPointJson();
    //     int num = 0;
    //     Object[] folderObj = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

    //     Dictionary<string, string> monsterNamePathDic = new Dictionary<string, string>();

    //     foreach (Object folder in folderObj)
    //     {
    //         string assetPath = AssetDatabase.GetAssetPath(folder) + "/";
    //         string fullPath = GetFullPathWithAssetPath(assetPath);
    //         DirectoryInfo direction = new DirectoryInfo(fullPath);
    //         DirectoryInfo[] dirs = direction.GetDirectories("*", SearchOption.TopDirectoryOnly);
    //         for (int i = 0; i < dirs.Length; i++)
    //         {
    //             string dirName = dirs[i].Name.ToLower();
    //             if (!dirName.Contains("test") && !dirName.Contains("root") && !dirName.Contains("materials"))
    //             {
    //                 if (monsterNamePathDic.ContainsKey(dirs[i].Name))
    //                 {
    //                     Debug.Log($"Key ===== '{dirs[i].Name}' {fullPath}");
    //                 }
    //                 else
    //                 {
    //                     monsterNamePathDic.Add(dirs[i].Name, fullPath);
    //                 }
    //                 num++;
    //                 // Debug.Log($"@@@@@@@@@@@@@@@@@Key: {dirs[i].Name}{num}");
    //             }
    //         }

    //         if (monsterNamePathDic.ContainsKey(folder.name))
    //         {
    //             Debug.Log($"Key == '{folder.name}'{AssetDatabase.GetAssetPath(folder)}");
    //         }
    //         else
    //         {
    //             monsterNamePathDic.Add(folder.name, AssetDatabase.GetAssetPath(folder));
    //         }
    //         num++;
    //         // Debug.Log($"@@@@@@@@@@@@@@@@@Key: {folder.name}{num}");
    //     }
    //     num = 0;

    //     List<string> monsterKeys = new List<string>();
    //     foreach (string key in modelPointJs.fields.Keys)
    //     {
    //         if (monsterNamePathDic.ContainsKey(key))
    //         {
    //             num++;
    //             string path = MODEL_CONFIG_PATH_MONSTER + key + ".json";
    //             JSON js1 = new JSON();
    //             js1.serialized = modelPointJs.ToString2(key);
    //             // js1.serialized = modelPointJs.Value[key].ToString();
    //             SaveJson(js1, path);
    //             monsterKeys.Add(key);
    //             Debug.Log($"@@@@Key: {key} path {monsterNamePathDic[key]} ===> {path}");
    //         }
    //     }
    //     AssetDatabase.Refresh();
    //     for (int i = 0; i < monsterKeys.Count; i++)
    //     {
    //         JSON roleJson = new JSON();
    //         modelPointJs[monsterKeys[i]] = roleJson;
    //     }
    //     SaveModelPointJson(modelPointJs);

    //     AssetDatabase.Refresh();

    // }

    // [MenuItem("Assets/--+++++ClearMonsterJson")]
    // static void ClearMonsterJson()
    // {


    //     JSON modelPointJs = GetModelPointJson();

    //     int num = 0;
    //     Object[] folderObj = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

    //     List<string> monsterKeys = new List<string>();

    //     foreach (Object folder in folderObj)
    //     {
    //         string assetPath = AssetDatabase.GetAssetPath(folder) + "/";
    //         string fullPath = GetFullPathWithAssetPath(assetPath);
    //         DirectoryInfo direction = new DirectoryInfo(fullPath);
    //         DirectoryInfo[] dirs = direction.GetDirectories("*", SearchOption.TopDirectoryOnly);
    //         for (int i = 0; i < dirs.Length; i++)
    //         {
    //             string dirName = dirs[i].Name.ToLower();
    //             if (!dirName.Contains("test") && !dirName.Contains("root") && !dirName.Contains("materials"))
    //             {
    //                 monsterKeys.Add(dirs[i].Name);
    //                 // Debug.Log($"@@@@@@@@@@@@@@@@@Key: {dirs[i].Name}{num}");
    //             }
    //         }
    //         monsterKeys.Add(folder.name);
    //         // Debug.Log($"@@@@@@@@@@@@@@@@@Key: {folder.name}{num}");
    //     }
    //     num = 0;
    //     for (int i = 0; i < monsterKeys.Count; i++)
    //     {
    //         //JSON roleJson = new JSON();

    //         modelPointJs.fields.Remove(monsterKeys[i]);


    //     }
    //     SaveModelPointJson(modelPointJs);

    //     AssetDatabase.Refresh();

    // }


    [MenuItem("Assets/CreateRolePrefab")]
    static void CreateRolePrefab()
    {
        Object[] folderObj = null;
        folderObj = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
        CreateRolePrefabLogic(folderObj);
    }

    public static void CreateRolePrefabLogic(Object[] folderObj)
    {
        LoadAsset();
        JSON modelPointJs = GetModelPointJson();
        animClipFDic = GetRoleAnimClipArr("/Animations/RoleAnim/Role_F");
        animClipMDic = GetRoleAnimClipArr("/Animations/RoleAnim/Role_M");
        audioClipDic = new Dictionary<string, AudioClip>();

        foreach (Object folder in folderObj)
        {
            string assetPath = AssetDatabase.GetAssetPath(folder) + "/";
            tModelInfo info = new tModelInfo();
            string[] folderNameArr = folder.name.Split('@');
            bool isShareAsset = folderNameArr.Length > 1;
            info.name = folderNameArr[0];
            info.createPath = assetPath;
            info.modelPointJs = modelPointJs;
            string folderPath = assetPath;
            string createPath = folderPath;
            for (int i = 0; i < folderNameArr.Length; i++)
            {
                string path = assetPath;
                if (i == 0)
                {
                    createPath = assetPath.Replace(folder.name, folderNameArr[i]);
                }
                else
                {
                    path = assetPath.Replace(folder.name, folderNameArr[i]);
                }

                bool isAddMatOrTex = true;
                if (isShareAsset && i > 0)
                {
                    isAddMatOrTex = false;
                }

                LoadFolder(info, path, isAddMatOrTex, folder.name);
            }

            info.Create(folderPath);
            SavNewModelInfoJson(folder.name,modelPointJs.ToString2(folder.name));
        }

        SaveModelPointJson(modelPointJs);

        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/CreatePetOrMonsterPrefab")]
    static void CreateMonsterPrefab()
    {
        Object[] folderObj = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
        LoadAsset();
        JSON modelPointJs = GetModelPointJson();
        animClipFDic = GetRoleAnimClipArr("/Animations/RoleAnim/Role_F");
        animClipMDic = GetRoleAnimClipArr("/Animations/RoleAnim/Role_M");
        audioClipDic = new Dictionary<string, AudioClip>();

        foreach (Object folder in folderObj)
        {
            string assetPath = AssetDatabase.GetAssetPath(folder) + "/";
            tModelInfo info = new tModelInfo();
            info.name = folder.name;
            info.baseName = folder.name;
            info.modelPointJs = modelPointJs;
            info.createPath = assetPath;
            Debug.Log($"@@@@@@@@@@@@@@文件夹路径===>{assetPath}");
            LoadFolderAsset(info, assetPath, folder.name);
            info.Create(assetPath);
            SavNewModelInfoJson(folder.name,modelPointJs.ToString2(folder.name));
            // 换皮
            string fullPath = GetFullPathWithAssetPath(assetPath);
            DirectoryInfo direction = new DirectoryInfo(fullPath);
            DirectoryInfo[] dirs = direction.GetDirectories("*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < dirs.Length; i++)
            {
                string dirName = dirs[i].Name.ToLower();
                if (!dirName.Contains("test") && !dirName.Contains("root"))
                {
                    string createPath = assetPath + dirs[i].Name + "/"; // 换皮怪创建路径
                    tModelInfo shareModelInfo = new tModelInfo();
                    shareModelInfo.name = dirs[i].Name;
                    shareModelInfo.createPath = createPath;
                    shareModelInfo.baseName = folder.name;
                    shareModelInfo.basePath = assetPath;
                    shareModelInfo.modelPointJs = modelPointJs;
                    shareModelInfo.isReplace = true;

                    LoadFolderAsset(shareModelInfo, shareModelInfo.basePath, shareModelInfo.baseName, true);
                    LoadFolderAsset(shareModelInfo, shareModelInfo.createPath, shareModelInfo.name, false, true);
                    shareModelInfo.Create(assetPath);


                    SavNewModelInfoJson(dirs[i].Name,modelPointJs.ToString2(dirs[i].Name));
                }
            }

            SaveModelPointJson(modelPointJs);
        }

        AssetDatabase.Refresh();
    }
    [MenuItem("Assets/CreateFarmPrefab")]
    static void CreateFarmPrefab()
    {
        Object[] folderObj = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
        LoadAsset();
        JSON modelPointJs = GetModelPointJson();
        foreach (Object folder in folderObj)
        {
            string assetPath = AssetDatabase.GetAssetPath(folder) + "/";
            tModelInfo info = new tModelInfo();
            info.name = folder.name;
            info.baseName = folder.name;
            info.modelPointJs = modelPointJs;
            info.createPath = assetPath;
            Debug.Log($"@@@@@@@@@@@@@@文件夹路径===>{assetPath}");
            LoadFolderAsset(info, assetPath, folder.name);
            info.Create(assetPath);
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/CreateShowIdlePrefab")]
    static void CreateShowIdlePrefab()
    {
        Object[] folderObj = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
        LoadAsset();
        JSON modelPointJs = GetModelPointJson();
        foreach (Object folder in folderObj)
        {
            string assetPath = AssetDatabase.GetAssetPath(folder) + "/";
            tModelInfo info = new tModelInfo();
            info.name = folder.name;
            info.baseName = folder.name;
            info.modelPointJs = modelPointJs;
            info.createPath = assetPath;
            Debug.Log($"@@@@@@@@@@@@@@文件夹路径===>{assetPath}");
            LoadFolderAsset(info, assetPath, folder.name);
            info.Create(assetPath);
        }
        AssetDatabase.Refresh();
    }
    
    static void LoadFolderAsset(tModelInfo info, string assetPath, string folderName, bool isLoadBase = false,
        bool isReplace = false)
    {
        string fullPath = GetFullPathWithAssetPath(assetPath);
        DirectoryInfo direction = new DirectoryInfo(fullPath);
        FileInfo[] files = direction.GetFiles("*", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < files.Length; i++)
        {
            FileInfo file = files[i];
            if (file.Extension == ".meta" || file.Extension == ".DS_Store")
            {
                continue;
            }

            string path = GetFileAssetPath(file);
            string extension = file.Extension.ToLower();
            AssetImporter ai = AssetImporter.GetAtPath(path);
            string replacePath = "Assets/Models/";
            if (!isLoadBase)
            {
                string abPath = assetPath.Replace(replacePath, "");
                if (isReplace)
                {
                    abPath = abPath.Replace(info.baseName + "/" + folderName + "/", folderName);
                }
                else
                {
                    abPath = abPath.Replace(folderName + "/", folderName);
                }

                ai.assetBundleName = abPath;
            }

            switch (extension)
            {
                case ".fbx":
                    if (info.obj == null)
                    {
                        info.obj = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                    }

                    break;
                case ".asset":
                    string fileName = file.Name.Replace(".asset", "");
                    if (info.mesh == null && fileName.Equals(folderName))
                    {
                        info.mesh = AssetDatabase.LoadAssetAtPath(path, typeof(Mesh)) as Mesh;
                    }

                    break;
                case ".png":
                case ".tga":
                    if (!isReplace) // 非替换
                    {
                        if (info.tex == null)
                        {
                            info.tex = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
                        }
                    }

                    break;
                case ".anim":
                    AnimationClip animClip =
                        AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
                    if (!path.Contains("Test") && !path.Contains("Root"))
                    {
                        info.animClipDic.Add(animClip.name, animClip);
                    }

                    break;
                case ".wav":
                    AudioClip audioClip = AssetDatabase.LoadAssetAtPath(path, typeof(AudioClip)) as AudioClip;
                    info.audioClipDic.Add(audioClip.name, audioClip);
                    break;
                case ".prefab":
                    GameObject effectObj = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                    if (fullPath.Contains("/ModelMonster/"))
                    {
                        if (effectObj.name.Equals("SK_Camera"))
                            info.skCamera = effectObj;
                        else if (effectObj.name.Equals("FX"))
                            info.modelFx = effectObj;
                    }
                    else
                    {
                        info.effectObjList.Add(effectObj);
                    }

                    break;
                case ".mat":
                    if (!path.Contains("Test") && !path.Contains("Root"))
                    {
                        if (!isReplace)
                        {
                            Material mat = AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
                            //info.matList.Add(mat);
                            info.AddMat(mat);
                        }
                    }

                    break;
                case ".playable":
                    if (info.modelTimeline == null)
                    {
                        info.modelTimeline =
                            AssetDatabase.LoadAssetAtPath(path, typeof(TimelineAsset)) as TimelineAsset;
                    }

                    break;
            }
        }

        if (roleJobAnimDic.TryGetValue(folderName, out string animFolderName))
        {
            fullPath = fullPath.Replace(folderName, animFolderName);
            DirectoryInfo direction2 = new DirectoryInfo(fullPath);
            FileInfo[] files2 = direction2.GetFiles("*.anim", SearchOption.AllDirectories);
            for (int i = 0; i < files2.Length; i++)
            {
                FileInfo file = files2[i];
                if (file.Extension == ".meta" || file.Extension == ".DS_Store")
                {
                    continue;
                }

                string path = GetFileAssetPath(file);
                string extension = file.Extension.ToLower();
                AssetImporter ai = AssetImporter.GetAtPath(path);
                // ai.assetBundleName = "";
                AnimationClip animClip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
                if (!path.Contains("Test") && !path.Contains("Root"))
                {
                    if (info.animClipDic.ContainsKey(animClip.name))
                    {
                        info.animClipDic[animClip.name] = animClip;
                    }
                    else
                    {
                        info.animClipDic.Add(animClip.name, animClip);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 角色资源设置ModelShareBundle中(状态机/动画片段)
    /// </summary>
    public static void CheckToSetRoleAssetIntoModelShareBundle(AssetImporter ai, string ext, tModelInfo.Kind kind)
    {
        switch (ext)
        {
            case ".anim":
            case ".controller":
                Debug.Log(ai.assetPath);
                if (ai.assetPath.Contains("battleshow", StringComparison.CurrentCultureIgnoreCase) ||
                    ai.assetPath.Contains("xuanjue", StringComparison.CurrentCultureIgnoreCase) ||
                    ai.assetPath.Contains("login", StringComparison.CurrentCultureIgnoreCase) ||
                    ai.assetPath.Contains("weaponswitch", StringComparison.CurrentCultureIgnoreCase) ||
                    ai.assetPath.Contains("wait_witheye", StringComparison.CurrentCultureIgnoreCase)
                   )
                {
                    ai.assetBundleName = "plane/logingameplane";
                }
                else if (ai.assetPath.Contains("/wait.anim", StringComparison.CurrentCultureIgnoreCase))
                {
                    ai.assetBundleName = "modelanimation/showidle";
                }
                else if (ai.assetPath.Contains("/showidle.anim", StringComparison.CurrentCultureIgnoreCase))
                {
                    ai.assetBundleName = "modelshare";
                }
                else if (ai.assetPath.Contains("attack_wait", StringComparison.CurrentCultureIgnoreCase))
                {
                    ai.assetBundleName = "modelshare";
                }
                else if (ai.assetPath.Contains("showidle", StringComparison.CurrentCultureIgnoreCase))
                {
                    ai.assetBundleName = "modelanimation/showidle";
                }
                else
                {
                    string folderPath = ai.assetPath;
                    if (//folderPath.Contains("/ModelRole/") || folderPath.Contains("/ModelSuit/") ||
                     folderPath.Contains("/ModelWeapon/Ranger_F_Bow/"))
                    {
                        ai.assetBundleName = "modelshare";
                    }
                    else
                    {
                        string abPath = folderPath.Replace("Assets/Models/", "");
                        abPath = abPath.Remove(abPath.LastIndexOf('/'));
                        if (kind == tModelInfo.Kind.MAN || kind == tModelInfo.Kind.WOMAN || folderPath.Contains("/ModelHair/"))
                        {
                            abPath = abPath.Substring(abPath.LastIndexOf('/') + 1);
                            ai.assetBundleName = $"modelanimation/battle_{abPath}";
                        }
                        else
                        {
                            ai.assetBundleName = abPath;
                        }
                    }
                }

                break;
        }
    }


    /// <summary>
    /// 目前 角色创建使用
    /// </summary>
    /// <param name="info"></param>
    /// <param name="assetPath"></param>
    /// <param name="isAddMatOrTex"></param>
    /// <param name="folderName"></param>
    static void LoadFolder(tModelInfo info, string assetPath, bool isAddMatOrTex, string folderName)
    {
        string fullPath = GetFullPathWithAssetPath(assetPath);
        DirectoryInfo direction = new DirectoryInfo(fullPath);
        FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            FileInfo file = files[i];
            if (file.Extension == ".meta" || file.Extension == ".DS_Store")
            {
                continue;
            }

            string path = GetFileAssetPath(file);
            string extension = file.Extension.ToLower();
            AssetImporter ai = AssetImporter.GetAtPath(path);
            if (ai == null)
            {
                Debug.Log($"资源存在，但是unity无法解析，跳过：{path}");
                continue;
            }
            if (path.Contains("/ModelRole/"))
            {
                CheckToSetRoleAssetIntoModelShareBundle(ai, extension, tModelInfo.Kind.MAN);
            }
            else
            {
                CheckToSetRoleAssetIntoModelShareBundle(ai, extension, tModelInfo.Kind.FASHION);
            }
            switch (extension)
            {
                case ".fbx":
                    if (info.obj == null)
                    {
                        info.obj = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                    }

                    break;
                case ".asset":
                    string fileName = file.Name.Replace(".asset", "");
                    if (info.mesh == null && fileName.Equals(folderName))
                    {
                        info.mesh = AssetDatabase.LoadAssetAtPath(path, typeof(Mesh)) as Mesh;
                    }

                    break;
                case ".png":
                case ".tga":
                    if (isAddMatOrTex)
                    {
                        if (info.tex == null)
                        {
                            info.tex = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
                        }
                    }

                    break;
                case ".anim":
                    AnimationClip animClip =
                        AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
                    if (!path.Contains("Test") && !path.Contains("Root"))
                    {
                        info.animClipDic.Add(animClip.name, animClip);
                    }

                    break;
                case ".wav":
                    AudioClip audioClip = AssetDatabase.LoadAssetAtPath(path, typeof(AudioClip)) as AudioClip;
                    info.audioClipDic.Add(audioClip.name, audioClip);
                    break;
                case ".prefab":
                    GameObject effectObj = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                    if (fullPath.Contains("/ModelMonster/"))
                    {
                        if (effectObj.name.Equals("SK_Camera"))
                            info.skCamera = effectObj;
                        else if (effectObj.name.Equals("FX"))
                            info.modelFx = effectObj;
                    }
                    else
                    {
                        info.effectObjList.Add(effectObj);
                    }

                    break;
                case ".mat":
                    if (!path.Contains("Test") && !path.Contains("Root"))
                    {
                        if (isAddMatOrTex)
                        {
                            Material mat = AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
                            //info.matList.Add(mat);
                            info.AddMat(mat);
                        }
                    }

                    break;
                case ".playable":
                    info.modelTimeline = AssetDatabase.LoadAssetAtPath(path, typeof(TimelineAsset)) as TimelineAsset;
                    break;
            }
        }


        if (roleJobAnimDic.TryGetValue(folderName, out string animFolderName))
        {
            fullPath = fullPath.Replace(folderName, animFolderName);
            DirectoryInfo direction2 = new DirectoryInfo(fullPath);
            FileInfo[] files2 = direction2.GetFiles("*.anim", SearchOption.AllDirectories);
            for (int i = 0; i < files2.Length; i++)
            {
                FileInfo file = files2[i];
                if (file.Extension == ".meta" || file.Extension == ".DS_Store")
                {
                    continue;
                }

                string path = GetFileAssetPath(file);
                string extension = file.Extension.ToLower();
                AssetImporter ai = AssetImporter.GetAtPath(path);
                // ai.assetBundleName = "";
                AnimationClip animClip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
                if (!path.Contains("Test") && !path.Contains("Root"))
                {
                    if (info.animClipDic.ContainsKey(animClip.name))
                    {
                        info.animClipDic[animClip.name] = animClip;
                    }
                    else
                    {
                        info.animClipDic.Add(animClip.name, animClip);
                    }
                }
            }
        }
    }

    static Dictionary<string, AnimationClip> GetRoleAnimClipArr(string assetPath)
    {
        return new Dictionary<string, AnimationClip>();
        ;
    }

    public static void SaveJson(JSON js, string path)
    {
        path = GetFullPathWithAssetPath(path);
        byte[] bytes = Encoding.UTF8.GetBytes(js.serialized);
        ToolText.WriteFile(path, bytes);
    }

    static void SaveModelPointJson(JSON js)
    {
        SaveJson(js, MODEL_CONFIG_PATH);
    }

    public static JSON GetAssetJson(string path)
    {
        TextAsset textAsset = AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset)) as TextAsset;
        JSON js = new JSON();
        js.serialized = textAsset.text;
        return js;
    }

    public static JSON GetAssetJsonMonster(string path)
    {
        TextAsset textAsset = AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset)) as TextAsset;
        JSON js = new JSON();
        js.serialized = textAsset.text;
        return js;
    }



    static JSON GetModelPointJson()
    {
        return GetAssetJson(MODEL_CONFIG_PATH);
    }
    static JSON GetModelPointJson(string fileName)
    {
        return GetAssetJsonMonster(fileName);
    }

    public static string GetFileName(FileInfo file)
    {
        string name = file.Name.Replace(file.Extension, string.Empty);
        return name;
    }

    public static string GetFileAssetPath(FileInfo file)
    {
        string path = file.FullName.Replace("\\", "/");
        path = path.Replace(Application.dataPath, "Assets");
        return path;
    }

    public static string GetFullPathWithAssetPath(string assetPath)
    {
        return Application.dataPath.Replace("Assets", string.Empty) + assetPath;
    }

    public static string GetParentPath(Transform tra)
    {
        string path = tra.name;
        while (tra.parent != null && tra.root != tra.parent)
        {
            tra = tra.parent;
            path = tra.name + "/" + path;
        }

        return path;
    }
}

public class tModelInfo
{
    /// <summary>
    /// 是否是 换皮
    /// </summary>
    public bool isReplace = false;

    /// <summary>
    /// 预制名字
    /// </summary>
    public string name;

    public string baseName;
    public string basePath;
    public string folderPath;
    public string createPath;

    public Mesh mesh;
    public GameObject obj;
    public GameObject mainObj;
    public GameObject modelObj;
    public GameObject shadowObj;
    public TimelineAsset modelTimeline;
    public GameObject skCamera;
    public GameObject modelFx;
    public List<GameObject> effectObjList = new List<GameObject>();
    public Texture2D tex;
    public List<Material> matList = new List<Material>();

    public string animPath;
    public string loginAnimPath;
    public string showIdleAnimPath;
    public Kind kind = Kind.MONSTER;
    public JSON modelPointJs;
    public AnimatorOverrideController animControl;
    public Dictionary<string, AnimationClip> animClipDic = new Dictionary<string, AnimationClip>();
    public Dictionary<string, AudioClip> audioClipDic = new Dictionary<string, AudioClip>();
    public AnimEvent animEvent;

    private Dictionary<string, List<SortInfo>> m_dicMats = new Dictionary<string, List<SortInfo>>();
    private Dictionary<string, List<Material>> m_commonMats = new Dictionary<string, List<Material>>();

    private Dictionary<string, string> m_commonMatName = new Dictionary<string, string>()
    {
        // 臉
        { "BaseBody_M_Face", "Bodybase_&&_Head" },
    };

    //----------------- 使用通用皮肤的材质
    private Dictionary<string, Material> m_commonBodyMats = new Dictionary<string, Material>();

    private Dictionary<string, string> m_commonBodyMatName = new Dictionary<string, string>()
    {
        // 皮肤
        { "Ranger_F_Equip", "Bodybase_&&_Body" },
        { "Ranger_M_Equip", "Bodybase_&&_Body" },
        { "Magician_M_Equip", "Bodybase_&&_Body" },
        { "Assassin_F_Equip", "Bodybase_&&_Body" },
        { "Assassin_M_Equip", "Bodybase_&&_Body" },
        { "Reverend_M_Equip", "Bodybase_&&_Body" },
        { "Reverend_F_Equip", "Bodybase_&&_Body" },
        { "Berserker_F_Equip", "Bodybase_&&_Body" },
        { "Berserker_M_Equip", "Bodybase_&&_Body" },
        { "Hunter_F_Equip", "Bodybase_&&_Body" },
        { "Mage_F_Equip", "Bodybase_&&_Body" },
        { "Wanderer_F_Equip", "Bodybase_&&_Body" },
        { "Wanderer_M_Equip", "Bodybase_&&_Body" },
        { "Priest_F_Equip", "Bodybase_&&_Body" },
        { "Priest_M_Equip", "Bodybase_&&_Body" },
    };

    //使用到通用皮肤材质的模型 都是在元素第二位 模型名字 ，mesh使用的名字
    public Dictionary<string, string> m_useCommonBodyMats = new Dictionary<string, string>()
    {
        // 一阶
        { "Ranger_M", "Ranger_M_Equip" },
        { "Magician_M", "Magician_M_Equip" },
        { "Assassin_M", "Assassin_M_Equip" },
        { "Reverend_M", "Reverend_M_Equip" },
        { "Reverend_F", "Reverend_F_Equip" },
        //二阶
        { "Berserker_M", "Berserker_F_Equip" },
        { "Berserker_F", "Berserker_M_Equip" },
        { "Mage_F", "Mage_F_Equip" },

        { "Priest_M", "Priest_F_Equip" },
        { "Priest_F", "Priest_M_Equip" },
    };

    const string BASE_BODY_F_FACE_PATH = "Assets/Art/Character/Bodybase/Famale/";
    const string BASE_BODY_M_FACE_PATH = "Assets/Art/Character/Bodybase/Male/";

    public enum Kind
    {
        MONSTER,
        MAN,
        WOMAN,
        FASHION,
        EYE,
        FARM
    }

    private void LoadCommonMat()
    {
        foreach (var item in m_commonMatName)
        {
            string skinName = item.Key;
            string path = "";
            string matName = "";
            switch (kind)
            {
                case Kind.MAN:
                    path = BASE_BODY_M_FACE_PATH;
                    matName = item.Value.Replace("&&", "M");
                    break;
                case Kind.WOMAN:
                    path = BASE_BODY_F_FACE_PATH;
                    matName = item.Value.Replace("&&", "F");
                    break;
            }

            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            path = path + matName + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
            if (mat != null)
            {
                if (m_commonMats.TryGetValue(skinName, out List<Material> list))
                {
                    list.Add(mat);
                }
                else
                {
                    List<Material> materials = new List<Material>();
                    list = materials;
                    list.Add(mat);
                    m_commonMats.Add(skinName, list);
                }
            }
        }

        LoadCommonBodyMat();
    }

    private void LoadCommonBodyMat()
    {
        foreach (var item in m_commonBodyMatName)
        {
            string skinName = item.Key;
            string path = "";
            string matName = "";
            switch (kind)
            {
                case Kind.MAN:
                    path = BASE_BODY_M_FACE_PATH;
                    matName = item.Value.Replace("&&", "M");
                    break;
                case Kind.WOMAN:
                    path = BASE_BODY_F_FACE_PATH;
                    matName = item.Value.Replace("&&", "F");
                    break;
            }

            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            path = path + matName + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
            if (mat != null)
            {
                if (!m_commonBodyMats.TryGetValue(skinName, out Material list))
                {
                    m_commonBodyMats.Add(skinName, mat);
                }
            }
        }
    }

    public void Create(string _folderPath)
    {
        folderPath = _folderPath;
        if (folderPath.Contains("/ModelRole/") || folderPath.Contains("/ModelMonster/") ||
            folderPath.Contains("/ModelSuit/") || folderPath.Contains("/ModelPet/") || folderPath.Contains("/ModelMount/"))
        {
            kind = Kind.MONSTER;
            string[] nameArr = name.Split('_');
            if (!folderPath.Contains("/ModelMonster/"))
            {
                switch (nameArr[nameArr.Length - 1])
                {
                    case "M":
                        kind = Kind.MAN;
                        break;
                    case "F":
                        kind = Kind.WOMAN;
                        break;
                }
            }

            CreateRole();
        }
        else if (folderPath.Contains("/ModelFarm/"))
        {
            kind = Kind.MONSTER;
            CreateFarm();
        }
        else if (folderPath.Contains("/ModelShowIdle/"))
        {
            kind = Kind.FASHION;
            CreateShowIdle();
        }
        else
        {
            kind = Kind.FASHION;
            // if (folderPath.Contains("/Eye/"))
            // {
            //     kind = Kind.EYE;
            // }

            CreateFashion();
        }
    }

    AnimatorOverrideController CheckToCreateLoginOverrideAnimator()
    {
        if (kind == Kind.MAN || kind == Kind.WOMAN || (kind == Kind.FASHION && folderPath.Contains("/ModelHair/")))
        {
            bool isBaseRole = false;
            foreach (var name in tModelEditor.baseJobAnimNameArr)
            {
                if (folderPath.Contains(name) && kind != Kind.FASHION)
                {
                    isBaseRole = true;
                    break;
                }
            }
            //登录动画控制器状态机重载
            AnimatorOverrideController loginAnimControl = new AnimatorOverrideController();
            loginAnimControl.runtimeAnimatorController = tModelEditor.baseLoginAnim;
            foreach (KeyValuePair<string, AnimationClip> dic in animClipDic)
            {
                if (dic.Key.Contains("xuanjue", StringComparison.CurrentCultureIgnoreCase) ||
                    dic.Key.Contains("weaponswitch", StringComparison.CurrentCultureIgnoreCase) ||
                    dic.Key.Contains("wait_witheye", StringComparison.CurrentCultureIgnoreCase) ||
                    dic.Key.Contains("wait", StringComparison.CurrentCultureIgnoreCase) ||
                    dic.Key.Contains("showidle", StringComparison.CurrentCultureIgnoreCase) ||//只要包含showidle都被loginAnim引用
                    dic.Key.Contains("battleshowpose_evolve", StringComparison.CurrentCultureIgnoreCase)
                   )
                    loginAnimControl[dic.Key] = dic.Value;
            }
            //检查通用动作
            if (isBaseRole) CheckBaseRoleCommonAnim(loginAnimControl);
            if (kind == Kind.FASHION) CheckBaseFashionCommonAnim(loginAnimControl);
            if (isBaseRole || kind == Kind.FASHION)
            {
                loginAnimPath = folderPath + name + "_Login.controller";
                AssetDatabase.CreateAsset(loginAnimControl, loginAnimPath);
            }

            return loginAnimControl;
        }

        return null;
    }
    AnimatorOverrideController CheckToCreateShowIdleAnimator()
    {
        if (kind == Kind.MAN || kind == Kind.WOMAN || (kind == Kind.FASHION && folderPath.Contains("/ModelHair/")))
        {
            bool isBaseRole = false;
            foreach (var name in tModelEditor.baseJobAnimNameArr)
            {
                if (folderPath.Contains(name) && kind != Kind.FASHION)
                {
                    isBaseRole = true;
                    break;
                }
            }

            AnimatorOverrideController idleAnimatorController = new AnimatorOverrideController();
            idleAnimatorController.runtimeAnimatorController = tModelEditor.baseIdleAnim;
            foreach (KeyValuePair<string, AnimationClip> dic in animClipDic)
            {
                if (
                    dic.Key.Contains("showidle", StringComparison.CurrentCultureIgnoreCase) ||
                    dic.Key.Contains("attack_wait", StringComparison.CurrentCultureIgnoreCase) ||
                    dic.Key.Contains("wait", StringComparison.CurrentCultureIgnoreCase) ||
                    dic.Key.Contains("walk", StringComparison.CurrentCultureIgnoreCase)

                )
                    idleAnimatorController[dic.Key] = dic.Value;
            }
            //检查通用动作
            if (isBaseRole) CheckBaseRoleCommonAnim(idleAnimatorController, BindAnimationType.ShowIdle);
            if (kind == Kind.FASHION) CheckBaseFashionCommonAnim(idleAnimatorController, BindAnimationType.ShowIdle);
            if (isBaseRole || kind == Kind.FASHION)
            {
                showIdleAnimPath = folderPath + name + "_ShowIdle.controller";
                AssetDatabase.CreateAsset(idleAnimatorController, showIdleAnimPath);
            }

            return idleAnimatorController;
        }

        return null;
    }

    //bindType目的是区分不同状态机需要绑定的动作
    void CheckBaseRoleCommonAnim(AnimatorOverrideController animController, BindAnimationType bindType = BindAnimationType.Default)
    {
        if (kind == Kind.MAN)
        {
            ModelEditorConfig.InitBaseRoleAnimator(animController, "Assets/Animations/CommonRoleAnim/Male/", bindType);
        }
        else
        {
            ModelEditorConfig.InitBaseRoleAnimator(animController, "Assets/Animations/CommonRoleAnim/FeMale/", bindType);
        }
    }

    void CheckBaseFashionCommonAnim(AnimatorOverrideController animController, BindAnimationType bindType = BindAnimationType.Default)
    {
        ModelEditorConfig.InitBaseFashionCommonAnim(animController, folderPath, bindType);
    }

    void CreateBase()
    {
        modelObj = MonoBehaviour.Instantiate(obj) as GameObject;
        SkinnedMeshRenderer[] skinArr = modelObj.transform.GetComponentsInChildren<SkinnedMeshRenderer>();
        bool isOffRef = tModelEditor.offReflectionProbesList.Contains(name);
        foreach (SkinnedMeshRenderer skin in skinArr)
        {
            if (mesh != null)
            {
                skin.sharedMesh = mesh;
            }

            skin.gameObject.layer = tModelEditor.LAYER_3D;
            string skinName = skin.name;
            if (m_commonMats.TryGetValue(skinName, out List<Material> commonList))
            {
                skin.sharedMaterials = commonList.ToArray();
            }
            else
            {
                if (m_dicMats.TryGetValue(skinName, out List<SortInfo> list))
                {
                    list.Sort((x, y) => x.Index.CompareTo(y.Index));
                    List<Material> mats = new List<Material>();
                    for (int i = 0; i < list.Count; i++)
                    {
                        mats.Add(list[i].Mat);
                    }

                    if (m_useCommonBodyMats.ContainsKey(name))
                    {
                        m_commonBodyMats.TryGetValue(skinName, out Material mat);
                        if (mat != null)
                        {
                            if (mats.Count > 1)
                            {
                                mats.Insert(1, mat);
                            }
                            else
                            {
                                mats.Add(mat);
                            }
                        }
                    }

                    skin.sharedMaterials = mats.ToArray();
                }
            }

            if (isOffRef)
            {
                skin.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
        }

        MeshRenderer[] meshRenArr = modelObj.transform.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer ren in meshRenArr)
        {
            ren.name = name;
            ren.gameObject.layer = tModelEditor.LAYER_3D;
            if (m_dicMats.TryGetValue(name, out List<SortInfo> list))
            {
                list.Sort((x, y) => x.Index.CompareTo(y.Index));
                List<Material> mats = new List<Material>();
                for (int i = 0; i < list.Count; i++)
                {
                    mats.Add(list[i].Mat);
                }

                ren.sharedMaterials = mats.ToArray();
            }

            if (isOffRef)
            {
                ren.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
        }

        Transform matTra = modelObj.transform.Find(obj.name);
        if (matTra == null)
        {
            matTra = modelObj.transform;
        }

        matTra.gameObject.layer = tModelEditor.LAYER_3D;
        matTra.name = name;


        if (animClipDic.Count == 0 && kind == Kind.FASHION)
        {
            MonoBehaviour.DestroyImmediate(modelObj.GetComponent<Animator>());
            return;
        }

        //是否基础职业角色
        bool isBaseRole = kind == Kind.MONSTER;
        if (kind == Kind.MAN || kind == Kind.WOMAN)
        {
            foreach (var name in tModelEditor.baseJobAnimNameArr)
            {
                if (folderPath.Contains(name))
                {
                    isBaseRole = true;
                    break;
                }
            }
        }
        //基础动画控制器状态机的重载
        animControl = new AnimatorOverrideController();
        animControl.runtimeAnimatorController = tModelEditor.baseAnim;
        CheckNilAnim();
        //角色通用动画片段,放在本体引用之前，优先级比本体路径下的动画文件低
        if (isBaseRole) CheckBaseRoleCommonAnim(animControl);
        if (kind == Kind.FASHION) CheckBaseFashionCommonAnim(animControl);
        foreach (KeyValuePair<string, AnimationClip> dic in animClipDic)
        {
            if (dic.Key.Contains("xuanjue") ||
                dic.Key.Contains("weaponswitch") ||
                dic.Key.Contains("wait_withEye") ||
                dic.Key.Contains("battleshow", StringComparison.CurrentCultureIgnoreCase)
               ) continue;
            //覆写Login外动画
            animControl[dic.Key] = dic.Value;
        }

        //角色创角动画控制器
        var loginAnim = CheckToCreateLoginOverrideAnimator();
        //角色动作展示控制器
        var showIdleAnim = CheckToCreateShowIdleAnimator();
        Animator anim = modelObj.GetComponent<Animator>();
        if (anim == null)
        {
            anim = modelObj.AddComponent<Animator>();
        }

        anim.runtimeAnimatorController = animControl;
        List<KeyValuePair<AnimationClip, AnimationClip>> clipList =
            new List<KeyValuePair<AnimationClip, AnimationClip>>();
        animControl.GetOverrides(clipList);
        List<KeyValuePair<AnimationClip, AnimationClip>> loginClipLst =
            new List<KeyValuePair<AnimationClip, AnimationClip>>();
        List<KeyValuePair<AnimationClip, AnimationClip>> showIdleClipLst =
            new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (KeyValuePair<AnimationClip, AnimationClip> clipPair in clipList)
        {
            if (clipPair.Value != null)
            {
                string clipName = clipPair.Value.name;
                foreach (var loopClipName in tModelEditor.loopAnimNameList)
                {
                    if (clipName.Contains(loopClipName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        SerializedObject serializedClip = new SerializedObject(clipPair.Value);
                        SerializedProperty settingProperty = serializedClip.FindProperty("m_AnimationClipSettings");
                        SerializedProperty loopProperty = settingProperty.FindPropertyRelative("m_LoopTime");
                        loopProperty.boolValue = true;
                        serializedClip.ApplyModifiedProperties();
                        break;
                    }
                }
            }
        }

        if (loginAnim != null)
        {
            loginAnim.GetOverrides(loginClipLst);
            foreach (KeyValuePair<AnimationClip, AnimationClip> clipPair in loginClipLst)
            {
                if (clipPair.Value != null)
                {
                    string clipName = clipPair.Value.name;
                    foreach (var loopClipName in tModelEditor.loopAnimNameList)
                    {
                        if (clipName.Contains(loopClipName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            SerializedObject serializedClip = new SerializedObject(clipPair.Value);
                            SerializedProperty settingProperty = serializedClip.FindProperty("m_AnimationClipSettings");
                            SerializedProperty loopProperty = settingProperty.FindPropertyRelative("m_LoopTime");
                            loopProperty.boolValue = true;
                            serializedClip.ApplyModifiedProperties();
                            break;
                        }
                    }
                }
            }
        }
        if (showIdleAnim != null)
        {
            showIdleAnim.GetOverrides(showIdleClipLst);
            foreach (KeyValuePair<AnimationClip, AnimationClip> clipPair in showIdleClipLst)
            {
                if (clipPair.Value != null)
                {
                    string clipName = clipPair.Value.name;
                    foreach (var loopClipName in tModelEditor.loopAnimNameList)
                    {
                        if (clipName.Contains(loopClipName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            SerializedObject serializedClip = new SerializedObject(clipPair.Value);
                            SerializedProperty settingProperty = serializedClip.FindProperty("m_AnimationClipSettings");
                            SerializedProperty loopProperty = settingProperty.FindPropertyRelative("m_LoopTime");
                            loopProperty.boolValue = true;
                            serializedClip.ApplyModifiedProperties();
                            break;
                        }
                    }
                }
            }
        }
        animControl.ApplyOverrides(clipList);
        UpdateModelAction(modelPointJs, clipList, loginClipLst, showIdleClipLst);
        if (isReplace)
        {
            RuntimeAnimatorController animControl =
                AssetDatabase.LoadAssetAtPath(basePath + baseName + ".controller", typeof(RuntimeAnimatorController)) as
                    RuntimeAnimatorController;
            anim.runtimeAnimatorController = animControl;
            return;
        }

        if (!isBaseRole && kind != Kind.FASHION) return;
        animPath = folderPath + name + ".controller";
        AssetDatabase.CreateAsset(animControl, animPath);
    }

    public void AddMat(Material mat)
    {
        string matName = mat.name;
        string[] skinsName = matName.Split('@');
        foreach (string skinName in skinsName)
        {
            string[] sortStr = skinName.Split('#');
            SortInfo info = new SortInfo();
            info.Mat = mat;
            info.CompleteSkinNam = skinName;
            if (sortStr.Length > 1)
            {
                info.SkinName = sortStr[0];
                info.Index = int.Parse(sortStr[1]);
            }
            else
            {
                info.SkinName = skinName;
                info.Index = 0;
            }

            //Debug.Log($"+++++++++++++++mat name={ info.SkinName} =原名{info.CompleteSkinNam}");
            List<SortInfo> infos;
            if (m_dicMats.TryGetValue(info.SkinName, out infos))
            {
                infos.Add(info);
            }
            else
            {
                infos = new List<SortInfo>() { info };
                m_dicMats.Add(info.SkinName, infos);
            }
        }
    }

    public class SortInfo
    {
        public int Index { get; set; }
        public string SkinName { get; set; }
        public Material Mat { get; set; }

        public string CompleteSkinNam { get; set; }
    }

    void ChangeNilAnim(AnimatorOverrideController animControl, string animName, string newAnimName)
    {
        if (kind == Kind.MAN || kind == Kind.WOMAN)
        {
            return;
        }

        if (animClipDic.ContainsKey(animName))
        {
            return;
        }

        if (animClipDic.ContainsKey(newAnimName))
        {
            animControl[animName] = animClipDic[newAnimName];
        }
        else
        {
            if (animClipDic.ContainsKey("wait"))
            {
                animControl[animName] = animClipDic["wait"];
            }
        }
    }

    void CheckNilAnim()
    {
        ChangeNilAnim(animControl, "walk", "wait");
        ChangeNilAnim(animControl, "slow", "walk");
        ChangeNilAnim(animControl, "attack", "wait");
        ChangeNilAnim(animControl, "attack_wait", "wait");
        ChangeNilAnim(animControl, "use_magic", "attack");
        //ChangeNilAnim(animControl, "use_skill1_1", "attack_wait");
        ChangeNilAnim(animControl, "use_skill1_2", "attack_wait");
        ChangeNilAnim(animControl, "use_skill1_3", "attack1_1");
        //ChangeNilAnim(animControl, "use_skill2_1", "attack_wait");
        ChangeNilAnim(animControl, "use_skill2_2", "attack_wait");
        ChangeNilAnim(animControl, "use_skill2_3", "attack1_1");
        //ChangeNilAnim(animControl, "use_skill3_1", "attack_wait");
        ChangeNilAnim(animControl, "use_skill3_2", "attack_wait");
        ChangeNilAnim(animControl, "use_skill3_3", "attack1_1");
        foreach (string i in tModelEditor.overlapWaitAnimName)
        {
            ChangeNilAnim(animControl, i, "wait");
        }
    }

    void CreateFashion()
    {
        CreateBase();
        CreatePrefab(modelObj);
    }

    /// <summary>
    /// 清空已存在的信息
    /// </summary>
    void ClearJs(JSON js)
    {
        JSON roleJson = new JSON();
        js[name] = roleJson;
    }

    void CreateRole()
    {
        LoadCommonMat();
        ClearJs(modelPointJs);
        mainObj = new GameObject(name);
        CreateBase();
        modelObj.name = "tex_Anim";
        modelObj.transform.SetParent(mainObj.transform);
        animEvent = modelObj.AddComponent<AnimEvent>();
        CreatePrefab(mainObj);
    }
    void CreateFarm()
    {
        mainObj = new GameObject(name);
        CreateBase();
        modelObj.name = "tex_Anim";
        modelObj.transform.SetParent(mainObj.transform);
        animEvent = modelObj.AddComponent<AnimEvent>();
        CreatePrefab(mainObj);
    }
    void CreateShowIdle()
    {
        // mainObj = new GameObject(name);
        // CreateBase();
        // modelObj.name = "tex_Anim";
        // modelObj.transform.SetParent(mainObj.transform);
        // animEvent = modelObj.AddComponent<AnimEvent>();
        // CreatePrefab(mainObj);
        CreateBase();
        CreatePrefab(modelObj);
    }

    void CreateAudioObj(Transform parentTra, string objName, AudioClip clip, bool isSetAudioEvent,
        List<AudioSource> souList)
    {
        GameObject audioObj = new GameObject("sou_" + objName);
        audioObj.transform.SetParent(parentTra);
        AudioSource sou = audioObj.AddComponent<AudioSource>();
        sou.clip = clip;
        sou.playOnAwake = false;
        sou.spatialBlend = 1f;
        sou.rolloffMode = AudioRolloffMode.Linear;
        sou.maxDistance = 10f;
        if (isSetAudioEvent)
        {
            string animName = objName.ToLower();
            AnimationClip animClip = animControl[animName];
            if (animClip != null)
            {
                AnimationEvent[] eventArr = new AnimationEvent[1];
                AnimationEvent animEvent = new AnimationEvent();
                animEvent.time = 0;
                animEvent.functionName = "ReceiveSouEvent";
                animEvent.stringParameter = "sou_" + objName;
                eventArr[0] = animEvent;
                AnimationUtility.SetAnimationEvents(animClip, eventArr);
            }
        }

        souList.Add(sou);
    }

    GameObject InitGameObject(GameObject obj, Transform tra)
    {
        GameObject effectObj = MonoBehaviour.Instantiate(obj);
        effectObj.name = obj.name;
        effectObj.transform.SetParent(tra);
        effectObj.transform.localPosition = Vector3.zero;
        effectObj.transform.localRotation = Quaternion.identity;
        return effectObj;
    }

    void CreateTimeline(GameObject obj)
    {
        if (skCamera != null)
        {
            PlayableDirector playableDirector = obj.AddComponent<PlayableDirector>();
            playableDirector.playableAsset = modelTimeline;
            Dictionary<string, PlayableBinding> bindingDict = new Dictionary<string, PlayableBinding>();
            foreach (PlayableBinding pb in playableDirector.playableAsset.outputs)
            {
                if (!bindingDict.ContainsKey(pb.streamName))
                {
                    bindingDict.Add(pb.streamName, pb);
                }
            }

            GameObject skObj = InitGameObject(skCamera, obj.transform);
            InitGameObject(modelFx, obj.transform);
            playableDirector.SetGenericBinding(bindingDict["Animation Track"].sourceObject,
                modelObj.GetComponent<Animator>());
            playableDirector.SetGenericBinding(bindingDict["Animation Track (3)"].sourceObject,
                skObj.GetComponent<Animator>());
        }
    }

    void TryAddModelPrefabAnimator(GameObject modelObj)
    {
        if ((createPath.Contains("ModelHair", StringComparison.CurrentCultureIgnoreCase) ||
             (createPath.Contains("ModelWeapon/Ranger", StringComparison.CurrentCultureIgnoreCase) &&
              !createPath.Contains("Arrow", StringComparison.CurrentCultureIgnoreCase))) &&
            kind == Kind.FASHION)
        {
            Animator anim = modelObj.GetComponent<Animator>();
            if (anim == null)
            {
                modelObj.AddComponent<Animator>();
            }
        }
    }

    public bool isCreatRolePrefabs = false;

    void CreatePrefab(GameObject obj)
    {
        UpdateModelPoint(obj, modelPointJs);
        CreateTimeline(obj);
        string prefabPath = createPath.Replace("Assets/Models/", tModelEditor.PREFAB_PATH);
        TryAddModelPrefabAnimator(obj);
        if (isReplace)
        {
            prefabPath = prefabPath.Replace(baseName + "/" + name + "/", name) + ".prefab";
        }
        else
        {
            prefabPath = prefabPath.Replace(name + "/", name) + ".prefab";
        }
        DynamicBonesEditor.CheckCreateObjDynamicBonesBefore(obj, prefabPath);
        RoleObjectBindEditor.CheckRoleObjBindBefore(prefabPath);
        CheckModelMaterial.CheckRoleModelMaterialByObj(obj, prefabPath);

        RoleObjectBindEditor.CheckRoleObjBindAfter(obj);
        PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
        MonoBehaviour.DestroyImmediate(obj);
        AssetImporter ai = AssetImporter.GetAtPath(prefabPath);
        string abName = prefabPath.Replace(tModelEditor.PREFAB_PATH, "");
        abName = abName.Replace(".prefab", "");
        ai.assetBundleName = abName;
        if (isCreatRolePrefabs)
        {
            AssetDatabase.Refresh();
            return;
        }
        DynamicBonesEditor.CheckCreateObjDynamicBonesAfter(prefabPath);

        AssetImporter importer2 = AssetImporter.GetAtPath(loginAnimPath);
        if (importer2 != null)
        {
            if (kind == Kind.MAN || kind == Kind.WOMAN || kind == Kind.FASHION)
            {
                importer2.assetBundleName = "plane/logingameplane";
            }
        }
        AssetImporter showIdleAnimtorImporter = AssetImporter.GetAtPath(showIdleAnimPath);
        if (showIdleAnimtorImporter != null)
        {
            if (kind == Kind.MAN || kind == Kind.WOMAN || kind == Kind.FASHION)
            {
                showIdleAnimtorImporter.assetBundleName = "modelanimation/showidle";
            }
        }

        AssetImporter importer = AssetImporter.GetAtPath(animPath);
        if (importer == null)
        {
            // Debug.Log("@@@@@@设置动画==> " + animPath);
            return;
        }

        // if (kind == Kind.MAN || kind == Kind.WOMAN || (kind == Kind.FASHION && (animPath.Contains("ModelHair") || animPath.Contains("ModelWeapon/Ranger_F_Bow/"))))
        if (animPath.Contains("ModelWeapon/Ranger_F_Bow/"))
        {
            importer.assetBundleName = "modelshare";
        }
        else if (kind == Kind.MAN || kind == Kind.WOMAN || (kind == Kind.FASHION && animPath.Contains("ModelHair")))
            tModelEditor.CheckToSetRoleAssetIntoModelShareBundle(importer, ".controller", kind);
        else
            importer.assetBundleName = abName;
        // importer.assetBundleName = abName;

        AssetDatabase.Refresh();
    }

    public void UpdateModelPoint(GameObject obj, JSON js)
    {
        JSON roleJson = js.ToJSON(name);
        Transform[] traArr = obj.GetComponentsInChildren<Transform>();
        foreach (Transform tra in traArr)
        {
            if (tModelEditor.pointPathDic.ContainsKey(tra.name))
            {
                string val = tModelEditor.pointPathDic[tra.name];
                roleJson[val] = tModelEditor.GetParentPath(tra.transform);
            }

            if (tModelEditor.skeletonPathDic.ContainsKey(tra.name))
            {
                string val = tModelEditor.skeletonPathDic[tra.name];
                roleJson[val] = tModelEditor.GetParentPath(tra.transform);
            }
        }

        js[name] = roleJson;
    }

    public void UpdateModelAction(JSON js, List<KeyValuePair<AnimationClip, AnimationClip>> clipList,
        List<KeyValuePair<AnimationClip, AnimationClip>> loginClipList, List<KeyValuePair<AnimationClip, AnimationClip>> showIdleClipList)
    {
        JSON roleJson = js.ToJSON(name);
        Debug.Log("@@@@@@@创建成功Name == " + name);
        foreach (KeyValuePair<AnimationClip, AnimationClip> clipPair in clipList)
        {
            if (clipPair.Value != null)
            {
                string clipName = clipPair.Value.name;
                roleJson[clipPair.Key.name] = (int)(clipPair.Value.length * 100) / 100f;
            }
        }

        //Login
        if (loginClipList == null) return;
        foreach (KeyValuePair<AnimationClip, AnimationClip> clipPair in loginClipList)
        {
            if (clipPair.Value != null)
            {
                string clipName = clipPair.Value.name;
                roleJson[clipPair.Key.name] = (int)(clipPair.Value.length * 100) / 100f;
            }
        }
        //ShowIdle
        if (showIdleClipList == null) return;
        foreach (KeyValuePair<AnimationClip, AnimationClip> clipPair in showIdleClipList)
        {
            if (clipPair.Value != null)
            {
                string clipName = clipPair.Value.name;
                roleJson[clipPair.Key.name] = (int)(clipPair.Value.length * 100) / 100f;
            }
        }
    }
}