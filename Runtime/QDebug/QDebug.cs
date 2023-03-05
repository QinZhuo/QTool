

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
