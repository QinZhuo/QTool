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
	public string Info
	{
		get
		{
			if (KeyInfos.ContainsKey(Key))
			{
				var info = "";
				var infoKey = KeyInfos[Key];
				if (KeyInfos.ContainsKey(infoKey))
				{
					info += infoKey.ToLocationString(infoKey.ToColor()) + "\n" + KeyInfos[infoKey] + "\n\n";
				}
				info.ForeachBlockValue('{', '}', key =>
				{
					if (KeyInfos.ContainsKey(key))
					{
						info += key.ToLocationString(key.ToColor()) + "\n" + KeyInfos[key] + "\n\n";
					};
					return key;
				});
				return info;
			}
			else
			{
				return "";
			}
		}
	}
	private void Awake()
	{
		Key = gameObject.QName();
	}
	public void Set(string key, float value = 0)
	{
		Key = key;
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
