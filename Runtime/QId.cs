using QTool.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
    //public interface IGameSave : IQSerialize
    //{
    //    string InstanceId { get; }
    //    string PrefabId { get; }
    //}
    public static class QIdExtends
    {
        //public static BinaryWriter SaveGameObject<T>(this BinaryWriter writer, ICollection<T> objList) where T : MonoBehaviour, IQSerialize
        //{
        //    return writer.Save(objList, (a) => { return  a.GetQId().idInfo ; });
        //}

        //public static BinaryWriter SaveObject<T>(this BinaryWriter writer, ICollection<T> objList) where T : class, IGameSave
        //{
        //    return writer.Save(objList, (a) => { return new QIdInfo(a) ; });
        //}

        //public static BinaryWriter SaveGameObject<T,InitT>(this BinaryWriter writer, ICollection<T> objList,System.Func<T, InitT> InitInfoGet) where T : MonoBehaviour, IQSerialize where InitT:QIdInfo, new ()
        //{
        //    return writer.Save(objList, InitInfoGet);
        //}
        //public static BinaryWriter SaveObject<T, InitT>(this BinaryWriter writer, ICollection<T> objList, System.Func<T, InitT> InitInfoGet ) where T :class, IGameSave where InitT : QIdInfo, new()
        //{
        //    return writer.Save(objList, InitInfoGet);
        //}

        //private static BinaryWriter Save<T,InitT>(this BinaryWriter writer, ICollection<T> objList, System.Func<T,InitT> InitInfoGet) where T : IQSerialize where InitT:QIdInfo, new()
        //{
        //    writer.Write(objList.Count);
        //    foreach (var obj in objList)
        //    {
        //        writer.WriteObject(InitInfoGet(obj));
        //        writer.WriteObject(obj);
        //    }
        //    return writer;
        //}
        //public static BinaryReader LoadObject<T>(this BinaryReader reader, ICollection<T> objList, Func<QIdInfo, T> createFunc, Action<T> destoryFunc = null) where T : class, IGameSave
        //{
        //    return reader.Load(objList, (a)=> new QIdInfo(a), createFunc, destoryFunc);
        //}
        //public static BinaryReader LoadObject<T,InitT>(this BinaryReader reader, ICollection<T> objList, Func<T, InitT> InitInfoGet, Func<InitT, T> createFunc, Action<T> destoryFunc ) where T : class, IGameSave where InitT : QIdInfo,new()
        //{
        //    return reader.Load(objList, InitInfoGet, createFunc, destoryFunc);
        //}
        //public static BinaryReader LoadGameObject<T>(this BinaryReader reader, ICollection<T> objList, Func<QIdInfo, T> createFunc, Action<T> destoryFunc = null) where T : MonoBehaviour, IQSerialize
        //{
        //    return reader.Load(objList, (a)=> a.GetQId().idInfo, createFunc, destoryFunc);
        //}
        //public static BinaryReader LoadGameObject<T,InitT>(this BinaryReader reader,ICollection<T> objList, Func<T, InitT> InitInfoGet ,Func<InitT, T> createFunc,Action<T> destoryFunc) where T : MonoBehaviour, IQSerialize where InitT : QIdInfo, new()
        //{
        //    return reader.Load(objList, InitInfoGet, createFunc, destoryFunc);
        //}
        //private static BinaryReader Load<T,InitT>(this BinaryReader reader, ICollection<T> objList, Func<T, InitT> InitInfoGet, Func<InitT,T> createFunc,Action<T> destoryFunc) where InitT : QIdInfo, new()
        //{
        //    var desoryList = new List<T>(objList);
        //    var count = reader.ReadInt32();
        //    for (int i = 0; i < count; i++)
        //    {
        //        var initInfo = reader.ReadObject<InitT>();
        //        if (objList.ContainsKey(initInfo.InstanceId,(a)=> InitInfoGet(a).InstanceId))
        //        {
        //            var obj = objList.Get(initInfo.InstanceId, (a) => InitInfoGet(a).InstanceId);
        //            reader.ReadObject(obj);
        //            desoryList.Remove(obj);
        //        }
        //        else
        //        {
        //            var newObj = createFunc(initInfo);
        //            reader.ReadObject(newObj);
        //        }
        //    }
        //    foreach (var item in desoryList)
        //    {
        //        destoryFunc?.Invoke(item);
        //    }
        //    return reader;
        //}


        public static QId GetQId(this MonoBehaviour mono)
        {
            if (mono == null)
            {
                Debug.LogError("游戏对象【" + mono.name + "】不存在QId脚本");
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
    //[System.Serializable]
    //public class QIdInfo
    //{
    //    public string PrefabId;
    //    public string InstanceId;
    //    public QIdInfo()
    //    {

    //    }
    //    public QIdInfo(IGameSave gameSave)
    //    {
    //        PrefabId = gameSave.PrefabId;
    //        InstanceId = gameSave.InstanceId;
    //    }
    //}
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
            UnityEditor.EditorUtility.SetDirty(this);
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

        
#endif
        public static string GetNewId(string key = "")
        {
            return string.IsNullOrWhiteSpace(key) ? System.Guid.NewGuid().ToString("N") : System.Guid.Parse(key).ToString("N");
        }
        public string Key { get => InstanceId; set { } }
        public string PrefabId;
        public string InstanceId;
        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(InstanceId))
            {
                InstanceId = GetNewId();
            }
        }
  
    }
}