using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.FlowGraph;

namespace QTool
{
	public abstract class QBuffData<T>:QDataList<T> where T: QBuffData<T>,IKey<string>,new()
	{
		[QName("效果")]
		public QFlowGraph Graph { get; private set; }
	}
	public class QBuffSystem<T> where T : QBuffData<T>, new()
	{
		public class QBuffRuntime:QRuntimeObject<T>
		{
			public int Count { get; set; } = 0;
			public float Time { get; set; }
			public float CurrentTime { get; set; }
			public QFlowGraph Graph { get; private set; }
			public override void Init(string key)
			{
				base.Init(key);
				Graph = Data.Graph.CreateInstance();
			}
		}
		public QDictionary<string, QBuffRuntime> Buffs { get; private set; } = new QDictionary<string, QBuffRuntime>();
		
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
		public void Add(string key,float time=-1)
		{
			if (Buffs.ContainsKey(key))
			{
				var buff = Buffs[key];
				if (buff.Time > 0)
				{
					buff.Time = Mathf.Max(buff.Time, time);
					buff.CurrentTime = Mathf.Max(buff.CurrentTime, time);
				}
				else
				{
					buff.Count += 1;
				}
				RunGraph(buff,"添加");
			}
			else
			{
				var buff = new QBuffRuntime
				{
					Time = time,
					Count = 1,
					CurrentTime = time
				};
				buff.Init(key);
				Buffs.Add(key, buff);
				RunGraph(buff, "添加");
			}
		}
		protected virtual void RunGraph(QBuffRuntime buff,string key)
		{
			buff.Graph.Run(key);
		}
		public void Remove(string key)
		{
			if (Buffs.ContainsKey(key))
			{
				var buff = Buffs[key];
				if (buff.Time > 0)
				{
					Buffs.Remove(key);
				}
				else
				{
					buff.Count--;
					if (buff.Count == 0)
					{
						Buffs.Remove(key);
					}
				}
				RunGraph(buff, "移除");
			}
		}

	}
}
