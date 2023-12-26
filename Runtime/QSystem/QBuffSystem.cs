using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.FlowGraph;

namespace QTool
{
	public class QBuffSystem<BuffData> where BuffData : QBuffData<BuffData>, new()
	{
		const string AddEventKey = "添加";
		const string RemoveEventKey = "移除";
		const string BeginEventKey = "开始";
		const string EndEventKey = "结束";
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
		public event Action<QBuffData<BuffData>.Runtime> OnBeginBuff = null;
		/// <summary>
		/// 结束Buff时执行 多个叠层只会执行一次
		/// </summary>
		public event Action<QBuffData<BuffData>.Runtime> OnEndBuff = null;
		public QDictionary<string, QBuffData<BuffData>.Runtime> Buffs { get; private set; } = new QDictionary<string, QBuffData<BuffData>.Runtime>();
		private QDictionary<string, Func<string, IEnumerator>> EventActions { get; set; } = new QDictionary<string, Func<string, IEnumerator>>();
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
				buff.Time.BaseValue = time;
				buff.Time.CurrentValue = buff.Time.MaxValue;
				buff.Count.BaseValue = count;
				BeginBuff(buff);
				for (int i = 0; i < count; i++)
				{
					OnAdd(buff);
				}

			}
			else
			{
				var buff = Buffs[key];
				if (buff.Data.MaxCount == 1)
				{
					buff.Time.BaseValue += time;
					buff.Time.CurrentValue += time;
					buff.Count.BaseValue = 1;
				}
				else
				{
					if (buff.Data.MaxCount > 0)
					{
						buff.Count.BaseValue = Mathf.Min(buff.Count.BaseValue + count, buff.Data.MaxCount);
					}
					else
					{
						buff.Count.BaseValue += count;
					}
					var oldValue = buff.Time.BaseValue;
					buff.Time.BaseValue = Mathf.Max(buff.Time.BaseValue, time);
					buff.Time.CurrentValue += buff.Time.BaseValue - oldValue;
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
				if (buff.Data.MaxCount == 1)
				{
					EndBuff(buff);
					buff.Count.BaseValue = 0;
					count = 1;
				}
				else
				{
					buff.Count.BaseValue -= count;
					if (buff.Count.BaseValue <= 0)
					{
						EndBuff(buff);
					}
				}
				for (int i = 0; i < count; i++)
				{
					OnRemove(buff);
				}
			}
		}
		private void BeginBuff(QBuffData<BuffData>.Runtime buff)
		{
			OnBeginBuff?.Invoke(buff);
			Buffs.Add(buff.Key, buff);
			if (buff.Graph != null)
			{
				foreach (var node in buff.Graph.StartNode)
				{
					if (!node.Value.Name.EndsWith("每层"))
					{
						EventActions[node.Value.Name] += buff.InvokeEventIEnumerator;
					}
				}
			}
			buff.InvokeEvent(BeginEventKey);
		}
		private void EndBuff(QBuffData<BuffData>.Runtime buff)
		{
			OnEndBuff?.Invoke(buff);
			buff.InvokeEvent(EndEventKey);
			Buffs.Remove(buff.Key);
			if (buff.Graph != null)
			{
				foreach (var node in buff.Graph.StartNode)
				{
					if (!node.Value.Name.EndsWith("每层"))
					{
						EventActions[node.Value.Name] -= buff.InvokeEventIEnumerator;
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
				if (buff.Time.Value > 0)
				{
					buff.Time.CurrentValue -= deltaTime;
					if (buff.Time.CurrentValue <= 0)
					{
						DelayRemove.Add(buff.Key);
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
				if (buff.Data.MaxCount != 1)
				{
					foreach (var node in buff.Graph.StartNode)
					{
						if (node.Value.Name.EndsWith("每层"))
						{
							EventActions[node.Value.Name] += buff.InvokeEventIEnumerator;
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
				if (buff.Data.MaxCount != 1)
				{
					foreach (var node in buff.Graph.StartNode)
					{
						if (node.Value.Name.EndsWith("每层"))
						{
							EventActions[node.Value.Name] -= buff.InvokeEventIEnumerator;
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
			InvokeEventIEnumerator(key).Start();
		}
		public IEnumerator InvokeEventIEnumerator(string key)
		{
			if (!key.EndsWith("每层"))
			{
				yield return InvokeEventIEnumerator(key + "每层");
			}
			if (EventActions.ContainsKey(key) && EventActions[key] != null)
			{
				foreach (Func<string, IEnumerator> func in EventActions[key].GetInvocationList())
				{
					yield return func(key);
				}
			}
		}
		public void Clear()
		{
			foreach (var buff in Buffs.ToList())
			{
				Remove(buff.Key, buff.Count.IntValue);
			}
		}

	}



	public abstract class QBuffData<T> : QEffectData<T> where T : QBuffData<T>, IKey<string>, new()
	{
		[QName("最大层数")]
		public int MaxCount { get; protected set; } = -1;
		public class Runtime : QRuntime<Runtime, T>
		{
			[QName("层数")]
			public QRuntimeValue Count { get; private set; } = new QRuntimeValue();

			[QName("时间")]
			public QRuntimeRangeValue Time { get; private set; } = new QRuntimeRangeValue();
			public QFlowGraph Graph { get; private set; }
			protected override void Init(string key)
			{
				base.Init(key);
				if (Data.Effect != null)
				{
					Graph = Data.Effect.Graph.QDataCopy();
					Graph.RegisterValue(this);
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
				InvokeEventIEnumerator(key).Start();
			}
			public IEnumerator InvokeEventIEnumerator(string key)
			{
				yield return Graph?.InvokeEventIEnumerator(key);
			}
		}
	}
	public abstract class QEffectData<T> : QDataList<T> where T : QEffectData<T>, IKey<string>, new()
	{
		private string GetAssetPath(string key)
		{
			return "Assets/Resources/" + nameof(QFlowGraph) + "/" + typeof(T).Name + "/" + key + ".asset";
		}
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
	}
}
