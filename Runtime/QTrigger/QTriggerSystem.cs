using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.FlowGraph;
namespace QTool
{
	public abstract class QTriggerSystem<T> : MonoBehaviour
	{
		protected virtual void OnTriggerEnter2D(Collider2D other)
		{
			var trigger = other.GetComponent<T>();
			if (trigger != null)
			{
				TriggerEnter(trigger);
			}
		}
		protected virtual void OnTriggerExit2D(Collider2D other)
		{
			var trigger = other.GetComponent<T>();
			if (trigger != null)
			{
				TriggerExit(trigger);
			}
		}
		protected virtual void OnTriggerEnter(Collider other)
		{
			var trigger = other.GetComponent<T>();
			if (trigger != null)
			{
				TriggerEnter(trigger);
			}
		}
		protected virtual void OnTriggerExit(Collider other)
		{
			var trigger = other.GetComponent<T>();
			if (trigger != null)
			{
				TriggerExit(trigger);
			}
		}
		protected abstract void TriggerEnter(T trigger);
		protected abstract void TriggerExit(T trigger);
	}
	public abstract class QTrigger : MonoBehaviour
	{
		public virtual Transform Start { get; set; }
		public virtual QTeamRelaction Relaction { get; set; }
		public QFlowNode Node { get; set; }
		public QFlowGraph Graph => Node?.Graph;
		public abstract IEnumerator Init();
		public abstract IEnumerator Run();
		public virtual IEnumerator InvokeTigger(Transform triggerObject)
		{
			Node[nameof(triggerObject)] = triggerObject;
			yield return Node.RunPortIEnumerator(nameof(triggerObject));
		}
		public abstract void Cancel();
	}
}

