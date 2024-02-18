using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace QTool
{
	public abstract class QAssetLoader<TPath, TObj> : MonoBehaviour where TObj : UnityEngine.Object
	{
		[QName("开始时加载")]
		public bool startSetColor = false;
		[UnityEngine.Serialization.FormerlySerializedAs("OnLoad")]
		public UnityEvent<TObj> OnLoadEvent = new UnityEvent<TObj>();
		public virtual void Start()
		{
			if (startSetColor)
			{
				InvokeLoad(name);
			}
		}
		public virtual void OnLoad(TObj obj)
		{
			if (obj != null)
			{
				OnLoadEvent?.Invoke(obj);
			}
		}
		public void InvokeLoad(string key)
		{
			var obj = Load(key);
			if (obj == null)
			{
				obj = Load(DirectoryPath);
			}
			OnLoad(obj);
		}
		public async void InvokeLoadAsync(string key)
		{
			var obj = await LoadAsync(key);
			if (obj == null)
			{
				obj = await LoadAsync(DirectoryPath);
			}
			OnLoad(obj); 
		}
		public static string DirectoryPath
		{
			get
			{
				return typeof(TPath).Name.Replace('_', '/');
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
		public void InvokePoolGet(string key)
		{
			PoolGet(key, transform);
		}
		public static GameObject PoolGet(string key, Transform parent)
		{
			return QPoolManager.Get(Load(key), parent);
		}
	}
}


