using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace QTool.Asset
{
	public abstract class QAssetLoader<TPath, TObj> where TObj : UnityEngine.Object
	{
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


