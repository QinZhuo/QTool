using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using UnityEngine;
#if UNITY_SWITCH_API
using UnityEngine.Switch;
#endif
namespace QTool
{
	public static class QFileTool
	{
		public static string SaveDataPathRoot
		{
			get
			{
				switch (Application.platform)
				{

					case RuntimePlatform.Switch:
						return nameof(QFileTool);
					default:
						return Application.persistentDataPath;
				}
			}
		}
		public const string ResourcesPathRoot = "Assets/Resources";
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
        public static void ForeachAllFiles(this string rootPath, Action<string> action)
        {
            ForeachFiles(rootPath, action);
            ForeachDirectory(rootPath, (path) =>
            {
                path.ForeachAllFiles(action);
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
		public static string DirectoryName(this string path)
		{
			return Path.GetDirectoryName(path);
		}
		public static string FileName(this string path,bool withExtension=false)
		{
			if (withExtension)
			{
				return Path.GetFileName(path);
			}
			else
			{
				return Path.GetFileNameWithoutExtension(path);
			}
		}
		public static void DirectoryRename(this string path, string newPath)
		{
			Directory.Move(path, newPath);
			if (path != newPath)
			{
				if (File.Exists(path + ".meta"))
				{
					File.Delete(path + ".meta");
				}
			}
		}
		public static void FileDelete(this string path)
		{
			File.Delete(path);
			if (File.Exists(path+".meta"))
			{
				File.Delete(path + ".meta");
			}
		}
		public static void FileRename(this string path,string newName)
		{
			var fileDirectory = path.DirectoryName();
			var name = path.FileName();
			var ext = path.FileExtension();
			if (File.Exists(path))
			{
				File.Move(fileDirectory + "\\" + name + ext, fileDirectory + "\\" + newName + ext);
			}
		}
		public static string FileExtension(this string path)
		{
			if (Path.HasExtension(path))
			{
				return Path.GetExtension(path);
			}
			else
			{
				return "";
			}
		}

	
		public static void ForeachAllDirectory(this string rootPath, Action<string> action, string endsWith = "")
		{
			rootPath.ForeachDirectory((path) =>
			{
				if (endsWith.IsNull() || path.EndsWith(endsWith))
				{
					path.ForeachAllDirectory(action, endsWith);
					action?.Invoke(path.Replace('\\', '/'));
				}
			});
		}
		public static bool ExistsFile(this string path)
		{
			if (path.IsNull()) return false;
			return File.Exists(path);
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
		/// <summary>
		/// 控制所有同名文件夹显示与否 会影响打包是否包含此文件夹
		/// </summary>
		/// <param name="name">文件夹名</param>
		/// <param name="value">是否显示</param>
		public static void DirectoryVisible(this string name, bool value)
		{
			var startKey = "/" + (value ? "." : "") + name;
			Application.dataPath.ForeachAllDirectory(path =>
			{
				if (path.EndsWith(startKey))
				{
					if (value)
					{
						path.DirectoryRename(path.Replace(startKey, "/" + name));
					}
					else
					{
						path.DirectoryRename(path.Replace(startKey, "/." + name));
					}
				}
			});
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
				sourcePath.ForeachAllFiles((file) =>
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
				if (Application.isPlaying && path.StartsWith(ResourcesPathRoot))
				{
					return File.GetLastWriteTime(path);
				}
				else
				{
					return File.GetLastWriteTime(path);
				}
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
					path.ForeachAllFiles((filePath) =>
					{
						action(Load(filePath, defaultValue),filePath);
					});
				}
			}
		}
   
        public static void ClearData(this string path)
        {
            var directoryPath = DirectoryName(path);
            Directory.Delete(directoryPath, true);
         
        }
		public static void SaveQData<T>(this T data,string path)
		{
			Save(path, data.ToQData());
		}
		public static void SaveQDataList<T>(this IList<T> data, string path)
		{
			Save(path, data.ToQDataList().ToString(), System.Text.Encoding.Unicode);
		}
		public static T LoadQData<T>(this T data, string path) 
		{
			return Load(path).ParseQData(data);
		}
		public static List<T> LoadQDataList<T>(this List<T> data, string path)
		{
			return new QDataTable(Load(path, data.ToQDataList().ToString())).ParseQDataList(data);
		}
		public static string Combine(this string rootPath,string childe)
		{
			if(childe.IsNull())return rootPath;
			return $"{rootPath}/{childe}";
		}
		public static string CheckDirectoryPath(this string path)
		{
			path = path.CheckPath();
			var directoryPath = Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(directoryPath) && !ExistsDirectory(directoryPath)) {
				try {
					Debug.LogWarning("自动创建文件夹 " + directoryPath);
					Directory.CreateDirectory(directoryPath);
				}
				catch (Exception e) {
					Debug.LogError("创建文件夹出错：" + directoryPath + ":" + e);
				}
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
								Notification.EnterExitRequestHandlingSection();
								QDebug.LogWarning("尝试创建文件 [" + bytes.LongLength + "]" + path);
								var t = nn.fs.File.Create(path, 0);
								t.abortUnlessSuccess();
								QDebug.LogWarning("自动创建文件 " + path);
							}
							else
							{
								Notification.EnterExitRequestHandlingSection(); ;
							}
							nn.Result result = nn.fs.File.Open(ref fileHandle, path, nn.fs.OpenFileMode.Write| nn.fs.OpenFileMode.AllowAppend);
							result.abortUnlessSuccess();
							QDebug.LogWarning("尝试保存文件 [" + bytes.LongLength + "]" + path);
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
#if QCrypto
							if (path.EndsWith(DataExtension))
							{
								bytes = bytes.Encrypt();
							}
#endif
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
		public static bool SaveCheckChange(string path, string data)
		{
			if (!Equals(Load(path).GetHashCode(), data.GetHashCode())) {
				Save(path, data, System.Text.Encoding.Unicode);
				return true;
			}
			return false;
		}
		public static void Save(string path, string data, System.Text.Encoding Encoding = null)
		{
#if QCrypto
			if (path.EndsWith(DataExtension))
			{
				Save(path, data.GetBytes());
			}
			else
#endif
			{
				path = CheckDirectoryPath(path);
				if (Encoding == null)
				{
					File.WriteAllText(path, data);
				}
				else
				{
					File.WriteAllText(path, data, Encoding);
				}
			}
		}
		public static string WithoutExtension(this string path)
		{
			try {
				path = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
			}
			catch (Exception) {
				Debug.LogError($"路径出错[{path}]");
				throw;
			}
			return path;
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
#if QCrypto
						if (path.EndsWith(DataExtension))
						{
							result = result.Decrypt();
						}
#endif
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
		public static string Load(string path, string defaultValue = "",bool ignoreResources=false, System.Text.Encoding Encoding = null)
		{
			try
			{
				if (!ignoreResources&&path.StartsWith(ResourcesPathRoot))
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
#if QCrypto
					if (path.EndsWith(DataExtension))
					{
						return LoadBytes(path, defaultValue.IsNull()?null:defaultValue.GetBytes()).GetString();
					}
					else
#endif
					{
						if (ExistsFile(path))
						{
							if(Encoding == null)
							{
								return File.ReadAllText(path);
							}
							else
							{
								return File.ReadAllText(path, Encoding);
							}
						}
						else
						{
							return defaultValue;
						}
					}
				}
			}
			catch (Exception e)
			{
				if (defaultValue.IsNull())
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

		public static string SelectOpenPath(string title = "打开文件", string extension = "*", string directory = "Assets")
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
		public static string SelectSavePath(string title = "保存文件", string directory= "Assets", string defaultName="newfile", string extension = "*" )
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
