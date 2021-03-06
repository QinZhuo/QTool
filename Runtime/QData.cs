
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using QTool.Binary;
#if Addressables
using UnityEngine.AddressableAssets;
using QTool.Async;
# endif
namespace QTool.Data
{
    //public static class QDataPathManager
    //{
    //    public string string 
    //}
    public class QData<T>: IKey<string>  where T :QData<T>, new()
    {
        #region 基础属性

        [XmlElement("关键名")]
        public string Key { get; set; }

        #endregion

        #region 数据表相关
        public static QList<string, T> list = new QList<string, T>();
        public static string TableType
        {
            get
            {
                return typeof(T).Name;
            }
        }
        public static string TableName
        {
            get
            {
                return "表【"+ TableType + "】";
            }
        }
        public static int Count
        {
            get
            {
                return list.Count;
            }
        }
        public static bool Contains(string key)
        {
            return list.ContainsKey(key);
        }
        public static bool Contains(string prefix, string key)
        {
            return Contains(prefix + "." + key);
        }
        public static bool Contains(System.Enum enumKey)
        {
            return Contains(enumKey.ToString());
        }

        public static void ClearTable()
        {
            _loadOverFile.Clear();
            list.Clear();
        }
        public static void Set(T newData)
        {
            if (newData == null)
            {
                new Exception(TableName + "不能添加空对象");
            }
            list.Set(newData.Key, newData);
        }
        public static void Set(string prefix,T newData)
        {
            Set(newData);
            newData = GetNew(newData.Key);
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                if (!newData.Key.StartsWith(prefix + "."))
                {
                    newData.Key = prefix + "." + newData.Key;
                }
            }
            Set(newData);
        }
        public static void Set(string prefix, ICollection<T> newDatas)
        {
            foreach (var data in newDatas)
            {
                Set(prefix, data);
            }
        }
        public static T Get(string key)
        {
            key = key.Trim();
            var obj =list.Get(key);
            if (obj == null)
            {
                new Exception(TableName + "未包含[" + key + "]");
            }
            return obj;
        }
        public static T GetNew(string key)
        {
            var data = Get(key);
            return FileManager.Copy(data);
        }
        public static T Get(string prefix, string key)
        {
            return Get(prefix + "." + key);
        }
     
        public static T Get(System.Enum enumKey)
        {
            return Get(enumKey.ToString());
        }
        #endregion

        #region 数据读取相关

        static string GetSubPath(string key = "")
        {
            return "Data/" + TableType + (string.IsNullOrWhiteSpace(key) ? "" : "/" + key) + ".xml";
        }

        public static string GetPlayerDataPath(string key = "")
        {
            var usePersistentPath = (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);
            return (usePersistentPath ? Application.persistentDataPath : Application.streamingAssetsPath) + "/" + GetSubPath(key);
        }
        public static string GetStaticTablePath(string key = "")
        {
            return Application.dataPath + "/" + GetSubPath(key);
        }

        public static void DeletePlayerData(string key="")
        {
            var path = GetPlayerDataPath(key);
            System.IO.File.Delete(path);
            Debug.LogError("删除" + path);
        }
        public static void Load(string key = "")
        {
            LoadPath(GetStaticTablePath(key),key);
        }
        public static void LoadPlayerData(string key = "")
        {
            LoadPath(GetPlayerDataPath(key), key);
        }
        public static void SaveDefaultStaticTable(T DefaultData=null,string key="")
        {
            QList<string, T> list = new QList<string, T>();
            list.Add(DefaultData==null?new T():DefaultData);
            var path = GetStaticTablePath(key);
            var xmlStr = FileManager.Serialize(list);
            FileManager.Save(path, xmlStr);
            Debug.LogError(TableName + "保存示例静态表成功：" + path);
        }
        public static void SavePlayerData(string key = "")
        {
            var saveList = new List<T>();
            saveList.AddRange(list);
            if (!string.IsNullOrEmpty(key))
            {
                saveList.RemoveAll((obj) =>
                {
                    return !obj.Key.StartsWith(key + ".");
                });
            }
            var xmlStr = FileManager.Serialize(saveList);
            var path = GetPlayerDataPath(key);
            FileManager.Save(path, xmlStr);
            Debug.Log(TableName + "保存数据：" + Count + " 大小：" + (xmlStr.Length * 8).ComputeScale());
        }
        static void LoadPath(string path,string key)
        {
            if (LoadOver(key, true))
            {
                InvokeLoadOver(path);
                return;
            }
            try
            {
                var data = FileManager.Load(path);
                if (data != null)
                {
                    var loadList = FileManager.Deserialize<QList<string, T>>(data);
                    foreach (var item in loadList)
                    {
                        Set(key, item);
                    }
                    Debug.Log(TableName + "加载数据：" + loadList.Count + " 大小：" + (data.Length * 8).ComputeScale());
                }
            }
            catch (Exception e)
            {
                Debug.LogError(TableName + "加载出错"+path+"  异常信息："+e);
            }
          
        }
        static List<string> _loadOverFile = new List<string>();
        static string GetLoadOverKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? "基础表" : key;
        }
        public static bool LoadOver(string key, bool writeOver = false)
        {
            var loadOverKey = GetLoadOverKey(key);
            var loadOver = _loadOverFile.Contains(loadOverKey);
            if (writeOver && !loadOver)
            {
                _loadOverFile.Add(loadOverKey);
            }
            return loadOver;
        }
        static Dictionary<string, System.Action> LoadOverCallBack = new Dictionary<string, Action>();
        static void InvokeLoadOver(string key)
        {
            var loadKey = GetLoadOverKey(key);
            if (LoadOverCallBack.ContainsKey(loadKey))
            {
                LoadOverCallBack[loadKey]?.Invoke();
                LoadOverCallBack[loadKey] = null;
            }
        }
        public static void LoadOverRun(System.Action action, string key = "")
        {
            key = GetLoadOverKey(key);
            if (LoadOver(key))
            {
                action?.Invoke();
            }
            else
            {
                if (LoadOverCallBack.ContainsKey(key))
                {
                    LoadOverCallBack[key] += action;
                }
                else
                {
                    LoadOverCallBack.Add(key, action);
                }
            }
           
        }
#if Addressables
        public static string AsyncloadPath(string key="")
        {
            return "Assets/" + GetSubPath(key);
        }
        static IEnumerator AsyncLoad(string key="")
        {
            var path = AsyncloadPath(key);
            if (LoadOver(key, true))
            {
                yield break;
            }
            Addressables.LoadAssetAsync<TextAsset>(path).Completed += (result) =>
            {
                var newList = FileManager.Deserialize<QList<string, T>>(result.Result.text);
                Set(key, newList);
                Debug.Log(TableName + "加载数据：" + newList.ToOneString());
                InvokeLoadOver(key);
            };
        }
     
        public static IEnumerator AsyncLoadList(params string[] keys)
        {
            if (keys.Length == 0)
            {
                yield return AsyncLoad();
            }
            else
            {
                foreach (var key in keys)
                {
                    yield return AsyncLoad(key);
                }
            }
         
        }
#endif
        #endregion
    }
}

