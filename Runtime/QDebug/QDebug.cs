using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using System.Reflection;
using UnityEngine.SceneManagement;
using QTool.Inspector;
namespace QTool
{
	public static class QDebug
	{
		public static int FPS => (int)Fps.SecondeSum;
		internal static QAverageValue Fps = new QAverageValue();
		static bool DebugPanelShow = false;
		private static QToolBar toolBar = null;

		static Vector2 LeftScrollPosition = Vector2.zero;
		static Vector2 RightScrollPosition = Vector2.zero;
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void QDebugGUI()
		{
			GUILayout.BeginArea(QScreen.AspectGUIRect);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label(FPS.ToString());
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
			if (DebugPanelShow)
			{
				GUILayout.BeginArea(QScreen.AspectGUIRect,QGUI.BackStyle);
				InitCommond();
				GUILayout.BeginHorizontal(QGUI.BackStyle);
				var select = toolBar.Draw();
				if (select is QCommandInfo qCommand)
				{
					qCommand.Draw("命令");
					if (QGUI.Button("运行命令"))
					{
						qCommand.Invoke(qCommand.TempValues.ToArray());
						ClosePanel();
					}
				}
				else if (nameof(ClosePanel).Equals(select))
				{
					ClosePanel();
				}
				GUILayout.EndHorizontal();
				using (new GUILayout.HorizontalScope())
				{
					using (new GUILayout.VerticalScope(GUILayout.Width(QScreen.Width * 0.2f)))
					{
						GUILayout.Label("场景层级", QGUI.BackStyle, GUILayout.Height(QGUI.Height));
						using (var scroll = new GUILayout.ScrollViewScope(LeftScrollPosition, false, false, QGUI.VerticalScrollbarStyle, QGUI.VerticalScrollbarStyle, QGUI.BackStyle))
						{
							for (int i = 0; i < SceneManager.sceneCount; i++)
							{
								DrawScene(SceneManager.GetSceneAt(i));
							}
							DrawScene(DontDestroyScene);
							LeftScrollPosition = scroll.scrollPosition;
						}
					}
					if (Camera.main != null)
					{
						Camera.main.targetTexture = GameTexture;
						Camera.main.Render();
						GUI.DrawTexture(GUILayoutUtility.GetAspectRect(QScreen.Aspect), GameTexture);
						Camera.main.targetTexture = null;
					}
					else
					{
						GUI.DrawTexture(GUILayoutUtility.GetAspectRect(QScreen.Aspect), GameTexture);
					}
					using (new GUILayout.VerticalScope(GUILayout.Width(QScreen.Width * 0.2f)))
					{
						GUILayout.Label("物体属性", QGUI.BackStyle, GUILayout.Height(QGUI.Height));
						using (var scroll = new GUILayout.ScrollViewScope(RightScrollPosition, false, false, QGUI.VerticalScrollbarStyle, QGUI.VerticalScrollbarStyle, QGUI.BackStyle))
						{
							DrawSelectObject();
							RightScrollPosition = scroll.scrollPosition;
						}
					}
					
				}
				GUILayout.EndArea();
			}
			else if(Event.current.type!= EventType.Layout)
			{
				if ((QDemoInput.Ctrl && QDemoInput.Enter)|| InputCircle > 720)
				{
					OpenPanel();
				}
			}
		}
		static QDictionary<int, List<GameObject>> rootGameObjects = new QDictionary<int, List<GameObject>>((key) => new List<GameObject>());
		private static void DrawScene(Scene scene)
		{
			GUILayout.Label(scene.name, QGUI.BackStyle);
			scene.GetRootGameObjects(rootGameObjects[scene.handle]);
			foreach (var obj in rootGameObjects[scene.handle])
			{
				DrawSceneObject(obj);
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
					SelectObject.name = GUILayout.TextField(SelectObject.name, QGUI.TextFieldStyle, QGUI.HeightLayout, GUILayout.ExpandWidth(true));
				}

				foreach (var component in SelectObject.GetComponents<Component>())
				{
					if (component == null) continue;
					var typeInfo = QInspectorType.Get(component.GetType());
					var show = false;
					using (new GUILayout.HorizontalScope(QGUI.BackStyle))
					{
						show = QGUI.Foldout(typeInfo.Type.Name);
					}
					if (show)
					{
						using (new GUILayout.HorizontalScope())
						{
							GUILayout.Space(QGUI.Height);
							using (new GUILayout.VerticalScope())
							{
								foreach (var memeberInfo in typeInfo.Members)
								{
									using (new GUILayout.HorizontalScope())
									{
										var value= memeberInfo.Get(component);
										var newValue= value.Draw(memeberInfo.QName, memeberInfo.Type);
										if (!Equals(value, newValue))
										{
											memeberInfo.Set(component, newValue);
										}
									}
								}
							}
						}
					}
				}
			}
		}
		private static void DrawSceneObject(GameObject obj)
		{
			if (obj == null) return;
			if (SelectObject == obj)
			{
				GUILayout.BeginHorizontal(QGUI.SelectStyle);
			}
			else
			{
				GUILayout.BeginHorizontal();
			}
			var showChild = false;
			if (obj.transform.childCount > 0)
			{
				showChild = QGUI.Foldout("", obj.GetHashCode());
			}
			else
			{
				GUILayout.Space(QGUI.Height);
			}

			QGUI.PushContentColor(obj.activeInHierarchy ? Color.white : Color.gray);
			if (GUILayout.Button(obj.name, QGUI.LabelStyle, QGUI.HeightLayout))
			{
				SelectObject = obj;
			}
			QGUI.PopContentColor();
			GUILayout.EndHorizontal();
			if (showChild)
			{
				using (new GUILayout.HorizontalScope())
				{
					GUILayout.Space(QGUI.Height);
					using (new GUILayout.VerticalScope())
					{
						for (int i = 0; i < obj.transform.childCount; i++)
						{
							DrawSceneObject(obj.transform.GetChild(i).gameObject);
						}
					}
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
			GUI.skin.verticalScrollbar = QGUI.VerticalScrollbarStyle;
			GUI.skin.verticalScrollbarThumb = QGUI.VerticalScrollbarStyleThumb;
			QTime.ChangeScale(nameof(QDebug), 0);
			DebugPanelShow = true;
			GameTexture = new RenderTexture(QScreen.Width/2, QScreen.Height/2, 30);
			
		}
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		private static void ClosePanel()
		{
			DebugPanelShow = false;
			QTime.RevertScale(nameof(QDebug));
			toolBar.CancelSelect();
			GameTexture.Release();
			GameTexture = null;
		}
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		private static void InitCommond()
		{
			if (toolBar == null)
			{
				toolBar = new QToolBar();
				toolBar["关闭"].Value = nameof(ClosePanel);
				foreach (var kv in QCommand.NameDictionary)
				{
					if (kv.Value.IsStringCommond)
					{
						if (kv.Value.name.SplitTowString("/", out var start, out var end))
						{
							toolBar["命令"][start][kv.Key].Value = kv.Value;
						}
						else if (kv.Value.fullName.SplitTowString("/", out start, out end))
						{
							toolBar["命令"][start][kv.Key].Value = kv.Value;
						}
						else
						{
							Debug.LogError("命令出错[" + kv.Value.fullName + "]:" + kv.Key);
						}
					}
				}
			}
		}
		static Vector2 last = default;
		static float angle = 0;
		private static float InputCircle
		{
			get
			{
				if (!QDemoInput.PointerPress || QDemoInput.PointerPosition.IsNull())
				{
					last = default;
					angle = 0;
					return angle;
				}
				var point = QDemoInput.PointerPosition;
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
		public static QDictionary<string, ProfilerMarker> ProfilerMarkerList = new QDictionary<string, ProfilerMarker>((key)=> new ProfilerMarker(key));
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void StartProfiler(string key)
		{
			var profiler = ProfilerMarkerList[key];
			profiler.Begin();
		}
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void StopProfiler(string key)
		{
			var profiler = ProfilerMarkerList[key];
			profiler.End();
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
