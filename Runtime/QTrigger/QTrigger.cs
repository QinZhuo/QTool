using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
	public abstract class QTrigger : MonoBehaviour
	{
		public Transform Start { get; set; }
		public Transform Target { get; set; }
		public abstract IEnumerator Run(Action<Transform> action);
	}
}

