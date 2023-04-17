using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
	public abstract class QTrigger : MonoBehaviour
	{
		public abstract IEnumerator Run(Action<GameObject> action);
	}
}

