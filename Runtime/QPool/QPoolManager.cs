using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using QTool.Inspector;
using QTool.Reflection;
using System.Threading.Tasks;
#if UNITY_2021_1_OR_NEWER
using UnityEngine.Pool;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
#if !UNITY_2022_1_OR_NEWER
public class ObjectPool<T> where T : class
{
	public Func<T> CreateFunc { get; }
	public Action<T> ActionOnGet { get; }
	public Action<T> OnRelease { get; }
	public Action<T> ActionOnDestroy { get; }
	public bool V1 { get; }
	public int V2 { get; }
	public int MaxSize { get; }
	public Stack<T> Stack { get; } = new Stack<T>();

	public void Release(T obj)
	{
		OnRelease?.Invoke(obj);
		Stack.Push(obj);
	}

	public T Get()
	{
		if (Stack.Count > 0)
		{
			var obj= Stack.Pop();
			ActionOnGet?.Invoke(obj);
			return obj;
		}
		else
		{

			return CreateFunc();
		}
	}
	public ObjectPool(Func<T> createFunc, Action<T> actionOnGet, Action<T> onRelease, Action<T> actionOnDestroy, bool v1, int v2, int maxSize)
	{
		CreateFunc = createFunc;
		ActionOnGet = actionOnGet;
		OnRelease = onRelease;
		ActionOnDestroy = actionOnDestroy;
		V1 = v1;
		V2 = v2;
		MaxSize = maxSize;
	}
}
#endif
namespace QTool
{
	
    public class QPoolManager:QInstanceManager<QPoolManager>
    {
		public static ObjectPool<T> GetPool<T>(string poolName, Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnRelease = null, Action<T> actionOnDestroy = null, int maxSize = 10000) where T : class
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
					OnRelease += obj => (obj as IQPoolObject).OnDestroy();
				}
				else if (typeof(T).Is(typeof(GameObject)))
				{
					OnRelease += obj =>
					{
						foreach (var poolObj in (obj as GameObject).GetComponents<IQPoolObject>())
						{
							poolObj.OnDestroy();
						}
					};
				}
				Pools[key] = new ObjectPool<T>(createFunc, actionOnGet, OnRelease, actionOnDestroy, true, 10, maxSize);
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
				result.GetComponent<QPoolInfo>(true).poolName = poolName;
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

		public static TCom Get<TCom>(TCom prefab, Transform parent, Vector3 position = default, Quaternion rotation = default) where TCom : Component
		{
			return Get(prefab.gameObject, parent, position, rotation).GetComponent<TCom>();
		}
		public static TCom Get<TCom>(TCom prefab, Vector3 position = default, Quaternion rotation = default) where TCom : Component
		{
			return Get(prefab.gameObject, position, rotation).GetComponent<TCom>();
		}
		public static GameObject Get(GameObject prefab, Transform parent, Vector3 position = default, Quaternion rotation = default)
		{
			var obj = Get(prefab, position, rotation);
			if (parent != null)
			{
				obj.transform.SetParent(parent, true);
			}
			return obj;
		}
		public static GameObject Get(GameObject prefab, Vector3 position = default, Quaternion rotation = default)
		{
			var obj = GetPool(prefab.name, prefab).Get();
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
		public static GameObject Get(string poolKey, GameObject prefab, Transform parent = null)
		{
			return GetPool(poolKey, prefab).Get();
		}
		public static void Release(GameObject gameObject)
        {
			var tag= gameObject.GetComponent<QPoolInfo>();
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
	}
  
	public interface IQPoolObject
	{
		void OnDestroy();
	}
	public static class QPoolTool
	{
		public static void PoolRelease(this GameObject poolObj)
		{
			QPoolManager.Release(poolObj);
		}
	}
}
