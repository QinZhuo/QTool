using QTool.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using QTool.Inspector;


#if TMPro
using TMPro;
#endif
namespace QTool {
	public class QEventSystem : MonoBehaviour {
		private QDictionary<string, Action> _events = null;
		public QDictionary<string, Action> Events => _events ??= new QDictionary<string, Action>(key => {
			var target = transform.FindAll(key);
			if (target != null) {
				var unityAction = target.gameObject.GetUnityAction();
				if (unityAction != null) {
					return unityAction.Invoke;
				}
			}
			return null;
		});
		private QDictionary<string, Action<string>> _stringEvents = null;
		public QDictionary<string, Action<string>> StringEvents => _stringEvents ??= new QDictionary<string, Action<string>>(key => {
			var target = transform.FindAll(key);
			if (target != null) {
				var unityAction = target.gameObject.GetStringUnityAction();
				if (unityAction != null) {
					return unityAction.Invoke;
				}
			}
			return null;
		});
		private QDictionary<string, Action<bool>> _boolEvents = null;
		public QDictionary<string, Action<bool>> BoolEvents => _boolEvents ??= new QDictionary<string, Action<bool>>(key => {
			var target = transform.FindAll(key);
			if (target != null) {
				var unityAction = target.gameObject.GetBoolUnityAction();
				if (unityAction != null) {
					return unityAction.Invoke;
				}
			}
			return null;
		});
		private QDictionary<string, Action<float>> _floatEvents = null;
		public QDictionary<string, Action<float>> FloatEvents => _floatEvents ??= new QDictionary<string, Action<float>>(key => {
			var target = transform.FindAll(key);
			if (target != null) {
				var unityAction = target.gameObject.GetFloatUnityAction();
				if (unityAction != null) {
					return unityAction.Invoke;
				}
			}
			return null;
		});
		private QDictionary<string, QObjectList> _objectLists = null;
		private QDictionary<string, QObjectList> objectLists => _objectLists ??= new QDictionary<string, QObjectList>(key => {
			return transform.FindAll(key)?.GetComponent<QObjectList>();
		});
		public void InvokeEvent(string key) {
			Events[key]?.Invoke();
		}
		public void InvokeEvent(string key, string value) {
			StringEvents[key]?.Invoke(value);
		}
		public void InvokeEvent(string key, bool value) {
			BoolEvents[key]?.Invoke(value);
		}
		public void InvokeEvent(string key, float value) {
			FloatEvents[key]?.Invoke(value);
		}
		public QObjectList Get(string key) {
			return objectLists[key];
		}
		public QEventSystem Get(string key, string childKey) {
			return Get(key)?.Get(childKey).GetComponent<QEventSystem>(true);
		}
		public QObjectList this[string key] => Get(key);
		public QEventSystem this[string key, string childKey] => Get(key, childKey);
	}


	public static class QEventTool {

		#region 自动绑定事件拓展
		public static void RegisterEvent(this GameObject gameObject, object obj, params string[] keys) {
			if (obj.IsNull())
				return;
			//var KeyTriggers = gameObject.GetComponentsInChildren<QKeyInfoTrigger>();
			if (gameObject != null) {
				var system = gameObject.GetComponent<QEventSystem>(true);
				var typeInfo = QSerializeHasReadOnlyType.Get(obj.GetType());
				foreach (var member in typeInfo.Members) {
					if (keys.Length > 0 ? keys.Contains(member.QName) : member.QNameAttribute == null)
						return;
					if (member.Type == typeof(string) || member.Type.Is(typeof(Enum))) {
						//	QDebug.Log(gameObject + " 静态数据初始化 " + obj + " " + member.QName + " : " + obj);
						gameObject.InvokeEvent(member.QName, member.Get(obj)?.ToString());
					}
					else if (member.Type == typeof(int) || member.Type == typeof(float)) {
						gameObject.InvokeEvent(member.QName, member.Get(obj).As<float>());
					}
					else if (member.Type.Is(typeof(QRuntimeValue<float>))) {
						var runtimeValue = member.Get(obj).As<QRuntimeValue<float>>();
						runtimeValue.OnValueChange += system.FloatEvents[member.QName];
						if (runtimeValue is QRuntimeRangeValue rangeValue) {
							rangeValue.OnCurrentValueChange += system.FloatEvents[member.QName + "_" + nameof(rangeValue.CurrentValue)];
							rangeValue.OnMinValueChange += system.FloatEvents[member.QName + "_" + nameof(rangeValue.MinValue)];
						}
						//var keyTrigger = KeyTriggers.Get(runtimeValue.Name);
						//if (keyTrigger != null) {
						//	runtimeValue.OnStringChange += keyTrigger.Set;
						//}
						runtimeValue.InvokeOnChange();
					}
					else if (member.Type.Is(typeof(QRuntimeValue<string>))) {
						var runtimeValue = member.Get(obj).As<QRuntimeValue<string>>();
						runtimeValue.OnValueChange += system.StringEvents[member.QName];
						runtimeValue.InvokeOnChange();
					}
					else if (member.Type.Is(typeof(QRuntimeValue<bool>)) ) {
						var runtimeValue = member.Get(obj).As<QRuntimeValue<bool>>();
						runtimeValue.OnValueChange += system.BoolEvents[member.QName];
						runtimeValue.InvokeOnChange();
					}
				}
			}
		}
		public static void UnRegisterEvent(this GameObject gameObject, object obj, params string[] keys) {
			if (obj.IsNull())
				return;
			if (gameObject != null) {
				var system = gameObject.GetComponent<QEventSystem>(true);
				var typeInfo = QSerializeHasReadOnlyType.Get(obj.GetType());
				foreach (var member in typeInfo.Members) {
					if (member.Type.Is(typeof(QRuntimeValue<float>))) {
						var runtimeValue = member.Get(obj).As<QRuntimeValue<float>>();
						runtimeValue.OnValueChange -= system.FloatEvents[member.QName];
						if (runtimeValue is QRuntimeRangeValue rangeValue) {
							rangeValue.OnCurrentValueChange -= system.FloatEvents[member.QName + "_" + nameof(rangeValue.CurrentValue)];
							rangeValue.OnMinValueChange -= system.FloatEvents[member.QName + "_" + nameof(rangeValue.MinValue)];
						}
					}
					else if (member.Type.Is(typeof(QRuntimeValue<string>))) {
						var runtimeValue = member.Get(obj).As<QRuntimeValue<string>>();
						runtimeValue.OnValueChange -= system.StringEvents[member.QName];
					}
					else if (member.Type.Is(typeof(QRuntimeValue<bool>))) {
						var runtimeValue = member.Get(obj).As<QRuntimeValue<bool>>();
						runtimeValue.OnValueChange -= system.BoolEvents[member.QName];
					}
				}
			}
		}

		/// <summary>
		/// 编辑器下 绑定Unity对象 注册持久化事件
		/// </summary>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void PersistentBind(this Component comp) {
			if (comp == null || Application.IsPlaying(comp))
				return;
			var gameObject = comp.gameObject;
			var typeInfo = QInspectorType.Get(comp.GetType());
			foreach (var memeber in typeInfo.Members) {
				if (memeber.Type.Is(typeof(Component))) {
					var view = gameObject.transform.FindAll(memeber.QName);
					if (view == null) {
						continue;
					}
					if (memeber.Get(comp) == null) {
						memeber.Set(comp,view.gameObject.GetComponent(memeber.Type));
					}
				}
				else if (memeber.Type.Is(typeof(UnityEventBase))) {
					var view = gameObject.transform.FindAll(memeber.QName);
					if (view == null) {
						continue;
					}
					if (memeber.Type.Is(typeof(UnityEvent<string>))) {
						var unityEvent = gameObject.GetStringUnityAction();
						if (unityEvent != null) {
							(memeber.Get(comp) as UnityEvent<string>).AddPersistentListener(unityEvent);
						}
					}
					else if (memeber.Type.Is(typeof(UnityEvent<bool>))) {
						var unityEvent = gameObject.GetBoolUnityAction();
						if (unityEvent != null) {
							(memeber.Get(comp) as UnityEvent<bool>).AddPersistentListener(unityEvent);
						}
					}
					else if (memeber.Type.Is(typeof(UnityEvent<float>))) {
						var unityEvent = gameObject.GetFloatUnityAction();
						if (unityEvent != null) {
							(memeber.Get(comp) as UnityEvent<float>).AddPersistentListener(unityEvent);
						}
					}
					else {
						var unityEvent = gameObject.GetUnityAction();
						(memeber.Get(comp) as UnityEventBase).AddPersistentListener(unityEvent);
					}
				}
			}
			foreach (var function in typeInfo.Functions) {
				if (function.MethodInfo.GetAttribute<QNameAttribute>() != null) {
					var view = gameObject.transform.FindAll(function.QName);
					if (view == null) {
						continue;
					}
					switch (function.ParamInfos.Length) {
						case 0: {
							var unityEvent = view.gameObject.GetUnityEventBase();
							if (unityEvent != null) {
								unityEvent.AddPersistentListener(comp.GetUnityAction(function.Key));
								continue;
							}
						}
						break;
						case 1: {
							var pType = function.ParamInfos[0].ParameterType;
							if (pType == typeof(bool)) {
								var unityEvent = view.gameObject.GetBoolUnityEvent();
								if (unityEvent != null) {
									unityEvent.AddPersistentListener(comp.GetUnityAction<bool>(function.Key));
									continue;
								}
							}
							else if (pType == typeof(string)) {
								var unityEvent = view.gameObject.GetStringUnityEvent();
								if (unityEvent != null) {
									unityEvent.AddPersistentListener(comp.GetUnityAction<string>(function.Key));
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
		#endregion
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
		#region UnityEvent UnityAction 获取拓展
		public static UnityEventBase GetUnityEventBase(this GameObject obj) {
			var button = obj.GetComponent<Button>();
			if (button != null) {
				return button.onClick;
			}
			var dropdown = obj.GetComponent<Dropdown>();
			if (dropdown != null) {
				return dropdown.onValueChanged;
			}
#if TMPro
			var tmp_dropdown = obj.GetComponent<TMP_Dropdown>();
			if (tmp_dropdown != null) {
				return tmp_dropdown.onValueChanged;
			}
#endif
			return null;
		}
		public static UnityEvent<bool> GetBoolUnityEvent(this GameObject obj) {
			var toggle = obj.GetComponent<Toggle>();
			if (toggle != null) {
				return toggle.onValueChanged;
			}
			return null;
		}
		public static UnityEvent<string> GetStringUnityEvent(this GameObject obj) {
			var input = obj.GetComponent<InputField>();
			if (input != null) {
				return input.onValueChanged;
			}
#if TMPro
			var tmp_input = obj.GetComponent<TMP_InputField>();
			if (tmp_input != null) {
				return tmp_input.onValueChanged;
			}
#endif
			return null;
		}
		public static UnityAction GetUnityAction(this GameObject obj) {
			var qEvent = obj.GetComponent<IQEvent>();
			if (qEvent != null && qEvent is MonoBehaviour mono) {
				return mono.GetUnityAction(nameof(qEvent.InvokeEvent));
			}
			return null;
		}
		public static UnityAction<T> GetUnityAction<T>(this GameObject obj) {
			var qEvent = obj.GetComponent<IQEvent<T>>();
			if (qEvent != null && qEvent is MonoBehaviour mono) {
				return mono.GetUnityAction<T>(nameof(qEvent.InvokeEvent));
			}
			return null;
		}
		public static UnityAction<string> GetStringUnityAction(this GameObject obj) {
			var valueEvent = obj.GetComponent<IQEvent<string>>();
			if (valueEvent != null && valueEvent is MonoBehaviour mono) {
				return mono.GetUnityAction<string>(nameof(valueEvent.InvokeEvent));
			}
			var text = obj.GetComponent<Text>();
			if (text != null) {
				return text.GetUnityAction<string>("set_text");
			}
#if TMPro
			var tmp_text = obj.GetComponent<TMP_Text>();
			if (tmp_text != null) {
				return tmp_text.GetUnityAction<string>("set_text");
			}
#endif
			return null;
		}
		public static UnityAction<bool> GetBoolUnityAction(this GameObject obj) {
			var valueEvent = obj.GetComponent<IQEvent<bool>>();
			if (valueEvent != null && valueEvent is MonoBehaviour mono) {
				return mono.GetUnityAction<bool>(nameof(valueEvent.InvokeEvent));
			}
			var toggle = obj.GetComponent<Toggle>();
			if (toggle != null) {
				return toggle.GetUnityAction<bool>("set_isOn");
			}
			return null;
		}
		public static UnityAction<float> GetFloatUnityAction(this GameObject obj) {
			var valueEvent = obj.GetComponent<IQEvent<float>>();
			if (valueEvent != null && valueEvent is MonoBehaviour mono) {
				return mono.GetUnityAction<float>(nameof(valueEvent.InvokeEvent));
			}
			var slider = obj.GetComponent<Slider>();
			if (slider != null) {
				return slider.GetUnityAction<float>("set_value");
			}
			var image = obj.GetComponent<Image>();
			if (image != null) {
				return obj.GetUnityAction<float>("set_fillAmount");
			}
			return null;
		}
		#endregion
		#region QEventSystem 拓展
		public static void InvokeEvent(this GameObject gameObject, string key) {
			gameObject.GetComponent<QEventSystem>(true).InvokeEvent(key);
		}
		public static void InvokeEvent(this GameObject gameObject, string key, string value) {
			gameObject.GetComponent<QEventSystem>(true).InvokeEvent(key, value);
		}
		public static void InvokeEvent(this GameObject gameObject, string key, bool value) {
			gameObject.GetComponent<QEventSystem>(true).InvokeEvent(key, value);
		}
		public static void InvokeEvent(this GameObject gameObject, string key, float value) {
			gameObject.GetComponent<QEventSystem>(true).InvokeEvent(key, value);
		}
		public static QObjectList Get(this GameObject gameObject, string key) {
			return gameObject.GetComponent<QEventSystem>(true).Get(key);
		}
		public static QEventSystem Get(this GameObject gameObject, string key, string childKey) {
			return gameObject.GetComponent<QEventSystem>(true).Get(key, childKey);
		}
		#endregion
	}
	public interface IQEvent {
		void InvokeEvent();
	}
	public interface IQEvent<T> {
		void InvokeEvent(T value);
	}
}
