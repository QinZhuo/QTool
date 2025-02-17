using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{
	public static class QData
	{
		public static string ToQData<T>(this T obj)
		{
			var type = typeof(T);
			return ToQDataType(obj, type);
		}
		public static T ParseQData<T>(this string qdataStr, T target = default)
		{
			return (T)ParseQDataType(qdataStr, typeof(T), target);
		}
		public static string ToQDataType(this object obj, Type type) {
			using (var writer = new StringWriter()) {
				WriteType(writer, obj, type);
				return writer.ToString();
			}
		}
		public static object ParseQDataType(this string qdataStr, Type type, object target = null) {
			if (string.IsNullOrEmpty(qdataStr)) {
				return type.IsValueType ? QReflection.CreateInstance(type, target) : null;
			}
			try {
				using (var reader = new StringReader(qdataStr)) {
					return ReadType(reader, type, target);
				}
			}
			catch (Exception e) {
				Debug.LogError("解析类型【" + type + "】出错：" + qdataStr?.Substring(0, Mathf.Min(1000, qdataStr.Length)));
				Debug.LogException(e);
				return type.IsValueType ? QReflection.CreateInstance(type, target) : null;
			}

		}

		public static void WriteQData<T>(this StringWriter writer, T obj) {
			WriteType(writer, obj, typeof(T));
		}
		static void WriteObject(this StringWriter writer, object obj, QSerializeType typeInfo) {

			if (obj == null) { writer.Write("null"); return; }
			writer.Write('{');
			for (int i = 0; i < typeInfo.Members.Count; i++) {
				var memberInfo = typeInfo.Members[i];
				var member = memberInfo.Get(obj);
				WriteCheckString(writer, memberInfo.Key);
				writer.Write(':');
				WriteType(writer, member, memberInfo.Type);
				if (i < typeInfo.Members.Count - 1) {
					writer.Write(',');
				}
			}
			writer.Write('}');
		}
		public static void WriteType(this StringWriter writer, object obj, Type type) {
			var typeCode = Type.GetTypeCode(type);
			switch (typeCode) {
				case TypeCode.Object: {
					var typeInfo = QSerializeType.Get(type);
					if (typeInfo.CustomTypeInfo != null) {
						WriteType(writer, typeInfo.CustomTypeInfo.ChangeType(obj), typeInfo.CustomTypeInfo.TargetType);
						return;
					}
					switch (typeInfo.ObjType) {
						case QObjectType.DynamicObject: {
							if (obj == null) {
								writer.Write("null");
							}
							else {
								writer.Write('{');
								var runtimeType = obj.GetType();
								var runtimeTypeInfo = QSerializeType.Get(runtimeType);
								switch (runtimeTypeInfo.ObjType) {
									case QObjectType.DynamicObject: {
										WriteCheckString(writer, runtimeType.FullName);
										writer.Write(':');
										WriteObject(writer, obj, runtimeTypeInfo);
									}
									break;
									case QObjectType.CantSerialize:
										break;
									default: {
										WriteCheckString(writer, runtimeType.FullName);
										writer.Write(':');
										WriteType(writer, obj, runtimeType);
									}
									break;
								}
								writer.Write('}');
							}


						}
						break;
						case QObjectType.UnityObject: {
							if (obj == null) {
								writer.Write("null");
							}
							else {
								writer.Write('{');
								writer.WriteCheckString("QId");
								writer.Write(':');
								writer.WriteCheckString(QObjectTool.GetPath(obj as UnityEngine.Object));
								writer.Write('}');
							}

						}
						break;
						case QObjectType.Object: {
							WriteObject(writer, obj, typeInfo);
							break;
						}
						case QObjectType.List:
						case QObjectType.Array: {
							var list = obj as IList;
							if (list == null)
								break;
							writer.Write('[');
							for (int i = 0; i < list.Count; i++) {
								WriteType(writer, list[i], typeInfo.ElementType);
								if (i < list.Count - 1) {
									writer.Write(',');
									writer.Write('\n');
								}
							}
							writer.Write(']');
							break;
						}
						case QObjectType.Dictionary: {
							var dic = obj as IDictionary;
							if (dic == null)
								break;
							writer.Write('{');
							var i = 0;
							foreach (DictionaryEntry kv in dic) {

								if (typeInfo.KeyType == typeof(string)) {
									WriteType(writer, kv.Key, typeInfo.KeyType);
								}
								else {
									WriteCheckString(writer, ToQDataType(kv.Key, typeInfo.KeyType));
								}
								writer.Write(":");
								WriteType(writer, kv.Value, typeInfo.ElementType);
								if (i++ < dic.Count - 1) {
									writer.Write(',');
									writer.Write('\n');
								}
							}
							writer.Write('}');
						}
						break;
						case QObjectType.TimeSpan: {
							writer.Write(((TimeSpan)obj).Ticks);
						}
						break;
						default:
							break;
					}
					break;
				}
				case TypeCode.DateTime:
					WriteCheckString(writer, ((DateTime)obj).ToQTimeString());
					break;
				case TypeCode.String:
					WriteCheckString(writer, obj?.ToString());
					break;
				case TypeCode.Boolean:
					writer.Write(obj.ToString().ToLower());
					break;
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					if (type.IsEnum) {
						WriteCheckString(writer, obj.ToString());
					}
					else {
						writer.Write(obj.ToString());
					}
					break;
				default:
					writer.Write(obj.ToString());
					break;
			}
		}
		public static T ReadQData<T>(this StringReader reader, T target = default) {
			return (T)ReadType(reader, typeof(T), target);
		}
		static object ReadObject(this StringReader reader, QSerializeType typeInfo, object target = null) {
			if (reader.NextIsSplit('{') || typeInfo.Type.ContainsGenericParameters) {
				target = QReflection.CreateInstance(typeInfo.Type, target);
				if (reader.NextIsSplit('}')) {
					return target;
				}
				while (!reader.IsEnd()) {
					var name = reader.ReadCheckString();
					var memeberInfo = typeInfo.GetMemberInfo(name);

					object result = null;

					if (!(reader.NextIsSplit(':'))) {
						throw new Exception(name + " 后缺少分隔符 : 或 =\n" + reader.ReadLine());
					}
					if (memeberInfo != null) {
						try {
							result = reader.ReadType(memeberInfo.Type, memeberInfo.Get(target));
							memeberInfo.Set(target, result);

						}
						catch (Exception e) {
							throw new Exception("读取成员【" + name + ":" + typeInfo.Type.Name + "." + memeberInfo.Key + "】出错" + memeberInfo.Type + ":" + result + ":" + memeberInfo.Get(target) + ":" + memeberInfo.Set + "\n 剩余信息" + reader.ReadToEnd(), e);
						}
					}
					else {
						var info = reader.ReadObjectString();
						Debug.LogWarning("不存在成员" + typeInfo.Type + "." + name + ":" + info);
					}

					if (!(reader.NextIsSplit(','))) {
						if (reader.NextIsSplit('}')) {
							break;
						}
					}
				}
			}
			else {
				var objData = reader.ReadObjectString();
				if (objData == "null") {
					target = null;
				}
				else if (objData.IsNull()) {
					target = null;
				}
				else {
					throw new Exception($"对象数据出错[{objData}]:{typeInfo.Type}");
				}
			}
			return target;
		}
		public static object ReadType(this StringReader reader, Type type, object target = null) {
			var typeCode = Type.GetTypeCode(type);
			switch (typeCode) {
				case TypeCode.Object: {
					var typeInfo = QSerializeType.Get(type);
					if (typeInfo.CustomTypeInfo != null && typeInfo.CustomTypeInfo.TargetType != null) {
						return typeInfo.CustomTypeInfo.ChangeType(ReadType(reader, typeInfo.CustomTypeInfo.TargetType, null));
					}
					switch (typeInfo.ObjType) {
						case QObjectType.DynamicObject: {
							if (reader.NextIsSplit('{')) {
								if (reader.NextIsSplit('}')) {
									break;
								}
								var str = reader.ReadCheckString();
								var runtimeType = QReflection.ParseType(str, type);
								if (reader.NextIsSplit(':')) {
									if (runtimeType == null) {
										var data = reader.ReadObjectString();
										Debug.LogException(new Exception($"runtimeType 丢失 [{str}][{data}]"));
									}
									else if (type == runtimeType) {
										target = ReadObject(reader, typeInfo, target);
									}
									else {
										target = ReadType(reader, runtimeType, target);
									}
								}
								while (!reader.IsEnd() && !reader.NextIsSplit('}')) {
									reader.Read();
								}
							}
							else {
								if (reader.ReadObjectString() == "null") {
									target = null;
								}
								else {
									target = null;
								}
							}

						}
						break;
						case QObjectType.UnityObject: {
							reader.NextIsSplit('{');
							var str = reader.ReadCheckString();
							if (str == "null") {
								target = null;

							}
							else {
								if (reader.NextIs(':')) {
									str = reader.ReadCheckString();
								}
								target = QObjectTool.GetObject(str, type);
							}
							reader.NextIsSplit('}');
							break;
						}
						case QObjectType.Object: {
							target = ReadObject(reader, typeInfo, target);
						}
						break;
					
						case QObjectType.List: {
							var list = QReflection.CreateInstance(type, target) as IList;
							if (list.Count != 0) {
								list.Clear();
							}
							if (reader.NextIsSplit('[')) {
								if (!reader.NextIs(']')) {
									while (!reader.IsEnd()) {
										list.Add(reader.ReadType(typeInfo.ElementType));
										if (!reader.NextIsSplit(',')) {
											if (reader.NextIsSplit(']')) {
												break;
											}
											else {
												throw new Exception("数组格式出错 缺少,或]而不是|" + reader.ReadObjectString() + "|");
											}
										}
									}
								}
							}
							else {
								var str = reader.ReadCheckString();
								if (str == "null") {
									list.Clear();
								}
							}
							target = list;
						}
						break;
						case QObjectType.Dictionary: {
							var dic = QReflection.CreateInstance(type, target) as IDictionary;
							if (dic.Count != 0) {
								dic.Clear();
							}
							if (reader.NextIsSplit('{')) {
								if (!reader.NextIsSplit('}')) {
									while (!reader.IsEnd()) {
										var key = typeInfo.KeyType == typeof(string) ? reader.ReadCheckString() : reader.ReadCheckString().ParseQDataType(typeInfo.KeyType);
										if (!reader.NextIsSplit(':')) {
											Debug.LogError(reader.ReadToEnd());
											throw new Exception("格式出错 缺少:");
										}
										dic.Add(key, reader.ReadType(typeInfo.ElementType));
										if (!reader.NextIsSplit(',')) {
											if (reader.NextIsSplit('}')) {
												break;
											}
											else {
												throw new Exception("格式出错 缺少," + " [" + reader.ReadToEnd() + "]");
											}
										}
									}
								}
							}
							target = dic;
						}
						break;
						case QObjectType.Array: {
							var tempList = QObjectPool<List<object>>.Get();
							if (reader.NextIsSplit('[')) {
								if (!reader.NextIsSplit(']')) {
									while (!reader.IsEnd()) {
										tempList.Add(reader.ReadType(typeInfo.ElementType));
										if (!reader.NextIsSplit(',')) {
											if (reader.NextIsSplit(']')) {
												break;
											}
											else {
												throw new Exception("格式出错 或,");
											}
										}
									}
								}
							}
							var array = QReflection.CreateInstance(type, null, tempList.Count) as Array;
							for (int i = 0; i < tempList.Count; i++) {
								array.SetValue(tempList[i], i);
							}
							tempList.Clear();
							QObjectPool<List<object>>.Release(tempList);
							target = array;
						}
						break;
						case QObjectType.TimeSpan: {
							return TimeSpan.FromTicks(reader.ReadQData<long>());
						}
						default:
							Debug.LogError("不支持类型[" + type + "]");
							return null;
					}
					if (typeInfo.HasCallback && target is IQSerializeCallback callback) {
						callback.OnLoad();
					}
					return target;
				}

				case TypeCode.Boolean:
					if (!bool.TryParse(ReadObjectString(reader), out var boolValue)) {
						boolValue = false;
					}
					return boolValue;
				case TypeCode.Char:
					return char.Parse(ReadObjectString(reader));
				case TypeCode.DateTime:
					return DateTime.Parse(ReadCheckString(reader));
				case TypeCode.DBNull:
					return null;
				case TypeCode.Decimal:
					return decimal.Parse(ReadObjectString(reader));
				case TypeCode.Double:
					return double.Parse(ReadObjectString(reader));
				case TypeCode.Empty:
					return null;
				case TypeCode.Single:
					return float.Parse(ReadObjectString(reader));
				case TypeCode.String:
					return ReadCheckString(reader);
				case TypeCode.Int16:
					if (type.IsEnum) {
						return type.ParseEnum(ReadCheckString(reader));
					}
					return short.Parse(ReadObjectString(reader));
				case TypeCode.Int32:
					if (type.IsEnum) {
						return type.ParseEnum(ReadCheckString(reader));
					}
					return int.Parse(ReadObjectString(reader));
				case TypeCode.Int64:
					if (type.IsEnum) {
						return type.ParseEnum(ReadCheckString(reader));
					}
					return long.Parse(ReadObjectString(reader));
				case TypeCode.SByte:
					if (type.IsEnum) {
						return type.ParseEnum(ReadCheckString(reader));
					}
					return sbyte.Parse(ReadObjectString(reader));
				case TypeCode.Byte:
					if (type.IsEnum) {
						return type.ParseEnum(ReadCheckString(reader));
					}
					return byte.Parse(ReadObjectString(reader));
				case TypeCode.UInt16:
					if (type.IsEnum) {
						return type.ParseEnum(ReadCheckString(reader));
					}
					return ushort.Parse(ReadObjectString(reader));
				case TypeCode.UInt32:
					if (type.IsEnum) {
						return type.ParseEnum(ReadCheckString(reader));
					}
					return uint.Parse(ReadObjectString(reader));
				case TypeCode.UInt64:
					if (type.IsEnum) {
						return type.ParseEnum(ReadCheckString(reader));
					}
					return ulong.Parse(ReadObjectString(reader));
				default:
					Debug.LogError("不支持类型[" + typeCode + "]");
					return null;
			}
		}


		const string BlockStart = "<{[\"";
		const string BlockEnd = ">}]\",;=:";
		static Stack<char> BlockStack = new Stack<char>();
		public static string ReadObjectString(this StringReader reader, string ignore = "") {
			return QTool.BuildString((writer) => {
				int index = -1;
				var hasChar = false;
				BlockStack.Clear();
				while (!reader.IsEnd()) {
					var c = (char)reader.Peek();
					if (BlockStack.Count == 0) {
						if (ignore.IndexOf(c) < 0 && BlockEnd.IndexOf(c) >= 0 && hasChar) {
							break;
						}
						else if ((index = BlockStart.IndexOf(c)) >= 0) {
							BlockStack.Push(BlockEnd[index]);
						}
					}
					else {
						if (BlockStack.Peek() == c) {
							BlockStack.Pop();
						}
						else if ((index = BlockStart.IndexOf(c)) >= 0) {
							BlockStack.Push(BlockEnd[index]);
						}
					}
					reader.Read();
					writer.Write(c);
					hasChar = true;
				}
			});
		}

		public static void WriteCheckString(this StringWriter writer, string value) {
			if (value == null) {
				writer.Write("\"\"");
				return;
			}
			using (StringReader reader = new StringReader(value)) {
				writer.Write("\"");
				while (!reader.IsEnd()) {

					var c = (char)reader.Read();
					switch (c) {
						case '"':
							writer.Write("\\\"");
							break;
						case '\\':
							writer.Write("\\\\");
							break;
						case '\b':
							writer.Write("\\b");
							break;
						case '\f':
							writer.Write("\\f");
							break;
						case '\n':
							writer.Write("\\n");
							break;
						case '\r':
							writer.Write("\\r");
							break;
						case '\t':
							writer.Write("\\t");
							break;
						default:
							writer.Write(c);
							break;
					}
				}
				writer.Write("\"");
			}
		}
		public static string ReadCheckString(this StringReader reader) {
			if (reader.NextIs('\"')) {
				using (var writer = new StringWriter()) {
					while (!reader.IsEnd() && !reader.NextIs('\"')) {
						if (reader.NextIs('\\')) {
							var c = (char)reader.Read();
							switch (c) {
								case '"':
								case '\\':
								case '/':
									writer.Write(c);
									break;
								case 'b':
									writer.Write('\b');
									break;
								case 'f':
									writer.Write('\f');
									break;
								case 'n':
									writer.Write('\n');
									break;
								case 'r':
									writer.Write('\r');
									break;
								case 't':
									writer.Write('\t');
									break;
							}
						}
						else {
							writer.Write((char)reader.Read());
						}
					}
					return writer.ToString();
				}
			}
			else {
				return reader.ReadObjectString();
			}
		}

		
	}
	public abstract class QSerializeObject<T>: ISerializationCallbackReceiver, IQSerializeCallback where T: QSerializeObject<T> {

		[QIgnore, HideInInspector, SerializeField]
		internal string SerializeString;
		public virtual void OnBeforeSerialize()
		{
			//Profiler.BeginSample("QData Save ");
			SerializeString = (this as T).ToQData();
			//Profiler.EndSample();
			//Profiler.BeginSample("Json Save");
			//var data = this.ToJson();
			//Profiler.EndSample();
			//GUIUtility.systemCopyBuffer = SerializeString + "\n" + this.ToJson();
		}
		public virtual void OnAfterDeserialize()
		{
			SerializeString.ParseQData(this as T);
			OnLoad();
		}

		public virtual void OnLoad()
		{
		}
	}


	public enum QObjectType
	{
		UnityObject,
		DynamicObject,
		Object,
		List,
		Array,
		//FixedString,
		Dictionary,
		TimeSpan,
		CantSerialize,
	}
	public interface IQSerializeCallback
	{
		void OnLoad();
	}
	public abstract class QSerializeType<T> : QTypeInfo<T> where T : QSerializeType<T>, new() {
		public QObjectType ObjType { get; protected set; } = QObjectType.Object;
		public bool IsIQSerialize { private set; get; }
		
		public bool HasCallback { private set; get; }
		protected override void Init(Type type) {
			base.Init(type);
			if (Code == TypeCode.Object) {
				ObjType = QObjectType.Object;
				if (typeof(Task).IsAssignableFrom(type)) {
					ObjType = QObjectType.CantSerialize;
					return;
				}
				HasCallback = typeof(IQSerializeCallback).IsAssignableFrom(type);
				if (typeof(UnityEngine.Object).IsAssignableFrom(type)) {
					ObjType = QObjectType.UnityObject;
				}
				else if (type == typeof(object) || type.IsAbstract || type.IsInterface) {
					ObjType = QObjectType.DynamicObject;
				}
				else if (IsArray) {
					ObjType = QObjectType.Array;
				}
				else if (IsList) {
					ObjType = QObjectType.List;
				}
				else if (IsDictionary) {
					ObjType = QObjectType.Dictionary;
				}
				else if (type == typeof(TimeSpan)) {
					ObjType = QObjectType.TimeSpan;
				}
				else {
					if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
						Debug.LogError("不支持序列化【" + type + "】Nullable类型");
					}
				}
			}
			Members.Sort(
				(a, b) => {
					if (a.MemeberInfo.DeclaringType == b.MemeberInfo.DeclaringType) {
						return 0;
					}
					else if (a.MemeberInfo.DeclaringType?.BaseType.Is(b.MemeberInfo.DeclaringType) == true) {
						return 1;
					}
					else {
						return -1;
					}
				}
			);
			Members.RemoveAll((member) => {
				return (member.Type.IsValueType && member.Type == type) || ((member.MemeberInfo.GetCustomAttribute<HideInInspector>() != null || member.MemeberInfo.GetCustomAttribute<QIgnoreAttribute>() != null || !member.IsPublic) && member.MemeberInfo.GetCustomAttribute<QNameAttribute>() == null) || member.Key == "Item" || member.Get == null || (IgnoreReadOnlyMember && member.Set == null) || (member.Type.IsArray && member.Type.GetArrayRank() > 1);
			});
		}
		public virtual bool IgnoreReadOnlyMember => true;
	}
	public class QSerializeType : QSerializeType<QSerializeType>
	{

	}
	public class QSerializeHasReadOnlyType : QSerializeType<QSerializeHasReadOnlyType>
	{
		public override bool IgnoreReadOnlyMember => false;
	}
}

