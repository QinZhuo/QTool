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
		public QFlowNode Node { get;internal set; }
		public QFlowGraph Graph => Node?.Graph;
		public abstract IEnumerator Run(Action<Transform> action);
	}
}

