```
ZIPALIGN_EXEC=/Users/muffin/Library/Android/sdk/build-tools/35.0.0/zipalign
APKSIGNER_EXEC=/Users/muffin/Library/Android/sdk/build-tools/35.0.0/apksigner
JAVA_11_HOME=/Users/muffin/Development/Jenkins/jdk-17.jdk/Contents/Home


GRADLE_EXEC=/Users/muffin/Development/Jenkins/gradle-8.10.2/bin/gradle
GRADLE_VERSION=8.8.2

GRADLE_EXEC=/Users/muffin/Development/Jenkins/gradle-8.1.1/bin/gradle
GRADLE_VERSION=8.0


ZIPALIGN_EXEC=/Users/muffin/Library/Android/sdk/build-tools/33.0.2/zipalign
APKSIGNER_EXEC=/Users/muffin/Library/Android/sdk/build-tools/33.0.2/apksigner
JAVA_11_HOME=/Users/muffin/Development/Jenkins/jdk-17.jdk/Contents/Home


JAVA_11_HOME=/Users/muffin/Development/Jenkins/jdk-11.0.2.jdk/Contents/Home
GRADLE_EXEC=/Users/muffin/Development/Jenkins/gradle-7.2/bin/gradle
GRADLE_VERSION=7.2

ndkVersion 21.3.6528147

android.aapt2FromMavenOverride=/Users/muffin/Library/Android/sdk/build-tools/35.0.0/aapt2
```

```
[CXX1104] NDK from ndk.dir at /Applications/Unity/Hub/Editor/2021.3.13f1/PlaybackEngines/AndroidPlayer/NDK had version [21.3.6528147] which disagrees with android.ndkVersion [27.0.12077973]
```

<font color="#ffc000">升级Gradle流程</font>
```

brew install openjdk@17
brew pin python@3.12
/opt/homebrew/Cellar/openjdk@17

[Gradle | Releases](https://gradle.org/releases/)
[Java Archive Downloads - Java SE 17.0.12 and earlier](https://www.oracle.com/java/technologies/javase/jdk17-archive-downloads.html)

/Library/Java/JavaVirtualMachines/

/Applications/Unity/Hub/Editor/2021.3.13f1/PlaybackEngines/AndroidPlayer/SDK/cmdline-tools/2.1/bin/sdkmanager "platforms;android-35"
/Applications/Unity/Hub/Editor/2021.3.13f1/PlaybackEngines/AndroidPlayer/SDK/cmdline-tools/2.1/bin/sdkmanager "build-tools;35.0.0"

/Applications/Unity/Hub/Editor/2021.3.13f1-arm64/PlaybackEngines/AndroidPlayer/SDK/cmdline-tools/2.1/bin/sdkmanager
[Releases · android/ndk · GitHub](https://github.com/android/ndk/releases)

This tool requires JDK 17 or later. Your version was detected as 1.8.0-adoptopenjdk.

To override this check, set SKIP_JDK_VERSION_CHECK.

curl -O https://dl.google.com/android/repository/commandlinetools-mac-6858069_latest.zip

```



```
 adb install -r /Users/gexianglin/aaboli/boli_branch/build_android/launcher/build/outputs/apk/debug/launcher-debug-3.0-1.apk

adb install -r 
/Users/gexianglin/aaboli/boli_branch/build_android/launcher/build/outputs/apk/release/launcher-release-3.0-1.apk

adb shell am start -n com.xd.muffin.dl.global/com.xd.muffin.MuffinUnityPlayerActivity

2021 unity 使用 android sdk 34报错
l: One of RECEIVER_EXPORTED or RECEIVER_NOT_EXPORTED should be specified when a receiver isn't being registered exclusively for system broadcasts
```


```
/Applications/Unity/Hub/Editor/2021.3.13f1-arm64/PlaybackEngines/AndroidPlayer/SDK/build-tools/35.0.0

```

本地打包流程
```本地打包流程
/Users/muffin/Development/Jenkins/_SharedWorkspace/2c53d772c3d55c596ab20d4fa3e63b1c/Unity2021-3-13/gl_obt_release/Package/Android/Project

cd build_android

export JAVA_HOME=/Users/muffin/Development/Jenkins/jdk-17.jdk/Contents/Home
/Users/muffin/Development/Jenkins/gradle-8.10.2/bin/gradle wrapper --gradle-version 8.10.2

mac本地打包
exexport JAVA_HOME=/opt/homebrew/Cellar/openjdk@17/17.0.18/libexec/openjdk.jdk/Contents/Home
/Users/gexianglin/self/gradle-8.10.2/bin/gradle wrapper --gradle-version 8.10.2
/Users/gexianglin/self/gradle-8.10.2/bin/gradle assembleRelease

adb install -r /Users/gexianglin/aaboli/gl-design/build_android/launcher/build/outputs/apk/release/launcher-release-3.0-1.apk

cd launcher
../gradlew clean

../gradlew assembleRelease -POUTPUT_APK_PATH=/Users/muffin/Development/Jenkins/_SharedWorkspace/2c53d772c3d55c596ab20d4fa3e63b1c/Unity2021-3-13/gl_obt_release/Package/Android/Project/output_android_TapTap


/Users/muffin/Library/Android/sdk/build-tools/35.0.0/zipalign  -p -v 16 /Users/muffin/Development/Jenkins/_SharedWorkspace/2c53d772c3d55c596ab20d4fa3e63b1c/Unity2021-3-13/gl_obt_release/Package/Android/Project/output_android_TapTap/launcher-release.aab /Users/muffin/Development/Jenkins/_SharedWorkspace/2c53d772c3d55c596ab20d4fa3e63b1c/Unity2021-3-13/gl_obt_release/Package/Android/Project/output_android_TapTap/launcher-release.apk
```





