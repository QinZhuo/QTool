using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace QTool
{
	public abstract class QAssetLoader<TPath, TObj> : MonoBehaviour where TObj : UnityEngine.Object
	{
		public UnityEvent<TObj> OnLoad;
		public void InvokeLoad(string key)
		{
			var obj = Load(key);
			if (obj != null)
			{
				OnLoad?.Invoke(obj);
			}
		}
		public async void InvokeLoadAsync(string key)
		{
			var obj = await LoadAsync(key);
			if (obj != null)
			{
				OnLoad?.Invoke(obj);
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


