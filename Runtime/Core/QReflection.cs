using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using Unity.Jobs.LowLevel.Unsafe;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QTool.Reflection
{
	#region 类型反射

	public class QMemeberInfo : IKey<string>
	{
		public string Key { get; set; }
		public string QName { get;private set; }
		public string QOldName { get;private set; }
		public Type Type { get; private set; }
		public Action<object, object> Set { get; private set; }
		public Func<object, object> Get { get; private set; }
		public Attribute QNameAttribute { get; set; }
		public MemberInfo MemeberInfo { get; private set; }
		public bool IsPublic { get; private set; }
		public bool IsUnityObject { get; private set; }
		public QMemeberInfo(FieldInfo info)
		{
			MemeberInfo = info;
			QNameAttribute = info.GetAttribute<QNameAttribute>();
			QName = info.QName();
			QOldName = info.QOldName();
			Key = info.Name;
			Type = info.FieldType;
			Set = info.SetValue;
			Get = info.GetValue;
			IsPublic = info.IsPublic;
			IsUnityObject = Type.Is(typeof(UnityEngine.Object));
		}
		public QMemeberInfo(PropertyInfo info) {
			MemeberInfo = info;
			QNameAttribute = info.GetAttribute<QNameAttribute>();
			QName = info.QName();
			QOldName = info.QOldName();
			Key = info.Name;
			Type = info.PropertyType;
			IsPublic = true;
			IsUnityObject = Type.Is(typeof(UnityEngine.Object));
			var setMethod = info.GetSetMethod(true);
			if (setMethod != null) {
				Set = (obj, value) => setMethod.Invoke(obj, new object[] { value });
				if (!setMethod.IsPublic) {
					IsPublic = false;
				}
			}
			else {
				IsPublic = false;
			}
			var getMethod = info.GetGetMethod(true);
			if (getMethod != null) {
				Get = (obj) => getMethod.Invoke(obj, null);
				if (!getMethod.IsPublic) {
					IsPublic = false;
				}
			}
			else {
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
		public string Key { get ; set ; }
		public string QName { get;private set; }
		public string NameWithParams { get; private set; }
		public ParameterInfo[] ParamInfos { get; private set; }
		public Type[] ParamTypes { get; private set; }
		public Type ReturnType
		{
			get
			{
				return MethodInfo.ReturnType;
			}
		}
		public MethodInfo MethodInfo { get; private set; }
		public bool IsPublic => MethodInfo.IsPublic;
		public object Invoke(object target, params object[] param)
		{
			var MethodInfo = this.MethodInfo;
			if (MethodInfo.ContainsGenericParameters) {
				var types = MethodInfo.GetGenericArguments();
				var paramArray=MethodInfo.GetParameters();
				for (int i = 0; i < types.Length; i++) {
					var type = types[i];
					for (int j = 0; j < paramArray.Length; j++) {
						if (paramArray[j].ParameterType.ToString().TrimEnd('&')==type.ToString()) {
							types[i] = param[j].GetType();
							break;
						}
					}
				}
				MethodInfo = MethodInfo.MakeGenericMethod(types);
			}
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
			Key = info.GetNameWithParams();
			MethodInfo = info;
			QName = info.QName();
			ParamInfos = info.GetParameters();
			ParamTypes = new Type[ParamInfos.Length];
			for (int i = 0; i < ParamInfos.Length; i++)
			{
				ParamTypes[i] = ParamInfos[i].ParameterType;
			}
		}
		public override string ToString()
		{
			return "function " + QName + "(" + ParamInfos.ToOneString(",") + ") \t\t(" + ReturnType + ")";
		}
	}
	public class QReflectionType : QTypeInfo<QReflectionType>
	{
		protected override void Init(Type type)
		{
			MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			FunctionFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			base.Init(type);
		}
	}
	public class QTypeInfo {


		public QList<string, QMemeberInfo> Members = new QList<string, QMemeberInfo>();
		public QList<string, QFunctionInfo> Functions = new QList<string, QFunctionInfo>();
		public QDictionary<string, QDictionary<int, List<QFunctionInfo>>> FunctionsCache = new QDictionary<string, QDictionary<int, List<QFunctionInfo>>>(key => new QDictionary<int, List<QFunctionInfo>>(key => new List<QFunctionInfo>()));
		public bool IsList { get; protected set; } = false;
		public bool IsDictionary { get; protected set; } = false;
		public Type KeyType { get; protected set; }
		public Type ElementType { get; protected set; }
		public virtual Type Type { get; protected set; }
		public TypeCode Code { get; protected set; }
		public QMemeberInfo GetMemberInfo(string keyOrViewName) {
			var info = Members[keyOrViewName];
			if (info == null) {
				info = Members.FirstOrDefault(obj => Equals(keyOrViewName, obj.QName));
			}
			if (info == null) {
				info = Members.FirstOrDefault(obj => Equals(keyOrViewName, obj.QOldName));
			}
			return info;
		}
		public bool IsArray {
			get {
				return Type.IsArray;
			}
		}
		public object Create(params object[] param) {
			return Activator.CreateInstance(Type, param);
		}
		public bool IsValueType {
			get {
				return Type.IsValueType;
			}
		}
		public int[] IndexArray { get; protected set; }
		public int ArrayRank {
			get {
				if (IndexArray == null) {
					return 0;
				}
				else {
					return IndexArray.Length;
				}
			}
		}
		public static Dictionary<Type, ICustomTypeInfo> CustomTypeInfos = new();
		static QTypeInfo() {
			foreach (Type type in typeof(ICustomTypeInfo).GetAllTypes()) {
				var obj = type.CreateInstance() as ICustomTypeInfo;
				CustomTypeInfos.Add(obj.Type, obj);
			}
		}
		public interface ICustomTypeInfo {
			public Type Type { get; }
			public Type TargetType { get; }
			public object ChangeType(object obj);
			public string[] Members { get; }
		}
		public abstract class CustomTypeInfo<TType> : ICustomTypeInfo {
			public Type Type => typeof(TType);
			public virtual Type TargetType { get; }
			public abstract string[] Members { get; }
			public virtual object ChangeType(object obj) {
				return obj;
			}
		}
		public abstract class CustomTypeInfo<TType,TTarget> : ICustomTypeInfo {
			public Type Type => typeof(TType);
			public Type TargetType => typeof(TTarget);
			public virtual string[] Members { get; }
			public abstract TTarget ChangeType(TType obj);
			public abstract TType ChangeType(TTarget obj);
			public object ChangeType(object obj) {
				if(obj is TType type) {
					return ChangeType(type);
				}
				else if(obj is TTarget target) {
					return ChangeType(target);
				}
				else {
					return obj;
				}
			}
		}
		public class RectMemebers : CustomTypeInfo<Rect> {
			public override string[] Members => new string[] {
					nameof(Rect.x),
					 nameof(Rect.y),
					 nameof(Rect.height),
					 nameof(Rect.width)
				};
		}
		public class QuaternionMemebers : CustomTypeInfo<Quaternion> {
			public override string[] Members => new string[] {
					nameof(Quaternion.x),
					 nameof(Quaternion.y),
					 nameof(Quaternion.z),
					 nameof(Quaternion.w)
				};
		}
		public class TransformMemebers : CustomTypeInfo<Transform> {
			public override string[] Members => new string[] {
					 nameof(Transform.localPosition),
					 nameof(Transform.localRotation),
					 nameof(Transform.localScale),
				};
		}
		public class CameraMemebers : CustomTypeInfo<Camera> {
			public override string[] Members => new string[] {
					nameof(Camera.aspect),
					nameof(Camera.rect),
				};
		}
	}

	
	public class QTypeInfo<T> : QTypeInfo where T : QTypeInfo<T>, new() {
		public static BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		public static BindingFlags FunctionFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		public static Dictionary<Type, T> table = new Dictionary<Type, T>();
		public ICustomTypeInfo CustomTypeInfo { get; private set; }
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
		protected virtual void Init(Type type) {
			Type = type;
			ElementType = type;
			Code = Type.GetTypeCode(type);
			if (TypeCode.Object.Equals(Code)) {
				if (type.IsArray) {
					ElementType = type.GetElementType();
					IndexArray = new int[type.GetArrayRank()];
				}
				else if (type.GetInterface(typeof(IList<>).FullName, true) != null) {
					ElementType = type.GetInterface(typeof(IList<>).FullName, true).GenericTypeArguments[0];
					IsList = true;
				}
				else if (type.GetInterface(typeof(IDictionary<,>).FullName, true) != null) {
					var arges = type.GetInterface(typeof(IDictionary<,>).FullName, true).GenericTypeArguments;
					KeyType = arges[0];
					ElementType = arges[1];
					//KeyValueType = typeof(QKeyValue<,>).MakeGenericType(KeyType, ElementType);
					IsDictionary = true;
				}
				if (Members != null) {
					QMemeberInfo memeber = null;
					type.ForeachMemeber((info) => {
						memeber = new QMemeberInfo(info);
						Members.Add(memeber);
					},
					(info) => {
						memeber = new QMemeberInfo(info);
						Members.Add(memeber);
					}, MemberFlags);
					if (CustomTypeInfos.ContainsKey(type)) {
						CustomTypeInfo = CustomTypeInfos[type];
						Members.RemoveAll(member => !CustomTypeInfo.Members.Contains(member.Key));
					}
				}
				if (Functions != null) {
					type.ForeachFunction((info) => {
						var function = new QFunctionInfo(info);
						Functions.Add(function);
						FunctionsCache[function.MethodInfo.Name][function.ParamInfos.Length].Add(function);
					}, FunctionFlags);
				}
			}
		}
	}

	#endregion

	//public class QPropertyIWrapper<TTarget, TValue>
	//{
	//	private Action<TTarget, TValue> setter;
	//	private Action<TTarget, TValue> getter;
	//	public QPropertyIWrapper(QMemeberInfo memeberInfo)
	//	{
	//		if(memeberInfo.getm)
	//		var m = propertyInfo.GetSetMethod(true);
	//		_setter = (Action<TTarget, TValue>)Delegate.CreateDelegate(typeof(Action<TTarget, TValue>), null, m);
	//	}

	//	public void SetValue(TTarget target, TValue val)
	//	{
	//		_setter(target, val);
	//	}
	//}
	public static class QReflection
	{
		static Type GetOperaterType(object a, object b)
		{
			Type type = a != null ? a.GetType() : b?.GetType();
			if (type == null)
			{
				return typeof(object);
			}
			else
			{
				return type;
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
		private static Dictionary<Type, string> _typeFriendlyName = new();
		public static string GetFriendlyName(this Type t) {

			if (t == null) {
				return null;
			}

			if (t.IsByRef) {
				t = t.GetElementType();
			}
			if (t == typeof(UnityEngine.Object)) {
				return "UnityObject";
			}

			string s;
			if (_typeFriendlyName.TryGetValue(t, out s)) {
				return s;
			}


			s = t.QName();
			if (s == "Single") { s = "Float"; }
			if (s == "Single[]") { s = "Float[]"; }
			if (s == "Int32") { s = "Integer"; }
			if (s == "Int32[]") { s = "Integer[]"; }

			if (t.IsGenericParameter) {
				s = "T";
			}

			if (t.IsGenericType) {
				s = t.QName();
				var args = t.GetGenericArguments();
				if (args.Length != 0) {

					s = s.Replace("`" + args.Length.ToString(), "");

					s += "<";
					for (var i = 0; i < args.Length; i++) {
						s += (i == 0 ? "" : ", ") + args[i].GetFriendlyName();
					}
					s += ">";
				}
			}
			return _typeFriendlyName[t] = s;
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
		public static bool OperaterEqual(this object a, object b)
		{

			return Equals(a, b) || a.ToComputeFloat() == b.ToComputeFloat();
		}
		public static T GetAttribute<T>(this ICustomAttributeProvider info) where T : Attribute
		{
			var array = info.GetCustomAttributes(typeof(T), true);
			return array.QueuePeek() as T;
		}
		private static QDictionary<Type, int> MinSizeCache = new QDictionary<Type, int>();
		public static int MinSize(this Type type)
		{
			if (MinSizeCache.ContainsKey(type)) return MinSizeCache[type];
			var info = QReflectionType.Get(type);
			var size = 0;
			switch (info.Code)
			{
				case TypeCode.Boolean:
					size = sizeof(bool); break;
				case TypeCode.Byte:
					size = sizeof(byte); break;
				case TypeCode.Char:
					size = sizeof(char); break;
				case TypeCode.DateTime:
					size = sizeof(long); break;
				case TypeCode.DBNull:
					size = 0; break;
				case TypeCode.Decimal:
					size = sizeof(decimal); break;
				case TypeCode.Double:
					size = sizeof(double); break;
				case TypeCode.Empty:
					break;
				case TypeCode.Int16:
					size = sizeof(ushort); break;
				case TypeCode.Int32:
					size = sizeof(int); break;
				case TypeCode.Int64:
					size = sizeof(long); break;
				case TypeCode.Object:
					{
						foreach (var member in info.Members)
						{
							if (Type.GetTypeCode(member.Type) != TypeCode.Object)
							{
								size += member.Type.MinSize();
							}
							else
							{
								size += 12;
							}
						}
						break;
					}
				case TypeCode.SByte:
					size = sizeof(sbyte); break;
				case TypeCode.Single:
					size = sizeof(float); break;
				case TypeCode.String:
					break;
				case TypeCode.UInt16:
					size = sizeof(ushort); break;
				case TypeCode.UInt32:
					size = sizeof(uint); break;
				case TypeCode.UInt64:
					size = sizeof(ulong); break;
				default:
					break;
			}
			MinSizeCache[type] = size;
			return size;
		}

		public static string QName(this MemberInfo member)
		{
			var att = member.GetCustomAttribute<QNameAttribute>();
			if (att != null && !string.IsNullOrWhiteSpace(att.name))
			{
				return att.name;
			}
			else
			{
				return member.Name;
			}
		}
		public static string QOldName(this MemberInfo method)
		{
			var att = method.GetCustomAttribute<QOldNameAttribute>();
			if (att != null && !string.IsNullOrWhiteSpace(att.name))
			{
				return att.name;
			}
			else
			{
				return method.Name;
			}
		}
		public static string GetNameWithParams(this MethodInfo method)
		{
			var nameStart = method.Name;
			var Params = method.GetParameters();
			if (Params.Length > 0)
			{
				nameStart += "_" + Params.ToOneString("_", item => item.ParameterType.Name);
			}
			return nameStart;
		}
		public static Type GetTrueType(this Type type)
		{
			if (type.Name.EndsWith("&"))
			{
				return type.GetElementType();
			}
			else if (typeof(Task).IsAssignableFrom(type))
			{
				return type.GenericTypeArguments[0];
			}
			else if (typeof(Nullable).IsAssignableFrom(type))
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

		public static object GetPathObject(this object target, string path)
		{
			if (path.SplitTowString(".", out var start, out var end))
			{
				try
				{
					if (start == "Array" && end.StartsWith("data"))
					{
						var list = target as IList;
						if (list == null)
						{
							return null;
						}
						else
						{
							return list[int.Parse(end.GetBlockValue('[', ']'))];
						}
					}
					else
					{

						return target.GetPathObject(start).GetPathObject(end);
					}

				}
				catch (Exception e)
				{
					Debug.LogError("路径出错：" + path + ":" + e);
				}
			}
			else
			{
				var memebers = QReflectionType.Get(target.GetType()).Members;
				if (memebers.ContainsKey(path))
				{
					var Get = memebers[path].Get;
					return Get(target);
				}
				else
				{
					Debug.LogError(" 找不到 key " + path);
				}
			}
			return null;
		}
		public static object SetPathObject(this object target, string path, object value)
		{
			if (path.SplitTowString(".", out var start, out var end))
			{
				try
				{
					if (start == "Array" && end.StartsWith("data"))
					{
						var list = target as IList;
						if (list == null)
						{
							return null;
						}
						else
						{
							return list[int.Parse(end.GetBlockValue('[', ']'))];
						}
					}
					else
					{

						return target.GetPathObject(start).SetPathObject(end, value);
					}

				}
				catch (Exception e)
				{
					throw new Exception("路径出错：" + path, e);
				}
			}
			else
			{
				var memebers = QReflectionType.Get(target.GetType()).Members;
				if (memebers.ContainsKey(path) && memebers[path].Set != null)
				{
					memebers[path].Set(target, value);
					return value;
				}
				else
				{
					throw new Exception(" 找不到 key " + path);
				}
			}
		}
		public static bool GetPathBool(this object target, string key)
		{
			var not = key.Contains("!");
			if (key.SplitTowString("==", out var start, out var value) || key.SplitTowString("!=", out start, out value))
			{
				not = false;
				var info = target.GetPathObject(start)?.ToString() == value;
				return not ? !(bool)info : (bool)info;
			}
			else
			{
				if (not)
				{
					key = key.TrimStart('!');
				}
				object info = null;
				switch (key)
				{
					case nameof(Application.isPlaying):
						info = Application.isPlaying;
						break;
					default:
						info = target.GetPathObject(key);
						break;
				}
				if (info == null)
				{
					return !not;
				}
				else
				{
					return not ? !(bool)info : (bool)info;
				}

			}

		}

		public static FieldInfo GetChildObject(Type type, string key)
		{
			if (type == null || string.IsNullOrWhiteSpace(key)) return null;
			const BindingFlags bindingFlags = System.Reflection.BindingFlags.GetField
											  | System.Reflection.BindingFlags.GetProperty
											  | System.Reflection.BindingFlags.Instance
											  | System.Reflection.BindingFlags.NonPublic
											  | System.Reflection.BindingFlags.Public;
			return type.GetField(key, bindingFlags);
		}
#if UNITY_EDITOR
		public static object[] GetAttributes<T>(this SerializedProperty prop, string parentKey)
		{
			var type = string.IsNullOrWhiteSpace(parentKey) ? prop.serializedObject.targetObject?.GetType() : QReflection.ParseType(parentKey);
			var field = GetChildObject(type, prop.name);
			if (field != null)
			{
				return field.GetCustomAttributes(typeof(T), true);
			}
			return new object[0];
		}
		public static T GetAttribute<T>(this SerializedProperty property, string parentKey = "") where T : Attribute
		{
			object[] attributes = GetAttributes<T>(property, parentKey);
			if (attributes.Length > 0)
			{
				return attributes[0] as T;
			}
			else
			{
				return null;
			}
		}

		public static string QName(this SerializedProperty property, string parentName = "")
		{
			var att = property.GetAttribute<QNameAttribute>(parentName);
			if (att != null && !string.IsNullOrWhiteSpace(att.name))
			{
				return att.name;
			}
			else
			{
				return property.displayName;
			}
		}


		public static object GetObject(this SerializedProperty property)
		{
			return property?.serializedObject.targetObject.GetPathObject(property.propertyPath);
		}
		public static object SetObject(this SerializedProperty property, object value)
		{
			return property?.serializedObject.targetObject.SetPathObject(property.propertyPath, value);
		}
		public static bool IsShow(this SerializedProperty property)
		{
			var att = property.GetAttribute<QNameAttribute>();
			if (att == null)
			{
				return true;
			}
			else
			{
				return att.Active(property.serializedObject.targetObject);
			}
		}
		public static void AddObject(this List<GUIContent> list, object obj)
		{
			if (obj != null)
			{
				if (obj is UnityEngine.GameObject)
				{
					var uObj = obj as UnityEngine.GameObject;
					var texture = AssetPreview.GetAssetPreview(uObj);
					list.Add(new GUIContent(uObj.name, texture, uObj.name));
				}
				else
				{
					list.Add(new GUIContent(obj.ToString()));
				}
			}
			else
			{
				list.Add(new GUIContent("空"));
			}
		}
#endif
		public static Assembly[] GetAllAssemblies()
		{
			return AppDomain.CurrentDomain.GetAssemblies();
		}
		private static QDictionary<string, MethodInfo> staticMethodCahce = new();
		public static MethodInfo GetStaticMethod(this Type type, string name)
		{
			if (name.SplitTowString(".", out var start, out var end))
			{
				return GetStaticMethod(ParseType(start), end);
			}
			if (staticMethodCahce.ContainsKey(name)) {
				return staticMethodCahce[$"{type.Name}.{name}"];
			}
			var tType = type;
			while (type.BaseType != null)
			{
				var funcInfo = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
				if (funcInfo != null)
				{
					staticMethodCahce[$"{type.Name}.{name}"] = funcInfo;
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

		public static object InvokeStaticFunction(this Type type, string name, params object[] param)
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
			if (funcName.IsNull())
			{
				return null;
			}
#if UNITY_EDITOR
			if (obj is SerializedProperty property)
			{
				obj = property.serializedObject.targetObject;
			}
#endif
			Type objType = obj == null ? typeof(object) : obj?.GetType();
			if (obj is Type)
			{
				objType = obj as Type;
			}
			var typeInfo = QReflectionType.Get(objType);
			if (typeInfo.FunctionsCache[funcName].ContainsKey(param.Length))
			{
				var methods = typeInfo.FunctionsCache[funcName][param.Length];
				if (methods.Count > 1)
				{
					foreach (var method in methods)
					{
						var match = true;
						for (int i = 0; i < param.Length; i++)
						{
							if (!method.ParamTypes[i].ContainsGenericParameters && param[i] != null && !param[i].GetType().Is(method.ParamTypes[i]))
							{
								match = false;
								break;
							}
						}
						if (match)
						{
							return method.Invoke(obj, param);
						}
					}
				}
				else
				{
					return methods[0].Invoke(obj, param);
				}
			}
			return objType.InvokeStaticFunction(funcName, param);
		}
		static List<Type> typeList = new List<Type>();
		static QDictionary<Type, List<Type>> AllTypesCache = new() {
			{typeof(object),new(){typeof(int),typeof(float),typeof(string),typeof(bool),typeof(Vector3),typeof(Vector2),typeof(Transform),typeof(UnityEngine.Object) } }
		};
		public static List<Type> GetAllTypes(this Type rootType,bool checkGeneric=true)
		{
			if (!AllTypesCache.ContainsKey(rootType))
			{
				typeList.Clear();
				foreach (var ass in GetAllAssemblies())
				{
					typeList.AddRange(ass.GetTypes());
				}
				if (typeof(Attribute).IsAssignableFrom(rootType))
				{
					typeList.RemoveAll(type => type.GetCustomAttribute(rootType) == null);
				}
				else
				{
					typeList.RemoveAll(type => !type.Is(rootType));

				}
				if (checkGeneric) {
					var count = typeList.Count;
					for (int i = 0; i < count; i++) {
						var type = typeList[i];
						if (type.IsGenericTypeDefinition && type.GetGenericArguments().Length == 1) {
							typeList.AddRange(type.GetAllGenericTypes());
						}
					}
				}
				typeList.RemoveAll(type => (type.IsAbstract && !type.IsSealed) || type.IsInterface || (checkGeneric && type.ContainsGenericParameters));
				typeList.Remove(rootType);
				AllTypesCache[rootType] = new List<Type>(typeList);
			}
			return AllTypesCache[rootType];
		}
		static List<Type> genericTypeList = new List<Type>();
		static QDictionary<Type, List<Type>> AllGenericTypesCache = new() {
			{typeof(object),new(){typeof(int),typeof(float),typeof(string),typeof(bool),typeof(UnityEngine.Object) } }
		};
		public static List<Type> GetAllGenericTypes(this Type rootType) {
			if (!AllGenericTypesCache.ContainsKey(rootType)) {
				genericTypeList.Clear();
				if (rootType.GetGenericArguments().Length == 1) {
					foreach (var type in GetAllTypes(rootType.GetGenericArguments()[0]?.BaseType,false)) {
						try {
							genericTypeList.Add(rootType.MakeGenericType(type));
						}
						catch (Exception e) {
							Debug.LogError($"创建泛型类出错{rootType} : {type}");
							Debug.LogException(e);
						}
					}
				}
				AllGenericTypesCache[rootType] = new List<Type>(genericTypeList);
			}
			return AllGenericTypesCache[rootType];
		}
		public static object CreateInstance(this Type type, object targetObj = null, params object[] param) {
			if (targetObj != null || type == null) {
				return targetObj;
			}
			try {
				if (type.ContainsGenericParameters) {
					return targetObj;
				}
				if (type == typeof(string) || type == typeof(object)) {
					return "";
				}
				else if (param.Length == 0 && type.IsArray) {
					return CreateInstance(type, targetObj, 0);
				}
				else if (type.Is(typeof(UnityEngine.Object))) {
					return null;
				}
				else if (type.IsInterface || type.IsAbstract) {
					foreach (var item in GetAllTypes(type)) {
						if (!item.Is(typeof(UnityEngine.Object))) {
							return CreateInstance(item, targetObj, param);
						}
					}
					return null;
				}
				return Activator.CreateInstance(type, param);
			}
			catch (Exception e) {
				Debug.LogException(e);
				return null;
			}
		}
		public static bool Is(this Type type, Type checkType)
		{
			if (type == null) return false;
			if (checkType.IsAssignableFrom(type)) return true;
			if (checkType.IsGenericTypeDefinition)
			{
				if (type.IsGenericType && !type.IsGenericTypeDefinition)
				{
					if (type.GetGenericTypeDefinition().Is(checkType))
					{
						return true;
					}
				}
				else if (type.BaseType != null)
				{
					return type.BaseType.Is(checkType);
				}
			}
			return false;
		}
		static Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
		public static Type ParseType(this string typeString, Type baseType = null) {
			if (typeString.IsNull())
				return null;
			if (_typeCache.ContainsKey(typeString)) {
				return _typeCache[typeString];
			}
			else {

				var type = Type.GetType(typeString);
				if (type == null) {
					Assembly[] assemblyArray = AppDomain.CurrentDomain.GetAssemblies();
					int assemblyArrayLength = assemblyArray.Length;
					for (int i = 0; i < assemblyArrayLength; ++i) {
						type = assemblyArray[i].GetType(typeString);
						if (type != null) {
							if (!_typeCache.ContainsKey(typeString)) {
								if (baseType != null && !type.Is(baseType))
									continue;
								_typeCache.Add(typeString, type);
							}
							return type;
						}

					}
					for (int i = 0; i < assemblyArrayLength; ++i) {
						try {
							foreach (var eType in assemblyArray[i].GetTypes()) {
								if (eType.FullName.Replace($".{eType.Name}", $".{eType.QOldName()}") == typeString || eType.Name == typeString) {
									if (baseType != null && !eType.Is(baseType)) {
										continue;
									}
									type = eType;
									if (!_typeCache.ContainsKey(typeString)) {
										_typeCache.Add(typeString, type);
									}
									return type;
								}
							}
						}
						catch (Exception e) {
							throw new Exception("[" + assemblyArray[i].FullName + "]", e);
						}

					}
				}
				if (typeString.Contains("System.Threading.Tasks.Task")) {
					_typeCache.Add(typeString, null);
				}
				if (type == null) {
					Debug.LogWarning($"找不到类型{typeString} parent {baseType}");
				}
				return type;
			}
		}
		public static void ForeachMemeber(this Type type, Action<FieldInfo> fieldInfo, Action<PropertyInfo> propertyInfo = null, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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
			var methods = type.GetMethods(bindingFlags);
			foreach (var method in methods)
			{
				methodeInfo?.Invoke(method);
			}
		}

		public static object GetValue(object target, string path) {
			if (target == null)
				return null;
			if (path.SplitTowString(".", out var start, out var end)) {
				try {
					return GetValue(GetValue(target, start), end);
				}
				catch (Exception e) {
					throw new Exception("路径出错：" + path, e);
				}
			}
			else {
				var typeInfo = QReflectionType.Get(target.GetType());
				var memeberInfo = typeInfo.GetMemberInfo(path);
				if (memeberInfo != null) {
					return memeberInfo.Get(target);
				}
				else {
					QDebug.LogError("[" + target + "](" + target.GetType() + ") 找不到 key " + path + "\n" + typeInfo.Members.ToOneString());
					return target;
				}
			}
		}
	}
}
