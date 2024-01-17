using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using QTool.Inspector;
using QTool.Reflection;
using System.Threading.Tasks;
using UnityEngine.Pool;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{
	
    public class QPoolManager:QInstanceManager<QPoolManager>
    {
		public static ObjectPool<T> GetPool<T>(string poolName, Func<T> createFunc, Action<T> OnGet = null, Action<T> actionOnRelease = null, Action<T> actionOnDestroy = null, int maxSize = 10000) where T : class
		{
			var key = poolName;
			if (string.IsNullOrEmpty(key))
			{
				key = typeof(T).ToString();
			}
			var Pools = QPoolManager<T>.Pools;
			if (!Pools.ContainsKey(key))
			{
				if (createFunc == null)
				{
					throw new Exception("不存在对象池[" + key + "]" + typeof(T));
				}
				Action<T> OnRelease = actionOnRelease;
				if (typeof(T).Is(typeof(IQPoolObject)))
				{
					OnGet += obj => (obj as IQPoolObject).OnPoolGet();
					OnRelease += obj => (obj as IQPoolObject).OnPoolRelease();
				}
				else if (typeof(T).Is(typeof(GameObject)))
				{
					OnGet += obj =>
					{
						foreach (var poolObj in (obj as GameObject).GetComponents<IQPoolObject>())
						{
							poolObj.OnPoolGet();
						}
					};
					OnRelease += obj =>
					{
						foreach (var poolObj in (obj as GameObject).GetComponents<IQPoolObject>())
						{
							poolObj.OnPoolRelease();
						}
					};
				}
				Pools[key] = new ObjectPool<T>(createFunc, OnGet, OnRelease, actionOnDestroy, true, 10, maxSize);
				QEventManager.Register(QEventKey.卸载场景, Pools[key].Clear);
			}
			return Pools[key];
		}
		public static T Get<T>(string poolName, Func<T> createFunc = null, Action<T> actionOnGet = null, Action<T> actionOnRelease = null, Action<T> actionOnDestroy = null,int maxSize = 10000) where T : class
		{
			return GetPool(poolName, createFunc, actionOnGet, actionOnRelease, actionOnDestroy, maxSize).Get();
		}
		public static ObjectPool<GameObject> GetPool(string poolName, GameObject prefab, int maxSize = 1000)
		{
			return GetPool(poolName, () =>
			{
				var result = GameObject.Instantiate(prefab);
				result.name = prefab.name;
				result.GetComponent<QPoolObject>(true).poolName = poolName;
				return result;
			}, obj =>
			{
				obj.transform.SetParent(null, true);
				obj.transform.localScale = prefab.transform.localScale;
				obj.transform.position = prefab.transform.position;
				obj.transform.rotation = prefab.transform.rotation;
				var rect = prefab.transform.RectTransform();
				if (rect != null)
				{
					var newRect = obj.transform.RectTransform();
					newRect.anchorMin = rect.anchorMin;
					newRect.anchorMax = rect.anchorMax;
					newRect.anchoredPosition = rect.anchoredPosition;
				}
				obj.SetActive(true);
			},
			obj =>
			{
				obj.SetActive(false);
				obj.transform.SetParent(Instance.transform, true);
			}, obj =>
			{
				GameObject.Destroy(obj);
			}, maxSize);
		}
		public static GameObject Get(string poolName, GameObject prefab, int maxSize = 1000)
		{
			return GetPool(poolName, prefab, maxSize).Get();
		}
		public static void Release<T>(string poolName, T obj) where T : class
		{
			var Pools = QPoolManager<T>.Pools;
			if (Pools.ContainsKey(poolName))
			{
				Pools[poolName].Release(obj);
			}
			else
			{
				Debug.LogError("不存在对象池 " + obj);
			}
		}

		public static TCom Get<TCom>(TCom prefab, Transform parent) where TCom : Component
		{
			return Get(prefab.gameObject, parent).GetComponent<TCom>();
		}
		public static TCom Get<TCom>(TCom prefab, Vector3 position, Quaternion rotation = default) where TCom : Component
		{
			return Get(prefab.gameObject, position, rotation).GetComponent<TCom>();
		}
		public static GameObject Get(GameObject prefab, Transform parent = null)
		{
			var pool = GetPool(prefab.name, prefab);
			var obj = pool.Get();
			while (obj == null)
			{
				obj = pool.Get();
			}
			if (parent != null)
			{
				obj.transform.SetParent(parent, true);
				obj.transform.localPosition = Vector3.zero;
				obj.transform.rotation = Quaternion.identity;
			}
			return obj;
		}
		public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation = default)
		{
			var obj = Get(prefab);
			if (position != default)
			{
				obj.transform.position = position;
			}
			if (rotation != default)
			{
				obj.transform.rotation = rotation;
			}
			return obj;
		}
		public static void Release(GameObject gameObject)
        {
			var tag= gameObject.GetComponent<QPoolObject>();
			if (tag == null)
			{
				GameObject.Destroy(gameObject);
				return;
			}
			Release(tag.poolName, gameObject);
        }
		
	}
	public class QPoolManager<T> where T : class
	{
		public static QDictionary<string, ObjectPool<T>> Pools = new QDictionary<string, ObjectPool<T>>();
		static QPoolManager()
		{
			QEventManager.Register(QEventKey.游戏退出, Pools.Clear);
		}
	}

	public interface IQPoolObject
	{
		void OnPoolGet();
		void OnPoolRelease();
	}
	public static class QPoolTool
	{
		public static void PoolRelease(this GameObject poolObj)
		{
			QPoolManager.Release(poolObj);
		}
	}
}
