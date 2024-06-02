using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using QTool.Reflection;
namespace QTool
{
	[System.Serializable]
	public class QIdObject
	{
		public string id;
		[SerializeField,HideInInspector,QIgnore]
		private Object _Object;
		public Object Object
		{
			get
			{  
				if (_Object == null) 
				{
					_Object = QIdTool.GetObject(id,typeof(Object));
				}
				return _Object;
			}
		}

		public T Get<T>() where T : Object
		{
			return QIdTool.GetObject<T>(id);
		}
	}
	public static class QIdTool
	{
#if UNITY_EDITOR
		public static QDictionary<string, Object> AssetObjectCache = new QDictionary<string, Object>();
		public static QDictionary<Object, string> AssetIdCache = new QDictionary<Object, string>();
		static bool CacheInitOver = false;
		static void InitCache()
		{
			if (CacheInitOver) return;
			CacheInitOver = true;
			var objs = Resources.LoadAll(nameof(QIdReference));
			foreach (var obj in objs)
			{
				if (obj is QIdReference qidr)
				{
					try
					{
						var id = UnityEditor.AssetDatabase.GetAssetPath(qidr.obj);
						AssetObjectCache.Add(id, qidr.obj);
						AssetIdCache.Add(qidr.obj, id);
					}
					catch (System.Exception e)
					{
						Debug.LogError("缓存资源 " + qidr + " 出错:\n" + e);
					}
				}
			}
		}
#endif
		private const string ResourcesKey = "/" + nameof(Resources) + "/";
		public static string GetQId(Object obj)
		{
			if (obj != null)
			{
				if (obj is GameObject gObj)
				{
					obj = gObj;
				}
				else if (obj is Component monoObj)
				{
					obj = monoObj.gameObject;
				}
#if UNITY_EDITOR
				if (!QTool.IsBuilding && obj.IsAsset())
				{
					InitCache();
					if (!AssetIdCache.ContainsKey(obj))
					{
						var key = UnityEditor.AssetDatabase.GetAssetPath(obj);
						if (key.Contains(ResourcesKey))
						{
							key = ResourcesKey + key.SplitEndString(ResourcesKey);
						}
						else
						{
							var qidr = ScriptableObject.CreateInstance<QIdReference>();
							qidr.obj = obj;
							var path = ResourcesKey + nameof(QIdReference) + "/" + key.WithoutExtension() + ".asset";
							path = QFileTool.CheckDirectoryPath(path);
							UnityEditor.AssetDatabase.CreateAsset(qidr, path);
							if (!Application.isPlaying)
							{
								UnityEditor.AssetDatabase.Refresh();
							}
							QDebug.Log("生成对象 " + obj.GetType() + " 引用Id：" + key + " 文件路径:" + path);
							key = ResourcesKey + path.SplitEndString(ResourcesKey);
						}
						AssetIdCache.Add(obj, key);
						AssetObjectCache.Add(key, obj);
					}
					return AssetIdCache[obj];
				}
				else
#endif
				{
					if (obj is GameObject gameObj)
					{
						var qId = gameObj.GetComponent<QId>();
						if (qId == null)
						{
							qId = gameObj.AddComponent<QId>();
							gameObj.SetDirty();
						}
						return qId.Id;
					}
				}
			}
			return "";
		}
		internal static async Task<Object> LoadObjectAsync(string key, System.Type type)
		{
			var id = key;
			if (id.IsNull()) return null;
			if (id.StartsWith("{") && id.EndsWith("}"))
			{
				id = id.GetBlockValue(":\"", "\"}");
			}
			var path = GetResourcesPath(id);
			var obj = await Resources.LoadAsync(path);
			if (obj is QIdReference idReference)
			{
				obj = idReference.obj;
			}
			if (obj != null && !obj.GetType().Is(type))
			{
				obj = await Resources.LoadAsync(path, type);
			}
			if (obj == null && !id.IsNull() && id != "null")
			{
				Debug.LogError("异步加载资源[" + key + "](" + type + ")失败 资源为空");
			}
			return obj;
		}
		private static string GetResourcesPath(string id)
		{
			string loadPath = null;
			if (id.Contains(ResourcesKey))
			{
				loadPath = id.SplitEndString("/" + nameof(Resources) + "/");
				if (loadPath.Contains("."))
				{
					loadPath = loadPath.WithoutExtension();
				}
			}
			else
			{
				loadPath = nameof(QIdReference) + "/" + id.WithoutExtension();
			}
			return loadPath;
		}
		public static Object GetObject(string id, System.Type type)
		{

			if (typeof(Component).IsAssignableFrom(type))
			{
				return GetObject<GameObject>(id)?.GetComponent(type);
			}
			if (string.IsNullOrWhiteSpace(id)) return null;
			if (QId.InstanceIdList.ContainsKey(id) && QId.InstanceIdList[id] != null)
			{
				return QId.InstanceIdList[id].gameObject;
			}
			else
			{

				Object obj = null;
#if UNITY_EDITOR
				if (QTool.IsBuilding) return obj;
#endif
				var loadPath = GetResourcesPath(id);
				obj = Resources.Load(loadPath);

				if (obj is QIdReference qidr)
				{
					obj = qidr.obj;
				}

				if (obj != null)
				{
					return obj;
				}
#if UNITY_EDITOR
				if (id.StartsWith("Assets"))
				{
					Debug.LogError("未找到【" + id + "】[" + loadPath + "]");
					obj = UnityEditor.AssetDatabase.LoadAssetAtPath(id, type);
					if (obj != null)
					{
						GetQId(obj);
						return obj;
					}
				}
#endif
			}
			QDebug.LogWarning("找不到[" + id + "]对象");
			return null;
		}
		public static T GetObject<T>(string id) where T : Object
		{
			return GetObject(id, typeof(T)) as T;
		}
	}
}