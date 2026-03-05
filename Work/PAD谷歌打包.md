# Unity使用Google Asset Delivery


[Unity使用Google Asset Delivery - 梁栋 - Confluence](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery)[![Content Report](https://us.viewtracker-static-assets.aws.bitvoodoo.cloud/static/2.3.15/images/viewtracker-16.svg "Content Report")Content Report](https://xindong.atlassian.net/plugins/servlet/ac/ch.bitvoodoo.confluence.plugins.viewtracker/ch.bitvoodoo.confluence.plugins.viewtracker__viewtracker-content-by-line-item?page.id=832286620&space.key=%7Eliangdong.XD&content.id=832286620&content.version=38&page.type=page&page.title=Unity%E4%BD%BF%E7%94%A8Google%20Asset%20Delivery&space.id=261750784&content.type=page&user.isExternalCollaborator=false&page.version=38)

- [Asset Delivery简介](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery#Asset-Delivery%E7%AE%80%E4%BB%8B)
- [Asset Delivery优/缺点](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery#Asset-Delivery%E4%BC%98/%E7%BC%BA%E7%82%B9)
- [Asset Delivery 分包规则](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery#Asset-Delivery-%E5%88%86%E5%8C%85%E8%A7%84%E5%88%99)
- [Asset Delivery 接入方式](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery#Asset-Delivery-%E6%8E%A5%E5%85%A5%E6%96%B9%E5%BC%8F)
    - [Unity插件方式 (不推荐使用)](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery#Unity%E6%8F%92%E4%BB%B6%E6%96%B9%E5%BC%8F-[inlineCard]-(%E4%B8%8D%E6%8E%A8%E8%8D%90%E4%BD%BF%E7%94%A8))
    - [Android Project方式](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery#Android-Project%E6%96%B9%E5%BC%8F)
- [aab包结构解析](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery#aab%E5%8C%85%E7%BB%93%E6%9E%84%E8%A7%A3%E6%9E%90)
- [Asset Delivery 测试](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery#Asset-Delivery-%E6%B5%8B%E8%AF%95)
- [适配Addressable](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery#%E9%80%82%E9%85%8DAddressable)
- [Asset Delivery使用调研](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery#Asset-Delivery%E4%BD%BF%E7%94%A8%E8%B0%83%E7%A0%94)
- [unity使用原生Asset Delivery （demo）](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery#unity%E4%BD%BF%E7%94%A8%E5%8E%9F%E7%94%9FAsset-Delivery-%EF%BC%88demo%EF%BC%89)
- [Asset Delivery避坑小结](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/832286620/Unity+Google+Asset+Delivery#Asset-Delivery%E9%81%BF%E5%9D%91%E5%B0%8F%E7%BB%93)

## Asset Delivery简介

Play Asset Delivery (简称PAD) [Google文档链接](https://developer.android.com/guide/playcore/asset-delivery?hl=zh-cn "https://developer.android.com/guide/playcore/asset-delivery?hl=zh-cn")

1. PAD 提供了灵活的分发模式、自动更新、压缩和增量修补功能，并且可**免费使用**。使用 PAD，所有资源包均在 Google Play 上托管和提供
    
2. 资源分发模式：
    
    1. `install-time` 资源包在用户安装应用时分发。
        
    2. `fast-follow` 资源包会在用户安装应用后立即自动下载
        
    3. `on-demand` 资源包会在应用运行时下载
        
3. 资源大小限制
    
    1. 所有 `install-time` Asset Pack 的总下载大小上限为 **1 GB**
        
    2. 每个 `fast-follow` 和 `on-demand` Asset Pack 的下载大小上限为 **512 MB**
        
    3. 一个 Android App Bundle 中的所有 Asset Pack 的总下载大小上限为 **2 GB**
        
    4. 一个 Android App Bundle 中最多可以使用 50 个资源包
        
4. 纹理压缩格式定位
    
    1. 可让 GPU 使用专用硬件直接从压缩纹理进行渲染，从而减少所需的纹理内存和内存带宽用量
        
5. 应用内触发更新（不需要跳转到Play商店去更新app）
    

**总结：**

- 上传google的aab包不能超过**150M**，如果超过150M还想上传，就**必须使用Play Asset Delivery**
    
- PAD**免费使用**，但是有资源大小限制。随包资源1G，补充资源单个包512M，**总资源大小不能超过2G**
    
- 资源三种分发模式：**安装时分发、快速跟进式分发**和按需分发
    

## Asset Delivery优/缺点

1. **完全免费**，可以白嫖Google的CDN，降低CDN成本
    
2. InstallPack和apk一起下载并安装，支持**后台下载**，用户体验较好，可以避免玩家在等待下载的过程中流失
    

缺点：

1. unity 2021.3以后的版本才支持
    
2. **unity插件版本bug多**，只支持ab包构建，不兼容addressable，测试不方便
    
3. 影响原游戏的**资源下载**和**加载**流程，增加开发和维护成本
    

## Asset Delivery 分包规则

1. **install-time** 资源包<= 1G
    
    1. 商店下载完成后install-time内资源就已经下载完成
        
    2. install-time 无需关心**资源下载**和**资源加载**（资源路径=`Application.streamingAssetsPath`）
        
2. **fast-follow** 1G<资源包<=2G
    
    1. 资源包超过1G的部分，就只能使用fast-follow 分包机制
        
    2. 应用下载完成后，fast-follow包会自动开始下载
        
    3. fast-follow单个包体不能超过512M
        
    4. 如果只是首包使用PAD分发机制，2个FastFollow pack就可以满足
        

## Asset Delivery 接入方式

### Unity插件方式 [![](https://unity.com/themes/contrib/unity_base/images/favicons/favicon.ico)Play 资源交付 - Unity 手册](https://docs.unity3d.com/cn/current/Manual/play-asset-delivery.html) (不推荐使用)

1. 配置资源包中的每个 AssetBundle：
    
    1. 依次选择 **Google > Android App Bundle > Asset Delivery Settings**。
        
    2. 如需选择直接包含 AssetBundle 文件的文件夹，请点击 **Add Folder**。
        
        
        
2. 针对每个 Bundle，将 **Delivery Mode** 更改为 **Install Time**、**Fast Follow** 或 **On Demand**。解决所有错误或依赖项，然后关闭窗口。
    
3. 依次选择 **Google > Build Android App Bundle** 以构建 App Bundle。
    
4. 如果unity版本>2021.3 ，在构建时配置`PlayerSettings.Android.useAPKExpansionFiles = true` ,SteamingAssets下的资源 会以install-time pack的形式构建


### **Android Project**方式

1. 这里用 **install_time** 举例： unity导出Android 工程后，在app同级目录新建一个名为install_time_asset_pack 的Modle目录。类似于创建一个moudle

    
2. 添加build.gradle
    
    `apply plugin: 'com.android.asset-pack' assetPack { packName = "install_time_asset_pack" dynamicDelivery { deliveryType = "install-time" //对应PAD分发模式 "install-time"， "fast-follow" } }`
    
3. 修改settings.gradle
    
    `include ':install_time_asset_pack' //命名要和packName一致`
    
4. 修改launcher/build.gradle,android字段内配置依赖属性
    
    `android { //指定了install-time模式，其他两种模式大同小异 //命名要和packName一致 assetPacks = [":install_time_asset_pack"] }`
    
5. 拷贝资源至“install_time_asset_pack/main/assets/”目录下，注意资源总大小不能超过1G
    
6. PAD依赖项
    
    - com.google.play.assetdelivery
        
    - com.google.android.appbundle
        
    - com.google.play.core
        
    - com.google.play.common
        
    - com.google.play.billing
        
    - com.google.play.review
        
7. java层主要接口（主要用于fastfollow包下载）
    

|   |   |
|---|---|
|**AssetPackManager**|**资源管理类**|
|`getPackStates(List<String> packNames)`|获取资源包的下载信息|
|`fetch(List<String> packNames)`|下载资源包|
|`AssetPackStateUpdatedListener`|监控下载状态|
|`getPackLocation(String packNames)`|获取资源包的下载后的路径|
|`removePack`|删除资源包|
|**AssetPackState**|资源包信息|
|`name`|资源包名称（自定义）|
|`errorCode`|错误码|
|`bytesDownloaded`|已经下载完成的大小|
|`totalBytesToDownload`|总大小|

## aab包结构解析

1. aab包体结构拆解，这里使用了一个**install-time** 和2个**fast-follow**
    
    
    
2. 使用bundletool导出apks,解压后模拟手机安装后的大致结构
    
    `#生成对应手机配置文件 java -jar bundletool-all-1.13.1.jar get-device-spec --output=./device-spec.json #导出apks java -jar bundletool-all-1.13.1.jar build-apks --bundle=FP-0.8.4.42924-release.aab --output=app-debug.apks --overwrite --ks=./android.keystore --ks-pass=pass:**** --ks-key-alias=**** --key-pass=pass:**** --device-spec=./device-spec.json #安装apks java -jar bundletool-all-1.13.1.jar install-apks --apks=./app-debug.apks`
    

    APKS解压后
    
    apks解压后可以看到pad资源包都是以apk形式存放，所以也不能使用File类访问内部资源
    

## Asset Delivery 测试

1. 本地测试
    
    1. 通过命令行导出apks 并重签，（实际操作下来并不能模拟FF包的下载流程）
        
2. 内部分享测试[https://play.google.com/console/internal-app-sharing/](https://play.google.com/console/internal-app-sharing/ "https://play.google.com/console/internal-app-sharing/")
    
    1. 内部分享测试可以完全模拟线上环境，而且无需审核，但是包体不能太大（控制在200M以内）
        
3. 上传google后台并发布beta测试
    
4. FastFollow下载参考流程图[GoogleFastFollow下载流程](https://xindong.atlassian.net/wiki/spaces/~liangdong.XD/pages/633906127)
    
5. google 商店下载流程展示
    

## 适配Addressable

因为PAD资源已经下载完毕，所以addressable只需要适配加载部分

1. 添加并缓存FastFollow**资源列表**和**资源所在目录**
    
    1. FastFollow包内的资源列表需要在构建时生成，游戏启动时加载
        
    2. FastFollow包目录在启动时通过API获取
        
        `UnityEngine.ResourceManagement.ResourceManager ... //FastFollow资源列表 public static List<List<string>> FastFollowAssetList = new List<List<string>>(); //FastFollow加载目录 public static List<string> FastFollowPackPath = new List<string>(); ... /// <summary> /// 检查资源是否在FFPack中 /// </summary> /// <param name="hash"></param> /// <returns></returns> public static bool AndroidCheckFileInFFEB1(string hash) { return FastFollowAssetList != null && FastFollowAssetList.Count > 0 && FastFollowAssetList[0] != null && FastFollowAssetList[0].Contains(hash); } /// <summary> /// 获取FastFollow资源下载目录 /// </summary> /// <returns></returns> public static string GetFastFollow1Path() { if (FastFollowPackPath != null && FastFollowPackPath.Count > 0) return FastFollowPackPath[0]; return null; }`
        
2. AssetBundleProvider.cs类中，修改资源加载方式
    
    1. 修改`AssetBundleResource.LoadType`，添加`FastFollow加载类型`
        
        `internal enum LoadType { None, Local, Web, FastFollow1, FastFollow2 }`
        
    2. 修改`AssetBundleResource.GetLoadInfo` 支持`FastFollow加载判断`
        
        `internal static void GetLoadInfo(){ ... if (Application.platform == RuntimePlatform.Android) { if (ResourceManager.AndroidCheckFileInEB(options.Hash)) loadType = LoadType.StreamingAssets; if (ResourceManager.AndroidCheckFileInFFEB1(options.Hash)) loadType = LoadType.FastFollow1; if (ResourceManager.AndroidCheckFileInFFEB2(options.Hash)) loadType = LoadType.FastFollow2; } ... }`
        
    3. 修改`AssetBundleResource.BeginOperation` 加载`FastFollow`目录中的资源
        
        `private void BeginOperation(){ ... else if (loadType == LoadType.FastFollow1) { var path = GetFastFollowAssetLocalPath(LoadType.FastFollow1, m_TransformedInternalId, m_Options); m_RequestOperation = AssetBundle.LoadFromFileAsync(path, m_Options == null ? 0 : m_Options.Crc, offset); AddCallbackInvokeIfDone(m_RequestOperation, LocalRequestOperationCompleted); } else if (loadType == LoadType.FastFollow2) { var path = GetFastFollowAssetLocalPath(LoadType.FastFollow2, m_TransformedInternalId, m_Options); m_RequestOperation = AssetBundle.LoadFromFileAsync(path, m_Options == null ? 0 : m_Options.Crc, offset); AddCallbackInvokeIfDone(m_RequestOperation, LocalRequestOperationCompleted); } ... }`
        

## Asset Delivery使用调研

1. FlashParty-首包使用**install-time** + **fast-follow***2，超过2G的资源游戏内单独下载。后续热更新依旧用游戏自己逻辑。
    
2. T3-只使用了 **install-time**，原因：**fast-follow**影响原游戏资源下载流程，自带的版本更新机制不够灵活
    
3. 麦芬-只使用了 **install-time**，原因：PAD unity插件版本 BUG太多了，资源包1G以内。
    
4. SSRPG google上是小包，游戏内下载资源
    

## unity使用原生Asset Delivery （demo）

1. C#层下载状态和进度FastFollowDownload.cs
    
2. PAD接口封装GooglePAD.cs AssetPackInfo.java GoogleTools.java
    
3. unity构建完成后gradle模板处理PadAndroidBuildPostprocess.cs
    
4. demo [https://git.xindong.com/liangdong/paddemo](https://git.xindong.com/liangdong/paddemo "https://git.xindong.com/liangdong/paddemo")
    

## Asset Delivery避坑小结

1. 不兼容addressable，构建，下载，加载都需要修改
    
2. unity插件版本资源引用丢失问题，层级过多时需要解决依赖报错
    
3. unity插件版本只支持asset bundle
    
4. 对unity版本有限制，对gradle版本有最低要求
    
    1. unity 2021.3之后的版本默认支持PAD，低于此版本需要使用[补丁版本](https://docs.unity3d.com/Manual/play-asset-delivery.html "https://docs.unity3d.com/Manual/play-asset-delivery.html")
        
    2. gradle插件要升级到4.0.1以后，gradle 版本6.9.3
        
5. 拷贝资源时需要删除`.DStore`文件，所有的pack内不能有重名文件。上传google后台时会有文件重名检查。
    
6. 资源下载时，文件是一个个按顺序下载，所以在更新下载速度时建议2s一次，避免速度不平滑