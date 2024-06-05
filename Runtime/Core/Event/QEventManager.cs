using System;
using System.Collections.Concurrent;
using UnityEngine;
namespace QTool {

	public enum QEventKey {
		设置更新,
		卸载场景,
		游戏退出,
	}
	public static class QEventManager {
		/// <summary>
		/// 事件列表 对应事件触发时调用对应Action 使用方法： EventList["事件名"]+=Action;
		/// </summary>
		private static QDictionary<string, Action> Events = new QDictionary<string, Action>();
		private static ConcurrentQueue<string> EventQueue = new ConcurrentQueue<string>();
		internal static Action OnUpdate = InvokeQueueEvent;
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
			QEventManager<T>.InvokeEvent(eventKey, value);
		}
		public static void QueueEvent<T>(string eventKey, T value) {
			QEventManager<T>.EventQueue.Enqueue((eventKey, value));
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
			QEventManager<T>.EventList[eventKey] += action;
		}
		public static void UnRegister<T>(string eventKey, Action<T> action) {
			QEventManager<T>.EventList[eventKey] -= action;
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
	}
	internal class QEventManager<T> {
		/// <summary>
		/// 事件列表 对应事件触发时调用对应Action 使用方法： EventList["事件名"]+=Action;
		/// </summary>
		internal static QDictionary<string, Action<T>> EventList = new QDictionary<string, Action<T>>();
		internal static ConcurrentQueue<(string, T)> EventQueue = new ConcurrentQueue<(string, T)>();
		static QEventManager() {
			QEventManager.OnUpdate += InvokeQueueEvent;
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
	public class QEventQueue {
		private Action eventQueue;
		public void Register(Action action) {
			eventQueue += action;
		}
		public void Invoke() {
			eventQueue?.Invoke();
			eventQueue = null;
		}
	}
}
