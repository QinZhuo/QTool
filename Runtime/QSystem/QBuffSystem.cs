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
	public abstract class QBuffData<T>:QDataList<T> where T: QBuffData<T>,IKey<string>,new()
	{
		[QName("叠加方式")]
		public QBuffMergeMode Megre { get; private set; } = QBuffMergeMode.时间叠层;
		[QName("触发间隔")]
		public float TimeEvent { get; private set; } = 0;
		[QName("效果")]
		public QFlowGraph Graph { get; private set; }
	}
	public class QBuffSystem<T> where T : QBuffData<T>, new()
	{
		public class QBuffRuntime:QRuntime<QBuffRuntime,T>
		{
			public int Count { get; set; } = 1;
			public float Time { get;internal set; } = -1;
			public float CurrentTime { get; set; } = 0;
			public QFlowGraph Graph { get; private set; }
			public override void Init(string key)
			{
				base.Init(key);
				Graph = Data.Graph.CreateInstance();
			}
			public void TriggerEvent(string key)
			{
				Graph.Run(key);
			}
		}
		public QDictionary<string, QBuffRuntime> Buffs { get; private set; } = new QDictionary<string, QBuffRuntime>();
		private QDictionary<string, Action<string>> EventActions { get; set; } = new QDictionary<string, Action<string>>();
		public int this[string key]
		{
			get
			{
				if (Buffs.ContainsKey(key))
				{
					return Buffs[key].Count;
				}
				return 0;
			}
		}
		const string AddEventKey = "添加";
		const string RemoveEventKey = "移除";
		public void Add(string key,float time=-1,int count=1)
		{
			if (!Buffs.ContainsKey(key))
			{
				var buff = QBuffRuntime.Get(key);
				switch (buff.Data.Megre)
				{
					case QBuffMergeMode.时间刷新:
					case QBuffMergeMode.时间叠加:
					case QBuffMergeMode.时间叠层:
						buff.Time = time;
						break;
					case QBuffMergeMode.永久唯一:
						buff.Time = -1;
						buff.CurrentTime =0;
						break;
					default:
						break;
				}
				buff.Count = count;
				buff.Init(key);
				Buffs.Add(key, buff);
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
						buff.Time = Mathf.Max(buff.Time, time);
						count = 1;
						break;
					case QBuffMergeMode.时间叠加:
						buff.Time += time;
						count = 1;
						break;
					case QBuffMergeMode.永久叠层:
					case QBuffMergeMode.时间叠层:
						buff.Count += count;
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
						Buffs.Remove(key);
						count = 1;
						break;
					case QBuffMergeMode.永久叠层:
					case QBuffMergeMode.时间叠层:
						buff.Count-= count;
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
						buff.Time -= deltaTime;
						if (buff.Time <= 0)
						{
							DelayRemove.Add(buff.Key);
						}
						break;
					default:
						continue;
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
		protected virtual void OnAdd(QBuffRuntime buff)
		{
			buff.TriggerEvent(AddEventKey);
			foreach (var node in buff.Graph.NodeList)
			{
				if (!node.Is(nameof(QFlowGraphNode.Start))) continue;
				if (node.Name == AddEventKey || node.Name == RemoveEventKey) continue;
				EventActions[node.Name] += buff.TriggerEvent;
			}
		}
		protected virtual void OnRemove(QBuffRuntime buff)
		{
			buff.TriggerEvent(RemoveEventKey);
			foreach (var node in buff.Graph.NodeList)
			{
				if (!node.Is(nameof(QFlowGraphNode.Start))) continue;
				if (node.Name == AddEventKey || node.Name == RemoveEventKey) continue;
				EventActions[node.Name] -= buff.TriggerEvent;
			}
		}
		public void TriggerEvent(string key)
		{
			if (EventActions.ContainsKey(key))
			{
				EventActions[key]?.Invoke(key);
			}
		}
	}
}
