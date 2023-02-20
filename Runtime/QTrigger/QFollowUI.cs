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
		private void Awake()
		{
			if(Canvas==null||Canvas.renderMode!= RenderMode.WorldSpace)
			{
				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.zero;
			}
		}
		private void LateUpdate()
        {
            if (_Target != null)
            {
				if (_Target.gameObject.activeInHierarchy)
				{
					var runtimeOffset = offset;
					if (useBoundsHeight)
					{
						runtimeOffset += bounds.size.y * Vector3.up;
					}
					var position = _Target.position + runtimeOffset;
					if (Canvas != null && Canvas.renderMode == RenderMode.WorldSpace)
					{
						rectTransform.position = position;
					}
					else
					{
						rectTransform.anchoredPosition = Camera.main.WorldToScreenPoint(position);
					}
				}
			}
			else
			{
				Recover();
			}
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
