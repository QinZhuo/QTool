using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
    
    [DisallowMultipleComponent]
    public class QId : MonoBehaviour,IKey<string>,IQSerialize
    {
#if UNITY_EDITOR

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                InitId();
            }
        }
        private void SetPrefabId(string id)
        {
            if (id != PrefabId)
            {
                PrefabId = id;
                //this.SetDirty();
            }
        }
   
        private void InitId()
        {
            if (Application.IsPlaying(gameObject)) return;
            FreshInstanceId();
            if (gameObject.IsPrefabAsset())
            {
                SetPrefabId(UnityEditor.AssetDatabase.GetAssetPath(gameObject));
           
            }
            else if (gameObject.IsPrefabInstance() || Application.IsPlaying(gameObject))
            {
                var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                if (prefab == null)
                {
                    Debug.LogError(gameObject + " 找不到预制体引用");
                }
                else
                {
                    SetPrefabId(UnityEditor.AssetDatabase.GetAssetPath(prefab));
                }
            }
        }
      


#endif
     
     
        public static QDictionary<string, QId> InstanceIdList = new QDictionary<string, QId>();
    
        public static string GetNewId(string key = "")
        {
            return string.IsNullOrWhiteSpace(key) ? System.Guid.NewGuid().ToString("N") : System.Guid.Parse(key).ToString("N");
        }
        public bool HasPrefabId
        {
            get
            {
                return !string.IsNullOrWhiteSpace(PrefabId);

            }
        }
        public string Key { get => InstanceId; set { } }

        [QReadonly]
        [QName("实例Id")]
        public string InstanceId;
		        [QReadonly]
        [QName("预制体Id", nameof(HasPrefabId) )]
        public string PrefabId;
        bool IsPlaying
        {
            get
            {
                return Application.isPlaying;
            }
        }
        public bool IsSceneInstance
        {
            get
            {
#if UNITY_EDITOR
                if (this == null)
                {
                    return false;
                }
                if (UnityEditor.EditorUtility.IsPersistent(gameObject))
                {
                    return false;
                }
#endif
                return true;
            }
        }
        public List<IQSerialize> qSerializes = new List<IQSerialize>();
        private void FreshInstanceId()
        {
            if (string.IsNullOrWhiteSpace(InstanceId))
            {
                SetInstanceId(GetNewId());
            }
            else if (InstanceIdList[InstanceId] == null)
            {
                InstanceIdList[InstanceId] = this;
			}
            else if (InstanceIdList[InstanceId] != this)
            {
                SetInstanceId(GetNewId());
            }
        }
        private void SetInstanceId(string id)
        {
            if (id != InstanceId)
            {
                InstanceId = id;
                InstanceIdList[id] = this;
            }
        }
        [ExecuteInEditMode]
        protected virtual void Awake()
        {
          
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                InitId();
            }
#endif
            FreshInstanceId();
            qSerializes.Clear();
            qSerializes.AddRange( GetComponents<IQSerialize>());
            qSerializes.Remove(this);
        }
        protected virtual void OnDestroy()
        {
            if (InstanceIdList.ContainsKey(InstanceId)){
                if (InstanceIdList[InstanceId] == this)
                {
                    InstanceIdList.Remove(InstanceId);
                }
            }
        }
        public virtual void Write(QBinaryWriter writer)
        {
            writer.WriteObject(transform.position);
            var byteLength = (byte)qSerializes.Count;
            writer.Write(byteLength);
            for (int i = 0; i < byteLength; i++)
            {
                writer.WriteObject(qSerializes[i]);
            }
        }
       
        public override string ToString()
        {
            return name + "(" + InstanceId +")["+ qSerializes.Count+ "]";
        }

        public virtual void Read(QBinaryReader reader)
        {
            transform.position = reader.ReadObject(transform.position);
            var byteLength = reader.ReadByte();
            if (qSerializes.Count == byteLength)
            {
                for (int i = 0; i < byteLength; i++)
                {
                 
                    reader.ReadObject(qSerializes[i]);
                }
            }
            else
            {
                Debug.LogError("读取序列化数据失败脚本数不匹配"+qSerializes.Count+":"+byteLength);
            }
           
        }
    }


}
