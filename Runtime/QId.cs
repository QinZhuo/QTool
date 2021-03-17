using QTool.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Serialize
{
    public interface IGameLoad:IQSerialize
    {
        void LoadCreate();
        void LoadDestory();
    }
    public static class QIdManager
    {
        public static List<QId> qIdList = new List<QId>();
        public static void Add(QId id)
        {
            qIdList.AddCheckExist(id);
        }
        public static void Remove(QId id)
        {
            qIdList.Remove(id);
        }
        public static void Clear()
        {
            qIdList.Clear();
        }
        public static BinaryWriter Save(this BinaryWriter writer,IList<QId> objList) 
        {
            writer.Write(objList.Count);
          
            foreach (var obj in objList)
            {
                if (obj as MonoBehaviour == null) continue;
                var qId = obj.GetQId();
                if (string.IsNullOrWhiteSpace(qId.InstanceId))
                {
                    Debug.LogError("保存信息ID不能为空【" + qId.PrefabId + "】");
                }
                Debug.LogError("保存" + qId.InstanceId);
                writer.Write(obj.InstanceId, LengthType.Byte);
                writer.Write(obj.PrefabId, LengthType.Byte);
                writer.WriteObject(obj.GetComponents<IQSerialize>());
            }
            return writer;
        }
        public static BinaryReader Load(this BinaryReader reader, IList<QId> objList,System.Func<string,QId> createFunc)
        {
            var createList = new List<QId>();
            var desoryList = new List<QId>(objList);

            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var instanceId = reader.ReadString(LengthType.Byte);
                var prefabId = reader.ReadString(LengthType.Byte);
                if (objList.ContainsKey(instanceId))
                {
                    var obj = objList.Get(instanceId);
                    reader.ReadObject(obj.GetComponents<IQSerialize>());
                    desoryList.Remove(obj);
                }
                else
                {
                    var newQid = createFunc(prefabId);
                    newQid.InstanceId= instanceId;
                    reader.ReadObject(newQid.GetComponents<IQSerialize>());
                    foreach (var iLoad in newQid.GetComponents<IGameLoad>())
                    {
                        iLoad.LoadCreate();
                    }
                    createList.Add(newQid);
                }
            }
            foreach (var item in createList)
            {
                objList.Add(item);
            }
            foreach (var item in desoryList)
            {
                foreach (var iLoad in item.GetComponents<IGameLoad>())
                {
                    iLoad.LoadDestory();
                }
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