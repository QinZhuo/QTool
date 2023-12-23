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
		public ulong PlayerId { get; internal set; } = 0;
		public bool IsPlayer => !PlayerId.IsNull(); 
		public bool IsLoaclPlayer => PlayerId == QNetManager.Instance.transport.PlayerId;
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
			if (QNetManager.Instance != null)
			{
				QNetManager.Instance.OnNetUpdate -= NetStart;
				QNetManager.Instance.OnNetUpdate -= OnNetUpdate;
			}
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
		private static int GetFlag(object obj, Type type)
		{
			var typeInfo = QSerializeType.Get(type);
			switch (typeInfo.Code)
			{
				case TypeCode.Object:
					{
						switch (typeInfo.ObjType)
						{
							case QObjectType.Object:
								{
									var flag = 0;
									foreach (var member in typeInfo.Members)
									{
										flag ^= GetFlag(member.Get(obj), member.Type);
									}
									return flag;
								}
							default:
								break;
						}
					}
					break;
				case TypeCode.Single:
				case TypeCode.Double:
					return ((int)(float)obj).GetHashCode();
				default:
					return obj.GetHashCode();
			}
			return 0;
		}
		internal void OnSyncCheck(QNetSyncFlag flag)
		{
			foreach (var check in TypeInfo.CheckList)
			{
				flag.Check(GetFlag(check.Get(this), check.Type));
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
								CheckList.Add(memberInfo);
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
