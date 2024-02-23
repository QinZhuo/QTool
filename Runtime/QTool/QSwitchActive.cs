using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool
{
	public class QSwitchActive : MonoBehaviour
	{
		public GameObject on;
		public GameObject off;
		private void Reset()
		{
			var toggle = GetComponent<Toggle>();
			if (toggle != null)
			{
				toggle.onValueChanged.AddPersistentListener(Switch);
			}
		}
		public void Switch(bool value)
		{
			on.gameObject.SetActive(value);
			off.gameObject.SetActive(!value);
		}
	}
}
