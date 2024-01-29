using QTool.Reflection;
using System.Collections;
using System.Collections.Generic;
using System;
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
		public static void Start([QNodeName] string startKey = "起点")
		{
		}
		[QStartNode]
		[QName("起点/计数起点")]
		public static void CountStart(QFlowNode This, [QNodeName] string startKey = "起点", [QName("次数")] int times = 3, [QOutputPort] QFlow Run = default)
		{
			var count = 0;
			count = This[nameof(count)].As<int>();
			if (++count % times == 0)
			{
				This.SetNetFlowPort(nameof(Run));
			}
			This[nameof(count)] = count;
		}
		[QName("流程图/引用子图")]
		public static IEnumerator GraphAsset(QFlowNode This, [QNodeName, QName("流程图")] QFlowGraphAsset asset, string startNode = StartKey)
		{
			if (asset == null) yield break;
			var key = asset.GetHashCode().ToString();
			if (!This.Graph.Values.ContainsKey(key))
			{
				This.Graph.Values[key] = asset.Graph.QDataCopy();
			}
			yield return (This.Graph.Values[key] as QFlowGraph).InvokeEventIEnumerator(startNode);
		}
		[QName("流程图/停止"), QEndNode]
		public static void Stop(QFlowNode This)
		{
			This.Graph.Stop();
		}
		[QName("流程图/判断分支")]
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
		[QName("流程图/异步分支")]
		public static void AsyncBranch(QFlowNode This, [QOutputPort] List<QFlow> branchs)
		{
			for (int i = 0; i < branchs.Count; i++)
			{
				if (i == branchs.Count - 1)
				{
					This.SetNetFlowPort(nameof(branchs), i);
				}
				else
				{
					This.RunPort(nameof(branchs), i);
				}
			}
		}
		[QName("流程图/全部完成")]
		public static IEnumerator AllOver(QFlowNode This, [QFlowPort(onlyOneRunning = true)] List<QFlow> branchs)
		{
			List<int> taskList = new List<int> { };
			for (int i = 0; i < branchs.Count; i++)
			{
				taskList.Add(i);
			}
			QDebug.Log("全部完成节点开始：[" + taskList.ToOneString("|") + "]");
			This.RunningPortList.Clear();
			while (taskList.Count > 0)
			{
				foreach (var port in This.RunningPortList)
				{
					if (port.port == nameof(branchs))
					{
						taskList.Remove(port.index);
						QDebug.Log("完成[" + port.index + "]剩余[" + taskList.ToOneString("|") + "]");
					}
				}
				This.RunningPortList.Clear();
				yield return QFlowGraph.Step;
			}
			QDebug.Log("全部完成");
		}
		[QName("触发/触发事件")]
		public static void InvokeEvent(QFlowNode This,string eventName,[QName("全局")]bool global=false)
		{
			if (global)
			{
				QEventManager.InvokeEvent(eventName);
			}
			else
			{
				This.Graph.InvokeEvent(eventName);
			}
		}
		[QName("触发/等待事件")]
		public static IEnumerator WaitEvent(QFlowNode This, string eventName, [QName("全局")] bool global = false)
		{
			var eventOver = false;
			if (global)
			{
				Action action = null;
				action = () =>
				{
					eventOver = true;
				};
				while (!eventOver && This.State != QNodeState.失败)
				{
					yield return null;
				}
				QEventManager.Register(eventName, action);
			}
			else
			{
				Action<string> action = null;
				action = key =>
				{
					if (key == eventName)
					{
						eventOver = true;
					}
				};
				This.Graph.OnEvent += action;
				while (!eventOver && This.State != QNodeState.失败)
				{
					yield return null;
				}
				This.Graph.OnEvent -= action;
			}
		}
		[QName("触发/触发器")]
		public static IEnumerator Trigger(QFlowNode This, [QName("起点"), QInputPort("起点")] Transform start, [QName("预制体"), QPopup(nameof(Resources) + "/" + nameof(QTrigger))] string prefabKey, QTeamRelaction relaction, [QName("初始化"), QFlowPort, QOutputPort] Transform init, [QName("触发"), QFlowPort, QOutputPort] Transform triggerObject)
		{
			var prefab = Resources.Load<GameObject>(nameof(QTrigger) + "/" + prefabKey);
			if (prefab == null)
			{
				Debug.LogError("不存在[" + nameof(Resources) + "/" + nameof(QTrigger) + "/" + prefabKey + "]触发器预制体");
			}
			var trigger = prefab.CheckInstantiate()?.GetComponent<QTrigger>();
			if (start != null)
			{
				trigger.transform.position = start.transform.position;
			}
			if (trigger != null)
			{
				trigger.Node = This;
				trigger.Start = start;
				trigger.Relaction = relaction;
				yield return trigger.Init();
				if (This.State != QNodeState.失败)
				{
					This[nameof(init)] = trigger.transform;
					yield return This.RunPortIEnumerator(nameof(init));
					if (This.GetPortConnectState(nameof(init)) != QNodeState.失败 && trigger != null)
					{
						yield return trigger.Run();
					}
					else
					{
						trigger.Cancel();
						This.State = QNodeState.失败;
					}
				}
				else
				{
					trigger.Cancel();
				}
			}
			if (trigger != null)
			{
				trigger.gameObject.CheckDestory();
			}
		}
		[QIgnore]
		public static string ToInfoString(QFlowNode node)
		{
			var info = "";
			switch (node.command.method.Name)
			{
				case nameof(Start):
					if (node.Name != StartKey)
					{
						info += node.Name.ToLozalizationBoldKey() + " ";
					}
					break;
				case nameof(CountStart):
					if (node.Name != StartKey)
					{
						info = "每{0}次{1}".ToLozalizationKey(node.Ports["times"].ToInfoString(), node.Name.ToLozalizationBoldKey()) + " ";
					}
					info += node.Ports["Run"].GetConnectNode()?.ToInfoString();
					return info;
				case nameof(Trigger):
					return node.Ports["init"].ToInfoString() + " " + node.Ports["triggerObject"].ToInfoString();
				case nameof(GetValue):
					return node["key"]?.ToString().ToLozalizationKey();
				case nameof(Add):
					return "(" + node.Ports["a"].ToInfoString() + "+" + node.Ports["b"].ToInfoString() + ")";
				case nameof(Subtract):
					return "(" + node.Ports["a"].ToInfoString() + "-" + node.Ports["b"].ToInfoString() + ")";
				case nameof(Multiply):
					return "(" + node.Ports["a"].ToInfoString() + "x" + node.Ports["b"].ToInfoString() + ")";
				case nameof(Divide):
					return "(" + node.Ports["a"].ToInfoString() + "/" + node.Ports["b"].ToInfoString() + ")";
				case nameof(AsyncBranch):
					var port = node.Ports["branchs"];
					var list = port?.Value.As<IList>();
					if (list != null)
					{
						for (int i = 0; i < list.Count; i++)
						{
							if (port.HasConnect(i))
							{
								info += port.GetConnectNode(i).ToInfoString();
							}
						}
					}
					break;
				default:
					break;
			}
			info += node.Ports[QFlowKey.NextPort]?.GetConnectNode()?.ToInfoString();
			return info;
		}
		[QIgnore]
		public static float ToFloat(QFlowNode node, Func<QFlowNode, float> toFloatFunc = null)
		{
			var value = 0f;
			if (node.Ports.Count > 0)
			{
				switch (node.command.method.Name)
				{
					case nameof(Trigger):
						return node.Ports["init"].ToFloat(toFloatFunc) + node.Ports["triggerObject"].ToFloat(toFloatFunc);
					case nameof(GetValue):
						return node["key"].ToComputeFloat();
					case nameof(Add):
						return node.Ports["a"].ToFloat(toFloatFunc) + node.Ports["b"].ToFloat(toFloatFunc);
					case nameof(Subtract):
						return node.Ports["a"].ToFloat(toFloatFunc) - node.Ports["b"].ToFloat(toFloatFunc);
					case nameof(Multiply):
						return node.Ports["a"].ToFloat(toFloatFunc) * node.Ports["b"].ToFloat(toFloatFunc);
					case nameof(Divide):
						return node.Ports["a"].ToFloat(toFloatFunc) / node.Ports["b"].ToFloat(toFloatFunc);
					case nameof(AsyncBranch):
						var port = node.Ports["branchs"];
						var list = port?.Value.As<IList>();
						if (list != null)
						{
							for (int i = 0; i < list.Count; i++)
							{
								if (port.HasConnect(i))
								{
									value += port.GetConnectNode(i).ToFloat(toFloatFunc);
								}
							}
						}
						break;
					default:
						break;
				}
			}
			var nextNode = node.Ports[QFlowKey.NextPort]?.GetConnectNode();
			value += nextNode != null ? nextNode.ToFloat(toFloatFunc) : 0f;
			return value;
		}
	}
}
