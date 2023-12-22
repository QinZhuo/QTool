using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using UnityEngine;
namespace QTool.Net
{
	[RequireComponent(typeof(QId))]
	public abstract class QNetBehaviour : MonoBehaviour
	{
		public System.Random Random => QNetManager.Instance.Random;
		public static float NetDeltaTime => QNetManager.Instance.NetDeltaTime;
		public static float NetTime => QNetManager.Instance.NetTime;
		public string PlayerId { get; internal set; } = null;
		public bool IsPlayer => !PlayerId.IsNull(); 
		public bool IsLoaclPlayer => PlayerId == QNetManager.Instance.transport.ClientId;
		public bool IsDestoryed { get; internal set; }
		public T PlayerValue<T>(string key, T value)
		{
			if (!IsPlayer)
			{
				throw new Exception(this + " 非玩家对象");
			}
			return QNetManager.Instance.PlayerValue(PlayerId, key, value);
		}
		public void PlayerAction(string key,params object[] Params)
		{
			if (IsLoaclPlayer)
			{
				QNetManager.PlayerAction(key, Params);
			}
		}
		//private List<object> Params = new List<object>();
		//public void _AddActionParam<T>(T type)
		//{
		//	Params.Add(type);
		//}
		//public void _InvokeAction(string key)
		//{
		//	if (IsLoaclPlayer)
		//	{
		//		QNetManager.PlayerAction(key, Params.ToArray());
		//		Params.Clear();
		//	}
		//	else
		//	{
		//		throw new Exception(this + " 非本地玩家对象");
		//	}
		//}
		internal QNetTypeInfo TypeInfo { get; set; }
		public virtual void Awake()
		{
			TypeInfo = QNetTypeInfo.Get(GetType());
			QNetManager.Instance.OnNetUpdate += NetStart;
			QNetManager.Instance.OnNetUpdate += OnNetUpdate;
		}
		
		public virtual void OnDestroy()
		{
			NetDestroy();
			if (QNetManager.Instance != null)
			{ 
				QNetManager.Instance.OnNetUpdate -= NetStart;
				QNetManager.Instance.OnNetUpdate -= OnNetUpdate;
			}
		}
		private void NetStart()
		{
			QNetManager.Instance.OnNetUpdate -= NetStart;
			QNetManager.Instance.OnSyncCheck += OnSyncCheck;
			if (TypeInfo.Members.Count > 0)
			{
				var qid = GetComponent<QId>().Id;
				if (!QNetManager.Instance.QNetSyncCheckList.ContainsKey(qid))
				{
					QNetManager.Instance.QNetSyncCheckList.Add(qid, new List<QNetBehaviour>());
				}
				QNetManager.Instance.QNetSyncCheckList[qid].AddCheckExist(this);
			}
			if (IsPlayer && TypeInfo.Functions.Count > 0)
			{
				QNetManager.Instance.Players[PlayerId].Action += InvokeAction;
			}
			OnNetStart();
		}
		internal void InvokeAction(string key, params object[] value)
		{
			if (TypeInfo.Functions.ContainsKey(key))
			{
				TypeInfo.Functions[key].Invoke(this, value);
			}
		}
		public virtual void OnNetStart() { }

		internal void NetDestroy()
		{
			if (IsDestoryed) return;
			IsDestoryed = true;
			OnNetDestroy();
			if (QNetManager.Instance != null)
			{
				QNetManager.Instance.OnSyncCheck -= OnSyncCheck;
				if (TypeInfo.Members.Count > 0)
				{
					var qid = GetComponent<QId>().Id;
					if (QNetManager.Instance.QNetSyncCheckList.ContainsKey(qid))
					{
						QNetManager.Instance.QNetSyncCheckList[qid].Remove(this);
						if (QNetManager.Instance.QNetSyncCheckList[qid].Count == 0)
						{
							QNetManager.Instance.QNetSyncCheckList.Remove(qid);
						}
					}
				}
				if (IsPlayer && TypeInfo.Functions.Count > 0)
				{
					QNetManager.Instance.Players[PlayerId].Action -= InvokeAction;
				}
			}
		}
		public virtual void OnNetDestroy() { }
		public abstract void OnNetUpdate();
		
		public new static void Destroy(UnityEngine.Object obj)
		{
			QNetManager.Destroy(obj);
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
	/// 标记函数为玩家动作 本地玩家使用PlayerAction远程调用所有客户端
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class QSyncActionAttribute : Attribute
	{
		public QSyncActionAttribute()
		{
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
		public const string QSyncAction_ = nameof(QSyncAction_);
		public List<QMemeberInfo> CheckList { get; private set; } = new List<QMemeberInfo>();
		protected override void Init(Type type)
		{
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
									QDebug.LogError(Type + "." + memberInfo.QName + "同步检测出错 只有值类型才能通过" + nameof(System.Object.GetHashCode) + "()进行同步检测");
								}
							}
							return false;
						}
					}
					return true;
				});
				Functions.RemoveAll(function =>
				{
					return function.MethodInfo.GetAttribute<QSyncActionAttribute>() == null;
				});
			}
		}
	}

}
