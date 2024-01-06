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
		public void OnPoolGet()
		{

		}

		private void LateUpdate()
		{
			if (View.parent == transform)
			{
				if (transform is RectTransform Rect)
				{
					this.Rect = Rect;
					var parent = transform.parent.GetChild(nameof(QFollowView), true);
					parent.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
					View.transform.SetParent(parent, true);
					View.GetComponent<CanvasGroup>(true).blocksRaycasts = false;
				}
				else
				{
					View.transform.SetParent(null, true);
				}
			}
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
	}
}

