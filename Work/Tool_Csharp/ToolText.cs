using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using UnityEngine.Networking;
using SLua;

[CustomLuaClassAttribute]
public class ToolText
{
	public static Dictionary<RuntimePlatform, string> platformMd5ConfigNameDic = new Dictionary<RuntimePlatform, string>(){
		{RuntimePlatform.IPhonePlayer, "IOS"}, {RuntimePlatform.Android, "ANDROID"}
	};
	const int ENCRYPT_LENGTH = -1;
	const byte ENCRYPT_FEILD = 255;
	const int ENCRYPT_NUM = 30;

	public static float GetMusicVolume()
	{
		return PlayerPrefs.GetFloat("Music", 0.8f);
	}
	public static float GetSoundVolume()
	{
		return PlayerPrefs.GetFloat("Sound", 0.8f);
	}

	public static void SetEncryptOrDecrypt(byte[] bytes)
	{
		int len = bytes.Length;
		int finNum = ENCRYPT_NUM;
		if(len < finNum)
		{
			finNum = len;
		}
		int addition = len / finNum;
		for(int i = 0; i < finNum; i++)
		{
			int index = addition * i;
			bytes[index] ^= ENCRYPT_FEILD;
		}
	}

	public static string GetDataPath(string objName) {
		string platform = "";
		string dataPath = "";
		string abPath = "";
		string protoPath = "";
#if UNITY_IPHONE
		platform = "IOS";
#endif
#if UNITY_ANDROID
		platform = "ANDROID";
#endif
#if UNITY_EDITOR
		protoPath = GetProtocolPath(true);
		dataPath = Application.dataPath + "/../_AssetsBundles";
		abPath = Path.Combine(Path.Combine(dataPath, platform), objName);
#else
		dataPath = Application.persistentDataPath;
		abPath = Path.Combine(Path.Combine(dataPath, platform), objName);
		protoPath = GetProtocolPath(true);
		if (!File.Exists(abPath)) {
			protoPath = GetProtocolPath(false);
			dataPath = Application.streamingAssetsPath;
			abPath = Path.Combine(Path.Combine(dataPath, platform), objName);
		}
#endif
		
//		string platform = GetPlatformPath(true);
//		string dataPath = Application.persistentDataPath + platform + objName;
//		string protocolPath = GetProtocolPath(true);
//		if(!File.Exists(dataPath))
//		{
//			platform = GetPlatformPath(false);
//			dataPath = Application.dataPath +  "/../_AssetsBundles" + platform + objName;
//			protocolPath = GetProtocolPath(false);
//		}
//		return protocolPath + dataPath;
		return protoPath + abPath;
	}
	public static void EncryptFileData(string path)
	{
		using(FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
		{
			byte[] bytes = new byte[fs.Length];
			fs.Read(bytes, 0, (int)fs.Length);
			SetEncryptOrDecrypt(bytes);
			fs.Close();
			File.Delete(path);
			using(FileStream newFS = new FileStream(path, FileMode.Create))
			{
				newFS.Write(bytes, 0, bytes.Length);
				newFS.Close();
			}
		}
	}
	public static string GetPlatformPath(bool isCache)
	{
		string platform = "/";
		if(Application.isEditor)
		{
			if(!isCache)
			{
				#if UNITY_ANDROID
//				platform = "/Plugins/Android/assets/android/";
				platform = "/ANDROID/";
				#elif UNITY_IPHONE
				platform = "/IOS/";
				#endif
			}
		}
		else
		{
			if(!isCache)
			{
				#if UNITY_ANDROID
//				platform = "!/assets/android/";
				platform = "/../ANDROID/";
				#elif UNITY_IPHONE
				platform = "/../IOS/";
				#endif
			}
		}
		return platform;
	}
	public static string GetProtocolPath(bool isCache)
	{
		string protocolPath = "file://";
		if(!isCache && Application.platform == RuntimePlatform.Android)
		{
			protocolPath = "";
		}
		return protocolPath;
	}

	public static int GetTimeStamp() {
		return GetTimeStampByDateTime(System.DateTime.UtcNow);
	}

	public static int GetTimeStampByDateTime(DateTime dt) {
		var epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
		var timeStamp = (dt - epochStart).TotalSeconds;
		return Convert.ToInt32(timeStamp);
	}

	public static string SHA1Encrypt(string sign)
	{
		SHA1 sha = new SHA1CryptoServiceProvider();
		byte[] dataToHash = Encoding.ASCII.GetBytes(sign);
		byte[] dataHashed = sha.ComputeHash(dataToHash);
		return BitConverter.ToString(dataHashed).Replace("-" , "").ToLower();
	}
	
	public static string GetMD5HashFromFile(string fileName)
	{
		using (var md5 = MD5.Create())
		{
			using (var stream = File.OpenRead(fileName))
			{
				return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLower();
			}
		}
	}
	
	public static string GetMD5Hash(string str)
	{
		return GetMD5Hash(Encoding.UTF8.GetBytes(str));
	}
	public static string GetMD5Hash(byte[] fileBytes)
	{
		try
		{
			MD5 md5 = new MD5CryptoServiceProvider();
			byte[] bytes = md5.ComputeHash(fileBytes);
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < bytes.Length; i++)
			{
				sb.Append(bytes[i].ToString("x2"));
			}
			return sb.ToString();
		}
		catch (Exception ex)
		{
			throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
		}
	}
	public static string GetSha1Hash(string secret_key, byte[] fileBytes)
	{
		HMACSHA1 hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secret_key));
		byte[] hashBytes = hmac.ComputeHash(fileBytes);
		return Convert.ToBase64String(hashBytes);
	}

	public static byte[] GZipCompressString(byte[] bytes)
	{
		using(MemoryStream ms = new MemoryStream())
		{
			using(GZipStream gzip = new GZipStream(ms, CompressionMode.Compress))
			{
				gzip.Write(bytes, 0, bytes.Length);
			}
			return ms.ToArray();
		}
	}

	public static byte[] GZipDecompressString(byte[] data)
	{
		using(MemoryStream dms = new MemoryStream())
		{
			using(MemoryStream cms = new MemoryStream(data))
			{
				using(GZipStream gzip = new GZipStream(cms, CompressionMode.Decompress))
				{
					byte[] bytes = new byte[1024];
					int len = 0;
					while((len = gzip.Read(bytes, 0, bytes.Length)) > 0)
					{
						dms.Write(bytes, 0, len);
					}
				}
			}
			return dms.ToArray();
		}
	}

	public static string GetCacheStr()
	{
		return "?p=" + System.DateTime.Now.TimeOfDay.TotalMilliseconds.ToString();
	}
	
	public static string GetDevtype()
	{
		string devtype = string.Empty;
		#if UNITY_IPHONE
		devtype = "ios";
		#else
		devtype = "android";
		#endif
		return devtype;
	}

	public static FileStream GetWriteFileStream(string path)
	{
		return new FileStream(path, FileMode.Create, FileAccess.Write);
	}

	public static void WriteFile(string path, byte[] bytes)
	{
		using(FileStream fs = GetWriteFileStream(path))
		{
			if (bytes != null)
			{
				fs.Write(bytes, 0, bytes.Length);
			}
			fs.Close();
		}
	}

	public static byte[] GetFileBytes(string path)
	{
		using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
		{
			long fsLen = fs.Length;
			byte[] bytes = new byte[fsLen];
			fs.Read(bytes, 0, (int)fsLen);
			fs.Close();
			return bytes;
		}
	}

	public static Texture2D ResizeTex(Texture2D source, int newWidth, int newHeight)
	{
		source.filterMode = FilterMode.Point;
		RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
		rt.filterMode = FilterMode.Point;
		RenderTexture.active = rt;
		Graphics.Blit(source, rt);
		Texture2D newTex = new Texture2D(newWidth, newHeight);
		newTex.ReadPixels(new Rect(0, 0, newWidth, newWidth), 0, 0);
		newTex.Apply();
		RenderTexture.active = null;
		return newTex;
	}

	public static void WriteAudio(string path, byte[] bytes, AudioClip clip)
	{
		using(FileStream fs = GetWriteFileStream(path))
		{
			fs.Write(bytes, 0, bytes.Length);
			int hz = clip.frequency;
			int channels = clip.channels;
			int samples = clip.samples;
			fs.Seek(0, SeekOrigin.Begin);
			Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
			fs.Write(riff, 0, 4);
			Byte[] chunkSize = BitConverter.GetBytes(fs.Length - 8);
			fs.Write(chunkSize, 0, 4);
			Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
			fs.Write(wave, 0, 4);
			Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
			fs.Write(fmt, 0, 4);
			Byte[] subChunk1 = BitConverter.GetBytes(16);
			fs.Write(subChunk1, 0, 4);
			UInt16 one = 1;
			Byte[] audioFormat = BitConverter.GetBytes(one);
			fs.Write(audioFormat, 0, 2);
			Byte[] numChannels = BitConverter.GetBytes(channels);
			fs.Write(numChannels, 0, 2);
			Byte[] sampleRate = BitConverter.GetBytes(hz);
			fs.Write(sampleRate, 0, 4);
			Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2  
			fs.Write(byteRate, 0, 4);
			UInt16 blockAlign = (ushort) (channels * 2);
			fs.Write(BitConverter.GetBytes(blockAlign), 0, 2);
			UInt16 bps = 16;
			Byte[] bitsPerSample = BitConverter.GetBytes(bps);
			fs.Write(bitsPerSample, 0, 2);
			Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
			fs.Write(datastring, 0, 4);
			Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
			fs.Write(subChunk2, 0, 4);
			fs.Close();
		}
	}

	public static long calcKeyByXZ(int x, int z)
	{
		if (z < 0)
		{
			return ((long)x << 32) | ((long)-z | (long)0x80000000);
		}
		else
		{
			return ((long)x << 32) | (long)z;
		}
	}

	public static void Logout()
	{
		LuaSvr.mainState.Dispose();
	}

	public static void ShaderVariantWarmUp(ShaderVariantCollection collection)
	{
		collection.WarmUp();
	}

	public static bool AssetBundleNeedEncrypted(string assetBundleName) {
		return Config.NeedEncryptedAssetBundleNames.Contains(assetBundleName);
	}
	
	[DoNotToLua]
	public static void EncryptAssetBundleBytes(Stream stream, byte[] bytes) {
		var buffer = new byte[bytes.Length];
		Array.Copy(bytes, buffer, bytes.Length);
		ToolText.SetEncryptOrDecrypt(buffer);
		
		using (var fs = stream) {
			fs.Flush();
			fs.Position = 0;
			fs.Write(Config.ENCRYPTEDFLAG, 0, Config.ENCRYPTEDFLAG.Length);
			fs.Write(buffer, 0, buffer.Length);
		}
	}

	public static bool StripEncryptedAssetBundleBytes(byte[] bytes, out byte[] result) {
		bool encrypted = true;
		var flagLength = Config.ENCRYPTEDFLAG.Length;

		if (flagLength <= bytes.Length) {
			for (var i = 0; i < flagLength; i++) {
				encrypted = encrypted && (bytes[i] == Config.ENCRYPTEDFLAG[i]);
			}	
		}
		else {
			encrypted = false;
		}

		if (!encrypted) {
			flagLength = 0;
		}
		var realBytesCount = bytes.Length - flagLength;
		result = new byte[realBytesCount];
		Array.Copy(bytes, flagLength, result, 0, realBytesCount);
		if (encrypted) {
			ToolText.SetEncryptOrDecrypt(result);
		}

		return encrypted;
	}

	public static string PrepareLocalAssetPathForUnityWebRequest(string path) {
		// https://docs.unity3d.com/Manual/StreamingAssets.html
		// https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html
// #if !UNITY_ANDROID || UNITY_EDITOR
//         lsPath = "file://" + lsPath;
// #endif
// #if UNITY_ANDROID && !UNITY_EDITOR
//         if (persistExists) {
//             lsPath = "file://" + lsPath;
//         }
// #endif
		if (!path.StartsWith("jar:file://")) {
			path = "file://" + path;
		}

		return path;
	}
}
