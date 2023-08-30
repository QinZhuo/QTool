using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QObjectView : MonoBehaviour
	{
		[SerializeField]
		private Transform m_view = null;
		public Transform View=>m_view??= transform.GetChild(nameof(View), true);
		[QName("脱离跟随")]
		public bool followView = false;
		[QName("跟随速度", nameof(followView))]
		public float followSpeed = 10;
		private void Reset()
		{
			m_view = transform.GetChild(nameof(View),true);
		}

		private void Start()
		{
			if (followView)
			{
				View.transform.SetParent(null, true);
			}
		}
		private void OnDestroy()
		{
			if (View != null)
			{
				Destroy(View.gameObject);
			}
		}
		void Update()
		{
			if (followView)
			{
				View.transform.position = Vector3.Lerp(View.transform.position, transform.position, Time.deltaTime * followSpeed);
			}
		}
	}
}

