using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
	public abstract class QTrigger : MonoBehaviour
	{
		public GameObject Start { get; set; }
		public GameObject Target { get; set; }
		public abstract IEnumerator Run(Action<GameObject> action);
	}
}

