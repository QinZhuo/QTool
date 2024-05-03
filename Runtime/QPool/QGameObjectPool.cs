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

	/// <summary>
	/// GameObject对象池
	/// </summary>
	public class QGameObjectPool : QInstanceManager<QGameObjectPool>
	{
		static Dictionary<string, ObjectPool<GameObject>> GameObjectPools = new Dictionary<string, ObjectPool<GameObject>>();
		protected override void Awake()
		{
			base.Awake();
			QEventManager.Register(QEventKey.游戏退出, GameObjectPools.Clear);
		}
		internal static ObjectPool<GameObject> GetPool(string key, Func<GameObject> createFunc, Action<GameObject> OnGet = null, Action<GameObject> actionOnRelease = null, Action<GameObject> actionOnDestroy = null, int maxSize = 10000)
		{
			if (!GameObjectPools.TryGetValue((string)key, out var pool))
			{
				if (createFunc == null)
				{
					throw new Exception((string)("不存在对象池[" + key + "]" + typeof(GameObject)));
				}
				Action<GameObject> OnRelease = actionOnRelease;
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
				GameObjectPools[(string)key] = new ObjectPool<GameObject>(createFunc, OnGet, OnRelease, actionOnDestroy, true, 10, maxSize);
				QEventManager.Register(QEventKey.卸载场景, (Action)GameObjectPools[(string)key].Clear);
			}
			return pool;
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
		public static GameObject Get(GameObject prefab, Transform parent = null)
		{
			var pool = GetPool(prefab.name + "_" + prefab.GetHashCode(), prefab);
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
			var tag = gameObject.GetComponent<QPoolObject>();
			try
			{
				if (tag != null && GameObjectPools.TryGetValue(tag.poolName, out var pool))
				{
					pool.Release(gameObject);
					return;
				}
			}
			catch (Exception e)
			{
				Debug.LogError("回收【" + gameObject + "】出错 " + e);
			}
			Destroy(gameObject);
		}
	}

	/// <summary>
	/// 简单类型对象池
	/// </summary>
	public class QObjectPool<T> where T : class, new()
	{
		internal static ObjectPool<T> Instance { get; private set; }
		static QObjectPool()
		{
			if (typeof(T).Is(typeof(IQPoolObject)))
			{
				Instance = new ObjectPool<T>(() => new T(), obj => (obj as IQPoolObject).OnPoolGet(), obj => (obj as IQPoolObject).OnPoolRelease());
			}
			else
			{
				Instance = new ObjectPool<T>(() => new T());
			}
			QEventManager.Register(QEventKey.游戏退出, Instance.Clear);
		}
		public static T Get()
		{
			return Instance.Get();
		}
		public static void Release(T obj)
		{
			Instance.Release(obj);
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
			QGameObjectPool.Release(poolObj);
		}
	}
}
