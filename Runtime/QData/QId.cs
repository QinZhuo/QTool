using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
    
    [DisallowMultipleComponent]
    public class QId : MonoBehaviour
    {
		#region 静态数据方法
		public static QDictionary<string, QId> InstanceIdList = new QDictionary<string, QId>();
		public static string NewId()
		{
			return Guid.NewGuid().ToString("N");
		}
		#endregion
	
        [QReadonly,QName("Id"),UnityEngine.Serialization.FormerlySerializedAs("InstanceId")]
        public string Id;
        [QReadonly,QName("预制体Id", nameof(HasPrefabId) )]
        public string PrefabId;
		public bool HasPrefabId
		{
			get
			{
				return !PrefabId.IsNullOrEmpty();
			}
		}
		private void FreshInstanceId()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                SetInstanceId(NewId());
            }
            else if (InstanceIdList[Id] == null)
            {
                InstanceIdList[Id] = this;
			}
            else if (InstanceIdList[Id] != this)
            {
                SetInstanceId(NewId());
            }
        }
        private void SetInstanceId(string id)
        {
            if (id != Id)
            {
                Id = id;
                InstanceIdList[id] = this;
            }
        }
        [ExecuteInEditMode]
        protected virtual void Awake()
		{
			FreshInstanceId();
			#region 刷新PrefabId

#if UNITY_EDITOR
			if (!Application.IsPlaying(this)) {
				if (gameObject.IsPrefabAsset())
				{
					PrefabId = UnityEditor.AssetDatabase.GetAssetPath(gameObject);
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
						PrefabId = UnityEditor.AssetDatabase.GetAssetPath(prefab);
					}
				}
			}
#endif

			#endregion
		}
		protected virtual void OnDestroy()
        {
            if (InstanceIdList.ContainsKey(Id)){
                if (InstanceIdList[Id] == this)
                {
                    InstanceIdList.Remove(Id);
                }
            }
        }
        public override string ToString()
        {
            return name + "(" + Id +")";
        }
    }


}
