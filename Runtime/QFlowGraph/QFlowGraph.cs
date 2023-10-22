using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using QTool.Reflection;
using System.Reflection;

namespace QTool.FlowGraph
{
	[Serializable]
    public class QFlowGraph: QSerializeObject<QFlowGraph>
	{
		public override void OnQDeserializeOver()
		{
			base.OnQDeserializeOver();
			if (StartNode.Count == 0)
			{
				foreach (var node in NodeList)
				{
					node.Init(this);
				}
			}
			foreach (var node in NodeList)
			{
				foreach (var port in node.Ports)
				{
					port.m_value = null;
				}
			}
		}
        public override string ToString()
        {
            return this.ToQData();
        }
		[QIgnore]
		public float ToFloat(string startKey = null, Func<QFlowNode, float> toFloatFunc = null)
		{
			var value = 0f;
			if (startKey.IsNull())
			{
				foreach (var kv in StartNode)
				{
					value += ToFloat(kv.Key, toFloatFunc);
				}
			}
			else
			{
				var node = GetNode(startKey);
				if (node?.command != null)
				{
					value += node.ToFloat(toFloatFunc);
				}
			}
			return value;
		}
		public string ToInfoString(string startKey=null)
		{
			var info = "";
			if (startKey.IsNull())
			{
				foreach (var kv in StartNode)
				{
					info += ToInfoString(kv.Key) + " ";
				}
			}
			else
			{
				var node = GetNode(startKey);
				if (node?.command != null)
				{
					info = node.ToInfoString();
				}
			}
			return info;
		}
		[QName]
		public string Name {  set; get; }
		[QName]
		public Vector2 ViewPos { get; set; }
		[QName]
		public QList<string,QFlowNode> NodeList { private set; get; } = new QList<string,QFlowNode>();
		[QName]
		public QDictionary<string, object> Values { private set; get; } = new QDictionary<string, object>();

		[QIgnore]
		public QDictionary<string, QFlowNode> StartNode = new QDictionary<string, QFlowNode>();

		[QIgnore]
		public List<string> RunningNodeList { private set; get; } = new List<string>();
		public bool IsRunning => RunningNodeList.Count > 0;

		public T GetValue<T>(string key="")
        {
            var type = typeof(T);
			if (key.IsNull())
			{
				key = type.Name;
			}
			var obj = Values[key];
            if (obj==null&& type.IsValueType)
            {
                obj = type.CreateInstance();
            }
            return (T)obj;
        }
		public void SetValue<T>( T value)
		{
			SetValue(typeof(T).Name, value);
		}
		public void SetValue<T>(string key,T value)
        {
            Values[key] = value;
        }
		public object this[string key] { get => Values[key];set => Values[key] = value; }
		public QFlowNode GetNode(string key)
		{
			if (key.IsNull()) return null;

			if (NodeList.ContainsKey(key))
			{
				return NodeList[key];
			}
			else if (StartNode.ContainsKey(key))
			{
				return StartNode[key];
			}
			return null;
		}
		public void Connect(PortId start, PortId end)
		{
			GetPort(start)?.Connect(end,start.index);
		}
		public void DisConnect(PortId start, PortId end)
		{
			GetPort(start)?.DisConnect(end, start.index);
		}
		[QIgnore]
		public List<IEnumerator> CoroutineList { private set; get; } = new List<IEnumerator>();
        public ConnectInfo GetConnectInfo(PortId? portId)
        {
            if (portId == null) return null;
            var connectInfo= GetPort(portId)?[portId.Value.index];
			return connectInfo;
        }
		public QFlowPort GetPort(PortId? portId)
		{
			if (portId == null) return null;
			return GetNode(portId.Value.node)?.Ports[portId.Value.port];
		}
		public object GetPortValue(PortId? portId)
		{
			if (portId == null) return null;
			return GetPort(portId)?.GetValue(portId.Value.index);
		}
		public void Remove(QFlowNode node)
        {
            if (node == null) return;
            node.ClearAllConnect();   
            NodeList.Remove(node);
        }
        public QFlowNode AddNode(string commandKey)
        {
            var newNode= Add(new QFlowNode(commandKey));
			newNode.rect.center = new Vector2(300 * 1.1f * (NodeList.Count - 1), 0);
			return newNode;
        }
	
		public QFlowNode AddNodeToEnd(string commandKey)
		{
			if (NodeList.Count > 0)
			{
				return NodeList.StackPeek().AddNextNode(commandKey);
			}
			else
			{
				return AddNode(commandKey);
			}
		}
		public void Parse(IList<QFlowNode> nodes,Vector2 startPos)
        {
            var lastKeys = new List<string>();
            var keys = new List<string>();
            var offsetPos = Vector2.one * float.MaxValue;
            foreach (var node in nodes)
            {
                offsetPos = new Vector2(Mathf.Min(offsetPos.x, node.rect.x), Mathf.Min(offsetPos.y, node.rect.y));
                lastKeys.Add(node.Key);
                node.Key = QId.NewId();
                keys.Add(node.Key);
            }

            foreach (var node in nodes)
            {
                node.rect.position = node.rect.position - offsetPos + startPos;
                foreach (var port in node.Ports)
                {
                    foreach (var c in port.ConnectInfolist)
                    {
                        var lastConnect = c.ConnectList.ToArray();
                        c.ConnectList.Clear();
                        foreach (var connect in lastConnect)
                        {
                            var keyIndex = lastKeys.IndexOf(connect.node);
                            if (keyIndex >= 0)
                            {
                               c.ConnectList.Add(new PortId
                                {
                                    node = keys[keyIndex],
                                    port = connect.port,
                                });
                            }
                        }
                    }
                }
            }
            AddRange(nodes);
        }
        public void AddRange(IList<QFlowNode> nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }
        public QFlowNode Add(QFlowNode node)
        {
            NodeList.Add(node);
            node.Init(this);
            return node;
		}
		
	
		public void Run(string startNode= QFlowGraphNode.StartKey)
		{
			if (NodeList.Count == 0)
			{
				OnQDeserializeOver();
			}
			RunIEnumerator(startNode).Start(CoroutineList);
		}
		public IEnumerator RunIEnumerator(string startNode)
        {
			if (startNode.IsNull()) yield break;
			if (!Application.isPlaying)
			{
				Debug.LogError("运行流程图[" + Name + "]出错 不能在非运行时运行流程图");
				yield break;
			}
			var curNode = GetNode(startNode); 
			if (curNode != null)
			{
				while (curNode != null)
				{
					RunningNodeList.Add(curNode.Key);
					yield return curNode.RunIEnumerator();
					if (RunningNodeList.Contains(curNode.Key))
					{
						RunningNodeList.RemoveAt(RunningNodeList.IndexOf(curNode.Key));
					}
					else
					{
						yield break;
					}
					var port = curNode.NextNodePort;
					if (port != null)
					{
						if (port.Value.port == QFlowKey.FromPort)
						{
							curNode = GetNode(port.Value.node);
						}
						else
						{
							var targetPort = GetPort(port);
							if (targetPort != null && targetPort.IsFlow)
							{
								if (targetPort.FlowPort.onlyOneRunning)
								{
									if (targetPort.Node.State != QNodeState.运行)
									{
										Run(targetPort.Node.Key);
									}
								}
								else
								{
									Run(targetPort.Node.Key);
								}
							}
							targetPort.Node.SetPortRunning(port.Value);
							curNode = null;
						}
					}
					else
					{
						curNode = null;
					}
				}
			}
			else
			{
				Debug.LogError("不存在开始节点 [" + startNode + "]");
			}
        }
		public void Stop()
		{
			RunningNodeList.Clear();
			foreach (var coroutine in CoroutineList.ToArray())
			{
				coroutine.Stop();
			}
			CoroutineList.Clear();
		}
		public void Clear()
		{
			Stop();
			foreach (var node in NodeList.ToArray())
			{
				Remove(node);
			}
		}
	
		public static IEnumerator Step { get; set; } = FixedUpdateStep();

		static WaitForFixedUpdate wait= new WaitForFixedUpdate();
		static IEnumerator FixedUpdateStep()
		{
			yield return wait;
		}
		public void RegisterValue<RuntimeT, DataT>(QRuntime<RuntimeT, DataT> Runtime) where RuntimeT : QRuntime<RuntimeT, DataT>, new() where DataT : QDataList<DataT>, new()
		{
			if (Runtime != null)
			{
				SetValue((RuntimeT)Runtime);
				var dataTypeInfo = QSerializeType.Get(typeof(DataT));
				foreach (var member in dataTypeInfo.Members)
				{
					if (member.QNameAttribute == null) continue;
					if(member.Type.IsValueType || member.Type == typeof(string))
					{
						Values[member.QName] = member.Get(Runtime.Data);
					}
				}
				var runtimeTypeInfo = QSerializeType.Get(typeof(RuntimeT));
				foreach (var member in runtimeTypeInfo.Members)
				{
					if (member.QNameAttribute == null) continue;
					if (member.Type.Is(typeof(QRuntimeValue<float>)))
					{
						var runtimeValue = member.Get(Runtime).As<QRuntimeValue<float>>();
						runtimeValue.Name = member.QName;
						runtimeValue.OnValueChange += SetValue;
						runtimeValue.InvokeOnChange();
					}
					else if (member.Type.Is(typeof(QRuntimeValue<string>)))
					{
						var runtimeValue = member.Get(Runtime).As<QRuntimeValue<string>>();
						runtimeValue.Name = member.QName;
						runtimeValue.OnValueChange += SetValue;
						runtimeValue.InvokeOnChange();
					}
					else if (member.Type.Is(typeof(QRuntimeValue<bool>)))
					{
						var runtimeValue = member.Get(Runtime).As<QRuntimeValue<bool>>();
						runtimeValue.Name = member.QName;
						runtimeValue.OnValueChange += SetValue;
						runtimeValue.InvokeOnChange();
					}
				}
			}
		}
		public void UnRegisterValue<RuntimeT, DataT>(QRuntime<RuntimeT, DataT> Runtime) where RuntimeT : QRuntime<RuntimeT, DataT>, new() where DataT : QDataList<DataT>, new()
		{
			if (Runtime != null)
			{
				var runtimeTypeInfo = QSerializeType.Get(typeof(RuntimeT));
				foreach (var member in runtimeTypeInfo.Members)
				{
					if (member.QNameAttribute == null) continue;

					if (member.Type.Is(typeof(QRuntimeValue<float>)))
					{
						var runtimeValue = member.Get(Runtime).As<QRuntimeValue<float>>();
						runtimeValue.OnValueChange -= SetValue;
					}
					else if (member.Type.Is(typeof(QRuntimeValue<string>)))
					{
						var runtimeValue = member.Get(Runtime).As<QRuntimeValue<string>>();
						runtimeValue.OnValueChange -= SetValue;
					}
					else if (member.Type.Is(typeof(QRuntimeValue<bool>)))
					{
						var runtimeValue = member.Get(Runtime).As<QRuntimeValue<bool>>();
						runtimeValue.OnValueChange -= SetValue;
					}
				}
			}
		}
	}
	
	public struct PortId
    {
        public string node;
        public string port;
        public int index ;
        internal PortId(QFlowPort statePort, int index=0)
        {
            node = statePort.Node?.Key;
            port = statePort.Key;
            this.index = index;
        }
        public override string ToString()
        {
            return index==0? port:port+"["+index+"]";
        }
		
	}


	/// <summary>
	///  指定参数端口为输入端口
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
	public class QInputPortAttribute : Attribute
	{
		public static QInputPortAttribute Normal = new QInputPortAttribute();

		public bool HasAutoValue
		{
			get
			{
				return !string.IsNullOrEmpty(autoGetValue);
			}
		}
		public string autoGetValue= "";
		public bool CheckNull = false;
		public QInputPortAttribute(string autoGetValue ="", bool checkNull = false)
		{
			this.autoGetValue = autoGetValue;
			CheckNull = checkNull;
		}
		public QInputPortAttribute(bool checkNull)
		{
			CheckNull = checkNull;
		}
	}
	/// <summary>
	///  指定参数端口为输出端口
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter| AttributeTargets.ReturnValue, AllowMultiple = false)]
    public class QOutputPortAttribute : Attribute
	{
        public static QOutputPortAttribute Normal = new QOutputPortAttribute();

		public bool autoRunNode=false;

		public QOutputPortAttribute(bool autoRunNode=false)
        {
			this.autoRunNode = autoRunNode;
        }
    }
    /// <summary>
    /// 指定参数端口为流程端口
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class QFlowPortAttribute : Attribute
	{
        public static QFlowPortAttribute Normal = new QFlowPortAttribute();
		public bool fixedShowValue = false;
		public bool onlyOneRunning = false;
		public QFlowPortAttribute()
		{
		}
	}
    /// <summary>
    /// 指定以端口数值自动更改节点名字 
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class QNodeNameAttribute : Attribute
    {
        public QNodeNameAttribute()
        {
        }
    }
    /// <summary>
    /// 指定函数节点为起点节点 即没有流程输入端口 节点Key为函数名
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class QStartNodeAttribute : Attribute
    {
        public QStartNodeAttribute()
        {
        }
    }
	/// <summary>
	/// 指定函数节点为结束节点 即没有流程输出端口
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class QEndNodeAttribute : Attribute
	{
		public QEndNodeAttribute()
		{
		}
	}
	public struct QFlow
    {
        public static Type Type = typeof(QFlow);
    }
    public class ConnectInfo
    {
        public QList<PortId> ConnectList = new QList<PortId>();
        public void ChangeKey(int oldKey, int newKey,QFlowPort port)
        {
			var newId = port.GetPortId(newKey);
			var oldId = port.GetPortId(oldKey);
            foreach (var portId in ConnectList)
            {
                var list = port.Node.Graph.GetConnectInfo(portId).ConnectList;
                list.Remove(oldId);
                list.Add(newId);
            }
        }
        public PortId? FirstConnect
        {
			get
			{
				if (ConnectList.Count > 0)
				{
					return ConnectList.QueuePeek();
				}
				else
				{
					return null;
				}
			}
        }
		public PortId? GetConnectPortId(QFlowGraph graph,bool onlyFlowNode)
		{
			switch (ConnectList.Count)
			{
				case 0:return null;
				case 1:
					{
						var connect = FirstConnect;
						var port = graph.GetPort(connect);
						if (port == null || (onlyFlowNode && !port.IsFlow))
						{
							return null;
						}
						return connect;
					}
				default:
					{
						if (onlyFlowNode)
						{
							foreach (var connect in ConnectList)
							{
								var port = graph.GetPort(connect);
								if (port == null) continue;
								if (port.IsFlow)
								{
									return port.GetPortId(connect.index);
								}
							}
						}
						else
						{
							foreach (var connect in ConnectList)
							{
								var port = graph.GetPort(connect);
								if (port == null) continue;
								if (port.ConnectType != typeof(QFlow))
								{
									return port.GetPortId(connect.index);
								}
							}
						}
					}
					return null;
			}
		}
    }
	public class QFlowPort : IKey<string>
	{

		public string name { get; set; }
		public string Key { get; set; }
		public string ViewName
		{
			get
			{
				if (IsOutput)
				{
					return name;
				}
				else if (InputPort != null && InputPort.HasAutoValue && !HasConnect())
				{
					return name + " = {" + InputPort.autoGetValue + "}";
				}
				else
				{
					return name;
				}
			}
		}

		[QName]
		public string StringValue { get; private set; }
		[QIgnore]
		public bool IsOutput { get; internal set; } = false;
		[QIgnore]
		public bool IsList { get; internal set; }
		public override string ToString()
		{
			return name + " (" + ConnectType + ")["+Key+"]";
		}
		public float ToFloat(Func<QFlowNode, float> toFloatFunc = null, int index = 0)
		{
			if (HasConnect(index))
			{
				var node = GetConnectNode(index);
				if (node == null)
				{
					node = GetConnectNode(index, false);
				}
				if (node == null) return 0;
				return node.ToFloat(toFloatFunc);
			}
			else if (!IsOutput)
			{
				return GetValue(index).ToComputeFloat();
			}
			else
			{
				return 0;
			}
		}
		public string ToInfoString(int index = 0)
		{
			if (HasConnect(index))
			{
				var node = GetConnectNode(index);
				if (node == null)
				{
					node = GetConnectNode(index, false);
				}
				return node.ToInfoString();
			}
			else if(!IsOutput)
			{
				return GetValue(index)?.ToString();
			}
			else
			{
				return ""; 
			}
		}
		public void IndexChange(int a, int b)
		{
			var list = Value as IList;
			if (a < 0)
			{
				ConnectInfolist.Insert(b, new ConnectInfo());
				for (int i = b; i < list.Count; i++)
				{
					var c = this[i];
					c.ChangeKey(i - 1, i, this);
				}
			}
			else if (b < 0)
			{
				ClearAllConnect(a);
				ConnectInfolist.RemoveAt(a);
				for (int i = a; i < list.Count; i++)
				{
					var c = this[i];
					c.ChangeKey(i + 1, i, this);
				}
			}
		}
		public ConnectInfo this[int index]
		{
			get
			{
				if (index < 0) return null;
				if (ConnectInfolist[index] == null)
				{
					ConnectInfolist[index] = new ConnectInfo();
				}
				return ConnectInfolist[index];
			}
		}
		public QList<ConnectInfo> ConnectInfolist { set; get; } = new QList<ConnectInfo>();
		public bool IsFlow => FlowPort != null;
        [QIgnore]
        public Type ConnectType { internal set; get; }
        public bool IsShowValue(int index=0)
        {
			if (IsFlow && FlowPort.fixedShowValue)
			{
				return true;
			}
			if (!IsOutput)
			{
				if (InputPort!=null&&!InputPort.HasAutoValue && !HasConnect(index))
				{
					return true;
				}
			}
			return false;
		}
		[QIgnore]
		public System.Reflection.ParameterInfo parameterInfo;
        [QIgnore]
        public QNodeNameAttribute NameAttribute;
        [QIgnore]
        public QOutputPortAttribute OutputPort;
		[QIgnore]
		public QInputPortAttribute InputPort;
		[QIgnore]
        public QFlowPortAttribute FlowPort;
        [QIgnore]
        public int paramIndex = -1;
        [QIgnore]
        public Type ValueType { internal set; get; }

        [QIgnore]
        public QFlowNode Node { get; internal set; }
		public bool HasConnect(int index = 0)
		{
			{
				return this[index].ConnectList.Count > 0;
			}
		}

		[QIgnore]
        internal object m_value;
		
        [QIgnore]
        public object Value
        {
            get
            {
                if (ValueType == QFlow.Type|| Node.command==null) return null;
			
				if (IsOutput)
				{
					if (OutputPort.autoRunNode)
					{
						Node.Run();
					}
				}
				else
				{
					if (IsList)
					{
						if (m_value is IList list)
						{
							for (int i = 0; i < ConnectInfolist.Count; i++)
							{
								if (HasConnect(i) && list.Count > i)
								{
									var element = Node.Ports[Key].GetConnectValue(i); 
									if (element != null && CanConnect(element.GetType()))
									{
										list[i] = element;
									}
								}
							}
						}
					}
					else
					{
						if (HasConnect())
						{
							return GetConnectValue();
						}
						else
						{
							if (InputPort.HasAutoValue)
							{
								return Node.Graph.Values[InputPort.autoGetValue].AsType(ValueType);
							}
						}
					}
				}
				if (m_value.IsNull())
				{
					m_value = StringValue.ParseQDataType(ValueType, true, m_value);
				}
				return m_value;
            }
			set
			{
				if (ValueType == QFlow.Type || Node.command == null) return;
				if (!IsList && Equals(m_value, value)) return;
				if (m_value.IsNull() && value.IsNull()) return;
				m_value = value; 
                StringValue = value.ToQDataType(ValueType);
                if (NameAttribute != null)
                {
					var newName= m_value?.ToString();
					if (!newName.IsNull())
					{
						Node.Name = newName;
					}
				}
            }
        }
		public PortId GetPortId(int index = 0)
		{
			return new PortId(this, index);
		}
		public object GetValue(int Index=0)
		{
			System.Object value = null;
			if (IsList)
			{
				if (Value is IList list)
				{
					if (list.Count > Index)
					{
						value = list[Index];
					}
				}
			}
			else
			{
				value = Value;
			}
			return value is QFlow ? null : value;
		}

		public QFlowNode GetConnectNode(int index = 0, bool onlyFlowNode = true)
		{
			if (HasConnect(index))
			{
				var connectPortKey = this[index].GetConnectPortId(Node.Graph, onlyFlowNode);
				if (connectPortKey != null)
				{
					return Node.Graph.GetNode(connectPortKey.Value.node);
				}
			}
			return null;
		}
		public QFlowPort GetConnectPort(int index = 0, bool onlyFlow = true)
		{
			if (HasConnect(index))
			{
				return Node.Graph.GetPort(this[index].GetConnectPortId(Node.Graph, onlyFlow));
			}
			return null;
		}
		public object GetConnectValue(int index = 0)
		{
			if (HasConnect(index))
			{
				return Node.Graph.GetPortValue(this[index].GetConnectPortId(Node.Graph, false)).AsType(ValueType);
			}
			return null;
		}
		public void Init(QFlowNode node)
        {
            this.Node = node;
            if (IsList && Value is IList list)
            {
                ConnectInfolist.RemoveAll((obj) => ConnectInfolist.IndexOf(obj)>= list.Count|| ConnectInfolist.IndexOf(obj) < 0); 
            }
        }
        public static QDictionary<Type, List<Type>> CanConnectList = new QDictionary<Type, List<Type>>()
        {
            {
                 typeof(int),
                 new List<Type>{typeof(float),typeof(double)}
            }
        };
        public bool CanConnect(Type type)
        {
			if (ConnectType == null) return false;
            if (ConnectType == type)
            {
                return true;
            }
            else if (ConnectType != QFlow.Type && type != QFlow.Type)
            {
				if (ConnectType == typeof(object))
				{
					return true;
				}
    			else if (type == typeof(object))
                {
                    return true;
                }
				else if (ConnectType.Is(type))
                {
                    return true;
                }
				else if (ConnectType.Is(typeof(Component)) && type.Is(typeof(Component)))
				{
					return true;
				}
				else if (CanConnectList.ContainsKey(ConnectType))
                {
                    return CanConnectList[ConnectType].Contains(type);
				}
            }
            return false;
        }
        public bool CanConnect(QCommandInfo info, out string portKey)
        {
            if (ConnectType == QFlow.Type)
            {
                portKey = QFlowKey.FromPort;
                return true;
            }
            foreach (var paramInfo in info.paramInfos)
            {
                if (paramInfo.IsOut) continue;
                var can = CanConnect(paramInfo.ParameterType);
                if (can)
                {
                    portKey = paramInfo.Name;
                    return true;
                }
            }
            portKey = "";
            return false;
        }
        public bool CanConnect(QFlowPort port)
        {
            if (IsOutput == port.IsOutput) return false;
			if (IsFlow && port.IsFlow) return true;
            return CanConnect(port.ConnectType);
        }
        public void Connect(PortId? portId, int index =0)
        {
            if (portId == null) return;
            var targetPort = Node.Graph.GetPort(portId);
            if (targetPort == null) return;
            if (!CanConnect(targetPort))
            {
                Debug.LogError("不能将 " + this + " 连接 " + targetPort);
                return;
            }
			if (IsFlow && targetPort.IsFlow)
			{
				if (IsOutput)
				{
					ClearAllConnect(index,true);
				}
			}
			else
			{
				if (IsOutput)
				{
					targetPort.ClearAllConnect(portId.Value.index);
				}
				else
				{
					ClearAllConnect(index);
				}
			}
            this[index].ConnectList.AddCheckExist(portId.Value);
            targetPort[portId.Value.index].ConnectList.AddCheckExist(GetPortId(index));


        }
     
        public void DisConnect(PortId? connect, int index = 0)
        {
            if (connect == null) return;
            this[index].ConnectList.Remove(connect.Value);
            var port = Node.Graph.GetPort(connect);
            if (port==null)
            {
				Debug.LogError("不存在端口[" + connect + "]");
                return;
            }
            port[connect.Value.index]?.ConnectList.Remove(GetPortId(index));
        }
     
        public void ClearAllConnect(int index = -1,bool onlyFlow=false)
        {
			if (index >= 0)
			{
				foreach (var connect in this[index].ConnectList.ToArray())
				{
					var port = Node.Graph.GetPort(connect);
					if (port == null) continue;
					if (!onlyFlow || port.IsFlow)
					{
						DisConnect(connect, index);
					}
				}
			}
			else
			{
				for (int i = 0; i < ConnectInfolist.Count; i++)
				{
					ClearAllConnect(i,onlyFlow);
				}
			}
        }

 
    }
    
    public static class QFlowKey
	{
		public const string FuncName = "funcName";
		public const string Object = "obj";
		public const string Asset = "asset";
		public const string Param = "param";
		public const string FromPort = "#From";
		public const string NextPort = "#Next";
		public const string ReturnPort = "return";
        public const string This = "This";
		public const string Node = "Node";

	}
	public enum QNodeState
	{
		闲置,
		运行,
		成功,
		失败,
	}
    public class QFlowNode:IKey<string>
    {
        public override string ToString()
        {
            return "(" + commandKey + ")";
		}
	
		[Flags]
        public enum ReturnType
        {
            Void,
            ReturnValue,
            CoroutineDelay,
            TaskDelayVoid,
            TaskDelayValue
        }
		[QIgnore]
		public QNodeState State { get; set; } = QNodeState.闲置;
        [QIgnore]
        public QFlowGraph Graph { private set; get; }
        [QIgnore]
        public ReturnType returnType { private set; get; }= ReturnType.Void;
        [QIgnore]
        public List<QFlowPort> OutParamPorts = new List<QFlowPort>();
		public bool IsRunning => Graph.RunningNodeList.Contains(Key);
		public string Name { get; set; }
		public string Key { get;  set; } = QId.NewId();
		public string ViewName { 
            get
            {
                switch (returnType)
                {
                    case ReturnType.CoroutineDelay:
                        return Name + " (协程)";
                    case ReturnType.TaskDelayValue:
                    case ReturnType.TaskDelayVoid:
                        return Name + " (线程)";
                    default:
                        return Name;
                }
            }
        }
		[QName]
        public string commandKey { get; private set; }
        public Rect rect = new Rect(Vector2.zero, new Vector2(320, 80));
		public object this[string valueKey]
        {
            get
            {
				if (Ports.ContainsKey(valueKey))
				{
					return Ports[valueKey].Value;
				}
				else
				{
					return Graph.Values[Key + "." + valueKey];
				}
            }
            set
            {
				if (Ports.ContainsKey(valueKey))
				{
					Ports[valueKey].Value = value;
				}
				else
				{
					Graph.Values[Key + "." + valueKey] = value;
				}
            }
        }
        [QIgnore]
        public QCommandInfo command { get; private set; }
		[QIgnore]
		private MethodInfo NodeToInfoString { get; set; }
		[QIgnore]
		private MethodInfo NodeToFloat { get; set; }
		[QIgnore]
        public List<PortId> RunningPortList { get; private set; } = new List<PortId>();
        public QFlowNode()
        {
        }
        public QFlowNode(string commandKey)
        {
            this.commandKey = commandKey;
        }
        public QFlowPort AddPort(string key, Attribute portAttribute , string name="",Type type=null,QFlowPortAttribute FlowPort=null)
        {
            
            if (type == null)
            {
                type = QFlow.Type;
            }

            var typeInfo = QSerializeType.Get(type);


            if (!Ports.ContainsKey(key))
            {
                Ports.Set(key, new QFlowPort());
            }
            var port = Ports[key];
            if (string.IsNullOrEmpty(name))
            {
                port.name = key;
            }
            else
            {
                port.name = name;
            }
            port.Key = key;
            port.ValueType = type;
			port.IsOutput = portAttribute is QOutputPortAttribute;
            port.FlowPort = FlowPort ?? (( typeInfo.ElementType==QFlow.Type) ? QFlowPortAttribute.Normal : null);
			port.IsList = typeInfo.IsList || typeInfo.IsArray;

			port.ConnectType = typeInfo.ElementType;
			port.InputPort = portAttribute as QInputPortAttribute;
			port.OutputPort = portAttribute as QOutputPortAttribute;
            port.Init(this);
            return port;
        }
        public bool Init(QFlowGraph graph)
        {
            this.Graph = graph;
			NodeToInfoString = null;
			NodeToFloat = null;
			if (command != null) return true;
            command = QCommand.GetCommand(commandKey);
            if (command == null)
            {
                foreach (var port in Ports)
                {
                    port.Init(this);
                }
                Debug.LogError("不存在命令【" + commandKey + "】");
                return false; 
            }
			NodeToInfoString = command.method.DeclaringType.GetStaticMethod(nameof(ToInfoString));
			NodeToFloat = command.method.DeclaringType.GetStaticMethod(nameof(ToFloat));
			if (Name.IsNull())
			{
				Name = command.name.SplitEndString("/");
			}
			if (command.method.GetAttribute<QStartNodeAttribute>() == null)
			{
				AddPort(QFlowKey.FromPort, QInputPortAttribute.Normal);
			}
			else
			{
				Graph.StartNode[Name] = this;
			}
			if (command.method.GetAttribute<QEndNodeAttribute>() == null)
			{
				AddPort(QFlowKey.NextPort, QOutputPortAttribute.Normal);
			}
            commandParams = new object[command.paramInfos.Length];
            OutParamPorts.Clear();
            for (int i = 0; i < command.paramInfos.Length; i++)
            {
                var paramInfo = command.paramInfos[i];
				switch (paramInfo.Name)
				{
					case QFlowKey.This:
					case QFlowKey.Node:
						continue;
					default:
						break;
				}
				Attribute attribute = paramInfo.GetAttribute<QOutputPortAttribute>() ;
				if (attribute == null)
				{
					attribute = paramInfo.GetAttribute<QInputPortAttribute>();
				}
				if (attribute == null)
				{
					attribute = paramInfo.IsOut ? (Attribute)QOutputPortAttribute.Normal :(Attribute) QInputPortAttribute.Normal;
				}
                var port = AddPort(paramInfo.Name, attribute, QReflection.QName(paramInfo), paramInfo.ParameterType.GetTrueType(), paramInfo.GetAttribute<QFlowPortAttribute>());
                port.paramIndex = i;
                port.NameAttribute = paramInfo.GetAttribute<QNodeNameAttribute>();
				port.parameterInfo = paramInfo;
				if (paramInfo.HasDefaultValue)
                {
					if(string.IsNullOrEmpty( port.StringValue ))
					{
						port.Value = paramInfo.DefaultValue;
					}
                } 
                if (port.IsOutput)
                {
                    if (port.OutputPort.autoRunNode)
                    {
                        Ports.RemoveKey(QFlowKey.FromPort);
                        Ports.RemoveKey(QFlowKey.NextPort);
                    }
                    else if(port.IsFlow)
                    {
						Ports.RemoveKey(QFlowKey.NextPort);
                    }
                    else
                    {
                        if (paramInfo.IsOut ||( Key != QFlowKey.ReturnPort && !port.ValueType.IsValueType))
                        {
                            OutParamPorts.Add(port);
                        }
                    }
				}
				else
				{
					if (port.IsFlow)
					{
						Ports.RemoveKey(QFlowKey.FromPort);
					}
				}
				if (port.NameAttribute != null)
				{
					if (!port.StringValue.IsNull())
					{
						if (port.StringValue.Contains("/") && port.StringValue.Contains("."))
						{
							Name= port.StringValue.SplitEndString("/").SplitStartString(".");
						}
						else
						{
							Name = port.StringValue.Trim('\"');
						}
					}
				}


			}
            if (command.method.ReturnType == typeof(void))
            {
                returnType = ReturnType.Void;
            }
            else if (command.method.ReturnType == typeof(IEnumerator))
            {
                returnType = ReturnType.CoroutineDelay;
            }
            else if (typeof(Task).IsAssignableFrom(command.method.ReturnType))
            {
                if (typeof(Task) == command.method.ReturnType)
                {
                    returnType = ReturnType.TaskDelayVoid;
                }
                else
                {
                    returnType = ReturnType.TaskDelayValue;
                    TaskReturnValueGet = command.method.ReturnType.GetProperty("Result").GetValue;
                    AddPort(QFlowKey.ReturnPort, QOutputPortAttribute.Normal, "结果", command.method.ReturnType.GetTrueType());
                }
            }
            else
            {
				var outputAtt = command.method.ReturnTypeCustomAttributes.GetAttribute<QOutputPortAttribute>() ??  QOutputPortAttribute.Normal;
				if (outputAtt.autoRunNode)
				{
					Ports.RemoveKey(QFlowKey.FromPort);
					Ports.RemoveKey(QFlowKey.NextPort);
				}
				AddPort(QFlowKey.ReturnPort, outputAtt, "结果", command.method.ReturnType.GetTrueType());
                returnType = ReturnType.ReturnValue; 
            } 
            Ports.RemoveAll((port) => port.Node == null);
			return true;
        }


		public string ToInfoString()
		{
			if (NodeToInfoString == null)
			{
				return Name;
			}
			else
			{
				return NodeToInfoString.Invoke(null, new object[] { this })?.ToString();
			}
		}

		public float ToFloat(Func<QFlowNode, float> toFloatFunc = null)
		{
			if (NodeToFloat != null)
			{
				if (NodeToFloat.GetParameters().Length > 1)
				{
					return (float)NodeToFloat.Invoke(null, new object[] { this, toFloatFunc });
				}
				else if (toFloatFunc != null)
				{
					return toFloatFunc(this);
				}
				else
				{
					return (float)NodeToFloat.Invoke(null, new object[] { this });
				}
			}
			else if (toFloatFunc != null)
			{
				return toFloatFunc(this);
			}
			else
			{
				return 0;
			}
		}
		internal PortId? NextNodePort
        {
            get
            {
                if (_nextFlowPort == null)
                {
					return Ports[QFlowKey.NextPort]?[0].FirstConnect;
                }
                else
                {
                    return Ports[_nextFlowPort.Value.port]?[_nextFlowPort.Value.index].FirstConnect;
                }
            }
        }
        public void ClearAllConnect()
        {
            foreach (var port in Ports)
            {
                port.ClearAllConnect();
            }
        }
		
		public QFlowNode SetNextNode(QFlowNode nextNode)
        {
            Ports[QFlowKey.NextPort].Connect(nextNode.Ports[QFlowKey.FromPort].GetPortId());
			return nextNode;
        }
		public QFlowNode AddNextNode(string commandKey)
		{
			return SetNextNode(Graph.AddNode(commandKey));
		}
		public QFlowNode ReplaceNode(QFlowNode newNode)
		{
			foreach (var port in Ports)
			{
				if (newNode.Ports.ContainsKey(port.Key))
				{
					for (int i = 0; i < port.ConnectInfolist.Count; i++)
					{
						var connectInfo = port.ConnectInfolist[i];
						foreach (var connect in connectInfo.ConnectList.ToArray())
						{
							newNode.Ports[port.Key].Connect(connect, i);
						}
					}
				}
			}
			Graph.NodeList.Replace(Graph.NodeList.IndexOf(this), Graph.NodeList.IndexOf(newNode));
			newNode.rect = rect;
			Graph.Remove(this);
			return newNode;
		}
		public QFlowNode ReplaceNode( string commandKey)
		{
			return ReplaceNode(Graph.AddNode(commandKey));
		}
		[QName]
        public QList<string, QFlowPort> Ports { get; private set; } = new QList<string, QFlowPort>();
        PortId? _nextFlowPort;
        public void SetNetFlowPort(string portKey,int listIndex=0)
        {
            if (!Ports.ContainsKey(portKey))
            {
                Debug.LogError(ViewName + "不存在端口[" + portKey + "]");
            }
			_nextFlowPort = Ports[portKey].GetPortId(listIndex);
        }
        object[] commandParams;
        static Func<object,object> TaskReturnValueGet;
		object InvokeCommand()
		{
			_nextFlowPort = null;
			for (int i = 0; i < command.paramInfos.Length; i++)
			{
				var info = command.paramInfos[i];
				switch (info.Name)
				{
					case QFlowKey.This:
					case QFlowKey.Node:
						commandParams[i] = this;
						break;
					default:
						var port = Ports[info.Name];
						if (port != null)
						{
							commandParams[i] = port.Value;
							if (!port.IsOutput && port.InputPort.CheckNull && commandParams[i].IsNull())
							{
								QDebug.LogError(Name + " " + info.Name + " 为空");
								return null;
							}
						}
						break;
				} 
			}
			return command.Invoke(commandParams);
		}
        internal void Run()
        {
            var returnObj = InvokeCommand();
            switch (returnType)
            {
                case ReturnType.ReturnValue:
                    Ports[QFlowKey.ReturnPort].Value = returnObj;
                    break;
                case ReturnType.CoroutineDelay:
                case ReturnType.TaskDelayVoid:
                case ReturnType.TaskDelayValue:
                    Debug.LogError(commandKey+" 等待逻辑无法自动运行");
                    break;
                default:
                    break;
            }
            foreach (var port in OutParamPorts)
            {
                port.Value = commandParams[port.paramIndex];
            }
        }
        internal void SetPortRunning(PortId port)
        {
            RunningPortList.AddCheckExist(port);
        }
        public void RunPort(string portKey,int index=0)
        {
			RunPortIEnumerator(portKey, index).Start(Graph.CoroutineList);
        }
		public QFlowNode GetPortConnectNode(string portKey, int index = 0)
		{
			if (Ports.ContainsKey(portKey))
			{
				return Ports[portKey].GetConnectNode(index);
			}
			else
			{
				return null;
			}
		}
		public QNodeState GetPortConnectState(string portKey, int index = 0)
		{
			var node = GetPortConnectNode(portKey, index);
			return node == null ? QNodeState.成功 : node.State;
		}
        public IEnumerator RunPortIEnumerator(string portKey, int index = 0)
		{
			if (Ports.ContainsKey(portKey))
            {
				var node = Ports[portKey].GetConnectNode(index);
				return Graph.RunIEnumerator(node?.Key);
            }
            else
            {
                Debug.LogError("不存在端口[" + portKey + "]");
                return null;
            }
        }
        public IEnumerator RunIEnumerator()
        {
			if (command == null)
			{
				Debug.LogError("不存在命令【" + commandKey + "】");
				yield break;
			}
			State = QNodeState.运行;
			object returnObj = null;
			try
			{
				returnObj = InvokeCommand();
			}
			catch (Exception e)
			{
				Debug.LogError(e);
				State = QNodeState.失败;
			}
            switch (returnType)
            {
                case ReturnType.ReturnValue:
                    Ports[QFlowKey.ReturnPort].Value= returnObj;
                    break;
                case ReturnType.CoroutineDelay:
                    yield return returnObj;
                    break;
                case ReturnType.TaskDelayVoid:
                case ReturnType.TaskDelayValue:
                    var task= returnObj as Task;
					if (task.Exception != null)
					{
						Debug.LogError(task.Exception);
						State = QNodeState.失败;
					}
                    while (!task.IsCompleted)
                    {
                        yield return QFlowGraph.Step;
                    }
                    if (returnType== ReturnType.TaskDelayValue)
                    {
                        Ports[QFlowKey.ReturnPort].Value= TaskReturnValueGet(returnObj);
                    }
                    break;
                default:
                    break;
            }
			if(State== QNodeState.运行)
			{
				State = QNodeState.成功;
			}
			foreach (var port in OutParamPorts)
            {
                port.Value = commandParams[port.paramIndex];
            }
        }
		public bool Is(string key)
		{
			if (command.method.Name == key)
			{
				return true;
			}
			return false;
		}

    }
}
