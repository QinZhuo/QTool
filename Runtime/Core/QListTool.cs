using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using QTool.Reflection;
using System.Linq;
namespace QTool
{

	public interface IKey<KeyType>
	{
		KeyType Key { get; set; }
	}


	public class QList<T> : List<T>
	{
		public new T this[int index]
		{
			get
			{
				if (index < 0)
				{
					return default;
				}
				if (index >= Count)
				{
					if (AutoCreate == null)
					{
						return default;
					}
					else
					{
						for (int i = Count; i <= index; i++)
						{
							Add(AutoCreate());
						}
					}
				}
				return base[index];
			}
			set
			{
				if (AutoCreate == null)
				{
					for (int i = Count; i <= index; i++)
					{
						Add(default);
					}
				}
				else
				{
					for (int i = Count; i <= index; i++)
					{
						Add(AutoCreate());
					}
				}
				base[index] = value;
			}
		}
		[QIgnore]
		public Func<T> AutoCreate { get; set; }
		public QList()
		{

		}
		public QList(int count):base(count)
		{

		}
		public QList(Func<T> AutoCreate)
		{
			this.AutoCreate = AutoCreate;
		}
		public override string ToString()
		{
			return this.ToOneString();
		}
	}
	public class QDelayList<T> : List<T>
	{
		public List<T> AddList { get; private set; } = new List<T>();
		public List<T> RemoveList { get; private set; } = new List<T>();
		public new bool Contains(T item)
		{
			return !RemoveList.Contains(item) && (base.Contains(item) || AddList.Contains(item));
		}
		public new void Add(T item)
		{
			AddList.Add(item);
		}
		public new void Remove(T item)
		{
			if (AddList.Contains(item))
			{
				AddList.Remove(item);
			}
			RemoveList.Add(item);
		}
		public void UpdateRemove()
		{
			if (RemoveList.Count > 0)
			{
				base.RemoveAll(item => RemoveList.Contains(item));
				RemoveList.Clear();
			}
		}
		public void UpdateAdd()
		{
			if (AddList.Count > 0)
			{
				base.AddRange(AddList);
				AddList.Clear();
			}
		}
		public void Update()
		{
			UpdateAdd();
			UpdateRemove();
		}
		public new void Clear()
		{
			base.Clear();
			AddList.Clear();
			RemoveList.Clear();
		}
	}
	public class QList<TKey, T> : QList<T> where T : IKey<TKey>
	{
		[QIgnore]
		private QDictionary<TKey, T> Cache = new QDictionary<TKey, T>();
		[QIgnore]
		public bool Changed { get; private set; }
		internal void CheckFreshCache()
		{
			if (Count == Cache.Count) return;
			Cache.Clear();
			foreach (var kv in this)
			{
				if (kv.Key.IsNull()) continue;
				Cache[kv.Key] = kv;
			}
		}
		public QList()
		{
		}
		public QList(Func<T> AutoCreate) : base(AutoCreate)
		{
		}
		public QList(int count):base(count)
		{

		}

		public new void Add(T value)
		{
			if (value != null)
			{
				Set(value.Key, value);
			}
		}
		public new bool Contains(T value)
		{
			if (value == null) return false;
			return ContainsKey(value.Key);
		}
		public bool ContainsKey(TKey key)
		{
			if (key == null)
			{
				Debug.LogError("key is null");
				return false;
			}
			CheckFreshCache();
			return Cache.ContainsKey(key);
		}
		public T Get(TKey key)
		{
			if (key == null)
			{
				Debug.LogError("key is null");
				return default;
			}
			if (!ContainsKey(key)&& AutoCreate!=null)
			{
				var value = AutoCreate();
				value.Key = key;
				Add(value);
			}
			if (!Cache.ContainsKey(key))
			{
				return default;
			}
			else
			{
				return Cache[key];
			}
		}
		public void Set(TKey key, T value)
		{
			if (key == null)
			{
				Debug.LogError("key is null");
			}
			if (ContainsKey(key))
			{
				this.Set<T, TKey>(key, value);
			}
			else
			{
				value.Key = key;
				base.Add(value);
			}
			Changed = true;
			Cache[key] = value;
		}
		public new void AddRange(IEnumerable<T> collection)
		{
			foreach (var obj in collection)
			{
				Cache.Add(obj);
			}
			base.AddRange(collection);
			Changed = true;
		}
		public void Remove(TKey key)
		{
			RemoveKey(key);
		}
		List<TKey> keyList = new List<TKey>();
		public new void RemoveAll(Predicate<T> match)
		{
			keyList.Clear();
			if (match != null)
			{
				foreach (var item in this)
				{
					if (item == null) return;
					if (match(item))
					{
						keyList.Add(item.Key);
					}
				}
			}
			foreach (var key in keyList)
			{
				RemoveKey(key);
			}
		}
		public T this[TKey key]
		{
			get
			{
				return Get(key);
			}
			set
			{
				Set(key, value);
			}
		}
		public new void Remove(T obj)
		{
			if (obj != null)
			{
				Changed = true;
				base.Remove(obj);
				Cache.Remove(obj.Key);
			}
		}
		public void RemoveKey(TKey key)
		{
			Remove(this[key]);
		}
		public new void Clear()
		{
			Changed = true;
			base.Clear();
			Cache.Clear();
		}

	}

	public class QKeyValueList<TKey, T> : QList<TKey, QKeyValue<TKey, T>>
	{
		public new T this[TKey key]
		{
			get=> base[key].Value;
			set => base[key] =new QKeyValue<TKey, T>(key, value);
		}
		public QKeyValueList()
		{
			AutoCreate = () => new QKeyValue<TKey, T>();
		}
		public QKeyValueList(int count):base(count)
		{
			AutoCreate = () => new QKeyValue<TKey, T>();
		}
	}
	public class QDictionary<TKey, TValue> : Dictionary<TKey, TValue>
	{
		public new TValue this[TKey key]
		{
			get
			{
				return Get(key);
			}
			set
			{
				Add(key, value);
			}
		}
		public Func<TKey, TValue> AutoCreate = null;
		internal Action<TKey> OnChange = null;
		public QDictionary()
		{
		}
		public new void Add(TKey key, TValue value)
		{
			this.Set(key, value);
			if (OnChange != null)
			{
				if (this[key].IsNull())
				{
					base.Remove(key);
				}
				OnChange(key);
			}
		}
		public new void Remove(TKey key)
		{
			base.Remove(key);
			OnChange?.Invoke(key);
		}
	

		public TValue Get(TKey key)
		{
			if (key == null) return default;
			if (!ContainsKey(key))
			{
				if (AutoCreate != null)
				{
					Add(key, AutoCreate(key));
				}
				else
				{
					return default;
				}
			}
			return base[key];
		}
		public QDictionary(Func<TKey, TValue> AutoCreate)
		{
			this.AutoCreate = AutoCreate;
		}
	}
	public class QConnect<TKey,TValue>:IEnumerable
	{
		protected Dictionary<TKey, TValue> KeyToValue = new Dictionary<TKey, TValue>();
		protected Dictionary<TValue, TKey> ValueToKey = new Dictionary<TValue, TKey>();
		public int Count => KeyToValue.Count;
		public IEnumerator GetEnumerator() => KeyToValue.GetEnumerator();
		public bool ContainsKey(TKey key) => KeyToValue.ContainsKey(key);
		public bool ContainsValue(TValue value) => ValueToKey.ContainsKey(value);
		public TValue this[TKey key]
		{
			get
			{
				return KeyToValue[key];
			}
		}
		public TKey this[TValue value]
		{
			get
			{
				return ValueToKey[value];
			}
		}
		
		public virtual void Set(TKey key,TValue value)
		{
			Remove(key);
			Remove(value);
			KeyToValue[key] = value;
			ValueToKey[value] = key;
		}
		public void Set(TValue value, TKey key)
		{
			Set(key, value);
		}
		public virtual void Remove(TKey key)
		{
			if (KeyToValue.ContainsKey(key))
			{
				ValueToKey.Remove(KeyToValue[key]);
				KeyToValue.Remove(key);
			}
		}
		public void Remove(TValue value)
		{
			if (ValueToKey.ContainsKey(value))
			{
				Remove(ValueToKey[value]);
			}
		}
		public virtual void Clear()
		{
			KeyToValue.Clear();
			ValueToKey.Clear();
		}
	}

	[Serializable]
	public struct QKeyValue<TKey, T> : IKey<TKey>
	{
		public TKey Key { get; set; }
		public T Value { get; set; }
		public QKeyValue(TKey key, T value)
		{
			Key = key;
			Value = value;
		}
		public override string ToString()
		{
			return "{" + Key + ":" + Value + "}";
		}
	}
	public static class QListTool
	{
		
		public static int ClearNull<T>(this List<T> array)
		{
			return array.RemoveAll(obj => obj.IsNull());
		}
		public static int RemoveSpace(this List<string> array)
		{
			return array.RemoveAll(obj => string.IsNullOrWhiteSpace(obj));
		}
		public static IList<T> Replace<T>(this IList<T> array, int indexA, int indexB)
		{
			if (indexA == indexB) return array;
			var temp = array[indexA];
			array[indexA] = array[indexB];
			array[indexB] = temp;
			return array;
		}
		public static int NextIndex(this int curIndex,int count)
		{
			curIndex++;
			if (curIndex >= count)
			{
				curIndex = 0;
			}
			return curIndex;
		}
		public static IList CreateAt(this IList list, QSerializeType typeInfo, int index = -1)
		{
			var newObj = (index < 0||list[index]==null)? typeInfo.ElementType.CreateInstance():list[index].QDataCopy();
			if (index < 0) 
			{
				index = 0;
			}
			if (list.IsFixedSize)
			{
				if (typeInfo.ArrayRank == 1)
				{
					var newList = typeInfo.Type.CreateInstance(null, list.Count + 1) as IList;

					for (int i = 0; i < index; i++)
					{
						newList[i] = list[i];
					}
					newList[index] = newObj;
					for (int i = index + 1; i < newList.Count; i++)
					{
						newList[i] = list[i - 1];
					}
					return newList;
				}
			}
			else
			{
				list.Add(newObj);
			}
			return list;

		}
		public static IList RemoveAt(this IList list, QSerializeType typeInfo, int index)
		{
			if (list.IsFixedSize)
			{
				if (typeInfo.ArrayRank == 1)
				{
					var newList = typeInfo.Type.CreateInstance(null,list.Count - 1) as IList;

					for (int i = 0; i < index; i++)
					{
						newList[i] = list[i];
					}
					for (int i = index; i < newList.Count; i++)
					{
						newList[i] = list[i + 1];
					}
					return newList;
				}
			}
			else
			{
				list.RemoveAt(index);
			}
			return list;

		}
		public static bool Contains<T>(this IList<T> array, T obj)
		{
			if (array == null)
				return false;
			return array.IndexOf(obj) >= 0;
		}
		public static bool ContainsKey<T, KeyType>(this IList<T> array, KeyType key) where T : IKey<KeyType>
		{
			return array.ContainsKey(key, (item) => item.Key);
		}
		public static void RemoveKey<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key)
		{
			dic.Remove(key);
		}
		public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dic, Func<KeyValuePair<TKey, TValue>, bool> keyFunc, IList<TKey> buffer)
		{
			buffer.Clear();
			foreach (var kv in dic)
			{
				if (keyFunc(kv))
				{
					buffer.Add(kv.Key);
				}
			}
			if (dic is QDictionary<TKey, TValue> QDic)
			{
				foreach (var key in buffer)
				{
					QDic.Remove(key);
				}
			}
			else
			{
				foreach (var key in buffer)
				{
					dic.Remove(key);
				}
			}
		}
		public static void Add<TKey, TValue>(this IDictionary<TKey, TValue> dic, TValue value) where TValue : IKey<TKey>
		{
			Set(dic, value);
		}
		public static void Set<TKey, TValue>(this IDictionary<TKey, TValue> dic, TValue value) where TValue : IKey<TKey>
		{
			Set(dic,value.Key, value);
		}
		public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue value)
		{
			if (!dic.ContainsKey(key))
			{
				Set(dic, key, value);
			}
			return dic[key];
		}
		public static void Set<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue value)
		{
			lock (dic)
			{
				if (dic.ContainsKey(key))
				{
					dic[key] = value;
				}
				else
				{
					dic.Add(key, value);
				}
			}
		}
		public static List<ObjT> ToList<KeyT, ObjT>(this IDictionary<KeyT, ObjT> dic, List<ObjT> list = null)
		{
			if (list == null)
			{
				list = new List<ObjT>();
			}
			else
			{
				list.Clear();
			}
			foreach (var kv in dic)
			{
				list.Add(kv.Value);
			}
			return list;
		}
		//public static Dictionary<KeyT, ObjT> ToDictionary<KeyT, ObjT>(this IList<ObjT> list, Dictionary<KeyT, ObjT> dic = null) where ObjT : IKey<KeyT>
		//{
		//	if (dic == null)
		//	{
		//		dic = new Dictionary<KeyT, ObjT>();
		//	}
		//	else
		//	{
		//		dic.Clear();
		//	}
		//	foreach (var item in list)
		//	{
		//		if (item == null) continue;
		//		dic.Add(item.Key, item);
		//	}
		//	return dic;
		//}
		public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key)
		{
			if (dic.ContainsKey(key))
			{
				return dic[key];
			}
			else
			{
				return default;
			}
		}
		public static bool ContainsKey<T, TKey>(this IList<T> array, TKey key, Func<T, TKey> keyGetter)
		{
			if (key == null || array == null)
			{
				return false;
			}
			for (int i = array.Count - 1; i >= 0; i--)
			{
				var value = array[i];
				if (value == null) continue;
				if (key.Equals(keyGetter(value)))
				{
					return true;
				}
			}
			return false;
		}
		public static T Get<T>(this IList<T> array, int index)
		{
			if (index < 0 || index >= array.Count) return default;
			return array[index];
		}
		public static T Get<T, TKey>(this IList<T> array, TKey key) where T : IKey<TKey>
		{
			return array.FirstOrDefault(item => Equals(item.Key, key));
		}
		public static T Get<T>(this IList<T> array, string key) where T : UnityEngine.Object
		{
			return array.FirstOrDefault(item => Equals(item?.name, key));
		}
		public static TValue Get<TKey,TValue>(this IDictionary<TKey, TValue> array, TKey key, Func<TValue, TKey> keyGetter)
		{
			if (key == null)
			{
				return default;
			}
			foreach (var kv in array)
			{
				if (kv.Value == null) continue;
				if (key.Equals(keyGetter(kv.Value)))
				{
					return kv.Value;
				}
			}
			return default;
		}
		public static List<T> GetList<T, TKey>(this IList<T> array, TKey key, List<T> tempList = null) where T : IKey<TKey>
		{
			var list = tempList == null ? new List<T>() : tempList;
			for (int i = 0; i < array.Count; i++)
			{
				var value = array[i];
				if (key.Equals(value.Key))
				{
					list.Add(value);
				}
			}

			return list;
		}
		public static T StackPeek<T>(this IList<T> array)
		{
			if (array == null || array.Count == 0)
			{
				return default;
			}
			return array[array.Count - 1];
		}
		public static T QueuePeek<T>(this IList<T> array)
		{
			if (array == null || array.Count == 0)
			{
				return default;
			}
			return array[0];
		}
		public static object[] ToObjects<T>(this IList<T> array)
		{
			var objs = new object[array.Count];
			for (int i = 0; i < array.Count; i++)
			{
				objs[i] = array[i];
			}
			return objs;
		}
		public static void Enqueue<T>(this IList<T> array, T obj)
		{
			array.Add(obj);
		}
		public static void Push<T>(this IList<T> array, T obj)
		{
			array.Add(obj);
		}
		public static T Pop<T>(this IList<T> array)
		{
			if (array == null || array.Count == 0)
			{
				return default;
			}
			var obj = array.StackPeek();
			array.RemoveAt(array.Count - 1);
			return obj;
		}
		public static T Dequeue<T>(this IList<T> array)
		{
			if (array == null || array.Count == 0)
			{
				return default;
			}
			var obj = array.QueuePeek();
			array.RemoveAt(0);
			return obj;
		}
		public static void AddCheckExist<T>(this IList<T> array, params T[] objs)
		{
			foreach (var obj in objs)
			{
				if (!array.Contains(obj))
				{
					array.Add(obj);
				}
			}
		}
		public static void RemoveKey<T, KeyType>(this IList<T> array, KeyType key) where T : IKey<KeyType>
		{
			var old = array.Get(key);
			if (old != null)
			{
				array.Remove(old);
			}
		}
		public static void Set<T, KeyType>(this IList<T> array, KeyType key, T value) where T : IKey<KeyType>
		{
			array.RemoveKey(key);
			value.Key = key;
			array.Add(value);
		}
		public static int IndexOf<T>(this T[] array,T value)
		{
			return Array.IndexOf(array, value);
		}
		public static T GetAndCreate<T, KeyType>(this IList<T> array, KeyType key, System.Action<T> creatCallback = null) where T : IKey<KeyType>, new()
		{
			var value = array.Get(key);
			if (value != null)
			{
				return value;
			}
			else
			{
				var t = new T { Key = key };
				creatCallback?.Invoke(t);
				array.Add(t);
				return t;
			}
		}

	}
}
