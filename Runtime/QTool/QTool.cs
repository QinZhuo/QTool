using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using System;
using System.Threading.Tasks;
using QTool.Reflection;
using System.Reflection;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
namespace QTool
{


    public static partial class Tool
    {
		public static bool IsBuilding { set; get; }
		public static CultureInfo RealyCulture= CultureInfo.CurrentCulture;
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#endif
		static void Init()  
		{
			RealyCulture = CultureInfo.CurrentCulture;
			QDebug.Log("系统语言环境"+RealyCulture);
			CultureInfo.CurrentCulture = new CultureInfo("en-US");
			CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
			QTranslate.KeyReplace["版本号"] = Application.version;
		}
		public static string Version => Application.version; 
		public static bool IsTestVersion => Application.version.StartsWith("0.");
        static QDictionary<string, Color> KeyColor = new QDictionary<string, Color>();
		public static void RunURLWeb(string url)
		{
			QDebug.Log(nameof(RunURLWeb) + url);
			Application.OpenURL(url);
		}
		const string NetworkTranslateURL = "https://translate.googleapis.com/translate_a/single?client=gtx&sl={2}&tl={1}&dt=t&q={0}";

		static List<List<List<string>>> translateData = new List<List<List<string>>>();
		public static async Task<string> NetworkTranslateAsync(this string chineseText, string toLanguage="en",string fromLanguage= "zh-CN")
		{
			var jsonStr= await Tool.RunURLAsync(string.Format(NetworkTranslateURL, chineseText, toLanguage,fromLanguage ));
			jsonStr.ParseQData(translateData);
			return translateData[0][0][0];
		}
		public static async Task<string> RunURLAsync(this string requestUrl)
		{
			UnityWebRequest req = UnityWebRequest.Get(requestUrl);
			await req.SendWebRequest();
			if (!req.error.IsNull())
			{
				throw new Exception(req.error);
			}
			return req.downloadHandler.text;
		}
		public static async Task LoadSceneAsync(this string sceneName,float time=2f)
		{
			await QTask.Wait(time / 2,true);
			await SceneManager.LoadSceneAsync(sceneName);
			GCCollect();
			await QTask.Wait(time / 2,true);
		}
		public static void GCCollect()
		{
			Resources.UnloadUnusedAssets();
			System.GC.Collect();
		}
		public static Color ToColor(this string key, float s = 0.5f, float v = 1f)
        {
            if (string.IsNullOrWhiteSpace(key)) return Color.white;
			if(ColorUtility.TryParseHtmlString(key,out var newColor))
			{
				return newColor;
			}
			else
			{
				var colorKey = key + s + v;
				if (!KeyColor.ContainsKey(colorKey))
				{
					var colorValue = Mathf.Abs(key[0].GetHashCode() % 800) + Mathf.Abs(key.GetHashCode() % 200f);
					KeyColor[colorKey] = Color.HSVToRGB(colorValue / 1000, s, v);
				}
				return KeyColor[colorKey];
			}
        }

		//internal static Task<bool> Wait(float m_Seconds)
		//{
		//	throw new NotImplementedException();
		//}
		public static string GetPath(this Transform transform)
		{
			if (transform.parent == null)
			{
				return transform.name;
			}
			else
			{
				return transform.parent.GetPath() + "." + transform.name;
			}
		}
		public static Transform GetChild(this Transform transform,string childPath,bool autuCreate=false)
		{
			if (childPath.SplitTowString(".", out var start, out var end))
			{
				try
				{
					return GetChild(transform,start,autuCreate).GetChild(end,autuCreate);
				}
				catch (Exception e)
				{
					throw new Exception("路径出错 [" + childPath+"]", e);
				}
			}
			else
			{
				var child = transform.Find(start);
				if (child!=null)
				{
					return child;
				}
				else
				{
					if (autuCreate)
					{
						child = new GameObject(start).transform;
						child.position = transform.position;
						child.SetParent(transform);
						return child;
					}
					else
					{
						return null;
					}
				}
			}
		}
		public static void RemoveComponent<T>(this Component com) where T : Component
		{
			if (com == null) return;
			com?.gameObject.RemoveComponent<T>();
		}
		public static void RemoveComponent<T>(this GameObject obj) where T : Component
		{
			var com = obj?.GetComponent<T>();
			if (com != null )
			{
				CheckDestory(com);
			}
		}
		public static T GetComponent<T>(this Component com, bool autoCreate) where T : Component
		{
			return com?.gameObject.GetComponent<T>(autoCreate);
		}
		public static T GetComponent<T>(this GameObject obj, bool autoCreate) where T : Component
		{
			var com = obj?.GetComponent<T>();
			if (com==null&&autoCreate)
			{
				com = obj?.AddComponent<T>();
			}
			return com;
		}
		public static bool IsNull(this object obj)
		{
			return Equals(obj, null) || (obj is UnityEngine.Object uobj && uobj == null)||(obj is string str&& string.IsNullOrWhiteSpace(str));
		}
		public static string RemveChars(this string str,params char[] exceptchars)
		{
			if (str.IsNull()|| exceptchars==null) return str;
			foreach (var c in exceptchars)
			{
				str = str.Replace(c.ToString(), "");
			}
			return str;
		}
		public static string ToIdString(this string str, int length = -1)
		{
			if (length > 0)
			{
				str = str.Substring(0, Math.Min(str.Length, length));
			}
			return str.RemveChars('{','}', '（','）','~','\n','\t','\r','、','|', '*', '“','”', '—','。', '…','=','#', ' ', ';', '；', '-', ',', '，', '<', '>', '【', '】', '[', ']', '{', '}', '!', '！', '?', '？', '.', '\'', '‘', '’', '\"', ':', '：');
		}
		public static string ToShortString(this object obj, int length =5000)
		{
			var str = obj?.ToString();
			if (str!=null&&str.Length > length)
			{
				str = str.Substring(0, length)+"...";
			}
			return str;
		}
		public static string QName(this MonoBehaviour behaviour)
		{
			return behaviour.GetGameObject().QName();
		}
		public static string QName(this GameObject gameObject)
		{
			if (gameObject == null) return "";
			return gameObject.name.SplitStartString("(").TrimEnd();
		}

		class QKeyParseData : QDataList<QKeyParseData>, IKey<string>
		{
			public string Key { get; set; }
			public float Float;
		}
		public static Vector3 ToVector3(this Vector3Int vector3Int)
		{
			return new Vector3(vector3Int.x, vector3Int.y, vector3Int.z);
		}
		public static float ToComputeFloat(this object value)
		{
			if (value == null) return 0;
			var key = value.ToString();
			if (QKeyParseData.ContainsKey(key))
			{
				Debug.LogError("获取[" + key + "]:" + QKeyParseData.Get(key).Float);
				return QKeyParseData.Get(key).Float;
			}
			if (value is string str)
			{
				if (string.IsNullOrWhiteSpace(str)) return 0;
				if(float.TryParse(str, out var newFloat))
				{
					return newFloat;
				}
				else
				{
					List<string> numbers = new List<string>();
					var newNamber ="";
					for (int i = str.Length-1; i>=0; i--)
					{
						var c = str[i];
						if (char.IsNumber(c))
						{
							newNamber = c+newNamber;
						}
						else
						{
							if (newNamber.Length > 0)
							{
								numbers.Add(newNamber);
								newNamber = "";
							}
						}
					}
					if (newNamber.Length > 0)
					{
						numbers.Add(newNamber);
					}
					var sum = 0f;
					for (int i = 0; i < numbers.Count; i++)
					{
						sum += float.Parse(numbers[i]) * Mathf.Pow(10, i * 2);
					}
					if (numbers.Count==0)
					{
						return value.GetHashCode();
					}
					return sum;
				}
			}
			switch (Convert.GetTypeCode(value))
			{
				case TypeCode.DBNull:
				case TypeCode.Empty:
					return 0;
				case TypeCode.Object:
					return value.GetHashCode();
				default:
					return Convert.ToSingle(value);
			}
		}
		public static async void RunTimeCheck(string name, System.Action action, Func<int> getLength = null, Func<string> getInfo = null)
		{
			await RunTimeCheck(name, () => { action();return 0; },getLength,getInfo);
		}
		public static async Task RunTimeCheck(string name, System.Func<object> action, Func<int> getLength = null, Func<string> getInfo = null)
        {
            var last = System.DateTime.Now;
            try
            {
                var obj= action.Invoke();
				if(obj is Task task)
				{
					await task.Run();
				}
            }
            catch (Exception e)
            {
                Debug.LogError("【" + name + "】运行出错:" + e);
                return;
            }
            var checkInfo = "【" + name + "】运行时间:" + (System.DateTime.Now - last).TotalMilliseconds;
            if (getLength != null)
            {
                checkInfo += " " + " 大小" + getLength().ToSizeString();
            }
            if (getInfo != null)
            {
                checkInfo += "\n" + getInfo();
            }
            Debug.LogError(checkInfo);
        }
		public static Material GetInstanceMaterial(this UnityEngine.UI.Graphic graphic)
		{
			if (graphic.material == null) return null;
			if (Application.isPlaying)
			{
				if (graphic.material.name != "Instance_" + graphic.GetHashCode())
				{
					var instance = new Material(graphic.material);
					instance.name = "Instance_" + graphic.GetHashCode();
					graphic.material = instance;
				}
			}
			return graphic.material;
		}
        public static bool PercentRandom(float percent)
        {
            var value = UnityEngine.Random.Range(0, 100);
            return value <= percent;
        }
       
		public static object ParseEnum(this Type type,string str)
		{
			if (int.TryParse(str,out var intValue))
			{
				return Enum.ToObject(type, intValue);
			}
			else
			{
				return Enum.Parse(type, str);
			}
		}
		public static  bool IsInCamera(this Camera camera,Vector3 pos)
		{
			Vector3 vec = camera.WorldToViewportPoint(pos);
			return (vec.x > 0 && vec.x < 1 && vec.y > 0 && vec.y < 1);
		}

	

		public static string ToQTimeString(this DateTime time)
		{
			return time.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"); 
		}
		public static string ToQVersionString(this DateTime time)
		{
			return time.ToString(time.Year+"."+time.Month+"."+time.Day);
		}

		internal static void ForeachArray(this Array array, int deep, int[] indexArray, Action<int[]> Call, Action start = null, Action end = null, Action mid = null)
        {
            start?.Invoke();
            var length = array.GetLength(deep);
            for (int i = 0; i < length; i++)
            {
                indexArray[deep] = i;
                if (deep + 1 < indexArray.Length)
                {
                    ForeachArray(array, deep + 1, indexArray, Call, start, end, mid);
                }
                else
                {
                    Call?.Invoke(indexArray);
                }
                if (i < length - 1)
                {

                    mid?.Invoke();
                }

            }
            end?.Invoke();
        }

		public static string BuildString(Action<StringWriter> action)
		{
			using (var writer=new StringWriter())
			{
				action(writer);
				return writer.ToString();
			}
		}
		public static bool NextIsSplit(this StringReader reader, char value)
		{
			reader.IgnoreSpace();
			var flag = reader.NextIs(value);
			reader.IgnoreSpace();
			return flag;
        }
        public static bool NextIs(this StringReader reader, char value)
        {
            if (reader.Peek() == value)
			{
				reader.Read();
				return true;
            }
			return false;
		}
		public static void IgnoreSpace(this StringReader reader)
		{
			while (reader.NextIs('\n') || reader.NextIs('\r') || reader.NextIs(' ')) ;
		}
		public static bool IsEnd(this StringReader reader)
        {
            return reader.Peek() < 0;
		}
        /// <summary>
        /// 获取异或校验值
        /// </summary>
        public static byte ToCheckFlag(this byte[] bytes, byte flag = 0)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                flag ^= bytes[i];
            }
            return flag;
        }
		public static bool IsPrefabAsset(this UnityEngine.Object obj)
		{
#if UNITY_EDITOR
			return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(obj);
#else
			return false;
#endif
		}

		public static bool IsAsset(this UnityEngine.Object obj)
		{
#if UNITY_EDITOR
			return UnityEditor.EditorUtility.IsPersistent(obj);
#else
            return false;
#endif
		}
		public static bool IsPrefabInstance(this UnityEngine.Object obj)
		{
#if UNITY_EDITOR
			var gameObj = obj.GetGameObject();
			return gameObj!=null&&UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(gameObj) && !obj.IsPrefabAsset();
#else
            return false;
#endif
		}
		public static bool IsSceneInstance(this UnityEngine.Object obj)
		{
#if UNITY_EDITOR
			if (obj == null)
			{
				return false;
			}
			if (UnityEditor.EditorUtility.IsPersistent(obj))
			{
				return false;
			}
#endif
			return true;
		}
		public static GameObject GetGameObject(this UnityEngine.Object obj)
		{
			if (obj == null)
			{
				return null;
			}
			if (obj is Component com)
			{
				return com.gameObject;
			}
			if (obj is GameObject gameObject)
			{
				return gameObject;
			}
			else
			{
				return null;
			}
		}
		public static GameObject GetPrefab(this UnityEngine.Object obj)
		{
#if UNITY_EDITOR
			var gameObj= obj.GetGameObject();
			return gameObj == null ? null : UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObj));
#else
            return null;
#endif
		}
		public static T As<T>(this object obj)
		{
			try
			{
				return (T)obj;
			}
			catch (System.Exception)
			{
				Debug.LogError("强制转换"+ typeof(T) + "["+obj + "]出错");
			}
			return default;
		}

		static System.Diagnostics.ProcessStartInfo RunInfo = new System.Diagnostics.ProcessStartInfo()
		{
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
		};
		static QDictionary<string, bool> Cmd = new QDictionary<string, bool>();
		public static  string ProcessCommand(string fileName,string Arguments,string workPah,bool openWindow=false)
		{
			if (Cmd[fileName])
			{
				var errorInfo= "error : " + fileName + "程序正在运行"; 
				Debug.LogError(errorInfo);
				return errorInfo;
			}
			Cmd[fileName] = true;
			RunInfo.FileName = fileName;
			RunInfo.Arguments = Arguments;
			RunInfo.WorkingDirectory = workPah;
			RunInfo.CreateNoWindow = !openWindow;
			using (var process=new System.Diagnostics.Process {StartInfo= RunInfo })
			{
				try
				{
#if UNITY_EDITOR
					if (!Application.isPlaying)
					{
						UnityEditor.EditorUtility.DisplayProgressBar("运行命令", RunInfo.FileName + " " + RunInfo.Arguments + "\n" + RunInfo.WorkingDirectory, 0.5f);
					}
#endif
					
					QDebug.Log(RunInfo.FileName + " " + RunInfo.Arguments+"\n 运行路径"+ RunInfo.WorkingDirectory);
					process.Start();
					var info = process.StandardOutput.ReadToEnd();
					var error = process.StandardError.ReadToEnd();
#if UNITY_EDITOR
					if (!Application.isPlaying)
					{
						UnityEditor.EditorUtility.ClearProgressBar();
					}
#endif
					if (!string.IsNullOrWhiteSpace(error))
					{

						if (error.ToLower().Contains("warning"))
						{
							Debug.LogWarning(error);
						}
						else
						{
							Debug.LogError(error);
						}
					}
					else
					{
						QDebug.Log(info);
					}
					Cmd[fileName] = false;
					return info+error;
				}
				catch (Exception e)
				{
					Debug.LogError("运行 " + RunInfo.FileName + " 出错 " + RunInfo.FileName + " " + RunInfo.Arguments +"\n" + e);
					Cmd[fileName] = false;
					return "";
				}

			}
		}
	}
   


}
