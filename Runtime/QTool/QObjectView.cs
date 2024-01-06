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
		private RectTransform RectView = null;
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
				if (View is RectTransform RectView)
				{
					this.RectView = RectView;
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
			if (followView)
			{
				if (RectView != null)
				{
					View.transform.position = Vector3.Lerp(RectView.Center(), transform.position, Time.deltaTime * followSpeed);
				}
				else
				{
					View.transform.position = Vector3.Lerp(View.transform.position, transform.position, Time.deltaTime * followSpeed);
				}
			}
		}
	}
}

