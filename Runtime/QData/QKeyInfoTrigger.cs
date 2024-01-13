using QTool;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class QKeyInfoTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IKey<string>
{
	public static event Action<QKeyInfoTrigger> OnEnter;
	public static event Action<QKeyInfoTrigger> OnExit;
	public string Key { get => name; set => name = value; }
	public string Info { get; set; }
	public void Set(string key, string info)
	{
		if (this == null) return;
		Key = key;
		Info = info;
	}
	public bool IsIn { get; private set; } = false;
	public bool IsTriggered { get; private set; } = false;
	private Coroutine coroutine = null;
	public void OnPointerEnter(PointerEventData eventData)
	{
		IsIn = true;
		if (coroutine != null)
		{
			StopCoroutine(coroutine);
		}
		coroutine = StartCoroutine(CheckTrigger());
	}
	private WaitForSeconds WaitCheck = new WaitForSeconds(0.2f);
	private IEnumerator CheckTrigger()
	{
		yield return WaitCheck;
		if (IsIn)
		{
			IsTriggered = true;
			OnEnter?.Invoke(this);
		}
	}
	public void OnPointerExit(PointerEventData eventData)
	{
		IsIn = false;
		if (IsTriggered)
		{
			IsTriggered = false;
			OnExit?.Invoke(this);
		}
	}
	private void OnDisable()
	{
		OnPointerExit(null);
	}
}
