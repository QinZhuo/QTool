using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using QTool.ByteExtends;
namespace QTool
{
    [AttributeUsage(AttributeTargets.Class| AttributeTargets.Interface)]
    public class DynamicAttribute : Attribute
    {

    }
    public class QSValueAttribute : Attribute
    {
        
    }
    public class QSIgnoreAttribute : Attribute
    {

    }
    public class QMemeberInfo
    {
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
            type =info.PropertyType;
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
        public Type ArrayType;
        public Type type;
        public int ArrayRank;
        public bool IsArray;
        public int[] indexArray;
        public bool dynamic = false;
        private QTypeInfo(Type type)
        {
           // Debug.LogError(" new Type " + type);
            this.type = type;
            dynamic = type.GetCustomAttribute<DynamicAttribute>() != null;
            IsArray = type.IsArray;
            if (type.IsArray)
            {
                ArrayRank= type.GetArrayRank();
                ArrayType = type.GetElementType();
                indexArray = new int[ArrayRank]; 
            }
            var listInterFace = type.GetInterface(typeof(IList<>).FullName, true);
            IsList = (listInterFace != null);
            if (IsList)
            {
                ListType= listInterFace.GenericTypeArguments[0];

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
        public static Dictionary<Type, QTypeInfo> table = new Dictionary<Type, QTypeInfo>();
        public static QTypeInfo Get(Type type)
        {
            if (table.ContainsKey(type))
            {
                return table[type];
            }
            else
            {
                var info= new QTypeInfo(type);
                table.Add(type, info);
                return info;
            }
           
        }
       
    }

    public static class QSerialize
    {

        public static BinaryReader reader=new BinaryReader();
        public static BinaryWriter writer=new BinaryWriter();
        public static Byte[] Serialize<T>(T value)
        {
            typeStrList.Clear();
            typeIndexDic.Clear();
            var type = typeof(T);
            writer.Clear();
            writer.WriteValue(value,type);
            for (int i = typeStrList.Count-1; i >= 0; i--)
            {
                var bytes=typeStrList[i].GetBytes();
                writer.byteList.InsertRange(0, bytes);
                writer.byteList.InsertRange(0, bytes.Length.GetBytes());
            }
            writer.byteList.InsertRange(0, typeStrList.Count.GetBytes());
            return writer.ToArray();
        }
        public static T Deserialize<T>(byte[] bytes)
        {
            var type = typeof(T);
            reader.Reset(bytes);
            typeStrList.Clear();
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                typeStrList.Add(reader.ReadString());
            }
            return (T)reader.ReadValue(type);
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
        static List<string> typeStrList = new List<string>();
        static Dictionary<string,byte> typeIndexDic =new Dictionary<string, byte>();
        static string GetTypeStr(byte index)
        {
            if(index>= typeStrList.Count)
            {
                Debug.LogError("错误：" + index + "/" + typeStrList.Count);
            }
            return typeStrList[index];
        }
        static byte GetTypeIndex(string typeName)
        {
            if (typeIndexDic.ContainsKey(typeName))
            {
                return typeIndexDic[typeName];
            }
            else
            {
                var index = (byte)typeStrList.Count;
                typeStrList.Add(typeName);
                typeIndexDic.Add(typeName,index);
                return index;
            }
           
        }
        static Dictionary<string, Type> typeDic = new Dictionary<string, Type>();
        static Type ParseType(string typeString)
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
        static object CreateInstance(Type type, params object[] args)
        {
            return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);
        }
        static object ReadValue(this BinaryReader reader, Type type)
        {
            TypeCode typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Object:
                    if (reader.ReadBoolean())
                    {
                        return null;
                    }
                    QTypeInfo typeInfo = QTypeInfo.Get(type);
                    if (typeInfo.dynamic)
                    {
                        type = ParseType(GetTypeStr(reader.ReadByte()));
                        typeInfo = QTypeInfo.Get(type);
                    }
                    if (typeInfo.IsArray)
                    {
                        var rank = reader.ReadInt32();
                        var count = 1;
                        for (int i = 0; i < rank; i++)
                        {
                            typeInfo.indexArray[i] = reader.ReadInt32();
                            count *= typeInfo.indexArray[i];
                        }
                        var array = (Array)CreateInstance(type, count);
                        ForeachArray(array, 0, typeInfo.indexArray, (obj) => { array.SetValue(reader.ReadValue(typeInfo.ArrayType), typeInfo.indexArray); });
                        return array;
                    }
                    else
                    {
                        var obj = CreateInstance(type);
                        if (typeInfo.IsList)
                        {
                            var list = obj as IList;
                            var count = reader.ReadInt32();
                            for (int i = 0; i < count; i++)
                            {
                                var listObj = reader.ReadValue(typeInfo.ListType);
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
            }
        }
        static void WriteValue(this BinaryWriter writer ,object value,Type type)
        {
            TypeCode typeCode =Type.GetTypeCode(type);
         
            switch (typeCode)
            {
                case TypeCode.Object:
                    var isNull = value == null;
                    writer.Write(isNull);
                    if (isNull)
                    {
                        return;
                    }
                    QTypeInfo typeInfo = QTypeInfo.Get(type);
                    if (typeInfo.dynamic)
                    {
                        type = (value == null ? null : value.GetType());
                        typeInfo = QTypeInfo.Get(type);
                        writer.Write(GetTypeIndex(type.FullName));
                    }
                    if (typeInfo.IsArray)
                    {
                        var array = value as Array;
                        writer.Write(typeInfo.ArrayRank);
                        for (int i = 0; i < typeInfo.ArrayRank; i++)
                        {
                            writer.Write(array.GetLength(i));
                        }
                        ForeachArray(array, 0, typeInfo.indexArray, (obj) => { writer.WriteValue(obj,typeInfo.ArrayType); });
                    }
                    else
                    {
                        if (typeInfo.IsList)
                        {
                            var list = value as IList;
                            writer.Write(list.Count);
                            foreach (var item in list)
                            {
                                writer.WriteValue(item,typeInfo.ListType);
                            }
                        }
                        {
                            foreach (var item in typeInfo.memberList)
                            {
                                
                                    var memberObj = item.get(value);
                                    writer.WriteValue(memberObj, item.type);
                            
                                
                            }
                        }
                    }
                    break;
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
            }
        }
    }
}
