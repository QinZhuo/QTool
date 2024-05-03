using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool
{
	public class QEventSystem : MonoBehaviour
	{
		private QDictionary<string, Action> actions = null;
		private void Awake()
		{
			actions = new QDictionary<string, Action>(FindAction);
		}
		public Action FindAction(string key)
		{
			var target = transform.FindAll(key);
			if (target != null)
			{
				if(target.TryGetComponent<Button>(out var button)) { 
				}
			}
			return null;
		}
		public void InvokeEvent(string key)
		{

		}
	}
}
