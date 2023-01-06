using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
    
    [DisallowMultipleComponent,ExecuteInEditMode]
    public class QId : MonoBehaviour
    {
		#region 静态数据方法
		public static QDictionary<string, QId> InstanceIdList = new QDictionary<string, QId>();
		public static string NewId(UnityEngine.Object obj=null)
		{
			if (obj!=null&&Application.IsPlaying(obj) && Net.QNetManager.Instance != null)
			{
				return Net.QNetManager.Instance.IdIndex++ +"_"+ Net.QNetManager.Instance.ClientIndex;
			}
			else
			{
				return Guid.NewGuid().ToString("N");
			}
		}
		#endregion
	
        [QReadonly,QName("Id","!"+ nameof(IsInstance)),UnityEngine.Serialization.FormerlySerializedAs("InstanceId")]
        public string Id;
        [QReadonly,QName("预制体", nameof(HasPrefab)),UnityEngine.Serialization.FormerlySerializedAs("PrefabId")]
        public string Prefab;
		private bool HasPrefab
		{
			get
			{
				return !Prefab.IsNullOrEmpty(); 
			}
		}
		private bool IsInstance
		{
			get
			{
				return !this.IsAsset();
			}
		}
		protected virtual void Awake()
		{
			FreshId();
		}
		private void OnValidate()
		{
			FreshId();
		}
		protected virtual void OnDestroy()
		{
			if (InstanceIdList.ContainsKey(Id))
			{
				if (InstanceIdList[Id] == this)
				{
					InstanceIdList.Remove(Id);
				}
			}
		}
		private void FreshId()
		{

#if UNITY_EDITOR
			if (this.IsAsset())
			{
				if (!Application.IsPlaying(this))
				{
					if (gameObject.IsPrefabAsset())
					{
						Prefab = UnityEditor.AssetDatabase.GetAssetPath(gameObject);
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
							Prefab = UnityEditor.AssetDatabase.GetAssetPath(prefab);
						}
					}
				}
				Id = "";
			}
			else
#endif
			{
				if (Id.IsNullOrEmpty() || (InstanceIdList[Id]!=null&& InstanceIdList[Id] != this))
				{
					Id = NewId(this);
				}
				InstanceIdList[Id] = this;
			}
		}

		public override string ToString()
        {
            return name + "(" + Id +")";
        }
    }


}
