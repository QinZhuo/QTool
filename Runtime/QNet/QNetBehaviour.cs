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
		public virtual void OnNetStart() { }
		public virtual void OnNetDestroy() { }
		public abstract void OnNetUpdate();
		public virtual void OnSyncCheck()
		{

		}
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
	public static partial class QTool
	{
		public static float QNetFix(this float value)
		{
			return Mathf.RoundToInt(value * 1000) / 1000f;
		}
		public static Vector3 QNetFix(this Vector3 value)
		{
			value.x = value.x.QNetFix();
			value.y = value.y.QNetFix();
			value.z = value.z.QNetFix();
			return value;
		}
	}
}
