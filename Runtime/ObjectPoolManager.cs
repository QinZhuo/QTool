using System;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public class PoolManager
    {

        static Dictionary<string, PoolBase> poolDic = new Dictionary<string, PoolBase>();
        public static T Push<T>(string poolName, T obj) where T : class
        {
            var pool = GetPool<T>(poolName);
            if (pool != null)
            {
                return pool.Push(obj);
            }
            return obj;
        }
        public static ObjectPool<GameObject> GetPool(GameObject prefab)
        {
            return GetPool(prefab.name, () => GameObject.Instantiate(prefab));
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
        Stack<T> CanUsePool = new Stack<T>();
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
                if (isGameObject)
                {
                    (obj as GameObject).SetActive(true);
                }
                else if (isMonobehaviour)
                {
                    (obj as MonoBehaviour).gameObject.SetActive(true);
                }
            }
            if (isPoolObj) (obj as IPoolObj).PoolReset();
            return obj;
        }
        T CheckPush(T obj)
        {
            if (isPoolObj) (obj as IPoolObj).PoolRecover();

            if (isGameObject)
            {
                (obj as GameObject).SetActive(false);
            }
            else if (isMonobehaviour)
            {
                (obj as MonoBehaviour).gameObject.SetActive(false);
            }
            return obj;
        }
        public virtual T Get()
        {
            var getStackTrace = debugStack ? new System.Diagnostics.StackTrace().ToString() : "";
            if (CanUsePool.Count > 0)
            {
                var index = AllPool.IndexOf(CanUsePool.Peek());
                stackTrace[index] = getStackTrace;
                var obj = CanUsePool.Pop();
                return CheckGet(obj);
            }
            else
            {
                var obj = newFunc();
                AllPool.Add(obj);
                stackTrace.Add(getStackTrace);
                UnityEngine.Debug.Log("【" + Key + "】对象池当前池大小：" + AllCount);
                return CheckGet(obj);
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