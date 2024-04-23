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
	static class RuntimeUnityEvent
	{
		public static QDictionary<UnityEvent, UnityEvent> RuntimeEvents = new QDictionary<UnityEvent, UnityEvent>(key => {
			var value = new UnityEvent();
			key.AddListener(value.Invoke);
			return value;
		});
	}
	static class RuntimeUnityEvent<T>
	{
		public static QDictionary<UnityEvent<T>, UnityEvent<T>> RuntimeEvents = new QDictionary<UnityEvent<T>, UnityEvent<T>>(key => {
			var value = new UnityEvent<T>();
			key.AddListener(value.Invoke);
			return value;
		});
	}
	public interface IValueEvent<T>
	{
		void SetValue(T value);
	}
	public static class QEventTool
	{
		public static UnityAction GetUnityAction(this Object obj, string funcName)
		{
			return UnityEventBase.GetValidMethodInfo(obj, funcName, new System.Type[0]).CreateDelegate(typeof(UnityAction), obj) as UnityAction;
		}
		public static UnityAction<T> GetUnityAction<T>(this Object obj, string funcName)
		{
			var method = UnityEventBase.GetValidMethodInfo(obj, funcName, new System.Type[] { typeof(T) });
			return method.CreateDelegate(typeof(UnityAction<T>), obj) as UnityAction<T>;
		}


		public static void AddPersistentListener(this UnityEventBase onValueChanged, UnityAction action, bool editorAndRuntime = false)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (!onValueChanged.ContainsPersistentListener(action))
				{
					UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(onValueChanged, action);
					if (editorAndRuntime)
					{
						onValueChanged.SetPersistentListenerState(onValueChanged.GetPersistentEventCount() - 1, UnityEventCallState.EditorAndRuntime);
					}
				}
			}
#endif
		}
		public static void ClearPersistentListener(this UnityEventBase onValueChanged)
		{
			var count = onValueChanged.GetPersistentEventCount();
			for (int i = count - 1; i >= 0; i--)
			{
				if (onValueChanged.GetPersistentTarget(i) == null || onValueChanged.GetPersistentMethodName(i).IsNull())
				{
					UnityEditor.Events.UnityEventTools.RemovePersistentListener(onValueChanged, i);
				}
			}
		}
		public static bool ContainsPersistentListener(this UnityEventBase onValueChanged, System.Delegate action)
		{
			var count = onValueChanged.GetPersistentEventCount();
			for (int i = count - 1; i >= 0; i--)
			{
				if (onValueChanged.GetPersistentTarget(i) == action.Target as Object || onValueChanged.GetPersistentMethodName(i) == action.Method.Name)
				{
					return true;
				}
			}
			return false;
		}
		public static void RemovePersistentListener(this UnityEventBase onValueChanged, UnityAction action)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (onValueChanged.ContainsPersistentListener(action))
				{
					UnityEditor.Events.UnityEventTools.RemovePersistentListener(onValueChanged, action);
				}
			}
#endif
		}
	
		public static UnityEvent GetRuntime(this UnityEvent onValueChanged)
		{
			return RuntimeUnityEvent.RuntimeEvents[onValueChanged];
		}
		public static UnityEvent<T> GetRuntime<T>(this UnityEvent<T> onValueChanged)
		{
			return RuntimeUnityEvent<T>.RuntimeEvents[onValueChanged];
		}
		public static void RemovePersistentListener<T>(this UnityEvent<T> onValueChanged, UnityAction<T> action)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (onValueChanged.ContainsPersistentListener(action))
				{
					UnityEditor.Events.UnityEventTools.RemovePersistentListener(onValueChanged, action);
				}	
			}
#endif
		}
		public static void AddPersistentListener<T>(this UnityEvent<T> onValueChanged, UnityAction<T> action, bool editorAndRuntime = false)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (!onValueChanged.ContainsPersistentListener(action))
				{
					onValueChanged.RemovePersistentListener(action);
					UnityEditor.Events.UnityEventTools.AddPersistentListener(onValueChanged, action);
					if (editorAndRuntime)
					{
						onValueChanged.SetPersistentListenerState(onValueChanged.GetPersistentEventCount() - 1, UnityEventCallState.EditorAndRuntime);
					}
				}
			}

#endif
		}
		private static bool IsEventKey(this string key, out string eventKey)
		{
			if (key.StartsWith("on") || key.StartsWith("On"))
			{
				key = key.Substring(2);
				if (key.EndsWith("Event"))
				{
					key = key.Substring(0, key.Length - 5);
				}
				else if (key.EndsWith("Changed"))
				{
					key = key.Substring(0, key.Length - 7);
				}
				eventKey = key;
				return true;
			}
			eventKey = key;
			return false;
		}
		/// <summary>
		/// 自动注册持久化Unity事件
		/// </summary>
		public static void AutoAddPersistentListener(this GameObject gameObject, Object obj)
		{
			if (obj == null) return;
			var typeInfo = QSerializeType.Get(obj.GetType());
			foreach (var memeber in typeInfo.Members)
			{
				if (memeber.Key.IsEventKey(out var eventKey))
				{
					var view = gameObject.transform.FindAll(eventKey);
					if (view == null)
					{
						view = gameObject.transform;
					}

					if (memeber.Type.Is(typeof(UnityEvent<string>)))
					{
						var valueEvent = view.GetComponent<IValueEvent<string>>();
						if (valueEvent != null && valueEvent is MonoBehaviour mono)
						{
							(memeber.Get(obj) as UnityEvent<string>).AddPersistentListener(mono.GetUnityAction<string>(nameof(valueEvent.SetValue)));
							continue;
						}
						var text = view.GetComponent<Text>();
						if (text != null)
						{
							(memeber.Get(obj) as UnityEvent<string>).AddPersistentListener(text.GetUnityAction<string>("set_text"));
							continue;
						}
#if TMPro
						var tmp_text = view.GetComponent<TMP_Text>();
						if (tmp_text != null)
						{
							(memeber.Get(obj) as UnityEvent<string>).AddPersistentListener(tmp_text.GetUnityAction<string>("set_text"));
							continue;
						}
#endif
					}
					else if (memeber.Type.Is(typeof(UnityEvent<bool>)))
					{
						var valueEvent = view.GetComponent<IValueEvent<bool>>();
						if (valueEvent != null && valueEvent is MonoBehaviour mono)
						{
							(memeber.Get(obj) as UnityEvent<bool>).AddPersistentListener(mono.GetUnityAction<bool>(nameof(valueEvent.SetValue)));
							continue;
						}
						var toggle = view.GetComponent<Toggle>();
						if(toggle != null)
						{
							(memeber.Get(obj) as UnityEvent<bool>).AddPersistentListener(toggle.GetUnityAction<bool>("set_isOn"));
							continue;
						}
					}
					else if (memeber.Type.Is(typeof(UnityEvent<float>)))
					{
						var valueEvent = view.GetComponent<IValueEvent<float>>();
						if (valueEvent != null && valueEvent is MonoBehaviour mono)
						{
							(memeber.Get(obj) as UnityEvent<float>).AddPersistentListener(mono.GetUnityAction<float>(nameof(valueEvent.SetValue)));
							continue;
						}
						var slider = view.GetComponent<Slider>();
						if(slider != null)
						{
							(memeber.Get(obj) as UnityEvent<float>).AddPersistentListener(slider.GetUnityAction<float>("set_value"));
							continue;
						}
						var image = view.GetComponent<Image>();
						if(image != null)
						{
							(memeber.Get(obj) as UnityEvent<float>).AddPersistentListener(image.GetUnityAction<float>("set_fillAmount"));
							continue;
						}
					}
				}
			}
			foreach (var function in typeInfo.Functions)
			{
				if (function.Key.IsEventKey(out var eventKey))
				{
					var view = gameObject.transform.FindAll(eventKey);
					if (view == null)
					{
						view = gameObject.transform;
					}
					switch (function.ParamInfos.Length)
					{
						case 0:
							{
								var button = view.GetComponent<Button>();
								if (button != null)
								{
									button.onClick.AddPersistentListener(obj.GetUnityAction(function.Key));
									continue;
								}
								var dropdown = view.GetComponent<Dropdown>();
								if (dropdown != null)
								{
									dropdown.onValueChanged.AddPersistentListener(obj.GetUnityAction(function.Key));
									continue;
								}
#if TMPro
								var tmp_dropdown = view.GetComponent<TMP_Dropdown>();
								if(tmp_dropdown != null)
								{
									tmp_dropdown.onValueChanged.AddPersistentListener(obj.GetUnityAction(function.Key));
									continue;
								}
#endif
							}
							break;
						case 1:
							{
								var pType = function.ParamInfos[0].ParameterType;
								if (pType == typeof(bool))
								{
									var toggle = view.GetComponent<Toggle>();
									if (toggle != null)
									{
										toggle.onValueChanged.AddPersistentListener(obj.GetUnityAction<bool>(function.Key));
										continue;
									}
								}
								else if (pType == typeof(string))
								{
									var input = view.GetComponent<InputField>();
									if (input != null)
									{
										input.onValueChanged.AddPersistentListener(obj.GetUnityAction<string>(function.Key));
										continue;
									}
#if TMPro
									var tmp_input = view.GetComponent<TMP_InputField>();
									if (tmp_input != null)
									{
										tmp_input.onValueChanged.AddPersistentListener(obj.GetUnityAction<string>(function.Key));
										continue;
									}
#endif
								}
							}
							break;
						default:
							break;
					}
				}
			}
		}

		//public static void RegisterEvent(this GameObject gameObject, object obj, params string[] keys)
		//{
		//	if (obj.IsNull()) return;
		//	var trigger = gameObject?.GetTrigger();
		//	var KeyTriggers= gameObject.GetComponentsInChildren<QKeyInfoTrigger>();
		//	if (trigger != null)
		//	{
		//		var typeInfo = QSerializeHasReadOnlyType.Get(obj.GetType());
		//		foreach (var member in typeInfo.Members)
		//		{
		//			if (keys.Length > 0 ? keys.IndexOf(member.QName) < 0 : member.QNameAttribute == null) return;
		//			if (member.Type == typeof(string) || member.Type.Is(typeof(System.Enum)))
		//			{
		//				//	QDebug.Log(gameObject + " 静态数据初始化 " + obj + " " + member.QName + " : " + obj);
		//				gameObject.InvokeEvent(member.QName, member.Get(obj)?.ToString());
		//			}
		//			else if (member.Type == typeof(int) || member.Type == typeof(float))
		//			{
		//				gameObject.InvokeEvent(member.QName, member.Get(obj).As<float>());
		//			}
		//			else if (member.Type.Is(typeof(QRuntimeValue<float>)) && (trigger.floatEventList.ContainsKey(member.QName)
		//						|| trigger.floatEventList.ContainsKey("当前" + member.QName) || trigger.floatEventList.ContainsKey(member.QName + "比例")))
		//			{
		//				var runtimeValue = member.Get(obj).As<QRuntimeValue<float>>();
		//				runtimeValue.Name = member.QName;
		//				runtimeValue.OnValueChange += gameObject.InvokeEvent;
		//				var keyTrigger = KeyTriggers.Get(runtimeValue.Name);
		//				if (keyTrigger != null)
		//				{
		//					runtimeValue.OnStringChange += keyTrigger.Set;
		//				}
		//				runtimeValue.InvokeOnChange();
		//				//	QDebug.Log(gameObject + " 注册" + member.Type.Name + "数据更改事件 " + obj + " " + member.QName);
		//			}
		//			else if (member.Type.Is(typeof(QRuntimeValue<string>)) && trigger.stringEventList.ContainsKey(member.QName))
		//			{
		//				var runtimeValue = member.Get(obj).As<QRuntimeValue<string>>();
		//				runtimeValue.Name = member.QName;
		//				runtimeValue.OnValueChange += gameObject.InvokeEvent;
		//				runtimeValue.InvokeOnChange();
		//				//	QDebug.Log(gameObject + " 注册" + member.Type.Name + "数据更改事件 " + obj + " " + member.QName);
		//			}
		//			else if (member.Type.Is(typeof(QRuntimeValue<bool>)) && trigger.boolEventList.ContainsKey(member.QName))
		//			{
		//				var runtimeValue = member.Get(obj).As<QRuntimeValue<bool>>();
		//				runtimeValue.Name = member.QName;
		//				runtimeValue.OnValueChange += gameObject.InvokeEvent;
		//				runtimeValue.InvokeOnChange();
		//				//	QDebug.Log(gameObject + " 注册" + member.Type.Name + "数据更改事件 " + obj + " " + member.QName);

		//			}
		//		}
		//		foreach (var func in typeInfo.Functions)
		//		{
		//			var name = func.QName();
		//			if (trigger.actionEventList.ContainsKey(name))
		//			{
		//				trigger.actionEventList.Get(name).eventAction.AddListener(() => func.Invoke(obj));
		//			}
		//		}
		//	}
		//}
		//public static void UnRegisterEvent(this GameObject gameObject, object obj, params string[] keys)
		//{
		//	if (obj.IsNull()) return;
		//	var trigger = gameObject?.GetTrigger();
		//	var KeyTriggers = gameObject.GetComponentsInChildren<QKeyInfoTrigger>();
		//	if (trigger != null)
		//	{
		//		var typeInfo = QSerializeHasReadOnlyType.Get(obj.GetType());
		//		foreach (var member in typeInfo.Members)
		//		{
		//			if (keys.Length > 0 ? keys.IndexOf(member.QName) < 0 : member.QNameAttribute == null) return;
		//			if (member.Type.Is(typeof(QRuntimeValue<float>)) && (trigger.floatEventList.ContainsKey(member.QName)
		//						|| trigger.floatEventList.ContainsKey("当前" + member.QName) || trigger.floatEventList.ContainsKey(member.QName + "比例")))
		//			{
		//				var runtimeValue = member.Get(obj).As<QRuntimeValue<float>>();
		//				runtimeValue.OnValueChange -= gameObject.InvokeEvent;
		//				var keyTrigger = KeyTriggers.Get(runtimeValue.Name);
		//				if (keyTrigger != null)
		//				{
		//					runtimeValue.OnStringChange -= keyTrigger.Set;
		//				}
		//			}
		//			else if (member.Type.Is(typeof(QRuntimeValue<string>)) && trigger.stringEventList.ContainsKey(member.QName))
		//			{
		//				var runtimeValue = member.Get(obj).As<QRuntimeValue<string>>();
		//				runtimeValue.OnValueChange -= gameObject.InvokeEvent;
		//			}
		//			else if (member.Type.Is(typeof(QRuntimeValue<bool>)) && trigger.boolEventList.ContainsKey(member.QName))
		//			{
		//				var runtimeValue = member.Get(obj).As<QRuntimeValue<bool>>();
		//				runtimeValue.OnValueChange -= gameObject.InvokeEvent;
		//			}
		//		}
		//		foreach (var func in typeInfo.Functions)
		//		{
		//			var name = func.QName();
		//			if (trigger.actionEventList.ContainsKey(name))
		//			{
		//				trigger.actionEventList.Get(name).eventAction.RemoveAllListeners();
		//			}
		//		}
		//	}
		//}
	}
}
