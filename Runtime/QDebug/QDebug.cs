

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using System.Reflection;

namespace QTool
{
	public static class QDebug
	{
		public static int FPS => (int)Fps.SecondeSum;
		internal static QAverageValue Fps = new QAverageValue();
		static bool DebugPanelShow = false;
		private static QToolBar toolBar = null;
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		private static void Close()
		{
			DebugPanelShow = false;
			toolBar.CancelSelect();
		}
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		private static void InitCommond()
		{
			if (toolBar == null)
			{
				toolBar = new QToolBar();
				toolBar["关闭"].Value = nameof(Close);
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
		static Vector2 ScrollPosition=Vector2.zero;
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
				QGUI.BeginRuntimeGUI();
				GUI.Box(QScreen.AspectGUIRect, "", QGUI.BackStyle);
				GUILayout.BeginArea(QScreen.AspectGUIRect);
				InitCommond();
				using (new GUILayout.HorizontalScope(QGUI.BackStyle))
				{
					var select = toolBar.Draw();
					if (select is QCommandInfo qCommand)
					{
						qCommand.Draw("命令");
						if (QGUI.Button("运行命令"))
						{
							qCommand.Invoke(qCommand.TempValues.ToArray());
							Close();
						}
					}
					else if (nameof(Close).Equals(select))
					{
						Close();
					}
				}
				GUILayout.Space(QGUI.BorderSize);
				GUILayout.Label("层级",QGUI.BackStyle, GUILayout.Width(QScreen.Width * 0.2f),GUILayout.Height(QGUI.BorderSize*2));
				using (var scroll=new GUILayout.ScrollViewScope(ScrollPosition,QGUI.BackStyle,GUILayout.Width(QScreen.Width*0.2f)))
				{
					var objects= UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
					foreach (var obj in objects)
					{
						QGUI.PushContentColor(obj.activeInHierarchy ? Color.white : Color.gray);
						if (QGUI.Button(obj.name))
						{
							obj.SetActive(!obj.activeInHierarchy);
						}
						QGUI.PopContentColor();
					}
					ScrollPosition =scroll.scrollPosition;
				}
				GUILayout.EndArea();
				QGUI.EndRuntimeGUI();
			}
			else if (QDemoInput.Ctrl && QDemoInput.Enter)
			{
				QTime.ChangeScale(nameof(QCommand), 0);
				DebugPanelShow = true;
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
