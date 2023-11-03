using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QPoolObject : MonoBehaviour, IQPoolObject
	{
		[QName("对象池"), QReadonly, SerializeField]
		internal string poolName = "";
		public ActionEvent OnRelease = new ActionEvent();
		public void OnDestroy()
		{
			OnRelease.Invoke();
		}
	}
}
