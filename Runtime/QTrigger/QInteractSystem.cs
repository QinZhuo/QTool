using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QInteractSystem : MonoBehaviour
	{
		public List<QInteractObject> objectList = new List<QInteractObject>();
		private void OnTriggerEnter(Collider other)
		{
			var interactObject = other.GetComponent<QInteractObject>();
			if (interactObject != null)
			{
				objectList.AddCheckExist(interactObject);
				FreshCurrent();
			}
		}
		private void OnTriggerExit(Collider other)
		{
			var interactObject = other.GetComponent<QInteractObject>();
			if (interactObject != null)
			{
				objectList.Remove(interactObject);
				FreshCurrent();
			}
		}
		public System.Action OnInteractFresh;
		public QInteractObject FreshCurrent()
		{
			if (objectList.Count > 0)
			{
				objectList.Sort((a, b) => Mathf.FloorToInt((a.transform.position - transform.position).sqrMagnitude - (b.transform.position - transform.position).sqrMagnitude));
			}
			OnInteractFresh?.Invoke();
			return objectList.Get(0);
		}
		public void Interact()
		{
			if (objectList.Count > 0)
			{
				Interact(objectList.Get(0));
			}
		}
		public void Interact(QInteractObject interactObject)
		{
			if (interactObject == null) return;
			interactObject.OnInteract.Invoke(gameObject);
		}
	}
}
