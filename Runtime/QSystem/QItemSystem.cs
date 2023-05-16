using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.FlowGraph;

namespace QTool
{
	public abstract class QItemData<T>: QEffectData<T> where T: QItemData<T>,IKey<string>,new()
	{
		[QName("最大叠层")]
		public int MaxCount { get; protected set; } = 1;
		public class QItemRuntime : QRuntime<QItemRuntime, T>
		{
			[QName("层数")]
			public QRuntimeValue Count { get; private set; } = new QRuntimeValue();
			public QFlowGraph Graph { get; private set; }
			protected override void Init(string key)
			{
				base.Init(key);
				if (Data.Effect != null)
				{
					Graph = Data.Effect.Graph.CreateInstance();
					Graph.RegisterMember(this);
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
				if (Graph != null&& Graph[key] != null)
				{
					Graph.Run(key);
				}
			}
		}
	}
	public class QItemSystem<ItemData> where ItemData : QItemData<ItemData>, new()
	{
		const string AddEventKey = "添加";
		const string RemoveEventKey = "移除";
		public List<QItemData<ItemData>.QItemRuntime> Items { get; private set; } = new List<QItemData<ItemData>.QItemRuntime>();
		private QDictionary<string, Action<string>> EventActions { get; set; } = new QDictionary<string, Action<string>>();
		public int this[string key]
		{
			get
			{
				var count = 0;
				foreach (var item in Items)
				{
					if (key == item.Key)
					{
						count += item.Count.IntValue;
					}
				}
				return count;
			}
		}
		public void AddBuff(QItemData<ItemData>.QItemRuntime item)
		{
			Items.Add(item);
			for (int i = 0; i < item.Count.IntValue; i++)
			{
				OnAdd(item);
			}
		}
		public void RemoveBuff(QItemData<ItemData>.QItemRuntime item)
		{
			if (Items.Contains(item))
			{
				Items.Remove(item);
				for (int i = 0; i < item.Count.IntValue; i++)
				{
					OnRemove(item);
				}
			}
		}
		public void Add(string key, int count = 1)
		{
			foreach (var item in Items)
			{
				if (item.Key == key)
				{
					if (item.Count.IntValue == item.Data.MaxCount)
					{
						continue;
					}
					else if (item.Data.MaxCount <= 0 || item.Count.IntValue + count <= item.Data.MaxCount)
					{
						item.Count.OffsetValue += count;
						for (int i = 0; i < count; i++)
						{
							OnAdd(item);
						}
						count = 0;
						break;
					}
					else
					{
						var tCount= item.Data.MaxCount - item.Count.IntValue;
						for (int i = 0; i < tCount; i++)
						{
							OnAdd(item);
						}
						count -= tCount;
						item.Count.OffsetValue = item.Data.MaxCount;
					}
				}
			}
			if (count >= 0)
			{
				var item = QItemData<ItemData>.QItemRuntime.Get(key);
				item.Count.OffsetValue = count;
				Items.Add(item);
				for (int i = 0; i < count; i++)
				{
					OnAdd(item);
				}
			}
		}
		public void Remove(string key, int count = 1)
		{
			Items.RemoveAll((item) =>
			{
				if (item.Key == key && count > 0)
				{
					if (item.Count.IntValue == count)
					{
						item.Count.OffsetValue -= count;
						for (int i = 0; i < count; i++)
						{
							OnRemove(item);
						}
						count = 0;
					}
					else if (item.Count.IntValue > count)
					{
						for (int i = 0; i < count; i++)
						{
							OnRemove(item);
						}
						item.Count.OffsetValue -= count;
					}
					else
					{
						for (int i = 0; i < item.Count.IntValue; i++)
						{
							OnRemove(item);
						}
						count -= item.Count.IntValue;
					}
				}
				return item.Count.IntValue <= 0;
			});
		}
		public event Action<QItemData<ItemData>.QItemRuntime> OnAddItem = null;
		public event Action<QItemData<ItemData>.QItemRuntime> OnRemoveItem = null;
		protected virtual void OnAdd(QItemData<ItemData>.QItemRuntime item)
		{
			if (item.Graph != null)
			{
				item.TriggerEvent(AddEventKey);
				foreach (var node in item.Graph.StartNode)
				{
					var name = node.Value.Name;
					if (name != AddEventKey && name != RemoveEventKey)
					{
						EventActions[name] += item.TriggerEvent;
					}
				}
			}
			OnAddItem?.Invoke(item);
		}
		protected virtual void OnRemove(QItemData<ItemData>.QItemRuntime item)
		{
			if (item.Graph!=null)
			{
				item.TriggerEvent(RemoveEventKey);
				foreach (var node in item.Graph.StartNode)
				{
					var name = node.Value.Name;
					if (name != AddEventKey && name != RemoveEventKey)
					{
						EventActions[name] -= item.TriggerEvent;
					}
				}
			}
			OnRemoveItem?.Invoke(item);
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
