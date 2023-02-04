using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[System.Serializable]
	public class QIdObject
	{
		public string id;
		Object _obj;
		public Object Object
		{
			get
			{
				if (_obj == null)
				{
					_obj = GetObject(id, typeof(Object));
				}
				return _obj;
			}
		}

		public T Get<T>() where T : Object
		{
			return GetObject<T>(id);
		}

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
		public static string GetId(Object obj)
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
				if (!Application.isPlaying&&obj.IsAsset())
				{
					InitCache();
					if (!AssetIdCache.ContainsKey(obj))
					{
						var key = UnityEditor.AssetDatabase.GetAssetPath(obj);
						AssetIdCache.Add(obj, key);
						AssetObjectCache.Add(key, obj);
						if (key.Contains("/" + nameof(Resources) + "/") || key.StartsWith(nameof(Resources) + "/"))
						{
							return key;
						}
						var qidr = ScriptableObject.CreateInstance<QIdReference>();
						qidr.obj = obj;
						var path = "Assets/Resources/" + nameof(QIdReference) + "/" + key + ".asset";
						path = QFileManager.CheckDirectoryPath(path);
						UnityEditor.AssetDatabase.CreateAsset(qidr, path);
						if (!Application.isPlaying)
						{
							UnityEditor.AssetDatabase.Refresh();
						}
						QDebug.Log("生成对象 " + obj.GetType() + " 引用Id：" + key + " 文件路径:" + path);

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
						if (!Application.isPlaying && !gameObj.activeSelf&& QSceneObjectSetting.Instance!=null)
						{
							QSceneObjectSetting.Instance.qIdInitList.AddCheckExist(qId);
						}
						return qId.Id;
					}
				}
			}
			return "";
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
				if (Tool.IsBuilding) return obj;
#endif
				string loadPath = null;
				if (id.Contains("/" + nameof(Resources) + "/"))
				{
					loadPath = id.SplitEndString("/" + nameof(Resources) + "/");
					if (loadPath.Contains("."))
					{
						loadPath = loadPath.WithoutExtension();
					}
				}
				else
				{
					loadPath = nameof(QIdReference) + "/" + id;
				}

				obj = Resources.Load(loadPath);
				if (obj == null && id.Contains("_"))
				{
					obj = GetObject(id.SplitStartString("_"), type);
					if (obj != null)
					{
						return obj;
					}
				}
				else if (obj is QIdReference qidr)
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
						GetId(obj);
						return obj;
					}
				}
#endif
			}
			Debug.LogWarning("找不到[" + id + "]对象");
			return null;
		}
		public static T GetObject<T>(string id) where T : Object
		{
			return GetObject(id, typeof(T)) as T;
		}

	}
}
