using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace QTool.Reflection
{
	#region 类型反射

	public class QMemeberInfo : IKey<string>
	{
		public string Key { get; set; }
		public string QName { get; set; }
		public Type Type { get; private set; }
		public Action<object, object> Set { get; private set; }
		public Func<object, object> Get { get; private set; }
		public Attribute Attribute { get; set; }
		public MemberInfo MemeberInfo { get; private set; }
		public bool IsPublic { get; private set; }
		public QMemeberInfo(FieldInfo info)
		{
			MemeberInfo = info;
			QName = info.QName();
			Key = info.Name;
			Type = info.FieldType;
			Set = info.SetValue;
			Get = info.GetValue;
			IsPublic = info.IsPublic;
		}
		public QMemeberInfo(PropertyInfo info)
		{
			MemeberInfo = info;
			QName = info.QName();
			Key = info.Name;
			Type = info.PropertyType;
			IsPublic = true;
			if (info.SetMethod != null)
			{
				Set =(obj,value)=> info.SetMethod.Invoke(obj,new object[] { value });
				if (!info.SetMethod.IsPublic)
				{
					IsPublic = false;
				}
			}
			else
			{
				IsPublic = false;
			}
			if (info.GetMethod != null)
			{
				Get =(obj)=> info.GetMethod.Invoke(obj,null);
				if (! info.GetMethod.IsPublic)
				{
					IsPublic = false;
				}
			}
			else
			{
				IsPublic = false;
			}
		}
		public override string ToString()
		{
			return "var " + Key + " \t\t(" + Type + ")";
		}
	}
	public class QFunctionInfo : IKey<string>
	{
		public string Key { get => Name; set => Name = value; }
		public string Name { get; private set; }
		public ParameterInfo[] ParamInfos { get; private set; }
		public Type ReturnType {
			get
			{
				return MethodInfo.ReturnType;
			}
		}
		public MethodInfo MethodInfo { get; private set; }
		public bool IsPublic => MethodInfo.IsPublic;
		public Attribute Attribute { get; set; }
		public object Invoke(object target, params object[] param)
		{
			if (ParamInfos.Length > param.Length)
			{
				var newParam = new object[ParamInfos.Length];
				for (int i = 0; i < param.Length; i++)
				{
					newParam[i] = param[i];
				}
				for (int i = param.Length; i < newParam.Length; i++)
				{
					if (ParamInfos[i].HasDefaultValue)
					{
						newParam[i] = ParamInfos[i].DefaultValue;
					}
				}
				param = newParam;
			}
			return MethodInfo?.Invoke(target, param);
		}
		public QFunctionInfo(MethodInfo info)
		{
			this.MethodInfo = info;
			Key = info.Name;
			ParamInfos = info.GetParameters();
		//	Function = info.Invoke;
		}
		public override string ToString()
		{
			return "function " + Name + "(" + ParamInfos.ToOneString(",") + ") \t\t(" + ReturnType + ")";
		}
	}
	public class QReflectionTypeInfo : QTypeInfo<QReflectionTypeInfo>
	{
	
	}
    public class QTypeInfo<T>:IKey<string> where T:QTypeInfo<T> ,new()
	{
		public static BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public|BindingFlags.NonPublic;
		public static BindingFlags FunctionFlags = BindingFlags.Instance | BindingFlags.Public|BindingFlags.NonPublic;
		public string Key { get;  set; }
        public QList<string, QMemeberInfo> Members = new QList<string, QMemeberInfo>();
        public QList<string, QFunctionInfo> Functions = new QList<string, QFunctionInfo>();
        public bool IsList;
		public bool IsDictionary;
		public Type KeyType { get; private set; }
		public Type ElementType { get; private set; }
		public Type KeyValueType { get; private set; }
        public Type Type { get; private set; }
        public TypeCode Code { get; private set; }
		public QMemeberInfo GetMemberInfo(string keyOrViewName)
		{
			var info = Members[keyOrViewName];
			if (info == null)
			{
				info= Members.Get(keyOrViewName, (obj) => obj.QName);
			}
			return info;
			
		}
        public bool IsArray {
            get
            {
                return Type.IsArray;
            }
        }
        public object Create(params object[] param)
        {
            return Activator.CreateInstance(Type, param);
        }
        public bool IsValueType
        {
            get
            {
                return Type.IsValueType;
            }
        }
        public int[] IndexArray { get; private set; }
        public int ArrayRank
        {
            get
            {
                if (IndexArray == null)
                {
                    return 0;
                }
                else
                {
                    return IndexArray.Length;
                }
            }
        }
        protected void CheckInit(Type type,Func<QMemeberInfo,bool> memeberCheck,Func<QFunctionInfo,bool> functionCheck)
        {
            Key = type.FullName;
            Type = type;
			ElementType = type;
			Code = Type.GetTypeCode(type);
            if (TypeCode.Object.Equals(Code))
            {
				if (type.IsArray)
                {
                    ElementType = type.GetElementType();
                    IndexArray = new int[type.GetArrayRank()];
                }
                else if (type.GetInterface(typeof(IList<>).FullName, true) != null)
                {
                    ElementType = type.GetInterface(typeof(IList<>).FullName, true).GenericTypeArguments[0];
                    IsList = true;
				}
				else if (type.GetInterface(typeof(IDictionary<,>).FullName, true) != null)
				{
					var arges = type.GetInterface(typeof(IDictionary<,>).FullName, true).GenericTypeArguments;
					KeyType = arges[0];
					ElementType = arges[1];
					KeyValueType = typeof(QKeyValue<,>).MakeGenericType(KeyType, ElementType);
					IsDictionary = true;
				}
				if (Members != null)
                {
                    QMemeberInfo memeber=null;
                    type.ForeachMemeber((info) =>
                    {
                        memeber = new QMemeberInfo(info);
                        if (memeberCheck == null || memeberCheck(memeber))
                        {
                            Members.Add(memeber);
                        }
                    },
                    (info) =>
                    {
                        memeber = new QMemeberInfo(info);
                        if (memeberCheck==null||memeberCheck(memeber))
                        {
                            Members.Add(memeber);
                        }
                    }, MemberFlags);
                }

                if (Functions != null)
                {
                    type.ForeachFunction((info) =>
                    {
                        var function = new QFunctionInfo(info);
                        if (functionCheck==null|| functionCheck(function))
                        {
                            Functions.Add(function);
                        }
                    }, FunctionFlags);
                }
            }
        }
        protected virtual void Init(Type type)
        {
            CheckInit(type,null,null);
        }
        static Type[] defaultCreatePrams = new Type[0];
        public static Dictionary<Type, T> table = new Dictionary<Type, T>();
        public static T Get(Type type)
        {
			lock (table)
			{
				if (!table.ContainsKey(type))
				{

					var info = new T();
					info.Init(type);
					table.Add(type, info);
				}
			}
            return table[type];
        }
        public override string ToString()
        {
            return "Type " + Key + " \n{\n\t" + Members.ToOneString("\n\t") + "\n\t" + Functions.ToOneString("\n\t") + "}";
        }
    }

    #endregion
    public static class QReflection
	{
		static Type GetOperaterType(object a,object b)
		{
			if (a == null)
			{
				return b?.GetType();
			}
			else if(b==null)
			{
				return a?.GetType();
			}
			else
			{
				return a?.GetType();
			}
		}
		public static object OperaterAdd(this object a, object b)
		{

			var type = GetOperaterType(a, b);


			if (type == typeof(string))
			{
				return a?.ToString() + b;
			}
			else
			{
				switch (type.Name)
				{
					case nameof(Vector2Int):
						{
							return (Vector2Int)a + (Vector2Int)b;
						}
					default:
						return a.ToComputeFloat() + b.ToComputeFloat();
				}
			}
		}
		public static object OperaterSubtract(this object a, object b)
		{
			var type = GetOperaterType(a, b);
			switch (type.Name)
			{
				case nameof(Vector2Int):
					{
						return (Vector2Int)a - (Vector2Int)b;
					}
				default:
					return a.ToComputeFloat() - b.ToComputeFloat();
			}
		}
		public static object OperaterMultiply(this object a, object b)
		{
			var type = GetOperaterType(a, b);
			switch (type.Name)
			{
				case nameof(Vector2Int):
					{
						return (Vector2Int)a * (Vector2Int)b;
					}
				default:
					return a.ToComputeFloat() * b.ToComputeFloat();
			}
		}
		public static object OperaterDivide(this object a, object b)
		{

			var type = GetOperaterType(a, b);
			switch (type.Name)
			{
				default:
					return a.ToComputeFloat() / b.ToComputeFloat();
			}
		}

		public static bool OperaterGreaterThan(this object a, object b)
		{

			var type = GetOperaterType(a, b);
			switch (type.Name)
			{
				default:
					return a.ToComputeFloat() > b.ToComputeFloat();
			}
		}
		public static bool OperaterLessThan(this object a, object b)
		{
			var type = GetOperaterType(a, b);
			switch (type.Name)
			{
				default:
					return a.ToComputeFloat() < b.ToComputeFloat();
			}
		}
		public static bool OperaterGreaterThanOrEqual(this object a, object b)
		{
			var type = GetOperaterType(a, b);
			switch (type.Name)
			{
				default:
					return a.ToComputeFloat() >= b.ToComputeFloat();
			}
		}
		public static bool OperaterLessThanOrEqual(this object a, object b)
		{
			var type = GetOperaterType(a, b);
			switch (type.Name)
			{
				default:
					return a.ToComputeFloat() <= b.ToComputeFloat();
			}
		}
		public static T ConvertTo<T>(this object obj)
		{
			return (T)ConvertToType(obj, typeof(T));
		}
		public static object ConvertToType(this object obj,Type type)
		{
			return Expression.Lambda<Func<object>>(Expression.Convert(Expression.Convert(Expression.Constant(obj), type),typeof(object))).Compile()();
		}
		public static bool OperaterEqual(this object a, object b)
		{
			
			return Equals(a, b)|| a.ToComputeFloat() == b.ToComputeFloat();
		}
		public static T GetAttribute<T>(this ICustomAttributeProvider info) where T :Attribute
        {
            var type = typeof(T);
            var array= info.GetCustomAttributes(typeof(T), true);
            return array.QueuePeek() as T;
        }
		public static string QTypeName(this Type type)
		{
			if (type.IsGenericType)
			{
				return type.FullName;
			}
			else
			{
				return type.Name;
			}
		}
        public static string QName(this MemberInfo type)
        {
            var att = type.GetCustomAttribute<QNameAttribute>();
            if (att != null && !string.IsNullOrWhiteSpace(att.name))
            {
                return att.name;
            }
            else
            {
                return type.Name;
            }
        }
        public static Type GetTrueType(this Type type)
        {
            if (type.Name.EndsWith("&"))
            {
                return type.GetElementType();
            }
            else if(typeof(Task).IsAssignableFrom(type))
            { 
                return type.GenericTypeArguments[0];
            }
            else if(typeof(Nullable).IsAssignableFrom(type))
            {
                return type.GenericTypeArguments[0];
            }
            return type;
        }
        public static string QName(this ParameterInfo info)
        {
            var att = info.GetCustomAttribute<QNameAttribute>();
            if (att != null && !string.IsNullOrWhiteSpace(att.name))
            {
                return att.name;
            }
            else
            {
                return info.Name;
            }
        }
        public static Assembly[] GetAllAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }
        public static MethodInfo GetStaticMethod(this Type type, string name)
		{
			if (name.SplitTowString(".", out var start, out var end))
			{
				return GetStaticMethod(QReflection.ParseType(start), end);
			}
			var tType = type;
			while (type.BaseType != null)
			{
				var funcInfo = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
				if (funcInfo != null)
				{
					return funcInfo;
				}
				else
				{
					type = type.BaseType;
				}
			}
			if (!name.StartsWith("get_"))
			{
				return GetStaticMethod(tType, "get_" + name);
			};
			return null;
        }
	
		public static object InvokeStaticFunction(this Type type,string name,params object[] param)
        {
			var method = GetStaticMethod(type, name);
			
			if (method != null)
			{
				return method.Invoke(null, param);
			}
			else
			{
				throw new Exception("不存在静态函数 " + type + " " + name);
			
			}
		}
		public static object InvokeFunction(this object obj, string funcName, params object[] param)
		{
			if (funcName.IsNullOrEmpty())
			{
				return null;
			}
#if UNITY_EDITOR
			if (obj is UnityEditor.SerializedProperty property)
			{
				obj = property.serializedObject.targetObject;
			}
#endif
			Type objType = obj.GetType();
			if(obj is Type)
			{
				objType = obj as Type;
			}
			var typeInfo = QReflectionTypeInfo.Get(objType);
			var method=typeInfo.Functions[funcName]?.MethodInfo;
			if (method == null)
			{
				method=typeInfo.Functions["get_"+funcName]?.MethodInfo;
			}
			if (method == null)
			{
				return objType.InvokeStaticFunction(funcName, param);
			}
			else
			{
				return method.Invoke( obj, param);
			}
		}
		static List<Type> typeList = new List<Type>();
		static QDictionary<Type, Type[]> AllTypesCache = new QDictionary<Type, Type[]>();
		public static Type[] GetAllTypes(this Type rootType)
        {
			if (!AllTypesCache.ContainsKey(rootType))
			{
				if (typeof(Attribute).IsAssignableFrom(rootType))
				{
					List<Type> typeList = new List<Type>();
					foreach (var ass in GetAllAssemblies())
					{
						typeList.AddRange(ass.GetTypes());
					}
					typeList.RemoveAll((type) =>
					{
						return type.GetCustomAttribute(rootType) == null;
					});
					AllTypesCache[rootType] = typeList.ToArray();
				}
				else
				{
					typeList.Clear();
					foreach (var ass in GetAllAssemblies())
					{
						typeList.AddRange(ass.GetTypes());
					}
					typeList.RemoveAll((type) =>
					{
						var baseType = type;
						while (baseType != null && !type.IsAbstract)
						{
							if (baseType.Name == rootType.Name)
							{
								return false;
							}
							else
							{
								baseType = baseType.BaseType;
							}
						}
						return true;
					});
					AllTypesCache[rootType] = typeList.ToArray();
				}
			}
			return AllTypesCache[rootType];
		}
		public static object CreateInstance(this Type type, object targetObj=null, params object[] param)
        {
			if (type.ContainsGenericParameters)
			{
				return targetObj;
			}
            if (targetObj != null)
            { 
				return targetObj;
			}
            try
            {
                if (type == typeof(string)||type==typeof(object))
                {
                    return ""; 
				}
				else if (param.Length == 0 && type.IsArray)
				{
					return CreateInstance(type, targetObj, 0);
				}
				else if(type==typeof(UnityEngine.Object))
				{
					return null;
				}
                return Activator.CreateInstance(type, param);
            }
            catch (Exception e)
            {
                throw new Exception("通过" + type + "(" + param.ToOneString(",") + ")创建对象" + type + "出错", e);
            }
        }
		public static bool Is(this Type type,Type checkType)
		{
			if (type == checkType)
			{
				return true;
			}
			else if (type.BaseType == null || type.BaseType.IsAbstract)
			{
				return false;
			}
			else if (type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				return type.GetGenericTypeDefinition().Is(checkType);
			}
			else
			{ 
				return type.BaseType.Is(checkType);
			}
		}
        static Dictionary<string, Type> TypeBuffer = new Dictionary<string, Type>();
        public static Type ParseType(string typeString)
        {
			if (typeString.IsNullOrEmpty()) return null;
            if (TypeBuffer.ContainsKey(typeString))
            {
                return TypeBuffer[typeString];
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
						if (!TypeBuffer.ContainsKey(typeString))
						{
							TypeBuffer.Add(typeString, type);
						}
                        return type; 
                    }
                    
                }
                for (int i = 0; i < assemblyArrayLength; ++i)
                {
                    foreach (var eType in assemblyArray[i].GetTypes())
                    {
                        if (eType.Name.Equals(typeString))
                        {
                            type = eType;
                            if (type != null)
                            {
								if (!TypeBuffer.ContainsKey(typeString))
								{
									TypeBuffer.Add(typeString, type);
								}
                                return type;
                            }
                        }
                    }
                }  

            }
			if (!TypeBuffer.ContainsKey(typeString))
			{ 
				TypeBuffer.Add(typeString, null);
			}
            if (typeString.Contains("System.Threading.Tasks.Task"))
            {
                return null;
            }
			return null;
        }
        public static void ForeachMemeber(this Type type, Action<FieldInfo> fieldInfo, Action<PropertyInfo> propertyInfo = null, BindingFlags bindingFlags= BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            var infos = type.GetProperties(bindingFlags);
            foreach (var item in infos)
            {
                propertyInfo?.Invoke(item);
            }
			FieldInfo[] fields = type.GetFields(bindingFlags);
			foreach (var item in fields)
			{
				if (item.Name.EndsWith("k__BackingField"))
				{
					continue;
				}
				fieldInfo?.Invoke(item);
			}
		}
        public static void ForeachFunction(this Type type, Action<MethodInfo> methodeInfo, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            var methods= type.GetMethods(bindingFlags);
            foreach (var method in methods)
            {
                methodeInfo?.Invoke(method);
            }
        }

		public static object GetValue(this object target, string path)
		{
			if (target == null) return null;
			if (path.SplitTowString(".", out var start, out var end))
			{
				try
				{
					return target.GetValue(start).GetValue(end);
				}
				catch (Exception e)
				{
					throw new Exception("路径出错：" + path, e);
				}
			}
			else
			{
				var typeInfo = QReflectionTypeInfo.Get(target.GetType());
				var memeberInfo = typeInfo.GetMemberInfo(path);
				if (memeberInfo!=null)
				{
					return memeberInfo.Get(target);
				} 
				else
				{
					Debug.LogError("["+target+"]("+target.GetType() + ") 找不到 key " + path+"\n"+ typeInfo.Members.ToOneString());
					return target;
				}
			}
		}
	}
}
