using System;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public static class PoolManager
    {
        public static bool ShowLog
        {
            get
            {
                return PlayerPrefs.GetInt("PoolManager日志", 0) == 1;
            }
            set
            {
                PlayerPrefs.SetInt("PoolManager日志", value ? 1 : 0);
            }
        }
     
        static Dictionary<string, PoolBase> poolDic = new Dictionary<string, PoolBase>();

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
                return poolDic[key] as ObjectPool<T>;
            }
            else if (newFunc == null)
            {
                return null;
            }
            else
            {
                var pool = new ObjectPool<T>(newFunc, key);
                poolDic.Add(key, pool);
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
    public interface IPoolObj
    {
        void PoolReset();
        void PoolRecover();
    }
    public abstract class PoolBase
    {

    }
    public class ObjectPool<T> : PoolBase where T : class
    {
        public string Key { get; set; }
        public List<T> AllPool = new List<T>();
        public List<string> stackTrace = new List<string>();
        List<T> CanUsePool = new List<T>();
        public bool debugStack = false;
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
                    foreach (var poolObj in gameObj.GetComponents<IPoolObj>())
                    {
                        poolObj.PoolReset();
                    }
                }
            }
            else if (isPoolObj) (obj as IPoolObj).PoolReset();
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
                foreach (var poolObj in gameObj.GetComponents<IPoolObj>())
                {
                    poolObj.PoolRecover();
                }
            }
            else if (isPoolObj)
            {
                (obj as IPoolObj).PoolRecover();

            }
            return obj;
        }
        public virtual T Get()
        {
            var getStackTrace = debugStack ? new System.Diagnostics.StackTrace().ToString() : "";
            if (CanUsePool.Count > 0)
            {
                var index = AllPool.IndexOf(CanUsePool.StackPeek());
                stackTrace[index] = getStackTrace;
                var obj = CanUsePool.Pop();
                return CheckGet(obj);
            }
            else
            {
                var obj = newFunc();
                AllPool.Add(obj);
                stackTrace.Add(getStackTrace);
                if (PoolManager.ShowLog)
                {
                    UnityEngine.Debug.Log("【" + Key + "】对象池当前池大小：" + AllCount);
                }
                return CheckGet(obj);
            }
        }
        public virtual T Get(T obj)
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
        public virtual T Push(T obj)
        {
            if (AllPool.Contains(obj))
            {
                if (isPoolObj) (obj as IPoolObj).PoolRecover();
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