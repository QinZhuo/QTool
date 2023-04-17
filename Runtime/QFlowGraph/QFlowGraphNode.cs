using QTool.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FlowGraph
{
	[QCommandType("基础")]
	public static class QFlowGraphNode
	{

		[QName("数据/获取变量")]
		[return: QOutputPort(true)]
		public static object GetValue(QFlowNode This, string key)
		{
			return This.Graph.Values[key];
		}
		[QName("数据/设置变量")]
		public static void SetValue(QFlowNode This, string key, object value)
		{
			This.Graph.Values[key] = value;
		}
		[QName("数据/对象实例")]
		[return: QOutputPort(true)]
		public static object ObjectInstance([QNodeName] object obj)
		{
			return obj;
		}
		[QName("数据/内置函数")]
		public static object ObjectFunction(QFlowNode This, object obj, [QName("函数名"), QNodeName] string funcName, [QName("参数")] object[] param)
		{
			if (obj == null) return null;
			var typeInfo = QReflectionType.Get(obj.GetType());
			var funcInfo = typeInfo.Functions[funcName];
			if (funcInfo == null)
			{
				Debug.LogError("obj ".GetType() + " 不存在函数[" + funcName + "]");
				return null;
			}
			return funcInfo.Invoke(obj, param);
		}

	


		[QName("运算/加")]
		[return: QOutputPort(true)]
		public static object Add(object a, object b)
		{
			return QReflection.OperaterAdd(a, b);
		}
		[QName("运算/减")]
		[return: QOutputPort(true)]
		public static object Subtract(object a, object b)
		{
			return QReflection.OperaterSubtract(a, b);
		}
		[QName("运算/乘")]
		[return: QOutputPort(true)]
		public static object Multiply(object a, object b)
		{
			return QReflection.OperaterMultiply(a, b);
		}
		[QName("运算/除")]
		[return: QOutputPort(true)]
		public static object Divide(object a, object b)
		{
			return QReflection.OperaterDivide(a, b);
		}
		[QName("运算/大于")]
		[return: QOutputPort(true)]
		public static bool GreaterThan(object a, object b)
		{
			return QReflection.OperaterGreaterThan(a, b);
		}
		[QName("运算/大于等于")]
		[return: QOutputPort(true)]
		public static bool GreaterThanOrEqual(object a, object b)
		{
			return QReflection.OperaterGreaterThanOrEqual(a, b);
		}
		[QName("运算/小于")]
		[return: QOutputPort(true)]
		public static bool LessThan(object a, object b)
		{
			return QReflection.OperaterLessThan(a, b);
		}
		[QName("运算/小于等于")]
		[return: QOutputPort(true)]
		public static bool LessThanOrEqual(object a, object b)
		{
			return QReflection.OperaterLessThanOrEqual(a, b);
		}
		[QName("运算/等于")]
		[return: QOutputPort(true)]
		public static bool Equal(object a, object b)
		{
			return QReflection.OperaterEqual(a, b);
		}
		[QName("运算/逻辑运算/与")]
		[return: QOutputPort(true)]
		public static bool And(bool a, bool b)
		{
			return a && b;
		}
		[QName("运算/逻辑运算/或")]
		[return: QOutputPort(true)]
		public static bool Or(bool a, bool b)
		{
			return a || b;
		}
		[QName("运算/逻辑运算/非")]
		[return: QOutputPort(true)]
		public static bool Not(bool a)
		{
			return !a;
		}
	
		internal const string StartKey = "起点";
		[QStartNode]
		[QName("起点/起点")]
		public static void Start()
		{
		}

		[QStartNode]
		[QName("起点/事件")]
		public static void Event([QNodeName] string eventKey = "事件名")
		{
		}
		[QName("流程图/内置子图")]
		public static IEnumerator SubGraph(QFlowNode This, QFlowGraph subGraph, string startNode = StartKey)
		{
			yield return subGraph.CreateInstance().RunIEnumerator(startNode);
		}

		[QName("流程图/引用子图")]
		public static IEnumerator GraphAsset(QFlowNode This, [QNodeName] QFlowGraphAsset obj, string startNode = StartKey)
		{
			if (obj == null) yield break;
			var key = obj.GetHashCode().ToString();
			if (!This.Graph.Values.ContainsKey(key))
			{
				This.Graph.Values[key] = obj.Graph.CreateInstance();
			}
			yield return (This.Graph.Values[key] as QFlowGraph).RunIEnumerator(startNode);
		}
		[QName("流程图/停止"), QEndNode]
		public static void Stop(QFlowNode This)
		{
			This.Graph.Stop();
		}
		[QName("流程图/分支/判断分支")]
		public static void BoolCheck(QFlowNode This, bool boolValue, [QOutputPort] QFlow True, [QOutputPort] QFlow False)
		{
			if (boolValue)
			{
				This.SetNetFlowPort(nameof(True));
			}
			else
			{
				This.SetNetFlowPort(nameof(False));
			}
		}
		[QName("流程图/分支/异步分支")]
		public static void AsyncBranch(QFlowNode This, [QOutputPort] List<QFlow> branchs)
		{
			for (int i = 0; i < branchs.Count; i++)
			{
				if (i == 0)
				{
					This.SetNetFlowPort(nameof(branchs), i);
				}
				else
				{
					This.RunPort(nameof(branchs), i);
				}

			}

		}
		[QName("流程图/分支/全部完成")]

		public static IEnumerator AllOver(QFlowNode This, [QFlowPort(onlyOneRunning = true)] List<QFlow> branchs)
		{
			List<int> taskList = new List<int> { };
			for (int i = 0; i < branchs.Count; i++)
			{
				taskList.Add(i);
			}
			QDebug.Log("全部完成节点开始：[" + taskList.ToOneString("|") + "]");
			This.TriggerPortList.Clear();
			while (taskList.Count > 0)
			{
				foreach (var port in This.TriggerPortList)
				{
					if (port.port == nameof(branchs))
					{
						taskList.Remove(port.index);
						QDebug.Log("完成[" + port.index + "]剩余[" + taskList.ToOneString("|") + "]");
					}
				}
				This.TriggerPortList.Clear();
				yield return QFlowGraph.Step;
			}
			QDebug.Log("全部完成");
		}
	}
}
