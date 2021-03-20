using QTool.Binary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace QTool.Serialize
{
    public interface IQSerialize
    {
        void Write(BinaryWriter write);
        void Read(BinaryReader read);
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class QTypeAttribute : Attribute
    {
        public string name = "";
        public QTypeAttribute(string name)
        {
            this.name = name;
        }
    }
    public class QSValueAttribute : Attribute
    {

    }
    public class QSIgnoreAttribute : Attribute
    {

    }
   
    public class QMemeberInfo : IKey<string>
    {
        public string Key { get => Name; set => value = Name; }
        public string Name;
        public Type type;
        public Action<object, object> set;
        public Func<object, object> get;
        public QMemeberInfo(FieldInfo info)
        {
            Name = info.Name;
            type = info.FieldType;
            set = info.SetValue;
            get = info.GetValue;
        }
        public QMemeberInfo(PropertyInfo info)
        {
            Name = info.Name;
            type = info.PropertyType;
            set = info.SetValue;
            get = info.GetValue;
        }
    }
    public class QTypeInfo
    {
        static bool IsQSValue(MemberInfo info, bool isPublic = true)
        {
            if (!isPublic && info.GetCustomAttribute<QSValueAttribute>() == null)
            {
                return false;
            }
            if (info.GetCustomAttribute<QSIgnoreAttribute>() != null)
            {
                return false;
            }
            return true;
        }
        public DicList<string,QMemeberInfo> memberList = new DicList<string,QMemeberInfo>();
        public bool IsList;
        public Type ListType;
        public Type ArrayType;
        public Type type;
        public int ArrayRank;
        public bool IsArray;
        public int[] indexArray;
        public bool IsISerialize = false;
        private QTypeInfo(Type type)
        {
            IsISerialize = typeof(IQSerialize).IsAssignableFrom(type);
            this.type = type;
            if (!IsISerialize && !type.IsInterface)
            {
                var qType = type.GetCustomAttribute<QTypeAttribute>();
                var fileName = type.Name;
                if (qType != null)
                {
                    fileName = qType.name;
                }
                IsArray = type.IsArray;
                if (type.IsArray)
                {
                    ArrayRank = type.GetArrayRank();
                    ArrayType = type.GetElementType();
                    indexArray = new int[ArrayRank];
                }
                else
                {
                    var listInterFace = type.GetInterface(typeof(IList<>).FullName, true);
                    IsList = (listInterFace != null);
                    if (IsList)
                    {
                        ListType = listInterFace.GenericTypeArguments[0];

                    }
                    type.ForeachMemeber((item) =>
                    {
                        if (IsQSValue(item, item.IsPublic))
                        {
                            memberList.Add(new QMemeberInfo(item));
                        }
                    },
                    (item) => {
                        if (IsQSValue(item, item.Name != "Item" && item.CanRead && item.CanWrite && item.SetMethod.IsPublic && item.GetMethod.IsPublic))
                        {
                            memberList.Add(new QMemeberInfo(item));
                        }
                    });
                }
            }
        }
        public static Dictionary<Type, QTypeInfo> table = new Dictionary<Type, QTypeInfo>();
        public static QTypeInfo Get(Type type)
        {
            if (!table.ContainsKey(type))
            {
                var info = new QTypeInfo(type);
                table.Add(type, info);

            }
            return table[type];
        }

    }

    public static class QSerialize
    {
        public static void ForeachMemeber(this Type type, Action<FieldInfo> fieldInfo, Action<PropertyInfo> propertyInfo = null)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var item in fields)
            {
                fieldInfo?.Invoke(item);
            }
            var infos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var item in infos)
            {
                propertyInfo?.Invoke(item);
            }
        }
        public static System.Byte[] Serialize<T>(this T value)
        {
            return SerializeType(value, typeof(T));
        }
        public static System.Byte[] SerializeType(object value,Type type)
        {
            var writer = BinaryWriter.Get();
            writer.Clear();
            writer.WriteObjectType(value,type);
            var bytes = writer.ToArray();
            BinaryWriter.Push(writer);
            return bytes;
        }
    
        public static T Deserialize<T>(byte[] bytes, T targetObj = default)
        {
            return (T)DeserializeType(bytes, typeof(T), targetObj);
        }
        public static object DeserializeType(byte[] bytes,Type type, object targetObj = default)
        {
            var reader = BinaryReader.Get();
            reader.Reset(bytes);
            var obj = reader.ReadObjectType(type, targetObj);
            BinaryReader.Push(reader);
            return obj;
        }
      
        static void ForeachArray(Array array, int deep, int[] indexArray, Action<int[]> Call)
        {
            for (int i = 0; i < array.GetLength(deep); i++)
            {
                indexArray[deep] = i;
                if (deep + 1 < indexArray.Length)
                {
                    ForeachArray(array, deep + 1, indexArray, Call);
                }
                else
                {
                    Call?.Invoke(indexArray);
                }
            }
        }

        static Dictionary<string, Type> typeDic = new Dictionary<string, Type>();
        public static Type ParseType(string typeString)
        {
            if (typeDic.ContainsKey(typeString))
            {
                return typeDic[typeString];
            }
            else
            {
                Type type = null;
                Assembly[] assemblyArray = AppDomain.CurrentDomain.GetAssemblies();
                int assemblyArrayLength = assemblyArray.Length;
                for (int i = 0; i < assemblyArrayLength; ++i)
                {
                    type = assemblyArray[i].GetType(typeString);
                    if (type != null)
                    {
                        typeDic.Add(typeString, type);
                        return type;
                    }
                }
            }
            return null;

        }
        static object CreateInstance(Type type, object targetObj, params object[] args)
        {
            if (targetObj != null)
            {
                return targetObj;
            }
            return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);
        }
        public enum ObjectState:byte
        {
            Null=0,
            Normal=1,
            Dynamic=2,
        }
      
        public static BinaryWriter WriteObjectType(this BinaryWriter writer, object value, Type type)
        {
            TypeCode typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                case TypeCode.Object:
                    var isNull = value == null;
                    if (isNull)
                    {
                        writer.Write((byte)ObjectState.Null);
                        return writer;
                    }
                    var trueType = value.GetType();
                    if (trueType != type)
                    {
                        writer.Write((byte)ObjectState.Dynamic);
                        writer.Write(trueType.FullName);
                      
                        type = trueType;
                       
                    }
                    else
                    {
                        writer.Write((byte)ObjectState.Normal);
                    }
                    QTypeInfo typeInfo = QTypeInfo.Get(type);
                    if (typeInfo.IsISerialize)
                    {
                        (value as IQSerialize).Write(writer);
                    }
                    else
                    {
                        if (typeInfo.IsArray)
                        {
                            var array = value as Array;
                            writer.Write((byte)typeInfo.ArrayRank);
                            for (int i = 0; i < typeInfo.ArrayRank; i++)
                            {
                                writer.Write(array.GetLength(i));
                            }
                            ForeachArray(array, 0, typeInfo.indexArray, (indexArray) => { writer.WriteObjectType(array.GetValue(indexArray), typeInfo.ArrayType); });
                        }
                        else
                        {
                            if (typeInfo.IsList)
                            {
                                var list = value as IList;
                                writer.Write(list.Count);
                                foreach (var item in list)
                                {
                                    writer.WriteObjectType(item, typeInfo.ListType);
                                }
                            }
                            writer.Write((byte)typeInfo.memberList.Count);
                            foreach (var item in typeInfo.memberList)
                            {
                                writer.Write(item.Name, LengthType.Byte);
                                var memberObj = item.get(value);
                                writer.Write(SerializeType(memberObj, item.type), LengthType.Byte);
                            }
                        }
                    }
                    break;
                #region 基础类型
                case TypeCode.Boolean:
                    writer.Write((bool)value);
                    break;
                case TypeCode.Byte:
                    writer.Write((byte)value);
                    break;
                case TypeCode.Char:
                    writer.Write((char)value);
                    break;
                case TypeCode.DateTime:
                    writer.Write(((DateTime)value).Ticks);
                    break;
                case TypeCode.DBNull:
                    break;
                case TypeCode.Decimal:
                case TypeCode.Double:
                    writer.Write((double)value);
                    break;
                case TypeCode.Empty:
                    break;
                case TypeCode.Int16:
                    writer.Write((Int16)value);
                    break;
                case TypeCode.Int32:
                    writer.Write((Int32)value);
                    break;
                case TypeCode.Int64:
                    writer.Write((Int64)value);
                    break;

                case TypeCode.SByte:
                    writer.Write((SByte)value);
                    break;
                case TypeCode.Single:
                    writer.Write((Single)value);
                    break;
                case TypeCode.String:
                    writer.Write((string)value);
                    break;
                case TypeCode.UInt16:
                    writer.Write((UInt16)value);
                    break;
                case TypeCode.UInt32:
                    writer.Write((UInt32)value);
                    break;
                case TypeCode.UInt64:
                    writer.Write((UInt64)value);
                    break;
                default:
                    break;


                    #endregion
            }
            return writer;
        }
        public static object ReadObjectType(this BinaryReader reader, Type type, object target = null)
        {
            TypeCode typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {

                case TypeCode.Object:
                    QTypeInfo typeInfo = null;
                    switch ((ObjectState)reader.ReadByte())
                    {
                        case ObjectState.Null:
                            return null;
                        case ObjectState.Normal:
                            typeInfo = QTypeInfo.Get(type);
                            break;
                        case ObjectState.Dynamic:
                            type = ParseType(reader.ReadString(LengthType.Byte));
                            typeInfo = QTypeInfo.Get(type);
                            break;
                        default:
                            break;
                    }
                    if (typeInfo.IsISerialize)
                    {
                        var serObj = CreateInstance(type, target) as IQSerialize;
                        serObj.Read(reader);
                        return serObj;
                    }
                    else
                    {
                        if (typeInfo.IsArray)
                        {
                            var rank = reader.ReadByte();
                            var count = 1;
                            for (int i = 0; i < rank; i++)
                            {
                                typeInfo.indexArray[i] = reader.ReadInt32();
                                count *= typeInfo.indexArray[i];
                            }
                            var array = (Array)CreateInstance(type, target, count);
                            ForeachArray(array, 0, typeInfo.indexArray, (indexArray) => {
                                var obj = array.GetValue(indexArray); if (obj == null)
                                {
                                    Debug.LogError(indexArray.ToOneString() + " 数据为空[" + target + "]");
                                }
                                array.SetValue(reader.ReadObjectType(typeInfo.ArrayType, array.GetValue(indexArray)), indexArray);
                            });
                            return array;
                        }
                        else
                        {
                            var obj = CreateInstance(type, target);

                            if (typeInfo.IsList)
                            {
                                var list = obj as IList;
                                var count = reader.ReadInt32();
                                for (int i = 0; i < count; i++)
                                {
                                    if (list.Count > i)
                                    {
                                        list[i] = reader.ReadObjectType(typeInfo.ListType, list[i]);
                                    }
                                    else
                                    {
                                        list.Add(reader.ReadObjectType(typeInfo.ListType));
                                    }

                                }
                            }
                            var memberCount = reader.ReadByte();
                            for (int i = 0; i < memberCount; i++)
                            {
                                var name = reader.ReadString(LengthType.Byte);
                                var bytes = reader.ReadBytes(LengthType.Byte);
                                if (typeInfo.memberList.ContainsKey(name))
                                {
                                    var memeberInfo = typeInfo.memberList[name];
                                    memeberInfo.set.Invoke(obj, DeserializeType(bytes, memeberInfo.type, target != null ? memeberInfo.get?.Invoke(target) : null));
                                }
                            }
                            return obj;
                        }
                    }
                #region 基础类型

                case TypeCode.Boolean:
                    return reader.ReadBoolean();
                case TypeCode.Byte:
                    return reader.ReadByte();
                case TypeCode.Char:
                    return reader.ReadChar();
                case TypeCode.DateTime:
                    return new DateTime(reader.ReadInt64());
                case TypeCode.DBNull:
                    return null;
                case TypeCode.Decimal:
                    return (decimal)reader.ReadDouble();
                case TypeCode.Double:
                    return reader.ReadDouble();
                case TypeCode.Empty:
                    return null;
                case TypeCode.Int16:
                    return reader.ReadInt16();
                case TypeCode.Int32:
                    return reader.ReadInt32();
                case TypeCode.Int64:
                    return reader.ReadInt64();

                case TypeCode.SByte:
                    return reader.ReadSByte();
                case TypeCode.Single:
                    return reader.ReadSingle();
                case TypeCode.String:
                    return reader.ReadString();
                case TypeCode.UInt16:
                    return reader.ReadUInt16();
                case TypeCode.UInt32:
                    return reader.ReadUInt32();
                case TypeCode.UInt64:
                    return reader.ReadUInt64();
                default:
                    return null;

                    #endregion
            }
        }
    }
}
