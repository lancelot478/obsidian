using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FilesTool : Editor
{
    public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool overrideDest = false, List<string> extensionList = null)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }
        else if (overrideDest)
        {
            //            Directory.Delete(destDirName, true);
            //            Directory.CreateDirectory(destDirName);
        }

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string temppath = Path.Combine(destDirName, file.Name);
            if (extensionList != null)
            {
                if (extensionList.Contains(file.Extension))
                {
                    file.CopyTo(temppath, overrideDest);
                }
            }
            else
            {
                file.CopyTo(temppath, overrideDest);
            }

        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, copySubDirs, overrideDest);
            }
        }
    }

    public static void FileCopy(string sourceFileName, string destDirectory, bool overwrite)
    {
        FileInfo fileInfo = new FileInfo(sourceFileName);
        if (fileInfo.Exists && Directory.Exists(destDirectory))
        {
            fileInfo.CopyTo(destDirectory + "/" + fileInfo.Name, overwrite);
        }
    }
}
