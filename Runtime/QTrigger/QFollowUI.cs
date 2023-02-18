using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[ExecuteInEditMode]
    public class QFollowUI : MonoBehaviour
    {
		[SerializeField]
        private Transform target;
		public Transform Target
		{
			get => target;
			set
			{
				target = value;
				if (target!=null&& useBoundsHeight)
				{
					bounds = target.GetBounds();
				}
			}
		}
		public RectTransform rectTransform => transform as RectTransform;
		private Canvas _Canvas;
		public Canvas Canvas => _Canvas??=GetComponentInParent<Canvas>();
		[QName("使用包围盒高度")]
		public bool useBoundsHeight=false;
		Bounds bounds;
		public Vector3 offset=Vector3.zero;
		private void Awake()
		{
			if(Canvas!=null&&Canvas.renderMode!= RenderMode.WorldSpace)
			{
				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.zero;
			}
		}
		private void LateUpdate()
        {
            if (target != null&&target.gameObject.activeInHierarchy)
            {
				var runtimeOffset = offset;
				if (useBoundsHeight)
				{
					runtimeOffset += bounds.size.y*Vector3.up;
				}
				var position = target.position + runtimeOffset;
				if(Canvas!=null&&Canvas.renderMode == RenderMode.WorldSpace)
				{
					rectTransform.position = position;
				}
				else
				{
					rectTransform.anchoredPosition= Camera.main.WorldToScreenPoint(position);
				}
            }
        }
    }

}
