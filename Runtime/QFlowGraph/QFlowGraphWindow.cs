using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace QTool.FlowGraph
{
	public class QFlowGraphWindow : QFileEditorWindow<QFlowGraphWindow>
	{
		#region 静态函数
		[UnityEditor.Callbacks.OnOpenAsset(0)]
		public static bool OnOpen(int instanceID, int line)
		{
			if (EditorUtility.InstanceIDToObject(instanceID) is QFlowGraphAsset asset)
			{
				var path = AssetDatabase.GetAssetPath(asset);
				FilePath = path;
				OpenWindow();
				return true;
			}
			return false;
		}

		[MenuItem("QTool/窗口/QFlowGraph")]
		public static void OpenWindow()
		{
			var window = GetWindow<QFlowGraphWindow>();
			window.minSize = new Vector2(500, 300);
			window.ViewOffset = Vector2.zero;
		}
		#endregion
		public override string GetData(UnityEngine.Object file)
		{
			if (file != null)
			{
#if UNITY_EDITOR
				serializedProperty = new SerializedObject(file).FindProperty(nameof(QFlowGraphAsset.Graph)).FindPropertyRelative(nameof(QFlowGraph.SerializeString));
#endif
				return (file as QFlowGraphAsset).Graph.SerializeString;
			}
			else if (serializedProperty != null)
			{
				return serializedProperty.stringValue;
			}
			else
			{
				return "";
			}
		}
		private QFlowGraph Graph { get; set; }
		public override void SaveData()
		{
			Graph.OnBeforeSerialize();
			Data = Graph.SerializeString;
#if UNITY_EDITOR
			if (serializedProperty != null)
			{
				serializedProperty.stringValue = Graph.ToQData();
				serializedProperty.serializedObject.ApplyModifiedProperties();
			}
#endif
		}

		private VisualElement ConnectCanvas { get; set; }
		protected override async void ParseData()
		{
			Graph = Data.ParseQData<QFlowGraph>();
			if (Graph == null) return;
			await QTask.Wait(() => Back != null);
			Back.Clear();
			ConnectCanvas.Clear();
			NodeViewList.Clear();
			foreach (var node in Graph.NodeList)
			{
				AddNodeView(Back, node);
			}
		}
		public void FreshPortConnect(QFlowPort port)
		{
			var color = port.ConnectType.Name.ToColor();
			for (int portIndex = 0; portIndex < port.ConnectInfolist.Count; portIndex++)
			{
				var connectList = port.ConnectInfolist[portIndex].ConnectList;
				foreach (var connect in connectList)
				{
					PortId startId;
					PortId endId;
					if (port.IsOutput)
					{
						startId = port.GetPortId(portIndex);
						endId = connect;
					}
					else
					{
						startId = connect;
						endId = port.GetPortId(portIndex);
					}
					var start = GetDotView(startId);
					var end = GetDotView(endId);
					if (start != null && end != null)
					{
						var connectView = GetConnectView(startId, endId);
						if (connectView == null)
						{
							connectView = ConnectCanvas.AddConnect(color);
							connectView.name = startId.ToQData() + " " + endId.ToQData();
							connectView.StartElement = start;
							connectView.EndElement = end;
							ConnectViewList.Add(connectView);
						}
					}
				}
			}
		}
		VisualElement Back = null;
		protected override void CreateGUI()
		{
			base.CreateGUI();
			var viewRange = rootVisualElement.AddVisualElement();
			viewRange.style.backgroundColor = Color.Lerp(Color.black, Color.white, 0.1f);
			viewRange.style.overflow = Overflow.Hidden;
			viewRange.style.height = new Length(100, LengthUnit.Percent);
			ConnectCanvas = viewRange.AddVisualElement().SetBackground();
			Back = viewRange.AddVisualElement().SetBackground();
			Back.RegisterCallback<MouseDownEvent>(data =>
			{
				if (data.ctrlKey)
				{

				}
				else
				{
					if (CurrentNode == null)
					{
						CurrentNode = Back;
					}
				}
			});
			Back.RegisterCallback<MouseMoveEvent>(data =>
			{
				if (StartPortId != null)
				{
					if (DragConnect != null)
					{
						DragConnect.End = data.mousePosition;
					}
				}
				else if (CurrentNode != null)
				{
					if (CurrentNode != Back)
					{
						CurrentNode.transform.position = data.mousePosition + DragOffset;
						Vector2 pos = CurrentNode.transform.position;
						Graph[CurrentNode.name].rect.position = pos - ViewOffset;
					}
					else
					{
						ViewOffset += data.mouseDelta;
						foreach (var node in NodeViewList)
						{
							node.transform.position = Graph[node.name].rect.position + ViewOffset;
						}
					}
				}
				foreach (var item in ConnectViewList)
				{
					item.MarkDirtyRepaint();
				}
			});
			Back.AddMenu(data =>
			{
				var position = data.localMousePosition;
				if (CurrentNode == null) 
				{
					foreach (var kv in QCommand.KeyDictionary)
					{
						data.menu.AppendAction(kv.fullName, action =>
						{
							var node = Graph.AddNode(kv.Key);
							node.rect.position = position;
							AddNodeView(Back, node);
						});
					}
				}
			});
			Back.RegisterCallback<MouseUpEvent>(data =>
			{
				if (StartPortId != null)
				{
					StartPortId = null;
					FreshConnectDotView(DragConnect.StartElement);
					ConnectCanvas.Remove(DragConnect);
					ConnectViewList.Remove(DragConnect);
					DragConnect = null;
				}
				CurrentNode = null;
			});
		}
		protected override void OnLostFocus()
		{
			base.OnLostFocus();
			CurrentNode = null;
		}
		private VisualElement CurrentNode { get; set; }
		private PortId? StartPortId { get; set; }
		private QConnectElement DragConnect { get; set; }
		private Vector2 ViewOffset { get; set; }
		private Vector2 DragOffset { get; set; }
		private List<VisualElement> NodeViewList = new List<VisualElement>();
		private List<QConnectElement> ConnectViewList = new List<QConnectElement>();
		private VisualElement AddNodeView(VisualElement root, QFlowNode node)
		{
			var nodeView = root.AddVisualElement();
			NodeViewList.Add(nodeView);
			var color = node.commandKey.ToColor(0.3f, 0.5f);
			nodeView.style.backgroundColor = color;
			nodeView.name = node.Key;
			nodeView.style.SetBorder(Color.black.Lerp(color, 0.5f), 3);
			nodeView.style.position = Position.Absolute;
			nodeView.transform.position = node.rect.position + ViewOffset;
			nodeView.style.width = Mathf.Max(200, node.rect.width);
			nodeView.style.height = new StyleLength(StyleKeyword.Auto);
			nodeView.RegisterCallback<MouseDownEvent>(data =>
			{
				CurrentNode = nodeView;
				DragOffset = nodeView.transform.position - new Vector3(data.mousePosition.x, data.mousePosition.y);
			});
			nodeView.AddMenu(data =>
			{
				CurrentNode = null;
				data.menu.AppendAction("运行", action =>
				{
					Graph.Run(node.Key);
				});
				data.menu.AppendSeparator();
				data.menu.AppendAction("删除", action =>
				{
					RemoveNodeView(nodeView);
				});
			});
			var label = nodeView.AddLabel(node.Name);
			label.name = "Title";
			label.style.unityTextAlign = TextAnchor.MiddleCenter;
			label.style.height = 20;
			label.style.backgroundColor = color.Lerp(Color.black, 0.5f);
			foreach (var port in node.Ports)
			{
				AddPortView(nodeView, port);
			}
			return nodeView;
		}
		public void RemoveNodeView(VisualElement visual)
		{
			var node = Graph[visual.name];
			foreach (var port in node.Ports)
			{
				for (int i = 0; i < port.ConnectInfolist.Count; i++)
				{
					if (port.IsOutput)
					{
						var start = port.GetPortId(i);
						foreach (var connect in port.ConnectInfolist[i].ConnectList)
						{
							RemoveConnectView(start, connect);
						}
					}
					else
					{
						var end = port.GetPortId(i);
						foreach (var connect in port.ConnectInfolist[i].ConnectList)
						{
							RemoveConnectView(connect, end);
						}
					}
				}
			}
			Graph.Remove(node);
			Back.Remove(visual);
			NodeViewList.Remove(visual);

		}
		public void AddPortView(VisualElement root, QFlowPort port)
		{
			if (port.Key == QFlowKey.NextPort || port.Key == QFlowKey.FromPort)
			{
				var title = root.Q<Label>("Title");
				var dot = AddDotView(title, port.ConnectType.Name.ToColor(), port.GetPortId());
				dot.style.position = Position.Absolute;
				if (port.IsOutput)
				{
					dot.style.right = 0;
				}
				else
				{
					dot.style.left = 0;
				}
			}
			else
			{
				var row = root.AddVisualElement(port.IsOutput ? FlexDirection.RowReverse : FlexDirection.Row);
				if (port.IsList)
				{
					var foldout = row.Add(port.ViewName, port.Value, port.ValueType, newValue => port.Value = newValue);
					row.Remove(foldout);
					var listView = foldout.Q<ListView>();
					row.Add(listView);
					listView.bindItem += (visual, index) =>
					{
						visual.style.flexDirection = port.IsOutput ? FlexDirection.Row : FlexDirection.RowReverse;
						AddDotView(visual, port.ConnectType.Name.ToColor(), port.GetPortId(index));
						FreshPortConnect(port);
					};
				}
				else
				{
					var dot = AddDotView(row, port.ConnectType.Name.ToColor(), port.GetPortId());
					if (port.IsShowValue())
					{
						row.Add(port.ViewName, port.Value, port.ValueType, newValue => port.Value = newValue);
					}
					else
					{
						row.AddLabel(port.ViewName, port.IsOutput ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
					}
				}

			}
			FreshPortConnect(port);
		}
		public QConnectElement GetConnectView(PortId start, PortId end)
		{
			return ConnectCanvas.Q<QConnectElement>(start.ToQData() + " " + end.ToQData());
		}
		public void RemoveConnectView(PortId start, PortId end)
		{
			ConnectCanvas.Remove(GetConnectView(start, end));
		}
		public VisualElement GetDotView(PortId portId)
		{
			return Back.Q<VisualElement>(portId.ToQData());
		}
		public VisualElement AddDotView(VisualElement root, Color color, PortId portId)
		{
			var dot = root.AddVisualElement();
			dot.name = portId.ToQData();
			dot.style.backgroundColor = Color.black;
			dot.style.width = 12;
			dot.style.height = 12;
			dot.style.alignSelf = Align.Center;
			dot.style.SetBorder(color, 2, 6);
			var center = dot.AddVisualElement();
			center.name = "center";
			center.style.width = 4;
			center.style.height = 4;
			center.transform.position = dot.transform.position + new Vector3(1, 1);
			center.style.SetBorder(color, 2, 2);
			dot.RegisterCallback<MouseDownEvent>(data =>
			{
				if (StartPortId == null)
				{
					var port = Graph.GetPort(portId);
					if (port.IsOutput)
					{
						StartPortId = portId;
						if (port.IsFlow)
						{
							if (port.HasConnect(StartPortId.Value.index))
							{
								var end = port.ConnectInfolist[StartPortId.Value.index].FirstConnect.Value;
								if (Graph.GetPort(end).IsFlow)
								{
									DragConnect = GetConnectView(StartPortId.Value, end);
									Graph.DisConnect(StartPortId.Value, end);
									FreshConnectDotView(GetDotView(end));
								}
							}
						}
					}
					else
					{
						var connectInfo = Graph.GetConnectInfo(portId);
						if (connectInfo.ConnectList.Count > 0)
						{
							StartPortId = connectInfo.FirstConnect.Value;
							Graph.DisConnect(StartPortId.Value, portId);
							DragConnect = GetConnectView(StartPortId.Value, portId);
							FreshConnectDotView(GetDotView(portId));
						}
						else
						{
							return;
						}
					}
					var startDot = GetDotView(StartPortId.Value);
					FreshConnectDotView(startDot,true);
					if (DragConnect == null)
					{
						DragConnect = ConnectCanvas.AddConnect(port.ConnectType.Name.ToColor());
						DragConnect.StartElement = startDot;
						DragConnect.End = startDot.worldBound.center;
						ConnectViewList.Add(DragConnect);
					}
					DragConnect.name = StartPortId.Value.ToQData();
				}
			});
			dot.RegisterCallback<MouseUpEvent>(data =>
			{
				if (StartPortId != null && !Equals(StartPortId, portId))
				{
					var port = Graph.GetPort(StartPortId);
					if (port.CanConnect(Graph.GetPort(portId)))
					{
						Graph.Connect(StartPortId.Value, portId);
						DragConnect.name += " " + portId.ToQData();
						FreshConnectDotView(dot);
						FreshConnectDotView(DragConnect.StartElement);
						StartPortId = null;
						DragConnect.EndElement = dot;
						DragConnect = null;
					}
				}
			});
			FreshConnectDotView(dot);
			return dot;
		}
		public void FreshConnectDotView(VisualElement visual, bool? visible = null)
		{
			var portId = visual.name.ParseQData<PortId>();
			var center = visual.Q<VisualElement>("center");
			if (visible == null)
			{
				center.visible = Graph.GetPort(portId).HasConnect(portId.index);
			}
			else
			{
				center.visible = visible.Value;
			}
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(QFlowGraph))]
	public class QFlowGraphDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var root= new VisualElement();
			root.style.flexDirection = FlexDirection.Row;
			root.AddLabel(property.QName());
			root.AddButton("编辑", () =>
			{
				if (property.serializedObject.targetObject.IsPrefabInstance(out var prefab))
				{
					AssetDatabase.OpenAsset(prefab);
				}
				else
				{
					var graph = property.GetObject() as QFlowGraph;
					QFlowGraphWindow.FilePath = "";
					QFlowGraphWindow.serializedProperty = property.FindPropertyRelative(nameof(QFlowGraph.SerializeString));
					QFlowGraphWindow.OpenWindow();
				}
			});
			return root;
		}
	}

#endif


	//    public class QFlowGraphWindow : EditorWindow
	//	{

	//		public static string OpenPathKey => nameof(QFlowGraphWindow) + "_" + nameof(Graph) + "Path";
	//		static QFlowGraph Graph = null;
	//#if UNITY_EDITOR
	//		[OnOpenAsset(0)]
	//        public static bool OnOpen(int instanceID, int line)
	//        {
	//            var asset = EditorUtility.InstanceIDToObject(instanceID) as QFlowGraphAsset;
	//            if (asset != null)
	//            {
	//				PlayerPrefs.SetString(OpenPathKey, AssetDatabase.GetAssetPath(asset));
	//				asset.Graph.Name = asset.name;
	//#pragma warning disable CS0618
	//				Open(asset.Graph, asset.SetDirty);
	//#pragma warning restore CS0618
	//				return true;
	//			}
	//            return false;
	//        }
	//		public static void AutoLoadPath()
	//		{
	//			if (Graph != null) return;
	//			var path = PlayerPrefs.GetString(OpenPathKey);
	//			if (path.IsNull())
	//			{
	//				PlayerPrefs.SetString(OpenPathKey, "");
	//				return;
	//			}
	//			var asset = AssetDatabase.LoadAssetAtPath<QFlowGraphAsset>(path);
	//			if (asset != null)
	//			{
	//#pragma warning disable CS0618 
	//				Open(asset.Graph, asset.SetDirty);  
	//#pragma warning restore CS0618
	//			}
	//		}
	//		[MenuItem("QTool/窗口/流程图")]
	//		public static void OpenWindow()
	//		{
	//			Open(null, null);
	//		}
	//#endif



	//		public event Action OnSave;
	//		public static void Open(QFlowGraph graph,Action OnSave=null)
	//		{
	//			var window = GetWindow<QFlowGraphWindow>();
	//			window.minSize = new Vector2(400, 300);
	//			window.titleContent = new GUIContent((graph?.Name == null ? "" : graph.Name + " - ") + nameof(QFlowGraph));
	//			graph?.Deserialize();
	//			Graph = graph;
	//#if UNITY_EDITOR
	//			if (Graph == null)
	//			{
	//				AutoLoadPath();
	//			}
	//#endif
	//			window.ViewRange = new Rect(Graph.IsNull()?Vector2.zero:graph.ViewPos, window.position.size);
	//			window.Show();
	//			window.OnSave = OnSave;
	//			window.Repaint();
	//		}


	//#if UNITY_EDITOR
	//		private void OnFocus()
	//        {
	//			AutoLoadPath();
	//		}
	//#endif
	//		protected void OnLostFocus()
	//        { 
	//			if (Graph != null)
	//			{
	//				Graph.ViewPos = ViewRange.position;
	//				Graph.OnBeforeSerialize();
	//				OnSave?.Invoke();
	//			}
	//        }

	//        private Rect ViewRange;
	//        void CreateMenu(PortId fromPortId)
	//        {
	//			QGenericMenu menu = new QGenericMenu();
	//            var fromPort = Graph.GetPort(fromPortId);
	//            foreach (var info in QCommand.KeyDictionary)
	//            {
	//                if (fromPort.CanConnect(info, out var portKey))
	//                {
	//                    menu.AddItem(new GUIContent(info.fullName), false, () =>
	//					{
	//						SelectNodes.Clear();
	//						var node =Graph.AddNode(info.Key);
	//						node.rect.center = mousePos;;
	//                        fromPort.Connect( node.Ports[portKey],fromPortId.index);
	//						SelectNodes.Add(node);

	//					});
	//                }
	//			}
	//			var value = fromPort.GetValue(fromPortId.index);
	//			if (!value.IsNull())
	//			{
	//				var typeInfo = QReflectionType.Get(value.GetType());
	//				if (typeInfo.Type != typeof(QFlow))
	//				{
	//					foreach (var funcInfo in typeInfo.Functions)
	//					{
	//						if (funcInfo.IsPublic)
	//						{
	//							menu.AddItem(new GUIContent("内置函数/" + funcInfo.MethodInfo.DeclaringType.QTypeName() + "/" + funcInfo.Name.Replace("_", "/")), false, () =>
	//							{
	//								SelectNodes.Clear();
	//								var node = Graph.AddNode(nameof(QFlowGraphNode.ObjectFunction));
	//								node.rect.center = mousePos;
	//								fromPort.Connect(node.Ports[QFlowKey.Object], fromPortId.index);
	//								node.Ports[QFlowKey.FuncName].Value = funcInfo.Name;
	//								var param = new object[funcInfo.ParamInfos.Length];
	//								for (int i = 0; i < funcInfo.ParamInfos.Length; i++)
	//								{
	//									param[i] = funcInfo.ParamInfos[i].HasDefaultValue ? funcInfo.ParamInfos[i].DefaultValue : QReflection.CreateInstance(funcInfo.ParamInfos[i].ParameterType);
	//								}
	//								node.Ports[QFlowKey.Param].Value = param;
	//								SelectNodes.Add(node);
	//							}
	//							);


	//						}
	//					}
	//				}
	//			}

	//            menu.ShowAsContext();
	//        }
	//		void AddObject(params UnityEngine.Object[] objs)
	//		{
	//			SelectNodes.Clear();
	//			foreach (var obj in objs)
	//			{
	//				if(obj is QFlowGraphAsset asset)
	//				{
	//					var node = Graph.AddNode(nameof(QFlowGraphNode.GraphAsset));
	//					node.rect.center = mousePos;
	//					node.Ports[QFlowKey.Asset].Value = asset;
	//					SelectNodes.Add(node);
	//				}
	//				else
	//				{
	//					var node = Graph.AddNode(nameof(QFlowGraphNode.ObjectInstance));
	//					node.rect.center = mousePos;
	//					node.Ports[QFlowKey.Object].Value = obj;
	//					SelectNodes.Add(node);
	//				}
	//			}
	//		}
	//        private void ShowMenu()
	//        {
	//			QGenericMenu menu = new QGenericMenu();
	//            if (curNode == null)
	//            {
	//                foreach (var kv in QCommand.KeyDictionary)
	//                {
	//                    menu.AddItem(new GUIContent(kv.fullName), false, () =>
	//					{
	//						SelectNodes.Clear();
	//						var node =Graph.AddNode(kv.Key);
	//						node.rect.center = mousePos;
	//						SelectNodes.Add(node);

	//				   });
	//                }
	//                if (!string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
	//				{
	//					menu.AddSeparator("");
	//					menu.AddItem(new GUIContent("粘贴"), false, Parse);
	//                }
	//            }
	//            else
	//            {
	//                if (curPortId != null)
	//                {

	//                    menu.AddItem(new GUIContent("清空" + curPortId + "端口连接"), false, ()=> Graph.GetPort(curPortId)?.ClearAllConnect(curPortId.Value.index));
	//                }
	//                else
	//                {
	//                    menu.AddItem(new GUIContent("复制"), false, Copy);
	//                    if (!string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
	//                    {
	//                        menu.AddItem(new GUIContent("粘贴"), false, Parse);
	//                    }
	//                    menu.AddItem(new GUIContent("删除"), false, DeleteSelectNodes);
	//                    menu.AddItem(new GUIContent("清空连接"), false, ClearAllConnect);
	//                    if (Application.isPlaying)
	//					{
	//						menu.AddSeparator("");
	//						menu.AddItem(new GUIContent("运行节点"), false, () =>
	//						{
	//							Graph.Run(curNode.Key);
	//                        });
	//                    }
	//                }

	//            }
	//            menu.ShowAsContext();
	//        }
	//        void Copy()
	//        {
	//            if (SelectNodes.Count == 0)
	//            {
	//                SelectNodes.Add(curNode);
	//            }

	//            GUIUtility.systemCopyBuffer = SelectNodes.ToQData();
	//        }
	//        void Parse()
	//        {
	//            try
	//            {
	//                var nodeList = GUIUtility.systemCopyBuffer.ParseQData<List<QFlowNode>>();
	//				Graph.Parse(nodeList, mousePos);
	//            }
	//            catch (Exception e)
	//            {
	//                throw new Exception("粘贴出错", e);
	//            }
	//        }
	//        void ClearAllConnect()
	//        {
	//            ForeachSelectNodes((node) => node.ClearAllConnect());
	//        }
	//        void DeleteSelectNodes()
	//		{
	//			ForeachSelectNodes((node) => Graph.Remove(node));
	//			Repaint();
	//		}
	//        void ForeachSelectNodes(Action<QFlowNode> action)
	//        {
	//            if (SelectNodes.Count > 0)
	//            {
	//                foreach (var node in SelectNodes)
	//                {
	//                    action(node);
	//                }
	//            }
	//            else
	//            {
	//                action(curNode);
	//            }
	//        }
	//        public void UpdateNearPort()
	//        {
	//            UpdateCurrentData();
	//            if (curNode != null)
	//            {
	//                if (curPortId == null)
	//                {
	//                    if (curNode.Key == connectStartPort?.node) {
	//                        nearPortId = null;return;
	//                    }
	//                    nearPortId = null;
	//                    var minDis = float.MaxValue;
	//                    foreach (var port in curNode.Ports)
	//                    {

	//                        if (Graph.GetPort(connectStartPort).CanConnect(port))
	//                        {
	//                            var index = 0;
	//                            foreach (var c in port.ConnectInfolist)
	//                            {
	//                                var dis = Vector2.Distance(c.rect.position, mousePos);
	//                                if (dis < minDis)
	//                                {
	//									nearPortId = port.GetPortId(index);;
	//                                    minDis = dis;
	//                                }
	//                                index++;
	//                            }
	//                        }

	//                    }
	//                }
	//                else
	//                {
	//                    nearPortId = curPortId;
	//                }
	//            }
	//            else
	//            {
	//                nearPortId = null;
	//            }

	//        }
	//        protected void UpdateCurrentData()
	//        {
	//            curNode = null;
	//            foreach (var state in Graph.NodeList)
	//            {
	//                if (state.rect.Contains(mousePos))
	//                {
	//                    curNode = state;
	//                    break;
	//                }
	//            }
	//            curPortId = null;
	//            if (curNode != null)
	//            {
	//                foreach (var port in curNode.Ports)
	//                {
	//                    var index = 0;
	//                    foreach (var c in port.ConnectInfolist)
	//                    {
	//                        if (c.rect.Contains(mousePos))
	//                        {
	//							curPortId = port.GetPortId(index); 
	//                            break;
	//                        }
	//                        index++;
	//                    }

	//                }
	//            }

	//        }
	//        Vector2 mousePos;
	//        QFlowNode curNode;
	//        PortId? curPortId;
	//        PortId? nearPortId;
	//       protected  void OnQGUI()
	//        {
	//            ViewRange.size = position.size;
	//            mousePos = Event.current.mousePosition + ViewRange.position;
	//			GUI.DrawTexture(new Rect(Vector2.zero, position.size), QGUI.ColorTexture[QGUI.BackColor]);
	//			if (Graph==null)
	//            {
	//                if (GUILayout.Button("创建新的QFlowGraph"))
	//                {
	//#if UNITY_EDITOR
	//					var graphAsset = CreateInstance<QFlowGraphAsset>();
	//					AssetDatabase.CreateAsset(graphAsset, "Assets/" + nameof(QFlowGraphAsset) + ".asset");
	//					Graph = graphAsset.Graph;
	//#else
	//					Graph = new QFlowGraph();
	//#endif
	//				}
	//				return;
	//            }
	//            Controls();
	//            BeginWindows();
	//            for (int i = 0; i <Graph.NodeList.Count; i++)
	//            {
	//                var node = Graph.NodeList[i];
	//                if (node == null)
	//                {
	//                    Debug.LogError(i + "/" +Graph.NodeList.Count);
	//                    continue;
	//                }
	//				if (ViewRange.Overlaps(node.rect))
	//				{
	//					node.rect.position -= ViewRange.position;
	//					var select = SelectNodes.Contains(node);
	//					var color = node.commandKey.ToColor(select ? 0.35f : 0.4f, select ? 1f : 0.9f);
	//					if (Application.isPlaying)
	//					{
	//						switch (node.State)
	//						{
	//							case QNodeState.闲置:
	//								color = Color.Lerp(color, Color.gray, 0.8f);
	//								break;
	//							case QNodeState.运行中:
	//								color = Color.Lerp(color, Color.green, 0.8f);
	//								break;
	//							case QNodeState.失败:
	//								color = Color.Lerp(color, Color.red, 0.8f);
	//								break;
	//							default:
	//								break;
	//						}
	//					}
	//					QGUI.PushBackColor(color);
	//					var newRect = Window(i, node.rect, DrawNode, node.ViewName);
	//					if (newRect != node.rect)
	//					{
	//						var offset = newRect.position - node.rect.position;
	//						foreach (var selectNode in SelectNodes)
	//						{
	//							if (selectNode != null)
	//							{
	//								selectNode.rect.position += offset;
	//							}
	//						}
	//						node.rect = newRect;
	//					}
	//					QGUI.PopBackColor();
	//					node.rect.position += ViewRange.position;
	//                }

	//            }
	//            EndWindows();
	//            DrawCurve();
	//            switch (ControlState)
	//            {
	//                case EditorState.BoxSelect:
	//                    {
	//						QGUI.PushColor(Color.black);
	//                        var box = SelectBox;
	//                        box.position -= ViewRange.position;
	//                        GUI.Box(box, "");
	//						QGUI.PopColor();
	//					}
	//                    break;
	//                default:
	//                    break;
	//            }
	//			if (Graph.IsRunning)
	//			{
	//				Repaint();
	//			}
	//		}
	//		enum EditorState
	//        {
	//            None,
	//            MoveOffset,
	//            BoxSelect,
	//            ConnectPort,
	//            MoveNode,
	//        }
	//        EditorState ControlState = EditorState.None;
	//        Vector2 StartPos = Vector2.zero;
	//        Rect SelectBox = new Rect();
	//        List<QFlowNode> SelectNodes = new List<QFlowNode>();
	//		private long lastClickTime = 0;

	//		void Controls()
	//        {
	//            switch (Event.current.type)
	//            {
	//                case EventType.MouseDown:
	//                    {
	//                        UpdateCurrentData();
	//						if (Event.current.button == 0)
	//						{
	//							if (curPortId != null&& ControlState == EditorState.None)
	//                            {
	//                                if (Graph.GetPort(curPortId.Value).IsOutput)
	//                                {
	//                                    StartConnect(curPortId.Value);
	//                                }
	//                                else
	//                                {
	//                                    var curPort = Graph.GetPort(curPortId);
	//                                    var fromPort = Graph.GetConnectInfo(curPortId).FirstConnect;
	//                                    if (fromPort != null)
	//                                    {
	//                                        Graph.GetPort(fromPort).DisConnect(curPortId, fromPort.Value.index);
	//                                        StartConnect(fromPort);
	//                                    }
	//                                }
	//                                Event.current.Use();
	//                            }
	//                            else if(ControlState == EditorState.None)
	//                            {
	//                                if (curNode == null)
	//                                {
	//                                    StartPos = mousePos;
	//                                    SelectBox = new Rect(StartPos, Vector2.zero);
	//                                    ControlState = EditorState.BoxSelect;
	//								}
	//								else
	//								{
	//									if (!SelectNodes.Contains(curNode))
	//									{
	//										SelectNodes.Clear();
	//									}
	//									ControlState = EditorState.MoveNode;
	//								}
	//							}
	//						}


	//                    }
	//                    break;
	//                case EventType.MouseDrag:
	//                    {
	//                        switch (ControlState)
	//                        {
	//                            case EditorState.BoxSelect:
	//                                {
	//                                    var endPos = mousePos;
	//                                    SelectBox = new Rect(Mathf.Min(StartPos.x, endPos.x), Mathf.Min(StartPos.y, endPos.y), Mathf.Abs(StartPos.x - endPos.x), Mathf.Abs(StartPos.y - endPos.y));
	//                                    Repaint();
	//                                }
	//                                break;
	//							case EditorState.None:
	//                            case EditorState.MoveOffset:
	//                                if (Event.current.delta.magnitude < 100)
	//                                {
	//                                    ViewRange.position -= Event.current.delta;
	//                                    ControlState = EditorState.MoveOffset;
	//                                    Repaint();
	//                                }
	//                                break;
	//                            case EditorState.ConnectPort:
	//                                UpdateNearPort(); Repaint();
	//                                break;
	//                            default:
	//                                break;
	//                        }

	//                    }
	//                    break;
	//                case EventType.MouseUp:
	//                    {
	//						UpdateCurrentData();
	//                        switch (ControlState)
	//                        {
	//                            case EditorState.BoxSelect:
	//                                {
	//                                    SelectNodes.Clear();
	//                                    foreach (var node in Graph.NodeList)
	//                                    {
	//                                        var rect = node.rect;
	//                                        if (SelectBox.Overlaps(rect))
	//                                        {
	//                                            SelectNodes.Add(node);
	//                                        }
	//                                    }
	//                                    Repaint();
	//                                }
	//                                break;
	//                            case EditorState.ConnectPort:
	//                                {
	//                                    StopConnect(nearPortId);
	//                                    Event.current.Use();
	//                                }
	//                                break;
	//                            case EditorState.None:
	//                                if (Event.current.button == 1)
	//                                {
	//                                    ShowMenu();
	//                                    Event.current.Use();
	//                                }
	//                                else
	//                                {
	//									SelectNodes.Clear();
	//                                }
	//                                break;
	//                            default:
	//								{
	//									if (Event.current.button == 0)
	//									{
	//										if (curNode?.command != null && curNode.command.method.Name == nameof(QFlowGraphNode.GraphAsset))
	//										{

	//											if (lastClickTime.GetIntervalSeconds() < 0.5f)
	//											{
	//												if (curNode["asset"] is QFlowGraphAsset asset)
	//												{
	//#pragma warning disable CS0618 // 类型或成员已过时
	//													Open(asset.Graph, asset.SetDirty);
	//#pragma warning restore CS0618 // 类型或成员已过时
	//												}
	//											}
	//											lastClickTime = QTime.Timestamp;
	//										}
	//									}
	//								}
	//                                break;
	//                        } 

	//                        ControlState = EditorState.None;
	//                    }
	//                    break;
	//                case EventType.KeyUp:
	//                    {
	//                        switch (Event.current.keyCode)
	//                        {
	//                            case KeyCode.Delete:
	//                                DeleteSelectNodes();
	//                                break;
	//                            case KeyCode.C:
	//                                if (Event.current.control)
	//                                {
	//                                    Copy();
	//                                }
	//                                break;
	//                            case KeyCode.V:
	//                                if (Event.current.control&&curNode==null) 
	//                                {
	//                                    Parse();
	//                                }
	//                                break;
	//                            default:
	//                                break;
	//                        }
	//                    }break;
	//#if UNITY_EDITOR
	//				case EventType.DragUpdated:
	//					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
	//					break;
	//				case EventType.DragPerform:
	//					DragAndDrop.AcceptDrag();
	//					AddObject(DragAndDrop.objectReferences);
	//					break;
	//#endif
	//				case EventType.ScrollWheel:
	//					if (Event.current.shift)
	//					{
	//						ViewRange.position += new Vector2(Event.current.delta.y, Event.current.delta.x)*4;
	//					}
	//					else
	//					{
	//						ViewRange.position += Event.current.delta*4;
	//					}
	//					Repaint();
	//					break;
	//				default: break;
	//            }
	//        }
	//#region 图形绘制
	//        static float Fix(float pos, float min, float max, float fixStep)
	//        {
	//            while (pos > max)
	//            {
	//                pos -= fixStep;
	//            } while (pos < min)
	//            {
	//                pos += fixStep;
	//            }
	//            return pos;
	//        }
	//        Rect windowRect;
	//        void DrawNode(int id)
	//        {
	//			QGUI.BeginRuntimeGUI();
	//            var node = Graph.NodeList[id];
	//			if (node == null) return;
	//            windowRect = node.rect;
	//#if UNITY_EDITOR
	//			EditorGUI.DrawRect(new Rect(1, 21, windowRect.width - 2, 2), QGUI.AlphaBackColor);
	//			EditorGUI.DrawRect(new Rect(1, 21, windowRect.width - 2, windowRect.height - 20), QGUI.AlphaBackColor);
	//#endif
	//			GUILayout.BeginHorizontal();
	//			GUILayout.Space(QGUI.Size*2);
	//            GUILayout.BeginVertical();
	//			GUILayout.Space(QGUI.Size);
	//			if (node.command == null)
	//            {
	//				GUILayout.Label("丢失【" + node.commandKey + "】 ",GUILayout.MaxWidth(200));
	//			}
	//			foreach (var port in node.Ports)
	//			{
	//				DrawPort(port);
	//			}
	//			GUILayout.EndVertical();
	//			GUILayout.Space(QGUI.Size*2);
	//			GUILayout.EndHorizontal();
	//            if (Event.current.type== EventType.Repaint)
	//            {
	//                node.rect.height = GUILayoutUtility.GetLastRect().height + 30;
	//            }
	//            GUI.DragWindow();
	//			QGUI.EndRuntimeGUI();
	//		}
	//        void DrawCurve(Vector2 start, Vector2 end,Color color,bool isFlow=false)
	//        {
	//            if (!ViewRange.Contains(start) &&!ViewRange.Contains(end)) return;
	//			if (isFlow)
	//			{
	//				DrawCurve(start + Vector2.up * 2, end + Vector2.up * 2, color);
	//				DrawCurve(start + Vector2.down * 2, end + Vector2.down *2, color);
	//			}
	//			start -= ViewRange.position;
	//			end -= ViewRange.position;
	//			if (Vector3.Distance(start, end) < 0.1f)
	//			{
	//				return;
	//			}
	//			float size = (end.x - start.x) / 2;
	//			var yMax = Mathf.Min(end.y, start.y) - 100;
	//			var t = Mathf.Clamp(start.x - end.x, 0, 100) / 100;
	//			var startOffset = Vector2.Lerp(start + Vector2.right * size, new Vector2(start.x + 100, yMax),t );
	//			var endOffset= Vector2.Lerp(end + Vector2.left * size, new Vector2(end.x - 100, yMax), t);
	//#if UNITY_EDITOR
	//			Handles.DrawBezier(start, end, startOffset, endOffset, color, null, 3f);
	//#endif
	//		}
	//		public void DrawCurve()
	//		{
	//			if (connectStartPort != null)
	//			{
	//				var connectInfo = Graph.GetConnectInfo(connectStartPort);
	//				var color = GetTypeColor(Graph.GetPort(connectStartPort).ConnectType);
	//				DrawCurve(connectInfo.rect.center, mousePos, color, Graph.GetPort(connectStartPort).IsFlow);
	//				DrawDot(mousePos - ViewRange.position, 1, color, true);
	//				if (nearPortId != null)
	//				{
	//					DrawCurve(connectInfo.rect.center, Graph.GetConnectInfo(nearPortId.Value).rect.center,Color.Lerp( color,Color.clear,0.4f), Graph.GetPort(connectStartPort).IsFlow);
	//				}
	//			}
	//			foreach (var name in Graph.NodeList)
	//			{
	//				foreach (var port in name.Ports)
	//				{
	//					if (port.IsOutput)
	//					{
	//						var color = GetTypeColor(port.ConnectType);
	//						foreach (var c in port.ConnectInfolist)
	//						{
	//							foreach (var connect in c.ConnectList)
	//							{
	//								var next = Graph.GetConnectInfo(connect);
	//								if (next != null)
	//								{
	//									DrawCurve(c.rect.center, next.rect.center, color, port.IsFlow&&Graph.GetPort(connect).IsFlow);
	//								}
	//							}
	//						}

	//					}
	//				}
	//			}
	//		}

	//		public static Texture2D DotTexture => _DotTexture ??= Resources.Load<Texture2D>(nameof(QGUI) + "/" + nameof(DotTexture));
	//		static Texture2D _DotTexture = null;
	//		public static Texture2D DotConnectTexture => _DotConnectTexture ??= Resources.Load<Texture2D>(nameof(QGUI) + "/" + nameof(DotConnectTexture));
	//		static Texture2D _DotConnectTexture = null;
	//		Rect DrawDot(Vector2 center,float size,Color color,bool isConnect)
	//        {
	//            var rect = new Rect();
	//			rect.size = Vector3.one * QGUI.Size * size*1.4f;
	//            rect.center = center;
	//			QGUI.PushColor(color);
	//			if (isConnect)
	//			{
	//				GUI.DrawTexture(rect, DotConnectTexture);
	//			}
	//			else
	//			{
	//				GUI.DrawTexture(rect, DotTexture);
	//			}
	//			QGUI.PopColor();
	//			rect.size *= 2;
	//			rect.center = center;
	//			return rect;
	//        }

	//        void DrawPort(QFlowPort port)
	//        {
	//            curDrawPort = port;
	//            Rect lastRect = default;
	//            if (port.Key == QFlowKey.NextPort|| port.Key == QFlowKey.FromPort)
	//            {
	//                lastRect = new Rect(50, 5, windowRect.width - 100, 20);
	//            }
	//            else
	//			{
	//				if (port.IsList)
	//				{
	//					port.Value = port.Value.Draw(port.ViewName, port.ValueType,  port.parameterInfo, DrawList, port.IndexChange);
	//					return;
	//				}
	//				else
	//				{
	//					if (port.IsShowValue())
	//					{
	//						port.Value = port.Value.Draw(port.ViewName, port.ValueType);
	//					}
	//					else
	//					{
	//						GUILayout.Label(port.ViewName, port.IsOutput ? QGUI.RightLabel : QGUI.LeftLable);
	//					}
	//				}
	//				lastRect = GUILayoutUtility.GetLastRect();
	//			}
	//			DrawPortDot(lastRect, port.GetPortId(0), port.IsOutput, port.ConnectType);
	//		}
	//        QFlowPort curDrawPort;
	//        object DrawList(int i, object value,string name,Type type)
	//        {
	//            if (curDrawPort == null) return value;
	//			if (curDrawPort.IsShowValue(i))
	//			{
	//				value= value.Draw(name, type);
	//			}
	//			else
	//			{
	//				GUILayout.Label(name, curDrawPort.IsOutput ? QGUI.RightLabel : QGUI.LeftLable);
	//			}
	//			DrawPortDot(GUILayoutUtility.GetLastRect(), curDrawPort.GetPortId(i), curDrawPort.IsOutput, curDrawPort.ConnectType);
	//			return value;
	//		}
	//		public void DrawPortDot(Rect rect, PortId port, bool isOutput, Type connectType)
	//		{
	//			var typeColor = GetTypeColor(connectType);
	//			Rect dotRect = default;
	//			var connectInfo = Graph.GetConnectInfo(port);
	//			if (connectInfo != null)
	//			{
	//				if (isOutput)
	//				{
	//					var center = new Vector2(rect.xMax, rect.y) + Vector2.one * QGUI.Size;
	//					dotRect = DrawDot(center, Equals(connectStartPort, port) ? 1.2f : 1, typeColor, connectInfo.ConnectList.Count > 0 || Equals(connectStartPort, port));
	//				}
	//				else
	//				{
	//					var center = rect.position + new Vector2(-QGUI.Size, QGUI.Size);
	//					var canConnect = connectStartPort != null && Graph.GetPort(connectStartPort).CanConnect(connectType);
	//					dotRect = DrawDot(center, (canConnect ? 1.2f : 1), typeColor, connectInfo.ConnectList.Count > 0 || Equals(nearPortId, port));
	//				}
	//				if (Event.current.type == EventType.Repaint)
	//				{
	//					connectInfo.rect = new Rect(dotRect.position + windowRect.position, dotRect.size);
	//				}
	//			}
	//		}

	//#endregion

	//        void StartConnect(PortId? startPort)
	//        {
	//            if (startPort == null) return;
	//            ControlState = EditorState.ConnectPort;
	//            connectStartPort = startPort;
	//        }
	//        void StopConnect(PortId? endPort)
	//        {
	//            ControlState = EditorState.None;
	//            if (endPort != null)
	//            {
	//               Graph.GetPort(connectStartPort).Connect(endPort,connectStartPort.Value.index);
	//            }
	//            else
	//			{
	//				CreateMenu(connectStartPort.Value);
	//            }
	//            connectStartPort = null;
	//			nearPortId = null;
	//        }
	//        PortId? connectStartPort;
	//        QDictionary<string, Color> KeyColor = new QDictionary<string, Color>();

	//        public Color GetTypeColor(Type type,float s=0.4f,float v=0.9f)
	//        {
	//			if (type == null) return Color.black;
	//            if (type == QFlow.Type) return Color.HSVToRGB(0.6f, s, v);
	//            return type.Name.ToColor(s,v);
	//        }


	//    }
}
