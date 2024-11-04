using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
namespace QTool {
	public static class QEventManager {
		/// <summary>
		/// 事件列表 对应事件触发时调用对应Action 使用方法： EventList["事件名"]+=Action;
		/// </summary>
		private static QDictionary<string, Action> Events = new QDictionary<string, Action>();
		private static ConcurrentQueue<string> EventQueue = new ConcurrentQueue<string>();
		private static Action OnUpdate = InvokeQueueEvent;
		public static void InvokeEvent(Enum value) {
			InvokeEvent(value.ToString());
		}
		/// <summary>
		/// 触发事件
		/// </summary>
		/// <param name="eventKey">事件名</param>
		public static void InvokeEvent(string eventKey) {
			QDebug.Log("触发事件[" + eventKey + "]");
			try {
				eventKey = eventKey.Trim();
				if (string.IsNullOrWhiteSpace(eventKey)) {
					return;
				}
				if (Events.ContainsKey(eventKey)) {
					Events[eventKey]?.Invoke();
				}
			}
			catch (Exception e) {
				Debug.LogError("触发事件[" + eventKey + "]出错：\n" + e);
			}
		}
		public static void QueueEvent(string eventKey) {
			EventQueue.Enqueue(eventKey);
		}
		public static void InvokeEvent<T>(string eventKey, T value) {
			QEventList<T>.InvokeEvent(eventKey, value);
		}
		public static void QueueEvent<T>(string eventKey, T value) {
			QEventList<T>.EventQueue.Enqueue((eventKey, value));
		}
		public static void Register(Enum eventKey, params Action[] action) {
			var key = eventKey.ToString();
			foreach (var item in action) {
				Register(key, item);
			}
		}
		public static void Register(string eventKey, Action action) {
			Events[eventKey] += action;
		}

		public static void UnRegister(Enum eventKey, params Action[] action) {
			var key = eventKey.ToString();
			foreach (var item in action) {
				UnRegister(key, item);
			}
		}
		public static void UnRegister(string eventKey, Action action) {
			Events[eventKey] -= action;
		}
		public static void Register<T>(string eventKey, Action<T> action) {
			QEventList<T>.EventList[eventKey] += action;
		}
		public static void UnRegister<T>(string eventKey, Action<T> action) {
			QEventList<T>.EventList[eventKey] -= action;
		}
		private static void InvokeQueueEvent() {
			while (EventQueue.Count > 0) {
				if (EventQueue.TryDequeue(out var eventData)) {
					InvokeEvent(eventData);
				}
			}
		}
		public static void Update() {
			OnUpdate();
		}

		private class QEventList<T> {
			/// <summary>
			/// 事件列表 对应事件触发时调用对应Action 使用方法： EventList["事件名"]+=Action;
			/// </summary>
			internal static QDictionary<string, Action<T>> EventList = new QDictionary<string, Action<T>>();
			internal static ConcurrentQueue<(string, T)> EventQueue = new ConcurrentQueue<(string, T)>();
			static QEventList() {
				OnUpdate += InvokeQueueEvent;
			}
			internal static void InvokeEvent(string eventKey, T value) {
				try {
					eventKey = eventKey.Trim();
					if (string.IsNullOrWhiteSpace(eventKey)) {
						return;
					}
					if (EventList.ContainsKey(eventKey)) {
						EventList[eventKey]?.Invoke(value);
					}
				}
				catch (Exception e) {
					Debug.LogError("触发事件[" + eventKey + ":" + value + "]出错\n" + e);
				}
			}
			private static void InvokeQueueEvent() {
				while (EventQueue.Count > 0) {
					if (EventQueue.TryDequeue(out var result)) {
						InvokeEvent(result.Item1, result.Item2);
					}
				}
			}
		}
	}
	public static class QEventTool {
		#region UnityEvent UnityAction 操作拓展
		public static UnityAction GetUnityAction(this UnityEngine.Object obj, string funcName) {
			return UnityEventBase.GetValidMethodInfo(obj, funcName, new Type[0]).CreateDelegate(typeof(UnityAction), obj) as UnityAction;
		}
		public static UnityAction<T> GetUnityAction<T>(this UnityEngine.Object obj, string funcName) {
			var method = UnityEventBase.GetValidMethodInfo(obj, funcName, new Type[] { typeof(T) });
			return method.CreateDelegate(typeof(UnityAction<T>), obj) as UnityAction<T>;
		}
		public static bool ContainsPersistentListener(this UnityEventBase onValueChanged, Delegate action) {
			var count = onValueChanged.GetPersistentEventCount();
			for (int i = count - 1; i >= 0; i--) {
				if (onValueChanged.GetPersistentTarget(i) == action.Target as UnityEngine.Object || onValueChanged.GetPersistentMethodName(i) == action.Method.Name) {
					return true;
				}
			}
			return false;
		}
		public static void AddPersistentListener(this UnityEventBase onValueChanged, UnityAction action, bool editorAndRuntime = true) {
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				if (!onValueChanged.ContainsPersistentListener(action)) {
					UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(onValueChanged, action);
					if (editorAndRuntime) {
						onValueChanged.SetPersistentListenerState(onValueChanged.GetPersistentEventCount() - 1, UnityEventCallState.EditorAndRuntime);
					}
				}
			}
#endif
		}
		public static void RemovePersistentListener(this UnityEventBase onValueChanged, UnityAction action) {
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				if (onValueChanged.ContainsPersistentListener(action)) {
					UnityEditor.Events.UnityEventTools.RemovePersistentListener(onValueChanged, action);
				}
			}
#endif
		}

		public static void AddPersistentListener<T>(this UnityEvent<T> onValueChanged, UnityAction<T> action, bool editorAndRuntime = true) {
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				if (!onValueChanged.ContainsPersistentListener(action)) {
					onValueChanged.RemovePersistentListener(action);
					UnityEditor.Events.UnityEventTools.AddPersistentListener(onValueChanged, action);
					if (editorAndRuntime) {
						onValueChanged.SetPersistentListenerState(onValueChanged.GetPersistentEventCount() - 1, UnityEventCallState.EditorAndRuntime);
					}
				}
			}
#endif
		}
		public static void RemovePersistentListener<T>(this UnityEvent<T> onValueChanged, UnityAction<T> action) {
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				if (onValueChanged.ContainsPersistentListener(action)) {
					UnityEditor.Events.UnityEventTools.RemovePersistentListener(onValueChanged, action);
				}
			}
#endif
		}
		#endregion
	}
}
