using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SLua;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class LuaScriptsProcessEditor {
    private const string luaExtension = "lua";
    private const string txtExtension = "txt";
    
    private static List<string> luaRequireFilters = new List<string>() {
        "AllRequires.lua", "AllBaseRequires.lua", "Global/GlobalType.lua",
        "GameEntry.lua", "GlobalMain.lua", "ServerMain.lua", "GameMain.lua",
        "Lib/__oop.lua", "Lib/_base64.lua", "Lib/_bit.lua", "Lib/_dkjson.lua", "Lib/_vec3.lua", "Lib/_vec2.lua", "Lib/sha1/", "Lib/pb/", "Lib/structure/", "Lib/ui/",
        "_test/",
        "Tools/PlayerPrefs/",
        "Lib/debug",
        "Plane/BattlePlane/BattleMap/Data/Create/Create_BattleMapTimelineType.lua"
    };

    private static List<string> configTableLS = new List<string>() {
        "Data/ConfigTableTW/",
        "Data/ConfigTableCN/",
        "Data/ConfigTableJP/",
        "Data/ConfigTableKR/",
        "Data/ConfigTableSEA/",
        "Data/ConfigTableEU/",
        "Data/ConfigTableNA/",
    };

    static LuaScriptsProcessEditor() {
        luaRequireFilters.AddRange(configTableLS);
    }

    private const string LUA_SCRIPTS_PATH = "Scripts/LuaScript";
    private const string LUA_SCRIPTS_TXT_PATH = "Scripts/LuaScript_Txt";

    [MenuItem("Assets/ProcessAssets/GenerateLuaRequires")]
    static void GenerateLuaRequires() {
        AssetDatabase.Refresh();
        string       requireFilePath = Application.dataPath + "/Scripts/LuaScript/AllRequires.lua";
        StreamWriter writer          = File.CreateText(requireFilePath);
        string luaPath = PathExtension.NormalizePathSeparatorChar(new DirectoryInfo(Application.dataPath + "/Scripts/LuaScript/").FullName);
        string[]     luafiles     = Directory.GetFiles(luaPath, "*." + luaExtension, SearchOption.AllDirectories);
        List<string> luafilesList = luafiles.ToList();
        luafilesList.Sort((s1, s2) => {
            var file1 = new FileInfo(s1);
            var file2 = new FileInfo(s2);
            if (file1.Name.Equals(file2.Name)) {
                var directoryInfo1 = file1.Directory;
                var directoryInfo2 = file2.Directory;
                while (directoryInfo2 != null && directoryInfo1 != null 
                    && directoryInfo1.Name.Equals(directoryInfo2.Name)) {
                    directoryInfo1 = directoryInfo1.Parent;
                    directoryInfo2 = directoryInfo2.Parent;
                }
                
                return String.Compare(directoryInfo1?.Name, directoryInfo2?.Name, StringComparison.InvariantCultureIgnoreCase);
            }
            else {
                string name1 = file1.Name;
                string name2 = file2.Name;
                return String.Compare(name1, name2, StringComparison.InvariantCultureIgnoreCase);
            }
        });
        luafiles = luafilesList.ToArray();
        foreach (string asset in luafiles) {
            string path    = PathExtension.NormalizePathSeparatorChar(asset);
            bool   needGen = true;
            foreach (string filter in luaRequireFilters) {
                if (path.Contains(filter)) {
                    needGen = false;
                    break;
                }
            }

            if (path.StartsWith("Views/"))
            {
                needGen = false;
            }

            if (path.EndsWith("." + luaExtension) && needGen) {
                string requireName = PathExtension.NormalizePathSeparatorChar(Path.GetRelativePath(luaPath, path)).Replace("." + luaExtension, "");
                if (requireName.StartsWith("Views/")|| requireName.StartsWith("Tools/UIView/"))
                {
                    continue;
                }
                writer.WriteLine("require \"" + requireName + "\"");
                //                Debug.Log(requireName);
            }
        }

        writer.Close();
        AssetDatabase.Refresh();

        // GenerateLuaScriptTxtFlush();
    }

    // [MenuItem("Assets/ProcessAssets/GenerateLuaTxt")]
    public static void GenerateLuaScriptTxtFlush() {
        GenerateLuaScriptTxt(true);
    }
    
    [MenuItem("Assets/ProcessAssets/LuaTxt/GenerateLuaTxtNoFlush")]
    public static void GenerateLuaScriptTxtNoFlush() {
        GenerateLuaScriptTxt(false);
    }

    [MenuItem("Assets/ProcessAssets/LuaTxt/RemoveLuaTxt")]
    public static void RemoveGeneratedLuaScriptTxt() {
        var luaScriptTxtPath = Path.Combine(Application.dataPath, LUA_SCRIPTS_TXT_PATH);
        luaScriptTxtPath = PathExtension.NormalizePathSeparatorChar(luaScriptTxtPath);

        var luaScriptTxtDirectory = new DirectoryInfo(luaScriptTxtPath);
        if (luaScriptTxtDirectory.Exists) {
            FileInfo[] files = luaScriptTxtDirectory.GetFiles("*." + txtExtension, SearchOption.AllDirectories);
            foreach (FileInfo fileInfo in files) {
                if (fileInfo.Exists) {
                    fileInfo.Delete();
                }
            }
        }
        
        AssetDatabase.Refresh();
    }

    public static void DeleteOtherRegionConfigTable(List<string> preserved) {
        foreach (var configName in configTableLS) {
            var needDelete = true;
            foreach (var preservedName in preserved) {
                if (!string.IsNullOrEmpty(preservedName) && configName.EndsWith(preservedName + "/")) {
                    needDelete = false;
                    break;
                }
            }

            if (needDelete) {
                var path = Path.Combine(Application.dataPath, LUA_SCRIPTS_PATH, configName);
                if (Directory.Exists(path)) {
                    Directory.Delete(path, true);
                }
            }
        }
        
        AssetDatabase.Refresh();
    }
    
    public static void GenerateLuaScriptTxt(bool flushFile = true) {
        Config.LoadRegionInEditor();
        var currentRegionName = Config.GetRegionName();
        if (Config.IsVI())
        {
            currentRegionName = Config.GL;
        }
        DeleteOtherRegionConfigTable(new List<string> { currentRegionName });
        
        var luaScriptPath = Path.Combine(Application.dataPath, LUA_SCRIPTS_PATH);
        var luaScriptTxtPath = Path.Combine(Application.dataPath, LUA_SCRIPTS_TXT_PATH);

        luaScriptPath = PathExtension.NormalizePathSeparatorChar(luaScriptPath);
        luaScriptTxtPath = PathExtension.NormalizePathSeparatorChar(luaScriptTxtPath);

        var luaScriptTxtDirectory = new DirectoryInfo(luaScriptTxtPath);
        if (!luaScriptTxtDirectory.Exists) {
            luaScriptTxtDirectory.Create();
        }

        var luaFiles = Directory.GetFiles(luaScriptPath, "*." + luaExtension, SearchOption.AllDirectories);
        var luaTxtFiles = Directory.GetFiles(luaScriptTxtPath, "*." + txtExtension, SearchOption.AllDirectories);
        foreach (var luaTxtFile in luaTxtFiles) {
            var fi = new FileInfo(luaTxtFile);
            if (fi.Exists) {
                fi.Delete();
            }
        }

        var copyToDirectory = new DirectoryInfo(luaScriptTxtPath);
        
        foreach (var file in luaFiles) {
            var oldFileInfo = new FileInfo(file);
            var oldFilePath = oldFileInfo.FullName;
            oldFilePath = PathExtension.NormalizePathSeparatorChar(oldFilePath);

            var newFileName = PathExtension.NormalizePathSeparatorChar(Path.GetRelativePath(luaScriptPath, 
                    (oldFilePath.Substring(0, oldFilePath.Length - luaExtension.Length) + txtExtension)))
                .Replace("/", ".");

            var newFileInfo = new FileInfo(Path.Combine(copyToDirectory.FullName, newFileName));
         
            // Debug.Log($"Coping form {oldFilePath} to {newFileInfo.FullName}");
            File.Copy(oldFilePath, newFileInfo.FullName, true);

            if (flushFile) {
                File.WriteAllText(newFileInfo.FullName, string.Empty);
            }
        }
        
        AssetDatabase.Refresh();
    }

    // [MenuItem("Assets/ProcessAssets/RenameLuaScripts")]
    public static void RenameLuaScripts() {
        string   pathLS     = new DirectoryInfo(Application.dataPath + "/Scripts/LuaScript/").FullName;
        string preExtension = "txt";
        string[] allLSFiles = Directory.GetFiles(pathLS, "*." + preExtension, SearchOption.AllDirectories);
        foreach (string lsFile in allLSFiles) {
            FileInfo fi = new FileInfo(lsFile);
            FileInfo newFi = new FileInfo(fi.FullName.Substring(0, fi.FullName.Length - preExtension.Length) + luaExtension);
            File.Move(fi.FullName, newFi.FullName);
        }
        AssetDatabase.Refresh();
    }

    public static void PrePackLS() {
        // string       pathLS        = new DirectoryInfo(Application.dataPath + "/Scripts/LuaScript/").FullName;
        // string[]     allLSFiles    = Directory.GetFiles(pathLS, "*." + luaExtension, SearchOption.AllDirectories);
        // List<string> newAllLSFiles = new List<string> { };
        // foreach (string lsFile in allLSFiles) {
        //     newAllLSFiles.Add(lsFile.Replace("\\", "/"));
        // }
        //
        // foreach (string file in newAllLSFiles) {
        //     string newFile = file.Replace(file.Substring(file.LastIndexOf("/", StringComparison.Ordinal)), "") + "/" +
        //                      file.Replace(Application.dataPath + "/Scripts/LuaScript/", "").Replace("/", ".");
        //     newFile = newFile.Substring(0, newFile.Length - luaExtension.Length) + "txt";
        //     File.Copy(file, newFile, true);
        // }
        //
        // AssetDatabase.Refresh();
    }

    public static void PostPackLS() {
        // string   pathLS     = new DirectoryInfo(Application.dataPath + "/Scripts/LuaScript/").FullName;
        // string[] allLSFiles = Directory.GetFiles(pathLS, "*.txt", SearchOption.AllDirectories);
        // foreach (string lsFile in allLSFiles) {
        //     File.Delete(lsFile);
        // }
        //
        // AssetDatabase.Refresh();
    }

    // [MenuItem("Assets/ProcessAssets/MergeLS")]
    public static void MergeLS() {
        // LuaState l = new LuaState();
        //
        // LuaSvr.doBindForPublic(l.L);
        //
        // l.openSluaLib();
        // l.openExtLib();
        // Lua3rdDLL.open(l.L);
        //
        // l.loaderDelegate = (string fn, ref string absoluteFn) => {
        //     string path = Application.dataPath + "/Scripts/LuaScript/" + fn + ".lua";
        //     return File.ReadAllBytes(path);
        // };
        // l.doString("require('Global/GlobalType')");
        // l.bindUnity();
        // l.doString(@"
        //     for k, v in pairs(_G) do
        //         print('---->>', k, type(v), v)
        //     end
        // ");
        // l.Dispose();
        // LuaSvr svr = new LuaSvr();

        // byte[] loader(string fn, ref string absoluteFn) {
        //     string path = Application.dataPath + "/Scripts/LuaScript/" + fn + ".lua";
        //     return File.ReadAllBytes(path);
        // }
        //
        // void complete() {
        //
        //         LuaState l        = new LuaState();
        //         l.loaderDelegate = loader;
        //         // l.Name           = require;
        //
        //         // string fileName = new DirectoryInfo(Application.dataPath + "/Scripts/LuaScript/" + require.Substring() + ".lua").FullName;
        //
        //         l.openSluaLib();
        //         l.openExtLib();
        //         Lua3rdDLL.open(l.L);
        //         l.bindUnity();
        //         
        //         l.doString("require('AllBaseRequires')");
        //         l.doString("require('Global/GlobalType')");
        //         
        //         LuaTable _G = l.getTable("_G");
        //         List<string> commonGlobal = new List<string>();
        //         foreach (LuaTable.TablePair tablePair in _G) {
        //             commonGlobal.Add(tablePair.key.ToString());
        //         }
        //
        //         foreach (string require in requires) {
        //             if (string.IsNullOrEmpty(require)) {
        //                 continue;
        //             }
        //
        //             l.doString(require);
        //
        //             StringBuilder sb = new StringBuilder();
        //             sb.Append(require);
        //             sb.AppendLine();
        //             foreach (LuaTable.TablePair tablePair in _G) {
        //                 if (!commonGlobal.Contains(tablePair.key.ToString())) {
        //                     sb.Append(tablePair.key);
        //                     sb.Append(", ");
        //                     sb.Append(tablePair.value);
        //                     sb.AppendLine();
        //                 }
        //             }
        //
        //             Debug.Log(sb.ToString());
        //             // l.Dispose();
        //         }
        // }
        //
        // svr.init(null, complete);

        string pathAllRequires =
            new DirectoryInfo(Application.dataPath + "/Scripts/LuaScript/AllRequires.lua").FullName;
        string[] requires = File.ReadAllLines(pathAllRequires);

        const string insertText = "local _ENV = Boli2Env";

        foreach (string require in requires) {
            string fileName = require.Replace("require \"", "").Replace("\"", "");
            string scriptFilePath =
                new DirectoryInfo(Application.dataPath + "/Scripts/LuaScript/" + fileName + ".lua").FullName;
            string currentScriptText = File.ReadAllText(scriptFilePath);
            if (!currentScriptText.StartsWith(insertText)) {
                File.WriteAllText(scriptFilePath, currentScriptText.Insert(0, insertText + "\n\n"));
            }
        }

        AssetDatabase.Refresh();
    }
}