using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if Addressables
using UnityEngine.AddressableAssets;
namespace QTool.Async
{
  
    public abstract class AsyncList<ClassT,ObjT> where ObjT:UnityEngine.Object where ClassT:AsyncList<ClassT,ObjT>
    {
        public static Dictionary<string, ObjT> objDic = new Dictionary<string, ObjT>();
        public static string Label
        {
            get
            {
                return typeof(ClassT).ToString();
            }
        }
        public static bool LabelLoadOver = false;
        private static Action OnLabelLoadOver;
        public static void LoadOverRun(Action action)
        {
            if (LabelLoadOver)
            {
                action?.Invoke();
            }
            else
            {
                OnLabelLoadOver += action;
            }
        }
        public static bool ContainsKey(string key)
        {
            return objDic.ContainsKey(key);
        }
        public static bool Contains(string key)
        {
            return ContainsKey(key);
        }
        public static ObjT Get(string key)
        {
            if (ContainsKey(key))
            {
                return objDic[key];
            }
            else
            {
                return null;
            }
        }
        public static void AsyncGet(string key,Action<ObjT> loadOver)
        {
            var obj = Get(key);
            if (obj!=null)
            {
                loadOver?.Invoke(obj);
            }
            else
            {
                var load = Addressables.LoadAssetAsync<ObjT>(key);
                load.Completed += (result) =>
                {
                    if (result.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    {
                        Set(key, result.Result);
                        loadOver?.Invoke(result.Result);
                    }
                    else
                    {
                        loadOver?.Invoke(null);
                    }
                };
            }
        }
        public static void Set(string key,ObjT obj)
        {
            if (!ContainsKey(key))
            {
                objDic.Add(key, obj);
            }
            else
            {
                objDic[key] = obj;
            }
        }
        public static IEnumerator AsyncLoadLabel()
        {
            if (LabelLoadOver) yield break;
            Addressables.LoadAssetsAsync<ObjT>(Label, (result) =>
            {
                Set(result.name, result);
            }).Completed+=(results)=> {
                Debug.LogError("[" + Label + "]加载完成总数" + objDic.Count);
                LabelLoadOver = true;
                OnLabelLoadOver?.Invoke();
                OnLabelLoadOver = null;
            };
            while (!LabelLoadOver)
            {
                yield return null;
            }
        }
   
    }
    public abstract class AsyncPrefabList<ClassT>: AsyncList<ClassT,GameObject> where ClassT:AsyncPrefabList<ClassT>
    {
        static Dictionary<string, ObjectPool<GameObject>> PoolDic = new Dictionary<string, ObjectPool<GameObject>>();
        static ObjectPool<GameObject> GetPool(string key)
        {
            var poolkey = key + "_ObjPool";
            if (!PoolDic.ContainsKey(poolkey))
            {
                var prefab = Get(key) as GameObject;
                if (prefab == null)
                {
                    new Exception(Label + "找不到预制体资源" + key);
                    PoolDic.Add(poolkey, null);
                }
                else
                {
                    var pool = PoolManager.GetPool(poolkey, prefab);
                    PoolDic.Add(poolkey, pool);
                }
            }
            return PoolDic[poolkey];
        }
        public static GameObject GetInstance(string key,Transform parent = null)
        {
            var obj = GetPool(key)?.Get();
            if (obj == null)
            {
                return null;
            }
            obj.transform.SetParent(parent);
            if(obj.transform is RectTransform)
            {
                var prefab = Get(key);
                (obj.transform as RectTransform).anchoredPosition = (prefab.transform as RectTransform).anchoredPosition;
            }
            obj.name = key;
            return obj;
        }
        public static GameObject GetInstance(string key, Vector3 position,Quaternion rotation,Transform parent = null)
        {
            var obj = GetInstance(key, parent);
            obj.transform.position = position;
            obj.transform.localRotation = rotation;
            return obj;
        }
        public static void Push(string key,GameObject obj)
        {
            var pool = GetPool(key);
            if (pool != null)
            {
                obj = pool.Push(obj);
                if (obj != null && obj.transform.parent != null)
                {
                    obj = pool.Push(obj.transform.parent.gameObject) == null ? null : obj;
                }
            }
            if (obj != null)
            {
                GameObject.Destroy(obj);
                Debug.LogError("强制删除[" + key + "]:" + obj.name);
            }
        }
        public static void Push(GameObject obj)
        {
            Push(obj.name, obj);
        }
        public static CT GetInstance<CT>(string key, Transform parent = null) where CT : Component
        {
            var obj = GetInstance(key, parent);
            if (obj == null)
            {
                return null;
            }
            return obj.GetComponentInChildren<CT>();
        }
        public static CT GetInstance<CT>(string key, Vector3 pos, Quaternion rotation, Transform parent = null) where CT : Component
        {
            var obj = GetInstance(key, pos, rotation, parent);
            if (obj == null)
            {
                return null;
            }
            return obj.GetComponentInChildren<CT>();
        }
    }
}
#endif

