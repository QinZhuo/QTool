using QTool;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class QKeyInfoTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,IKey<string>
{
	public static event Func<string, string> KeyToInfo;
	public static event Action<QKeyInfoTrigger> OnEnter;
	public static event Action<QKeyInfoTrigger> OnExit;
	public string Key { get; set; }
	private string _Info;
	public string Info
	{
		set => _Info = value; get
		{
			if (_Info.IsNull())
			{
				if (KeyToInfo != null)
				{
					return KeyToInfo(Key);
				}
			}
			return _Info;
		}
	}
	private void Awake()
	{
		Key = gameObject.QName();
	}
	public void Set(string key, string info)
	{
		Key = key;
		Info = info;
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
