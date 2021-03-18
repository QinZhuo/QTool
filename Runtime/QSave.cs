using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
    public interface IQSave<T>
    {
        string InstanceId { get; set; }
        T Save(T newData);
        void Load(T data);
    }
    public class QSaveBase:IKey<string>
    {
        public string Key { get; set; }
    }
    public static class QSave
    {
        public static List<ObjT> ToOneList<KeyT, ObjT>(this Dictionary<KeyT,ObjT> objList)
        {
            var list = new List<ObjT>();
            foreach (var kv in objList)
            {
                list.Add(kv.Value);
            }
            return list;
        }
        public static List<ObjT> ToOneList<KeyT, ObjT>(this Dictionary<KeyT, ICollection<ObjT>> objList)
        {
            var list = new List<ObjT>();
            foreach (var kv in objList)
            {
                list.AddRange(kv.Value);
            }
            return list;
        }
        public static void Save<SaveT,ObjT>(this ICollection<SaveT> saveList,ICollection<ObjT> objList) where SaveT : QSaveBase, new() where ObjT : IQSave<SaveT>
        {
            foreach (var obj in objList)
            {
                if(obj is MonoBehaviour)
                {
                    if ((obj as MonoBehaviour) == null) continue;
                }
                if (string.IsNullOrWhiteSpace(obj.InstanceId))
                {
                    Debug.LogError("保存信息ID不能为空【" + typeof(SaveT) + "】");
                }
                else
                {
                    saveList.Set(obj.InstanceId, obj.Save(new SaveT()));
                }
            }
        }
        public static void Load<SaveT, ObjT>(this ICollection<SaveT> saveList, ICollection<ObjT> objList,Func<SaveT,ObjT> createFunc=null,Action<ObjT> destoryFunc=null) where SaveT : QSaveBase, new() where ObjT : IQSave<SaveT>
        {
            var destoryList = new List<ObjT>(objList);
            foreach (var saveData in saveList)
            {
                var obj= objList.Get(saveData.Key, (a) => a.InstanceId);
                if (obj == null)
                {
                    if(createFunc != null)
                    {
                        obj = createFunc(saveData);
                        obj.InstanceId = saveData.Key;
                    }
                }
                else
                {
                    destoryList.Remove(obj);
                }
                if (obj != null)
                {
                    obj.Load(saveData);
                }
            }
            if (destoryFunc != null)
            {
                foreach (var obj in destoryList)
                {
                    destoryFunc(obj);
                }
            }
          
        }
    }
}

