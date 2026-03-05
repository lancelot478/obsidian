using System.IO;
using SLua;
using UnityEngine;

[CustomLuaClass]
public static class FileTool {
    public static string Path_Combine(params string[] paths) {
        return Path.Combine(paths);
    }

    public static void File_WriteAllBytes(string path, byte[] bytes) {
        File.WriteAllBytes(path, bytes);
    }

    public static bool File_Exists(string path) {
        return File.Exists(path);
    }
    public static bool Directory_Exists(string path) {
        return Directory.Exists(path);
    } 
    public static void Directory_Create(string path) {
        Directory.CreateDirectory(path);
    }
    public static string[] Directory_GetFiles(string path) {
        return Directory.GetFiles(path, "*", SearchOption.AllDirectories);
    }

    public static void Directory_Delete(string path, bool recursive) {
        if (Directory.Exists(path)) {
            Directory.Delete(path, recursive);
        }
    }

    public static bool CropTextureFile(string sourceFile, string destinationFile, string encode, int x, int y, int width, int height) {
        if (!File.Exists(sourceFile)) {
            return false;
        }

        var result = false;
        byte[] fileData = System.IO.File.ReadAllBytes(sourceFile);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        int cropX = x;
        int cropY = y;
        int cropWidth = width;
        int cropHeight = height;
        Texture2D croppedTexture = new Texture2D(cropWidth, cropHeight);
        Color[] pixels = texture.GetPixels(cropX, cropY, cropWidth, cropHeight);
        croppedTexture.SetPixels(pixels);
        croppedTexture.Apply();

        // Save the cropped texture
        byte[] croppedBytes = null;
        if (encode.ToLower() == "jpg") {
            croppedBytes = croppedTexture.EncodeToJPG();
        }else if (encode.ToLower() == "png") {
            croppedBytes = croppedTexture.EncodeToPNG();
        }

        if (croppedBytes != null) {
            System.IO.File.WriteAllBytes(destinationFile, croppedBytes);
            result = true;
        }

        return result;
    }
}
