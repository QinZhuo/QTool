using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[ExecuteInEditMode]
    public class QFollowUI : MonoBehaviour
    {
		[SerializeField]
        private Transform _Target;
		public Transform Target
		{
			get => _Target;
			set
			{
				_Target = value;
				if (_Target!=null&& useBoundsHeight)
				{
					bounds = _Target.GetBounds();
				}
				FreshPosition();
			}
		}
		public QFollowList QFollowList { get; internal set; }
		public RectTransform rectTransform => transform as RectTransform;
		private Canvas _Canvas;
		public Canvas Canvas => _Canvas??=GetComponentInParent<Canvas>();
		[QName("使用包围盒高度")]
		public bool useBoundsHeight=false;
		Bounds bounds;
		public Vector3 offset=Vector3.zero;
		private Vector3 _lastPosition = default;
		public void FreshPosition()
		{
			if (_Target != null)
			{
				if (_Target.gameObject.activeInHierarchy)
				{
					if (_lastPosition != _Target.position)
					{
						_lastPosition = _Target.position;
						var runtimeOffset = offset;
						if (useBoundsHeight)
						{
							runtimeOffset += bounds.size.y * Vector3.up;
						}
						var position = _Target.position + runtimeOffset;
						if (Canvas != null)
						{
							switch (Canvas.renderMode)
							{
								case RenderMode.ScreenSpaceOverlay:
									rectTransform.position = Camera.main.WorldToScreenPoint(position);
									break;
								case RenderMode.ScreenSpaceCamera:
									var point = Camera.main.WorldToViewportPoint(position);
									rectTransform.position = Canvas.transform.position + new Vector3(Screen.width * (point.x - 0.5f) * rectTransform.lossyScale.x, Screen.height * (point.y - 0.5f) * rectTransform.lossyScale.y, 0);
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
			else
			{
				Recover();
			}
		}
		private void LateUpdate()
        {
			FreshPosition();
		}
		public void Recover()
		{
			if (QFollowList != null)
			{
				QFollowList.Push(gameObject);
				QFollowList = null;
			}
		}
    }

}
