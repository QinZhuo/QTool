using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.FlowGraph;

namespace QTool
{
	public enum QBuffMergeMode
	{
		唯一 = 0,
		时间 = 1 << 1,
		叠层 = 1 << 2,
		时间叠层 = 时间 | 叠层,
	}
	public abstract class QEffectData<T> : QDataList<T> where T : QEffectData<T>, IKey<string>, new()
	{
		[QIgnore]
		protected string m_effectInfo = "";
		[QName("效果说明")]
		public virtual string EffectInfo
		{
			get
			{
				if (m_effectInfo.IsNull() && Effect != null)
				{
					m_effectInfo = Effect.Graph.QDataCopy().ToInfoString();
				}
				return m_effectInfo;
			}
			protected set => m_effectInfo = value;
		}
		[QIgnore]
		protected QFlowGraphAsset m_effect = null;
		[QName("效果")]
		public virtual QFlowGraphAsset Effect
		{
			get { if (!Application.isPlaying) m_effect = null; return m_effect ??= QTool.LoadAndCreate<QFlowGraphAsset>(nameof(QFlowGraph) + "/" + typeof(T).Name + "/" + Key); }
			protected set => m_effect = value;
		}
		public override string ToString()
		{
			return EffectInfo;
		}
	}
	public abstract class QBuffData<T> : QEffectData<T> where T : QBuffData<T>, IKey<string>, new()
	{
		[QName("叠加方式")]
		public QBuffMergeMode Megre { get; protected set; } = QBuffMergeMode.时间叠层;
		[QName("时间事件")]
		public float TimeEvent { get; protected set; } = 0;
		public class Runtime : QRuntime<Runtime, T>
		{
			[QName("层数")]
			public QRuntimeValue Count { get; private set; } = new QRuntimeValue();

			[QName("时间")]
			public QRuntimeRangeValue Time { get; private set; } = new QRuntimeRangeValue();
			public QFlowGraph Graph { get; private set; }
			public QTimer TimeEvent { get; private set; }
			protected override void Init(string key)
			{
				base.Init(key);
				if (Data.Effect != null)
				{
					Graph = Data.Effect.Graph.QDataCopy();
					Graph.RegisterValue(this);
				}
				if (Data.TimeEvent > 0)
				{
					TimeEvent = new QTimer(Data.TimeEvent);
				}
			}
			public override void OnDestroy()
			{
				base.OnDestroy();
				if (Graph != null)
				{
					Graph.UnRegisterValue(this);
					Graph = null;
				}
			}
			public void InvokeEvent(string key)
			{
				if (Graph != null && Graph.GetNode(key) != null)
				{
					Graph.Run(key);
				}
			}
		}
	}
	public class QBuffSystem<BuffData> where BuffData : QBuffData<BuffData>, new()
	{
		const string AddEventKey = "添加";
		const string RemoveEventKey = "移除";
		const string StartEventKey = "开始";
		const string StopEventKey = "停止";
		const string TimeEventKey = "时间";
		/// <summary>
		/// 添加Buff时执行
		/// </summary>
		public event Action<QBuffData<BuffData>.Runtime> OnAddBuff = null;
		/// <summary>
		/// 移除Buff时执行
		/// </summary>
		public event Action<QBuffData<BuffData>.Runtime> OnRemoveBuff = null;
		/// <summary>
		/// 初始化Buff时执行 多个叠层只会执行一次
		/// </summary>
		public event Action<QBuffData<BuffData>.Runtime> OnStartBuff = null;
		/// <summary>
		/// 结束Buff时执行 多个叠层只会执行一次
		/// </summary>
		public event Action<QBuffData<BuffData>.Runtime> OnStopBuff = null;
		public QDictionary<string, QBuffData<BuffData>.Runtime> Buffs { get; private set; } = new QDictionary<string, QBuffData<BuffData>.Runtime>();
		private QDictionary<string, Action<string>> EventActions { get; set; } = new QDictionary<string, Action<string>>();
		public int this[string key]
		{
			get
			{
				if (Buffs.ContainsKey(key))
				{
					return Buffs[key].Count.IntValue;
				}
				return 0;
			}
		}
		public void Add(string key, int count = 1, float time = -1)
		{
			if (!Buffs.ContainsKey(key))
			{
				var buff = QBuffData<BuffData>.Runtime.Get(key);
				if (buff.Data.Megre.HasFlag(QBuffMergeMode.时间))
				{
					buff.Time.BaseValue = time;
					buff.Time.CurrentValue = buff.Time.MaxValue;
				}
				buff.Count.BaseValue = count;
				StartBuff(buff);
				for (int i = 0; i < count; i++)
				{
					OnAdd(buff);
				}

			}
			else
			{
				var buff = Buffs[key];
				if (buff.Data.Megre.HasFlag(QBuffMergeMode.叠层))
				{
					buff.Count.BaseValue += count;
					var oldValue = buff.Time.BaseValue;
					buff.Time.BaseValue = Mathf.Max(buff.Time.BaseValue, time);
					buff.Time.CurrentValue += buff.Time.BaseValue - oldValue;
				}
				else
				{
					buff.Time.BaseValue += time;
					buff.Time.CurrentValue += time;
					buff.Count.BaseValue = 1;
				}
				for (int i = 0; i < count; i++)
				{
					OnAdd(buff);
				}
			}
		}
		public void Remove(string key, int count = 1)
		{
			if (Buffs.ContainsKey(key))
			{
				var buff = Buffs[key];
				if (buff.Data.Megre.HasFlag(QBuffMergeMode.叠层))
				{
					buff.Count.BaseValue -= count;
					if (buff.Count.BaseValue <= 0)
					{
						StopBuff(buff);
					}
				}
				else
				{
					StopBuff(buff);
					buff.Count.BaseValue = 0;
					count = 1;
				}
				for (int i = 0; i < count; i++)
				{
					OnRemove(buff);
				}
			}
		}
		private void StartBuff(QBuffData<BuffData>.Runtime buff)
		{
			OnStartBuff?.Invoke(buff);
			Buffs.Add(buff.Key, buff);
			if (buff.Graph != null)
			{
				foreach (var node in buff.Graph.StartNode)
				{
					if (!node.Value.Name.EndsWith("每层"))
					{
						EventActions[node.Value.Name] += buff.InvokeEvent;
					}
				}
			}
			buff.InvokeEvent(StartEventKey);
		}
		private void StopBuff(QBuffData<BuffData>.Runtime buff)
		{
			OnStopBuff?.Invoke(buff);
			buff.InvokeEvent(StopEventKey);
			Buffs.Remove(buff.Key);
			if (buff.Graph != null)
			{
				foreach (var node in buff.Graph.StartNode)
				{
					if (!node.Value.Name.EndsWith("每层"))
					{
						EventActions[node.Value.Name] -= buff.InvokeEvent;
					}
				}
			}
		}
		private List<string> DelayRemove = new List<string>();
		public void Update(float deltaTime)
		{
			foreach (var kv in Buffs)
			{
				var buff = kv.Value;
				if (buff.Data.Megre.HasFlag(QBuffMergeMode.时间))
				{
					buff.Time.CurrentValue -= deltaTime;
					if (buff.Time.CurrentValue <= 0)
					{
						DelayRemove.Add(buff.Key);
					}
				}
				if (buff.TimeEvent != null)
				{
					if (buff.TimeEvent.Check(deltaTime))
					{
						buff.InvokeEvent(TimeEventKey);
					}
				}
			}
			if (DelayRemove.Count > 0)
			{
				foreach (var key in DelayRemove)
				{
					Remove(key);
				}
				DelayRemove.Clear();
			}
		}

		protected virtual void OnAdd(QBuffData<BuffData>.Runtime buff)
		{
			OnAddBuff?.Invoke(buff);
			if (buff.Graph != null)
			{
				buff.InvokeEvent(AddEventKey);
				if (buff.Data.Megre.HasFlag(QBuffMergeMode.叠层))
				{
					foreach (var node in buff.Graph.StartNode)
					{
						if (node.Value.Name.EndsWith("每层"))
						{
							EventActions[node.Value.Name] += buff.InvokeEvent;
						}
					}
				}
			}
		}
		protected virtual void OnRemove(QBuffData<BuffData>.Runtime buff)
		{
			OnRemoveBuff?.Invoke(buff);
			if (buff.Graph != null)
			{
				buff.InvokeEvent(RemoveEventKey);
				if (buff.Data.Megre.HasFlag(QBuffMergeMode.叠层))
				{
					foreach (var node in buff.Graph.StartNode)
					{
						if (node.Value.Name.EndsWith("每层"))
						{
							EventActions[node.Value.Name] -= buff.InvokeEvent;
						}
					}
				}
			}
		}
		public void SetValue<T>(string key, T value)
		{
			foreach (var buff in Buffs)
			{
				buff.Value.Graph.SetValue(key, value);
			}
		}
		public void SetValue<T>(T value)
		{
			foreach (var buff in Buffs)
			{
				buff.Value.Graph.SetValue(value);
			}
		}
		public void InvokeEvent(string key)
		{
			if (EventActions.ContainsKey(key))
			{
				EventActions[key]?.Invoke(key);
			}
			if (!key.EndsWith("每层"))
			{
				InvokeEvent(key + "每层");
			}
		}

	}
}
