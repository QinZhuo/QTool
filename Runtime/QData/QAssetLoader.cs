using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace QTool.Asset
{
	#region AddressableTool

#if Addressables
	using UnityEngine.AddressableAssets;
	using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
	using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets;
    using UnityEditor;
    using System.IO;
	public static  class AddressableTool
    {
        public static QDictionary<string, List<AddressableAssetEntry>> labelDic = new QDictionary<string, List<AddressableAssetEntry>>();
        public static QDictionary<string, AddressableAssetGroup> groupDic = new QDictionary<string, AddressableAssetGroup>();
        public static QDictionary<string, AddressableAssetEntry> entryDic = new QDictionary<string, AddressableAssetEntry>();
        public static void SetAddresableGroup(string assetPath, string groupName, string key = "")
        {
            var group = GetGroup(groupName);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = entryDic.ContainsKey(guid) ? entryDic[guid] : AssetSetting.FindAssetEntry(guid);
          
            if (entry == null)
            {
                entry = AssetSetting.CreateOrMoveEntry(guid, group);
                if (entry == null)
                {
                    Debug.LogError("生成资源【" + key + "】出错：" + assetPath);
                    return;
                }
            }
            else if (entry.parentGroup != group)
            {
                AssetSetting.MoveEntry(entry, group);
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                entry.address = Path.GetFileNameWithoutExtension(assetPath);
            }
            else if (entry.address != key)
            {
                entry.address = key;
            }
            if (!entry.labels.Contains(groupName))
            {
             //   entry.labels.Clear();
                entry.SetLabel(groupName, true, true);
            }
            EditorUtility.SetDirty(AssetSetting);
            EditorUtility.SetDirty(group);
        }
        public static AddressableAssetSettings AssetSetting
        {
            get
            {
                return AddressableAssetSettingsDefaultObject.Settings;
            }
        }
		public static List<T> GetLabelList<T>(string label) where T : UnityEngine.Object
		{
            if (labelDic[label] == null)
            {
                labelDic[label] = new List<AddressableAssetEntry>();
            }
            if (AssetSetting != null)
            {

                labelDic[label].Clear();
                foreach (var group in AssetSetting.groups)
                {
                    if (group == null) continue;
                    foreach (var item in group.entries)
                    {
						if(item.TargetAsset is T)
						{
							if (item.labels.Contains(label))
							{
								labelDic[label].Add(item);
							}
						}
                    }
                }
            }
			List<T> objList = new List<T>();
			foreach (var entry in labelDic[label])
			{
				objList.Add(entry.TargetAsset as T);
			}
            return objList;
      
        }
        public static AddressableAssetGroup GetGroup(string groupName)
        {
            var group = groupDic[groupName];
            if (group == null)
            {
                group = AssetSetting.FindGroup(groupName);
                if (group == null)
                {
                    group = AssetSetting.CreateGroup(groupName, false, false, false, new List<AddressableAssetGroupSchema>
                    {AssetSetting.DefaultGroup.Schemas[0],AssetSetting.DefaultGroup.Schemas[1] }, typeof(System.Data.SchemaType));
                }
                else
                {
                    foreach (var e in group.entries)
                    {
                        entryDic[e.guid] = e;
                    }
                }
            }
            return group;
        }
    }
#endif

#endif

	#endregion
	public abstract class QAssetLoader<TPath, TObj> where TObj : UnityEngine.Object
	{
		public static string DirectoryPath
		{
			get
			{
				return typeof(TPath).Name;
			}
		}
		static QDictionary<TObj,int> ResoucesList = new QDictionary<TObj, int>();
#if Addressables
		static AsyncOperationHandle<IList<TObj>> AllLoader = default;
#endif
		public static async Task LoadAllAsync(List<TObj> assetList)
		{
			assetList.Clear();
			assetList.AddRange(Resources.LoadAll<TObj>(DirectoryPath));
			foreach (var obj in assetList)
			{
				ResoucesList[obj]++;
			}
			#region Addressables
#if Addressables
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				try
				{
					assetList.AddRange(AddressableTool.GetLabelList<TObj>(DirectoryPath.Replace('\\', '/')));
				}
				catch (Exception e)
				{
					Debug.LogError("加载资源表[" + DirectoryPath.Replace('\\', '/') + "]出错\n" + e);
				}
			}
			else
#endif
			{
				try
				{
					AllLoader = Addressables.LoadAssetsAsync<TObj>(DirectoryPath, null); ;
					if (AllLoader.OperationException != null)
					{
						throw AllLoader.OperationException;
					}
					var loaderTask = AllLoader.Task;
					var list = await loaderTask;
					if (loaderTask.Exception != null)
					{
						throw loaderTask.Exception;
					}
					assetList.AddRange( list);
					list.Clear();
				}
				catch (Exception e)
				{
					Debug.LogWarning("加载资源表[" + DirectoryPath + "]出错\n" + e);
				}
			}
#endif
			#endregion
			QDebug.Log("加载 [" + DirectoryPath + "][" + typeof(TObj) + "] 资源：\n" + assetList.ToOneString());
		
		}
#if UNITY_EDITOR
		public static List<TObj> GetEditorList()
		{
			List<TObj> assetList = new List<TObj>();
			assetList.Clear();
			assetList.AddRange(Resources.LoadAll<TObj>(DirectoryPath));
#if Addressables
			assetList.AddRange( AddressableTool.GetLabelList<TObj>(DirectoryPath.Replace('\\', '/')));
#endif
			return assetList;
		}
#endif
		public static async Task<TObj> LoadAsync(string key)
		{
			TObj obj = null;
			key = key.Replace('\\', '/');
#if Addressables
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				obj = AssetDatabase.LoadAssetAtPath<TObj>("AddressableResources/" + DirectoryPath + "/" + key);
			}
			else

#endif
			{
				var loader = Addressables.LoadAssetAsync<TObj>(DirectoryPath + "/" + key);
				obj = await loader.Task;
				if (loader.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
				{
					if (loader.OperationException != null)
					{
						Debug.LogError("异步加载" + DirectoryPath + "/" + key + "出错" + loader.OperationException);
					}
				}
			}
#else
			obj= Resources.Load<TObj>(DirectoryPath + "/" + key);
			ResoucesList[obj]++;
#endif
			return obj;
		}

#pragma warning disable CS1998 
		public static async void ReleaseAll()
		{
			foreach (var kv in ResoucesList)
			{
				if (!(kv.Key is GameObject))
				{
					Resources.UnloadAsset(kv.Key);
				}
			}
			ResoucesList.Clear();
#if Addressables

			if (!AllLoader.Equals(default))
			{
				Addressables.Release(AllLoader);
			}
#endif

		}
		public static void Release(TObj obj)
		{
			if (ResoucesList.ContainsKey(obj))
			{
				ResoucesList[obj]--;
				if (ResoucesList[obj]<=0)
				{
					if(!(obj is GameObject))
					{
						Resources.UnloadAsset(obj);
					}
					ResoucesList.Remove(obj);
				}
			}
#if Addressables
			else if(obj!=null)
			{
				Addressables.Release(obj);
			}
#endif
		}
		

	}
	public abstract class QPrefabLoader<TPath> : QAssetLoader<TPath, GameObject> where TPath : QPrefabLoader<TPath>
	{
		static async Task<ObjectPool<GameObject>> GetPool(string key)
		{
			var poolKey = DirectoryPath + "_" + key;
			var pool = QPoolManager.GetPool<GameObject>(key) as GameObjectPool;
			if (pool == null||pool.prefab==null)
			{
				var prefab = await LoadAsync(key);
				if (prefab != null)
				{
					pool= QPoolManager.GetPool(poolKey, prefab);
					pool.OnRelease += () =>
					{
						Release(prefab);
					};
				}
			}
			return pool;
		}
		public static async Task<GameObject> PoolGet(string key, Transform parent = null)
		{
			var pool = await GetPool(key);
			if (pool == null)
			{
				Debug.LogError("无法实例化预制体[" + key + "]");
				return null;
			}
			try
			{
				var obj = pool.Get();
				if (obj == null)
				{
					return null;
				}
				if (parent != null)
				{
					obj.transform.SetParent(parent, false);
				}
				obj.name = key;
				return obj;
			}
			catch (Exception e)
			{
				Debug.LogError("尝试获取对象【"+key+"】出错 :" + e);
				return null;
			}
		}
		public static bool PoolPush(string key, GameObject obj)
		{
			if (key.Contains(" "))
			{
				key = key.Substring(0, key.IndexOf(" "));
			}
			var pool= QPoolManager.GetPool<GameObject>(DirectoryPath + "_" + key) as GameObjectPool;
			if (pool == null)
			{
				Debug.LogError("不存在对象池【" + DirectoryPath + "_" + key + "】");
				return false;
			}
			return pool.Push(obj);
		}


		public static async Task AddressablesPreviewLoad(string key, Transform parent = null)
		{
			var previewObj = await PoolGet(key, parent);
			try
			{
				if (parent == null)
				{
					var pos = Camera.main.transform.forward * -100 + Camera.main.transform.position;
					previewObj.transform.position = pos;
				}
				if (!await QTask.Wait(0.1f, true).IsCancel())
				{
					PoolPush(key,previewObj);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("预加载[" + key + "]出错 " + e);
			}
		}
	}
}


