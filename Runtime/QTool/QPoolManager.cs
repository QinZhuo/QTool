using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using QTool.Inspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{
	
    public class QPoolManager: InstanceManager<QPoolManager>
    {
		public static bool PoolActive => QOnPlayModeAttribute.CurrentrState != PlayModeState.EnteredEditMode;
		public static QDictionary<string, PoolBase> Pools = new QDictionary<string, PoolBase>();

		public static ObjectPool<T> GetPool<T>(string poolName, System.Func<T> newFunc = null) where T : class
        {
			
            var key = poolName;
            if (string.IsNullOrEmpty(key))
            {
                key = typeof(T).ToString();
            }
			if (Pools.ContainsKey(key))
			{
				if (Pools[key] is ObjectPool<T>)
				{
					var pool = Pools[key] as ObjectPool<T>;
					if (newFunc != null )
					{
						pool.newFunc = newFunc;
					}
					return pool;
				}
				else
				{
					throw new Exception("已存在重名不同类型对象池" + Pools[key]);
				}

			}
			else if(newFunc !=null&&PoolActive)
			{
				var type = typeof(T);
				if (type.IsSubclassOf(typeof(UnityEngine.Object))&&type!=typeof( GameObject))
				{
					throw new Exception("错误的对象池类型[" + type + "][" + poolName + "]");
				}
				var pool = type == typeof(GameObject)?new GameObjectPool(key,newFunc as Func<GameObject>) as ObjectPool<T>: new ObjectPool<T>(key, newFunc) ;
				lock (Pools)
				{
					Pools[key] = pool;
				}
				return pool;
			}
			else
			{
				return null;
			}
        }
		public static T Get<T>(string poolName, System.Func<T> newFunc = null) where T : class
		{
			return GetPool<T>(poolName, newFunc).Get();
		}
		public static bool Push<T>(string poolName, T obj) where T : class
		{
			if (Pools.ContainsKey(poolName))
			{
				return (Pools[poolName] as ObjectPool<T>).Push(obj);
			}
			else
			{
				Debug.LogError("不存在对象池 " + obj);
				return false;
			}
		}


		public static GameObjectPool GetPool(string poolKey, GameObject prefab)
		{
			var pool= GetPool(poolKey, () => {
				var result = GameObject.Instantiate(prefab);
				result.name = prefab.name;
				return result;
			}) as GameObjectPool;
			pool.prefab = prefab;
			return pool;
		}
		public static TCom Get<TCom>(TCom prefab, Transform parent, Vector3 position=default, Quaternion rotation = default) where TCom : Component
		{
			return Get(prefab.gameObject,parent,position,rotation).GetComponent<TCom>();
		}
		public static TCom Get<TCom>(TCom prefab,Vector3 position = default, Quaternion rotation = default) where TCom : Component
		{
			return Get(prefab.gameObject, position, rotation).GetComponent<TCom>();
		}
		public static GameObject Get(GameObject prefab, Transform parent , Vector3 position = default, Quaternion rotation = default)
		{
			var obj= Get(prefab, position, rotation);
			if (parent != null)
			{
				obj.transform.SetParent(parent, true);
			}
			return obj;
		}
		public static GameObject Get(GameObject prefab,Vector3 position=default,Quaternion rotation=default)
		{
			var obj= GetPool(prefab.name, prefab).Get();
			if(position!= default)
			{
				obj.transform.position = position;
			}
			if (rotation != default)
			{
				obj.transform.rotation = rotation;
			}
			return obj;
		}
		public static GameObject Get(string poolKey, GameObject prefab,Transform parent=null)
		{
			return GetPool(poolKey, prefab).Get();
		}
		public static bool Push(GameObject gameObject)
        {
            return Push(gameObject.name, gameObject);
        }
       
    }

    public abstract class PoolObject<T>:IPoolObject where T : PoolObject<T>,new()
    {
        internal static ObjectPool<T> _pool;
        public static ObjectPool<T> Pool
        {
            get
            {
                if (_pool == null)
                {
                   _pool= QPoolManager.GetPool(typeof(T).FullName, () => new T());
                }
                return _pool;
            }
        }
        public static T Get()
        {
			return Pool.Get();
		}
        public static void Push(T obj)
        {
			Pool.Push(obj);
		}
        public void Recover()
        {
            Push(this as T);
        }
        public abstract void OnPoolRecover();
    }
    public interface IPoolObject
    {
        void OnPoolRecover();
    }
    public abstract class PoolBase
    {
        public string Key { get; set; }
        public override string ToString()
        {
            var type = GetType();
            return "对象池["+ Key + "](" + type.Name + (type.IsGenericType ? "<" + type.GenericTypeArguments[0] + ">":"")+")";
        }
    }

    public class ObjectPool<T> : PoolBase where T : class
    {
		public override string ToString()
		{
			return base.ToString() + " using " + UsingPool.Count + "/" + (UsingPool.Count + CanUsePool.Count);
		}
		public readonly List<T> UsingPool = new List<T>();
        public readonly List<T> CanUsePool = new List<T>();
        public int AllCount
        {
            get
            {
                return UsingPool.Count + CanUsePool.Count;
            }
        }
        protected virtual T CheckGet(T obj)
        {

            
			OnGet?.Invoke(obj);
			UsingPool.AddCheckExist(obj);
			return obj;
        }

		public event Action<T> OnGet;
		public event Action<T> OnPush;
		protected virtual T CheckPush(T obj)
        {
			OnPush?.Invoke(obj);
			if (isPoolObj)
            {
                (obj as IPoolObject).OnPoolRecover();
            }
			UsingPool.Remove(obj);
			return obj;
        }
        protected T PrivateGet()
        {
			if (CanUsePool.Count > 0)
			{
				T obj = default;
				obj = CanUsePool.Dequeue();
				QDebug.ChangeProfilerCount(Key + " UseCount", AllCount - CanUseCount);
				return CheckGet(obj);
			}
			else
			{
				if (newFunc == null)
				{
					throw new Exception("对象池创建函数为空  " + this);
				}
				var obj = newFunc();
				QDebug.ChangeProfilerCount(Key + " "+nameof(AllCount), AllCount);
				return CheckGet(obj);
			}
        }
		public T Get(T obj = null)
		{
			if (obj != null && CanUsePool.Contains(obj))
			{

				CanUsePool.Remove(obj);
				return CheckGet(obj);
			}
			else
			{
				return PrivateGet();
			}
		}
		public bool Push(T obj)
		{

			if (obj == null || CanUsePool.Contains(obj)) return false;
			if (!UsingPool.Contains(obj))
			{
				if(obj is GameObject gameObj&&Application.isPlaying)
				{
					gameObj.transform.SetParent(null);
					gameObj.SetActive(false);
					Debug.LogWarning("对象[" + obj + "]在对象池[" + Key + "]中并不存在 无法回收 强制删除");
					GameObject.Destroy(gameObj);
				}
				else
				{
					Debug.LogWarning("对象[" + obj + "]在对象池[" + Key + "]中并不存在 无法回收");
				}
				return false;
			}
			var resultObj = CheckPush(obj);
			CanUsePool.Enqueue(resultObj);
			QDebug.ChangeProfilerCount(Key + " UseCount", AllCount - CanUseCount);
			return true;
		}
        public int CanUseCount
        {
            get
            {
                return CanUsePool.Count;
            }
        }
        public void Clear()
        {
			UsingPool.Clear();
			QDebug.ChangeProfilerCount(Key + " " + nameof(AllCount), AllCount);
			QDebug.ChangeProfilerCount(Key + " UseCount", AllCount - CanUseCount);
		}

        public Func<T> newFunc;
        public bool isPoolObj = false;
        public ObjectPool(string poolName,Func<T> newFunc=null)
        {
            var type = typeof(T);
            isPoolObj = typeof(IPoolObject).IsAssignableFrom(type);
            this.newFunc = newFunc;
            this.Key = poolName;

        }
	
    }

	public class GameObjectPool : ObjectPool<GameObject>
	{
		public GameObject prefab { get; internal set; }
		public GameObjectPool(string poolName, Func<GameObject> newFunc = null):base(poolName,newFunc)
		{
			OnGet += (obj) =>
			{
				obj.transform.localScale = prefab.transform.localScale;
				obj.transform.position = prefab.transform.position;
				obj.transform.rotation = prefab.transform.rotation;
				obj.SetActive(true);
			};
			OnPush += (obj) =>
			{
				if (obj == null||!QPoolManager.PoolActive) return;
				obj.SetActive(false);
				obj.transform.SetParent(PoolParent, true);
				foreach (var poolObj in obj.GetComponents<IPoolObject>())
				{
					poolObj.OnPoolRecover();
				}
			};
			SceneManager.sceneLoaded += OnSceneChange;
		}
		protected void OnSceneChange(Scene scene, LoadSceneMode mode)
		{
			if ( QPoolManager.PoolActive)
			{
				SceneManager.sceneLoaded -= OnSceneChange;
				if (prefab == null)
				{
					Release();
				}
			}
		}
		protected void Release()
		{
			UsingPool.RemoveNull();
			newFunc = null;
			prefab = null;
		}

		Transform _poolParent = null;
		public Transform PoolParent
		{
			get
			{
				if (_poolParent == null&&QPoolManager.PoolActive)
				{
					_poolParent = QPoolManager.Instance.transform.GetChild(Key, true);
				}
				return _poolParent;
			}
		}
		protected override GameObject CheckGet(GameObject obj)
		{
			if (obj==null)
			{
				UsingPool.Remove(obj);
				obj = PrivateGet();
			}
			return base.CheckGet(obj);
		}
	}
	public static partial class Tool
	{
		/// <summary>
		/// 对象池回收
		/// </summary>
		/// <param name="poolKey">对象池名</param>
		public static void PoolRecover(this GameObject poolObj, string poolKey)
		{
			QPoolManager.Push(poolKey, poolObj);
		}
		public static void PoolRecover(this GameObject poolObj)
		{
			QPoolManager.Push(poolObj);
		}
	}
}
