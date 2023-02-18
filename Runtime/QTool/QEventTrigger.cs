using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace QTool
{
	
    public static class QEventManager
    {
        ///// <summary>
        ///// 当任意事件触发时调用
        ///// </summary>
        //public static event System.Action<string> OnEventTigger;
        /// <summary>
        /// 事件列表 对应事件触发时调用对应Action 使用方法： EventList["事件名"]+=Action;
        /// </summary>
        internal static QDictionary<string, System.Action> EventList = new QDictionary<string, System.Action>();
		internal static QDictionary<string, System.Action> OnceEventList = new QDictionary<string, System.Action>();
		internal static QDictionary<string, System.Action<string>> KeyEventList = new QDictionary<string ,System.Action<string>>();
		/// <summary>
		/// 触发事件
		/// </summary>
		/// <param name="eventKey">事件名</param>
		public static void Trigger(string eventKey)
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
				if (OnceEventList.ContainsKey(eventKey))
				{
					OnceEventList[eventKey]?.Invoke();
					OnceEventList[eventKey] = null;
				}
				if (KeyEventList.ContainsKey(eventKey))
				{
					KeyEventList[eventKey]?.Invoke(eventKey);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError("触发事件[" + eventKey + "]出错：\n" + e);
			}
        }
        public static void Trigger<T>(string eventKey,T value)
        {
            QEventManager<T>.Trigger(eventKey, value);
        }
		public static void RegisterKeyEvent(string eventKey, System.Action<string> action)
		{
			KeyEventList[eventKey] += action;
		}
		public static void UnRegisterKeyEvent(string eventKey, System.Action<string> action)
		{
			KeyEventList[eventKey] -= action;
		}
		public static void Register(string eventKey, System.Action action)
        {
			EventList[eventKey] += action;
		}
		public static void RegisterOnce(string eventKey, System.Action action)
		{
			OnceEventList[eventKey] += action;
		}
		public static void UnRegister(string eventKey, System.Action action)
        {
            EventList[eventKey] -= action;
		//	OnceEventList[eventKey] -= action;
		}
        public static void Register<T>(string eventKey,System.Action<T> action)
        {
			QEventManager<T>.EventList[eventKey] += action;
		}
		public static void RegisterOnce<T>(string eventKey, System.Action<T> action)
		{
			QEventManager<T> .OnceEventList[eventKey] += action;
		}
		public static void UnRegister<T>(string eventKey, System.Action<T> action)
        {
            QEventManager<T>.EventList[eventKey] -= action;
        }
    }
    public class QEventManager<T>
    {
        ///// <summary>
        ///// 当任意事件触发时调用
        ///// </summary>
        //public static event System.Action<string, T> OnEventTigger;
        /// <summary>
        /// 事件列表 对应事件触发时调用对应Action 使用方法： EventList["事件名"]+=Action;
        /// </summary>
        internal static QDictionary<string, System.Action<T>> EventList = new QDictionary<string, System.Action<T>>();

		internal static QDictionary<string, System.Action<T>> OnceEventList = new QDictionary<string, System.Action<T>>();
		public static void Trigger(string eventKey,T value)
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
				if (OnceEventList.ContainsKey(eventKey))
				{
					OnceEventList[eventKey]?.Invoke(value);
					OnceEventList[eventKey] = null;
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError("触发事件[" + eventKey + ":" + value + "]出错\n" + e);
			}
			
		}
    }
    [System.Serializable]
    public class QEventTrigger : MonoBehaviour
    {
		[QName("注册全局事件")]
		public bool GlobalEvent = false;
		public List<ActionEventTrigger> actionEventList = new List<ActionEventTrigger>();
        public List<StringEventTrigger> stringEventList = new List<StringEventTrigger>();
        public List<BoolEventTrigger> boolEventList = new List<BoolEventTrigger>();
        public List<FloatEventTrigger> floatEventList = new List<FloatEventTrigger>();
		public List<ObjectEventTrigger> objectEventList = new List<ObjectEventTrigger>();
		public void Awake()
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
				foreach (var eventTrigger in objectEventList)
				{
					QEventManager.Register<Object>(eventTrigger.Key, eventTrigger.eventAction.Invoke);
				}
			}
		}
		public void OnDestroy()
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
				foreach (var eventTrigger in objectEventList)
				{
					QEventManager.UnRegister<Object>(eventTrigger.Key, eventTrigger.eventAction.Invoke);
				}
			}
		}
		public void Invoke(string eventName, string value)
		{
#if UNITY_EDITOR
			Log(eventName, value);
#endif
			stringEventList.Get(eventName)?.eventAction?.Invoke(value);
        }
        public void Invoke(string eventName)
        {
#if UNITY_EDITOR
			Log(eventName);
#endif
			actionEventList.Get(eventName)?.eventAction?.Invoke();
        }
        public void Invoke(string eventName, bool value)
		{
#if UNITY_EDITOR
			Log(eventName, value);
#endif
			boolEventList.Get(eventName)?.eventAction?.Invoke((bool)value);
        }
        public new void Invoke(string eventName, float value)
		{
#if UNITY_EDITOR
			Log(eventName, value);
#endif
			floatEventList.Get(eventName)?.eventAction?.Invoke(value);
		}
		public void Invoke(string eventName, Object value)
		{
#if UNITY_EDITOR
			Log(eventName, value);
#endif
			objectEventList.Get(eventName)?.eventAction?.Invoke(value);
		}
#if UNITY_EDITOR
		void Log(string eventName, object value=null)
		{

			if (UnityEditor.Selection.activeGameObject == gameObject)
			{
				if (value == null)
				{
					QDebug.Log("["+nameof(QEventTrigger)+"]"+eventName);
				}
				else
				{
					QDebug.Log("[" + nameof(QEventTrigger) + "]" + eventName + "_" + value);
				}
				
			}
		}
#endif
	}
    public class EventTriggerBase<T> : IKey<string> where T : UnityEventBase
	{
		public string EventName;
        public string Key { get => EventName; set => EventName = value; }
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
	public class ObjectEvent : UnityEvent<Object>
	{
	}
	[System.Serializable]
	public class GameObjectEvent : UnityEvent<GameObject>
	{
	}
	[System.Serializable]
    public class FloatEventTrigger : EventTriggerBase<FloatEvent>
    {
    }
    [System.Serializable]
    public class BoolEventTrigger : EventTriggerBase<BoolEvent>
    {
    }
    [System.Serializable]
    public class ActionEventTrigger : EventTriggerBase<UnityEvent>
    {
    }
    [System.Serializable]
    public class StringEventTrigger : EventTriggerBase<StringEvent>
    {
    }
	[System.Serializable]
	public class ObjectEventTrigger : EventTriggerBase<ObjectEvent>
	{
	}

	public static class ValueEventTriggerExtends
    {

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
	}
}
