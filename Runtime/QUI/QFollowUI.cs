using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[ExecuteInEditMode]
    public class QFollowUI : MonoBehaviour
	{

		public RectTransform rectTransform => transform as RectTransform;
		[SerializeField]
        private Transform _Target;
		public Transform Target
		{
			get => _Target;
			set
			{
				_Target = value;
				FreshPosition();
			}
		}
		public RenderMode Mode = RenderMode.ScreenSpaceOverlay;
		public Vector3 Offset;
		private void LateUpdate()
        {
			FreshPosition();
		}
		public void FreshPosition()
		{
			if (_Target != null)
			{
				if (_Target.gameObject.activeInHierarchy)
				{
					if (Target is RectTransform targetRect)
					{
						rectTransform.SetLeftUpPosition(targetRect.RightUp()+(Vector2)Offset*transform.lossyScale);
					}
					else
					{
						var position = _Target.position + Offset;
						switch (Mode)
						{
							case RenderMode.ScreenSpaceOverlay:
								rectTransform.position = Camera.main.WorldToScreenPoint(position);
								break;
							case RenderMode.ScreenSpaceCamera:
								var point = Camera.main.WorldToViewportPoint(position);
								QDebug.LogError("未补全逻辑");
								//	rectTransform.position = Canvas.transform.position + new Vector3(Screen.width * (point.x - 0.5f) * rectTransform.lossyScale.x, Screen.height * (point.y - 0.5f) * rectTransform.lossyScale.y, 0);
								break;
							case RenderMode.WorldSpace:
								rectTransform.position = position;
								break;
							default:
								break;
						}
					}
					
				}
			}
		}
	}

}
