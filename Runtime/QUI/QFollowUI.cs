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
				Start();
				FreshPosition();
			}
		}
		public RenderMode Mode = RenderMode.ScreenSpaceOverlay;
		public Vector2 Offset;
		bool IsTargetInWorld = false;
		private void Start()
		{
			if (_Target is RectTransform rect)
			{
				IsTargetInWorld = rect.GetComponentInParent<Canvas>().renderMode == RenderMode.WorldSpace;
			}
			else
			{
				IsTargetInWorld = true;
			}
		}
		private void OnValidate()
		{
			Start();
		}
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
						var rightUp = targetRect.RightUp();
						if (IsTargetInWorld)
						{
							rightUp = Camera.main.WorldToScreenPoint(Target.position + Target.TransformVector((targetRect.rect.size / 2)));
						}
						rectTransform.SetLeftUpPosition(rightUp + Offset * transform.lossyScale);
					}
					else
					{
						var position = _Target.position;
						switch (Mode)
						{
							case RenderMode.ScreenSpaceOverlay:
								rectTransform.SetPosition((Vector2)Camera.main.WorldToScreenPoint(position) + Offset * transform.lossyScale);
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
