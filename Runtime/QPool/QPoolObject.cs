using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QPoolObject : MonoBehaviour, IQPoolObject
	{
		[QName("对象池"), QReadonly, SerializeField]
		internal string poolName = "";
		[QName("延迟自动回收")]
		public float delayRelease = -1;
		public ActionEvent OnRelease = new ActionEvent();
		private bool isReleased = false;
		public void OnPoolGet()
		{
			isReleased = false;
			if (delayRelease > 0)
			{
				StartCoroutine(DelayRelease());
			}
		}
		private IEnumerator DelayRelease()
		{
			yield return new WaitForSecondsRealtime(delayRelease);
			if (!isReleased)
			{
				gameObject.PoolRelease();
			}
		}
		public void OnPoolRelease()
		{
			isReleased = true;
			OnRelease.Invoke();
		}
	}
}
