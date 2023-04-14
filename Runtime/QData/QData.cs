using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{
	public static class QData
	{
		public static string ToQData<T>(this T obj, bool hasName = true)
		{
			var type = typeof(T);
			return ToQDataType(obj, type, hasName);
		}
		public static T ParseQData<T>(this string qdataStr, T target = default, bool hasName = true)
		{
			if (typeof(T) == typeof(string))
			{
				return (T)(object)qdataStr;
			}
			return (T)ParseQDataType(qdataStr, typeof(T), hasName, target);
		}
		public static T QDataGet<T>(this string qdataStr, int index)
		{
			return qdataStr.ParseQData<string[]>()[index].ParseQData<T>();
		}
		public static T QDataGet<T>(this string qdataStr, string key)
		{
			var dic = qdataStr.ParseQData<Dictionary<string, string>>();
			if(dic.ContainsKey(key))return dic[key].ParseQData<T>();
			return default;
		}
		public static string ToQDataType(this object obj, Type type, bool hasName = true)
		{
			using (var writer=new StringWriter())
			{
				WriteType(writer, obj, type, hasName);
				return writer.ToString();
			}
		}
		public static object ParseQDataType(this string qdataStr, Type type, bool hasName = true, object target = null)
		{
			if (string.IsNullOrEmpty(qdataStr))
			{
				return type.IsValueType ? QReflection.CreateInstance(type, target) : null;
			}
			try 
			{
				using (var reader = new StringReader(qdataStr))
				{
					return ReadType(reader, type, hasName, target);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("解析类型【" + type + "】出错：" + "\n" + e+"\n"+ qdataStr?.Substring(0, Mathf.Min(1000,qdataStr.Length)));
				return type.IsValueType ? QReflection.CreateInstance(type, target) : null; 
			}

		} 

		public static void WriteQData<T>(this StringWriter writer, T obj, bool hasName = true)
		{
			WriteType(writer, obj, typeof(T), hasName);
		}
		static void WriteObject(this StringWriter writer, object obj, QSerializeType typeInfo, bool hasName = true)
		{

			if (obj == null) { writer.Write("null"); return; }
			writer.Write('{');
			if (typeInfo.IsIQData)
			{
				(obj as IQData).ToQData(writer);
			}
			else
			{
				if (hasName)
				{
					for (int i = 0; i < typeInfo.Members.Count; i++)
					{
						var memberInfo = typeInfo.Members[i];
						var member = memberInfo.Get(obj);
						WriteCheckString(writer, memberInfo.Key);
						writer.Write(':');
						WriteType(writer, member, memberInfo.Type, hasName);
						if (i < typeInfo.Members.Count - 1)
						{
							writer.Write(',');
						}
					}
				}
				else
				{
					for (int i = 0; i < typeInfo.Members.Count; i++)
					{
						var memberInfo = typeInfo.Members[i];
						var member = memberInfo.Get(obj);
						WriteType(writer, member, memberInfo.Type, hasName);
						if (i < typeInfo.Members.Count - 1)
						{
							writer.Write(',');
						}
					}
				}

			}
			writer.Write('}');


		}
		public static void WriteType(this StringWriter writer, object obj, Type type, bool hasName=true)
		{
			var typeCode = Type.GetTypeCode(type);
			switch (typeCode)
			{
				case TypeCode.Object:
					{
						var typeInfo = QSerializeType.Get(type);
						switch (typeInfo.objType)
						{
							case QObjectType.DynamicObject:
								{
									if(obj == null)
									{
										writer.Write("null");
									}
									else
									{
										writer.Write('{');
										var runtimeType = obj.GetType();
										var runtimeTypeInfo = QSerializeType.Get(runtimeType);
										switch (runtimeTypeInfo.objType)
										{
											case QObjectType.DynamicObject:
												{
													WriteCheckString(writer, runtimeType.QTypeName());
													writer.Write(':');
													WriteObject(writer, obj, runtimeTypeInfo, hasName);
												}
												break;
											case QObjectType.CantSerialize:
												break;
											default:
												{
													WriteCheckString(writer, runtimeType.QTypeName());
													writer.Write(':');
													WriteType(writer, obj, runtimeType, hasName);
												}
												break;
										}
										writer.Write('}');
									}
									
									
								}
								break;
							case QObjectType.UnityObject:
								{
									if (obj == null)
									{
										writer.Write("null");
									}
									else
									{
										writer.Write('{');
										writer.WriteCheckString("QId");
										writer.Write(':');
										writer.WriteCheckString(QIdTool.GetQId(obj as UnityEngine.Object));
										writer.Write('}');
									}
								
								}
								break;
							case QObjectType.Object:
								{
									WriteObject(writer, obj, typeInfo, hasName);
									break;
								}
							case QObjectType.List:
							case QObjectType.Array:
								{
									var list = obj as IList;
									if (list == null) break;
									writer.Write('[');
									for (int i = 0; i < list.Count; i++)
									{
										WriteType(writer, list[i], typeInfo.ElementType, hasName);
										if (i < list.Count - 1)
										{
											writer.Write(',');
											if (hasName)
											{
												writer.Write('\n');
											}
										}
									}
									writer.Write(']');
									break;
								}
							case QObjectType.Dictionary:
								{
									var dic = obj as IDictionary;
									if (dic == null) break;
									writer.Write('{');
									var i = 0;
									foreach (DictionaryEntry kv in dic)
									{

										if (typeInfo.KeyType == typeof(string))
										{
											WriteType(writer, kv.Key, typeInfo.KeyType, hasName);
										}
										else
										{
											WriteCheckString(writer, ToQDataType(kv.Key, typeInfo.KeyType, hasName));
										}
										writer.Write(":");
										WriteType(writer, kv.Value, typeInfo.ElementType, hasName);
										if (i++ < dic.Count - 1)
										{ 
											writer.Write(',');
											if (hasName)
											{
												writer.Write('\n');
											}
										}
									}
									writer.Write('}');
								}
								break;
							case QObjectType.TimeSpan:
								{
									writer.Write(((TimeSpan)obj).Ticks);
								}break;
							default:
								break;
						}
						break;
					}
				case TypeCode.DateTime:
					WriteCheckString(writer,((DateTime)obj).ToQTimeString());
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
					if (type.IsEnum)
					{
						WriteCheckString(writer, obj.ToString());
					}
					else
					{
						writer.Write(obj.ToString());
					}
					break;
				default:
					writer.Write(obj.ToString());
					break;
			}
		}
		public static T ReadQData<T>(this StringReader reader, T target = default,bool hasName = true)
		{
			return (T)ReadType(reader, typeof(T), hasName, target);
		}
		static object ReadObject(this StringReader reader, QSerializeType typeInfo, bool hasName = true, object target = null)
		{
			if (reader.NextIsSplit('{')||typeInfo.Type.ContainsGenericParameters)
			{
				target = QReflection.CreateInstance(typeInfo.Type, target);
				if (reader.NextIsSplit('}'))
				{
					return target;
				}
				if (typeInfo.IsIQData)
				{
					(target as IQData).ParseQData(reader); reader.NextIsSplit('}');
				}
				else
				{
					if (hasName)
					{
						while (!reader.IsEnd())
						{
							var name = reader.ReadCheckString();
							var memeberInfo = typeInfo.GetMemberInfo(name);

							object result = null;

							if (!(reader.NextIsSplit(':') ))
							{
								throw new Exception(name + " 后缺少分隔符 : 或 =\n" + reader.ReadLine());
							}
							if (memeberInfo != null)
							{
								try
								{
									result = reader.ReadType(memeberInfo.Type, hasName, memeberInfo.Get(target));
									memeberInfo.Set(target, result);

								}
								catch (Exception e)
								{
									throw new Exception("读取成员【" + name + ":" + typeInfo.Type.Name + "." + memeberInfo.Key + "】出错" + memeberInfo.Type + ":" + result + ":" + memeberInfo.Get(target) + ":" + memeberInfo.Set + "\n" + e + "\n 剩余信息" + reader.ReadToEnd());
								}
							}
							else
							{
								var info = reader.ReadCheckString();
								//Debug.LogWarning("不存在成员" + typeInfo.Key + "." + name + ":" + info);
							}

							if (!( reader.NextIsSplit(',')))
							{
								if (reader.NextIsSplit('}'))
								{
									break;
								}
							}
						}
					}
					else
					{
						bool isOver = false;
						foreach (var memeberInfo in typeInfo.Members)
						{
							try
							{
								memeberInfo.Set(target, reader.ReadType(memeberInfo.Type, hasName, memeberInfo.Get(target)));
								if (!(reader.NextIsSplit(','))) 
								{
									if (reader.NextIsSplit('}'))
									{
										isOver = true;
										break;
									}
									else
									{
										throw new Exception("读取对象出错缺少,或}而不是" + reader.ReadToEnd());
									}
								}
							} 
							catch (Exception e)
							{
								throw new Exception("读取"+typeInfo.Type+"."+memeberInfo.QName+"出错",e);
							}
						}
						if (!isOver)
						{
							while (!reader.NextIsSplit('}'))
							{
								reader.NextIsSplit(',');
								reader.ReadCheckString();
							}
						}
					}

				}
				
			}
			else
			{
				var objData = reader.ReadObjectString();
				if (objData == "null")
				{
					target = null; 
				}
				else if(objData.IsNull())
				{
					target = null;
				}
				else
				{
					throw new Exception("对象数据出错|" + objData + "|");
				}
			}
			return target;
		}
		public static object ReadType(this StringReader reader, Type type, bool hasName = true, object target = null)
		{
			var typeCode = Type.GetTypeCode(type);
			switch (typeCode)
			{
				case TypeCode.Object:
					{
						var typeInfo = QSerializeType.Get(type);
						switch (typeInfo.objType)
						{
							case QObjectType.DynamicObject:
								{
									if(reader.NextIsSplit('{'))
									{
										if (reader.NextIsSplit('}'))
										{
											return null;
										}
										var str = reader.ReadCheckString();
										var runtimeType = QReflection.ParseType(str);
										if (reader.NextIsSplit(':') )
										{
											if (type == runtimeType)
											{
												target = ReadObject(reader, typeInfo, hasName, target);
											}
											else
											{
												target = ReadType(reader, runtimeType, hasName, target);
											}
										}
										while (!reader.IsEnd() && !reader.NextIsSplit('}'))
										{
											reader.Read();
										}
									}
									else
									{
										if (reader.ReadObjectString() == "null")
										{
											target = null;
										}
										else
										{
											target = null;
										}
									}

								}break;
							case QObjectType.UnityObject:
								{
									reader.NextIsSplit('{');
									var str = reader.ReadCheckString();
									if (str == "null")
									{
										target = null;
										
									}
									else
									{
										if (reader.NextIs(':'))
										{
											str = reader.ReadCheckString();
										}
										target = QIdTool.GetObject(str, type);
									}
									reader.NextIsSplit('}');
									break;
								}
							case QObjectType.Object:
								{
									target=ReadObject(reader, typeInfo, hasName, target);
								}
								break;

							case QObjectType.List:
								{
									var list = QReflection.CreateInstance(type, target) as IList;

									if (reader.NextIsSplit('['))
									{
										
										var count = 0;
										for (var i = 0; !reader.IsEnd() && !reader.NextIsSplit(']'); i++)
										{
											if (i < list.Count)
											{
												list[i] = reader.ReadType(typeInfo.ElementType, hasName, list[i]);
											}
											else
											{
												list.Add(reader.ReadType(typeInfo.ElementType, hasName));
											}
											count++;
											if (! reader.NextIsSplit(',')){
												if (reader.NextIsSplit(']'))
												{
													break; 
												}
												else
												{
													throw new Exception("数组格式出错 缺少,或]而不是|"+ reader.ReadToEnd()+"|"); ;
												}
											}
										}
										for (int i = count; i < list.Count; i++)
										{
											list.RemoveAt(i);
										}
										
									}
									else
									{
										var str = reader.ReadCheckString();
										if (str == "null")
										{
											list.Clear();
										}
									}
									target= list;
								}break;
							case QObjectType.Dictionary:
								{
									var dic = QReflection.CreateInstance(type, target) as IDictionary;
									dic.Clear();
									if (reader.NextIsSplit('[')||reader.NextIsSplit('{'))
									{
										while (!reader.IsEnd() && !( reader.NextIsSplit(']')|| reader.NextIsSplit('}')))
										{
											var obj = QReflection.CreateInstance(typeInfo.KeyValueType);

											var key = typeInfo.KeyType==typeof(string)? reader.ReadCheckString(): reader.ReadCheckString().ParseQDataType(typeInfo.KeyType, hasName);
											if (!reader.NextIsSplit(':'))
											{
												Debug.LogError(reader.ReadToEnd());
												throw new Exception("格式出错 缺少:"); ;
											}
											var value = reader.ReadType(typeInfo.ElementType, hasName);
											if (dic.Contains(key))
											{
												dic[key]=value;
											}
											else
											{
												dic.Add(key, value);
											}
											if (!(reader.NextIsSplit(',')))
											{
												if (reader.NextIsSplit(']')||reader.NextIsSplit('}'))
												{
													break;
												}
												else
												{
													Debug.LogError(reader.ReadToEnd());
													throw new Exception("格式出错 缺少,"+" ["+dic.ToQData()+"]");
												}
											}
										}
									}
									target= dic;
								}break;
							case QObjectType.Array:
								{
									List<object> list = new List<object>();
									if (reader.NextIsSplit('['))
									{
										for (int i = 0; !reader.IsEnd() && !reader.NextIsSplit(']'); i++)
										{
											list.Add(reader.ReadType(typeInfo.ElementType, hasName));
											if (!( reader.NextIsSplit(',')))
											{
												if (reader.NextIsSplit(']'))
												{
													break;
												}
												else
												{
													throw new Exception("格式出错 或,"); ;
												}
											}
										}
									}
									var array = QReflection.CreateInstance(type, null,list.Count) as Array;
									for (int i = 0; i < list.Count; i++)
									{
										array.SetValue(list[i], i);
									}
									target= array;
								}break;
							case QObjectType.TimeSpan:
								{
									return TimeSpan.FromTicks(reader.ReadQData<long>());
								}
							default:
								Debug.LogError("不支持类型[" + type + "]");
								return null;
						}
						if (typeInfo.HasCallback && target is IQSerializeCallback callback)
						{
							callback.OnDeserializeOver();
						}
						return target;
					}

				case TypeCode.Boolean:
					if(!bool.TryParse(ReadObjectString(reader),out var boolValue))
					{
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
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return short.Parse(ReadObjectString(reader));
				case TypeCode.Int32:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return int.Parse(ReadObjectString(reader));
				case TypeCode.Int64:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return long.Parse(ReadObjectString(reader));
				case TypeCode.SByte:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return sbyte.Parse(ReadObjectString(reader));
				case TypeCode.Byte:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return byte.Parse(ReadObjectString(reader));
				case TypeCode.UInt16:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return ushort.Parse(ReadObjectString(reader));
				case TypeCode.UInt32:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return uint.Parse(ReadObjectString(reader));
				case TypeCode.UInt64:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
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
		public static string ReadObjectString(this StringReader reader,string ignore="")
		{
			return QTool.BuildString((writer) =>
			{
				int index = -1;
				BlockStack.Clear();
				while (!reader.IsEnd())
				{
					var c = (char)reader.Peek();
					if (BlockStack.Count == 0)
					{
						if (ignore.IndexOf(c) < 0 && BlockEnd.IndexOf(c) >= 0)
						{
							break;
						}
						else if ((index = BlockStart.IndexOf(c)) >= 0)
						{
							BlockStack.Push(BlockEnd[index]);
						}
					}
					else
					{
						if (BlockStack.Peek() == c)
						{
							BlockStack.Pop();
						}
						else if ((index = BlockStart.IndexOf(c)) >= 0)
						{
							BlockStack.Push(BlockEnd[index]);
						}
					}
					reader.Read();
					writer.Write(c);
				}
			});
		}

		public static void WriteCheckString(this StringWriter writer, string value)
		{
			if (value == null)
			{
				writer.Write("\"\"");
				return;
			}
			using (StringReader reader = new StringReader(value))
			{
				writer.Write("\"");
				while (!reader.IsEnd())
				{

					var c = (char)reader.Read();
					switch (c)
					{
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
		public static string ReadCheckString(this StringReader reader,string ignore="")
		{
			if (reader.NextIs('\"'))
			{
				using (var writer = new StringWriter())
				{
					while (!reader.IsEnd() && !reader.NextIs('\"'))
					{
						if (reader.NextIs('\\'))
						{
							var c = (char)reader.Read();
							switch (c)
							{
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
						else
						{
							writer.Write((char)reader.Read());
						}
					}
					return writer.ToString();
				}
			}
			else
			{
				return ReadObjectString(reader, ignore);
			}
		}

		public static QDataList ToQDataList<T>(this IList<T> list,QDataList qdataList=null, Type type=null) 
		{
			if (type == null)
			{
				type = typeof(T);
				if (type == typeof(object))
				{
					throw new Exception(nameof(QDataList)+ "类型出错 " + type);
				}
			}
			if (qdataList == null)
			{
				qdataList = new QDataList();
			}
			else
			{
				qdataList.Clear();
			}
			
			var typeInfo = QSerializeType.Get(type);
			foreach (var member in typeInfo.Members)
			{
				qdataList.TitleRow.Add(member.QName);
				for (int i = 0; i < list.Count; i++)
				{
					qdataList[i + 1].SetValueType(member.QName, member.Get(list[i]), member.Type);
				}
			}
			return qdataList;
		}
		public static List<T> ParseQDataList<T>(this QDataList qdataList, List<T> list,Type type=null) 
		{
			var startTime = QDebug.Timestamp;
			if (type == null)
			{
				type = typeof(T);
				if (type == typeof(object))
				{
					throw new Exception(nameof(QDataList) + "类型出错 " + type);
				}
			}
			var typeInfo = QSerializeType.Get(type);
			list.Clear();
			var titleRow = qdataList.TitleRow;
			var memeberList = new List<QMemeberInfo>();
			foreach (var title in titleRow)
			{
				var member = typeInfo.GetMemberInfo(title);
				if (member == null)
				{
					Debug.LogWarning("读取 " + type.Name + "出错 不存在属性 " + title);
				}
				memeberList.Add(member);
			}
			foreach (var row in qdataList)
			{
				if (row == titleRow) continue;
				var t = type.CreateInstance();
				for (int i = 0; i < titleRow.Count; i++)
				{
					var member = memeberList[i]; 
					if (member != null)
					{
						try
						{
							var value = row[i].ParseElement();
							member.Set(t, value.ParseQDataType(member.Type, false));
						}
						catch (System.Exception e)
						{

							Debug.LogError("读取 " + type.Name + "出错 设置[" + row.Key + "]属性 " + member.Key + "(" + member.Type + ")异常：\n" + e);
						}

					}
				}
				list.Add((T)t);
			}
			QDebug.Log("解析 QDataList<" + type.Name + "> 数据 " + list.Count+" 条 ",startTime);
			return list;
		}

		public static async Task<List<T>> ParseQDataListAsync<T>(this QDataList qdataList, List<T> list, Type type = null)
		{
			var startTime = QDebug.Timestamp;
			if (type == null)
			{
				type = typeof(T);
				if (type == typeof(object))
				{
					throw new Exception(nameof(QDataList) + "类型出错 " + type);
				}
			}
			var typeInfo = QSerializeType.Get(type);
			list.Clear();
			var titleRow = qdataList.TitleRow;
			var memeberList = new List<QMemeberInfo>();
			foreach (var title in titleRow)
			{
				var member = typeInfo.GetMemberInfo(title);
				if (member == null)
				{
					Debug.LogWarning("读取 " + type.Name + "出错 不存在属性 " + title);
				}
				memeberList.Add(member);
			}
			foreach (var row in qdataList)
			{
				await QTask.Step();
				if (row == titleRow) continue;
				var t = type.CreateInstance();
				for (int i = 0; i < titleRow.Count; i++)
				{
					var member = memeberList[i];
					if (member != null)
					{
						var value = row[i].ParseElement();
						try
						{
							if (member.IsUnityObject)
							{
								var obj = await QIdTool.LoadObjectAsync(value, member.Type);
								member.Set(t, obj);
							}
							else
							{
								member.Set(t, value.ParseQDataType(member.Type, false));
							}
						}
						catch (System.Exception e)
						{
							Debug.LogError("读取 " + type.Name + "出错 设置[" + row.Key + "]属性 " + member.Key + "(" + member.Type + ")异常["+value+"]：\n" + e);
						}

					}
				}
				list.Add((T)t);
			}
			QDebug.Log("异步解析 QDataList<" + type.Name + "> 数据 " + list.Count + " 条 ", startTime);
			return list;
		}
	}
	public class QDataParser
	{
		QDictionary<string, string> Dic;
		QList<string> List;
		public string Value { get; private set; }
		public T GetValue<T>()
		{
			return Value.ParseQData<T>();
		}
		public QDataParser(string data)
		{
			Value = data;
		}
		QDictionary<string, QDataParser> DicParser = new QDictionary<string, QDataParser>();
		QDictionary<int, QDataParser> ListParser = new QDictionary<int, QDataParser>();
		public QDataParser this[string key]
		{
			get
			{
				if (Dic == null)
				{
					try
					{
						Dic = Value.ParseQData(Dic);
					}
					catch (Exception e)
					{
						Debug.LogError("解析" + key + "【" + Value + "】");
						throw e;
					}
				}
				if (!DicParser.ContainsKey(key))
				{
					DicParser[key] = new QDataParser(Dic[key]);
				}
			
				return DicParser[key];
			}
		}
		public QDataParser this[int index]
		{
			get
			{
				if (List == null)
				{
					List = Value.ParseQData(List);
				}
				if (!ListParser.ContainsKey(index))
				{
					ListParser[index] = new QDataParser(List[index]);
				}
				return ListParser[index];
			}
		}
	}
	public interface IQData
	{
		void ToQData(StringWriter writer);
		void ParseQData(StringReader reader);
	}
	public interface IQSerializeCallback
	{
		void OnDeserializeOver();
	}
	public abstract class QSerializeObject : ISerializationCallbackReceiver, IQSerializeCallback
	{
		
		[QIgnore, HideInInspector]
		public string SerializeString;
		[QIgnore]
		bool Dirty { get; set; } = false;
		public virtual void SetDirty()
		{
			Dirty = true;
		}
		public void OnBeforeSerialize()
		{
			if (!Dirty) return;
			Dirty = false;
			SerializeString = this.ToQData();
		}
		public void OnAfterDeserialize()
		{
			SerializeString.ParseQData(this);
		}
		public abstract void OnDeserializeOver();
	}
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Interface)]
	public class QIgnoreAttribute : Attribute
	{

	}
	[AttributeUsage(AttributeTargets.Class ,AllowMultiple = false,Inherited = false)]
	public class QDynamicAttribute : Attribute
	{

	}

	public enum QObjectType
	{
		UnityObject,
		DynamicObject,
		Object,
		List,
		Array,
		Dictionary,
		TimeSpan,
		CantSerialize,
	}
	public class QSerializeType : QTypeInfo<QSerializeType>
	{
	
		public QObjectType objType = QObjectType.Object;
		public bool IsIQSerialize { private set; get; }
		public bool IsIQData { private set; get; }
		public bool HasCallback { private set; get; }
		protected override void Init(Type type)
		{
			Functions = null;
			base.Init(type);
			if (Code == TypeCode.Object)
			{
				objType = QObjectType.Object;
				if (typeof(Task).IsAssignableFrom(type))
				{
					objType = QObjectType.CantSerialize;
					return;
				}
				IsIQSerialize = typeof(IQSerialize).IsAssignableFrom(type);
				IsIQData = typeof(IQData).IsAssignableFrom(type);
				HasCallback= typeof(IQSerializeCallback).IsAssignableFrom(type);
				if (IsIQData)
				{
					objType = QObjectType.Object;
				}
				else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
				{
					objType = QObjectType.UnityObject;
				}
				else if( type==typeof(System.Object)||type.IsAbstract||type.IsInterface|| type.GetCustomAttribute<QDynamicAttribute>()!=null)
				{
					objType = QObjectType.DynamicObject;
				}
				else if (IsArray)
				{
					objType = QObjectType.Array;
				}
				else if (IsList)
				{
					objType = QObjectType.List;
				}
				else if(IsDictionary)
				{
					objType = QObjectType.Dictionary;
				}
				else if (type == typeof(TimeSpan))
				{
					objType = QObjectType.TimeSpan;
				}
				else
				{
					if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
					{
						Debug.LogError("不支持序列化【" + type + "】Nullable类型");
					}
				}
			}
			if (!TypeMembers.ContainsKey(type))
			{
				Members.RemoveAll((member) =>
				{
					return member.MemeberInfo.GetCustomAttribute<QIgnoreAttribute>() != null || (!member.IsPublic && member.MemeberInfo.GetCustomAttribute<QNameAttribute>() == null) || member.Key == "Item" || member.Set == null || member.Get == null || (member.Type.IsArray && member.Type.GetArrayRank() > 1);
				});
			}
		}

	}
}

