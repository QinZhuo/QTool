using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.FlowGraph;

namespace QTool
{
	public abstract class QItemData<T>: QEffectData<T> where T: QItemData<T>,IKey<string>,new()
	{
		[QName("最大层数")]
		public int MaxCount { get; protected set; } = -1;
		public class Runtime : QRuntime<Runtime, T>
		{
			[QName("层数")]
			public QRuntimeValue Count { get; private set; } = new QRuntimeValue();
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
	public class QItemSystem<ItemData> where ItemData : QItemData<ItemData>, new()
	{
		const string AddEventKey = "添加";
		const string RemoveEventKey = "移除";
		public QDelayList<QItemData<ItemData>.Runtime> Items { get; private set; } = new QDelayList<QItemData<ItemData>.Runtime>();
		private QDictionary<string, QDelayList<QItemData<ItemData>.Runtime>> EventItems { get; set; } = new QDictionary<string, QDelayList<QItemData<ItemData>.Runtime>>(key => new QDelayList<QItemData<ItemData>.Runtime>());
		public void Clear()
		{
			Items.Clear();
			EventItems.Clear();
		}
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
		public void Add(QItemData<ItemData>.Runtime item)
		{
			Items.Add(item);
			for (int i = 0; i < item.Count.IntValue; i++)
			{
				OnAdd(item);
			}
		}
		public void Remove(QItemData<ItemData>.Runtime item)
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
		public void Update(float deltaTime)
		{
			Items.Update();
			foreach (var items in EventItems)
			{
				items.Value.Update();
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
						item.Count.BaseValue += count;
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
						item.Count.BaseValue = item.Data.MaxCount;
					}
				}
			}
			if (count >= 0)
			{
				var item = QItemData<ItemData>.Runtime.Get(key);
				item.Count.BaseValue = count;
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
						item.Count.BaseValue -= count;
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
						item.Count.BaseValue -= count;
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
		public event Action<QItemData<ItemData>.Runtime> OnAddItem = null;
		public event Action<QItemData<ItemData>.Runtime> OnRemoveItem = null;
		protected virtual void OnAdd(QItemData<ItemData>.Runtime item)
		{
			OnAddItem?.Invoke(item);
			if (item.Graph != null)
			{
				item.InvokeEvent(AddEventKey);
				foreach (var node in item.Graph.StartNode)
				{
					var name = node.Value.Name;
					if (name != AddEventKey && name != RemoveEventKey)
					{
						EventItems[name].Add(item);
					}
				}
			}
		}
		protected virtual void OnRemove(QItemData<ItemData>.Runtime item)
		{
			OnRemoveItem?.Invoke(item);
			if (item.Graph!=null)
			{
				item.InvokeEvent(RemoveEventKey);
				foreach (var node in item.Graph.StartNode)
				{
					var name = node.Value.Name;
					if (name != AddEventKey && name != RemoveEventKey)
					{
						EventItems[name].Remove(item);
					}
				}
			}
		}
		public void SetValue<T>(string key, T value)
		{
			foreach (var buff in Items)
			{
				buff.Graph.SetValue(key, value);
			}
		}
		public void SetValue<T>(T value)
		{
			foreach (var buff in Items)
			{
				buff.Graph.SetValue(value);
			}
		}
		public void InvokeEvent(string key)
		{
			InvokeEventIEnumerator(key).Start();
		}
		public IEnumerator InvokeEventIEnumerator(string key,params string[] ignores)
		{
			if (EventItems.ContainsKey(key))
			{
				foreach (var item in EventItems[key])
				{
					if (ignores.Contains(item.Key)) continue;
					yield return item.InvokeEventIEnumerator(key);
				}
			}
		}
		public void InvokeEventImmediate(string key,params string[] ignores)
		{
			if (EventItems.ContainsKey(key))
			{
				foreach (var item in EventItems[key])
				{
					if (ignores.Contains(item.Key)) continue;
					try
					{
						item.InvokeEventIEnumerator(key).RunImmediate();
					}
					catch (Exception e)
					{
						throw new Exception("item " + item, e);
					}
				}
			}
		}
	}
}
