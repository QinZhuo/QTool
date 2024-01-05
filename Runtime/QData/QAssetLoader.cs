using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace QTool
{
	public abstract class QAssetLoader<TPath, TObj> : MonoBehaviour where TObj : UnityEngine.Object
	{
		[UnityEngine.Serialization.FormerlySerializedAs("OnLoad")]
		public UnityEvent<TObj> OnLoadEvent = new UnityEvent<TObj>();
		[QName("刷新")]
		public void Fresh()
		{
			InvokeLoad(name);
		}
		public virtual void OnLoad(TObj obj)
		{
			OnLoadEvent?.Invoke(obj);
		}
		public void InvokeLoad(string key)
		{
			var obj = Load(key);
			if (obj == null)
			{
				obj = Load(DirectoryPath);
			}
			if (obj != null)
			{
				OnLoad(obj);
			}
		}
		public async void InvokeLoadAsync(string key)
		{
			var obj = await LoadAsync(key);
			if (obj == null)
			{
				obj = await LoadAsync(DirectoryPath);
			}
			if (obj != null)
			{
				OnLoad(obj);
			}
		}
		public static string DirectoryPath
		{
			get
			{
				return typeof(TPath).Name;
			}
		}
		public static TObj[] LoadAll()
		{
			return Resources.LoadAll<TObj>(DirectoryPath);
		}
		public static TObj Load(string key)
		{
			if (key.IsNull()) return null;
			key = key.Replace('\\', '/');
			return Resources.Load<TObj>(DirectoryPath + "/" + key);
		}
		public static async Task<TObj> LoadAsync(string key)
		{
			if (key.IsNull()) return null;
			key = key.Replace('\\', '/');
			return await Resources.LoadAsync<TObj>(DirectoryPath + "/" + key) as TObj; 
		}
	}
	public abstract class QPrefabLoader<TPath> : QAssetLoader<TPath, GameObject> where TPath : QPrefabLoader<TPath>
	{
		public static GameObject PoolGet(string key, Transform parent = null)
		{
			return QPoolManager.Get(DirectoryPath + "_" + key, Load(key), parent);
		}
	}
}


