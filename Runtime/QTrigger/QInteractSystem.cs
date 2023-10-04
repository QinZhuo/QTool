using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QInteractSystem : QTriggerSystem<QInteractTrigger>
	{
		public List<QInteractTrigger> InteractList { get; private set; } = new List<QInteractTrigger>();
		private void Start()
		{
			if (QInteractUIList.Instance != null&& QInteractUIList.Instance.Target==null)
			{
				QInteractUIList.Instance.Target = this;
			}
		}
		protected override void TriggerEnter(QInteractTrigger trigger)
		{
			if (trigger.IsManual)
			{
				InteractList.AddCheckExist(trigger);
				OnInteractAdd?.Invoke(trigger);
				FreshInteract();
			}
			else
			{
				trigger.OnTrigger.Invoke(gameObject);
			}
		}
		protected override void TriggerExit(QInteractTrigger trigger)
		{
			if (trigger.IsManual)
			{
				InteractList.Remove(trigger);
				OnInteractRemove?.Invoke(trigger);
				FreshInteract();
			}
		}
		public System.Action<QInteractTrigger> OnInteractFresh;
		public System.Action<QInteractTrigger> OnInteractAdd;
		public System.Action<QInteractTrigger> OnInteractRemove;
		public QInteractTrigger FreshInteract()
		{
			if (InteractList.Count > 0)
			{
				InteractList.Sort((a, b) => Mathf.FloorToInt((a.transform.position - transform.position).sqrMagnitude - (b.transform.position - transform.position).sqrMagnitude));
			}
			var current = InteractList.Get(0);
			OnInteractFresh?.Invoke(current);
			return current;
		}
		public void Interact()
		{
			if (InteractList.Count > 0)
			{
				Interact(InteractList.Get(0));
			}
		}
		public void Interact(QInteractTrigger interactObject)
		{
			if (interactObject == null) return;
			interactObject.OnTrigger.Invoke(gameObject);
		}
	}


}
