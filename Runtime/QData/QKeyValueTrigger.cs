using QTool;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class QKeyValueTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,IKey<string>
{
	public static Action<QKeyValueTrigger> OnEnter;
	public static Action<QKeyValueTrigger> OnExit;
	public string Key { get; set; }
	public string Value { get; set; }
	private void Awake()
	{
		Key = gameObject.QName();
	}
	public void Set(string key,string value)
	{
		Key = key;
		Value = value;
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
