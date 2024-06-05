using System;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
using UnityEngine.Pool;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool {

	/// <summary>
	/// GameObject对象池
	/// </summary>
	public class QGameObjectPool : QInstanceManager<QGameObjectPool> {
		private static Dictionary<GameObject, ObjectPool<GameObject>> GameObjectPools = new Dictionary<GameObject, ObjectPool<GameObject>>();
		protected override void Awake() {
			base.Awake();
			QEventManager.Register(QEventKey.游戏退出, GameObjectPools.Clear);
		}
		public static ObjectPool<GameObject> GetPool(GameObject prefab, int maxSize = 1000) {
			if (GameObjectPools.TryGetValue(prefab, out var pool)) {
				return pool;
			}
			else {
				pool = new ObjectPool<GameObject>(() => {
					var result = Instantiate(prefab);
					result.name = prefab.name;
					result.GetComponent<QPoolObject>(true).prefab = prefab;
					return result;
				}, obj => {
					obj.transform.SetParent(null, false);
					obj.transform.localScale = prefab.transform.localScale;
					obj.transform.position = prefab.transform.position;
					obj.transform.rotation = prefab.transform.rotation;
					var rect = prefab.transform.RectTransform();
					if (rect != null) {
						var newRect = obj.transform.RectTransform();
						newRect.anchorMin = rect.anchorMin;
						newRect.anchorMax = rect.anchorMax;
						newRect.anchoredPosition = rect.anchoredPosition;
					}
					foreach (var poolObj in obj.GetComponents<IQPoolObject>()) {
						poolObj.OnPoolGet();
					}
					obj.SetActive(true);
				}, obj => {
					foreach (var poolObj in obj .GetComponents<IQPoolObject>()) {
						poolObj.OnPoolRelease();
					}
					obj.SetActive(false);
					obj.transform.SetParent(Instance.transform, true);
				}, obj => {
					Destroy(obj);
				}, true, 10, maxSize);
				GameObjectPools[prefab] = pool;
				QEventManager.Register(QEventKey.卸载场景, pool.Clear);
				return pool;
			}
		}
		public static GameObject Get(GameObject prefab) {
			return GetPool(prefab).Get();
		}
		public static GameObject Get(GameObject prefab, Transform parent = null) {
			var pool = GetPool(prefab);
			var obj = pool.Get();
			while (obj == null) {
				obj = pool.Get();
			}
			if (parent != null) {
				obj.transform.SetParent(parent, false);
				obj.transform.localPosition = prefab.transform.localPosition;
				obj.transform.localRotation = prefab.transform.localRotation;
				obj.transform.localScale = prefab.transform.localScale;
			}
			return obj;
		}
		public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation = default) {
			var obj = Get(prefab);
			if (position != default) {
				obj.transform.position = position;
			}
			if (rotation != default) {
				obj.transform.rotation = rotation;
			}
			return obj;
		}
		public static void Release(GameObject gameObject) {
			var tag = gameObject.GetComponent<QPoolObject>();
			try {
				if (tag != null && GameObjectPools.TryGetValue(tag.prefab, out var pool)) {
					pool.Release(gameObject);
					return;
				}
			}
			catch (Exception e) {
				Debug.LogError("回收【" + gameObject + "】出错 " + e);
			}
			Destroy(gameObject);
		}
	}

	/// <summary>
	/// 简单类型对象池
	/// </summary>
	public class QObjectPool<T> where T : class, new() {
		internal static ObjectPool<T> Instance { get; private set; }
		static QObjectPool() {
			if (typeof(T).Is(typeof(IQPoolObject))) {
				Instance = new ObjectPool<T>(() => new T(), obj => (obj as IQPoolObject).OnPoolGet(), obj => (obj as IQPoolObject).OnPoolRelease());
			}
			else {
				Instance = new ObjectPool<T>(() => new T());
			}
			QEventManager.Register(QEventKey.游戏退出, Instance.Clear);
		}
		public static T Get() {
			return Instance.Get();
		}
		public static void Release(T obj) {
			Instance.Release(obj);
		}
	}
	public interface IQPoolObject {
		void OnPoolGet();
		void OnPoolRelease();
	}
	public static class QPoolTool {
		public static void PoolRelease(this GameObject poolObj) {
			QGameObjectPool.Release(poolObj);
		}
		public static void PoolReleaseList(this IList<GameObject> poolObjList) {
			foreach (var poolObj in poolObjList) {
				QGameObjectPool.Release(poolObj);
			}
			poolObjList.Clear();
		}
	}
}
