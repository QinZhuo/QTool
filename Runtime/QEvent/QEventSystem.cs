using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace QTool
{
	public class QEventSystem : MonoBehaviour
	{
		private QDictionary<string, UnityEvent> _events = null;
		private QDictionary<string, QObjectList> _objectLists = null;
		private QDictionary<string, UnityEvent<string>> _stringEvents = null;
		private QDictionary<string, UnityEvent<bool>> _boolEvents = null;
		private QDictionary<string, UnityEvent<float>> _floatEvents = null;
		private void Awake()
		{
			_events = new QDictionary<string, UnityEvent>(key =>
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
			_stringEvents = new QDictionary<string, UnityEvent<string>>(key =>
			{
				var target = transform.FindAll(key);
				if (target != null)
				{
					var unityEvent = new UnityEvent<string>();
					var unityAction = gameObject.GetStringUnityAction();
					if (unityAction != null)
					{
						unityEvent.AddListener(unityAction);
					}
					return unityEvent;
				}
				return null;
			});
			_boolEvents = new QDictionary<string, UnityEvent<bool>>(key =>
			{
				var target = transform.FindAll(key);
				if (target != null)
				{
					var unityEvent = new UnityEvent<bool>();
					var unityAction = gameObject.GetBoolUnityAction();
					if (unityAction != null)
					{
						unityEvent.AddListener(unityAction);
					}
					return unityEvent;
				}
				return null;
			});
			_floatEvents = new QDictionary<string, UnityEvent<float>>(key =>
			{
				var target = transform.FindAll(key);
				if (target != null)
				{
					var unityEvent = new UnityEvent<float>();
					var unityAction = gameObject.GetFloatUnityAction();
					if (unityAction != null)
					{
						unityEvent.AddListener(unityAction);
					}
					return unityEvent;
				}
				return null;
			});
			_objectLists = new QDictionary<string, QObjectList>(key =>
			{
				return transform.FindAll(key)?.GetComponent<QObjectList>();
			});
		}
		public void Set(string key, UnityAction action)
		{
			_events[key]?.AddListener(action);
		}
		public void Set(string key, string value)
		{
			_stringEvents[key]?.Invoke(value);
		}
		public void Set(string key, bool value)
		{
			_boolEvents[key]?.Invoke(value);
		}
		public void Set(string key, float value)
		{
			_floatEvents[key]?.Invoke(value);
		}
		public QObjectList Get(string key)
		{
			return _objectLists[key];
		}
		public QEventSystem Get(string key, string childKey)
		{
			return _objectLists[key]?.Get(childKey)?.GetComponent<QEventSystem>();
		}
	}
}
