using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using QTool.Inspector;
using QTool.Reflection;
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
	public class QDictionary<TKey, T> : Dictionary<TKey, T>
	{
		public new T this[TKey key]
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
		public Func<TKey, T> AutoCreate { protected set; get; }
		public QDictionary()
		{
		}
		public new void Add(TKey key, T value)
		{
			this.Set(key, value);
			
		}
		public T Get(TKey key)
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
		public QDictionary(Func<TKey, T> AutoCreate)
		{
			this.AutoCreate = AutoCreate;
		}
	}

	[System.Serializable]
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
		public static string SplitEndString(this string str, string splitStart)
		{
			if (str.Contains(splitStart))
			{

				return str.Substring(str.LastIndexOf(splitStart) + splitStart.Length);
			}
			else
			{
				return str;
			}
		}
		public static string SplitStartString(this string str, string splitStart)
		{
			if (str.Contains(splitStart))
			{

				return str.Substring(0, str.IndexOf(splitStart));
			}
			else
			{
				return str;
			}
		}
		public static string ForeachBlockValue(this string value, char startChar, char endChar, Func<string, string> action)
		{
			if (string.IsNullOrEmpty(value)) { return value; }
			var start = value.IndexOf(startChar);
			if (start < 0) return value;
			var end = value.IndexOf(endChar, start + 1);
			if (start < 0 || end < 0) return value;
			while (start >= 0 && end >= 0)
			{
				var key = "";
				if (end > start)
				{
					key = value.Substring(start + 1, end - start - 1);
				}
				var result = action(key);
				value = value.Substring(0, start) + result + value.Substring(end + 1);
				end += result.Length - key.Length - 2;
				start = value.IndexOf(startChar, end + 1);
				if (start < 0) break;
				end = value.IndexOf(endChar, start);
			}
			return value;
		}
		public static string GetBlockValue(this string value, char startChar, char endChar)
		{
			var start = value.IndexOf(startChar) + 1;
			var end = value.IndexOf(endChar, start);
			if (end >= 0)
			{
				return value.Substring(start, end - start);
			}
			else
			{
				return value.Substring(start);
			}
		}
		public static string GetBlockValue(this string value, string startStr, string endStr)
		{
			var index = value.IndexOf(startStr);
			if (index < 0)
			{
				return "";
			}
			var start = index + startStr.Length;

			var end = value.IndexOf(endStr, start);

			if (end >= 0)
			{
				return value.Substring(start, end - start);
			}
			else
			{
				return value.Substring(start);
			}
		}
		public static bool SplitTowString(this string str, string splitStart, out string start, out string end)
		{

			if (str.Contains(splitStart))
			{
				var startIndex = str.IndexOf(splitStart);
				start = str.Substring(0, startIndex);
				end = str.Substring(startIndex + splitStart.Length);
				return true;
			}
			else
			{
				start = str;
				end = "";
				return false;
			}
		}
		public static string ToSizeString(this string array)
		{
			return array.Length.ToSizeString();
		}
		public static string ToSizeString(this IList array)
		{
			return array.Count.ToSizeString();
		}
		public static int RemoveNull<T>(this List<T> array)
		{
			return array.RemoveAll(obj => obj.IsNull());
		}
		public static int RemoveSpace(this List<string> array)
		{
			return array.RemoveAll(obj => string.IsNullOrWhiteSpace(obj));
		}
		public static string ToSizeString(this float byteLength)
		{
			return ToSizeString((long)byteLength);
		}
		public static string ToColorText(this string text, string color)
		{
			if (!color.StartsWith("#"))
			{
				color = "#" + color;
			}
			return "<color=" + color + ">" + text + "</color>";
		}
		public static string ToColorText(this string text,Color color)
		{
			return ToColorText(text, ColorUtility.ToHtmlStringRGB(color));
		}
		public static string ToSizeString(this int byteLength)
		{
			return ToSizeString((long)byteLength);
		}

		public static string ToSizeString(this long longLength)
		{
			string[] Suffix = { "Byte", "KB", "MB", "GB", "TB" };
			int i = 0;
			double dblSByte = longLength;
			if (longLength > 1000)
				for (i = 0; (longLength / 1000) > 0; i++, longLength /= 1000)
					dblSByte = longLength / 1000.0;
			if (i == 0)
			{
				return dblSByte.ToString("f0") + "" + Suffix[i];
			}
			else
			{
				return dblSByte.ToString("f1") + "" + Suffix[i];
			}
		}
		public static string ToOneString<T>(this ICollection<T> array, string splitChar = "\n", Func<T, string> toString = null)
		{
			if (array == null)
			{
				return "";
			}
			return QTool.BuildString((writer) =>
			{
				int i = 0;
				if (toString == null)
				{
					foreach (var item in array)
					{
						writer.Write(item);
						if (i < array.Count - 1)
						{
							writer.Write(splitChar);
						}
						i++;
					}
				}
				else
				{
					foreach (var item in array)
					{
						writer.Write(toString(item));
						if (i < array.Count - 1)
						{
							writer.Write(splitChar);
						}
						i++;
					}
				}

			});
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
			foreach (var key in buffer)
			{
				dic.Remove(key);
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
		public static Dictionary<KeyT, ObjT> ToDictionary<KeyT, ObjT>(this IList<ObjT> list, Dictionary<KeyT, ObjT> dic = null) where ObjT : IKey<KeyT>
		{
			if (dic == null)
			{
				dic = new Dictionary<KeyT, ObjT>();
			}
			else
			{
				dic.Clear();
			}
			foreach (var item in list)
			{
				if (item == null) continue;
				dic.Add(item.Key, item);
			}
			return dic;
		}
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
		public static bool ContainsKey<T, KeyType>(this IList<T> array, KeyType key, Func<T, KeyType> keyGetter)
		{
			if (key == null)
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
		public static T Get<T, KeyType>(this IList<T> array, KeyType key) where T : IKey<KeyType>
		{
			return array.Get(key, (item) => item.Key);
		}
		public static T Get<T, KeyType>(this IList<T> array, KeyType key, Func<T, KeyType> keyGetter)
		{
			if (key == null)
			{
				return default;
			}
			for (int i = 0; i < array.Count; i++)
			{
				var value = array[i];
				if (value.IsNull()) continue;
				if (key.Equals(keyGetter(value)))
				{
					return value;
				}
			}
			return default;
		}
		public static T Get<T, KeyType>(this IDictionary<KeyType, T> array, KeyType key, Func<T, KeyType> keyGetter)
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
		public static List<T> GetList<T, KeyType>(this IList<T> array, KeyType key, List<T> tempList = null) where T : IKey<KeyType>
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
		
	}
}
