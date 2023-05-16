using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.FlowGraph;

namespace QTool
{
	public enum QBuffMergeMode
	{
		时间叠层,
		永久叠层,
		时间刷新,
		时间叠加,
		永久唯一,
	}
	public abstract class QEffectData<T> : QDataList<T> where T : QEffectData<T>, IKey<string>, new()
	{
		[QIgnore]
		private string _EffctInfo = "";
		[QName("效果说明")]
		public string EffectInfo
		{
			get
			{
				if (_EffctInfo.IsNull() && Effect != null)
				{
					var graph = Effect.Graph;
					_EffctInfo = "";
					foreach (var kv in graph.StartNode)
					{
						graph.ToInfoString(kv.Key);
					}
				}
				return _EffctInfo;
			}
			set => _EffctInfo = value;
		}
		[QName("效果")]
		public QFlowGraphAsset Effect { get; set; }
	}
	public abstract class QBuffData<T>:QEffectData<T> where T: QBuffData<T>,IKey<string>,new()
	{
		[QName("叠加方式")]
		public QBuffMergeMode Megre { get; protected set; } = QBuffMergeMode.时间叠层;
		[QName("时间事件")]
		public float TimeEvent { get; protected set; } = 0;
		public class QBuffRuntime : QRuntime<QBuffRuntime, T>
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
					Graph = Data.Effect.Graph.CreateInstance();
					Graph.RegisterMember(this);
				}
				if (Data.TimeEvent > 0)
				{
					TimeEvent = new QTimer(Data.TimeEvent);
				}
			}
			public override void OnDestroy()
			{
				if (Graph != null)
				{
					Graph.UnRegisterMember(this);
				}
			}
			public void TriggerEvent(string key)
			{
				if (Graph != null && Graph[key] != null)
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
		const string TimeEventKey = "时间";
		public QDictionary<string, QBuffData<BuffData>.QBuffRuntime> Buffs { get; private set; } = new QDictionary<string, QBuffData<BuffData>.QBuffRuntime>();
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
				var buff = QBuffData<BuffData>.QBuffRuntime.Get(key);
				switch (buff.Data.Megre)
				{
					case QBuffMergeMode.时间刷新:
					case QBuffMergeMode.时间叠加:
					case QBuffMergeMode.时间叠层:
						buff.Time.OffsetValue = time;
						buff.Time.CurrentValue = buff.Time.MaxValue;
						break;
					case QBuffMergeMode.永久唯一:
						buff.Time.OffsetValue = -1;
						buff.Time.CurrentValue = 0;
						break;
					default:
						break;
				}
				buff.Count.OffsetValue = count;
				PrivateAdd(buff);
				for (int i = 0; i < count; i++)
				{
					OnAdd(buff);
				}
			
			}
			else
			{
				var buff = Buffs[key];
				switch (buff.Data.Megre)
				{
					case QBuffMergeMode.时间刷新:
						var oldValue = buff.Time.OffsetValue;
						buff.Time.OffsetValue = Mathf.Max(buff.Time.OffsetValue, time);
						buff.Time.CurrentValue += buff.Time.OffsetValue - oldValue;
						count = 1;
						break;
					case QBuffMergeMode.时间叠加:
						buff.Time.OffsetValue += time;
						buff.Time.CurrentValue += time;
						count = 1;
						break;
					case QBuffMergeMode.永久叠层:
					case QBuffMergeMode.时间叠层:
						buff.Count.OffsetValue += count;
						break;
					default:
						return;
				}
				for (int i = 0; i < count; i++)
				{
					OnAdd(buff);
				}
			}
		}
		public void Remove(string key,int count=1)
		{
			if (Buffs.ContainsKey(key))
			{
				var buff = Buffs[key];
				switch (buff.Data.Megre)
				{
					case QBuffMergeMode.时间刷新:
					case QBuffMergeMode.永久唯一:
					case QBuffMergeMode.时间叠加:
						PrivateRemove(buff);
						buff.Count.OffsetValue = 0;
						count = 1;
						break;
					case QBuffMergeMode.永久叠层:
					case QBuffMergeMode.时间叠层:
						buff.Count.OffsetValue-= count;
						if (buff.Count.OffsetValue <= 0)
						{
							PrivateRemove(buff);
						}
						break;
					default:
						break;
				}
				for (int i = 0; i < count; i++)
				{
					OnRemove(buff);
				}
			}
		}
		private void PrivateAdd(QBuffData<BuffData>.QBuffRuntime buff)
		{
			Buffs.Add(buff.Key, buff);
			if (buff.Graph != null)
			{
				foreach (var node in buff.Graph.StartNode)
				{
					if (node.Value.Name.StartsWith("叠层"))
					{
						EventActions[node.Value.Name] += buff.TriggerEvent;
					}
				}
			}
		}
		private void PrivateRemove(QBuffData<BuffData>.QBuffRuntime buff)
		{
			Buffs.Remove(buff.Key);
			if (buff.Graph != null)
			{
				foreach (var node in buff.Graph.StartNode)
				{
					if (node.Value.Name.StartsWith("叠层"))
					{
						EventActions[node.Value.Name] -= buff.TriggerEvent;
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
				switch (buff.Data.Megre)
				{
					case QBuffMergeMode.时间叠层:
					case QBuffMergeMode.时间刷新:
					case QBuffMergeMode.时间叠加:
						buff.Time.CurrentValue -= deltaTime;
						if (buff.Time.CurrentValue <= 0)
						{
							DelayRemove.Add(buff.Key);
						}
						break;
					default:
						continue;
				}
				if (buff.TimeEvent != null)
				{
					if (buff.TimeEvent.Check(deltaTime))
					{
						buff.TriggerEvent(TimeEventKey);
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
		public event Action<QBuffData<BuffData>.QBuffRuntime> OnAddBuff = null;
		public event Action<QBuffData<BuffData>.QBuffRuntime> OnRemoveBuff = null;
		protected virtual void OnAdd(QBuffData<BuffData>.QBuffRuntime buff)
		{
			if (buff.Graph != null)
			{
				buff.TriggerEvent(AddEventKey);
				foreach (var node in buff.Graph.StartNode)
				{
					var name = node.Value.Name;
					if (name != AddEventKey && name != RemoveEventKey && !node.Value.Name.StartsWith("叠层"))
					{
						EventActions[node.Value.Name] += buff.TriggerEvent;
					}
				}
			}
			OnAddBuff?.Invoke(buff);
		}
		protected virtual void OnRemove(QBuffData<BuffData>.QBuffRuntime buff)
		{
			if (buff.Graph!=null)
			{
				buff.TriggerEvent(RemoveEventKey);
				foreach (var node in buff.Graph.StartNode)
				{
					var name = node.Value.Name;
					if (name != AddEventKey && name != RemoveEventKey && !node.Value.Name.StartsWith("叠层"))
					{
						EventActions[node.Value.Name] -= buff.TriggerEvent;
					}
				}
			}
			OnRemoveBuff?.Invoke(buff);
		}
		public void TriggerEvent(string key)
		{
			if (EventActions.ContainsKey(key))
			{
				EventActions[key]?.Invoke(key);
			}
			if (!key.StartsWith("叠层"))
			{
				TriggerEvent("叠层" + key);
			}
		}
		
	}
}
