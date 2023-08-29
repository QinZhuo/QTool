using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.FlowGraph;
namespace QTool
{
	public abstract class QTrigger : MonoBehaviour
	{
		public Transform Start { get; set; }
		public Transform Target { get; set; }
		public QFlowNode Node { get; set; }
		public abstract IEnumerator Run(Action<Transform> action);
	}
}

