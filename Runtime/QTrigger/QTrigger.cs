using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.FlowGraph;
namespace QTool
{
	public abstract class QTrigger : MonoBehaviour
	{
		public virtual Transform Start { get; set; }
		public virtual Transform Target { get; set; }
		public QFlowNode Node { get; set; }
		public QFlowGraph Graph => Node?.Graph;
		public abstract IEnumerator Run(Action<Transform> action);
	}
}

