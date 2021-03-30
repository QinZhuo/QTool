using QTool.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
   
    public static class QIdExtends
    {
      


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
    [DisallowMultipleComponent]
    public class QId : MonoBehaviour,IKey<string>
    {
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.IsPlaying(gameObject)) return;

            InitId();
        }
        private void SetPrefabId(string id)
        {
            if (id != PrefabId)
            {
                PrefabId = id;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        private void SetInstanceId(string id)
        {
            if (id != InstanceId)
            {
                InstanceId = id;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        private void InitId()
        {
            if (IsPrefabAssets)
            {
                SetPrefabId(UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(gameObject)));
                SetInstanceId("");
            }
            else if (IsPrefabInstance)
            {
                if (string.IsNullOrWhiteSpace(InstanceId))
                {
                    SetInstanceId(GetNewId());
                }
                var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                if (prefab == null)
                {
                    Debug.LogError(gameObject + " 找不到预制体引用");
                }
                else
                {
                    SetPrefabId( UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(prefab)));
                }
            }
            else
            {
                SetPrefabId("");
                SetInstanceId("");
            }
        }
        private bool IsPrefabInstance
        {
            get
            {
                return UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject) || UnityEditor.PrefabUtility.IsPartOfVariantPrefab(gameObject);
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