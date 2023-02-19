using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QInteractSystem : MonoBehaviour
	{
		public List<QTriggerObject> InteractList { get; private set; } = new List<QTriggerObject>();
		private void Start()
		{
			if (QInteractUIList.Instance != null&& QInteractUIList.Instance.Target==null)
			{
				QInteractUIList.Instance.Target = this;
			}
		}
		private void OnTriggerEnter(Collider other)
		{
			var trigger = other.GetComponent<QTriggerObject>();
			if (trigger != null)
			{
				if (trigger.IsInteract)
				{
					InteractList.AddCheckExist(trigger);
					OnInteractAdd?.Invoke(trigger);
					FreshInteract();
				}
				else
				{
					trigger.OnEvent.Invoke(gameObject);
				}
			}
		}
		private void OnTriggerExit(Collider other)
		{
			var trigger = other.GetComponent<QTriggerObject>();
			if (trigger != null)
			{
				if (trigger.IsInteract)
				{
					InteractList.Remove(trigger);
					OnInteractRemove?.Invoke(trigger);
					FreshInteract();
				}
			}
		}
		public System.Action<QTriggerObject> OnInteractFresh;
		public System.Action<QTriggerObject> OnInteractAdd;
		public System.Action<QTriggerObject> OnInteractRemove;
		public QTriggerObject FreshInteract()
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
		public void Interact(QTriggerObject interactObject)
		{
			if (interactObject == null) return;
			interactObject.OnEvent.Invoke(gameObject);
		}
	}
}
