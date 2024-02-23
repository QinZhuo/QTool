using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace QTool.UI
{
	[RequireComponent(typeof(Toggle))]
	public class QToggle : MonoBehaviour
	{
		[QReadonly]
		public Toggle toggle;
		public Graphic off;
		public UnityEvent<bool> onValueChanged;
		private void Reset()
		{
			toggle = GetComponent<Toggle>();
			if (toggle != null)
			{
				toggle.onValueChanged.AddPersistentListener(InvokeValueChange, true);
			}
		}
		private void InvokeValueChange(bool isOn)
		{
			onValueChanged?.Invoke(isOn);
			PlayEffect(isOn);
		}
		private void PlayEffect(bool instant)
		{
			if (off == null)
				return;

#if UNITY_EDITOR
			if (!Application.isPlaying)
				off.canvasRenderer.SetAlpha(!toggle.isOn ? 1f : 0f);
			else
#endif
				off.CrossFadeAlpha(!toggle.isOn ? 1f : 0f, instant ? 0f : 0.1f, true);
		}
	}
}
