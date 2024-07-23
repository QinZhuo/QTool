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
				if (obj.IsAsset())
				{
					var key = UnityEditor.AssetDatabase.GetAssetPath(obj);
					if (key.Contains(ResourcesKey)) {
						key = ResourcesKey + key.SplitEndString(ResourcesKey);
					}
					else {
						throw new System.Exception($"无法引用非{ResourcesKey}目录下的文件");
					}
					return key;
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
		private static string GetResourcesPath(string id) {
			string loadPath = null;
			if (id.Contains(ResourcesKey)) {
				loadPath = id.SplitEndString(ResourcesKey);
				if (loadPath.Contains(".")) {
					loadPath = loadPath.WithoutExtension();
				}
			}
			else {
				loadPath = id.WithoutExtension();
			}
			return loadPath;
		}
		public static Object GetObject(string id, System.Type type) {

			if (typeof(Component).IsAssignableFrom(type)) {
				return GetObject<GameObject>(id)?.GetComponent(type);
			}
			if (string.IsNullOrWhiteSpace(id))
				return null;
			if (QId.InstanceIdList.ContainsKey(id) && QId.InstanceIdList[id] != null) {
				return QId.InstanceIdList[id].gameObject;
			}
			else {

				Object obj = null;
#if UNITY_EDITOR
				if (QTool.IsBuilding)
					return obj;
#endif
				var loadPath = GetResourcesPath(id);
				obj = Resources.Load(loadPath);
				if (obj != null) {
					return obj;
				}
				QDebug.LogWarning("未找到【" + id + "】[" + loadPath + "]");
			}
			return null;
		}
		public static T GetObject<T>(string id) where T : Object
		{
			return GetObject(id, typeof(T)) as T;
		}
	}
}
