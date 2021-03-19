using QTool.Binary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace QTool.Serialize
{
    //[AttributeUsage(AttributeTargets.Class| AttributeTargets.Interface)]
    //public class DynamicAttribute : Attribute
    //{

    //}
    public interface IQSerialize
    {
        void Write(BinaryWriter write);
        void Read(BinaryReader read);
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class QTypeAttribute : Attribute
    {
        public string name="";
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
    public class QMemeberFile:IKey<string>
    {
        public string Key { get; set; }
        public QMemeberFile()
        {

        }
        public QMemeberFile(FieldInfo info)
        {
            Key = info.Name;
        }
        public QMemeberFile(PropertyInfo info)
        {
            Key = info.Name;
        }
    }
    public class QTypeFile : IKey<string>
    {
        public static string BasePath { get { return Application.streamingAssetsPath + "/QSerialize/"; } }
        public static List<QTypeFile> fileList = new List<QTypeFile>();
        
        public static QTypeFile Get(string name,bool autoCreate=false,Func<QTypeFile> createFunc=null)
        {
            if (fileList.ContainsKey(name))
            {
                return fileList.Get(name);
            }
            var path = BasePath + name;
            if (FileManager.ExistsFile(path))
            {
                var qtype = FileManager.Deserialize<QTypeFile>(FileManager.Load(path));
                fileList.Add(qtype);
                return qtype;
            }
            else if(autoCreate)
            {
                var qtype = createFunc?.Invoke();
                qtype.Key = name;
                fileList.Add(qtype);
                FileManager.Save(path, FileManager.Serialize(qtype));
                return qtype;
            }
            else
            {
                return null;
            }
        }
        public string Key { get; set; }
        public List<QMemeberFile> Members = new List<QMemeberFile>()
        {
        };
        //public List<string> typeList = new List<string>();
        //public string GetTypeStr(byte index)
        //{
        //    if (index >= typeList.Count)
        //    {
        //        Debug.LogError("错误：" + index + "/" + typeList.Count);
        //    }
        //    return typeList[index];
        //}
        //public byte GetTypeIndex(string typeName)
        //{
        //    if (typeList.Contains(typeName))
        //    {
        //        return (byte)typeList.IndexOf(typeName);
        //    }
        //    else
        //    {
        //        var index = (byte)typeList.Count;
        //        typeList.Add(typeName);
        //        return index;
        //    }
        //}
    }
    public class QMemeberInfo:IKey<string>
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
        public QTypeFile qTypeFile;
        public bool IsISerialize=false;
        private QTypeInfo Init(Type type,bool autoCreate)
        {
            IsISerialize = typeof(IQSerialize).IsAssignableFrom(type);
            if (type.IsInterface&&!IsISerialize)
            {
                throw new Exception("不能序列化接口[" + type + "]");
            }
            this.type = type;
            if (IsISerialize)
            {
                return this;
            }
            var qType = type.GetCustomAttribute<QTypeAttribute>();
            var fileName = type.Name;
            if (qType != null)
            {
                fileName=qType.name;
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
                qTypeFile = QTypeFile.Get(fileName, autoCreate, () => {
                    qTypeFile = new QTypeFile();
                    qTypeFile.Key = fileName;
                    FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var item in fields)
                    {
                        if (IsQSValue(item, item.IsPublic))
                        {
                            qTypeFile.Members.Add(new QMemeberFile(item));
                        }
                    }
                    var infos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var item in infos)
                    {
                        if (IsQSValue(item, item.Name != "Item" && item.CanRead && item.CanWrite && item.SetMethod.IsPublic && item.GetMethod.IsPublic))
                        {
                            qTypeFile.Members.Add(new QMemeberFile(item));
                        }
                    }
                    Debug.LogError("新类型 " + fileName);
                    return qTypeFile;
                });
                if (qTypeFile == null)
                {
                    return null;
                }

            }
            var listInterFace = type.GetInterface(typeof(IList<>).FullName, true);
            IsList = (listInterFace != null);
            if (IsList)
            {
                ListType= listInterFace.GenericTypeArguments[0];

            }
            if (qTypeFile != null)
            {
                foreach (var member in qTypeFile.Members)
                {
                    var fieldInfo = type.GetField(member.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fieldInfo != null)
                    {
                        memberList.Add(new QMemeberInfo(fieldInfo));
                    }
                    var propertyInfo = type.GetProperty(member.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (propertyInfo != null)
                    {
                        memberList.Add(new QMemeberInfo(propertyInfo));
                    }
                }
            }
            return this;
        }
        public static Dictionary<Type, QTypeInfo> table = new Dictionary<Type, QTypeInfo>();
        public static QTypeInfo Get(Type type,bool write=false)
        {
            if (table.ContainsKey(type))
            {
                return table[type];
            }
            else
            {
                var info= new QTypeInfo().Init(type, write);
                if (info != null)
                {
                    table.Add(type, info);
                    return info;
                }
                else
                {
                    Debug.LogError("不存在类型[" + type.Name + "]的QSerialise序列化配置文件");
                    return null;
                }
              
            }
           
        }
       
    }

    public static class QSerialize
    {

       // public static BinaryReader reader=new BinaryReader();
      // public static BinaryWriter writer=new BinaryWriter();
        
        public static System.Byte[] Serialize<T>(T value)
        {
            return Serialize(typeof(T),value);
        }
        public static System.Byte[] Serialize(Type type,object value)
        {
            var writer = BinaryWriter.Get();
            writer.Clear();
            writer.Serialize(value);
            var bytes = writer.ToArray();
            BinaryWriter.Push(writer);
            return bytes;
        }
        public static BinaryWriter Serialize(this BinaryWriter writer,Type type,object value)
        {
            writer.WriteValue(value, type);
            return writer;
        }
        public static BinaryWriter Serialize<T>(this BinaryWriter writer,  T value)
        {
            return writer.Serialize(typeof(T), value);
        }
        public static object Deserialize(byte[] bytes,Type type, object targetObj = default)
        {
            var reader = BinaryReader.Get();
            reader.Reset(bytes);
            var obj = reader.Deserialize(type,targetObj);
            BinaryReader.Push(reader);
            return obj;
        }
        public static T Deserialize<T>(byte[] bytes, T targetObj = default)
        {
            return (T)Deserialize(bytes, typeof(T), targetObj);
        }
        public static object Deserialize(this BinaryReader reader,Type type, object targetObj = default)
        {
            return reader.ReadValue(type, targetObj);
        }
        public static T Deserialize<T>(this BinaryReader reader, T targetObj = default)
        {
            try
            {
                var obj = (T)reader.Deserialize(typeof(T), targetObj);
                return obj;
            }
            catch (Exception e)
            {
                throw new Exception("反序列化类型[" + typeof(T) + "]失败 error:[" + e + "]");
            }

        }
        static void ForeachArray(Array array,int deep,int[] indexArray,Action<int[]> Call)
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
        static object CreateInstance(Type type,object targetObj, params object[] args)
        {
            if (targetObj != null)
            {
                return targetObj;
            }
            return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);
        }
        
        static object ReadValue(this BinaryReader reader, Type type,object target=null)
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
                    //if (typeInfo.dynamic)
                    //{
                    //    type = ParseType(typeInfo.qTypeFile.GetTypeStr(reader.ReadByte()));
                    //    typeInfo = QTypeInfo.Get(type);
                    //}
                    if (typeInfo.IsISerialize)
                    {
                        var serObj = CreateInstance(type,target) as IQSerialize;
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
                            var array = (Array)CreateInstance(type,target, count);
                            ForeachArray(array, 0, typeInfo.indexArray, (indexArray) => { var obj = array.GetValue(indexArray);if (obj == null) {
                                    Debug.LogError(indexArray.ToOneString() + " 数据为空["+target+"]");
                                } array.SetValue(reader.ReadValue(typeInfo.ArrayType,array.GetValue(indexArray)), indexArray); });
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
                                        list[i] = reader.ReadValue(typeInfo.ListType, list[i]);
                                    }
                                    else
                                    {
                                        list.Add( reader.ReadValue(typeInfo.ListType));
                                    }
                                  
                                }
                            }
                            {
                                foreach (var item in typeInfo.memberList)
                                {
                                    item.set?.Invoke(obj, reader.ReadValue(item.type, target!=null? item.get?.Invoke( target):default));
                                }
                            }
                            return obj;
                        }
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
                    QTypeInfo typeInfo = QTypeInfo.Get(type,true);
                    //if (typeInfo.dynamic)
                    //{
                    //    type = (value == null ? null : value.GetType());
                    //    typeInfo = QTypeInfo.Get(type,true);
                    //    writer.Write(typeInfo.qTypeFile.GetTypeIndex(type.FullName));
                    //}
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
                            ForeachArray(array, 0, typeInfo.indexArray, (indexArray) => { writer.WriteValue(array.GetValue(indexArray), typeInfo.ArrayType); });
                        }
                        else
                        {
                            if (typeInfo.IsList)
                            {
                                var list = value as IList;
                                writer.Write(list.Count);
                                foreach (var item in list)
                                {
                                    writer.WriteValue(item, typeInfo.ListType);
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
