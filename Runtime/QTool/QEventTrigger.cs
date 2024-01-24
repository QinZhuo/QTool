using QTool.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
        internal static QDictionary<string, System.Action> EventList = new QDictionary<string, System.Action>();
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
			QDebug.Log("触发事件[" + eventKey+"]");
			try
			{
				eventKey = eventKey.Trim();
				if (string.IsNullOrWhiteSpace(eventKey))
				{
					return;
				}
				if (EventList.ContainsKey(eventKey))
				{
					EventList[eventKey]?.Invoke();
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError("触发事件[" + eventKey + "]出错：\n" + e);
			}
        }
        public static void InvokeEvent<T>(string eventKey,T value)
        {
            QEventManager<T>.InvokeEvent(eventKey, value);
        }
		public static void Register(System.Enum eventKey,params System.Action[] action)
		{
			var key = eventKey.ToString();
			foreach (var item in action)
			{
				Register(key, item);
			}
		}
		public static void Register(string eventKey, System.Action action)
        {
			EventList[eventKey] += action;
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
            EventList[eventKey] -= action;
		}
        public static void Register<T>(string eventKey,System.Action<T> action)
        {
			QEventManager<T>.EventList[eventKey] += action;
		}
		public static void UnRegister<T>(string eventKey, System.Action<T> action)
        {
            QEventManager<T>.EventList[eventKey] -= action;
        }
    }
    public class QEventManager<T>
    {
        /// <summary>
        /// 事件列表 对应事件触发时调用对应Action 使用方法： EventList["事件名"]+=Action;
        /// </summary>
        internal static QDictionary<string, System.Action<T>> EventList = new QDictionary<string, System.Action<T>>();
		public static void InvokeEvent(string eventKey,T value)
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
    }
	public interface IQEvent<T>
	{
		void Set(T value);
	}
    [System.Serializable,DisallowMultipleComponent]
    public class QEventTrigger : MonoBehaviour
    {
		[QName("注册全局事件")]
		public bool GlobalEvent = false;
		public List<ActionEventTrigger> actionEventList = new List<ActionEventTrigger>();
        public List<StringEventTrigger> stringEventList = new List<StringEventTrigger>();
        public List<BoolEventTrigger> boolEventList = new List<BoolEventTrigger>();
        public List<FloatEventTrigger> floatEventList = new List<FloatEventTrigger>();
		[QName("自动绑定")]
		public void AutoRegisterEvent()
		{
			foreach (var kv in boolEventList)
			{
				var target = transform.FindAll(kv.Key);
				if (target == null) continue;
				foreach (var item in target.GetComponents<IQEvent<bool>>())
				{
					kv.eventAction.AddPersistentListener(item.Set);
				}
			}
			foreach (var kv in stringEventList)
			{
				var target = transform.FindAll(kv.Key);
				if (target == null) continue;
				foreach (var item in target.GetComponents<IQEvent<string>>())
				{
					kv.eventAction.AddPersistentListener(item.Set);
				}
				foreach (var item in target.GetComponents<UnityEngine.UI.Text>())
				{
					kv.eventAction.AddPersistentListener(item.GetAction<string>("set_text"));
				}
			}
			foreach (var kv in floatEventList)
			{
				var key = kv.Key;
				var target = transform.FindAll(key);
				if (target == null && key.StartsWith("当前"))
				{
					key = key.Substring("当前".Length);
				}
				target = transform.FindAll(key);
				if (target == null) continue;
				foreach (var item in target.GetComponents<IQEvent<float>>())
				{
					kv.eventAction.AddPersistentListener(item.Set);
				}
				foreach (var item in target.GetComponents<UnityEngine.UI.Image>())
				{
					if (item.type == UnityEngine.UI.Image.Type.Filled)
					{
						kv.eventAction.AddPersistentListener(item.GetAction<float>("set_fillAmount"));
					}
				}

			}
			gameObject.SetDirty();
		}
		protected void Awake()
		{
			if (GlobalEvent)
			{
				foreach (var eventTrigger in actionEventList)
				{
					QEventManager.Register(eventTrigger.Key, eventTrigger.eventAction.Invoke);
				}
				foreach (var eventTrigger in stringEventList)
				{
					QEventManager.Register<string>(eventTrigger.Key, eventTrigger.eventAction.Invoke);
				}
				foreach (var eventTrigger in boolEventList)
				{
					QEventManager.Register<bool>(eventTrigger.Key, eventTrigger.eventAction.Invoke);
				}
				foreach (var eventTrigger in floatEventList)
				{
					QEventManager.Register<float>(eventTrigger.Key, eventTrigger.eventAction.Invoke);
				}
			}
		}
		protected void OnDestroy()
		{
			if (GlobalEvent)
			{
				foreach (var eventTrigger in actionEventList)
				{
					QEventManager.UnRegister(eventTrigger.Key, eventTrigger.eventAction.Invoke);
				}
				foreach (var eventTrigger in stringEventList)
				{
					QEventManager.UnRegister<string>(eventTrigger.Key, eventTrigger.eventAction.Invoke);
				}
				foreach (var eventTrigger in boolEventList)
				{
					QEventManager.UnRegister<bool>(eventTrigger.Key, eventTrigger.eventAction.Invoke);
				}
				foreach (var eventTrigger in floatEventList)
				{
					QEventManager.UnRegister<float>(eventTrigger.Key, eventTrigger.eventAction.Invoke);
				}
			}
		}
		public void Invoke(string eventName, string value)
		{
			Log(eventName, value);
			stringEventList.Get(eventName)?.eventAction?.Invoke(value);
        }
        public void Invoke(string eventName)
        {
			Log(eventName);
			actionEventList.Get(eventName)?.eventAction?.Invoke();
        }
        public void Invoke(string eventName, bool value)
		{
			Log(eventName, value);
			boolEventList.Get(eventName)?.eventAction?.Invoke((bool)value);
			boolEventList.Get("!"+eventName)?.eventAction?.Invoke(!(bool)value);
		}
        public new void Invoke(string eventName, float value)
		{
			Log(eventName, value);
			floatEventList.Get(eventName)?.eventAction?.Invoke(value);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		void Log(string eventName, object value=null)
		{
#if UNITY_EDITOR
			if (UnityEditor.Selection.activeGameObject == gameObject)
			{
				if (value == null)
				{
					QDebug.Log("["+nameof(QEventTrigger)+"]"+eventName);
				}
				else
				{
					QDebug.Log("[" + nameof(QEventTrigger) + "]" + eventName + "<"+value.GetType()+">(" + value + ")");
				}
			}
#endif
		}
	}
    public class QUnityEvent<T> : IKey<string> where T : UnityEventBase
	{
		public string Key { get => eventName; set => eventName = value; }
		[UnityEngine.Serialization.FormerlySerializedAs("EventName")]
		public string eventName;
        public T eventAction = default;
    }
    [System.Serializable]
    public class ActionEvent : UnityEvent
    {
    }
    [System.Serializable]
    public class BoolEvent : UnityEvent<bool>
    {
    }
    [System.Serializable]
    public class IntEvent : UnityEvent<int>
    {
    }
    [System.Serializable]
    public class FloatEvent : UnityEvent<float>
    {
    }
   
	[System.Serializable]
	public class StringEvent : UnityEvent<string>
	{
	}
	[System.Serializable]
	public class ColorEvent : UnityEvent<Color>
	{
	}
	[System.Serializable]
	public class GameObjectEvent : UnityEvent<GameObject>
	{
	}
	[System.Serializable]
    public class FloatEventTrigger : QUnityEvent<FloatEvent>
    {
    }
    [System.Serializable]
    public class BoolEventTrigger : QUnityEvent<BoolEvent>
    {
    }
    [System.Serializable]
    public class ActionEventTrigger : QUnityEvent<UnityEvent>
    {
    }
    [System.Serializable]
    public class StringEventTrigger : QUnityEvent<StringEvent>
    {
    }
	
	public static class QEventTool
    {
		public static UnityAction GetAction(this Object obj, string funcName)
		{
			return UnityEventBase.GetValidMethodInfo(obj, funcName, new System.Type[0]).CreateDelegate(typeof(UnityAction), obj) as UnityAction;
		}
		public static UnityAction<T> GetAction<T>(this Object obj, string funcName)
		{
			var method = UnityEventBase.GetValidMethodInfo(obj, funcName, new System.Type[] { typeof(T) });
			return method.CreateDelegate(typeof(UnityAction<T>), obj) as UnityAction<T>;
		}
		public static void AddPersistentListener(this UnityEvent onValueChanged, UnityAction action)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				onValueChanged.RemovePersistentListener(action);
				UnityEditor.Events.UnityEventTools.AddPersistentListener(onValueChanged, action);
			}
			else

#endif
			{
				onValueChanged.AddListener(action);
			}
		}
		public static void RemovePersistentListener(this UnityEvent onValueChanged, UnityAction action)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				UnityEditor.Events.UnityEventTools.RemovePersistentListener(onValueChanged, action);
			}
			else

#endif
			{
				onValueChanged.RemoveListener(action);
			}
		}
		public static void RemovePersistentListener<T>(this UnityEvent<T> onValueChanged, UnityAction<T> action)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				UnityEditor.Events.UnityEventTools.RemovePersistentListener(onValueChanged, action);
			}
			else

#endif
			{
				onValueChanged.RemoveListener(action);
			}
		}
		public static void AddPersistentListener<T>(this UnityEvent<T> onValueChanged, UnityAction<T> action)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				onValueChanged.RemovePersistentListener(action);
				UnityEditor.Events.UnityEventTools.AddPersistentListener(onValueChanged, action);
			}
			else

#endif
			{
				onValueChanged.AddListener(action);
			}
		}
		public static QEventTrigger GetTrigger(this GameObject obj)
        {
            if (obj == null)
            {
                return null;
            }
            var tigger = obj.GetComponentInChildren<QEventTrigger>(true);
            return tigger;
        }

        public static QEventTrigger GetParentTrigger(this GameObject obj)
        {
            if (obj.transform.parent == null || obj == null)
            {
                return null;
            }
            var tigger = obj.transform.parent.GetComponentInParent<QEventTrigger>();
            return tigger;
        }
		public static void InvokeEvent(this GameObject obj, string eventName)
        {
            obj.GetTrigger()?.Invoke(eventName.Trim());
        }
        public static void InvokeEvent(this GameObject obj, string eventName, bool value)
        {
            obj.GetTrigger()?.Invoke(eventName.Trim(), value);
        }
        public static void InvokeEvent(this GameObject obj, string eventName, float value)
        {
            obj.GetTrigger()?.Invoke(eventName.Trim(), value);
        }
        public static void InvokeEvent(this GameObject obj, string eventName, string value)
        {
            obj.GetTrigger()?.Invoke(eventName.Trim(), value);
        }
		public static void InvokeEvent(this GameObject obj, string eventName, Object value)
		{
			obj.GetTrigger()?.Invoke(eventName.Trim(), value);
		}
		public static void InvokeParentEvent(this GameObject obj, string eventName)
        {
            obj.GetParentTrigger()?.Invoke(eventName.Trim());
        }
        public static void InvokeParentEvent(this GameObject obj, string eventName, bool value)
        {
            obj.GetParentTrigger()?.Invoke(eventName.Trim(), value);
        }
        public static void InvokeParentEvent(this GameObject obj, string eventName, float value)
        {
            obj.GetParentTrigger()?.Invoke(eventName.Trim(), value);
        }
        public static void InvokeParentEvent(this GameObject obj, string eventName, string value)
        {
            obj.GetParentTrigger()?.Invoke(eventName.Trim(), value);
        }
		public static void InvokeParentEvent(this GameObject obj, string eventName, Object value)
		{
			obj.GetParentTrigger()?.Invoke(eventName.Trim(), value);
		}
		public static void RegisterEvent(this GameObject gameObject, object obj, params string[] keys)
		{
			if (obj.IsNull()) return;
			var trigger = gameObject?.GetTrigger();
			var KeyTriggers= gameObject.GetComponentsInChildren<QKeyInfoTrigger>();
			if (trigger != null)
			{
				var typeInfo = QSerializeHasReadOnlyType.Get(obj.GetType());
				foreach (var member in typeInfo.Members)
				{
					if (member.QNameAttribute == null) continue;
					if (keys.Length > 0 && keys.IndexOf(member.QName) < 0) continue;
					if (member.Type == typeof(string) || member.Type.Is(typeof(System.Enum)))
					{
						//	QDebug.Log(gameObject + " 静态数据初始化 " + obj + " " + member.QName + " : " + obj);
						gameObject.InvokeEvent(member.QName, member.Get(obj)?.ToString());
					}
					else if (member.Type == typeof(int) || member.Type == typeof(float))
					{
						gameObject.InvokeEvent(member.QName, member.Get(obj).As<float>());
					}
					else if (member.Type.Is(typeof(QRuntimeValue<float>)) && (trigger.floatEventList.ContainsKey(member.QName)
								|| trigger.floatEventList.ContainsKey("当前" + member.QName) || trigger.floatEventList.ContainsKey(member.QName + "比例")))
					{
						var runtimeValue = member.Get(obj).As<QRuntimeValue<float>>();
						runtimeValue.Name = member.QName;
						runtimeValue.OnValueChange += gameObject.InvokeEvent;
						var keyTrigger = KeyTriggers.Get(runtimeValue.Name);
						if (keyTrigger != null)
						{
							runtimeValue.OnStringChange += keyTrigger.Set;
						}
						runtimeValue.InvokeOnChange();
						//	QDebug.Log(gameObject + " 注册" + member.Type.Name + "数据更改事件 " + obj + " " + member.QName);
					}
					else if (member.Type.Is(typeof(QRuntimeValue<string>)) && trigger.stringEventList.ContainsKey(member.QName))
					{
						var runtimeValue = member.Get(obj).As<QRuntimeValue<string>>();
						runtimeValue.Name = member.QName;
						runtimeValue.OnValueChange += gameObject.InvokeEvent;
						runtimeValue.InvokeOnChange();
						//	QDebug.Log(gameObject + " 注册" + member.Type.Name + "数据更改事件 " + obj + " " + member.QName);
					}
					else if (member.Type.Is(typeof(QRuntimeValue<bool>)) && trigger.boolEventList.ContainsKey(member.QName))
					{
						var runtimeValue = member.Get(obj).As<QRuntimeValue<bool>>();
						runtimeValue.Name = member.QName;
						runtimeValue.OnValueChange += gameObject.InvokeEvent;
						runtimeValue.InvokeOnChange();
						//	QDebug.Log(gameObject + " 注册" + member.Type.Name + "数据更改事件 " + obj + " " + member.QName);

					}
				}
				foreach (var func in typeInfo.Functions)
				{
					var name = func.QName();
					if (trigger.actionEventList.ContainsKey(name))
					{
						trigger.actionEventList.Get(name).eventAction.AddListener(() => func.Invoke(obj));
					}
				}
			}
		}
		public static void UnRegisterEvent(this GameObject gameObject, object obj, params string[] keys)
		{
			if (obj.IsNull()) return;
			var trigger = gameObject?.GetTrigger();
			var KeyTriggers = gameObject.GetComponentsInChildren<QKeyInfoTrigger>();
			if (trigger != null)
			{
				var typeInfo = QSerializeHasReadOnlyType.Get(obj.GetType());
				foreach (var member in typeInfo.Members)
				{
					if (member.QNameAttribute == null) continue;
					if (keys.Length > 0 && keys.IndexOf(member.QName) < 0) continue;
					if (member.Type.Is(typeof(QRuntimeValue<float>)) && (trigger.floatEventList.ContainsKey(member.QName)
								|| trigger.floatEventList.ContainsKey("当前" + member.QName) || trigger.floatEventList.ContainsKey(member.QName + "比例")))
					{
						var runtimeValue = member.Get(obj).As<QRuntimeValue<float>>();
						runtimeValue.OnValueChange -= gameObject.InvokeEvent;
						var keyTrigger = KeyTriggers.Get(runtimeValue.Name);
						if (keyTrigger != null)
						{
							runtimeValue.OnStringChange -= keyTrigger.Set;
						}
					}
					else if (member.Type.Is(typeof(QRuntimeValue<string>)) && trigger.stringEventList.ContainsKey(member.QName))
					{
						var runtimeValue = member.Get(obj).As<QRuntimeValue<string>>();
						runtimeValue.OnValueChange -= gameObject.InvokeEvent;
					}
					else if (member.Type.Is(typeof(QRuntimeValue<bool>)) && trigger.boolEventList.ContainsKey(member.QName))
					{
						var runtimeValue = member.Get(obj).As<QRuntimeValue<bool>>();
						runtimeValue.OnValueChange -= gameObject.InvokeEvent;
					}
				}
				foreach (var func in typeInfo.Functions)
				{
					var name = func.QName();
					if (trigger.actionEventList.ContainsKey(name))
					{
						trigger.actionEventList.Get(name).eventAction.RemoveAllListeners();
					}
				}
			}
		}
	}
}
