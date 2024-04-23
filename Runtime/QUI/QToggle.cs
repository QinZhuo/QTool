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
		public Toggle toggle;
		public Graphic off;
		private void Reset()
		{
			toggle = GetComponent<Toggle>();
		}
#if UNITY_EDITOR
		private void OnValidate()
		{
			if (toggle != null)
			{
				toggle.onValueChanged.AddPersistentListener(PlayEffect, true);
			}
		}
#endif
		public void PlayEffect(bool instant)
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
