using QTool;
using System.Collections;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;

public class QKeyValueTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public static Action<string, QKeyValueTrigger> OnEnter;
	public static Action<string, QKeyValueTrigger> OnExit;
	public string Key { get;private set; }
	public string Value { get; private set; }
	private void Awake()
	{
		Key = gameObject.QName();
	}
	public void OnPointerEnter(PointerEventData eventData)
	{
		OnEnter?.Invoke(Key, this);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		OnExit?.Invoke(Key, this);
	}
	private void OnDisable()
	{
		OnExit?.Invoke(Key, this);
	}
}
