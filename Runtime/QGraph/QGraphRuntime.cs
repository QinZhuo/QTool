using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using QTool.Reflection;
using System.Reflection;
using System.Threading.Tasks;

#if UNITY_EDITOR
using QTool.Inspector;
using UnityEditor;
#endif
namespace QTool.Graph {
	public class QGraphRuntime {
		[QIgnore]
		public QGraphAgent Agent { get; private set; }
		[QIgnore]
		public QGraph Graph { get; internal set; }
		[QName]
		internal List<string> runningList = new();
		[QName]
		private List<string> removeList = new();
		public bool IsRunning => runningList.Count > 0;
		[QName]
		public QDictionary<string, QNodeRuntime> Nodes { get; private set; }
		[QName]
		public QDictionary<string, QGraphRuntime> SubgraphRuntimes { get; private set; }
		[QName]
		public Blackboard blackboard { get; private set; }
		private event Action<bool> onStop = null;
		public QGraphRuntime() {

		}

		internal void OnRelease() {
			Stop(false);
			Agent = null;
			foreach (var node in Nodes) {
				node.Value?.OnRelease();
			}
			foreach (var graph in SubgraphRuntimes) {
				graph.Value.OnRelease();
			}
			blackboard.Clear();
		}
		public T GetVar<T>(string key) {
			return blackboard.Get<T>(key);
		}
		public void SetVar<T>(string key, T value) {
			blackboard.Set(key, value);
		}
		internal static QGraphRuntime Get(QGraph graph) {
			var runtime = new QGraphRuntime();
			runtime.Graph = graph;
			if (runtime.Nodes == null) {
				runtime.Nodes = new(key => QNodeRuntime.Get(runtime, runtime.Graph.GetNode(key)));
			}
			if (runtime.SubgraphRuntimes == null) {
				runtime.SubgraphRuntimes = new(key => {
					if (runtime.Nodes[key] is Subgraph subgraph) {
						var subgraphRuntime = QGraph.Load(subgraph.graphAsset).GetRuntime(runtime.Agent);
						subgraphRuntime.blackboard.SetParent(runtime.blackboard);
						return subgraphRuntime;
					}
					else {
						Debug.LogError($"{runtime.Nodes[key]?.GetType()} not is {nameof(Subgraph)}");
						return null;
					}
				});
			}
			runtime.blackboard = new Blackboard();
			runtime.blackboard.SetParent(graph.blackboard);
			return runtime;
		}
		public QGraphRuntime Init(QGraphAgent graphAgent) {
			Agent = graphAgent;
			foreach (var graph in SubgraphRuntimes) {
				graph.Value.Init(graphAgent);
			}
			return this;
		}
		public void Start(string nodeKey = null) {
			if (nodeKey == null) {
				if (Graph.StartNodes.Count == 0) {
					InternalStop();
				}
				else {
					foreach (var key in Graph.StartNodes) {
						Start(key);
					}
				}
			}
			else {
				var node = Nodes[nodeKey];
				if (node?.Node == null) {
					Debug.LogError($"{Graph.Name}找不到节点{nodeKey}");
					return;
				}
				node.Start();
			}
		}
		//public void Start<T>(string key, T lastValue, object value) {
		//	var node = Nodes[Graph.GetNode(key).Key];
		//	if (node?.Node == null) {
		//		Debug.LogError($"{Graph.Name}找不到节点{key}");
		//		return;
		//	}
		//	node.SetValue(nameof(lastValue), lastValue);
		//	node.SetValue(nameof(value), value);
		//	node.Start();
		//}
		public string Save() {
			return this.ToQData();
		}
		public void Load(string data) {
			Stop(false);
			data.ParseQData(this);
			Fresh(Graph);
		}
		public void Fresh(QGraph graph) {
			this.Graph = graph;
			foreach (var node in Nodes) {
				if (Graph.Nodes.ContainsKey(node.Key)) {
					node.Value.Fresh(this, Graph.Nodes[node.Key]);
				}
				if (node.Value is Subgraph subgraphNode) {
					if (SubgraphRuntimes.ContainsKey(node.Key)) {
						SubgraphRuntimes[node.Key].Fresh(QGraph.Load(subgraphNode.graphAsset));
					}
				}
			}
		}
		public void OnStop(Action<bool> onEnd) {
			onStop += onEnd;
		}
		public void Release() {
			Graph.ReleaseRuntime(this);
		}
		public void Stop(bool success = true) {
			if (!IsRunning) {
				return;
			}
			InternalStop(success);
		}
		private void InternalStop(bool success = true) {
			foreach (var runningKey in runningList) {
				if (SubgraphRuntimes.ContainsKey(runningKey)) {
					SubgraphRuntimes[runningKey].Stop(success);
				}
				Nodes[runningKey].End(success);
			}
			runningList.Clear();
			onStop?.Invoke(success);
			onStop = null;
		}

		public void Update() {
			if (!IsRunning)
				return;
			Profiler.BeginSample($"GraphUpdate");
			for (int i = 0; i < runningList.Count; i++) {
				var node = Nodes[runningList[i]];
				if (node.Node.Breakpoint)
					continue;
				Profiler.BeginSample($"NodeUpdate");
				try {
					if (node.State == QNodeState.running) {
						if (node is INodeUpdate nodeUpdate) {
							nodeUpdate.OnUpdate();
						}
					}
				}
				catch (Exception e) {
					Debug.LogException(e);
					node.End(false);
				}
				switch (node.State) {
					case QNodeState.success:
					case QNodeState.fail:
						if (node.NextConnection != null) {
							foreach (var portId in node.NextConnection) {
								Start(portId.node);
							}
						}
						Profiler.EndSample();
						removeList.Add(node.Node.Key);
						continue;
					default:
						break;
				}
				Profiler.EndSample();
				continue;
			}
			foreach (var key in removeList) {
				runningList.Remove(key);
			}
			removeList.Clear();
			Profiler.EndSample();
			if (runningList.Count == 0) {
				InternalStop(true);
			}
		}
		public bool Contains(QGraphRuntime runtime) {
			if (this == runtime)
				return true;
			foreach (var subGraph in SubgraphRuntimes) {
				if (subGraph.Value.Contains(runtime)) {
					return true;
				}
			}
			return false;
		}

	}

	public abstract class QNodeRuntime {
		[QIgnore]
		public QGraphRuntime Graph { get; private set; }
		[QIgnore]
		public QNode Node { get; internal set; }
		[QIgnore]
		public QDictionary<string, QPortRuntime> Ports { get; private set; }
		[QName]
		public QNodeState State { get; internal set; } = QNodeState.init;
		private PortId? _nextFlowPort;
		//public void SetValue(string name, object obj) {
		//	Ports[name].SetValue(obj);
		//}
		//public object GetValue(string name) {
		//	return Ports[name].GetValue();
		//}
		internal Connection NextConnection {
			get {
				if (_nextFlowPort == null) {
					if (Ports.ContainsKey(QFlowKey.NextPort)) {
						return Ports[QFlowKey.NextPort].Port.Connections.Get(0);
					}
					else {
						return null;
					}
				}
				else {
					return Ports[_nextFlowPort.Value.port].Port.Connections.Get(_nextFlowPort.Value.index);
				}
			}
		}
		internal void OnRelease() {
			State = QNodeState.init;
			foreach (var port in Ports) {
				port.Value.OnRelease();
			}
			_nextFlowPort = null;
		}
		internal void Fresh(QGraphRuntime graph, QNode node) {
			if (node?.Info == null)
				return;
			Graph = graph;
			Node = node;
			Ports = new(key => QPortRuntime.Get(Node.Ports[key]));
			if (node.Info.method != null) {
				if (this is ReflectedMethodNode methodNode) {
					if (methodNode.methodParams?.Length != Node.Info.ParamCount) {
						methodNode.methodParams = new object[Node.Info.ParamCount];
					}
					foreach (var port in Ports) {
						if (port.Value.Port.Info.ParamIndex < 0)
							continue;
						methodNode.methodParams[port.Value.Port.Info.ParamIndex] = port.Value.GetValue();

					}
					foreach (var port in Node.Ports) {
						if (port.Info == null)
							continue;
						var portRuntime = Ports[port.Key];
						switch (port.Key) {
							case nameof(ReflectedMethodNode.result):
								portRuntime.GetValue = () => methodNode.result;
								break;
							case nameof(ReflectedMethodNode.obj):
								portRuntime.SetValue = value => methodNode.obj = value;
								break;
							default:
								portRuntime.SetValue = value => methodNode.methodParams[port.Info.ParamIndex] = value;
								portRuntime.GetValue = () => methodNode.methodParams[port.Info.ParamIndex];
								break;
						}
					}
				}
			}
			else {
				foreach (var port in Node.Ports) {
					var portRuntime = Ports[port.Key];
					if (port.Info != null) {
						if (port.Info.SetValue != null) {
							portRuntime.SetValue = value => { port.Info.SetValue(this, value); };
						}
						if (port.Info.GetValue != null) {
							portRuntime.GetValue = () => port.Info.GetValue(this);
						}
					}
				}
			}
			foreach (var port in Ports) {
				if (port.Value.SetValue != null) {
					port.Value.SetValue(port.Value.Port.DefaultValue);
				}
			}

		}
		internal static QNodeRuntime Get(QGraphRuntime graph, QNode node) {
			if (node?.Info == null)
				return null;
			if (node.Info.type?.ContainsGenericParameters == true) {
				return null;
			}
			var runtime = node.Info.method != null ? new ReflectedMethodNode() : Activator.CreateInstance(node.Info.type) as QNodeRuntime;
			runtime.Fresh(graph, node);
			runtime._nextFlowPort = null;
			runtime.State = QNodeState.init;
			return runtime;
		}
		public void Start() {
			if (Node == null) {
				return;
			}
			try {

				if (State == QNodeState.running)
					return;
				if (Node.Breakpoint) {
					State = QNodeState.running;
					Action action= async () => {
						while (Node.Breakpoint) {
							await Task.Yield();
						}
						State = QNodeState.init;
						Start();
					};
					return;
				}
				_nextFlowPort = null;
				foreach (var item in Ports) {
					item.Value.FreshValue(Graph);
				}
				State = QNodeState.running;
				Graph.runningList.Add(Node.Key);
				OnStart();
			}
			catch (Exception e) {
				Debug.LogException(e);
				End(false);
			}
		}
		protected abstract void OnStart();
		protected virtual void OnEnd() { }
		public void End() {
			End(true);
		}
		public void End(bool success) {
			if (State != QNodeState.running)
				return;
			State = success ? QNodeState.success : QNodeState.fail;
			OnEnd();
		}
		public void SetNetFlowPort(string portKey, int listIndex = 0) {
			if (!Node.Ports.ContainsKey(portKey)) {
				Debug.LogError(Node.name + "不存在端口[" + portKey + "] " + Node.Ports.ToOneString(" ", port => port.name));
			}
			_nextFlowPort = Node.Ports[portKey].GetPortId(listIndex);
		}
		public void RunPort(string portKey, int index = 0) {
			foreach (var portId in Node.Ports[portKey][index]) {
				Graph.Start(portId.node);
			}
		}
		public void FreshPortValue(string portKey) {
			Ports[portKey].FreshValue(Graph);
		}
		public override string ToString() {
			return $"{Node} {State}";
		}

		public float Time => Agent == null ? 0 : Agent.Time;
		public virtual string Description => null;
		public QGraphAgent Agent => Graph?.Agent;
	}
	public class QPortRuntime {
		public Action<object> SetValue;
		public Func<object> GetValue;
		public QPort Port { get; private set; }
		public QPortRuntime() {

		}
		internal static QPortRuntime Get(QPort port) {
			if (port == null)
				return null;
			var runtime = port.Info == null ? new QPortRuntime() : (QPortRuntime)typeof(QPortRuntime<>).MakeGenericType(port.Info.ParameterType).CreateInstance();
			runtime.Port = port;
			return runtime;
		}
		public void FreshValue(QGraphRuntime Graph) {
			if (Port.InputPort && !Port.FlowPort && Port.HasConnect()) {
				foreach (var connection in Port.Connections) {
					foreach (var portId in connection) {
						var fromNode = Graph.Nodes[portId.node];
						var fromPort = fromNode.Ports[portId.port];
						if (Graph.Graph.Type == QGraph.GraphType.BehaviorTree && fromNode is FunctionNode functionNode) {
							functionNode.Start();
						}
						else if (fromPort.Port.OutputPort.autoRunNode) {
							fromNode.Start();
						}
						SetValue(fromPort.GetValue());
					}
				}
			}
		}
		public override string ToString() {
			return $"{Port}:{GetValue?.Invoke()}";
		}
		internal void OnRelease() {
		}
	}
	public class QPortRuntime<T> : QPortRuntime {
		//public new Action<T> SetValue;
		//public new Func<T> GetValue;
		//public override string ToString() {
		//	return $"{Port}:{GetValue.Invoke()}";
		//}
	}
}