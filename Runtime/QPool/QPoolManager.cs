using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using QTool.Inspector;
using QTool.Reflection;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{
	
    public class QPoolManager: InstanceManager<QPoolManager>
    {
		public static QDictionary<string, QPool> Pools = new QDictionary<string, QPool>();

		public static QObjectPool<T> GetPool<T>(string poolName, System.Func<T> newFunc = null) where T : class
        {
            var key = poolName;
            if (string.IsNullOrEmpty(key))
            {
                key = typeof(T).ToString();
            }
			if (Pools.ContainsKey(key))
			{
				if (Pools[key] is QObjectPool<T>)
				{
					var pool = Pools[key] as QObjectPool<T>;
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
			else if(newFunc !=null&&QTool.IsPlaying)
			{
				var type = typeof(T);
				if (type.IsSubclassOf(typeof(UnityEngine.Object))&&type!=typeof( GameObject))
				{
					throw new Exception("错误的对象池类型[" + type + "][" + poolName + "]");
				}
				var pool = type == typeof(GameObject)?new GameObjectPool(key,newFunc as Func<GameObject>) as QObjectPool<T>: new QObjectPool<T>(key, newFunc) ;
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
			return GetPool(poolName, newFunc).Get();
		}
		public static void Push<T>(string poolName, T obj) where T : class
		{
			if (Pools.ContainsKey(poolName))
			{
				(Pools[poolName] as QObjectPool<T>).Push(obj);
			}
			else
			{
				Debug.LogError("不存在对象池 " + obj);
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
		public static void Push(GameObject gameObject)
        {
			var tag= gameObject.GetComponent<QPoolTag>();
			if (tag == null)
			{
				GameObject.Destroy(gameObject);
				return;
			}
			Push(tag.poolKey, gameObject);
        }
		
	}

    public abstract class QPoolObject<T>:IQPoolObject where T : QPoolObject<T>, new()
    {
        internal static QObjectPool<T> _pool;
        public static QObjectPool<T> Pool
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
		public virtual void Start() { }
		public abstract void OnDestroy();
    }
  
    public abstract class QPool
    {
        public string Key { get; set; }

		public override string ToString()
        {
            var type = GetType();
			return "对象池【" + Key + "】";
        }
    }

    public class QObjectPool<T> : QPool where T : class
    {
		public override string ToString()
		{
			return base.ToString() + " 使用情况 " + UsingPool.Count + " / " + AllCount + "  参考大小 " + (UsingPool.Count * typeof(T).MinSize()).ToSizeString() + " / " + (AllCount * typeof(T).MinSize()).ToSizeString() + "\n" + UsingPool.ToOneString();
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
        protected virtual T OnGet(T obj)
        {
			UsingPool.AddCheckExist(obj);
			return obj;
        }

		protected virtual T OnPush(T obj)
        {
			UsingPool.Remove(obj);
			return obj;
        }
		protected virtual async Task InvokeStart(T obj)
		{
			await QTask.Step();
			if (IsQPoolObject)
			{
				(obj as IQPoolObject).Start();
			}
		}
        protected T PrivateGet()
        {
			if (CanUsePool.Count > 0)
			{
				T obj = default;
				obj = CanUsePool.Dequeue();
				_=InvokeStart(obj);
				QDebug.ChangeProfilerCount(Key + " UseCount", AllCount - CanUseCount);
				return OnGet(obj);
			}
			else
			{
				if (newFunc == null)
				{
					throw new Exception("对象池创建函数为空  " + this);
				}
				var obj = newFunc();
				QDebug.ChangeProfilerCount(Key + " "+nameof(AllCount), AllCount);
				return OnGet(obj);
			}
        }
		public T Get(T obj = null)
		{
			if (obj != null && CanUsePool.Contains(obj))
			{

				CanUsePool.Remove(obj);
				return OnGet(obj);
			}
			else
			{
				return PrivateGet();
			}
		}
		public void Push(T obj)
		{
			if (obj == null || CanUsePool.Contains(obj) || !QTool.IsPlaying) return;
			var resultObj = OnPush(obj);
			if (IsQPoolObject)
			{
				(obj as IQPoolObject).OnDestroy();
			}
			CanUsePool.Enqueue(resultObj);
			QDebug.ChangeProfilerCount(Key + " UseCount", AllCount - CanUseCount);
		}
        public int CanUseCount
        {
            get
            {
                return CanUsePool.Count;
            }
        }
		
        public Func<T> newFunc;
		public bool IsQPoolObject { get; private set; } = false;
        public QObjectPool(string poolName,Func<T> newFunc=null)
        {
            var type = typeof(T);
			IsQPoolObject = type.Is(typeof(IQPoolObject));
			this.newFunc = newFunc;
            this.Key = poolName;
			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (mode == LoadSceneMode.Single)
				{
					Clear();
				}
			};
		}
		public virtual void Clear()
		{
			UsingPool.Clear();
			CanUsePool.Clear();
			QDebug.ChangeProfilerCount(Key + " " + nameof(AllCount), AllCount);
			QDebug.ChangeProfilerCount(Key + " UseCount", 0);
		}

	}
	public interface IQPoolObject
	{
		void Start();
		void OnDestroy();
	}
	public class GameObjectPool : QObjectPool<GameObject>
	{
		public GameObject prefab { get; internal set; }
		public GameObjectPool(string poolName, Func<GameObject> newFunc = null) : base(poolName, newFunc) { }

		public override void Clear()
		{
			newFunc = null;
			prefab = null;
			base.Clear();
		}

		Transform _poolParent = null;
		public Transform PoolParent
		{
			get
			{
				if (_poolParent == null && QTool.IsPlaying)
				{
					_poolParent = QPoolManager.Instance.transform.GetChild(Key, true);
				}
				return _poolParent;
			}
		}
		protected override GameObject OnGet(GameObject obj)
		{
			if (obj == null)
			{
				UsingPool.Remove(obj);
				obj = PrivateGet();
			}
			obj.GetComponent<QPoolTag>(true).poolKey = Key;
			obj.transform.localScale = prefab.transform.localScale;
			obj.transform.position = prefab.transform.position;
			obj.transform.rotation = prefab.transform.rotation;
			obj.SetActive(true);
			return base.OnGet(obj);
		}
		protected override async Task InvokeStart(GameObject obj)
		{
			await QTask.Step();
			foreach (var poolObj in obj.GetComponents<IQPoolObject>())
			{
				poolObj.Start();
			}
		}
		protected override GameObject OnPush(GameObject obj)
		{
			if (!QTool.IsPlaying || PoolParent == null)
			{
				return null;
			}
			if (obj != null)
			{
				obj.SetActive(false);
				obj.transform.SetParent(PoolParent, true);
				foreach (var poolObj in obj.GetComponents<IQPoolObject>())
				{
					poolObj.OnDestroy();
				}
			}
			return base.OnPush(obj);
		}
	}
	public static class QPoolTool
	{
		public static void PoolRecover(this GameObject poolObj)
		{
			QPoolManager.Push(poolObj);
		}
	}
}
