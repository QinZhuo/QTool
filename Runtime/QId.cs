using QTool.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Serialize
{
    public static class SaveExtends
    {
        public static string GetInstanceId(this MonoBehaviour mono)
        {
            if (mono == null)
            {
                return "";
            }
            var qId = mono.GetComponent<QId>();
            if (qId == null) return "";
            return qId.InstanceId;
        }
    }
    [DisallowMultipleComponent]
    public class QId : MonoBehaviour
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
                    return UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(gameObject));
                }
                else if (!UnityEditor.PrefabUtility.IsPartOfNonAssetPrefabInstance(gameObject))
                {
                    Debug.LogError(gameObject + " 不是一个预制体物体");
                    return null;
                }
                else
                {
                    var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                    if (prefab == null)
                    {
                        Debug.LogError(gameObject + " 找不到预制体引用");
                        return null;
                    }
                    else
                    {
                        return UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(prefab));
                    }
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
            return string.IsNullOrWhiteSpace(key) ? System.Guid.NewGuid().ToString() : System.Guid.Parse(key).ToString();
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
    }
}