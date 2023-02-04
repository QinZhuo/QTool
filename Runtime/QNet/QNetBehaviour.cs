using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace QTool.Net
{
	[RequireComponent(typeof(QId))]
	public abstract class QNetBehaviour : MonoBehaviour
	{
		public System.Random Random => QNetManager.Instance.Random;
		public float NetDeltaTime => QNetManager.Instance.NetDeltaTime;
		public float NetTime => QNetManager.Instance.NetTime;
		public string PlayerId { get; internal set; }
		public T PlayerValue<T>(string key, T value)
		{
			if (PlayerId.IsNull())
			{
				throw new System.Exception(this + " 非玩家对象");
			}
			return QNetManager.Instance.PlayerValue(PlayerId, key, value);
		}
		public void PlayerAction<T>(string key, T value, Action<T> action)
		{
			if (PlayerId.IsNull())
			{
				throw new System.Exception(this + " 非玩家对象");
			}
			QNetManager.Instance.PlayerAction(PlayerId, key, value, action);
		}
		internal QNetTypeInfo TypeInfo { get; set; }
		public virtual void Awake()
		{
			TypeInfo = QNetTypeInfo.Get(GetType());
			QNetManager.Instance.OnNetUpdate += NetStart;
			QNetManager.Instance.OnNetUpdate += OnNetUpdate;
		}
		
		public virtual void OnDestroy()
		{
			if (QNetManager.Instance != null)
			{
				QNetManager.Instance.OnNetUpdate -= NetStart;
				QNetManager.Instance.OnNetUpdate -= OnNetUpdate;
			}
		}
		private void NetStart()
		{
			OnNetStart();
			QNetManager.Instance.OnNetUpdate -= NetStart;
			QNetManager.Instance.OnSyncCheck += OnSyncCheck;

			if (TypeInfo.Members.Count > 0)
			{
				var qid = GetComponent<QId>().Id;
				if (!QNetManager.QNetSyncCheckList.ContainsKey(qid))
				{
					QNetManager.QNetSyncCheckList.Add(qid, new List<QNetBehaviour>());
				}
				QNetManager.QNetSyncCheckList[qid].AddCheckExist(this);
			}
		}
		public virtual void OnNetStart() { }
		private void NetDestroy()
		{
			OnNetDestroy();
			QNetManager.Instance.OnSyncCheck -= OnSyncCheck;
			if (TypeInfo.Members.Count > 0)
			{
				var qid = GetComponent<QId>().Id;
				if (QNetManager.QNetSyncCheckList.ContainsKey(qid))
				{
					QNetManager.QNetSyncCheckList[qid].Remove(this);
					if (QNetManager.QNetSyncCheckList[qid].Count == 0)
					{
						QNetManager.QNetSyncCheckList.Remove(qid);
					}
				}
			}
		}
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
					net.NetDestroy();
				}
			}
			GameObject.Destroy(obj);
		}

		internal void OnSyncCheck(QNetSyncFlag flag)
		{
			foreach (var check in TypeInfo.CheckList)
			{
				flag.Check(check.Get(this).GetHashCode());
			}
		}
		internal void OnSyncSave(QBinaryWriter writer)
		{
			foreach (var member in TypeInfo.Members)
			{
				writer.WriteObjectType(member.Get(this),member.Type);
			}
		}
		internal void OnSyncLoad(QBinaryReader reader)
		{
			foreach (var member in TypeInfo.Members)
			{
				member.Set(this, reader.ReadObjectType(member.Type,member.Get(this)));
			}
		}
	}

	/// <summary>
	/// 标记变量在同步检测出错时进行同步更新 如果SyncCheck为True则会对该变量进行同步检测
	/// </summary>
	[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property, AllowMultiple = false)]
	public class QSyncVarAttribute : Attribute
	{
		public bool SyncCheck { get; private set; }
		public QSyncVarAttribute(bool SyncCheck = false)
		{
			this.SyncCheck = SyncCheck;
		}
	}
	public class QNetTypeInfo : QTypeInfo<QNetTypeInfo>
	{
		public List<QMemeberInfo> CheckList { get; private set; } = new List<QMemeberInfo>();
		protected override void Init(Type type)
		{
			Functions = null;
			base.Init(type);
			if (!TypeMembers.ContainsKey(type))
			{
				Members.RemoveAll((memberInfo) =>
				{
					if (memberInfo.Get != null || memberInfo.Set != null)
					{
						var syncVar = memberInfo.MemeberInfo.GetAttribute<QSyncVarAttribute>();
						if (syncVar != null)
						{
							if (syncVar.SyncCheck)
							{
								if (memberInfo.Type.IsValueType)
								{
									CheckList.Add(memberInfo);
								}
								else
								{
									Debug.LogError("只有值类型才能通过" + nameof(System.Object.GetHashCode) + "()进行同步检测");
								}
							}
							return false;
						}
					}
					return true;
				});
			}
			
		}
	}

}
