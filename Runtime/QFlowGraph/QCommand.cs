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
        public static object Invoke(string commandStr) 
        {
            if (string.IsNullOrWhiteSpace(commandStr)) return null;
			commandStr =commandStr.ForeachBlockValue('\"', '\"',(value)=> { return value.Replace(" ", "@#&"); });
			commandStr = commandStr.ForeachBlockValue('“', '”', (value) => { return value.Replace(" ", "@#&"); });
			List<string> commands = new List<string>();
			commands.AddRange(commandStr.Split(' '));
			commands.RemoveSpace();
			for (int i = 0; i < commands.Count; i++)
			{
				commands[i] = commands[i].Replace("@#&", " ");
			}
            if (commands.Count > 0)
            {
                var name = commands.Dequeue();
				bool not = false;
				if (name.StartsWith("!"))
				{
					not = true;
					name =name.Substring(1);
				}
                if (NameDictionary.ContainsKey(name))
				{
					var result= NameDictionary[name].Invoke(commands);
					if(not && result is bool boolValue)
					{
						result = !boolValue;
					}
					return result;
				}
            }
            return null;
        }
        public static QList<string, QCommandInfo> KeyDictionary = new QList<string, QCommandInfo>();
        public static QDictionary<string, QCommandInfo> NameDictionary = new QDictionary<string, QCommandInfo>();
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
            else
            {
                return KeyDictionary.Get(key, (info) => info.method.Name);
            }
        }
        internal static void FreshCommands(params Type[] types)
        {
            foreach (var t in types)
			{
				if (!TypeList.Contains(t))
                {
                    FreshTypeCommands(t);
				}
            }
        }
        static void FreshTypeCommands(Type type)
        {
            type.ForeachFunction((methodInfo) =>
            {
                var typeKey = type.Name; 
                var typeName = type.QName();
                if (methodInfo.DeclaringType != typeof(object))
                {
                    var info = new QCommandInfo(methodInfo);
                    KeyDictionary[typeKey + '/' + methodInfo.Name] = info;
                    NameDictionary[methodInfo.QName().SplitEndString("/")] = info;

                }
            }, BindingFlags.Public | BindingFlags.Static);
            TypeList.AddCheckExist(type);

			QDebug.Log("初始化命令："+type+"\n" + KeyDictionary.ToOneString());
		}
      

    }
	[QCommandType("基础")]
	public static class BaseCommands
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
		public bool IsStringCommond { get; private set; } = false;
        public QCommandInfo(MethodInfo method)
        {
            Key = method.DeclaringType.Name + "/" + method.Name;
            name = method.QName();
            fullName = method.DeclaringType.QName() + '/' + name;
            this.method = method;
            paramInfos = method.GetParameters();
			if (method.ReturnType == typeof(void) || method.ReturnType == typeof(Task) || method.ReturnType == typeof(IEnumerator))
			{
				IsStringCommond = true;
			}
            foreach (var paramInfo in paramInfos)
            {
                paramNames.Add(paramInfo.Name);
                paramViewNames.Add(paramInfo.QName());
				if (IsStringCommond)
				{
					if ((!paramInfo.HasDefaultValue&& paramInfo.ParameterType != typeof(string) && paramInfo.ParameterType != typeof(float) && paramInfo.ParameterType != typeof(int) && paramInfo.ParameterType != typeof(object)) || paramInfo.IsOut)
					{
						IsStringCommond = false;
					}
				}
			}
		}
        public object Invoke(params object[] Params)
        {
            return method.Invoke(null, Params);
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
            return method.QName() + " " + paramViewNames.ToOneString(" ");
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
