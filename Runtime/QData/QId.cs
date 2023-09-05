using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
    
    [ExecuteInEditMode]
    public class QId : MonoBehaviour
    {
		#region 静态数据方法
		public static QDictionary<string, QId> InstanceIdList = new QDictionary<string, QId>();
		public static string NewId()
		{
			return Guid.NewGuid().ToString("N");
		}
		public static void InitSceneId()
		{
			var ids = GameObject.FindObjectsOfType<QId>(true);
			foreach (var id in ids)
			{
				if (!id.isActiveAndEnabled)
				{
					InstanceIdList[id.Id] = id;
				}
			}
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
		private void Reset()
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
			if (!Application.IsPlaying(this))
			{
				if (gameObject.IsPrefab())
				{
					Prefab = UnityEditor.AssetDatabase.GetAssetPath(gameObject);
					gameObject.SetDirty();
				}
				else if (gameObject.IsPrefabInstance(out var prefab))
				{
					if (prefab != null)
					{
						Prefab = UnityEditor.AssetDatabase.GetAssetPath(prefab);
						gameObject.SetDirty();
					}
				}
			}
#endif
			if(this.IsAsset())
			{
				Id = "";
			}
			else
			{
				if (Id.IsNull() || (InstanceIdList[Id] != null && InstanceIdList[Id] != this))
				{
					if (Net.QNetManager.Instance != null && Application.IsPlaying(this))
					{
						Id = Net.QNetManager.Instance.IdIndex++.ToString() + "_" + Net.QNetManager.ClientIndex;
					}
					else
					{
						Id = NewId();
					}
				}
				InstanceIdList[Id] = this;
				gameObject.SetDirty();
			}
		}

		public override string ToString()
        {
            return name + "(" + Id +")"+(Prefab.IsNull()?"":"["+Prefab+"]");
        }
    }


}
