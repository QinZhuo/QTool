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
			if (transform is RectTransform Rect)
			{
				if (transform.parent?.parent == null) return;
				if (View.parent == transform)
				{
					this.Rect = Rect;
					var parent = transform.parent.GetChild(nameof(QFollowView), true);
					parent.SetAsFirstSibling();
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
				View.transform.localScale = Vector3.Lerp(View.transform.localScale, transform.localScale, Time.deltaTime * followSpeed);
				View.transform.rotation = Quaternion.Lerp(View.transform.rotation, transform.rotation, Time.deltaTime * followSpeed * 36);
			}
		}
		public void OnPoolRelease()
		{
			if (View != null)
			{
				View.transform.SetParent(transform, true);
				View.transform.localPosition = Vector3.zero;
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

