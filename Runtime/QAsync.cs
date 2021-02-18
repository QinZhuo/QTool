using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if Addressables
using UnityEngine.AddressableAssets;
namespace QTool.Async
{
    public abstract class AsyncList<ObjT>where ObjT:UnityEngine.Object
    {
        public static Dictionary<string, ObjT> objDic = new Dictionary<string, ObjT>();
        public static string Label
        {
            get
            {
                return typeof(ObjT).ToString();
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
             //   var load=ad
            }
        }
    }
}
#endif

