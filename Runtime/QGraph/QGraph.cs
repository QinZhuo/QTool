using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace QTool.Graph {
	public class Blackboard: QList<string, Var> {
		[QIgnore] public Blackboard parent { get; private set; }
		public void SetParent(Blackboard parent) {
			if (this == parent)
				return;
			if (this.parent != null) {
				this.parent.SetParent(parent);
			}
			else {
				this.parent = parent;
			}
		}
		public void Set<T>(string key, T value) {
			if (ContainsKey(key) || parent.ContainsKey(key)) {
				if (this[key] is Var<T> var) {
					var.value = value;
				}
				else {
					var varBase = this[key];
					if (varBase == null) {
						varBase = new Var<T>() { name = key };
						Add(varBase);
					}
					varBase.Set(value);
				}
			}
			else if (parent?.parent?.ContainsKey(key) == true) {
				parent.parent.Set(key, value);
			}
			else {
				Debug.LogError($"设置黑板变量失败[{key}] 不存在变量");
			}
		}
		public T Get<T>(string key) {
			if (ContainsKey(key)) {
				if (this[key] is Var<T> var) {
					return var.value;
				}
				else {
					return this[key].Get<T>();
				}
			}
			else if (parent != null) {
				return parent.Get<T>(key);
			}
			return default;
		}
		public override string ToString() {
			return $"{nameof(Blackboard)} {Count}";
		}
	}
	public class Var<T> : Var {
		[QIgnore]
		public override string Key { get => name; set => name = value; }
		public string name;
		public T value;
		public override void Set<TObject>(TObject obj) {
			if (obj is T newValue) {
				value = newValue;
			}
			else {
				value = obj.As<T>();
			}
		}
		public override TObject Get<TObject>() {
			if (value is TObject newValue) {
				return newValue;
			}
			else {
				return value.As<TObject>();
			}
		}
		public override string ToString() {
			return $"{name}:{value}";
		}
	}
	public abstract class Var : IKey<string> {
		public abstract string Key { get; set; }
		public abstract void Set<TObject>(TObject obj);
		public abstract TObject Get<TObject>();
	}
	[Serializable]
    public class QGraph: QSerializeObject<QGraph> 
	{

		#region 运行时引用
		private static QDictionary<string, QGraph> graphs = new QDictionary<string, QGraph>();
		public static QGraph Load(string path) {
			if (!graphs.ContainsKey(path)) {
				try {
					var data = string.Empty;
					if (path.StartsWith(Application.streamingAssetsPath)) {
						data = File.ReadAllText(path);
					}
					else {
						data = QObjectTool.GetObject<TextAsset>(path).text;
					}
					if (data.IsNull()) {
						return null;
					}
					var graph = data.ParseQData<QGraph>();
					graphs.Add(path, graph);
					if (path.Contains(QObjectTool.ResourcesKey)) {
						graph.Name = QObjectTool.GetResourcesPath(path);
					}
					else {
						graph.Name = path;
					}
				}
				catch (Exception e) {
					Debug.LogException(e);
					return null;
				}
			}
			return graphs[path];
		}
		[QIgnore] internal List<QGraphRuntime> runtimes = new List<QGraphRuntime>();

		[QIgnore] private Stack<QGraphRuntime> runtimePool = new Stack<QGraphRuntime>();
		public QGraphRuntime GetRuntime(QGraphAgent graphAgent) {
			if (runtimePool.Count == 0) {
				var runtime = QGraphRuntime.Get(this).Init(graphAgent);
				runtimes.Add(runtime);
				return runtime;
			}
			else {
				return runtimePool.Pop().Init(graphAgent);
			}
		}
		public void ReleaseRuntime(QGraphRuntime runtime) {
			runtime.OnRelease();
			runtimePool.Push(runtime);
		}
		#endregion
		public const string ext = ".QGraph";
		public enum GraphType {
			FlowGraph,
			BehaviorTree,
		}
		[QName] public GraphType Type = GraphType.FlowGraph;
		[QIgnore] public string Name { private set; get; }
		[QName] public Vector2 Position { get; set; }
		[QName, QOldName("NodeList")] public QList<string,QNode> Nodes { get; private set; } = new();
		[QName] public List<QGroup> Groups { get; private set; } = new();
		[QName] public Blackboard blackboard { get; private set; } = new();
		[QIgnore] public List<string> StartNodes = new();
		//[QIgnore] public QGraphAsset Asset { get; internal set; }
		public override string ToString() {
			return Name + "_" + GetHashCode();
		}
		public override void OnLoad() {
			base.OnLoad();
			StartNodes.Clear();
			foreach (var node in Nodes) {
				node.Init(this);
			}
			foreach (var runtime in runtimes) {
				runtime.Fresh(this);
			}
		}
		public void PreLoadUnityObjects() {
		 	foreach (var node in Nodes) {
		 		foreach (var port in node.Ports) {
		 			if (port.ValueType.Is(typeof(UnityEngine.Object))) {
		 				_ = port.DefaultValue;
		 			}
		 		}
		 	}
		 }
		public QNode GetNode(string key)
		{
			if (key.IsNull()) return null;
			if (Nodes.ContainsKey(key)) {
				return Nodes[key];
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
        public Connection GetConnection(PortId? portId)
        {
            if (portId == null) return null;
            var connectInfo= GetPort(portId)?[portId.Value.index];
			return connectInfo;
        }
		public QPort GetPort(PortId? portId)
		{
			if (portId == null) return null;
			return GetNode(portId.Value.node)?.Ports[portId.Value.port];
		}
		public void Remove(QNode node)
        {
            if (node == null) return;
			if (node.Ports.ContainsKey(QFlowKey.FromPort) && node.Ports.ContainsKey(QFlowKey.ChildPort)) {
				var from = node.Ports[QFlowKey.FromPort].Connections[0];
				var next = node.Ports[QFlowKey.ChildPort].Connections[0];
				foreach (var portId in from) {
					foreach (var nextPortId in next) {
						GetPort(portId)?.Connect(nextPortId, portId.index);
					}
				}
			}
            node.ClearAllConnect();   
            Nodes.Remove(node);
        }
        public QNode AddNode(string commandKey)
        {
            var newNode= Add(new QNode(commandKey));
			newNode.Position = new Vector2(300 * 1.1f * (Nodes.Count - 1), 0);
			return newNode;
        }
	
		public QNode AddNodeToEnd(string commandKey)
		{
			if (Nodes.Count > 0)
			{
				return Nodes.StackPeek().AddNextNode(commandKey);
			}
			else
			{
				return AddNode(commandKey);
			}
		}
		public void Parse(IList<QNode> nodes,Vector2 startPos)
        {
            var lastKeys = new List<string>();
            var keys = new List<string>();
            var offsetPos = Vector2.one * float.MaxValue;
            foreach (var node in nodes)
            {
				if (node == null)
					continue;
                offsetPos = new Vector2(Mathf.Min(offsetPos.x, node.Position.x), Mathf.Min(offsetPos.y, node.Position.y));
                lastKeys.Add(node.Key);
                node.Key = QTool.GetGuid();
                keys.Add(node.Key);
            }

            foreach (var node in nodes) {
				if (node == null)
					continue;
				node.Position = node.Position - offsetPos + startPos;
                foreach (var port in node.Ports)
                {
                    foreach (var connection in port.Connections)
                    {
                        var lastConnect = connection.ToArray();
                        connection.Clear();
                        foreach (var connect in lastConnect)
                        {
                            var keyIndex = lastKeys.IndexOf(connect.node);
                            if (keyIndex >= 0)
                            {
                               connection.Add(new PortId
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
        public void AddRange(IList<QNode> nodes)
        {
            foreach (var node in nodes)
            {
				if(node==null) continue;
                Add(node);
            }
        }
        public QNode Add(QNode node)
        {
            Nodes.Add(node);
            node.Init(this);
            return node;
		}
		public void Clear()
		{
			//Stop();
			foreach (var node in Nodes.ToArray())
			{
				Remove(node);
			}
		}
	}
	public class QGroup {
		public string name;
		public Rect rect;
	}
	public struct PortId
    {
        public string node;
        public string port;
        public int index;
		public string Key => $"{node} {port} {index}";
		internal PortId(QPort statePort, int index=0)
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
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Field, AllowMultiple = false)]
	public class QInputPortAttribute : Attribute {
		public static QInputPortAttribute Normal = new QInputPortAttribute();

		public QInputPortAttribute() {
		}
		public static implicit operator bool(QInputPortAttribute d) => d != null;
	}
	/// <summary>
	///  指定参数端口为输出端口
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter| AttributeTargets.ReturnValue| AttributeTargets.Field, AllowMultiple = false)]
    public class QOutputPortAttribute : Attribute
	{
        public static QOutputPortAttribute Normal = new QOutputPortAttribute();

		public bool autoRunNode=false;

		public QOutputPortAttribute(bool autoRunNode=false)
        {
			this.autoRunNode = autoRunNode;
        }
		public static implicit operator bool(QOutputPortAttribute d) => d != null;
	}
    /// <summary>
    /// 指定参数端口为流程端口
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter| AttributeTargets.Field, AllowMultiple = false)]
    public class QFlowPortAttribute : Attribute
	{
        public static QFlowPortAttribute Normal = new QFlowPortAttribute();
		//public bool fixedShowValue = false;
		public bool onlyOneConnection = false;
		public QFlowPortAttribute()
		{
		}
		public static implicit operator bool(QFlowPortAttribute d) => d != null;
	}
    ///// <summary>
    ///// 指定函数节点为起点节点 即没有流程输入端口 节点Key为函数名
    ///// </summary>
    //[AttributeUsage(AttributeTargets.Method|AttributeTargets.Class, AllowMultiple = false)]
    //public class QStartNodeAttribute : Attribute
    //{
    //    public QStartNodeAttribute()
    //    {
    //    }
    //}
	/// <summary>
	/// 指定函数节点为结束节点 即没有流程输出端口
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
	public class QEndNodeAttribute : Attribute
	{
		public QEndNodeAttribute()
		{
		}
	}
	public struct QFlow
    {
        public static Type Type = typeof(QFlow);
//#if UNITY_EDITOR
//		static QFlow() {
//			QUIElements.TypeOverride.Add(typeof(QFlow), (name, obj, chaneEvent) => {
//				return new UnityEngine.UIElements.Label(name);
//			});
//		}
//#endif
		public override string ToString() {
			return nameof(QFlow);
		}
	}
	public class Connection:QList<PortId> {
		public void ChangeKey(int oldKey, int newKey, QPort port) {
			var newId = port.GetPortId(newKey);
			var oldId = port.GetPortId(oldKey);
			foreach (var portId in this) {
				var list = port.Node.Graph.GetConnection(portId);
				list.Remove(oldId);
				list.Add(newId);
			}
		}
		public void SortByPosition(QGraph graph) {
			RemoveAll(id => !graph.Nodes.ContainsKey(id.node));
			switch (graph.Type) {
				case QGraph.GraphType.FlowGraph:
					Sort((a,b) =>graph.Nodes[a.node].Position.y.CompareTo(graph.Nodes[b.node].Position.y));
					break;
				case QGraph.GraphType.BehaviorTree:
					Sort((a, b) => graph.Nodes[a.node].Position.x.CompareTo(graph.Nodes[b.node].Position.x));
					break;
				default:
					break;
			}
		}
		
		public override string ToString() {
			return this.ToOneString();
		}
	}
	public class QPort : IKey<string>
	{
		public override string ToString() {
			return $"{Node.name}.{name} ({ConnectType})[{Key}]";
		}
		[QName] public string Key { get; set; }
		[QName] public string StringValue { get; internal set; }
		[QName] public QList<Connection> Connections { private set; get; } = new QList<Connection>(()=>new Connection());
		[QIgnore] public bool IsList { get; internal set; }
		public Connection this[int index] {
			get {
				return Connections[index];
			}
		}
		[QIgnore] public string name { get; set; }
		public string ViewName => name;
		[QIgnore] public Type ConnectType { internal set; get; }
		[QIgnore] public ICustomAttributeProvider attributeProvider;
		[QIgnore] public QOutputPortAttribute OutputPort;
		[QIgnore] public QInputPortAttribute InputPort;
		[QIgnore] public QFlowPortAttribute FlowPort;
		[QIgnore] public QPortInfo Info { get; internal set; }
		[QIgnore] public Type ValueType { internal set; get; }
		[QIgnore] public QNode Node { get; internal set; }
		[QIgnore] internal object m_defaultValue;
		[QIgnore]
		public object DefaultValue
        {
            get
            {
                if (ValueType == QFlow.Type|| Node.Info==null) return null;
				if (m_defaultValue.IsNull())
				{
					m_defaultValue = StringValue.ParseQDataType(ValueType, m_defaultValue);
				}
				return m_defaultValue;
            }
			set
			{
				if (ValueType == QFlow.Type || Node.Info == null) return;
				//if (!IsList && Equals(m_defaultValue, value)) return;
				if (m_defaultValue.IsNull() && value.IsNull()) return;
				m_defaultValue = value; 
                StringValue = value.ToQDataType(ValueType);
            }
		}
		public bool IsShowValue(int index = 0) {
			if (FlowPort) {
				return true;
			}
			if (!OutputPort && !HasConnect(index)) {
				return true;
			}
			return false;
		}

		public void IndexChange(int a, int b) {
			var list = DefaultValue as IList;
			if (a < 0) {
				Connections.Insert(b, new Connection());
				for (int i = b; i < list.Count; i++) {
					var c = this[i];
					c.ChangeKey(i - 1, i, this);
				}
			}
			else if (b < 0) {
				ClearAllConnect(a);
				Connections.RemoveAt(a);
				for (int i = a; i < list.Count; i++) {
					var c = this[i];
					c.ChangeKey(i + 1, i, this);
				}
			}
		}
		public bool HasConnect(int index = 0) {
			{
				return this[index].Count > 0;
			}
		}
		public void SortConnections() {
			foreach (var connection in Connections) {
				connection.SortByPosition(Node.Graph);
			}
		}
		public PortId GetPortId(int index = 0)
		{
			return new PortId(this, index);
		}
		public void Init(QNode node)
        {
			Node = node;
            if (IsList && DefaultValue is IList list)
            {
                Connections.RemoveAll((obj) => Connections.IndexOf(obj)>= list.Count|| Connections.IndexOf(obj) < 0); 
            }
        }

		public bool CanConnect(QPort port) {
			if (OutputPort && port.OutputPort)
				return false;
			if (InputPort && port.InputPort)
				return false;
			if (FlowPort && port.FlowPort)
				return true;
			return ConnectType.CanConnect(port.ConnectType);
		}
		public void Connect(PortId? portId, int index = 0) {
			if (portId == null)
				return;
			var targetPort = Node.Graph.GetPort(portId);
			if (targetPort == null)
				return;
			if (!CanConnect(targetPort)) {
				Debug.LogError($"不能将 {Node} {this} {index} 连接 {targetPort.Node} {targetPort}");
				return;
			}
			if (FlowPort && targetPort.FlowPort) {
				//if (IsOutput)
				//{
				//	ClearAllConnect(index,true);
				//}
			}
			else {
				if (OutputPort) {
					targetPort.ClearAllConnect(portId.Value.index);
				}
				else {
					ClearAllConnect(index);
				}
			}
			this[index].AddCheckExist(portId.Value);

			targetPort[portId.Value.index].AddCheckExist(GetPortId(index));

			this[index].SortByPosition(Node.Graph);
			targetPort[portId.Value.index].SortByPosition(Node.Graph);
		}
     
        public void DisConnect(PortId? connect, int index = 0)
        {
            if (connect == null) return;
            this[index].Remove(connect.Value);
            var port = Node.Graph.GetPort(connect);
            if (port==null)
            {
				Debug.LogError("不存在端口[" + connect + "]");
                return;
            }
            port[connect.Value.index]?.Remove(GetPortId(index));
        }
     
        public void ClearAllConnect(int index = -1,bool onlyFlow=false)
        {
			if (index >= 0)
			{
				foreach (var connect in this[index].ToArray())
				{
					var port = Node.Graph.GetPort(connect);
					if (port == null) continue;
					if (!onlyFlow || port.FlowPort)
					{
						DisConnect(connect, index);
					}
				}
			}
			else
			{
				for (int i = 0; i < Connections.Count; i++)
				{
					ClearAllConnect(i,onlyFlow);
				}
				Connections.Clear();
			}
        }

 
    }
    public static class QFlowKey
	{
		public const string FromPort = "#From";
		public const string NextPort = "#Next";
		public const string ChildPort = "child";
		public const string ReturnPort = "result";

	}
	public enum QNodeState {
		success = 0,
		fail = 1,
		running = 2,
		init = 3,
	}
    public class QNode:IKey<string>
    {
        public override string ToString()
        {
            return "(" + commandKey + ")";
		}
		[QName] public string Key { get;  set; } = QTool.GetGuid();
		[QName] public string commandKey { get; private set; }
		[QName] public Vector2 Position { get; set; }
		[QName] public bool Breakpoint { get; set; }
		[QName] public QList<string, QPort> Ports { get; private set; } = new QList<string, QPort>();
		[QIgnore] public QGraph Graph { private set; get; }
		[QIgnore] public string name { get; set; }
		[QIgnore] public QNodeInfo Info { get; private set; }
		public QNode()
        {
        }
        public QNode(string commandKey)
        {
            this.commandKey = commandKey;
        }
		public QPort AddPort(string key, string name = "", Type type = null, QFlowPortAttribute FlowPort = null) {

			if (type == null) {
				type = QFlow.Type;
			}

			var typeInfo = QSerializeType.Get(type);


			if (!Ports.ContainsKey(key)) {
				Ports.Set(key, new QPort());
			}
			var port = Ports[key];
			if (string.IsNullOrEmpty(name)) {
				port.name = key;
			}
			else {
				port.name = name;
			}
			port.Key = key;
			port.ValueType = type;
			port.FlowPort = FlowPort ?? ((typeInfo.ElementType == QFlow.Type) ? QFlowPortAttribute.Normal : null);
			port.IsList = typeInfo.IsList || typeInfo.IsArray;
			port.ConnectType = typeInfo.ElementType;
			port.Init(this);
			return port;
		}
		public bool Init(QGraph graph) {
			this.Graph = graph;
			Info = QCommand.GetCommand(commandKey);
			if (Info == null) {
				foreach (var port in Ports) {
					port.Init(this);
				}
				Debug.LogWarning(graph.Name + " 不存在命令【" + commandKey + "】");
				return false;
			}
			if (name.IsNull()) {
				name = Info.ViewName.SplitEndString("/");
			}
			if (Info.type.Is(typeof(StartNode))) {
				Graph.StartNodes.Add(Key);
			}
			else {
				var port = AddPort(QFlowKey.FromPort);
				port.InputPort = QInputPortAttribute.Normal;
			}
			if (Info.type.GetAttribute<QEndNodeAttribute>() == null) {
				var port = AddPort(QFlowKey.NextPort);
				port.OutputPort = QOutputPortAttribute.Normal;
			}
			for (int i = 0; i < Info.Ports.Count; i++) {
				var portInfo = Info.Ports[i];
				var port = AddPort(portInfo.Key, portInfo.ViewName, portInfo.ParameterType.GetTrueType(), portInfo.attributeProvider.GetAttribute<QFlowPortAttribute>());
				port.OutputPort = portInfo.attributeProvider.GetAttribute<QOutputPortAttribute>() ?? portInfo.OutputPort;
				port.InputPort = portInfo.attributeProvider.GetAttribute<QInputPortAttribute>() ?? portInfo.InputPort;
				port.Info = portInfo;
				port.attributeProvider = portInfo.attributeProvider;
				if (portInfo.HasDefaultValue) {
					if (string.IsNullOrEmpty(port.StringValue)) {
						port.DefaultValue = portInfo.DefaultValue;
					}
				}
				if (port.OutputPort) {
					if (port.OutputPort.autoRunNode) {
						Ports.RemoveKey(QFlowKey.FromPort);
						Ports.RemoveKey(QFlowKey.NextPort);
					}
					else if (port.FlowPort) {
						Ports.RemoveKey(QFlowKey.NextPort);
					}
				}
				else {
					if (port.FlowPort) {
						Ports.RemoveKey(QFlowKey.FromPort);
					}
				}

			}
			Ports.RemoveAll((port) => port.Node == null);
			return true;
		}

	
	
        public void ClearAllConnect()
        {
            foreach (var port in Ports)
            {
                port.ClearAllConnect();
            }
        }

		public QNode SetNextNode(QNode nextNode) {
			foreach (var port in Ports) {
				if (port.OutputPort && port.FlowPort) {
					if (port.IsList) {
						var count = (port.DefaultValue as IList).Count;
						for (int i = 0; i < count; i++) {
							port.Connect(nextNode.Ports[QFlowKey.FromPort].GetPortId(), i);
						}
					}
					else {
						port.Connect(nextNode.Ports[QFlowKey.FromPort].GetPortId());
					}
				}
			}
			return nextNode;
		}
		public QNode AddNextNode(string commandKey)
		{
			return SetNextNode(Graph.AddNode(commandKey));
		}
		public QNode ReplaceNode(QNode newNode) {
			newNode.Position = Position;
			newNode.Key = Key;
			foreach (var port in Ports) {
				if (newNode.Ports.ContainsKey(port.Key)) {
					var newPort = newNode.Ports[port.Key];
					if (newPort.ValueType == port.ValueType) {
						newPort.DefaultValue = port.DefaultValue;
					}
					for (int i = 0; i < port.Connections.Count; i++) {
						var connectInfo = port.Connections[i];
						foreach (var portId in connectInfo.ToArray()) {
							newPort[i].Add(portId);
						}
					}
				}
				else {
					port.ClearAllConnect();
				}
			}
			Graph.Nodes.Replace(Graph.Nodes.IndexOf(this), Graph.Nodes.IndexOf(newNode));
			Graph.Nodes.Remove(this);
			return newNode;
		}
		public QNode ReplaceNode( string commandKey)
		{
			return ReplaceNode(Graph.AddNode(commandKey));
		}
    }
	public static class GraphTool {
		public static QDictionary<Type, List<Type>> CanConnectList = new QDictionary<Type, List<Type>>()
	{
			{
				 typeof(int),
				 new List<Type>{typeof(float),typeof(double)}
			}
		};
		public static bool CanConnect(this Type fromType, Type type) {
			if (fromType == null)
				return false;
			if (fromType == type) {
				return true;
			}
			else if (fromType != QFlow.Type && type != QFlow.Type) {
				if (fromType == typeof(object)) {
					return true;
				}
				else if (type == typeof(object)) {
					return true;
				}
				else if (fromType.Is(type)) {
					return true;
				}
				else if (fromType.Is(typeof(Component)) && type.Is(typeof(Component))) {
					return true;
				}
				else if (fromType == typeof(GameObject) && type.Is(typeof(Component))) {
					return true;
				}
				else if (type == typeof(GameObject) && fromType.Is(typeof(Component))) {
					return true;
				}
				else if (CanConnectList.ContainsKey(fromType)) {
					return CanConnectList[fromType].Contains(type);
				}
			}
			return false;
		}
	}
}
