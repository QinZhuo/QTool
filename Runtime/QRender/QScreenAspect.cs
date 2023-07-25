using System;
using System.Reflection;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{
	[DisallowMultipleComponent]
    public class QScreenAspect : MonoBehaviour
    {
		private void Reset()
		{
			transform.localScale = Vector3.one;
		}
		private void Awake()
		{
			if (LastAspect == 0)
			{
				FreshRect();
				QToolManager.Instance.OnUpdateEvent += FreshRect;
			}
			FreshAspect();
			OnAspectChange += FreshAspect;
		}
		private void OnDestroy()
		{
			OnAspectChange -= FreshAspect;
		}

		public async void FreshAspect()
		{
			var camera = GetComponent<Camera>();
			if (camera != null)
			{
				camera.aspect = QScreen.TargetAspect;
				camera.rect = QScreen.AspectRect;
				Debug.LogError(camera.rect);
				camera.ResetProjectionMatrix();
			}
			var rectTransform = GetComponent<RectTransform>();
			if (rectTransform != null)
			{
				await 1;
				rectTransform.offsetMin = Vector2.zero;
				rectTransform.offsetMax = Vector2.zero;
				var offset = rectTransform.Size() * (Vector2.one - QScreen.AspectRect.size)/2;
				rectTransform.offsetMin = offset ;
				rectTransform.offsetMax = -offset ;
			}
		}
		public static event Action OnAspectChange;
		private static float LastAspect = 0;

		private static void FreshRect()
		{
			if (LastAspect != QScreen.Aspect)
			{
				LastAspect = QScreen.Aspect;
				if (QScreen.Aspect > QScreen.TargetAspect)
				{
					var offset = (1 - QScreen.TargetAspect / QScreen.Aspect) / 2;
					QScreen.AspectRect = new Rect(offset, 0.0f, 1.0f - offset * 2, 1.0f);
				}
				else
				{
					var offset = (1 - QScreen.Aspect / QScreen.TargetAspect) / 2;
					QScreen.AspectRect = new Rect(0.0f, offset, 1.0f, 1.0f - offset * 2);
				}
				OnAspectChange?.Invoke();
			}
		}
	}
}
