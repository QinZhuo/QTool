using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using QTool.FlowGraph;
using System.Threading.Tasks;

namespace QTool
{
	public static class QCommand
	{
		static QCommand()
		{
			FreshCommands(typeof(QCommandType).GetAllTypes());
		}
		private const string SpaceReplaceKey = nameof(QCommand) + nameof(SpaceReplaceKey);
		private const string EnterReplaceKey = nameof(QCommand) + nameof(EnterReplaceKey);
		
		public static object Invoke(string commandStr)
		{
			if (string.IsNullOrWhiteSpace(commandStr)) return null;
			commandStr = commandStr.ForeachBlockValue('\"', '\"', (value) => { return value.Replace(" ", SpaceReplaceKey).Replace("\n", EnterReplaceKey); });
			commandStr = commandStr.ForeachBlockValue('“', '”', (value) => { return value.Replace(" ", SpaceReplaceKey).Replace("\n", EnterReplaceKey); });

			List<List<string>> commandInfos = new List<List<string>>();
			var commands = commandStr.Split('\n');
			foreach (var command in commands)
			{
				if (command.IsNull()) continue;
				var commandInfo = new List<string>();
				commandInfo.AddRange(command.Split(' '));
				for (int i = 0; i < commandInfo.Count; i++)
				{
					commandInfo[i] = commandInfo[i].Replace(SpaceReplaceKey, " ").Replace(EnterReplaceKey, "\n");
				}
				commandInfos.Add(commandInfo);
			}
			object result = null;
			foreach (var commmandInfo in commandInfos)
			{
				result=InvokeOneCommand(commmandInfo);
			}
			return result;
		}
		private static object InvokeOneCommand(IList<string> commandInfo)
		{
			if (commandInfo.Count > 0)
			{
				QDebug.Log("字符命令调用:" + commandInfo.ToOneString());
				var name = commandInfo.Dequeue();
				bool not = false;
				if (name.StartsWith("!"))
				{
					not = true;
					name = name.Substring(1);
				}
				if (NameDictionary.ContainsKey(name))
				{
					var result = NameDictionary[name].Invoke(commandInfo);
					if (not && result is bool boolValue)
					{
						result = !boolValue;
					}
					return result;
				}
			}
			return null;
		}

		public static QList<string, QCommandInfo> KeyDictionary { get; private set; } = new QList<string, QCommandInfo>();
        public static QDictionary<string, QCommandInfo> NameDictionary { get; private set; } = new QDictionary<string, QCommandInfo>();
        public static List<Type> TypeList = new List<Type>();
        public static QCommandInfo GetCommand(string key)
        {
			if (key == null)
            {
                return null;
            }
            if (KeyDictionary.ContainsKey(key))
            {
                return KeyDictionary[key];
            }
            if (key.Contains("/"))
            {
                key = key.SplitEndString("/");
            }
            if(NameDictionary.ContainsKey(key))
            {
                return NameDictionary[key];
            }
            else if(key.Contains("/"))
            {
				return GetCommand(key.SplitEndString("/"));
			}
			else
			{
				return null;
			}
        }
        static void FreshCommands(params Type[] types)
        {
			QDebug.Begin(nameof(QCommandType)+"初始化");
			foreach (var t in types)
			{
				if (!TypeList.Contains(t))
                {
                    FreshTypeCommands(t);
				}
            }
			QDebug.End(nameof(QCommandType) + "初始化", " 命令数 " + KeyDictionary.Count);
        }
        static void FreshTypeCommands(Type type)
        {
            type.ForeachFunction((methodInfo) =>
            {
				if (methodInfo.GetAttribute<QIgnoreAttribute>() != null ) return;
                var typeKey = type.Name; 
                var typeName = QReflection.QName(type);
                if (methodInfo.DeclaringType != typeof(object))
                {
                    var info = new QCommandInfo(methodInfo);
                    KeyDictionary[typeKey + '/' + methodInfo.Name] = info;
					NameDictionary[methodInfo.Name] = info;
					NameDictionary[QReflection.QName(methodInfo).SplitEndString("/")] = info;
                }
            }, BindingFlags.Public | BindingFlags.Static| BindingFlags.NonPublic);
            TypeList.AddCheckExist(type);
		}
      

    }
	[QCommandType("基础")]
	public static class QCommandFunction
	{
		[QName("日志/普通日志")]
		public static void Log(object obj)
		{
			QDebug.Log(obj);
		}
		[QName("日志/警告日志")] 
		public static void LogWarning(object obj)
		{
			Debug.LogWarning(obj);
		}
		[QName("日志/错误日志")]
		public static void LogError(object obj)
		{
			Debug.LogError(obj);
		}
		[QName("时间/时间控制")]
		public static void TimeScale(float timeScale, object flag)
		{
			QTime.ChangeScale(flag, timeScale);
		}
		[QName("时间/延迟")]
		public static IEnumerator Deley(float time)
		{
			yield return new WaitForSeconds(time);
		}
		[QIgnore]
		public static string ToInfoString(QFlowNode node)
		{
			var info = "";
			switch (node.command.method.Name)
			{
				case nameof(Log):
				case nameof(LogWarning):
				case nameof(LogError):  
					info = node.Ports["obj"].ToInfoString(); 
					break;
				default:
					break;
			}
			info += node.Ports[QFlowKey.NextPort].GetConnectNode()?.ToInfoString();
			return info;
		}
	}
	public class QCommandInfo : IKey<string>
    {
        public string Key { set; get; } 
        public string name; 
        public string fullName;
        public MethodInfo method;
        public ParameterInfo[] paramInfos;
        public List<string> paramNames = new List<string>();
        public List<string> paramViewNames = new List<string>();
		internal QList<object> TempValues = new QList<object>();
		public bool IsStringCommond { get; private set; } = false;
        public QCommandInfo(MethodInfo method)
        {
            Key = method.DeclaringType.Name + "/" + method.Name;
			name = QReflection.QName(method);
			fullName = QReflection.QName(method.DeclaringType) + '/' + name;
            this.method = method;
            paramInfos = method.GetParameters();
			if (method.ReturnType == typeof(void) || method.ReturnType == typeof(Task) || method.ReturnType == typeof(IEnumerator))
			{
				IsStringCommond = true;
			}
            foreach (var paramInfo in paramInfos)
            {
                paramNames.Add(paramInfo.Name);
				paramViewNames.Add(QReflection.QName(paramInfo));
				if (IsStringCommond)
				{
					if ((!paramInfo.HasDefaultValue&& !paramInfo.ParameterType.IsValueType&& paramInfo.ParameterType!=typeof(string) && paramInfo.ParameterType != typeof(object)) || paramInfo.IsOut)
					{
						IsStringCommond = false;
					}
				}
			}
		}
        public object Invoke(params object[] Params)
        {
			try
			{
				return method.Invoke(null, Params);
			}
			catch (Exception e)
			{
				Debug.LogError("运行命令 " + fullName+"("+paramViewNames.ToOneString(",")+ ")(" + Params.ToOneString(",") + ")出错 "+e);
				return null;
			}
        }
        public object Invoke(IList<string> commands)
        {
            var paramObjs = new object[paramInfos.Length];
            for (int i = 0; i < paramInfos.Length; i++)
            {
                var pInfo = paramInfos[i];
                if (i < commands.Count)
                {
                    try
                    {
						if (pInfo.ParameterType == typeof(object))
						{
							paramObjs[i] = commands[i];
						}
						else
						{
							paramObjs[i] = commands[i].ParseQDataType(pInfo.ParameterType);
						}
                    }
                    catch (Exception e)
                    {

                        Debug.LogError("通过[" + commands.ToOneString(" ") + "]调用命令[" + this + "]出错 " + "参数[" + pInfo + "]解析出错 :\n" + e);
                        return null;
                    }
                }
                else if (pInfo.HasDefaultValue)
                {
                    paramObjs[i] = pInfo.DefaultValue;
                }
                else
                {
                    Debug.LogError("通过[" + commands.ToOneString(" ") + "]调用命令[" + this + "]出错 " + "缺少参数[" + pInfo + "]");
                    return null;
                }
            }
            return method.Invoke(null, paramObjs);
        }
        public override string ToString()
        {
            return QReflection.QName(method) + " " + paramViewNames.ToOneString(" ");
        }
    }


	/// <summary>
	///  指定类型为命令类 其中的所有公开静态函数都会转换为 可以调用的命令
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class QCommandType : QNameAttribute
	{
		public QCommandType(string viewName) : base(viewName)
		{
		}
	}
}
