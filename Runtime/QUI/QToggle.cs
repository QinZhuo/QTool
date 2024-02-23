using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool.UI
{
	[RequireComponent(typeof(Toggle))]
	public class QToggle : MonoBehaviour
	{
		[QReadonly]
		public Toggle toggle;
		public Graphic on;
		private void Reset()
		{
			toggle = GetComponent<Toggle>();
			if (toggle != null)
			{
				toggle.onValueChanged.AddPersistentListener(PlayEffect, true);
			}
		}
		private void PlayEffect(bool instant)
		{
			if (on == null)
				return;

#if UNITY_EDITOR
			if (!Application.isPlaying)
				on.canvasRenderer.SetAlpha(toggle.isOn ? 1f : 0f);
			else
#endif
				on.CrossFadeAlpha(toggle.isOn ? 1f : 0f, instant ? 0f : 0.1f, true);
		}
	}
}
