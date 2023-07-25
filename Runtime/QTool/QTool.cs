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
using UnityEngine.LowLevel;
using System.Linq;
using UnityEngine.Playables;
using System.Net;
using QTool.Inspector;
#if UNITY_EDITOR
#if UNITY_2022_1_OR_NEWER
using PrefabStageUtility = UnityEditor.SceneManagement.PrefabStageUtility;
#else
using PrefabStageUtility=UnityEditor.Experimental.SceneManagement.PrefabStageUtility;
#endif
#endif

namespace QTool
{


    public static class QTool
    {
		public static bool IsPlaying => Application.isPlaying && QOnPlayModeAttribute.CurrentrState != PlayModeState.EnteredEditMode && QOnPlayModeAttribute.CurrentrState != PlayModeState.ExitingPlayMode;
		public static bool IsBuilding { set; get; }
		public static CultureInfo RealyCulture= CultureInfo.CurrentCulture;
		private static string _LocalIp = null;
		public static string LocalIp => _LocalIp??= Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();

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
		public static void Quit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.ExitPlaymode();
#else
			Application.Quit();
#endif
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
			var jsonStr= await QTool.RunURLAsync(string.Format(NetworkTranslateURL, chineseText, toLanguage,fromLanguage ));
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
	
		private static PlayerLoopSystem AddPlayerLoop(this PlayerLoopSystem playerLoop,Type type, Action action)
		{
			var loopList = playerLoop.subSystemList.ToList();
			loopList.Add(new PlayerLoopSystem { type = type, updateDelegate =new PlayerLoopSystem.UpdateFunction(action)});
			playerLoop.subSystemList = loopList.ToArray();
			return playerLoop;
		}
		private static PlayerLoopSystem RemovePlayerLoop(this PlayerLoopSystem playerLoop,Type type)
		{
			var loopList = playerLoop.subSystemList.ToList();
			loopList.RemoveAll((loop) => loop.type == type);
			playerLoop.subSystemList = loopList.ToArray();
			return playerLoop;
		}
		public static void AddPlayerLoop(Type type, Action action, string subSystem = "")
		{
			var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
			if (!subSystem.IsNull())
			{
				var subList = playerLoop.subSystemList.ToList();
				var index = subList.FindIndex((loop) => loop.ToString() == subSystem);
				playerLoop.subSystemList[index] = playerLoop.subSystemList[index].AddPlayerLoop(type, action);
			}
			else
			{
				playerLoop = playerLoop.AddPlayerLoop(type, action);
			}
			QDebug.Log("更改主循环 "+nameof(PlayerLoop) + (subSystem.IsNull() ? "" : " 在" + subSystem+"中") + " 添加子系统 " + type + "\n主循环信息:\n" + playerLoop.subSystemList.ToOneString("\n", (loop) => "----------" + loop + "----------\n" + loop.subSystemList.ToOneString()));
			PlayerLoop.SetPlayerLoop(playerLoop);
		}
		public static void RemovePlayerLoop(Type type, string subSystem = "")
		{
			var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
			if (!subSystem.IsNull())
			{
				var subList = playerLoop.subSystemList.ToList();
				var index = subList.FindIndex((loop) => loop.ToString() == subSystem);
				playerLoop.subSystemList[index] = playerLoop.subSystemList[index].RemovePlayerLoop(type);
			}
			else
			{
			 	playerLoop= playerLoop.RemovePlayerLoop(type);
			}
			PlayerLoop.SetPlayerLoop(playerLoop);
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
		public static Transform FindAll(this Transform transform,string name)
		{
			var find= transform.Find(name);
			if(find == null)
			{
				for (int i = 0; i < transform.childCount; i++)
				{
					find= transform.GetChild(i).FindAll(name);
					if (find != null)
					{
						return find;
					}
				}
			}
			return find;
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
				obj?.SetDirty();
			}
			return com;
		}
		
		public static bool IsNull<T>(this T obj)
		{
			T checkObj = default;
			return Equals(obj, checkObj) || (obj is UnityEngine.Object uobj && uobj == null)||(obj is string str&& string.IsNullOrWhiteSpace(str));
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

		class QKeyParseData : QDataList<QKeyParseData>
		{
			public override string Key { get; set; }
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
		public static void ForeachFlags<T>(this T enumValue,Action<T> action)where T:Enum
		{
			var type = enumValue.GetType();
			foreach (var value in Enum.GetValues(type))
			{
				if (enumValue.HasFlag((Enum)value))
				{
					action((T)value);
				}
			}
		}
		public static  bool IsInCamera(this Camera camera,Vector3 pos)
		{
			Vector3 vec = camera.WorldToViewportPoint(pos);
			return (vec.x > 0 && vec.x < 1 && vec.y > 0 && vec.y < 1);
		}
		public static Transform GetHumanBone(this GameObject gameObject, HumanBodyBones name)
		{
			return gameObject.GetComponent<Animator>()?.GetBoneTransform(name);
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

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void CheckSaveAsset(this UnityEngine.Object obj,string path)
		{
#if UNITY_EDITOR
			if (Application.isPlaying) return;
			if (!obj.IsAsset())
			{
				path.CheckDirectoryPath();
				UnityEditor.AssetDatabase.CreateAsset(obj, path);
			}
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
		public static bool IsPrefab(this UnityEngine.Object obj)
		{
#if UNITY_EDITOR
			return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(obj);
#else
			return false;
#endif
		}
#if UNITY_EDITOR
		public static bool ApplyPrefab(this GameObject gameObject)
		{
			if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject))
			{
				UnityEditor.PrefabUtility.ApplyPrefabInstance(gameObject, UnityEditor.InteractionMode.AutomatedAction);
				return true;
			}
			return false;
		}
#endif
		public static bool IsPrefabInstance(this UnityEngine.Object obj, out GameObject prefab)
		{
			prefab = null;
#if UNITY_EDITOR
			if (obj == null || obj.IsPrefab())
			{
				return false;
			}
			if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(obj))
			{
				prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj.GetGameObject());
				return true;
			}
			else if (PrefabStageUtility.GetCurrentPrefabStage() != null)
			{
				var stage = PrefabStageUtility.GetPrefabStage(obj.GetGameObject());
				if (stage != null)
				{
					if (stage.assetPath != PrefabStageUtility.GetCurrentPrefabStage().assetPath)
					{
						prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(stage.assetPath);
						return true;
					}
				}
			}
#endif
			return false;
		}
		public static void PrefabSaveAsset(this GameObject prefabInstance, UnityEngine.Object asset)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (!asset.IsAsset() && prefabInstance.IsPrefabInstance(out var prefab))
				{
					if (asset.name.IsNull())
					{
						asset.name = prefab.name + "_" + asset.GetType().Name;
					}
					var path = UnityEditor.AssetDatabase.GetAssetPath(prefab).SplitStartString(".prefab") + "/" + asset.name+ ".asset";
					path.CheckDirectoryPath();
					UnityEditor.AssetDatabase.CreateAsset(asset, path);
				}
			}
#endif
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

		public static T As<T>(this object obj)
		{
			try
			{
				return (T)obj;
			}
			catch (Exception e)
			{
				Debug.LogError("强制转换" + typeof(T) + "[" + obj + "]出错 " + e);
			}
			return default;
		}
		public static object AsType(this object obj, Type type)
		{
			try
			{
				if (obj == null) return null;
				var objType = obj.GetType();
				if (objType.IsAssignableFrom(type))
				{
					return obj;
				}
				else
				{   
					if (type == typeof(object)||type.IsPrimitive)
					{
						return obj;
					}
					else if (type == typeof(string))
					{
						return obj?.ToString();
					}
					else if (obj is Component component)
					{
						if (type == typeof(GameObject))
						{
							return component.gameObject;
						}
						else
						{
							return component.GetComponent(type);
						}
					}
					else if (obj is GameObject gameObj)
					{
						return gameObj.GetComponent(type);
					}
				}
				return default;
			}
			catch (Exception e)
			{
				Debug.LogError("强制转换" + type + "[" + obj + "]出错 " + e);
			}
			return default;
		}
		/// <summary>
		/// 设置时间并演出
		/// </summary>
		public static void SetTime(this PlayableDirector playableDirector, float value)
		{
			playableDirector.time = value;
			playableDirector.Evaluate();
		}
		/// <summary>
		/// 立即完成当前演出
		/// </summary>
		public static void Complete(this PlayableDirector playableDirector,bool playforward=true)
		{
			if (playableDirector.playableAsset != null && playableDirector.state == PlayState.Playing)
			{
				SetTime(playableDirector, playforward ? (float)playableDirector.playableAsset.duration : 0);
			}
		}
		/// <summary>
		/// 完成上一个演出并播放新的
		/// </summary>
		public static void CompleteAndPlay(this PlayableDirector playableDirector, PlayableAsset value)
		{
			if (playableDirector.playableAsset != value)
			{
				playableDirector.Complete();
			}
			playableDirector.Play(value);
		}
	
		public static T RayCast<T>(this Ray ray, Func<T, bool> CanCast = null,float radius=0) where T : Component
		{
			var hits = radius <= 0 ? Physics.RaycastAll(ray) : Physics.SphereCastAll(ray, radius);
			foreach (var hit in hits)
			{
				var target = hit.collider.GetComponent<T>();
				if (target != null && (CanCast == null || CanCast(target)))
				{
					return target;
				}
			}
			return default;
		}
		public static Vector3 RayCast(this Ray ray)
		{
			if (Physics.Raycast(ray, out var hitInfo,100,-1, queryTriggerInteraction: QueryTriggerInteraction.Ignore))
			{
				return hitInfo.point;
			}
			return RayCastPlane(ray, Vector3.up, Vector3.zero);
		}
		
		public static Vector3 RayCastPlane(this Ray ray, Vector3 planeNormal, Vector3 planePoint = default)
		{
			float d = Vector3.Dot(planePoint - ray.origin, planeNormal) / Vector3.Dot(ray.direction, planeNormal);
			return d * ray.direction + ray.origin;
		}
		public static Bounds GetBounds(this GameObject obj)
		{
			return obj.transform.GetBounds();
		}
		public static Bounds GetBounds(this Component com)
		{
			var bounds = new Bounds(com.transform.position, Vector3.zero);
			Renderer[] meshs = com.GetComponentsInChildren<Renderer>();
			foreach (var mesh in meshs)
			{
				if (mesh is MeshRenderer || mesh is SpriteRenderer || mesh is SkinnedMeshRenderer)
				{
					if (bounds.extents == Vector3.zero)
					{
						bounds = mesh.bounds;
					}
					else
					{
						bounds.Encapsulate(mesh.bounds);
					}
				}
			}
			if (float.IsNaN(bounds.size.x))
			{
				bounds.center = com.transform.position;
				bounds.size = Vector3.zero;
			}
			return bounds;
		}

	
		public static bool Similar(this float value, float other, float scale)
		{
			return Mathf.Abs(value - other) < scale;
		}
		public static bool Similar(this float value, float other=default)
		{
			return Mathf.Approximately(value, other);
		}
		public static bool Similar(this Vector3 value, Vector3 other=default)
		{
			return value.x.Similar(other.x) && value.y.Similar(other.y) && value.z.Similar(other.z);
		}
		public static bool Similar(this Vector2 value, Vector2 other = default)
		{
			return value.x.Similar(other.x) && value.y.Similar(other.y);
		}
		public static int ToGrid(this float value, float size)
		{
			return Mathf.RoundToInt(value / size);
		}
		public static float GridFixed(this float value, float size)
		{
			return value.ToGrid(size) * size;
		}
		public static Vector3Int ToGrid(this Vector3 value, Vector3 size)
		{
			return new Vector3Int(value.x.ToGrid(size.x), value.y.ToGrid(size.y), value.z.ToGrid(size.z));
		}
		public static Vector3 GridFixed(this Vector3 value, Vector3 size)
		{
			return new Vector3(value.x.GridFixed(size.x), value.y.GridFixed(size.y), value.z.GridFixed(size.z));
		}
		public static Vector3 GetCenter(this Transform transform)
		{
			return transform.GetBounds().center;
		}
		public static void SetCenter(this Transform transform, Vector3 center)
		{
			var offset = transform.GetCenter() - transform.position;
			transform.position = center - offset;
		}
		public static Vector3 GridFixed(this Transform transform,Vector3 gridSize)
		{
			var bounds = transform.GetBounds();
			transform.SetCenter((bounds.center + bounds.size / 2).GridFixed(bounds.size.GridFixed(gridSize)) - bounds.size / 2);
			transform.position = transform.position.GridFixed(gridSize);
			return transform.position;
		}
		public static Vector3Int ToGrid(this Transform transform,Vector3 gridSize)
		{
			return transform.GridFixed(gridSize).ToGrid(gridSize);
		}


		public static void AddAssetObject(this UnityEngine.Object obj, UnityEngine.Object childObj)
		{
#if UNITY_EDITOR
			if (obj != null && !Application.IsPlaying(obj) && obj.IsAsset())
			{
				UnityEditor.AssetDatabase.AddObjectToAsset(childObj, obj);
				SetDirty(obj);
				UnityEditor.AssetDatabase.SaveAssetIfDirty(obj);
			}
#endif
		}
		public static void RemoveAssetObject(this UnityEngine.Object obj, UnityEngine.Object childObj)
		{
#if UNITY_EDITOR
			if (obj != null && !Application.IsPlaying(obj) && obj.IsAsset())
			{
				UnityEditor.AssetDatabase.RemoveObjectFromAsset(childObj);
				SetDirty(obj);
				UnityEditor.AssetDatabase.SaveAssetIfDirty(obj);
			}
#endif
		}
		public static void SetDirty(this UnityEngine.Object obj)
		{
#if UNITY_EDITOR
			if (obj != null && !Application.IsPlaying(obj))
			{
				UnityEditor.EditorUtility.SetDirty(obj);
			}
#endif
		}
		public static void Record(this UnityEngine.Object obj)
		{
#if UNITY_EDITOR
			UnityEditor.Undo.RecordObject(obj, "RecordObj" + obj.GetHashCode());
#endif
		}

		public static T CheckInstantiate<T>(this T prefab, Transform parent = null) where T : UnityEngine.Object
		{

#if UNITY_EDITOR
			if (!Application.isPlaying || (parent != null && !Application.IsPlaying(parent)))
			{
				var obj = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as T;
				return obj;
			}
			else
#endif
			{
				var obj = GameObject.Instantiate(prefab, parent);
				return obj;
			};
		}

		public static void CheckDestory(this UnityEngine.Object obj)
		{
			if (obj == null) return;
#if UNITY_EDITOR
			if (!Application.isPlaying || !Application.IsPlaying(obj))
			{
				try
				{
					GameObject.DestroyImmediate(obj);
				}
				catch (System.Exception e)
				{
					if (obj is GameObject gameObject)
					{
						Debug.LogError("删除物体出错 " + gameObject.transform.GetPath() + "  " + e);
					}
				}
			}
			else
#endif
			{
				GameObject.Destroy(obj);
			}
		}
		public static void ClearChild(this Transform transform)
		{
			for (int i = transform.childCount - 1; i >= 0; i--)
			{
				var child = transform.GetChild(i).gameObject;
				child.CheckDestory();
			}
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
					var infoAsync = process.StandardOutput.ReadToEndAsync();
					var errorAsync = process.StandardError.ReadToEndAsync();
					var info= infoAsync.Result;
					var error = errorAsync.Result;
#if UNITY_EDITOR
					if (!Application.isPlaying)
					{
						UnityEditor.EditorUtility.ClearProgressBar();
					}
#endif
					if (!error.IsNull())
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

	public static class QSceneTool
	{
		public static List<Task> PreLoadList = new List<Task>();
		public static async Task LoadSceneAsync(this string sceneName)
		{
			IsLoading = true;
			await PreLoadList.ToArray().WaitAllOver();
			PreLoadList.Clear();
			QDebug.Log("异步加载场景开始[" + sceneName + "]");
			QDebug.Begin("异步加载场景结束[" + sceneName + "]");
			await SceneManager.LoadSceneAsync(sceneName);
			QId.InitSceneId();
			GCCollect();
			QDebug.End("异步加载场景结束[" + sceneName + "]");
			await PreLoadList.ToArray().WaitAllOver();
			PreLoadList.Clear();
			IsLoading = false;
		}
		public static bool IsLoading { get; private set; } = false;
		public static async Task WaitLoading()
		{
			await QTask.Wait(() => !IsLoading);
		}
		public static void GCCollect()
		{
			Resources.UnloadUnusedAssets();
			Task.Run(GC.Collect);
		}
	}

}
