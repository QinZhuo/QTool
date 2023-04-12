using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif
using UnityEngine;
using QTool.Reflection;
namespace QTool.FlowGraph
{
    public class QFlowGraphWindow : QGUIEditorWindow
	{
		
		public static string OpenPathKey => nameof(QFlowGraphWindow) + "_" + nameof(Graph) + "Path";
		static QFlowGraph Graph = null;
#if UNITY_EDITOR
		[OnOpenAsset(0)]
        public static bool OnOpen(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as QFlowGraphAsset;
            if (asset != null)
            {
				PlayerPrefs.SetString(OpenPathKey, AssetDatabase.GetAssetPath(asset));
				Open(asset.Graph, asset.Save);
                return true;
			}
            return false;
        }
		public static void AutoLoadPath()
		{
			if (Graph != null) return;
			var path = PlayerPrefs.GetString(OpenPathKey);
			if (path.IsNull())
			{
				PlayerPrefs.SetString(OpenPathKey, "");
				return;
			}
			var asset = AssetDatabase.LoadAssetAtPath<QFlowGraphAsset>(path);
			Open(asset.Graph, asset.Save);
		}
		[MenuItem("Assets/QTool/Create/QFlowGraph")]
		public static void CreateNewFile()
		{
			var selectPath = Application.dataPath;
			if (Selection.activeObject != null)
			{
				selectPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			}
			var path = EditorUtility.SaveFilePanel("保存QFG文件", selectPath, nameof(QFlowGraphAsset), "qfg");
			if (!string.IsNullOrWhiteSpace(path))
			{
				QFileManager.Save(path, (new QFlowGraph()).ToQData());
				AssetDatabase.Refresh();
				var asset = AssetDatabase.LoadAssetAtPath<QFlowGraphAsset>(path.ToAssetPath());
				Open(asset.Graph, asset.Save);
			}
		}
		[MenuItem("QTool/窗口/流程图")]
		public static void OpenWindow()
		{
			Open(null, null);
		}
#endif



		public static QFlowGraphWindow Instance { get; private set; }
		public event Action OnSave;

		public static void Open(QFlowGraph graph,Action OnSave)
        {
            if (Instance == null)
            {
                Instance = GetWindow<QFlowGraphWindow>(); 
                Instance.minSize = new Vector2(400, 300);
            }
            Instance.titleContent = new GUIContent((graph?.Name== null?"":graph.Name + " - ") + nameof(QFlowGraph));
			Graph = graph;
#if UNITY_EDITOR
			if (Graph == null)
			{
				AutoLoadPath();
			}
#endif
			Instance.ViewRange = new Rect(Graph.IsNull()?Vector2.zero:graph.ViewPos, Instance.position.size);
			Instance.Show();
			Instance.OnSave = OnSave;

		}


#if UNITY_EDITOR
		private void OnFocus()
        {
			AutoLoadPath();
		}
#endif
		protected override void OnLostFocus()
        { 
			if (Graph != null)
			{
				Graph.SetDirty();
				Graph.ViewPos = ViewRange.position;
				OnSave?.Invoke();
			}
        }

        private Rect ViewRange;
        void CreateMenu(PortId fromPortId)
        {
			QGenericMenu menu = new QGenericMenu();
            var fromPort = Graph.GetPort(fromPortId);
            foreach (var info in QCommand.KeyDictionary)
            {
                if (fromPort.CanConnect(info, out var portKey))
                {
                    menu.AddItem(new GUIContent(info.fullName), false, () =>
					{
						SelectNodes.Clear();
						var node =Graph.AddNode(info.Key);
                        node.rect = new Rect(mousePos, new Vector2(300, 80));
                        fromPort.Connect( node.Ports[portKey],fromPortId.index);
						SelectNodes.Add(node);

					});
                }
			}
			var value = fromPort.GetValue(fromPortId.index);
			if (!value.IsNull())
			{
				var typeInfo = QReflectionType.Get(value.GetType());
				if (typeInfo.Type != typeof(QFlow))
				{
					foreach (var funcInfo in typeInfo.Functions)
					{
						if (funcInfo.IsPublic)
						{
							menu.AddItem(new GUIContent("内置函数/" + funcInfo.MethodInfo.DeclaringType.QTypeName() + "/" + funcInfo.Name.Replace("_", "/")), false, () =>
							{
								SelectNodes.Clear();
								var node = Graph.AddNode(nameof(QFlowGraphNode.ObjectFunction));
								node.rect = new Rect(mousePos, new Vector2(300, 80));
								fromPort.Connect(node.Ports[QFlowKey.Object], fromPortId.index);
								node.Ports[QFlowKey.FuncName].Value = funcInfo.Name;
								var param = new object[funcInfo.ParamInfos.Length];
								for (int i = 0; i < funcInfo.ParamInfos.Length; i++)
								{
									param[i] = funcInfo.ParamInfos[i].HasDefaultValue ? funcInfo.ParamInfos[i].DefaultValue : QReflection.CreateInstance(funcInfo.ParamInfos[i].ParameterType);
								}
								node.Ports[QFlowKey.Param].Value = param;
								SelectNodes.Add(node);
							}
							);
						

						}
					}
				}
			}
		
            menu.ShowAsContext();
        }
		void AddObject(params UnityEngine.Object[] objs)
		{
			SelectNodes.Clear();
			foreach (var obj in objs)
			{
				if(obj is QFlowGraphAsset asset)
				{
					var node = Graph.AddNode(nameof(QFlowGraphNode.GraphAsset));
					node.rect = new Rect(mousePos, new Vector2(300, 80));
					node.Ports[QFlowKey.Object].Value = asset;
					SelectNodes.Add(node);
				}
				else
				{
					var node = Graph.AddNode(nameof(QFlowGraphNode.ObjectInstance));
					node.rect = new Rect(mousePos, new Vector2(300, 80));
					node.Ports[QFlowKey.Object].Value = obj;
					SelectNodes.Add(node);
				}
			}
		}
        private void ShowMenu()
        {
			QGenericMenu menu = new QGenericMenu();
            if (curNode == null)
            {
                foreach (var kv in QCommand.KeyDictionary)
                {
                    menu.AddItem(new GUIContent(kv.fullName), false, () =>
					{
						SelectNodes.Clear();
						var node =Graph.AddNode(kv.Key);
                       node.rect = new Rect(mousePos, new Vector2(300, 80));
					   SelectNodes.Add(node);

				   });
                }
                if (!string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
				{
					menu.AddSeparator("");
					menu.AddItem(new GUIContent("粘贴"), false, Parse);
                }
            }
            else
            {
                if (curPortId != null)
                {

                    menu.AddItem(new GUIContent("清空" + curPortId + "端口连接"), false, ()=> Graph.GetPort(curPortId)?.ClearAllConnect(curPortId.Value.index));
                }
                else
                {
                    menu.AddItem(new GUIContent("复制"), false, Copy);
                    if (!string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
                    {
                        menu.AddItem(new GUIContent("粘贴"), false, Parse);
                    }
                    menu.AddItem(new GUIContent("删除"), false, DeleteSelectNodes);
                    menu.AddItem(new GUIContent("清空连接"), false, ClearAllConnect);
                    if (Application.isPlaying)
					{
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("运行节点"), false, () =>
						{
							Graph.Run(curNode.Key);
                        });
                    }
                }

            }
            menu.ShowAsContext();
        }
        void Copy()
        {
            if (SelectNodes.Count == 0)
            {
                SelectNodes.Add(curNode);
            }

            GUIUtility.systemCopyBuffer = SelectNodes.ToQData();
        }
        void Parse()
        {
            try
            {
                var nodeList = GUIUtility.systemCopyBuffer.ParseQData<List<QFlowNode>>();
				Graph.Parse(nodeList, mousePos);
            }
            catch (Exception e)
            {
                throw new Exception("粘贴出错", e);
            }
        }
        void ClearAllConnect()
        {
            ForeachSelectNodes((node) => node.ClearAllConnect());
        }
        void DeleteSelectNodes()
		{
			ForeachSelectNodes((node) => Graph.Remove(node));
			Repaint();
		}
        void ForeachSelectNodes(System.Action<QFlowNode> action)
        {
            if (SelectNodes.Count > 0)
            {
                foreach (var node in SelectNodes)
                {
                    action(node);
                }
            }
            else
            {
                action(curNode);
            }
        }
        public void UpdateNearPort()
        {
            UpdateCurrentData();
            if (curNode != null)
            {
                if (curPortId == null)
                {
                    if (curNode.Key == connectStartPort?.node) {
                        nearPortId = null;return;
                    }
                    nearPortId = null;
                    var minDis = float.MaxValue;
                    foreach (var port in curNode.Ports)
                    {
                        
                        if (Graph.GetPort(connectStartPort).CanConnect(port))
                        {
                            var index = 0;
                            foreach (var c in port.ConnectInfolist)
                            {
                                var dis = Vector2.Distance(c.rect.position, mousePos);
                                if (dis < minDis)
                                {
									nearPortId = port.GetPortId(index);;
                                    minDis = dis;
                                }
                                index++;
                            }
                        }

                    }
                }
                else
                {
                    nearPortId = curPortId;
                }
            }
            else
            {
                nearPortId = null;
            }

        }
        protected void UpdateCurrentData()
        {
            curNode = null;
            foreach (var state in Graph.NodeList)
            {
                if (state.rect.Contains(mousePos))
                {
                    curNode = state;
                    break;
                }
            }
            curPortId = null;
            if (curNode != null)
            {
                foreach (var port in curNode.Ports)
                {
                    var index = 0;
                    foreach (var c in port.ConnectInfolist)
                    {
                        if (c.rect.Contains(mousePos))
                        {
							curPortId = port.GetPortId(index); 
                            break;
                        }
                        index++;
                    }
                   
                }
            }

        }
        Vector2 mousePos;
        QFlowNode curNode;
        PortId? curPortId;
        PortId? nearPortId;
       protected override void OnQGUI()
        {
            ViewRange.size = position.size;
            mousePos = Event.current.mousePosition + ViewRange.position;
			GUI.DrawTexture(new Rect(Vector2.zero, position.size), QGUI.ColorTexture[QGUI.BackColor]);
			if (Graph==null)
            {
                if (GUILayout.Button("创建新的QFlowGraph"))
                {
#if UNITY_EDITOR
					CreateNewFile();
#else
					Graph = new QFlowGraph();
#endif
				}
				return;
            }
            Controls();
            BeginWindows();
            for (int i = 0; i <Graph.NodeList.Count; i++)
            {
                var node = Graph.NodeList[i];
                if (node == null)
                {
                    Debug.LogError(i + "/" +Graph.NodeList.Count);
                    continue;
                }
				if (ViewRange.Overlaps(node.rect))
				{
					node.rect.position -= ViewRange.position;
					var select = SelectNodes.Contains(node);
					var color = node.commandKey.ToColor(select ? 0.35f : 0.4f, select ? 1f : 0.9f);
					if (Application.isPlaying)
					{
						switch (node.State)
						{
							case QNodeState.闲置:
								color = Color.Lerp(color, Color.gray, 0.8f);
								break;
							case QNodeState.运行中:
								color = Color.Lerp(color, Color.green, 0.8f);
								break;
							case QNodeState.失败:
								color = Color.Lerp(color, Color.red, 0.8f);
								break;
							default:
								break;
						}
					}
					QGUI.PushBackColor(color);
					var newRect = Window(i, node.rect, DrawNode, node.ViewName);
					if (newRect != node.rect)
					{
						var offset = newRect.position - node.rect.position;
						foreach (var selectNode in SelectNodes)
						{
							if (selectNode != null)
							{
								selectNode.rect.position += offset;
							}
						}
						node.rect = newRect;
					}
					QGUI.PopBackColor();
					node.rect.position += ViewRange.position;
                }

            }
            EndWindows();
            DrawCurve();
            switch (ControlState)
            {
                case EditorState.BoxSelect:
                    {
						QGUI.PushColor(Color.black);
                        var box = SelectBox;
                        box.position -= ViewRange.position;
                        GUI.Box(box, "");
						QGUI.PopColor();
					}
                    break;
                default:
                    break;
            }
			if (Graph.IsRunning)
			{
				Repaint();
			}
		}
		enum EditorState
        {
            None,
            MoveOffset,
            BoxSelect,
            ConnectPort,
            MoveNode,
        }
        EditorState ControlState = EditorState.None;
        Vector2 StartPos = Vector2.zero;
        Rect SelectBox = new Rect();
        List<QFlowNode> SelectNodes = new List<QFlowNode>();
        void Controls()
        {
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    {
                        UpdateCurrentData();
                        if(ControlState== EditorState.None&&Event.current.button == 0)
                        {
                            if (curPortId != null)
                            {
                                if (Graph.GetPort(curPortId.Value).IsOutput)
                                {
                                    StartConnect(curPortId.Value);
                                }
                                else
                                {
                                    var curPort = Graph.GetPort(curPortId);
                                    var fromPort = Graph.GetConnectInfo(curPortId).FirstConnect;
                                    if (fromPort != null)
                                    {
                                        Graph.GetPort(fromPort).DisConnect(curPortId, fromPort.Value.index);
                                        StartConnect(fromPort);
                                    }
                                }
                                Event.current.Use();
                            }
                            else 
                            {
                                if (curNode == null)
                                {
                                    StartPos = mousePos;
                                    SelectBox = new Rect(StartPos, Vector2.zero);
                                    ControlState = EditorState.BoxSelect;
                                }
                                else
                                {
                                    ControlState = EditorState.MoveNode;
                                }
                            }
                        }
                        
                    }
                    break;
                case EventType.MouseDrag:
                    {
                        switch (ControlState)
                        {
                            case EditorState.BoxSelect:
                                {
                                    var endPos = mousePos;
                                    SelectBox = new Rect(Mathf.Min(StartPos.x, endPos.x), Mathf.Min(StartPos.y, endPos.y), Mathf.Abs(StartPos.x - endPos.x), Mathf.Abs(StartPos.y - endPos.y));
                                    Repaint();
                                }
                                break;
                            case EditorState.None:
                            case EditorState.MoveOffset:
                                if (Event.current.delta.magnitude < 100)
                                {
                                    ViewRange.position -= Event.current.delta;
                                    ControlState = EditorState.MoveOffset;
                                    Repaint();
                                }
                                break;
                            case EditorState.ConnectPort:
                                UpdateNearPort(); Repaint();
                                break;
                            default:
                                break;
                        }

                    }
                    break;
                case EventType.MouseUp:
                    {

                        UpdateCurrentData();
                        switch (ControlState)
                        {
                            case EditorState.BoxSelect:
                                {
                                    SelectNodes.Clear();
                                    foreach (var node in Graph.NodeList)
                                    {
                                        var rect = node.rect;
                                        if (SelectBox.Overlaps(rect))
                                        {
                                            SelectNodes.Add(node);
                                        }
                                    }
                                    Repaint();
                                }
                                break;
                            case EditorState.ConnectPort:
                                {
                                    StopConnect(nearPortId);
                                    Event.current.Use();
                                }
                                break;
                            case EditorState.None:
                                if (Event.current.button == 1)
                                {
                                    ShowMenu();
                                    Event.current.Use();
                                }
                                else
                                {
                                    SelectNodes.Clear();
                                }
                                break;
                            default:
                                break;
                        } 

                        ControlState = EditorState.None;
                    }
                    break;
                case EventType.KeyUp:
                    {
                        switch (Event.current.keyCode)
                        {
                            case KeyCode.Delete:
                                DeleteSelectNodes();
                                break;
                            case KeyCode.C:
                                if (Event.current.control)
                                {
                                    Copy();
                                }
                                break;
                            case KeyCode.V:
                                if (Event.current.control&&curNode==null) 
                                {
                                    Parse();
                                }
                                break;
                            default:
                                break;
                        }
                    }break;
#if UNITY_EDITOR
				case EventType.DragUpdated:
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
					break;
				case EventType.DragPerform:
					DragAndDrop.AcceptDrag();
					AddObject(DragAndDrop.objectReferences);
					break;
#endif
				case EventType.ScrollWheel:
					if (Event.current.shift)
					{
						ViewRange.position += new Vector2(Event.current.delta.y, Event.current.delta.x)*4;
					}
					else
					{
						ViewRange.position += Event.current.delta*4;
					}
					Repaint();
					break;
				default: break;
            }
        }
#region 图形绘制
        static float Fix(float pos, float min, float max, float fixStep)
        {
            while (pos > max)
            {
                pos -= fixStep;
            } while (pos < min)
            {
                pos += fixStep;
            }
            return pos;
        }
        Rect windowRect;
        void DrawNode(int id)
        {
			QGUI.BeginRuntimeGUI();
            var node = Graph.NodeList[id];
			if (node == null) return;
            windowRect = node.rect;
#if UNITY_EDITOR
			EditorGUI.DrawRect(new Rect(1, 21, windowRect.width - 2, 2), QGUI.AlphaBackColor);
			EditorGUI.DrawRect(new Rect(1, 21, windowRect.width - 2, windowRect.height - 20), QGUI.AlphaBackColor);
#endif
			GUILayout.BeginHorizontal();
			GUILayout.Space(QGUI.Size*2);
            GUILayout.BeginVertical();
			GUILayout.Space(QGUI.Size);
			if (node.command == null)
            {
				GUILayout.Label("丢失【" + node.commandKey + "】 ",GUILayout.MaxWidth(200));
			}
			foreach (var port in node.Ports)
			{
				DrawPort(port);
			}
			GUILayout.EndVertical();
			GUILayout.Space(QGUI.Size*2);
			GUILayout.EndHorizontal();
            if (Event.current.type== EventType.Repaint)
            {
                node.rect.height = GUILayoutUtility.GetLastRect().height + 30;
            }
            GUI.DragWindow();
			QGUI.EndRuntimeGUI();
		}
        void DrawCurve(Vector2 start, Vector2 end,Color color,bool isFlow=false)
        {
            if (!ViewRange.Contains(start) &&!ViewRange.Contains(end)) return;
			if (isFlow)
			{
				DrawCurve(start + Vector2.up * 2, end + Vector2.up * 2, color);
				DrawCurve(start + Vector2.down * 2, end + Vector2.down *2, color);
			}
			start -= ViewRange.position;
			end -= ViewRange.position;
			if (Vector3.Distance(start, end) < 0.1f)
			{
				return;
			}
			float size = (end.x - start.x) / 2;
			var yMax = Mathf.Min(end.y, start.y) - 100;
			var t = Mathf.Clamp(start.x - end.x, 0, 100) / 100;
			var startOffset = Vector2.Lerp(start + Vector2.right * size, new Vector2(start.x + 100, yMax),t );
			var endOffset= Vector2.Lerp(end + Vector2.left * size, new Vector2(end.x - 100, yMax), t);
#if UNITY_EDITOR
			Handles.DrawBezier(start, end, startOffset, endOffset, color, null, 3f);
#endif
		}
		public void DrawCurve()
		{
			if (connectStartPort != null)
			{
				var connectInfo = Graph.GetConnectInfo(connectStartPort);
				var color = GetTypeColor(Graph.GetPort(connectStartPort).ConnectType);
				DrawCurve(connectInfo.rect.center, mousePos, color, Graph.GetPort(connectStartPort).ConnectType == typeof(QFlow));
				DrawCircle(mousePos - ViewRange.position, color);
				if (nearPortId != null)
				{
					DrawCurve(connectInfo.rect.center, Graph.GetConnectInfo(nearPortId.Value).rect.center,Color.Lerp( color,Color.clear,0.4f), Graph.GetPort(connectStartPort).ConnectType == typeof(QFlow));
				}
			}
			foreach (var name in Graph.NodeList)
			{
				foreach (var port in name.Ports)
				{
					if (port.IsOutput)
					{
						var color = GetTypeColor(port.ConnectType);
						foreach (var c in port.ConnectInfolist)
						{
							foreach (var connect in c.ConnectList)
							{
								var next = Graph.GetConnectInfo(connect);
								if (next != null)
								{
									DrawCurve(c.rect.center, next.rect.center, color, port.ConnectType == typeof(QFlow));
								}
							}
						}

					}
				}
			}
		}
		public static Texture2D CircleTexture => _CircleTexture ??= Resources.Load<Texture2D>(nameof(QGUI)+"/"+nameof(CircleTexture)); 
		static Texture2D _CircleTexture = null;
		public static Texture2D DotTexture => _DotTexture ??= Resources.Load<Texture2D>(nameof(QGUI) + "/" + nameof(DotTexture));
		static Texture2D _DotTexture = null;
		public static Texture2D DotConnectTexture => _DotConnectTexture ??= Resources.Load<Texture2D>(nameof(QGUI) + "/" + nameof(DotConnectTexture));
		static Texture2D _DotConnectTexture = null;
		Rect DrawDot(Vector2 center,float size,Color color,bool isConnect)
        {
            var rect = new Rect();
			rect.size = Vector3.one * QGUI.Size * size*1.4f;
            rect.center = center;
			QGUI.PushColor(color);
			if (isConnect)
			{
				GUI.DrawTexture(rect, DotConnectTexture);
			}
			else
			{
				GUI.DrawTexture(rect, DotTexture);
			}
			QGUI.PopColor();
			rect.size *= 2;
			rect.center = center;
			return rect;
        }
		void DrawCircle(Vector2 center, Color color)
		{
			var rect = new Rect();
			rect.size = Vector3.one * QGUI.Size*0.8f;
			rect.center = center;
			QGUI.PushColor(color);
			GUI.DrawTexture(rect, CircleTexture);
			QGUI.PopColor();
		}
       
        void DrawPort(QFlowPort port)
        {
            curDrawPort = port;
            Rect lastRect = default;
            if (port.Key == QFlowKey.NextPort|| port.Key == QFlowKey.FromPort)
            {
                lastRect = new Rect(50, 5, windowRect.width - 100, 20);
            }
            else
			{
				if (port.IsList)
				{
					port.Value = port.Value.Draw(port.ViewName, port.ValueType,  port.parameterInfo, DrawList, port.IndexChange);
					return;
				}
				else
				{
					if (port.IsShowValue())
					{
						port.Value = port.Value.Draw(port.ViewName, port.ValueType);
					}
					else
					{
						GUILayout.Label(port.ViewName, port.IsOutput ? QGUI.RightLabel : QGUI.LeftLable);
					}
				}
				lastRect = GUILayoutUtility.GetLastRect();
			}
			DrawPortDot(lastRect, port.GetPortId(0), port.IsOutput, port.ConnectType);
		}
        QFlowPort curDrawPort;
        object DrawList(int i, object value,string name,Type type)
        {
            if (curDrawPort == null) return value;
			if (curDrawPort.IsShowValue(i))
			{
				value= value.Draw(name, type);
			}
			else
			{
				GUILayout.Label(name, curDrawPort.IsOutput ? QGUI.RightLabel : QGUI.LeftLable);
			}
			DrawPortDot(GUILayoutUtility.GetLastRect(), curDrawPort.GetPortId(i), curDrawPort.IsOutput, curDrawPort.ConnectType);
			return value;
		}
		public void DrawPortDot(Rect rect, PortId port, bool isOutput, Type connectType)
		{
			var typeColor = GetTypeColor(connectType);
			Rect dotRect = default;
			var connectInfo = Graph.GetConnectInfo(port);
			if (isOutput)
			{
				var center = new Vector2(rect.xMax, rect.y) + Vector2.one * QGUI.Size;
				dotRect=DrawDot(center, Equals(connectStartPort, port)?1.2f:1, typeColor, connectInfo.ConnectList.Count > 0 || Equals(connectStartPort, port));
			}
			else
			{
				var center = rect.position + new Vector2(-QGUI.Size, QGUI.Size);
				var canConnect = connectStartPort != null && Graph.GetPort(connectStartPort).CanConnect(connectType);
				dotRect = DrawDot(center, (canConnect ? 1.2f : 1), typeColor, connectInfo.ConnectList.Count > 0 || Equals(nearPortId, port));
			}
			if (Event.current.type == EventType.Repaint)
			{
				connectInfo.rect = new Rect(dotRect.position + windowRect.position, dotRect.size);
			}
		}

#endregion

        void StartConnect(PortId? startPort)
        {
            if (startPort == null) return;
            ControlState = EditorState.ConnectPort;
            connectStartPort = startPort;
        }
        void StopConnect(PortId? endPort)
        {
            ControlState = EditorState.None;
            if (endPort != null)
            {
               Graph.GetPort(connectStartPort).Connect(endPort,connectStartPort.Value.index);
            }
            else
			{
				CreateMenu(connectStartPort.Value);
            }
            connectStartPort = null;
			nearPortId = null;
        }
        PortId? connectStartPort;
        QDictionary<string, Color> KeyColor = new QDictionary<string, Color>();
   
        public Color GetTypeColor(Type type,float s=0.4f,float v=0.9f)
        {
			if (type == null) return Color.black;
            if (type == QFlow.Type) return Color.HSVToRGB(0.6f, s, v);
            return type.Name.ToColor(s,v);
        }
    
      
    }
#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(QFlowGraph))]
	public class QFlowGraphDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var leftRect = position;
			leftRect.width /= 2;
			EditorGUI.LabelField(leftRect, label.text);
			leftRect.x += leftRect.width;
			if (property.serializedObject.targetObject.IsPrefabInstance(out var prefab))
			{
				if (GUI.Button(leftRect, "进入预制体编辑"))
				{
					UnityEditor.AssetDatabase.OpenAsset(prefab);
				}
			}
			else
			{
				if (GUI.Button(leftRect, "编辑"))
				{
					var graph = property.GetObject() as QFlowGraph;
					var path = property.propertyPath;
					var targetObject = property.serializedObject.targetObject;
					QFlowGraphWindow.Open(graph, () => { graph.Name = path; targetObject.SetDirty(); });
				}
			}
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return base.GetPropertyHeight(property, label);
		}
	}
#endif
}