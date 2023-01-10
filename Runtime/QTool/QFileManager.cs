using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Security.Cryptography;
#if UNITY_SWITCH_API
using UnityEngine.Switch;
#endif
namespace QTool
{
	public static class QFileManager
	{
		public static string SaveDataPathRoot
		{
			get
			{
				switch (Application.platform)
				{

					case RuntimePlatform.Switch:
						return nameof(QFileManager);
					default:
						return Application.persistentDataPath;
				}
			}
		}
		public static string ModPathRoot=> Application.streamingAssetsPath;

		public const string ResourcesPathRoot = "Assets/Resources";
#if UNITY_SWITCH_API
		public static nn.account.Uid userId;
		public static nn.fs.FileHandle fileHandle = new nn.fs.FileHandle();
#endif
		static QFileManager()
		{
			switch (Application.platform)
			{
				case RuntimePlatform.Switch:
					{
#if UNITY_SWITCH_API

						nn.account.Account.Initialize();
						nn.account.UserHandle userHandle = new nn.account.UserHandle();
						if (!nn.account.Account.TryOpenPreselectedUser(ref userHandle))
						{
							nn.Nn.Abort("Failed to open preselected user.");
						}
						nn.Result result = nn.account.Account.GetUserId(ref userId, userHandle);
						result.abortUnlessSuccess();
						result = nn.fs.SaveData.Mount(nameof(QTool), userId);
						result.abortUnlessSuccess();
						Debug.Log("初始化" + Application.platform);
#endif
					}
					break;
			}
		}
        public static T QDataCopy<T>(this T target)
        {
			return target.ToQData().ParseQData<T>();
		}
		public static string ToAssetPath(this string path)
        {
			if (path.StartsWith(Application.dataPath))
			{
				return "Assets/" + path.SplitEndString(Application.dataPath);
			}
			return path;
        }
        public static void ForeachDirectoryFiles(this string rootPath, Action<string> action)
        {
            ForeachFiles(rootPath, action);
            ForeachDirectory(rootPath, (path) =>
            {
                path.ForeachDirectoryFiles(action);
            });
        }
        public static int DirectoryFileCount(this string rootPath)
        {
            var count = rootPath.FileCount();
            rootPath.ForeachDirectory((path) =>
            {
                count += rootPath.FileCount();
            });
            return count;
        }
        public static int FileCount(this string rootPath)
        {
            return ExistsDirectory(rootPath) ? Directory.GetFiles(rootPath).Length / 2 : 0;
        }
        public static void ForeachAllDirectoryWith(this string rootPath, string endsWith, Action<string> action)
        {
            rootPath.ForeachDirectory((path) =>
            {
                if (path.EndsWith(endsWith))
                {
                    action?.Invoke(path.Replace('\\', '/'));
                }
                else
                {
                    path.ForeachAllDirectoryWith(endsWith, action);
                }
            });
        }
		public const string SecretExtension = ".sec";
		public static byte[] SecretKey = (Application.companyName.IsNullOrEmpty()||Application.productName.IsNullOrEmpty())? "QTSC".GetBytes():(Application.companyName.Substring(0,2)+Application.productName.Substring(0,2)).GetBytes();
		public static byte[] Encrypt(this byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0) return bytes;
			using (var memery = new MemoryStream())
			{
				using (var des = new DESCryptoServiceProvider())
				{
					using (var writer = new CryptoStream(memery, des.CreateEncryptor(SecretKey,SecretKey), CryptoStreamMode.Write))
					{
						writer.Write(bytes, 0, bytes.Length);
						writer.FlushFinalBlock();
						return memery.ToArray();
					}
				}
			}
		}
		public static byte[] Decrypt(this byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0) return bytes;
			using (var memery = new MemoryStream())
			{
				using (var des = new DESCryptoServiceProvider())
				{
					using (var writer = new CryptoStream(memery, des.CreateDecryptor(SecretKey, SecretKey), CryptoStreamMode.Write))
					{
						writer.Write(bytes, 0, bytes.Length);
						writer.FlushFinalBlock();
						return memery.ToArray();
					}
				}
			}
		}
		public static bool ExistsFile(this string path)
		{
			path= CheckDirectoryPath( path);
			switch (Application.platform)
			{
				case RuntimePlatform.Switch:
					{
#if UNITY_SWITCH_API
						nn.fs.EntryType entryType = 0;
						nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, path);
						if (result.IsSuccess())
						{
							Debug.Log("存在 " + path);
							return true;
						}
						if (!nn.fs.FileSystem.ResultPathNotFound.Includes(result))
						{
							result.abortUnlessSuccess();
						}
						Debug.Log("不存在 " + path);
						return false;
#else
						return false;
#endif
					}
				default:
					{
						return File.Exists(path);
					}
			}

		}
		public static bool ExistsDirectory(this string path)
		{
			if (path.Contains(nameof(Resources)+'/'))
			{
				return true;
			}
			return Directory.Exists(path);
		}
		public static void ForeachDirectory(this string rootPath, Action<string> action)
        {
            if (ExistsDirectory(rootPath))
            {
                var paths = Directory.GetDirectories(rootPath);
                foreach (var path in paths)
                {
					if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
					}
					action?.Invoke(path.Replace('\\', '/'));
                }
            }
            else
            {
                Debug.LogWarning("错误" + " 不存在文件夹" + rootPath);
            }
        }
        public static void ForeachFiles(this string rootPath, Action<string> action)
        {
            if (ExistsDirectory(rootPath))
            {
                var paths = Directory.GetFiles(rootPath);
                foreach (var path in paths)
                {
                    if (string.IsNullOrWhiteSpace(path) || path.EndsWith(".meta"))
                    {
                        continue;
                    }
                    action?.Invoke(path.Replace('\\', '/'));
                }
            }
            else
            {
                Debug.LogWarning("错误" + " 不存在文件夹" + rootPath);
            }
        }
	
		public static void Copy(string sourcePath, string targetPath)
		{
			CheckDirectoryPath(targetPath + "/");
			if (Directory.Exists(sourcePath))
			{
				sourcePath.ForeachDirectoryFiles((file) =>
				{
					var newFile = file.Replace(sourcePath.CheckPath(), targetPath.CheckPath());
					newFile.CheckDirectoryPath();
					File.Copy(file, newFile, true);
				});
			}
			else if(File.Exists(sourcePath))
			{
				File.Copy(sourcePath, targetPath);
			}
		}

        public static Dictionary<string, XmlSerializer> xmlSerDic = new Dictionary<string, XmlSerializer>();
        public static XmlSerializer GetSerializer(Type type, params Type[] extraTypes)
        {
            var key = type.FullName;
            foreach (var item in extraTypes)
            {
                key += " " + item.FullName;
            }
            if (xmlSerDic.ContainsKey(key))
            {
                return xmlSerDic[key];
            }
            else
            {
                XmlSerializer xz = new XmlSerializer(type, extraTypes);
                xmlSerDic.Add(key, xz);
                return xz;
            }
        }
		public static string QXmlSerialize<T>(T t, params Type[] extraTypes)
		{
			using (StringWriter sw = new StringWriter())
			{
				if (t == null)
				{
					Debug.LogError("序列化数据为空" + typeof(T));
					return null;
				}
				GetSerializer(typeof(T), extraTypes).Serialize(sw, t);
				return sw.ToString();
			}
		}
		public static T QXmlDeserialize<T>(string s, params Type[] extraTypes)
		{
			using (StringReader sr = new StringReader(s))
			{
				try
				{
					XmlSerializer xz = GetSerializer(typeof(T), extraTypes);
					return (T)xz.Deserialize(sr);
				}
				catch (Exception e)
				{
					Debug.LogError("Xml序列化出错：\n" + e);
					return default;
				}
			}
		}
		public static DateTime GetLastWriteTime(string path)
		{
			
			try
			{
#if UNITY_SWITCH_API
				return DateTime.MinValue;
#else
				if (Application.isPlaying && path.StartsWith(ResourcesPathRoot))
				{
#if UNITY_EDITOR
					return File.GetLastWriteTime(path);
#else
					return DateTime.MinValue;
#endif
				}
				else
				{
					return File.GetLastWriteTime(path);
				}
#endif
			}
			catch (Exception e)
			{
				Debug.LogError(e);
				return DateTime.MinValue;
			}
	
		}
   
		public static void LoadAll(string path,Action<string,string> action, string defaultValue = "")
		{
			if (path.StartsWith(ResourcesPathRoot))
			{
				try 
				{ 
					var loadPath = path.SplitEndString(ResourcesPathRoot+"/").SplitStartString(".");
					var texts= Resources.LoadAll<TextAsset>(loadPath);
					foreach (var text in texts)
					{
						action(text.text,
#if UNITY_EDITOR
							UnityEditor.AssetDatabase.GetAssetPath(text)

#else
							text.name
#endif
							); 
					} 
				}
				catch (Exception e)
				{
					Debug.LogWarning(e);
				}
			}
			else
			{
				if (ExistsFile(path))
				{
					action(Load(path, defaultValue), path);
				}
				else
				{
					path = Path.GetFileNameWithoutExtension(path);
					path.ForeachDirectoryFiles((filePath) =>
					{
						action(Load(filePath, defaultValue),filePath);
					});
				}
			}
		}
   
        public static void ClearData(this string path)
        {
            var directoryPath = GetFolderPath(path);
            Directory.Delete(directoryPath, true);
         
        }
        /// <summary>
        /// 获取文件夹路径
        /// </summary>
        public static string GetFolderPath(this string path)
        {
            return Path.GetDirectoryName(path);
        }
    
     
   
		public static void SaveQData<T>(this T data,string path)
		{
			Save(path, data.ToQData());
		}
		public static void SaveQDataList<T>(this IList<T> data, string path)
		{
			Save(path, data.ToQDataList().ToString());
		}
		public static T LoadQData<T>(this T data, string path) 
		{
			return Load(path).ParseQData(data);
		}
		public static List<T> LoadQDataList<T>(this List<T> data, string path)
		{
			return new QDataList(Load(path, data.ToQDataList().ToString())).ParseQdataList(data);
		} 
		public static void SaveBytes<T>(this T data,string path)
		{
			Save(path, QSerialize.Serialize(data));
		}
		public static T LoadBytes<T>(this T target, string path)
		{
			target= LoadBytes(path, target.Serialize()).Deserialize(target);
			QDebug.Log("加载[" + path + "]成功\n" + target.ToShortString());
			return target;
		}

		public static string ChildPath(this string rootPath,string childe)
		{
			if (childe.IsNullOrEmpty())
			{
				return rootPath;
			}
			else
			{
				return rootPath + "/" + childe;
			}
		}
		public static string CheckDirectoryPath(this string path)
		{
			path = path.CheckPath();
			switch (Application.platform)
			{
				case RuntimePlatform.Switch:
					{
#if UNITY_SWITCH_API
						if (!path.StartsWith(Application.streamingAssetsPath))
						{
							if (!path.StartsWith(nameof(QTool) + ":/"))
							{
								path = nameof(QTool) + ":/" + path.Replace('/','_');
								Debug.Log("转换路径 " + path);
							}
						}
#endif
					}
					break;
				default:
					var directoryPath = Path.GetDirectoryName(path);
					if (!string.IsNullOrWhiteSpace(directoryPath) && !ExistsDirectory(directoryPath))
					{
						try
						{
							Debug.LogWarning("自动创建文件夹 " + directoryPath);
							Directory.CreateDirectory(directoryPath);
						}
						catch (Exception e)
						{
							Debug.LogError("创建文件夹出错：" + directoryPath + ":" + e);
						}
					}
					break;
			}
			return path;
		}
		public static string CheckPath(this string path)
		{
			return path.Replace('\\', '/');
		}
		public static void Save(string path, byte[] bytes)
		{
			path = CheckDirectoryPath(path);
			try
			{
				switch (Application.platform)
				{
					case RuntimePlatform.Switch:
						{
#if UNITY_SWITCH_API
							if (!ExistsFile(path))
							{
								Debug.LogWarning("不存在文件" + path);
								UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
								var t = nn.fs.File.Create(path, bytes.LongLength);
								t.abortUnlessSuccess();
								UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
								Debug.LogWarning("自动创建文件 " + path);
							}
							Notification.EnterExitRequestHandlingSection(); ;
							nn.Result result = nn.fs.File.Open(ref fileHandle, path, nn.fs.OpenFileMode.Write| nn.fs.OpenFileMode.AllowAppend);
							result.abortUnlessSuccess();
							result = nn.fs.File.Write(fileHandle, 0, bytes, bytes.LongLength, nn.fs.WriteOption.Flush);
							result.abortUnlessSuccess();
							nn.fs.File.Close(fileHandle);
							result = nn.fs.FileSystem.Commit(nameof(QTool));
							result.abortUnlessSuccess();
							Notification.LeaveExitRequestHandlingSection();
#endif
						}
						break;
					default:
						{
							if (path.EndsWith(SecretExtension))
							{
								bytes = bytes.Encrypt();
							}
							File.WriteAllBytes(path, bytes);
						}
						break;
				}
			}
			catch (Exception e)
			{
				Debug.LogError("向路径写入数据出错" + e);

			}
		}
		public static void Save(string path, string data)
		{
			if (path.EndsWith(SecretExtension))
			{
				Save(path, data.GetBytes());
			}
			else
			{
				path = CheckDirectoryPath(path);
				File.WriteAllText(path, data);
			}
		}
		public static byte[] LoadBytes(string path,byte[] defaultValue=default)
		{
			switch (Application.platform)
			{
				case RuntimePlatform.Switch:
					{
#if UNITY_SWITCH_API

						path = CheckDirectoryPath(path);
						Debug.Log("打开 " + path);
						nn.Result result = nn.fs.File.Open(ref fileHandle, path, nn.fs.OpenFileMode.Read);
						result.abortUnlessSuccess();
						long fileSize = 0;
						result = nn.fs.File.GetSize(ref fileSize, fileHandle);
						result.abortUnlessSuccess();
						var data = new byte[fileSize];
						result = nn.fs.File.Read(fileHandle, 0, data, fileSize);
						result.abortUnlessSuccess();
						nn.fs.File.Close(fileHandle);
						return data;
#else
						return null;
#endif
					}
				default:
					try
					{
						var result = File.ReadAllBytes(path);
						if (path.EndsWith(SecretExtension))
						{
							result = result.Decrypt();
						}
						return result;
					}
					catch (Exception e)
					{
						if (defaultValue == null || defaultValue.Length == 0)
						{
							Debug.LogError("加载 " + path + " 出错：" + e);
						}
						else
						{
							Debug.LogWarning("加载 " + path + " 出错：" + e);
						}
						return defaultValue==null?new byte[0]: defaultValue;
					}
					
			}


		}
		public static string Load(string path, string defaultValue = "")
		{
			try
			{
				if (path.StartsWith(ResourcesPathRoot))
				{
					var text = Resources.Load<TextAsset>(path.SplitEndString(ResourcesPathRoot + "/").SplitStartString("."));
					if (text == null)
					{
						return defaultValue;
					}
					else
					{
						return text.text;
					}
				}
				else
				{
					if (path.EndsWith(SecretExtension))
					{
						return LoadBytes(path, defaultValue.IsNullOrEmpty()?null:defaultValue.GetBytes()).GetString();
					}
					else
					{
						return File.ReadAllText(path);
					}
				}
			}
			catch (Exception e)
			{
				if (defaultValue.IsNullOrEmpty())
				{
					Debug.LogError("加载文件出错[" + path + "]" + e);
				}
				else
				{
					Debug.LogWarning("加载文件出错[" + path + "]" + e);
				}
				
				return defaultValue;
			}
		}
		public static void SaveJPG(this Texture2D tex, string path,int quality=50)
		{
			var bytes = tex.EncodeToJPG(quality);
			if (bytes != null)
			{
				Save(path, bytes);
			}
		}
		public static void SavePNG(this Texture2D tex, string path)
		{
			var bytes = tex.EncodeToPNG();
			if (bytes != null)
			{
				Save(path,bytes);
			}
		}
		public static Texture2D LoadTexture(this string path)
		{
			var bytes =LoadBytes(path);
			Texture2D tex = new Texture2D(2, 2);
			tex.LoadImage(bytes);
			return tex;
		}
		public static string SelectOpenPath(string title = "打开文件", string extension = "obj", string directory = "Assets")
        {
            var dialog = new FileDialog
            {
                title = title,
                initialDir = directory,
                // defExt = extension,
                filter = "(." + extension + ")\0*." + extension + "\0",
            };
            if (FileDialog.GetOpenFileName(dialog))
            {
                return dialog.file;
            }
            return "";
        }
        public static string SelectSavePath(string title = "保存文件", string directory= "Assets", string defaultName="newfile", string extension = "obj" )
        {
            var dialog = new FileDialog
            {
                title = title,
                initialDir = directory,
                file=defaultName,
                //  defExt = extension,
                filter = "(." + extension + ")\0*." + extension + "\0",
            };
            if (FileDialog.GetSaveFileName(dialog))
            {
                if (dialog.file.EndsWith("." + extension))
                {
                    return dialog.file + "." + extension;
                }
                return  dialog.file;
            }
            return "";
        }
	}



#region WindowsData

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public class FileDialog
	{
		public int structSize = 0;
		public IntPtr dlgOwner = IntPtr.Zero;
		public IntPtr instance = IntPtr.Zero;
		public String filter = null;
		public String customFilter = null;
		public int maxCustFilter = 0;
		public int filterIndex = 0;
		public String file = null;
		public int maxFile = 0;
		public String fileTitle = null;
		public int maxFileTitle = 0;
		public String initialDir = null;
		public String title = null;
		public int flags = 0;
		public short fileOffset = 0;
		public short fileExtension = 0;
		public String defExt = null;
		public IntPtr custData = IntPtr.Zero;
		public IntPtr hook = IntPtr.Zero;
		public String templateName = null;
		public IntPtr reservedPtr = IntPtr.Zero;
		public int reservedInt = 0;
		public int flagsEx = 0;
		public FileDialog()
		{
			structSize = Marshal.SizeOf(this);
			file = new string(new char[256]);
			maxFile = file.Length;
			fileTitle = new string(new char[64]);
			maxFileTitle = fileTitle.Length;
			flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
		}
		[DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
		public static extern bool GetOpenFileName([In, Out] FileDialog ofd);
		[DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
		public static extern bool GetSaveFileName([In, Out] FileDialog ofd);
	}
#endregion
}
