#if UNITY_EDITOR && UNITY_ANDROID
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Callbacks;
using PADPost;
public class PadAndroidBuildPostprocess : IPostGenerateGradleAndroidProject
{
    
   
    
    public int callbackOrder
    {
        get { return 9999; }
    }

    public static bool GeneratedAndroidGradle(string projectPath)
    {
        if (!Directory.Exists(projectPath + "/launcher"))
        {
            return true;
        }

        return false;
    }

    public static void AfterAndroidBuild(string outputPath)
    {
        if (Config.IsPad)
        {
            var installPathPackPath = Path.Combine(PADAssetManager.AssetPackPathDic[PADAssetManager.DownLoadType.InstallTime].ToArray());
            var fastFollow1PackPath = Path.Combine(PADAssetManager.AssetPackPathDic[PADAssetManager.DownLoadType.FastFollow1].ToArray());
            var fastFollow2PackPath = Path.Combine(PADAssetManager.AssetPackPathDic[PADAssetManager.DownLoadType.FastFollow2].ToArray());
            var installPath = Path.Combine(outputPath, "install_time_asset_pack/src/main/assets",installPathPackPath);
            var libraryAndroidPath = Path.Combine(outputPath, "unityLibrary/src/main/assets",installPathPackPath);
            
            PADAssetManager.InstallTimePath.ForEach(
                (path) =>
                {
                    PADPostFileHelper.DeleteDirectory(Path.Combine(libraryAndroidPath,path));
                    PADAssetManager.Log("Post Android Build delete base path\n"+Path.Combine(libraryAndroidPath,path));
                }
            );
            
            
            if (!PADAssetManager.NeedFastFollowPack) return;
            PADPostFileHelper.DeleteDirectory(Path.Combine(installPath,fastFollow2PackPath));
            PADAssetManager.EnumeratorFastFollow1(Path.Combine(installPath,fastFollow1PackPath), PADPostFileHelper.DeleteDirectory);
        }
    }
    void IPostGenerateGradleAndroidProject.OnPostGenerateGradleAndroidProject(string projectPath)
    {
        if (!PlatformSDKConfig.IsGooglePlay)
        {
            return;
        }

        Debug.Log("SPLIT_ASSET"+projectPath);
        
        // 拷贝google资源
        if (!BuildPackageEditor.UsingUnitySplitPack)//OutGameConst.isGoogle)
        {
            var install_asset_pack = Path.Combine(Application.dataPath, "..", "_PADCache/install_time_asset_pack/");
            PADPostFileHelper.CopyDirectory(install_asset_pack, Path.Combine(projectPath, "..", "install_time_asset_pack"));
            
            // 清理资源目录
            PADPostFileHelper.DeleteDirectory(Path.Combine(projectPath, "..", "install_time_asset_pack", "src", "main", "assets"));
            // PADPostFileHelper.DeleteDirectory(Path.Combine(projectPath, "..", "fast_follow_asset_pack1", "src", "main", "assets"));
            // PADPostFileHelper.DeleteDirectory(Path.Combine(projectPath, "..", "fast_follow_asset_pack2", "src", "main", "assets"));

            var installPathPackPath = Path.Combine(PADAssetManager.AssetPackPathDic[PADAssetManager.DownLoadType.InstallTime].ToArray());
            var install_time_asset_path = Path.Combine(projectPath, "..","install_time_asset_pack","src","main","assets",installPathPackPath);
            var library_asset_path = Path.Combine(projectPath,"src","main","assets");
            // #if RELEASE
            if (BuildPackageEditor.AddResource)
            {
                PADAssetManager.InstallTimePath.ForEach(
                    (path) =>
                    {
                        PADPostFileHelper.CopyDirectory(Path.Combine(library_asset_path,path), 
                            Path.Combine(install_time_asset_path,path));
                    }
                );
            }
            else
            {
                Directory.CreateDirectory(install_time_asset_path);
            }
            // #endif

            // 修改gradle文件
            var settings_gradle = Path.Combine(projectPath, "../settings.gradle");
            var settings_gradle_content = PADPostFileHelper.LoadConfigFile(settings_gradle).Replace(@"include ':launcher', ':unityLibrary'", @"include ':launcher', ':unityLibrary', ':install_time_asset_pack'");
            
            //修改 launch/build
            var launch_gradle_path = Path.Combine(projectPath, "../launcher/build.gradle");
            var launch_build_content = PADPostFileHelper.LoadConfigFile(launch_gradle_path).Replace("bundle {", "assetPacks = [':install_time_asset_pack'] \n \t bundle {");
            
          
          
            if (PADAssetManager.NeedFastFollowPack)
            {
                settings_gradle_content = settings_gradle_content.Replace(@"include ':launcher', ':unityLibrary', ':install_time_asset_pack'", 
                    @"include ':launcher', ':unityLibrary', ':install_time_asset_pack', ':fast_follow_asset_pack1', ':fast_follow_asset_pack2'");
                
                launch_build_content = launch_build_content.Replace(@"':install_time_asset_pack'", @"':install_time_asset_pack', ':fast_follow_asset_pack1', ':fast_follow_asset_pack2'");
               
                
                var fast_follow_asset_pack1 = Path.Combine(Application.dataPath, "..", "_PADCache/fast_follow_asset_pack1/");
                PADPostFileHelper.CopyDirectory(fast_follow_asset_pack1, Path.Combine(projectPath, "..", "fast_follow_asset_pack1"));
                
                var fast_follow_asset_pack2 = Path.Combine(Application.dataPath, "..", "_PADCache/fast_follow_asset_pack2/");
                PADPostFileHelper.CopyDirectory(fast_follow_asset_pack2, Path.Combine(projectPath, "..", "fast_follow_asset_pack2"));
                
              
                
                if (BuildPackageEditor.AddResource)
                {
                    var fastFollow1AssetPack1PackPath = Path.Combine(PADAssetManager.AssetPackPathDic[PADAssetManager.DownLoadType.FastFollow1].ToArray());
                    var fastFollow2AssetPack2PackPath = Path.Combine(PADAssetManager.AssetPackPathDic[PADAssetManager.DownLoadType.FastFollow2].ToArray());
                    
                    PADAssetManager.EnumeratorFastFollow1(Path.Combine(library_asset_path,fastFollow1AssetPack1PackPath), (path) =>
                    {
                        var dirName = Path.GetFileName(path);
                        PADPostFileHelper.CopyDirectory(path, Path.Combine(projectPath, "..", "fast_follow_asset_pack1", "src", "main", "assets",fastFollow1AssetPack1PackPath,dirName));   

                    });
                    
                    PADPostFileHelper.CopyDirectory(Path.Combine(library_asset_path, fastFollow2AssetPack2PackPath), Path.Combine(projectPath, "..", "fast_follow_asset_pack2", "src", "main", "assets",fastFollow2AssetPack2PackPath));
                }
                else
                {
                    Directory.CreateDirectory(Path.Combine(projectPath, "..", "fast_follow_asset_pack1", "src", "main", "assets"));
                    Directory.CreateDirectory(Path.Combine(projectPath, "..", "fast_follow_asset_pack2", "src", "main", "assets"));
                }
            }
            File.WriteAllText(settings_gradle, settings_gradle_content);
            File.WriteAllText(launch_gradle_path, launch_build_content);
           
            var unity_jar_path = Path.Combine(Application.dataPath, "..", "_PADCache","unityJar/");
            var unity_jar_lib_path =  Path.Combine(projectPath, "src","main","java","com","unity3d","player");
            PADPostFileHelper.CopyDirectory(unity_jar_path, unity_jar_lib_path);
            
            var unity_class_jar_path = Path.Combine(Application.dataPath, "..", "_PADCache","unityClass/");
            var unity_class_jar_lib_path =  Path.Combine(projectPath, "libs");
            PADPostFileHelper.CopyDirectory(unity_class_jar_path, unity_class_jar_lib_path);
            
            Debug.Log("PADAndroidBuildPostProcess Completed");
            // 删除.DStore
            PADPostFileHelper.DeleteDS_Store(Path.Combine(projectPath, ".."));
        }
    }
}
#endif