using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace QTool.Net
{
	public abstract class QNetBehaviour : MonoBehaviour
	{
		public System.Random Random => QNetManager.Instance.Random;
		public float NetDeltaTime => QNetManager.Instance.NetDeltaTime;
		public float NetTime => QNetManager.Instance.NetTime;
		public string PlayerId { get; internal set; }
		public T PlayerValue<T>(string key, T value)
		{
			if (PlayerId.IsNullOrEmpty())
			{
				throw new System.Exception(this + " 非玩家对象");
			}
			return QNetManager.Instance.PlayerValue(PlayerId, key, value);
		}
		public void PlayerAction<T>(string key, T value, Action<T> action)
		{
			if (PlayerId.IsNullOrEmpty())
			{
				throw new System.Exception(this + " 非玩家对象");
			}
			QNetManager.Instance.PlayerAction(PlayerId, key, value, action);
		}

		public virtual void Awake()
		{
			QNetManager.Instance.OnNetUpdate += NetStart;
			QNetManager.Instance.OnNetUpdate += OnNetUpdate;
			if(this is IQNetSyncCheck sync)
			{
				QNetManager.Instance.OnSyncCheck += sync.OnSyncCheck;
				QNetManager.Instance.OnSyncLoad += sync.OnSyncLoad;
				QNetManager.Instance.OnSyncSave += sync.OnSyncSave;
			}
		}
		public virtual void OnDestroy()
		{
			if (QNetManager.Instance != null)
			{
				QNetManager.Instance.OnNetUpdate -= NetStart;
				QNetManager.Instance.OnNetUpdate -= OnNetUpdate;
				if (this is IQNetSyncCheck sync)
				{
					QNetManager.Instance.OnSyncCheck -= sync.OnSyncCheck;
					QNetManager.Instance.OnSyncLoad -= sync.OnSyncLoad;
					QNetManager.Instance.OnSyncSave -= sync.OnSyncSave;
				}
			}
		}
		private void NetStart()
		{
			OnNetStart();
			QNetManager.Instance.OnNetUpdate -= NetStart;
		}
		public virtual void OnNetStart() { }
		public virtual void OnNetDestroy() { }
		public abstract void OnNetUpdate();
		
		public new static void Destroy(UnityEngine.Object obj)
		{
			var gameObj = obj.GetGameObject();
			if (gameObj != null)
			{
				var nets= gameObj.GetComponentsInChildren<QNetBehaviour>();
				foreach (var net in nets)
				{
					net.OnNetDestroy();
				}
			}
			GameObject.Destroy(obj);
		}
	}
	public interface IQNetSyncCheck
	{
		public void OnSyncCheck(QNetSyncFlag flag);
		public void OnSyncSave(QBinaryWriter writer);
		public void OnSyncLoad(QBinaryReader reader);
	}
	
}
