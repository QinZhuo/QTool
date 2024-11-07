#if NODECANVAS
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
#endif
using QTool.Graph;
using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.UIElements;

namespace QTool {
	public interface INodeUpdate {
		public void OnUpdate();
	}
	public static class QCommand
	{
		public static Type[] ReflectedTypes=new Type[] {typeof(Debug),typeof(Transform)};
		static QCommand()
		{
			QDebug.Begin(nameof(QNodeInfo) + "初始化");
			//FreshCommands(typeof(QCommandType).GetAllTypes());
			foreach (var item in typeof(QNodeRuntime).GetAllTypes(false)) {
				//if (item.IsGenericTypeDefinition) {
				//	Debug.LogError($" {nameof(NodeBase)} 不支持泛型类 {item?.FullName} ");
				//	continue;
				//}
				AddCommand(new QNodeInfo(item));
			}
			var typeInfo= typeof(Debug).GetTypeInfo();
			typeInfo.ForeachFunction(method => {
				AddCommand(new QNodeInfo(method));
			}, BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance);
			QDebug.End(nameof(QNodeInfo) + "初始化", " 命令数 " + KeyDictionary.Count);
		}
		
		public static QDictionary<string, QNodeInfo> KeyDictionary { get; private set; } = new();
        public static QDictionary<string, QNodeInfo> NameDictionary { get; private set; } = new QDictionary<string, QNodeInfo>();
		// public static List<Type> TypeList = new List<Type>();
		private static QDictionary<Type, MethodInfo[]> typeMethodsCache = new(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));
		private static QDictionary<MethodInfo, ParameterInfo[]> methodParamsCache = new(method => method.GetParameters());
		public static QNodeInfo GetCommand(string key) {
			if (key == null) {
				return null;
			}
			if (KeyDictionary.ContainsKey(key)) {
				return KeyDictionary[key];
			}
			if (NameDictionary.ContainsKey(key)) {
				return NameDictionary[key];
			}
			else {
				if (key.Contains('|')) {
					var keys = key.Split('|');
					var type = QReflection.ParseType(keys[0]);
					if (type != null) {
						foreach (var method in typeMethodsCache[type]) {
							if (method.Name == keys[1] && method.ReturnType.FullName == keys[2]) {
								var Params = methodParamsCache[method];
								if (Params.Length + 3 == keys.Length) {
									bool paramCheck = true;
									for (int i = 0; i < Params.Length; i++) {
										if (!Params[i].ParameterType.FullName.Equals(keys[i + 3])) {
											paramCheck = false;
											break;
										}
									}
									if (paramCheck) {
										var cmd = new QNodeInfo(method);
										AddCommand(cmd);
										return cmd;
									}
								}
							}
						}
					}
					Debug.LogError($"找不到命令[{key}]");
					KeyDictionary[key] = null;
					return null;
				}
				else {
					var type = QReflection.ParseType(key);
					if (type != null) {
						var cmd = new QNodeInfo(type);
						AddCommand(cmd);
						return cmd;
					}
					Debug.LogError($"找不到命令[{key}]");
					KeyDictionary[key] = null;
					return null;
				}
			}
		}
		public static void AddCommand(QNodeInfo commandInfo) {
			KeyDictionary[commandInfo.Key] = commandInfo;
			if (commandInfo.Key.Contains(".")) {
				NameDictionary[commandInfo.Key.SplitEndString(".")] = commandInfo;
			}
			if (commandInfo.Key.Contains("/")) {
				NameDictionary[commandInfo.Key.SplitEndString("/")] = commandInfo;
			}
			if (commandInfo.ViewName.Contains("/")) {
				NameDictionary[commandInfo.ViewName.SplitEndString("/")] = commandInfo;
			}
		}
      

    }
	public class QNodeInfo : IKey<string> {
		public string Key { set; get; }
		public string ViewName { get; private set; }
		public string FullName { get; private set; }
		public Type type { get; private set; }
		public MethodInfo method { get; private set; }
		public List<QPortInfo> Ports { get; private set; } = new();
		public int ParamCount { get; private set; }
		private object DefualtObj { get; set; }
		public QNodeInfo(Type type) {
			Key = type.FullName;
			ViewName = type.GetFriendlyName();
			var category = type.GetCustomAttribute<CategoryAttribute>()?.category;
			FullName = category.IsNull() ? ViewName : $"{category}/{ViewName}";
			this.type = type;
			var typeInfo = QSerializeType.Get(type);
			if (type.IsGenericTypeDefinition)
				return;
			DefualtObj = Activator.CreateInstance(type);
			if (type.Is(typeof(FunctionNode))) {
				var Invoke = type.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
				if (Invoke != null) {
					Ports.Add(new QPortInfo(Invoke, true, typeInfo));
					var ps = Invoke.GetParameters();
					for (int i = 0; i < ps.Length; i++) {
						Ports.Add(new QPortInfo(ps[i], i, typeInfo));
					}
				}
			}
			else {
				foreach (var member in typeInfo.Members) {
					if (member.MemeberInfo is FieldInfo) {
						Ports.Add(new QPortInfo(member, DefualtObj));
					}
				}
			}
		}
		public QNodeInfo(MethodInfo method) {
			Key = $"{method.DeclaringType.FullName}|{method.Name}|{method.ReturnType.FullName}|{method.GetParameters().ToOneString("|", p => p.ParameterType.FullName)}";
			ViewName = QReflection.QName(method);
			FullName = $"反射/{method.DeclaringType.GetFriendlyName()}/{ViewName}";
			type = method.DeclaringType;
			this.method = method;
			if (method.ReturnType != typeof(void)) {
				Ports.Add(new QPortInfo(method, true));
			}
			if (!method.IsStatic) {
				Ports.Add(new QPortInfo(method,false));
			}
			var ps = method.GetParameters();
			for (int i = 0; i < ps.Length; i++) {
				Ports.Add(new QPortInfo(ps[i], i));
			}
			ParamCount = ps.Length;
			//if (method.ReturnType == typeof(void) || method.ReturnType == typeof(Task) || method.ReturnType == typeof(IEnumerator))
			//{
			//	IsStringCommond = true;
			//}
			//         foreach (var paramInfo in Params)
			//         {
			//	if (IsStringCommond)
			//	{
			//		if ((!paramInfo.HasDefaultValue&& !paramInfo.ParameterType.IsValueType&& paramInfo.ParameterType!=typeof(string) && paramInfo.ParameterType != typeof(object)) || paramInfo.IsOut)
			//		{
			//			IsStringCommond = false;
			//		}
			//	}
			//}
		}
		public override string ToString() {
			return ViewName + " " + Ports.ToOneString(" ");
		}
	}
	public class QPortInfo : IKey<string> {
		public string Key { get; set; }
		public string ViewName { get; private set; }
		internal object TempValue;
		public object DefaultValue { get; private set; }
		public bool HasDefaultValue { get; private set; }
		public Type ParameterType { get; private set; }
		public QInputPortAttribute InputPort { get; private set; }
		public QOutputPortAttribute OutputPort { get; private set; }
		public ICustomAttributeProvider attributeProvider { get; private set; }
		public int ParamIndex { get; private set; } = -1;
		public Action<object,object> SetValue { get; private set; }
		public Func<object,object> GetValue { get; private set; }

		public QPortInfo(MethodInfo method, bool isResult, QSerializeType FunctionNodeType = null) {
			Key = isResult ? QFlowKey.ReturnPort : nameof(ReflectedMethodNode.obj);
			ViewName = isResult ? QFlowKey.ReturnPort : QReflection.QName(method.DeclaringType);
			HasDefaultValue = false;
			DefaultValue = null;
			ParameterType = isResult ? method.ReturnType : method.DeclaringType;
			if (isResult) {
				OutputPort = QOutputPortAttribute.Normal;
			}
			attributeProvider = isResult ? method.ReturnType : method.DeclaringType;
			if (FunctionNodeType != null) {
				var member = FunctionNodeType.GetMemberInfo(Key);
				if (member != null) {
					SetValue = FunctionNodeType.GetMemberInfo(Key).Set;
					GetValue = FunctionNodeType.GetMemberInfo(Key).Get;
				}
				else {

					Debug.LogError(FunctionNodeType.Type + " 找不到 " + Key);
				}
			}
		}
		public QPortInfo(ParameterInfo parameterInfo, int index, QSerializeType FunctionNodeType = null) {
			Key = FunctionNodeType != null ? $"p{index + 1}" : parameterInfo.Name;
			ViewName = QReflection.QName(parameterInfo);
			HasDefaultValue = parameterInfo.HasDefaultValue;
			DefaultValue = parameterInfo.DefaultValue;
			ParameterType = parameterInfo.ParameterType;
			if (parameterInfo.IsOut) {
				OutputPort = QOutputPortAttribute.Normal;
			}
			else {
				InputPort = QInputPortAttribute.Normal;
			}
			attributeProvider = parameterInfo;
			ParamIndex = index;
			if (FunctionNodeType != null) {
				var member = FunctionNodeType.GetMemberInfo(Key);
				if (member != null) {
					SetValue = FunctionNodeType.GetMemberInfo(Key).Set;
					GetValue = FunctionNodeType.GetMemberInfo(Key).Get;
				}
				else {

					Debug.LogError(FunctionNodeType.Type + " 找不到 " + Key);
				}

			}
		}
		public QPortInfo(QMemeberInfo parameterInfo, object defualObj) {
			SetValue = parameterInfo.Set;
			GetValue = parameterInfo.Get;
			Key = parameterInfo.Key;
			ViewName = parameterInfo.QName;
			HasDefaultValue = true;
			DefaultValue = parameterInfo.Get(defualObj);
			ParameterType = parameterInfo.Type;
			attributeProvider = parameterInfo.MemeberInfo;
		}
		public override string ToString() {
			return ViewName;
		}
	}

	public static class QCommandTool
	{
		public static VisualElement AddQCommandInfo(this VisualElement root, QNodeInfo commandInfo, Action callBack)
		{
			root = root.AddVisualElement();
			root.style.flexDirection = FlexDirection.Row;
			root.AddLabel(commandInfo.ViewName);
			foreach (var info in commandInfo.Ports) {
				if (info.HasDefaultValue) {
					info.TempValue = info.DefaultValue;
				}
				root.Add(info.ViewName, info.TempValue, info.ParameterType, (value) => {

				});
			}
		//	root.AddButton("运行", () => { commandInfo.Invoke(commandInfo.TempValues.ToArray()); callBack(); });
			return root;
		}
	}
}
