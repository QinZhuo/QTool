using QTool;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class QKeyInfoTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,IKey<string>
{
	public static QDictionary<string, string> KeyInfos = new QDictionary<string, string>();
	public static Action<QKeyInfoTrigger> OnEnter;
	public static Action<QKeyInfoTrigger> OnExit;
	public string Key { get; set; }
	public string Info { get; set; }
	private void Awake()
	{
		Key = gameObject.QName();
	}
	public void Set(string key, string info)
	{
		Key = key;
		Info = info.ForeachBlockValue('{', '}', key => KeyInfos.ContainsKey(key) ? KeyInfos[key] : key);
	}
	public void OnPointerEnter(PointerEventData eventData)
	{
		OnEnter?.Invoke(this);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		OnExit?.Invoke(this);
	}
	private void OnDisable()
	{
		OnExit?.Invoke(this);
	}
}
