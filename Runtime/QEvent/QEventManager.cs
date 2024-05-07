using QTool.Reflection;
using System.Collections.Generic;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace QTool
{

	public enum QEventKey
	{
		设置更新,
		卸载场景,
		游戏退出,
	}
	public static class QEventManager
	{
		/// <summary>
		/// 事件列表 对应事件触发时调用对应Action 使用方法： EventList["事件名"]+=Action;
		/// </summary>
		private static QDictionary<string, System.Action> Events = new QDictionary<string, System.Action>();
		private static Queue<string> EventQueue = new Queue<string>();
		internal static System.Action OnUpdate = InvokeQueueEvent;
		public static void InvokeEvent(System.Enum value)
		{
			InvokeEvent(value.ToString());
		}
		/// <summary>
		/// 触发事件
		/// </summary>
		/// <param name="eventKey">事件名</param>
		public static void InvokeEvent(string eventKey)
		{
			QDebug.Log("触发事件[" + eventKey + "]");
			try
			{
				eventKey = eventKey.Trim();
				if (string.IsNullOrWhiteSpace(eventKey))
				{
					return;
				}
				if (Events.ContainsKey(eventKey))
				{
					Events[eventKey]?.Invoke();
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError("触发事件[" + eventKey + "]出错：\n" + e);
			}
		}
		public static void QueueEvent(string eventKey)
		{
			EventQueue.Enqueue(eventKey);
		}
		public static void InvokeEvent<T>(string eventKey, T value)
		{
			QEventManager<T>.InvokeEvent(eventKey, value);
		}
		public static void QueueEvent<T>(string eventKey, T value)
		{
			QEventManager<T>.EventQueue.Enqueue((eventKey, value));
		}
		public static void Register(System.Enum eventKey, params System.Action[] action)
		{
			var key = eventKey.ToString();
			foreach (var item in action)
			{
				Register(key, item);
			}
		}
		public static void Register(string eventKey, System.Action action)
		{
			Events[eventKey] += action;
		}

		public static void UnRegister(System.Enum eventKey, params System.Action[] action)
		{
			var key = eventKey.ToString();
			foreach (var item in action)
			{
				UnRegister(key, item);
			}
		}
		public static void UnRegister(string eventKey, System.Action action)
		{
			Events[eventKey] -= action;
		}
		public static void Register<T>(string eventKey, System.Action<T> action)
		{
			QEventManager<T>.EventList[eventKey] += action;
		}
		public static void UnRegister<T>(string eventKey, System.Action<T> action)
		{
			QEventManager<T>.EventList[eventKey] -= action;
		}
		private static void InvokeQueueEvent()
		{
			while (EventQueue.Count > 0)
			{
				InvokeEvent(EventQueue.Dequeue());
			}
		}
		public static void Update()
		{
			OnUpdate();
		}
	}
	public class QEventManager<T>
	{
		/// <summary>
		/// 事件列表 对应事件触发时调用对应Action 使用方法： EventList["事件名"]+=Action;
		/// </summary>
		internal static QDictionary<string, System.Action<T>> EventList = new QDictionary<string, System.Action<T>>();
		internal static Queue<(string, T)> EventQueue = new Queue<(string, T)>();
		static QEventManager()
		{
			QEventManager.OnUpdate += InvokeQueueEvent;
		}
		public static void InvokeEvent(string eventKey, T value)
		{
			try
			{
				eventKey = eventKey.Trim();
				if (string.IsNullOrWhiteSpace(eventKey))
				{
					return;
				}
				if (EventList.ContainsKey(eventKey))
				{
					EventList[eventKey]?.Invoke(value);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError("触发事件[" + eventKey + ":" + value + "]出错\n" + e);
			}
		}
		private static void InvokeQueueEvent()
		{
			while (EventQueue.Count > 0)
			{
				var data = EventQueue.Dequeue();
				InvokeEvent(data.Item1, data.Item2);
			}
		}
	}

}
