using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QPositionPoint : MonoBehaviour
	{
		private void OnValidate()
		{
			name = gameObject.QName();
		}
		public bool CanCreate => transform.childCount == 0;
		private void OnDrawGizmos()
		{
			Gizmos.color = name.ToColor();
			Gizmos.DrawSphere(transform.position, 0.2f);
		}
	}
	
}

