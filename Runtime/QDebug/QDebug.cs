using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using System.Reflection;
using UnityEngine.SceneManagement;
using QTool.Inspector;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace QTool
{
	public static class QDebug
	{
		public static int FPS =>(int)(FrameCount.SecondeSum);
		private static QAverageValue FrameCount = new QAverageValue();
		private static long LastFrameTime { set; get; } = 0;
		private static Label InfoLabel=null;
		private static VisualElement DebugPanel=null;
		private static VisualElement LeftPanel = null;
		private static VisualElement RightPanel = null;
		private static VisualElement MidPanel = null;
		private static VisualElement DownPanel = null;
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			QToolManager.Instance.OnUpdateEvent += Update;
		}
		public static void Update()
		{
			if ((QInput.Ctrl && QInput.Enter) || InputCircle > 720)
			{
				OpenPanel();
			}
			FrameCount.Push(1);
			LastFrameTime = QTime.Timestamp;
			var useSize = Profiler.GetTotalAllocatedMemoryLong();
			if (InfoLabel == null)
			{
				InfoLabel = QToolManager.Instance.RootVisualElement.AddLabel(Application.productName + " v" + Application.version, TextAnchor.MiddleRight);
				InfoLabel.style.position = Position.Absolute;
				InfoLabel.style.width = new Length(100, LengthUnit.Percent);
			}
			InfoLabel.text = Application.productName + " v" + Application.version+"\t 内存：" + useSize.ToSizeString() + " / " + (useSize + Profiler.GetTotalReservedMemoryLong()).ToSizeString() + "\t 帧率：" + FPS.ToString()+" |";
		}

		static Vector2 LeftScrollPosition = Vector2.zero;
		static Vector2 RightScrollPosition = Vector2.zero;
		static string ObjectFilter = default;
		static string ObjectFilterTemp = default;
	
		static QDictionary<int, List<GameObject>> rootGameObjects = new QDictionary<int, List<GameObject>>((key) => new List<GameObject>());
		
		private static void AddScene(this VisualElement root,Scene scene)
		{
			var child = root.AddFoldout(scene.name,true).contentContainer;
			scene.GetRootGameObjects(rootGameObjects[scene.handle]);
			foreach (var gameObject in rootGameObjects[scene.handle])
			{
				child.AddGameObject(gameObject);
			}
		}
		private static void AddGameObject(this VisualElement root, GameObject gameObject)
		{
			var element = new VisualElement();
			element.style.flexDirection = FlexDirection.Row;
			var childRoot= element.AddFoldout(gameObject.name).contentContainer;
			element.Q<VisualElement>("unity-checkmark").visible = gameObject.transform.childCount > 0;
			root.Add(element);
			for (int i = 0; i < gameObject.transform.childCount; i++)
			{
				childRoot.AddGameObject(gameObject.transform.GetChild(i).gameObject);
			}
		}

		public static GameObject SelectObject { get; private set; }
		private static void DrawSelectObject()
		{
			if (SelectObject != null)
			{
				using (new GUILayout.HorizontalScope())
				{
					if (QGUI.Toggle("", SelectObject.activeSelf) != SelectObject.activeSelf)
					{
						SelectObject.SetActive(!SelectObject.activeSelf);
					}
					SelectObject.name = GUILayout.TextField(SelectObject.name, QGUI.Skin.textField, QGUI.HeightLayout, GUILayout.ExpandWidth(true));
				}

				foreach (var component in SelectObject.GetComponents<Component>())
				{
					if (component == null) continue;
					var typeInfo = QInspectorType.Get(component.GetType());
					typeInfo.DrawComponent(component);
				}
			}
		}
		
		private static Scene? _DontDestroyScene = null;
		public static Scene DontDestroyScene => _DontDestroyScene ??= GetDontDestroyOnLoadScene();
		private static Scene GetDontDestroyOnLoadScene()
		{
			GameObject temp = null;
			try
			{
				temp = new GameObject();
				GameObject.DontDestroyOnLoad(temp);
				Scene dontDestroyOnLoad = temp.scene;
				GameObject.DestroyImmediate(temp);
				temp = null;

				return dontDestroyOnLoad;
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
				return new Scene();
			}
			finally
			{
				if (temp != null)
					GameObject.DestroyImmediate(temp);
			}
		}
		private static RenderTexture GameTexture = null;
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		private static void OpenPanel()
		{
			if (DebugPanel != null && DebugPanel.visible) return;
			if (DebugPanel == null)
			{
				DebugPanel = QToolManager.Instance.RootVisualElement.AddVisualElement();
				var title = DebugPanel.AddVisualElement();
				title.style.backgroundColor = Color.gray;
				title= title.AddGroupBox();
				title.style.flexDirection = FlexDirection.Row;
				title.AddButton("继续游戏", ClosePanel);
				var commond = title.AddVisualElement();
				DebugPanel.style.backgroundColor = Color.Lerp(Color.white, Color.clear, 0.1f);
				DebugPanel.style.width = new Length(100, LengthUnit.Percent);
				DebugPanel.style.height = new Length(100, LengthUnit.Percent);
				LeftPanel = new VisualElement();
				RightPanel = new ScrollView();
				DebugPanel.Split(LeftPanel, RightPanel, 1200);
				DownPanel = new ScrollView();
				var top = new VisualElement();
				LeftPanel.Split(top, DownPanel, 600, TwoPaneSplitViewOrientation.Vertical);
				LeftPanel = new ScrollView();
				MidPanel= new ScrollView();
				top.Split(LeftPanel, MidPanel, 300);
				foreach (var kv in QCommand.NameDictionary)
				{
					if (kv.Value.IsStringCommond)
					{	
						if (kv.Value.fullName.SplitTowString("/", out var start, out var end))
						{
							var group = MidPanel.Q<GroupBox>(start);
							if (group == null)
							{
								MidPanel.AddLabel(start);
								group=MidPanel.AddGroupBox(start);
							}
							var button = group.Q<Button>(end);
							if (button == null)
							{
								group.AddButton(end, () =>
								{
									commond.Clear();
									var view = commond.AddQCommandInfo(kv.Value,ClosePanel);
									view.style.backgroundColor = Color.grey;
								});
							}
						}
						else
						{
							Debug.LogError("命令出错[" + kv.Value.fullName + "]:" + kv.Key);
						}
					}
				}
			}
			#region 场景信息
			LeftPanel.Clear();
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				LeftPanel.AddScene(SceneManager.GetSceneAt(i));
			}
			DebugPanel.visible = true;
			LeftPanel.AddScene(GetDontDestroyOnLoadScene());
			#endregion

			


			QTime.ChangeScale(nameof(QDebug), 0);
			GameTexture = new RenderTexture(QScreen.Width / 2, QScreen.Height / 2, 30);
		}
		private static void ClosePanel()
		{
			DebugPanel.visible = false;
			QTime.RevertScale(nameof(QDebug));
			GameTexture?.Release();
			GameTexture = null;
		}
		static Vector2 last = default;
		static float angle = 0;
		private static float InputCircle
		{
			get
			{
				if (!QInput.PointerPress || QInput.PointerPosition.IsNull())
				{
					last = default;
					angle = 0;
					return angle;
				}
				var point = QInput.PointerPosition;
				if (last.IsNull())
				{
					last = point - QScreen.Size / 2;
					angle = 0;
					return angle;
				}
				point -= QScreen.Size / 2;
				angle += Vector2.Angle(last, point) * (Vector3.Cross(last, point).z > 0 ? -1:1);
				last = point;
				return angle;
			}
		}
	
		public static void DebugRun(string key, Action action)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			var lastMemery = GC.GetTotalMemory(false);
			var lastCount = GC.CollectionCount(0);
			try
			{
				action();
			}
			catch (Exception e)
			{
				Debug.LogError("【" + key + "】运行出错:" + e);
				return;
			}
			Log("【" + key + "】运行时间 " + stopwatch.Elapsed + " 内存使用 " + (GC.GetTotalMemory(false) - lastMemery).ToSizeString() + " 垃圾回收次数 " + (GC.CollectionCount(0) - lastCount));
			stopwatch.Stop();
		}
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void Log(object obj)
		{
			Debug.Log("[" + nameof(QDebug) + "]  " + obj);
		}
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void LogWarning(object obj)
		{
			Debug.LogWarning("[" + nameof(QDebug) + "]  " + obj);
		}
		private static QDictionary<string, long> TimestampList= new QDictionary<string, long>();
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void Begin(string key)
		{
			TimestampList[key] =QTime.Timestamp;
		}
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void End(string key, string resultInfo = "")
		{
			Log(key + " " + resultInfo+ " 时间 " + TimestampList[key].GetIntervalSeconds().ToString("f3") + " s" + " 帧率 " + Mathf.Min(FPS, (int)(1f / LastFrameTime.GetIntervalSeconds())));
		}
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void ChangeProfilerCount(string key, int changeCount=0)
		{
#if Profiler
			var obj = ProfilerCount[key];
			obj.Value=changeCount;
#endif
		}
#if Profiler
		private static readonly ProfilerCategory filerCategory = ProfilerCategory.Scripts;
		private static QDictionary<string, ProfilerCounterValue<int>> ProfilerCount = new QDictionary<string, ProfilerCounterValue<int>>((key) => new ProfilerCounterValue<int>(filerCategory, key, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame));
#endif
	}

}
