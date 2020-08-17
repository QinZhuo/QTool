using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Collections;
using UnityEngine;
namespace QTool
{
    public class QSValueAttribute : Attribute
    {
        
    }
    public class QSIgnoreAttribute : Attribute
    {

    }
    public class QMemeberInfo
    {
        public Type type;
        public Action<object, object> set;
        public Func<object, object> get;
        public QMemeberInfo(FieldInfo info)
        {
            type = info.FieldType;
            set = info.SetValue;
            get = info.GetValue;
        }
        public QMemeberInfo(PropertyInfo info)
        {
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
        public List<QMemeberInfo> memberList = new List<QMemeberInfo>();
        public bool IsList;
        public Type ListType;
        private QTypeInfo(Type type)
        {

            IsList = (type.GetInterface(typeof(IList<>).FullName, true) != null);
            if (IsList)
            {
                ListType= type.GenericTypeArguments[0];
            }
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var item in fields)
            {
                if (IsQSValue(item, item.IsPublic))
                {
                    memberList.Add(new QMemeberInfo(item));
                }

            }
            var infos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var item in infos)
            {
                if (IsQSValue(item, item.Name != "Item" && item.CanRead && item.CanWrite && item.SetMethod.IsPublic && item.GetMethod.IsPublic))
                {
                    memberList.Add(new QMemeberInfo(item));
                }
            }
        }
        public static Hashtable hashtable=new Hashtable();
        public static QTypeInfo Get(Type type)
        {
            if (hashtable.ContainsKey(type.FullName))
            {
                return (QTypeInfo)hashtable[type.FullName];
            }
            else
            {
                var info= new QTypeInfo(type);
                hashtable.Add(type.FullName, info);
                return info;
            }
           
        }
       
    }
    public static class QSerialize
    {
        public static Byte[] Serialize<T>(T value)
        {
            using (MemoryStream memory=new MemoryStream())
            {
                using (BinaryWriter writer=new BinaryWriter(memory))
                {
                  
                    var type = typeof(T);
                    writer.WriteValue(type,value);
                    return memory.ToArray();
                }
            }
        }
        public static T Deserialize<T>(byte[] bytes)
        {
            using (MemoryStream memory = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(memory))
                {
                    var type = typeof(T);
                    return (T)reader.ReadValue(type);
                }
            }
        }
        static void ForeachArray(Array array,int deep,int[] indexArray,Action<object> Call)
        {
            for (int i = 0; i < array.GetLength(deep); i++)
            {
                indexArray[deep] = i;
                if (deep+1 < indexArray.Length)
                {
                    ForeachArray(array, deep + 1, indexArray,Call);
                }
                else
                {
                    Call?.Invoke(array.GetValue(indexArray));
                }
              
            }
        }
   
        static object CreateInstance(Type type,params object[] args)
        {
            return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);
        }
        static object ReadValue(this BinaryReader reader, Type type)
        {
            var typeCode = Type.GetTypeCode(type);
            var valueTypeCode= (TypeCode)reader.ReadByte();
            switch (typeCode)
            {
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
                case TypeCode.Object:
                    if (type.IsArray)
                    {
                        var elementType = type.GetElementType();
                        var rank = reader.ReadInt32();
                        var indexArray = new int[rank];
                        var count = 1;
                        for (int i = 0; i < rank; i++)
                        {
                            indexArray[i] = reader.ReadInt32();
                            count *= indexArray[i];
                        }
                        var array =(Array)CreateInstance(type, count) ;
                        ForeachArray(array, 0, indexArray, (obj)=> { array.SetValue( reader.ReadValue(elementType), indexArray ); });
                        return array;
                    }
                    else 
                    {
                        var obj =  CreateInstance(type);
                        
                        QTypeInfo typeInfo = QTypeInfo.Get(type);
                        if (typeInfo.IsList)
                        {
                            var list = obj as IList;
                            var count = reader.ReadInt32();
                            for (int i = 0; i < count; i++)
                            {
                                var listObj= reader.ReadValue(typeInfo.ListType);
                                list.Add(listObj);
                            }
                        }
                        {
                            foreach (var item in typeInfo.memberList)
                            {
                                item.set?.Invoke(obj, reader.ReadValue(item.type));
                            }
                        }
                        return obj;
                    }
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
            }
        }
        static void WriteValue(this BinaryWriter writer ,Type type ,object value)
        {
            var typeCode = Type.GetTypeCode(type);
            writer.Write((byte)typeCode);
            switch (typeCode)
            {
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
                case TypeCode.Object:
                    if (type.IsArray)
                    {
                        var array = value as Array;
                        var elementType = type.GetElementType();
                        writer.Write(array.Rank);
                        for (int i = 0; i < array.Rank; i++)
                        {
                            writer.Write(array.GetLength(i));
                        }
                        var indexArray = new int[array.Rank];
                        ForeachArray(array, 0, indexArray, (obj) => {writer.WriteValue(elementType,obj); });
     
                    }
                    else
                    {
                        QTypeInfo typeInfo = QTypeInfo.Get(type);
                        if (typeInfo.IsList)
                        {
                            var list = value as IList;
                            if (list == null)
                            {
                                UnityEngine.Debug.LogError(type+":"+ value);
                            }
                            writer.Write(list.Count);
                            foreach (var item in list)
                            {
                                writer.WriteValue(typeInfo.ListType, item);
                            }
                        }
                        {
                            foreach (var item in typeInfo.memberList)
                            {
                                writer.WriteValue(item.type, item.get(value));
                            }
                        }
                      
                          
                       
                    }
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
            }
        }
    }
}
