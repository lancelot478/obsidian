
import sys
import os
import _thread
from unityparser import UnityDocument

projectPath = "./.."

def modify(meta_file):
    doc = UnityDocument.load_yaml(meta_file)
    for entry in doc.entries:
        if "TextureImporter" in entry:
            textureImporter = entry['TextureImporter']
            
            if "platformSettings" in textureImporter:
                platformSettings = textureImporter["platformSettings"]

                iOSHaveExisting = False
                androidHaveExisting = False

                iOSModifyPlatformSetting = dict()
                iOSModifyPlatformSetting["buildTarget"] = "iPhone"
                iOSModifyPlatformSetting["maxTextureSize"] = 2048
                iOSModifyPlatformSetting["resizeAlgorithm"] = 0
                iOSModifyPlatformSetting["textureFormat"] = 50
                iOSModifyPlatformSetting["textureCompression"] = 1
                iOSModifyPlatformSetting["compressionQuality"] = 50 
                iOSModifyPlatformSetting["crunchedCompression"] = 0
                iOSModifyPlatformSetting["allowsAlphaSplitting"] = 0
                iOSModifyPlatformSetting["overridden"] = 1
                iOSModifyPlatformSetting["androidETC2FallbackOverride"] = 0
                iOSModifyPlatformSetting["forceMaximumCompressionQuality_BC6H_BC7"] = 0

                androidModifyPlatformSetting = dict()
                androidModifyPlatformSetting["buildTarget"] = "Android"
                androidModifyPlatformSetting["maxTextureSize"] = 2048
                androidModifyPlatformSetting["resizeAlgorithm"] = 0
                androidModifyPlatformSetting["textureFormat"] = 50
                androidModifyPlatformSetting["textureCompression"] = 1
                androidModifyPlatformSetting["compressionQuality"] = 50
                androidModifyPlatformSetting["crunchedCompression"] = 0
                androidModifyPlatformSetting["allowsAlphaSplitting"] = 0
                androidModifyPlatformSetting["overridden"] = 1
                androidModifyPlatformSetting["androidETC2FallbackOverride"] = 0
                androidModifyPlatformSetting["forceMaximumCompressionQuality_BC6H_BC7"] = 0

                for platformSetting in platformSettings:
                    if platformSetting["buildTarget"] == "iPhone":
                        iOSHaveExisting = True
                        platformSetting["maxTextureSize"] = 2048
                        platformSetting["textureFormat"] = 50
                        iOSModifyPlatformSetting["compressionQuality"] = 50
                        platformSetting["overridden"] = 1

                    if platformSetting["buildTarget"] == "Android":
                        iOSHaveExisting = True
                        platformSetting["maxTextureSize"] = 2048
                        platformSetting["textureFormat"] = 50
                        iOSModifyPlatformSetting["compressionQuality"] = 50
                        platformSetting["overridden"] = 1

                if iOSHaveExisting == False:
                    platformSettings.append(iOSModifyPlatformSetting)

                if androidHaveExisting == False:
                    platformSettings.append(androidModifyPlatformSetting)

                doc.dump_yaml()

g = os.walk(projectPath)  
for path,dir_list,file_list in g:  
    for file_name in file_list:
        if file_name.find(".meta") > 0:
            meta_file = os.path.join(path, file_name)
            # _thread.start_new_thread(modify, (meta_file))
            try:
                modify(meta_file)
            except:
                print("Unexpected error:", sys.exc_info()[0])
                continue
            else:
                continue

