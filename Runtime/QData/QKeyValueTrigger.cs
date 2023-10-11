using QTool;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class QKeyValueTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public static Action<QKeyValueTrigger> OnEnter;
	public static Action<QKeyValueTrigger> OnExit;
	public string Key { get; private set; }
	public string Value { get; private set; }
	private void Awake()
	{
		Key = gameObject.QName();
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
