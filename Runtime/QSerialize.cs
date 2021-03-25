using QTool.Binary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using QTool.Reflection;
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
        public bool dynamic = false;
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property|AttributeTargets.Interface)]
    public class QSValueAttribute : Attribute
    {

    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Interface)]
    public class QSIgnoreAttribute : Attribute
    {

    }
    #region 类型数据

  
    public enum QTypeState
    {
        Normal,
        List,
        Array,
        Dynamic,
        ISerialize,
    }
    public class QSerializeType:QTypeInfo<QSerializeType>
    {
        static bool IsQSValue(MemberInfo info)
        {
            if (info.GetCustomAttribute<QSIgnoreAttribute>() != null)
            {
                return false;
            }
            if (info.GetCustomAttribute<QSValueAttribute>() != null)
            {
                return true;
            }
            return true;
        }
        public QTypeState state= QTypeState.Normal;
        protected override void Init(Type type)
        {
            Functions = null;
            base.Init(type);
            if (Code == TypeCode.Object)
            {
                if (typeof(IQSerialize).IsAssignableFrom(type))
                {
                    state = QTypeState.ISerialize;
                    return;
                }
                else if (IsArray)
                {
                    state = QTypeState.Array;
                }
                else if (IsList)
                {
                    state = QTypeState.List;
                }
                else
                {
                    var qType = Type.GetCustomAttribute<QTypeAttribute>();
                    if ((qType != null && qType.dynamic) || type.IsInterface)
                    {
                        state = QTypeState.Dynamic;
                    }
                    Members.RemoveAll((info) =>
                    {
                        return !IsQSValue(info.MemeberInfo);
                    });
                }
            }
        }

    }
  
    #endregion
    public static class QSerialize
    {
      
        public static System.Byte[] Serialize<T>(this T value)
        {
            return SerializeType(value, typeof(T));
        }
        public static System.Byte[] SerializeType(object value,Type type)
        {
            var writer = BinaryWriter.Get();
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

       
        static object CreateInstance(Type type, object targetObj, params object[] args)
        {
            if (targetObj != null)
            {
                return targetObj;
            }
            return Activator.CreateInstance(type, args);
        }
       
      
        public static BinaryWriter WriteObjectType(this BinaryWriter writer, object value, Type type)
        {
            TypeCode typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Object:
                    {
                        var IsNull = object.Equals(value, null);
                        if (IsNull)
                        {
                            return writer;
                        }
                        QSerializeType typeInfo = QSerializeType.Get(type);
                        switch (typeInfo.state)
                        {
                            case QTypeState.ISerialize:
                                (value as IQSerialize).Write(writer);
                                break;
                            case QTypeState.List:
                                var list = value as IList;
                                writer.Write(list.Count);
                                foreach (var item in list)
                                {
                                    writer.WriteObjectType(item, typeInfo.ElementType);
                                }
                                break;
                            case QTypeState.Array:
                                var array = value as Array;
                                writer.Write((byte)typeInfo.ArrayRank);
                                for (int i = 0; i < typeInfo.ArrayRank; i++)
                                {
                                    writer.Write(array.GetLength(i));
                                }
                                ForeachArray(array, 0, typeInfo.IndexArray, (indexArray) => { writer.WriteObjectType(array.GetValue(indexArray), typeInfo.ElementType); });
                                break;
                            case QTypeState.Dynamic:
                            case QTypeState.Normal:
                                if(typeInfo.state== QTypeState.Dynamic)
                                {
                                    writer.Write(type.FullName);
                                }
                                writer.Write((byte)typeInfo.Members.Count);
                                foreach (var item in typeInfo.Members)
                                {
                                    writer.Write(item.Name, LengthType.Byte);
                                    var memberObj = item.Get(value);
                                    writer.Write(SerializeType(memberObj, item.Type), LengthType.Byte);
                                }
                                break;
                            default:
                                throw new Exception("序列化类型[" + type + "]出错");
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
                    QSerializeType typeInfo = null;
                    if (reader.IsEnd)
                    {
                        return null;
                    }
                    typeInfo = QSerializeType.Get(type);
                    switch (typeInfo.state)
                    {
                       
                        case QTypeState.List:
                            {
                                var obj = CreateInstance(type, target);
                                var list = obj as IList;
                                var count = reader.ReadInt32();
                                for (int i = 0; i < count; i++)
                                {
                                    if (list.Count > i)
                                    {
                                        list[i] = reader.ReadObjectType(typeInfo.ElementType, list[i]);
                                    }
                                    else
                                    {
                                        list.Add(reader.ReadObjectType(typeInfo.ElementType));
                                    }

                                }
                                return list;
                            }
                        case QTypeState.Array:
                            {
                                var rank = reader.ReadByte();
                                var count = 1;
                                for (int i = 0; i < rank; i++)
                                {
                                    typeInfo.IndexArray[i] = reader.ReadInt32();
                                    count *= typeInfo.IndexArray[i];
                                }
                                var array = (Array)CreateInstance(type, target, count);
                                ForeachArray(array, 0, typeInfo.IndexArray, (indexArray) =>
                                {
                                    var obj = array.GetValue(indexArray); if (obj == null)
                                    {
                                        Debug.LogError(indexArray.ToOneString() + " 数据为空[" + target + "]");
                                    }
                                    array.SetValue(reader.ReadObjectType(typeInfo.ElementType, array.GetValue(indexArray)), indexArray);
                                });
                                return array;
                            }
                        case QTypeState.ISerialize:
                            {
                                var serObj = CreateInstance(type, target) as IQSerialize;
                                serObj.Read(reader);
                                return serObj;
                            }
                        case QTypeState.Dynamic:
                        case QTypeState.Normal:
                            {
                                if (typeInfo.state == QTypeState.Dynamic)
                                {
                                    var typeName = reader.ReadString(LengthType.Byte);
                                }
                                var obj = CreateInstance(type, target);
                                var memberCount = reader.ReadByte();
                                for (int i = 0; i < memberCount; i++)
                                {
                                    var name = reader.ReadString(LengthType.Byte);
                                    var bytes = reader.ReadBytes(LengthType.Byte);
                                    if (typeInfo.Members.ContainsKey(name))
                                    {
                                        var memeberInfo = typeInfo.Members[name];
                                        memeberInfo.Set.Invoke(obj, DeserializeType(bytes, memeberInfo.Type, target != null ? memeberInfo.Get?.Invoke(target) : null));
                                    }
                                }
                                return obj;
                            }
                        default:
                            Debug.LogError("反序列化类型[" + type + "]出错");
                            return null;
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
