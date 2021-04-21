using System;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public class ToolDebug : DebugBase<ToolDebug>
    {

    }
    public class DebugBase<T> where T:DebugBase<T>
    {
        static bool Init = false;
        static bool _show=false;
        public static bool ShowLog
        {
            get
            {
                if (!Init)
                {
                    Init = true;
                    _show = PlayerPrefs.GetInt("PoolManager日志", 0) == 1;
                }
                return _show;
            }
            set
            {
                if (!Init)
                {
                    Init = true;
                }
                _show = value;
                PlayerPrefs.SetInt("PoolManager日志", value ? 1 : 0);
            }
        }
        public static void Log(object log)
        {
            if (ShowLog)
            {
                Debug.Log(log);
            }
        }
    }
    public static class PoolManager
    {
      
        
        static QDcitionary<string, PoolBase> poolDic = new QDcitionary<string, PoolBase>();

        public static GameObject Get(string poolKey ,GameObject prefab)
        {
            return GetPool(poolKey,prefab).Get() ;
        }
        public static ObjectPool<GameObject> GetPool(string poolKey,GameObject prefab)
        {
            return GetPool(poolKey, () => GameObject.Instantiate(prefab));
        }
        public static T Get<T>(string poolName, System.Func<T> newFunc = null) where T : class
        {
            return GetPool<T>(poolName, newFunc).Get();
        }
        public static ObjectPool<T> GetPool<T>(string poolName, System.Func<T> newFunc = null) where T : class
        {
            var key = poolName;
            if (string.IsNullOrEmpty(key))
            {
                key = typeof(T).ToString();
            }
            if (poolDic.ContainsKey(key))
            {
                if(poolDic[key] is ObjectPool<T>)
                {
                    return poolDic[key] as ObjectPool<T>;
                }
                else
                {
                    throw new Exception("已存在重名不同类型对象池" + poolDic[key]);
                }
                    
            }
            else if (newFunc == null)
            {
                throw new Exception("不能以空的创建函数初始化对象池[" + poolName+"]");
            }
            else
            {
                var pool = new ObjectPool<T>(newFunc, key);
                poolDic[key]= pool;
                return pool;
            }
        }

        public static T Push<T>(string poolName, T obj) where T : class
        {
            var pool = GetPool<T>(poolName);
            if (pool != null)
            {
                return pool.Push(obj);
            }
            return obj;
        }
    }
    public abstract class PoolObject<T>:IPoolObject where T : PoolObject<T>,new()
    {
        public static ObjectPool<T> Pool
        {
            get
            {
                return PoolManager.GetPool(typeof(T).FullName, () => new T());
            }
        }
        public static T Get()
        {
            return Pool.Get();
        }
        public static T Push(T obj)
        {
            return Pool.Push(obj);
        }
        public void Recover()
        {
            Pool.Push(this as T);
        }
        public abstract void OnPoolRecover();
        public abstract void OnPoolReset();
    }
    public interface IPoolObject
    {
        void OnPoolReset();
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
        public List<T> AllPool = new List<T>();
        List<T> CanUsePool = new List<T>();
        T CheckGet(T obj)
        {

            if (isMonobehaviour || isGameObject)
            {
                if ((obj as T).Equals(null))
                {
                    AllPool.Remove(obj);
                    obj = Get();
                }
                GameObject gameObj = null;
                if (isGameObject)
                {
                    gameObj = (obj as GameObject);
                   
                }
                else if (isMonobehaviour)
                {
                    gameObj = (obj as MonoBehaviour).gameObject;
                }
                if (gameObj != null)
                {
                    gameObj.SetActive(true);
                    foreach (var poolObj in gameObj.GetComponents<IPoolObject>())
                    {
                        poolObj.OnPoolReset();
                    }
                }
            }
            else if (isPoolObj) (obj as IPoolObject).OnPoolReset();
            return obj;
        }
        private static Dictionary<string, Transform> parentList = new Dictionary<string, Transform>();
        private static Transform GetPoolParent(string name)
        {
            if (parentList.ContainsKey(name))
            {
                return parentList[name];
            }
            else
            {
                var parent = new GameObject(name).transform;
                parentList.Add(name, parent);
                return parent;
            }
        }
        T CheckPush(T obj)
        {
          
            GameObject gameObj=null;
            if (isGameObject)
            {
                gameObj=(obj as GameObject);
            }
            else if (isMonobehaviour)
            {
                gameObj=(obj as MonoBehaviour).gameObject;
            }
            if (gameObj != null)
            {
                gameObj.SetActive(false);
                gameObj.transform.SetParent(GetPoolParent(Key));
                foreach (var poolObj in gameObj.GetComponents<IPoolObject>())
                {
                    poolObj.OnPoolRecover();
                }
            }
            else if (isPoolObj)
            {
                (obj as IPoolObject).OnPoolRecover();

            }
            return obj;
        }
        public  T Get()
        {
            if (CanUsePool.Count > 0)
            {
                var index = AllPool.IndexOf(CanUsePool.StackPeek());
                var obj = CanUsePool.Pop();
                return CheckGet(obj);
            }
            else
            {
        
                var obj = newFunc();
                AllPool.Add(obj);
                ToolDebug.Log("【" + Key + "】对象池当前池大小：" + AllCount);
                return CheckGet(obj);
            }
        }
        public  T Get(T obj)
        {
            if (CanUsePool.Contains(obj))
            {
                CanUsePool.Remove(obj);
                return CheckGet(obj);
            }
            else
            {
                return Get();
            }
        }
        public  T Push(T obj)
        {
            if (AllPool.Contains(obj))
            {
                if (isPoolObj) (obj as IPoolObject).OnPoolRecover();
                CanUsePool.Push(CheckPush(obj));
                return null;
            }
            return obj;
        }
        public int CanUseCount
        {
            get
            {
                return CanUsePool.Count;
            }
        }
        public int AllCount
        {
            get
            {
                return AllPool.Count;
            }
        }
        public void Clear()
        {
            AllPool.Clear();
            CanUsePool.Clear();
        }

        public Func<T> newFunc;
        public bool isPoolObj = false;
        public bool isMonobehaviour = false;
        public bool isGameObject = false;
        public ObjectPool(Func<T> newFunc, string poolName)
        {
            isPoolObj = typeof(T).GetInterface("IPoolObj") != null;
            isMonobehaviour = typeof(T).IsSubclassOf(typeof(MonoBehaviour));
            isGameObject = typeof(T) == typeof(GameObject);
            this.newFunc = newFunc;
            this.Key = poolName;
        }
    }
}