```note-imp
1.AS本地打包，需要删掉之前的本地包才能让改动代码生效
```

  >| 变量        | 值                                                       |
| ----------- | -------------------------------------------------------- |
| Gradle_HOME | %GRADLE_HOME%\bin\                                       |
| JAVA_HOME   | C:\Program Files\Java\jre-1.8                            |
| oracle      | C:\Program Files (x86)\Common Files\Oracle\Java\javapath |
|             |                /Users/gexianglin/self/gradle-8.1.1/bin/gradle assembleRelease                                          |

```
Xcode SDK Path
/Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/‌​SDKs/
PersistedPath
/storage/emulated/0/Android/data/com.xd.muffin.tw/files/
C:\Users\45068\AppData\LocalLow\X_D Network Inc_\出发吧麦芬\DownLoadImageDirectory
Android Studio SDK Path
C:\Users\45068\AppData\Local\Android\Sdk

C:\Program Files\Unity\Hub\Editor\2021.3.13f1-x86_64\Editor\Data\PlaybackEngines\AndroidPlayer\SDK

```
>打包指令
>gradle bundleRelease
>gradle assembleRelease
>gradle assembleDebug *BuildOptions.Development*
>


```ad-tip 
title:API升级到33
Target API LEVEL => Automatic
using Andorid Studio API33 SDK
DONT use unity sdk
```

>[!Google加密]
[Android Google Play app signing 最终完美解决方式 - 赵彦军 - 博客园](https://www.cnblogs.com/zhaoyanjun/p/12715125.html)
java 
-jar C:\Users\45068\Documents\google-secret\pepk.jar --keystore=C:\Users\45068\Documents\google-secret\SAGA --alias=saga --output=output.zip --include-cert --rsa-aes-encryption --encryption-key-path=C:\Users\45068\Documents\google-secret\encryption_public_key.pem
>![[企业微信截图_17017652735161.png]]

[[Unity Native 崩溃]]

[Unity - Manual:  Deep linking on iOS](https://docs.unity3d.com/2022.2/Documentation/Manual/deep-linking-ios.html)
[adb配置完环境变量后仍然不能用，\* daemon not running; starting now at tcp:5037 adb: CreateProcessW failed问题解决\_adb: createprocessw failed: 拒绝访问。 (5) \* failed to -CSDN博客](https://blog.csdn.net/zyn5211314zyn/article/details/95306711)

```
What went wrong:

Execution failed for task ':launcher: bundleReleaseResources'.

- ﻿﻿A failure occurred while executing com.android.build.gradle. internal. res.Aapt2ProcessResourcesRunnable
- ﻿﻿Android resource linking failed  
    ERROR:/Users/muffin/-gradle/caches/transforms-3/8d244db76bcb5ae93eec2bfac4befb91/transformed/core-1.9.0/res/values/values.ml:104:5-113:25: AAPT: error:

resource android:attr/Star not found.

targetSDKVersion 修改为 34
ProjectSettings.asset => AndroidTargetSdkVersion: 34
```

```
Upgrade's application-identifier entitlement string (NTC4BJ542G.com.xd.muffin.ios.kr) does not match installed application's application-identifier string (UE5H8B62F9.com.xd.muffin.ios.kr); rejecting upgrade.

Domain: MIInstallerErrorDomain


可以尝试删掉旧包下新包
```

```
adb offline问题：
Users/Local/Android adb
unity adb
模拟器 adb
用同一个版本

模拟器开发者模式，root,usb调试
adb kill-server
adb connect 127.0.0.1:62001(nox)
adb connect 127.0.0.1:16384(nox)
```


```
DllNotFoundException: diskutils assembly:<unknown assembly> type:<unknown type> member:(null)

可以删掉 Library
```

```
夜神 nox 模拟器
gralloc_alloc: Creating ashmem region of size 5816320
nox gc会导致游戏 闪退
nox android 9 版本升级可以解决

```

