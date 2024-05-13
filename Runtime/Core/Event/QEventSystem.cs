using QTool.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if TMPro
using TMPro;
#endif
namespace QTool
{
	public class QEventSystem : MonoBehaviour
	{
		private QDictionary<string, UnityEvent> _events = null;
		private QDictionary<string, UnityEvent> events => _events ??= new QDictionary<string, UnityEvent>(key =>
			{
				var target = transform.FindAll(key);
				if (target != null)
				{
					if (target.TryGetComponent<Button>(out var button))
					{
						return button.onClick;
					}
				}
				return null;
			});
		private QDictionary<string, UnityEvent<string>> _stringEvents = null;
		private QDictionary<string, UnityEvent<string>> stringEvents => _stringEvents ??= new QDictionary<string, UnityEvent<string>>(key =>
			{
				var target = transform.FindAll(key);
				if (target != null)
				{
					var unityEvent = new UnityEvent<string>();
					var unityAction = target.gameObject.GetStringUnityAction();
					if (unityAction != null)
					{
						unityEvent.AddListener(unityAction);
					}
					return unityEvent;
				}
				return null;
			});
		private QDictionary<string, UnityEvent<bool>> _boolEvents = null;
		private QDictionary<string, UnityEvent<bool>> boolEvents => _boolEvents ??= new QDictionary<string, UnityEvent<bool>>(key =>
			 {
				 var target = transform.FindAll(key);
				 if (target != null)
				 {
					 var unityEvent = new UnityEvent<bool>();
					 var unityAction = target.gameObject.GetBoolUnityAction();
					 if (unityAction != null)
					 {
						 unityEvent.AddListener(unityAction);
					 }
					 return unityEvent;
				 }
				 return null;
			 });
		private QDictionary<string, UnityEvent<float>> _floatEvents = null;
		private QDictionary<string, UnityEvent<float>> floatEvents => _floatEvents ??= new QDictionary<string, UnityEvent<float>>(key =>
		{
			var target = transform.FindAll(key);
			if (target != null)
			{
				var unityEvent = new UnityEvent<float>();
				var unityAction = target.gameObject.GetFloatUnityAction();
				if (unityAction != null)
				{
					unityEvent.AddListener(unityAction);
				}
				return unityEvent;
			}
			return null;
		});
		private QDictionary<string, QObjectList> _objectLists = null;
		private QDictionary<string, QObjectList> objectLists => _objectLists ??= new QDictionary<string, QObjectList>(key =>
		{
			return transform.FindAll(key)?.GetComponent<QObjectList>();
		});
		public void AddListener(UnityAction action)
		{
			events[name]?.AddListener(action);
		}
		public void RemoveListener(UnityAction action)
		{
			events[name]?.RemoveListener(action);
		}
		public void RemoveAllListeners()
		{
			events[name]?.RemoveAllListeners();
		}
		public void InvokeEvent(string value)
		{
			stringEvents[name]?.Invoke(value);
		}
		public void InvokeEvent(bool value)
		{
			boolEvents[name]?.Invoke(value);
		}
		public void InvokeEvent(float value)
		{
			floatEvents[name]?.Invoke(value);
		}
		public void AddListener(string key, UnityAction action)
		{
			events[key]?.AddListener(action);
		}
		public void RemoveListener(string key, UnityAction action)
		{
			events[key]?.RemoveListener(action);
		}
		public void RemoveAllListeners(string key)
		{
			events[key]?.RemoveAllListeners();
		}
		public void InvokeEvent(string key, string value)
		{
			stringEvents[key]?.Invoke(value);
		}
		public void InvokeEvent(string key, bool value)
		{
			boolEvents[key]?.Invoke(value);
		}
		public void InvokeEvent(string key, float value)
		{
			floatEvents[key]?.Invoke(value);
		}
		public QObjectList Get(string key)
		{
			return objectLists[key];
		}
		public QEventSystem Get(string key, string childKey)
		{
			return Get(key)?.Get(childKey).GetComponent<QEventSystem>(true);
		}
		public QObjectList this[string key] => Get(key);
		public QEventSystem this[string key, string childKey] => Get(key, childKey);
	}


	public static class QEventTool
	{
		#region QEventSystem拓展
		public static void AddListener(this GameObject gameObject, UnityAction action)
		{
			gameObject.GetComponent<QEventSystem>(true).AddListener(action);
		}
		public static void RemoveListener(this GameObject gameObject, UnityAction action)
		{
			gameObject.GetComponent<QEventSystem>(true).RemoveListener(action);
		}
		public static void RemoveAllListeners(this GameObject gameObject)
		{
			gameObject.GetComponent<QEventSystem>(true).RemoveAllListeners();
		}
		public static void InvokeEvent(this GameObject gameObject, string value)
		{
			gameObject.GetComponent<QEventSystem>(true).InvokeEvent(value);
		}
		public static void InvokeEvent(this GameObject gameObject, bool value)
		{
			gameObject.GetComponent<QEventSystem>(true).InvokeEvent(value);
		}
		public static void InvokeEvent(this GameObject gameObject, float value)
		{
			gameObject.GetComponent<QEventSystem>(true).InvokeEvent(value);
		}
		public static void AddListener(this GameObject gameObject, string key, UnityAction action)
		{
			gameObject.GetComponent<QEventSystem>(true).AddListener(key, action);
		}
		public static void RemoveListener(this GameObject gameObject, string key, UnityAction action)
		{
			gameObject.GetComponent<QEventSystem>(true).RemoveListener(key, action);
		}
		public static void RemoveAllListeners(this GameObject gameObject, string key, UnityAction action)
		{
			gameObject.GetComponent<QEventSystem>(true).RemoveAllListeners(key);
		}
		public static void InvokeEvent(this GameObject gameObject, string key, string value)
		{
			gameObject.GetComponent<QEventSystem>(true).InvokeEvent(key, value);
		}
		public static void InvokeEvent(this GameObject gameObject, string key, bool value)
		{
			gameObject.GetComponent<QEventSystem>(true).InvokeEvent(key, value);
		}
		public static void InvokeEvent(this GameObject gameObject, string key, float value)
		{
			gameObject.GetComponent<QEventSystem>(true).InvokeEvent(key, value);
		}
		public static QObjectList Get(this GameObject gameObject, string key)
		{
			return gameObject.GetComponent<QEventSystem>(true).Get(key);
		}
		public static QEventSystem Get(this GameObject gameObject, string key, string childKey)
		{
			return gameObject.GetComponent<QEventSystem>(true).Get(key, childKey);
		}
		#endregion
		public static UnityAction GetUnityAction(this Object obj, string funcName)
		{
			return UnityEventBase.GetValidMethodInfo(obj, funcName, new System.Type[0]).CreateDelegate(typeof(UnityAction), obj) as UnityAction;
		}
		public static UnityAction<T> GetUnityAction<T>(this Object obj, string funcName)
		{
			var method = UnityEventBase.GetValidMethodInfo(obj, funcName, new System.Type[] { typeof(T) });
			return method.CreateDelegate(typeof(UnityAction<T>), obj) as UnityAction<T>;
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
		public static void RemoveNullPersistentListeners(this UnityEventBase onValueChanged)
		{
			var count = onValueChanged.GetPersistentEventCount();
#if UNITY_EDITOR
			for (int i = count - 1; i >= 0; i--)
			{
				if (onValueChanged.GetPersistentTarget(i) == null || onValueChanged.GetPersistentMethodName(i).IsNull())
				{
					UnityEditor.Events.UnityEventTools.RemovePersistentListener(onValueChanged, i);
				}
			}
#endif
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
		public static UnityEventBase GetUnityEventBase(this GameObject obj)
		{
			var button = obj.GetComponent<Button>();
			if (button != null)
			{
				return button.onClick;
			}
			var dropdown = obj.GetComponent<Dropdown>();
			if (dropdown != null)
			{
				return dropdown.onValueChanged;
			}
#if TMPro
			var tmp_dropdown = obj.GetComponent<TMP_Dropdown>();
			if (tmp_dropdown != null)
			{
				return tmp_dropdown.onValueChanged;
			}
#endif
			return null;
		}
		public static UnityEvent<bool> GetBoolUnityEvent(this GameObject obj)
		{
			var toggle = obj.GetComponent<Toggle>();
			if (toggle != null)
			{
				return toggle.onValueChanged;
			}
			return null;
		}
		public static UnityEvent<string> GetStringUnityEvent(this GameObject obj)
		{
			var input = obj.GetComponent<InputField>();
			if (input != null)
			{
				return input.onValueChanged;
			}
#if TMPro
			var tmp_input = obj.GetComponent<TMP_InputField>();
			if (tmp_input != null)
			{
				return tmp_input.onValueChanged;
			}
#endif
			return null;
		}
		public static UnityAction<string> GetStringUnityAction(this GameObject obj)
		{
			var valueEvent = obj.GetComponent<ISetValue<string>>();
			if (valueEvent != null && valueEvent is MonoBehaviour mono)
			{
				return mono.GetUnityAction<string>(nameof(valueEvent.SetValue));
			}
			var text = obj.GetComponent<Text>();
			if (text != null)
			{
				return text.GetUnityAction<string>("set_text");
			}
#if TMPro
			var tmp_text = obj.GetComponent<TMP_Text>();
			if (tmp_text != null)
			{
				return tmp_text.GetUnityAction<string>("set_text");
			}
#endif
			return null;
		}
		public static UnityAction<bool> GetBoolUnityAction(this GameObject obj)
		{
			var valueEvent = obj.GetComponent<ISetValue<bool>>();
			if (valueEvent != null && valueEvent is MonoBehaviour mono)
			{
				return mono.GetUnityAction<bool>(nameof(valueEvent.SetValue));
			}
			var toggle = obj.GetComponent<Toggle>();
			if (toggle != null)
			{
				return toggle.GetUnityAction<bool>("set_isOn");
			}
			return null;
		}
		public static UnityAction<float> GetFloatUnityAction(this GameObject obj)
		{
			var valueEvent = obj.GetComponent<ISetValue<float>>();
			if (valueEvent != null && valueEvent is MonoBehaviour mono)
			{
				return mono.GetUnityAction<float>(nameof(valueEvent.SetValue));
			}
			var slider = obj.GetComponent<Slider>();
			if (slider != null)
			{
				return slider.GetUnityAction<float>("set_value");
			}
			var image = obj.GetComponent<Image>();
			if (image != null)
			{
				return obj.GetUnityAction<float>("set_fillAmount");
			}
			return null;
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
						continue;
					}
					if (memeber.Type.Is(typeof(UnityEvent<string>)))
					{
						var unityEvent = gameObject.GetStringUnityAction();
						if (unityEvent != null)
						{
							(memeber.Get(obj) as UnityEvent<string>).AddPersistentListener(unityEvent);
						}
					}
					else if (memeber.Type.Is(typeof(UnityEvent<bool>)))
					{
						var unityEvent = gameObject.GetBoolUnityAction();
						if (unityEvent != null)
						{
							(memeber.Get(obj) as UnityEvent<bool>).AddPersistentListener(unityEvent);
						}
					}
					else if (memeber.Type.Is(typeof(UnityEvent<float>)))
					{
						var unityEvent = gameObject.GetFloatUnityAction();
						if (unityEvent != null)
						{
							(memeber.Get(obj) as UnityEvent<float>).AddPersistentListener(unityEvent);
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
						continue;
					}
					switch (function.ParamInfos.Length)
					{
						case 0:
							{
								var unityEvent = view.gameObject.GetUnityEventBase();
								if (unityEvent != null)
								{
									unityEvent.AddPersistentListener(obj.GetUnityAction(function.Key));
									continue;
								}
							}
							break;
						case 1:
							{
								var pType = function.ParamInfos[0].ParameterType;
								if (pType == typeof(bool))
								{
									var unityEvent = view.gameObject.GetBoolUnityEvent();
									if (unityEvent != null)
									{
										unityEvent.AddPersistentListener(obj.GetUnityAction<bool>(function.Key));
										continue;
									}
								}
								else if (pType == typeof(string))
								{
									var unityEvent = view.gameObject.GetStringUnityEvent();
									if (unityEvent != null)
									{
										unityEvent.AddPersistentListener(obj.GetUnityAction<string>(function.Key));
										continue;
									}
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
	public interface IValueEvent<T>
	{
		UnityEvent<T> OnValueEvent { get; }
	}
	public interface ISetValue<T>
	{
		void SetValue(T value);
	}
}
