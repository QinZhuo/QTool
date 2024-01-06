using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QFollowView : MonoBehaviour, IQPoolObject
	{
		[SerializeField]
		private Transform m_view = null;
		public Transform View => m_view ??= transform.GetChild(nameof(View), true);
		private RectTransform Rect = null;
		[QName("跟随速度")]
		public float followSpeed = 10;
		private void Reset()
		{
			m_view = transform.GetChild(nameof(View), true);
		}
		private void Start()
		{
			FreshViewParent();
		}
		public void OnPoolGet()
		{

		}
		private void FreshViewParent()
		{
			if (!QToolSetting.Instance.qFollowViewSwitch) return;
			if (transform is RectTransform Rect)
			{
				if (transform.parent == null) return;
				if (View.parent == transform || View.parent.parent != transform.parent)
				{
					this.Rect = Rect;
					var parent = transform.parent.GetChild(nameof(QFollowView), true);
					parent.GetComponent<UnityEngine.UI.LayoutElement>(true).ignoreLayout = true;
					View.transform.SetParent(parent, true);
					View.GetComponent<CanvasGroup>(true).blocksRaycasts = false;
				}
			}
			else
			{
				if (View.transform.parent != null)
				{
					View.transform.SetParent(null, true);
				}
			}
		}
		private void LateUpdate()
		{
			if (!QToolSetting.Instance.qFollowViewSwitch) return;
			if (followSpeed > 0)
			{
				if (Rect != null)
				{
					View.transform.position = Vector3.Lerp(View.transform.position, Rect.Center(), Time.deltaTime * followSpeed);
				}
				else
				{
					View.transform.position = Vector3.Lerp(View.transform.position, transform.position, Time.deltaTime * followSpeed);
				}
			}
		}
		public void OnPoolRelease()
		{
			if (View != null)
			{
				View.transform.SetParent(transform, true);
			}
		}
		private void OnDestroy()
		{
			if (View != null)
			{
				Destroy(View.gameObject);
			}
		}
		private void OnTransformParentChanged()
		{
			FreshViewParent();
		}
	}
}

