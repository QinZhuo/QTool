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
    public class QCameraAspect : MonoBehaviour
    {
		[QName("渲染比例")]
		public float aspect = 16f / 9f;
		[QName("黑色填充")]
		public bool blackFill = true;
		private void Awake()
		{
			if (LastAspect == 0)
			{
				LastAspect = QScreen.Aspect;
				QToolManager.Instance.OnUpdateEvent += OnUpdate;
			}
			FreshAspect();
			OnAspectChange += FreshAspect;
		}
		private void OnDestroy()
		{
			OnAspectChange -= FreshAspect;
		}
		public void FreshAspect()
		{
			Camera.main.aspect = aspect;
			if (blackFill)
			{
				if (QScreen.Aspect > aspect)
				{
					var offset = (1 - aspect / QScreen.Aspect) / 2;
					Camera.main.rect = new Rect(offset, 0.0f, 1.0f - offset * 2, 1.0f);
					Camera.main.ResetProjectionMatrix();
				}
				else
				{
					var offset = (1 - QScreen.Aspect / aspect) / 2;
					Camera.main.rect = new Rect(0.0f, offset, 1.0f, 1.0f - offset * 2);
					Camera.main.ResetProjectionMatrix();
				}
			}
		}
		public static event Action OnAspectChange;
		static float LastAspect = 0;
		private static void OnUpdate()
		{
			if (LastAspect != QScreen.Aspect)
			{
				LastAspect = QScreen.Aspect;
				OnAspectChange?.Invoke();
			}
		}
	}
}
