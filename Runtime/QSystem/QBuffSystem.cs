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
		[QName("显示名")]
		public virtual string ViewName { get; set; }
		[QIgnore]
		private string _EffctInfo = "";
		[QName("效果说明")]
		public string EffectInfo
		{
			get
			{
				if (_EffctInfo.IsNull() && Effect != null)
				{
					var graph = Effect.Graph.CreateInstance();
					_EffctInfo = "";
					foreach (var kv in graph.StartNode)
					{
						_EffctInfo += graph.ToInfoString(kv.Key)+" ";
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
					Graph = Data.Effect.Graph.CreateInstance();
					Graph.RegisterValue(this);
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
					Graph.UnRegisterValue(this);
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
					buff.Time.OffsetValue = time;
					buff.Time.CurrentValue = buff.Time.MaxValue;
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
				if (buff.Data.Megre.HasFlag(QBuffMergeMode.时间))
				{
					buff.Time.OffsetValue += time;
					buff.Time.CurrentValue += time;
				}
				else
				{
					var oldValue = buff.Time.OffsetValue;
					buff.Time.OffsetValue = Mathf.Max(buff.Time.OffsetValue, time);
					buff.Time.CurrentValue += buff.Time.OffsetValue - oldValue;
				}
				if(buff.Data.Megre.HasFlag(QBuffMergeMode.叠层))
				{
					buff.Count.OffsetValue += count;
				}
				else
				{
					buff.Count.OffsetValue = 1;
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
				if (buff.Data.Megre.HasFlag(QBuffMergeMode.叠层))
				{
					buff.Count.OffsetValue -= count;
					if (buff.Count.OffsetValue <= 0)
					{
						PrivateRemove(buff);
					}
				}
				else
				{
					PrivateRemove(buff);
					buff.Count.OffsetValue = 0;
					count = 1;
				}
				for (int i = 0; i < count; i++)
				{
					OnRemove(buff);
				}
			}
		}
		private void PrivateAdd(QBuffData<BuffData>.Runtime buff)
		{
			Buffs.Add(buff.Key, buff);
			if (buff.Graph != null)
			{
				foreach (var node in buff.Graph.StartNode)
				{
					if(!buff.Data.Megre.HasFlag(QBuffMergeMode.叠层)|| !node.Value.Name.EndsWith("每层"))
					{
						EventActions[node.Value.Name] += buff.TriggerEvent;
					}
				}
			}
		}
		private void PrivateRemove(QBuffData<BuffData>.Runtime buff)
		{
			Buffs.Remove(buff.Key);
			if (buff.Graph != null)
			{
				foreach (var node in buff.Graph.StartNode)
				{
					if (!buff.Data.Megre.HasFlag(QBuffMergeMode.叠层) || !node.Value.Name.EndsWith("每层"))
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
				if(buff.Data.Megre.HasFlag(QBuffMergeMode.时间))
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
		public event Action<QBuffData<BuffData>.Runtime> OnAddBuff = null;
		public event Action<QBuffData<BuffData>.Runtime> OnRemoveBuff = null;
		protected virtual void OnAdd(QBuffData<BuffData>.Runtime buff)
		{
			if (buff.Graph != null)
			{
				buff.TriggerEvent(AddEventKey);
				foreach (var node in buff.Graph.StartNode)
				{
					var name = node.Value.Name;
					if (name != AddEventKey && name != RemoveEventKey)
					{
						if (!buff.Data.Megre.HasFlag(QBuffMergeMode.叠层) || node.Value.Name.EndsWith("每层"))
						{
							EventActions[node.Value.Name] += buff.TriggerEvent;
						}
					}
				}
			}
			OnAddBuff?.Invoke(buff);
		}
		protected virtual void OnRemove(QBuffData<BuffData>.Runtime buff)
		{
			if (buff.Graph != null)
			{
				buff.TriggerEvent(RemoveEventKey);
				foreach (var node in buff.Graph.StartNode)
				{
					var name = node.Value.Name;
					if (name != AddEventKey && name != RemoveEventKey)
					{
						if (!buff.Data.Megre.HasFlag(QBuffMergeMode.叠层) || node.Value.Name.EndsWith("每层"))
						{
							EventActions[node.Value.Name] -= buff.TriggerEvent;
						}
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
			if (!key.EndsWith("每层"))
			{
				TriggerEvent(key+"每层");
			}
		}
		
	}
}
