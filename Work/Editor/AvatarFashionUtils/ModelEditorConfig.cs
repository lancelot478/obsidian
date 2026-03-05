
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 预制体Create相关配置
/// </summary>
public  struct ModelEditorConfig
{
    //↓↓↓可修改↓↓↓ 所有需要的通用动作名称 不区分职业 
    private static string[] animationNameList =
    {
        "BodyCheck",
        "HeadCheck",
        "walk_ride",
        "walk_ride1",
        "walk_ride2",
        "walk_ride3",
        "walk_ride4",
        "walk_ride5",
        "walk_ride7",
        "showidle_2",
        "showidle_3",
        "showidle_4",
        "showidle_5",
        "showidle_6",
        "showidle_7",
        "showidle_11",
        "showidle_12",
        "showidle_13",
        "showidle_14",
        "showidle_15",
        "showidle_16",
        "showidle_17",
        "showidle_18",
        "showidle_19",
        "showidle_20",
        "showidle_21",
        "showidle_22",
        "showidle_23",
        "showidle_24",
        "showidle_25",
        "showidle_26",
        "showidle_27",
        "showidle_28",
        "showidle_29",
        "showidle_30",
        "showidle_31",
        "showidle_32",
        "showidle_33",
        "showidle_34",
        "showidle_35",
        "showidle_36",
        "showidle_37",
        "showIdle_38",
    };
        
    private static string[] animationShowIdleNameList =
    {
        "BodyCheck",
        "HeadCheck",
        "showidle_2",
        "showidle_3",
        "showidle_4",
        "showidle_5",
        "showidle_6",
        "showidle_7",
        "showidle_11",
        "showidle_12",
        "showidle_13",
        "showidle_14",
        "showidle_15",
        "showidle_16",
        "showidle_17",
        "showidle_18",
        "showidle_19",
        "showidle_20",
        "showidle_21",
        "showidle_22",
        "showidle_23",
        "showidle_24",
        "showidle_25",
        "showidle_26",
        "showidle_27",
        "showidle_28",
        "showidle_29",
        "showidle_30",
        "showidle_31",
        "showidle_32",
        "showidle_33",
        "showidle_34",
        "showidle_35",
        "showidle_36",
        "showidle_37",
        "showIdle_38",
        "walk"
    };

    private static string[] animationShowIdleNamePrefixList =
    {
        "attack_wait",
    };
   public static Dictionary<string, AnimationClip> BaseRoleCommonAnimationClips;

   private static void LoadAssetToDic(AnimatorOverrideController animController, string name,string path)
   {
       var obj =AssetDatabase.LoadAssetAtPath<AnimationClip>(path+name+".anim");
       if( obj!=null)
       {
           animController[name] =
               obj;
       }
   }  
   private static void LoadPrefixAsset(AnimatorOverrideController animController,string path)
   {
       string assetsPath = Application.dataPath;
       path = path.Replace("Assets/", "");
       path = Path.Combine(assetsPath, path);
   

      var filePaths = Directory.GetFiles(path);

       foreach (string filePath in filePaths)
       {
           foreach (var namePrefix in animationShowIdleNamePrefixList)
           {
            
               var name = filePath.Replace(Path.Combine(assetsPath, path), "");
               if (filePath.Contains(namePrefix, StringComparison.CurrentCultureIgnoreCase) && filePath.Contains(".anim") && !filePath.Contains(".anim.meta"))
               {
                   name = Path.Combine("Assets/", name);
                
                   var obj =AssetDatabase.LoadAssetAtPath<AnimationClip>(path+name);
                   if( obj!=null)
                   {
                       animController[name] =
                           obj;
                   }
                   break;
               }
     
           }
       }
   }
   
   /// <summary>
   /// 初始化通用角色动画  坐姿，动作
   /// path路径为:
   /// Assets/Animations/CommonRoleAnim/Male/
   /// Assets/Animations/CommonRoleAnim/FeMale/
   /// </summary>
   /// <param name="animController"></param>
   public static void InitBaseRoleAnimator(AnimatorOverrideController animController,string path,BindAnimationType bindType)
   {
       LoadAssetToDic(bindType, animController, path);
   }

   public static void LoadAssetToDic(BindAnimationType bindType,AnimatorOverrideController animController,string path)
   {
      if (bindType== BindAnimationType.ShowIdle)
      {
          LoadPrefixAsset(animController,path);
          foreach (var name in animationShowIdleNameList)
          {
              LoadAssetToDic(animController, name, path);
          }
      }
      else
      {
          foreach (var name in animationNameList)
          {
              LoadAssetToDic(animController, name, path);
          }
      }
   }
    /// <summary>
    /// 初始化通用时装部位动画(帽子头发）  坐姿 动作
    /// folderPath是对时装右键时的时装路径
    /// 例如Model/Head/xxxxHead
    /// </summary>
    /// <param name="animController"></param>
    /// <param name="folderPath"></param>
   public static void InitBaseFashionCommonAnim(AnimatorOverrideController animController,string folderPath,BindAnimationType bindType)
   {
        //帽子
        if (folderPath.Contains("Head", StringComparison.CurrentCultureIgnoreCase))
        {
            var path = "Assets/Animations/CommonRoleAnim/Head/";
            LoadAssetToDic(bindType, animController, path);
        
        }
        //头发
        else if (folderPath.Contains("Hair", StringComparison.CurrentCultureIgnoreCase))
        {
            var path = "Assets/Animations/CommonRoleAnim/Hair/";
            LoadAssetToDic(bindType, animController, path);
            if(folderPath.Contains("_F", StringComparison.CurrentCultureIgnoreCase))
            {
                path = "Assets/Animations/CommonRoleAnim/Female/Hair/";
                LoadAssetToDic(bindType, animController, path);
               // ModelEditorConfig.InitBaseRoleAnimator(animController,"Assets/Animations/CommonRoleAnim/FeMale/",bindType);
            }
            if(folderPath.Contains("_M", StringComparison.CurrentCultureIgnoreCase))
            {
                path = "Assets/Animations/CommonRoleAnim/Male/Hair/";
                LoadAssetToDic(bindType, animController, path);
                //ModelEditorConfig.InitBaseRoleAnimator(animController,"Assets/Animations/CommonRoleAnim/Male/",bindType);
            }
        }
   }
   
   
}
