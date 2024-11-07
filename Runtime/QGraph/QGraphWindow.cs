#if UNITY_EDITOR
using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.UIElements;
using UnityEditor;
using System.Threading.Tasks;

namespace QTool.Graph {
	public class QGraphWindow : QFileEditorWindow<QGraphWindow> {
		#region 静态函数
		[UnityEditor.Callbacks.OnOpenAsset(0)]
		public static bool OnOpen(int instanceID, int line) {
			var obj = EditorUtility.InstanceIDToObject(instanceID);
			var path = AssetDatabase.GetAssetPath(obj);
			if (path.EndsWith(QGraph.ext)) {
				Open(path);
				return true;
			}
			else {
				return false;
			}
		}
		
		[MenuItem("Tools/窗口/QGraph")]
		public static void OpenWindow() {
			var window = GetWindow<QGraphWindow>();
			window.minSize = new Vector2(500, 300);
		}
		private static QGraphAgent _curAgent;
		public QGraphAgent CurAgent {
			get => _curAgent;
			set {
				if (_curAgent?.GetHashCode() != value?.GetHashCode()) {
					if (_curAgent != null) {
						_curAgent.onFresh -= OnSelectionChange;
					}
					_curAgent = value;
					agentLabel.text = _curAgent == null ? "NoAgent" : _curAgent.name;
					if (_curAgent != null) {
						_curAgent.onFresh += OnSelectionChange;
						if (_curAgent.Graphs.Count > 0) {
							foreach (var graph in CurAgent.Graphs) {
								if (CheckRuntime(graph)) {
									return;
								}
							}
							//agentLabel.text = "NoAgent";
							Open(_curAgent.Graphs[0].Graph.Name);
						}
					}
				}
			}
		}
		private bool CheckRuntime(QGraphRuntime runtime) {
			if (runtime.Graph.Name == Graph?.Name) {
				GraphRuntime = runtime;
				Fresh();
				return true;
			}
			else {
				foreach (var item in runtime.SubgraphRuntimes) {
					if (CheckRuntime(item.Value)) {
						return true;
					}
				}
			}
			return false;
		}
		public void OnSelectionChange() {
			if (Selection.activeGameObject != null) {
				CurAgent = Selection.activeGameObject.GetComponent<QGraphAgent>();
			}
		}
		#endregion
		public QGraphRuntime GraphRuntime { get; set; }
		private QGraph Graph { get; set; }
		public override void SetDataDirty() {
			Graph?.OnBeforeSerialize();
			Data = Graph?.ToQData();
		}
		private VisualElement ConnectCanvas { get; set; }
		private QNode CurrentSubNode { get; set; }
		protected override void OnChangeFile() {
			base.OnChangeFile();
			CurAgent = null;
		}
		protected override async void ParseData() {

			var path = FilePath;
			Graph = QGraph.Load(path);
			if (Graph == null)
				return;
			while (Back==null) {
				await Task.Yield();
			}
			toolbar.Q<EnumField>().value = Graph.Type;
			Back.Clear();
			NodeViewList.Clear();
			ConnectCanvas.Clear();
			foreach (var node in Graph.Nodes) {
				AddNodeView(Back, node);
			}
			GroupList.Clear();
			foreach (var group in Graph.Groups) {
				AddGroup(group);
			}
			FreshBlackBoard();
			breadcrumbs.Clear();
			foreach (var graph in GraphList) {
				breadcrumbs.PushItem(graph.SplitEndString("/"), () => {
					var index = GraphList.IndexOf(graph);
					if (index >= 0) {
						GraphList.RemoveRange(index + 1, GraphList.Count - index - 1);
					}
					FilePath = graph;
				});
			}
			OnSelectionChange();
		}
		public void FreshPortConnect(QPort port) {
			for (int portIndex = 0; portIndex < port.Connections.Count; portIndex++) {
				foreach (var connection in port.Connections[portIndex]) {
					PortId startId;
					PortId endId;
					if (port.OutputPort) {
						startId = port.GetPortId(portIndex);
						endId = connection;
					}
					else {
						startId = connection;
						endId = port.GetPortId(portIndex);
					}
					var start = GetDotView(startId);
					var end = GetDotView(endId);
					if (start != null && end != null) {
						var connectView = GetConnectView(startId, endId);
						if (connectView == null) {
							AddConnectView(startId, endId);
						}
						else {
							connectView.StartElement = start;
							connectView.EndElement = end;
						}
					}
				}
			}
		}
		VisualElement Back = null;
		Action OnZoom = null;
		VisualElement blackBoard = null;
		VisualElement selectRect = null;
		Label agentLabel = null;
		bool MoveOffset = false;
		float baseOffset = 10000;
		public Vector2 GraphPosition {
			get => Graph != null ? baseOffset * Vector2.one - Graph.Position : Vector2.zero;
			set {
				if (Graph != null) {
					Graph.Position = baseOffset * Vector2.one - value;
				}
			}
		}
		private void Focus(VisualElement view) {
			ClearSetect();
			if (NodeViewList.Contains(view)) {
				SelectNodes.AddCheckExist(view);
				FreshSelect(view);
			}
			Graph.Position = (Vector2)view.transform.position + view.layout.center - GraphPosition - rootVisualElement.contentRect.size / 2;
			FreshPositions();
			FreshConnections();
		}
		private Vector2 startPosition;
		private void CreateNew() {
			var path = EditorUtility.SaveFilePanelInProject("新建QGraph", "QGraph", "QGraph", "选择新建路径");
			if (!path.IsNull()) {
				QFileTool.Save(path, new QGraph().ToQData());
				AssetDatabase.Refresh();
				Open(path);
			}
		}
		protected override void CreateGUI() {
			base.CreateGUI();
			Selection.selectionChanged -= OnSelectionChange;
			Selection.selectionChanged += OnSelectionChange;
			toolbar.AddLabel("    ");
			agentLabel = toolbar.AddLabel("NoAgent");
			toolbar.AddEnum("", QGraph.GraphType.FlowGraph, newValue => { Graph.Type = (QGraph.GraphType)newValue; Fresh(); });
			toolbar.AddButton("新建", CreateNew);
			var searchField = new ToolbarSearchField();
			searchField.RegisterCallback<KeyDownEvent>(value => {
				if (value.keyCode != KeyCode.Return)
					return;
				foreach (var group in GroupList) {
					if (group.Q<Label>().text.ToLower().Contains(searchField.value.ToLower())) {
						Focus(group);
						return;
					}
				}
				var first = 0;
				if (SelectNodes.Count > 0) {
					first = NodeViewList.IndexOf(SelectNodes[0]) + 1;
				}
				for (int i = first; i < NodeViewList.Count; i++) {
					var nodeView = NodeViewList[i];
					var node = Graph.Nodes[nodeView.name];
					if (node.name.ToLower().Contains(searchField.value.ToLower())) {
						Focus(nodeView);
						return;
					}
				}
				ClearSetect();
			});
			toolbar.Add(searchField);
			var viewRange = rootVisualElement.AddVisualElement();
			viewRange.style.backgroundColor = Color.Lerp(Color.black, Color.white, 0.1f);
			viewRange.style.overflow = Overflow.Hidden;
			viewRange.style.height = new Length(100, LengthUnit.Percent);
			ConnectCanvas = viewRange.AddVisualElement().SetBackground();
			Back = viewRange.AddVisualElement().SetBackground(default, -baseOffset);
			selectRect = Back.AddVisualElement();
			selectRect.name = nameof(selectRect);
			selectRect.style.position = Position.Absolute;
			selectRect.style.SetBorder(Color.white, 1, 1);
			selectRect.pickingMode = PickingMode.Ignore;
			selectRect.visible = false;
			selectRect.RegisterCallback<GeometryChangedEvent>(data => {
				FreshSelect(selectRect.worldBound);
			});
			AddBlackBoard(viewRange);

			Back.RegisterCallback<MouseDownEvent>(data => {
				if (data.target == Back || GroupList.Contains(data.target)) {
					if (data.button == 2) {
						MoveOffset = true;
					}
					if (!data.ctrlKey) {
						ClearSetect();
					}
					if (data.button == 0) {
						if (selectRect.parent != Back) {
							Back.Add(selectRect);
						}
						startPosition = Back.WorldToLocal(data.mousePosition);
						selectRect.transform.position = startPosition;
						selectRect.style.width = 0;
						selectRect.style.height = 0;
						selectRect.visible = true;
						return;
					}
				}
			});
			Back.RegisterCallback<WheelEvent>(data => {
				Back.transform.scale = Mathf.Clamp(Back.transform.scale.y - data.delta.y * 0.01f, 0.1f, 1) * Vector3.one;
				FreshConnections();
				OnZoom?.Invoke();
			});
			Back.RegisterCallback<MouseDownEvent>(data => {
				if (data.target == Back || GroupList.Contains(data.target)) {
					AutoLoad = true;
				}
			});
			Back.RegisterCallback<MouseMoveEvent>(data => {
				if (data.button > 1)
					return;
				var mouseDelta = data.mouseDelta / Back.transform.scale;
				if (selectRect.visible) {
					var mousePos = Back.WorldToLocal(data.mousePosition);
					var min = Vector2.Min(mousePos, startPosition);
					var size = Vector2.Max(mousePos, startPosition) - min;
					selectRect.transform.position = min;
					selectRect.style.width = size.x;
					selectRect.style.height = size.y;
					return;
				}
				if (StartPortId != null) {
					if (DragConnect != null) {
						DragConnect.End = data.mousePosition;
					}
				}
				else if (MoveOffset) {
					if (SelectNodes.Count != 0) {
						if (SelectNodes.Count == 1 && data.ctrlKey) {
							ForeachChild(SelectNodes[0], node => MoveNode(node, mouseDelta));
						}
						else {
							foreach (var node in SelectNodes) {
								if (node == null)
									continue;
								MoveNode(node, mouseDelta);
							}
						}
					}
					else {
						if (Graph != null) {
							GraphPosition += mouseDelta;
							FreshPositions();
						}
					}
				}
				FreshConnections();
			});
			Back.RegisterCallback<MouseUpEvent>(data => {

				if (selectRect.visible) {
					if (data.ctrlKey) {
						AddGroup();
					}
					selectRect.visible = false;
					return;
				}
				MoveOffset = false;
				if (StartPortId != null && DragConnect != null) {
					FreshDotView(DragConnect.StartElement);
					if (ConnectCanvas.Contains(DragConnect)) {
						ConnectCanvas.Remove(DragConnect);
					}
					ConnectViewList.Remove(DragConnect);
					DragConnect = null;
				}
				StartPortId = null;
				SetDataDirty();

			});
			Back.RegisterCallback<MouseLeaveEvent>(data => {
				selectRect.visible = false;
				MoveOffset = false;
			});
			Back.RegisterCallback<DragUpdatedEvent>(data => {
				if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is TextAsset graphAsset) {
					DragAndDrop.visualMode = DragAndDropVisualMode.Link;
				}
			});
			Back.RegisterCallback<DragPerformEvent>(data => {
				if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is TextAsset graphAsset) {
					var path = QObjectTool.GetPath(graphAsset);
					if (!path.IsNull()) {
						DragAndDrop.AcceptDrag();
						var subGraphNode = Graph.AddNode(typeof(Subgraph).FullName);
						subGraphNode.Position = data.localMousePosition - GraphPosition;
						subGraphNode.Ports[nameof(Subgraph.graphAsset)].DefaultValue = path;
						AddNodeView(Back, subGraphNode);
					}
				}
			});
			Back.AddMenu(data => {
				if (Graph == null) {
					data.menu.AppendAction("新建", action => {
						CreateNew();
					});
					return;
				}
				if (data.target != Back) {
					return;
				}
				MoveOffset = false;
				var position = data.localMousePosition - GraphPosition;
				foreach (var kv in QCommand.KeyDictionary) {
					if (kv.Value == null)
						continue;
					if (kv.Value.type.Is(typeof(BTNode)) && Graph.Type != QGraph.GraphType.BehaviorTree)
						continue;
					data.menu.AppendAction(kv.Value.FullName, action => {
						var node = Graph.AddNode(kv.Key);
						node.Position = position;
						AddNodeView(Back, node);
					});
				}
				foreach (var graphAsset in Resources.LoadAll<TextAsset>(nameof(Subgraph))) {
					data.menu.AppendAction("子图/" + AssetDatabase.GetAssetPath(graphAsset).SplitEndString(nameof(Subgraph) + "/"), action => {
						var subGraphNode = Graph.AddNode(typeof(Subgraph).FullName);
						subGraphNode.Position = position;
						subGraphNode.Ports[nameof(Subgraph.graphAsset)].DefaultValue = QObjectTool.GetPath(graphAsset);
						AddNodeView(Back, subGraphNode);
					});
				}
				if (!GUIUtility.systemCopyBuffer.IsNull()) {
					data.menu.AppendSeparator();
					data.menu.AppendAction("粘贴", action => {
						Pause(position);
					});
				}
			});
		}
		private void AddGroup(QGroup group = null) {
			var groupView = Back.AddVisualElement();
			var titleBack = groupView.AddVisualElement();
			titleBack.style.width = new Length(100, LengthUnit.Percent);
			titleBack.style.height = 30;
			titleBack.style.backgroundColor = Color.gray.Lerp(Color.black, 0.6f);
			titleBack.RegisterCallback<MouseDownEvent>(data => {
				if (data.button == 0) {
					FreshSelect(groupView.worldBound);
				}
			});
			titleBack.AddManipulator(new ElementDragger(mouseDelta => {
				mouseDelta /= Back.transform.scale;
				foreach (var node in SelectNodes) {
					if (node == null)
						continue;
					MoveNode(node, mouseDelta);
				}
				FreshConnections();
				var index = GroupList.IndexOf(groupView);
				if (index >= 0) {
					Graph.Groups[index].rect.position = (Vector2)groupView.transform.position - GraphPosition;
				}
			}));
			var label = titleBack.AddLabel("...", TextAnchor.MiddleCenter);
			label.style.fontSize = 18;
			label.style.width = new StyleLength(StyleKeyword.Auto);
			label.pickingMode = PickingMode.Ignore;
			label.transform.scale = new Vector3(1 / Back.transform.scale.x, 1 / Back.transform.scale.y);
			OnZoom += () => {
				label.transform.scale = new Vector3(1 / Back.transform.scale.x, 1 / Back.transform.scale.y);
			};
			var text = titleBack.AddText("", "...");
			text.RegisterValueChangedCallback(data => {
				label.text = data.newValue;
				group.name = label.text;
			});
			text.RegisterCallback<KeyDownEvent>(data => {
				if (data.keyCode == KeyCode.Return) {
					text.visible = false;
				}
			});

			text.visible = false;
			titleBack.RegisterCallback<ClickEvent>(data => {
				if (data.clickCount == 2) {
					text.visible = true;
				}
			});
			titleBack.AddMenu(menu => {
				menu.menu.AppendAction("删除", data => {
					var index = GroupList.IndexOf(groupView);
					GroupList.RemoveAt(index);
					Graph.Groups.RemoveAt(index);
					Fresh();
				});
			});

			titleBack.style.SetBorder(Color.black);
			groupView.style.position = Position.Absolute;
			groupView.style.backgroundColor = Color.gray.Lerp(Color.clear, 0.6f);
			groupView.style.SetBorder(Color.gray);
			var resizeView = groupView.AddVisualElement();
			resizeView.style.position = Position.Absolute;
			resizeView.style.backgroundColor = groupView.style.backgroundColor;
			resizeView.style.SetBorder(Color.gray);
			resizeView.SetCursor(MouseCursor.ResizeUpLeft);
			resizeView.style.width = 10;
			resizeView.style.height = 10;
			resizeView.style.right = 0;
			resizeView.style.bottom = 0;
			resizeView.AddManipulator(new ElementResizer());
			if (group == null) {
				if (selectRect.style.width.value.value < 50 || selectRect.style.height.value.value < 50) {
					return;
				}
				groupView.transform.position = selectRect.transform.position;
				groupView.style.width = selectRect.style.width;
				groupView.style.height = selectRect.style.height;
				group = new QGroup { rect = new Rect((Vector2)groupView.transform.position - GraphPosition, new Vector2(selectRect.style.width.value.value, selectRect.style.height.value.value)) };
				Graph.Groups.Add(group);
			}
			else {
				groupView.transform.position = group.rect.position + GraphPosition;
				groupView.style.width = group.rect.width;
				groupView.style.height = group.rect.height;
			}
			groupView.RegisterCallback<GeometryChangedEvent>(data => {
				group.rect.width = groupView.style.width.value.value;
				group.rect.height = groupView.style.height.value.value;
				SetDataDirty();
			});
			label.text = group.name;
			text.value = group.name;
			groupView.SendToBack();
			GroupList.Add(groupView);
		}
		private void FreshConnections() {
			foreach (var item in ConnectViewList) {
				item.MarkDirtyRepaint();
			}
		}
		private void FreshPositions() {
			foreach (var node in NodeViewList) {
				var flowNode = Graph.GetNode(node.name);
				if (flowNode == null)
					continue;
				node.transform.position = flowNode.Position + GraphPosition;
			}
			for (int i = 0; i < GroupList.Count; i++) {
				GroupList[i].transform.position = Graph.Groups[i].rect.position + GraphPosition;
			}
		}
		private void MoveNode(VisualElement nodeView, Vector3 mouseDelta) {
			var node = Graph.GetNode(nodeView.name);
			if (node == null)
				return;
			nodeView.transform.position += mouseDelta;
			Vector2 pos = nodeView.transform.position;
			node.Position = pos - GraphPosition;
			foreach (var port in node.Ports) {
				if (port.InputPort) {
					foreach (var connection in port.Connections) {
						foreach (var portId in connection) {
							Graph.GetPort(portId)?.SortConnections();
						}
					}
				}
			}
		}
		private void ForeachChild(VisualElement nodeView, Action<VisualElement> action) {
			var node = Graph.GetNode(nodeView.name);
			action(nodeView);
			if (node.Ports.ContainsKey(QFlowKey.ChildPort)) {
				foreach (var portId in node.Ports[QFlowKey.ChildPort].Connections[0].ToArray()) {
					var child = Back.Q(portId.node);
					ForeachChild(child, action);
				}
			}
			if (node.Ports.ContainsKey(QFlowKey.NextPort)) {
				foreach (var portId in node.Ports[QFlowKey.NextPort].Connections[0].ToArray()) {
					var child = Back.Q(portId.node);
					ForeachChild(child, action);
				}
			}
		}
		private void AddBlackBoard(VisualElement root) {
			blackBoard = root.AddScrollView();
			var color = Color.Lerp(Color.black, Color.white, 0.05f);
			blackBoard.style.backgroundColor = color;
			blackBoard.name = nameof(Blackboard);
			blackBoard.style.SetBorder(Color.black.Lerp(color, 0.5f), 3);
			//blackBoard.style.position = Position.Absolute;
			blackBoard.style.alignSelf = Align.FlexEnd;
			blackBoard.style.width = new StyleLength(StyleKeyword.Auto);
			blackBoard.style.minWidth = 400;
			blackBoard.style.height = new StyleLength(StyleKeyword.Auto);
			blackBoard.style.minHeight = 200;
			blackBoard.style.maxHeight = 400;
		}
		private void FreshBlackBoard() {
			blackBoard.Clear();
			if (GraphRuntime != null) {
				blackBoard.Add("", Graph.blackboard, typeof(Blackboard), value => { SetDataDirty(); }).style.minHeight = 100;
				var runtimeBack = blackBoard.AddVisualElement();
				runtimeBack.style.backgroundColor = Color.gray.Lerp(Color.black, 0.5f);
				runtimeBack.style.SetBorder(Color.black, 1, 2);
				runtimeBack.Add("运行时", GraphRuntime.blackboard, typeof(Blackboard), value => { });
				var curBlackBoard = GraphRuntime.blackboard.parent?.parent;
				while (curBlackBoard != null) {
					foreach (var varValue in curBlackBoard) {
						runtimeBack.AddLabel(varValue?.ToString());
					}
					curBlackBoard = curBlackBoard.parent;
				}
			}
			else {
				blackBoard.Add("", Graph.blackboard, typeof(Blackboard), value => { SetDataDirty(); }).style.minHeight = 100;
				var runtimeBack = blackBoard.AddVisualElement();
				runtimeBack.style.backgroundColor = Color.gray.Lerp(Color.black, 0.5f);
				runtimeBack.style.SetBorder(Color.black, 1, 2);
				//foreach (var graphPath in GraphList) {
				//	if (graphPath == FilePath) {
				//		return;
				//	}
				//	if(graphPath is QGraphAsset graph) {
				//		runtimeBack.AddLabel(graph.Graph.Name);
				//		foreach (var varValue in graph.Graph.blackboard) {
				//			runtimeBack.AddLabel(varValue?.ToString());
				//		}
				//	}
					
				//}
			}
		}
		public void Fresh() {
			AutoLoad = true;
			OnLostFocus();
			OnFocus();
		}
		protected override void OnLostFocus() {
			MoveOffset = false;
			base.OnLostFocus();
		}
		public void ClearSetect() {
			var list = SelectNodes.ToArray();
			SelectNodes.Clear();
			foreach (var nodeView in list) {
				FreshSelect(nodeView);
			}
		}
		private List<VisualElement> SelectNodes = new List<VisualElement>();
		private PortId? StartPortId { get; set; }
		private QConnectElement DragConnect { get; set; }
		private List<VisualElement> NodeViewList = new List<VisualElement>();
		private List<VisualElement> GroupList = new List<VisualElement>();
		private List<QConnectElement> ConnectViewList = new List<QConnectElement>();
		private Action onFreshDotValue = null;
		private VisualElement AddNodeView(VisualElement root, QNode node) {
			var nodeView = root.AddVisualElement();
			var select = nodeView.AddVisualElement().SetBackground(Color.clear, -3);
			select.name = "Select";
			select.style.SetBorder(Color.white.Lerp(Color.black, 0.3f), 2);
			select.visible = false;
			NodeViewList.Add(nodeView);
			var color = node.commandKey.ToColor(0.3f, 0.4f);
			if (node.Breakpoint) {
				color = color.Lerp(Color.black, 0.8f);
			}
			nodeView.style.backgroundColor = color;
			nodeView.name = node.Key;
			nodeView.style.SetBorder(Color.black.Lerp(color, 0.5f), 3);
			nodeView.style.position = Position.Absolute;
			nodeView.transform.position = node.Position + GraphPosition;
			nodeView.style.width = new StyleLength(StyleKeyword.Auto);
			nodeView.style.minWidth = 200;
			nodeView.style.height = new StyleLength(StyleKeyword.Auto);
			nodeView.style.minHeight = 50;
			nodeView.RegisterCallback<MouseEnterEvent>(data => {
				CurrentSubNode = node;
				nodeView.BringToFront();
			});
			nodeView.RegisterCallback<MouseLeaveEvent>(data => {
				if (CurrentSubNode == node) {
					CurrentSubNode = null;
				}
			});
			nodeView.RegisterCallback<MouseDownEvent>(data => {
				if (!SelectNodes.Contains(nodeView)) {
					if (!data.ctrlKey) {
						ClearSetect();
					}
					SelectNodes.AddCheckExist(nodeView);
					FreshSelect(nodeView);
				}
				if (data.button == 0) {
					MoveOffset = true;
				}
			});
#if UNITY_EDITOR
			nodeView.RegisterCallback<ClickEvent>(data => {
				switch (data.clickCount) {
					case 2:
						if (node.Info.type == typeof(Subgraph)) {
							if (node.Ports[nameof(Subgraph.graphAsset)].DefaultValue is string graphAsset) {
								Open(AssetDatabase.GetAssetPath(QObjectTool.GetObject<UnityEngine.Object>(graphAsset)), false);
							}
						}
						else {
							ClearSetect();
							ForeachChild(nodeView, child => {
								SelectNodes.AddCheckExist(child);
								FreshSelect(child);
							});
						}
						break;
					case 3:
						OpenScriptOfNode(node.Info.type);
						break;
					default:
						break;
				}
			});
#endif
			nodeView.AddMenu(data => {
				MoveOffset = false;

				if (Graph.Nodes[nodeView.name]?.Info?.type.IsGenericType == true) {
					var type = Graph.Nodes[nodeView.name].Info.type;
					var typeDef = type.GetGenericTypeDefinition();
					foreach (var newType in typeDef.GetAllGenericTypes()) {
						data.menu.AppendAction("更改泛型/" + newType.GetFriendlyName(), action => {
							Graph.Nodes[nodeView.name].ReplaceNode(newType.FullName);
							GraphRuntime?.Nodes.Remove(nodeView.name);
							Fresh();
						}, type == newType ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
					}
				}
				data.menu.AppendAction("复制", action => {
					Copy();
				});
				data.menu.AppendAction("删除", action => {
					Delete();
				});

				foreach (var kv in QCommand.KeyDictionary) {
					if (kv.Value == null)
						continue;
					data.menu.AppendAction("替换/" + kv.Value.FullName, action => {
						var newNode = Graph.AddNode(kv.Key);
						node.ReplaceNode(newNode);
						Fresh();
					});
					if (Graph.Type == QGraph.GraphType.BehaviorTree && node.Ports.ContainsKey(QFlowKey.FromPort)) {
						if (!kv.Value.type.Is(typeof(BTDecoratorNode)))
							continue;
						data.menu.AppendAction(kv.Value.FullName, action => {
							var newNode = Graph.AddNode(kv.Key);
							newNode.Position = node.Position - (nodeView.layout.size.y + 30) * Vector2.up;
							var newView = AddNodeView(Back, newNode);
							if (node.Ports[QFlowKey.FromPort][0].Count > 0) {
								var fromPort = node.Ports[QFlowKey.FromPort][0][0];
								node.Ports[QFlowKey.FromPort].DisConnect(fromPort, fromPort.index);
								newNode.Ports[QFlowKey.FromPort].Connect(fromPort);
							}
							newNode.Ports[QFlowKey.ChildPort].Connect(node.Ports[QFlowKey.FromPort].GetPortId());
							Fresh();
						});
					}
				}

				data.menu.AppendSeparator();
				data.menu.AppendAction("断点", action => {
					node.Breakpoint = !node.Breakpoint;
					Fresh();
				}, state => {
					return node.Breakpoint ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
				});
				if (CurAgent != null && Graph != null) {
					var nodeRuntime = GraphRuntime?.Nodes[node.Key];
					data.menu.AppendAction("运行", action => {
						if (nodeRuntime?.State == QNodeState.running) {
							nodeRuntime?.End();
						}
						else {
							if (GraphRuntime == null) {
								GraphRuntime = Graph.GetRuntime(CurAgent);
								GraphRuntime.OnStop(flag => {
									GraphRuntime = null;
								});
								nodeRuntime = GraphRuntime.Nodes[node.Key];
							}
							if (CurAgent != null && !CurAgent.Graphs.Contains(nodeRuntime.Graph)) {
								CurAgent.StartGraph(GraphRuntime, nodeRuntime.Node.Key);
							}
							nodeRuntime.Start();
						}
					}, nodeRuntime?.State == QNodeState.running ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
					data.menu.AppendSeparator();
				}
			});
			var label = nodeView.AddLabel(node.name);
			label.name = "Title";
			label.style.unityTextAlign = TextAnchor.MiddleCenter;
			label.style.height = Graph.Type == QGraph.GraphType.FlowGraph ? 20 : 40;
			label.style.backgroundColor = color.Lerp(Color.black, 0.5f);
			nodeView.AddLabel("Info", TextAnchor.UpperCenter).SetText(QNodeRuntime.Get(null, node)?.Description);
			foreach (var port in node.Ports) {
				AddPortView(nodeView, port);
			}
			SetDataDirty();
			return nodeView;
		}
		private void Copy() {
			var nodeList = new List<QNode>();
			foreach (var node in SelectNodes.ToArray()) {
				nodeList.AddCheckExist(Graph.GetNode(node.name));
			}
			GUIUtility.systemCopyBuffer = nodeList.ToQData();
		}
		private void Pause(Vector2 position) {
			var nodeList = new List<QNode>();
			GUIUtility.systemCopyBuffer.ParseQData(nodeList);
			Graph.Parse(nodeList, position);
			SetDataDirty();
			OnLostFocus();
			OnFocus();
		}
		private void Delete() {
			foreach (var node in SelectNodes.ToArray()) {
				Graph.Remove(Graph.Nodes[node.name]);
			}
			Fresh();
		}
		private void OnGUI() {
			if (Event.current.type == EventType.KeyUp) {
				switch (Event.current.keyCode) {
					case KeyCode.C:
						if (Event.current.control) {
							Copy();
						}
						break;
					case KeyCode.D:
						if (Event.current.control) {
							Copy();
							Pause(Back.WorldToLocal(Event.current.mousePosition) - GraphPosition);
						}
						break;
					case KeyCode.X:
						if (Event.current.control) {
							Copy();
							Delete();
						}
						break;
					case KeyCode.V:
						if (Event.current.control) {
							Pause(Back.WorldToLocal(Event.current.mousePosition) - GraphPosition);
						}
						break;
					case KeyCode.Delete:
						Delete();
						break;
					default:
						break;
				}
			}
		}
		private void FreshSelect(Rect rect) {
			ClearSetect();
			foreach (var node in NodeViewList) {
				if (rect.Contains(node.worldBound.min) && rect.Contains(node.worldBound.max)) {
					SelectNodes.Add(node);
					FreshSelect(node);
				}
			}
		}
		private void FreshSelect(VisualElement visual) {
			visual.Q<VisualElement>("Select").visible = SelectNodes.Contains(visual);
		}
		public void AddPortView(VisualElement root, QPort port) {
			if (Graph.Type == QGraph.GraphType.BehaviorTree && (port.Key == QFlowKey.ChildPort || (port.Key == QFlowKey.NextPort && !port.Node.Ports.ContainsKey(QFlowKey.FromPort)))) {
				var dot = AddDotView(root, GetColor(port), port.GetPortId());
			}
			else if (port.Key == QFlowKey.NextPort || port.Key == QFlowKey.FromPort) {
				switch (Graph.Type) {
					case QGraph.GraphType.FlowGraph: {
						var title = root.Q<Label>("Title");
						var dot = AddDotView(title, GetColor(port), port.GetPortId());
						dot.style.position = Position.Absolute;
						if (port.OutputPort) {
							dot.style.right = 0;
						}
						else {
							dot.style.left = 0;
						}
					}
					break;
					case QGraph.GraphType.BehaviorTree: {
						var title = root.Q<Label>("Title");
						if (port.InputPort) {
							if (!port.Node.Info.type.Is(typeof(FunctionNode))) {
								var dot = AddDotView(title, GetColor(port), port.GetPortId());
								dot.style.top = 0;
							}
						}
					}
					break;
					default:
						break;
				}
			}
			else {
				var row = root.AddVisualElement(port.OutputPort ? FlexDirection.RowReverse : FlexDirection.Row);
				if (port.IsList) {
					var view = row.AddVisualElement();
					VisualElement viewList = null;
					void FreshList() {
						view.Clear();
						var index = 0;
						foreach (var child in viewList.Children().ToArray()) {
							var childRow = view.AddVisualElement(port.OutputPort ? FlexDirection.Row : FlexDirection.RowReverse);
							childRow.style.alignSelf = port.OutputPort ? Align.FlexEnd : Align.FlexStart;
							childRow.Add(child);
							if (port.OutputPort || port.InputPort) {
								AddDotView(childRow, GetColor(port), port.GetPortId(index));
							}
							index++;
							FreshPortConnect(port);
							child.RegisterCallback<MouseEnterEvent>(ValueMouseEnterEvent);
						}
						if (index == 0) {
							var list = port.DefaultValue as IList;
							view.AddButton("新增 " + port.ViewName, () => {
								list = list.CreateAt(QSerializeType.Get(port.ValueType));
								port.DefaultValue = list;
								root.Remove(row);
								AddPortView(root, port);
							});
						}
					}
					viewList = row.Add(port.ViewName, port.DefaultValue, port.ValueType, newValue => { port.DefaultValue = newValue; if (port.DefaultValue is IList list && list.Count == viewList?.childCount) { FreshList(); } });
					row.Remove(viewList);
					FreshList();
				}
				else {
					if (port.OutputPort || port.InputPort) {
						AddDotView(row, GetColor(port), port.GetPortId());
					}
					if (port.IsShowValue()) {
						row.Add(port.ViewName, port.DefaultValue, port.ValueType, newValue => port.DefaultValue = newValue, port.attributeProvider).RegisterCallback<MouseEnterEvent>(ValueMouseEnterEvent);
					}
					else {
						var label = row.AddLabel(port.ViewName, port.OutputPort ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
						label.RegisterCallback<MouseEnterEvent>(ValueMouseEnterEvent);
						if (!port.FlowPort) {
							onFreshDotValue += () => {
								if (GraphRuntime != null && port.Node != null && GraphRuntime.Nodes[port.Node.Key] != null) {
									if (GraphRuntime.Nodes.ContainsKey(port.Node.Key) && GraphRuntime.Nodes[port.Node.Key].Ports.ContainsKey(port.Key)) {
										label.text = $"{port.ViewName} {GraphRuntime.Nodes[port.Node.Key].Ports[port.Key].GetValue?.Invoke()?.ToString()}";
										return;
									}
								}
								label.text = port.ViewName;
							};
						}

					}

				}

			}
			FreshPortConnect(port);
		}
		private void ValueMouseEnterEvent(MouseEnterEvent data) {
			AutoLoad = false;
		}
		public Color GetColor(QPort port) {
			return port.FlowPort || port.ConnectType == null ? Color.HSVToRGB(0.6f, 0.5f, 1) : port.ConnectType.Name.ToColor();
		}
		public QConnectElement AddConnectView(PortId start, PortId? end = null) {
			var port = Graph.GetPort(start);
			var color = GetColor(port);
			var visual = ConnectCanvas.AddConnect(color, port.FlowPort ? 4 : 2);
			visual.name = start.ToQData() + (end == null ? "" : " " + end.Value.ToQData());
			visual.StartElement = GetDotView(start);
			visual.Type = Graph.Type == QGraph.GraphType.FlowGraph || !port.FlowPort ? QConnectElement.ConnectType.Hor : QConnectElement.ConnectType.Ver;
			if (end != null) {
				var endPort = Graph.GetPort(end.Value);
				visual.EndElement = GetDotView(end.Value);
				visual.EndColor = GetColor(endPort);
				visual.LineWidth = port.FlowPort && endPort.FlowPort ? 4 : 2;
			}
			ConnectViewList.Add(visual);
			return visual;
		}
		public QConnectElement GetConnectView(PortId start, PortId end) {
			var view = ConnectCanvas.Q<QConnectElement>(start.ToQData() + " " + end.ToQData()); ;
			return view;
		}
		public void RemoveConnectView(PortId start, PortId end) {
			var view = GetConnectView(start, end);
			if (view != null) {
				ConnectCanvas.Remove(view);
			}
		}
		public VisualElement GetDotView(PortId portId) {
			return Back.Q<VisualElement>(portId.ToQData());
		}
		public VisualElement AddDotView(VisualElement root, Color color, PortId portId) {
			var dot = root.AddVisualElement();
			dot.name = portId.ToQData();
			dot.style.backgroundColor = Color.black;
			dot.style.SetFixedSize(12, 12);
			dot.style.alignSelf = Align.Center;
			dot.style.SetBorder(color, 2, 6);
			var center = dot.AddVisualElement();
			center.name = "center";
			center.style.width = 4;
			center.style.height = 4;
			center.transform.position = dot.transform.position + new Vector3(1, 1);
			center.style.SetBorder(color, 2, 2);
			dot.RegisterCallback<MouseDownEvent>(data => {
				if (StartPortId == null) {
					var port = Graph.GetPort(portId);
					if (port.OutputPort) {
						StartPortId = portId;
						if (port.FlowPort && port.FlowPort.onlyOneConnection) {
							if (port.HasConnect(StartPortId.Value.index)) {
								var end = port.Connections[StartPortId.Value.index][0];
								var targetPort = Graph.GetPort(end);
								if (targetPort != null && targetPort.FlowPort) {
									DragConnect = GetConnectView(StartPortId.Value, end);
									Graph.DisConnect(StartPortId.Value, end);
									FreshDotView(GetDotView(end));
								}
							}
						}
					}
					else {
						var connection = Graph.GetConnection(portId);
						if (connection.Count > 0) {
							StartPortId = connection[0];
							if (Graph.GetPort(StartPortId.Value) != null) {
								Graph.DisConnect(StartPortId.Value, portId);
							}
							else {
								Graph.GetPort(portId).ClearAllConnect();
							}
							DragConnect = GetConnectView(StartPortId.Value, portId);
							FreshDotView(GetDotView(portId));
							connection.SortByPosition(Graph);
						}
						else {
							return;
						}
					}
					var startDot = GetDotView(StartPortId.Value);
					FreshDotView(startDot, true);
					if (DragConnect != null) {
						DragConnect.name = StartPortId.Value.ToQData();
					}
					else if (Graph.GetPort(StartPortId.Value) != null) {
						DragConnect = AddConnectView(StartPortId.Value);
						DragConnect.End = startDot.worldBound.center;
					}
				}
			});
			dot.RegisterCallback<MouseUpEvent>(data => {
				if (StartPortId != null && !Equals(StartPortId, portId)) {
					var port = Graph.GetPort(StartPortId);
					if (port != null && port.CanConnect(Graph.GetPort(portId))) {
						Graph.Connect(StartPortId.Value, portId);
						DragConnect.name += " " + portId.ToQData();
						FreshDotView(dot);
						FreshDotView(DragConnect.StartElement);
						StartPortId = null;
						DragConnect.EndElement = dot;
						var endPort = Graph.GetPort(portId);
						DragConnect.EndColor = GetColor(endPort);
						DragConnect.LineWidth = port.FlowPort && endPort.FlowPort ? 4 : 2;
						DragConnect = null;
						Graph.GetPort(StartPortId)?.SortConnections();
						SetDataDirty();
					}
				}
			});
			FreshDotView(dot);
			return dot;
		}
		public void FreshDotView(VisualElement visual, bool? visible = null) {
			if (visual == null)
				return;
			var portId = visual.name.ParseQData<PortId>();
			var center = visual.Q<VisualElement>("center");
			if (visible == null) {
				center.visible = Graph.GetPort(portId)?.HasConnect(portId.index) == true;
			}
			else {
				center.visible = visible.Value;
			}
		}
		private void Update() {
			onFreshDotValue?.Invoke();
			if (GraphRuntime != null) {
				foreach (var nodeView in NodeViewList) {
					if (!GraphRuntime.Nodes.ContainsKey(nodeView.name)) {
						continue;
					}
					var node = GraphRuntime.Nodes[nodeView.name];
					if (node == null)
						continue;
					var state = node.State;
					var backColor = nodeView.style.backgroundColor.value;
					var color = Color.black;
					//	var tweenSpeed = 5;
					switch (state) {
						case QNodeState.running:
							color = Color.green;
							color = Color.Lerp(color, backColor, 0.5f);
							color = nodeView.style.borderTopColor.value.Lerp(color, Time.deltaTime);
							break;
						case QNodeState.success:
							color = Color.green.Lerp(Color.black, 0.8f);
							color = Color.Lerp(color, backColor, 0.5f);
							//	tweenSpeed = 2;
							break;
						case QNodeState.fail:
							color = Color.red;
							color = Color.Lerp(color, backColor, 0.5f);
							break;
						default:
							color = Color.Lerp(color, backColor, 0.5f);
							break;
					}
					nodeView.style.SetBorder(color, 3);
					var info = nodeView.Q<Label>("Info");
					info.SetText(node.Description);
				}
			}
		}
#if UNITY_EDITOR
		public static MonoScript MonoScriptFromType(Type targetType) {
			if (targetType == null)
				return null;
			var typeName = targetType.Name;
			if (targetType.IsGenericType) {
				targetType = targetType.GetGenericTypeDefinition();
				typeName = typeName.Substring(0, typeName.IndexOf('`'));
			}
			var mono = AssetDatabase.FindAssets(string.Format("{0} t:MonoScript", typeName))
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
				.FirstOrDefault(m => m != null && m.GetClass() == targetType);
			if (mono == null) {
				mono = AssetDatabase.GetAllAssetPaths().Where(path => path.EndsWith(".cs")).Select(AssetDatabase.LoadAssetAtPath<MonoScript>).FirstOrDefault(ms => {
					return ms.text.Contains($"class {targetType.Name.SplitStartString("`")} ");
				});
			}
			return mono;
		}

		public bool OpenScriptOfNode(Type type) {
			var mono = MonoScriptFromType(type);
			if (mono != null) {
				AssetDatabase.OpenAsset(mono);
				return true;
			}
			return false;
		}
#endif
	}
	class ElementDragger : MouseManipulator {
		private Vector2 m_Start;
		protected bool m_Active;
		private Action<Vector2> onUpdate;
		public ElementDragger(Action<Vector2> onUpdate = null) {
			activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
			m_Active = false;
			this.onUpdate = onUpdate;
		}

		protected override void RegisterCallbacksOnTarget() {
			target.RegisterCallback<MouseDownEvent>(OnMouseDown);
			target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
			target.RegisterCallback<MouseUpEvent>(OnMouseUp);
		}

		protected override void UnregisterCallbacksFromTarget() {
			target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
			target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
		}

		protected void OnMouseDown(MouseDownEvent e) {
			if (m_Active) {
				e.StopImmediatePropagation();
				return;
			}

			if (CanStartManipulation(e)) {
				m_Start = e.localMousePosition;

				m_Active = true;
				target.CaptureMouse();
				e.StopPropagation();
			}
		}

		protected void OnMouseMove(MouseMoveEvent e) {
			if (!m_Active || !target.HasMouseCapture())
				return;

			Vector2 diff = e.localMousePosition - m_Start;
			target.parent.transform.position += (Vector3)diff;
			//target.parent.style.top = target.parent.layout.y + diff.y;
			//target.parent.style.left = target.parent.layout.x + diff.x;
			e.StopPropagation();
			onUpdate?.Invoke(e.mouseDelta);
		}

		protected void OnMouseUp(MouseUpEvent e) {
			if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
				return;

			m_Active = false;
			target.ReleaseMouse();
			e.StopPropagation();
			onUpdate?.Invoke(Vector2.zero);
		}
	}

	class ElementResizer : MouseManipulator {
		private Vector2 m_Start;
		protected bool m_Active;

		public ElementResizer() {
			activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
			m_Active = false;
		}

		protected override void RegisterCallbacksOnTarget() {
			target.RegisterCallback<MouseDownEvent>(OnMouseDown);
			target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
			target.RegisterCallback<MouseUpEvent>(OnMouseUp);
		}

		protected override void UnregisterCallbacksFromTarget() {
			target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
			target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
		}

		protected void OnMouseDown(MouseDownEvent e) {
			if (m_Active) {
				e.StopImmediatePropagation();
				return;
			}

			if (CanStartManipulation(e)) {
				m_Start = e.localMousePosition;

				m_Active = true;
				target.CaptureMouse();
				e.StopPropagation();
			}
		}

		protected void OnMouseMove(MouseMoveEvent e) {
			if (!m_Active || !target.HasMouseCapture())
				return;

			Vector2 diff = e.localMousePosition - m_Start;
			target.parent.style.width = Mathf.Max(target.parent.layout.width + diff.x, 50);
			target.parent.style.height = Mathf.Max(target.parent.layout.height + diff.y, 50);
			e.StopPropagation();
		}

		protected void OnMouseUp(MouseUpEvent e) {
			if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
				return;

			m_Active = false;
			target.ReleaseMouse();
			e.StopPropagation();
		}
	}
}

#endif