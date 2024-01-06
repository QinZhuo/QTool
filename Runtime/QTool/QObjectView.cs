using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QObjectView : MonoBehaviour
	{
		[SerializeField]
		private Transform m_view = null;
		public Transform View => m_view ??= transform.GetChild(nameof(View), true);
		private RectTransform Rect = null;
		[QName("脱离跟随")]
		public bool followView = false;
		[QName("跟随速度", nameof(followView))]
		public float followSpeed = 10;
		private void Reset()
		{
			m_view = transform.GetChild(nameof(View), true);
		}
		private void Start()
		{
			if (followView)
			{
				if (transform is RectTransform Rect)
				{
					this.Rect = Rect;
					View.transform.SetParent(GetComponentInParent<Canvas>().transform.GetChild(nameof(QObjectView), true), true);
				}
				else
				{
					View.transform.SetParent(null, true);
				}
			}
		}
		private void OnDestroy()
		{
			if (View != null)
			{
				Destroy(View.gameObject);
			}
		}
		private void LateUpdate()
		{
			if (followView && followSpeed > 0)
			{
				if (Rect != null)
				{
					transform.position = Vector3.Lerp(transform.position, Rect.Center(), Time.deltaTime * followSpeed);
				}
				else
				{
					View.transform.position = Vector3.Lerp(View.transform.position, transform.position, Time.deltaTime * followSpeed);
				}
			}
		}
	}
}

