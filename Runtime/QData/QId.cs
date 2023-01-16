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
		public static string NewId()
		{
			return Guid.NewGuid().ToString("N");
		}
		#endregion
	
        [QReadonly,QName("Id", nameof(IsInstance)),UnityEngine.Serialization.FormerlySerializedAs("InstanceId")]
        public string Id;
        [QReadonly,QName("预制体", nameof(HasPrefab)),UnityEngine.Serialization.FormerlySerializedAs("PrefabId")]
        public string Prefab;
		private bool HasPrefab
		{
			get
			{
				return !Prefab.IsNull(); 
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
					var newPrefab = Prefab;
					if (gameObject.IsPrefabAsset())
					{
						newPrefab = UnityEditor.AssetDatabase.GetAssetPath(gameObject);
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
							newPrefab = UnityEditor.AssetDatabase.GetAssetPath(prefab);
						}
					}
					if (Prefab != newPrefab)
					{
						Prefab = newPrefab;
						gameObject.SetDirty();
					}
				}
				Id = "";
			}
			else
#endif
			{
				
				if (Id.IsNull() || (InstanceIdList[Id]!=null&& InstanceIdList[Id] != this))
				{
					if (Net.QNetManager.Instance != null && Application.IsPlaying(this))
					{
						Id = Net.QNetManager.Instance.IdIndex++.ToString() + "_" + Net.QNetManager.Instance.ClientIndex;
					}
					else 
					{
						Id = NewId();
					}
					gameObject.SetDirty();
				}
				InstanceIdList[Id] = this;
			}
		}

		public override string ToString()
        {
            return name + "(" + Id +")"+(Prefab.IsNull()?"":"["+Prefab+"]");
        }
    }


}
