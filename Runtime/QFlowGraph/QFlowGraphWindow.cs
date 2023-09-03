using QTool.Reflection;
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
#if UNITY_EDITOR
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
		}
#endif
	#endregion
		public override string GetData(UnityEngine.Object file)
		{
			if (file != null)
			{
#if UNITY_EDITOR
				SerializedProperty = new SerializedObject(file).FindProperty(nameof(QFlowGraphAsset.Graph)).FindPropertyRelative(nameof(QFlowGraph.SerializeString));
#endif
				return (file as QFlowGraphAsset).Graph.SerializeString;
			}
#if UNITY_EDITOR
			else if (SerializedProperty != null)
			{
				return SerializedProperty.stringValue;
			}
#endif
			else
			{
				return new QFlowGraph().ToQData();
			}
		}
		private QFlowGraph Graph { get; set; }
		public override void SaveData()
		{
			if (Graph != null)
			{
				Graph.OnBeforeQSerialize();
				ChangeData(Graph.SerializeString);
#if UNITY_EDITOR
				if (SerializedProperty != null)
				{
					SerializedProperty.stringValue = Graph.ToQData();
					SerializedProperty.serializedObject.ApplyModifiedProperties();
				}
#endif
			}

		}
		protected override void ChangeData(string newValue)
		{
			if (Data != newValue)
			{
				ViewOffset = Vector2.zero;
			}
			base.ChangeData(newValue);
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
							AddConnectView(startId, endId);
						}
					}
				}
			}
		}
		VisualElement Back = null;
		bool MoveOffset = false;
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
				if (data.button < 2)
				{
					MoveOffset = true;
					if (data.target == Back)
					{
						ClearSetect();
					}
				}
			});
			Back.RegisterCallback<MouseDownEvent>(data =>
			{
				if (data.target == Back)
				{
					AutoSaveLoad = true;
				}
			});
			Back.RegisterCallback<MouseMoveEvent>(data =>
			{
				if (data.button > 1) return;
				if (StartPortId != null)
				{
					if (DragConnect != null)
					{
						DragConnect.End = data.mousePosition;
					}
				}
				else if (MoveOffset)
				{
					if(SelectNodes.Count != 0)
					{
						foreach (var node in SelectNodes)
						{
							Vector3 mouseDelta = data.mouseDelta; ;
							node.transform.position += mouseDelta;
							Vector2 pos = node.transform.position;
							Graph.GetNode(node.name).rect.position = pos - ViewOffset;
						}
					}
					else
					{
						ViewOffset += data.mouseDelta;
						foreach (var node in NodeViewList)
						{
							node.transform.position = Graph.GetNode(node.name).rect.position + ViewOffset;
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
				MoveOffset = false;
				var position = data.localMousePosition;
				foreach (var kv in QCommand.KeyDictionary)
				{
					data.menu.AppendAction(kv.fullName, action =>
					{
						var node = Graph.AddNode(kv.Key);
						node.rect.position = position;
						AddNodeView(Back, node);
					});
				}
				if (!GUIUtility.systemCopyBuffer.IsNull())
				{
					data.menu.AppendSeparator();
					data.menu.AppendAction("粘贴", action =>
					{
						var nodeList = new List<QFlowNode>();
						GUIUtility.systemCopyBuffer.ParseQData(nodeList);
						Graph.Parse(nodeList, data.mousePosition - ViewOffset);
						OnLostFocus();
						OnFocus();
					});
				}
			
			});
			Back.RegisterCallback<MouseUpEvent>(data =>
			{
				MoveOffset = false;
				if (StartPortId != null)
				{
					StartPortId = null;
					FreshConnectDotView(DragConnect.StartElement);
					ConnectCanvas.Remove(DragConnect);
					ConnectViewList.Remove(DragConnect);
					DragConnect = null;
				}
			
			});
		}
		protected override void OnLostFocus()
		{
			MoveOffset = false;
			base.OnLostFocus();
		}
		public void ClearSetect()
		{
			var list = SelectNodes.ToArray();
			SelectNodes.Clear();
			foreach (var nodeView in list)
			{
				UpdateNodeSelect(nodeView);
			}
		}
		private List<VisualElement> SelectNodes = new List<VisualElement>();
		private PortId? StartPortId { get; set; }
		private QConnectElement DragConnect { get; set; }
		private Vector2 ViewOffset { get; set; }
		private List<VisualElement> NodeViewList = new List<VisualElement>();
		private List<QConnectElement> ConnectViewList = new List<QConnectElement>();
		private VisualElement AddNodeView(VisualElement root, QFlowNode node)
		{
			var nodeView = root.AddVisualElement();
			var select = nodeView.AddVisualElement().SetBackground(Color.clear,-3);
			select.name = "Select";
			select.style.SetBorder(Color.green.Lerp(Color.black,0.3f), 2);
			select.visible = false;
			NodeViewList.Add(nodeView);
			var color = node.commandKey.ToColor(0.3f, 0.4f);
			nodeView.style.backgroundColor = color;
			nodeView.name = node.Key;
			nodeView.style.SetBorder(Color.black.Lerp(color, 0.5f), 3);
			nodeView.style.position = Position.Absolute;
			nodeView.transform.position = node.rect.position + ViewOffset;
			nodeView.style.width = Mathf.Max(200, node.rect.width);
			nodeView.style.height = new StyleLength(StyleKeyword.Auto);
			
			nodeView.RegisterCallback<MouseDownEvent>(data =>
			{
				if (data.button == 0 && !data.shiftKey)
				{
					ClearSetect();
				}
				SelectNodes.AddCheckExist(nodeView);
				UpdateNodeSelect(nodeView);
			});
		
			nodeView.AddMenu(data =>
			{
				MoveOffset = false;
				data.menu.AppendAction("运行", action =>
				{
					Graph.Run(node.Key);
				});
				data.menu.AppendSeparator();
				data.menu.AppendAction("复制", action =>
				{
					var nodeList = new List<QFlowNode>();
					foreach (var node in SelectNodes.ToArray())
					{
						nodeList.AddCheckExist(Graph.GetNode(node.name));
					}
					GUIUtility.systemCopyBuffer = nodeList.ToQData();
				});
				data.menu.AppendAction("删除", action =>
				{
					foreach (var node in SelectNodes.ToArray())
					{
						RemoveNodeView(node);
					}
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
		private void UpdateNodeSelect(VisualElement visual)
		{
			visual.Q<VisualElement>("Select").visible = SelectNodes.Contains(visual);
		}
		public void RemoveNodeView(VisualElement visual)
		{
			var node = Graph.GetNode(visual.name);
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
			SelectNodes.Remove(visual);

		}
		public void AddPortView(VisualElement root, QFlowPort port)
		{
			if (port.Key == QFlowKey.NextPort || port.Key == QFlowKey.FromPort)
			{
				var title = root.Q<Label>("Title");
				var dot = AddDotView(title, GetColor(port), port.GetPortId());
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
						AddDotView(visual, GetColor(port), port.GetPortId(index));
						FreshPortConnect(port);
						visual.RegisterCallback<MouseEnterEvent>(ValueMouseEnterEvent);
					};
				}
				else
				{
					var dot = AddDotView(row,GetColor(port), port.GetPortId());
					if (port.IsShowValue())
					{
						row.Add(port.ViewName, port.Value, port.ValueType, newValue => port.Value = newValue,port.parameterInfo)
							.RegisterCallback<MouseEnterEvent>(ValueMouseEnterEvent);
					}
					else
					{
						row.AddLabel(port.ViewName, port.IsOutput ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft)
							.RegisterCallback<MouseEnterEvent>(ValueMouseEnterEvent);
					}
				}

			}
			FreshPortConnect(port);
		}
		private void ValueMouseEnterEvent(MouseEnterEvent data)
		{
			AutoSaveLoad = false;
		}
		public Color GetColor(QFlowPort port)
		{
			return port.IsFlow ? Color.HSVToRGB(0.6f, 0.5f, 1) : port.ConnectType.Name.ToColor();
		}
		public QConnectElement AddConnectView(PortId start,PortId? end=null)
		{
			var port = Graph.GetPort(start);
			var color = GetColor(port);
			var visual= ConnectCanvas.AddConnect(color,port.IsFlow?4:2);
			visual.name = start.ToQData() + (end == null ? "" : " " + end.Value.ToQData());
			visual.StartElement = GetDotView(start);
			if (end != null)
			{
				var endPort = Graph.GetPort(end.Value);
				visual.EndElement = GetDotView(end.Value);
				visual.EndColor = GetColor(endPort);
				visual.LineWidth = port.IsFlow && endPort.IsFlow ? 4 : 2;
			}
			ConnectViewList.Add(visual);
			return visual;
		}
		public QConnectElement GetConnectView(PortId start, PortId end)
		{
			return ConnectCanvas.Q<QConnectElement>(start.ToQData() + " " + end.ToQData());
		}
		public void RemoveConnectView(PortId start, PortId end)
		{
			var view = GetConnectView(start, end);
			if (view != null)
			{
				ConnectCanvas.Remove(view);
			}
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
						DragConnect=AddConnectView(StartPortId.Value);
						DragConnect.End = startDot.worldBound.center;
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
						var endPort = Graph.GetPort(portId);
						DragConnect.EndColor = GetColor(endPort);
						DragConnect.LineWidth = port.IsFlow && endPort.IsFlow ? 4 : 2;
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
					QFlowGraphWindow.FilePath = nameof(SerializedProperty); 
					QFlowGraphWindow.SerializedProperty = property.FindPropertyRelative(nameof(QFlowGraph.SerializeString));
					QFlowGraphWindow.OpenWindow();
				}
			});
			return root;
		}
	}

#endif
}
