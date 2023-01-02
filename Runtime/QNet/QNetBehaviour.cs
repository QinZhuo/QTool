using System;
using System.Collections;
using System.Collections.Generic;
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
			QNetManager.Instance.OnSyncCheck += OnSyncCheck;
		}
		public virtual void OnDestroy()
		{
			if (QNetManager.Instance != null)
			{
				QNetManager.Instance.OnNetUpdate -= NetStart;
				QNetManager.Instance.OnNetUpdate -= OnNetUpdate;
				QNetManager.Instance.OnSyncCheck -= OnSyncCheck;
			}
		}
		private void NetStart()
		{
			OnNetStart();
			QNetManager.Instance.OnNetUpdate -= NetStart;
		}
		public abstract void OnNetStart();
		public abstract void OnNetUpdate();
		public virtual void OnSyncCheck()
		{
		}
	}
}
