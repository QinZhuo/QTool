﻿using QTool.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Serialize
{
    public interface IGameSave : IQSerialize
    {
        string InstanceId { get; }
        string PrefabId { get; }
    }
    public static class QSaveManager
    {
        public static BinaryWriter SaveGameObject<T>(this BinaryWriter writer, ICollection<T> objList) where T : MonoBehaviour, IQSerialize
        {
            return writer.Save(objList, (a) => a.GetQId().InstanceId, (a) => a.GetQId().PrefabId);
        }
        public static BinaryWriter SaveObject<T>(this BinaryWriter writer, ICollection<T> objList) where T :class, IGameSave
        {
            return writer.Save(objList, (a) => a.InstanceId, (a) => a.PrefabId);
        }
        public static BinaryWriter Save<T>(this BinaryWriter writer, ICollection<T> objList, System.Func<T, string> GetInstanceKey,System.Func<T,string> GetPrefabKey) where T :IQSerialize
        {
            writer.Write(objList.Count);
            foreach (var obj in objList)
            {
                var id = GetInstanceKey(obj);
                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogError("保存信息实例ID不能为空【" + id + "】");
                }
                writer.Write(id, LengthType.Byte);
                writer.Write(GetPrefabKey(obj), LengthType.Byte);
                writer.WriteObject(obj);
            }
            return writer;
        }
        public static BinaryReader LoadObject<T>(this BinaryReader reader, ICollection<T> objList, System.Func<string, string, T> createFunc, System.Action<T> destoryFunc = null) where T : class, IGameSave
        {
            return reader.Load(objList, (a) => a.InstanceId, (a) => a.PrefabId, createFunc, destoryFunc);
        }
        public static BinaryReader LoadGameObject<T>(this BinaryReader reader,ICollection<T> objList ,System.Func<string, string, T> createFunc, System.Action<T> destoryFunc = null) where T : MonoBehaviour, IQSerialize
        {
            return reader.Load(objList, (a) => a.GetQId().InstanceId, (a) => a.GetQId().PrefabId, createFunc, destoryFunc);
        }
        public static BinaryReader Load<T>(this BinaryReader reader, ICollection<T> objList, System.Func<T, string> GetKey,System.Func<T,string> GetPrefabKey, System.Func<string,string,T> createFunc,System.Action<T> destoryFunc=null)
        {
            var desoryList = new List<T>(objList);
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var instanceId = reader.ReadString(LengthType.Byte);
                var prefabId = reader.ReadString(LengthType.Byte);
                if (objList.ContainsKey(instanceId,GetKey))
                {
                    var obj = objList.Get(instanceId,GetKey);
                    reader.ReadObject(obj);
                    desoryList.Remove(obj);
                }
                else
                {
                    var newObj = createFunc(instanceId,prefabId);
                    reader.ReadObject(newObj);
                }
              
            }
            foreach (var item in desoryList)
            {
                destoryFunc?.Invoke(item);
            }
            return reader;
        }


        public static QId GetQId(this MonoBehaviour mono)
        {
            if (mono == null)
            {
                return null;
            }
            return mono.gameObject.GetQId();
        }
        public static QId GetQId(this GameObject obj)
        {
            if (obj == null)
            {
                return null;
            }
            return obj.GetComponent<QId>();
        }
    }
    [DisallowMultipleComponent]
    public class QId : MonoBehaviour,IKey<string>
    {
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.IsPlaying(gameObject)) return;
            PrefabId = GetNewId(PrefabUID);
            if (!IsPrefabAssets)
            {
                if (string.IsNullOrWhiteSpace(InstanceId))
                {
                    InstanceId = GetNewId();
                }
            }
            else
            {
                InstanceId = "";
            }
        }
        private string PrefabUID
        {
            get
            {
                if (IsPrefabAssets)
                {
                    var id= UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(gameObject)); ;
                 //   Debug.LogError("是预制体"+id);
                    return id;
                }
                else if (IsPrefabInstance)
                {
                
                    var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                    if (prefab == null)
                    {
                        Debug.LogError(gameObject + " 找不到预制体引用");
                        return null;
                    }
                    else
                    {
                        var id= UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(prefab));
                     //   Debug.LogError("是预制体实例"+ id);
                        return id;
                    }
                }
                else
                {
                    Debug.LogError(gameObject + " 不是一个预制体物体");
                    return null;
                }
            }
        }
        private bool IsPrefabInstance
        {
            get
            {
                return UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject);
            }
        }
        private bool IsPrefabAssets
        {
            get
            {
                return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject);
            }
        }

        public string Key { get =>InstanceId; set{ }}
#endif
        public static string GetNewId(string key = "")
        {
            return string.IsNullOrWhiteSpace(key) ? System.Guid.NewGuid().ToString("N") : System.Guid.Parse(key).ToString("N");
        }
        public string PrefabId;
        public string InstanceId;
        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(InstanceId))
            {
                InstanceId = GetNewId();
            }
        }
        public void Init(string prefabId,string instanceId)
        {
            PrefabId = prefabId;
            InstanceId = instanceId;
        }

        //public virtual void Write(BinaryWriter write)
        //{
        //    write.Write(InstanceId, LengthType.Byte);
        //    write.Write(PrefabId, LengthType.Byte);
        //}

        //public virtual void Read(BinaryReader read)
        //{
        //    InstanceId = read.ReadString(LengthType.Byte);
        //    PrefabId = read.ReadString(LengthType.Byte);
        //}
    }
}