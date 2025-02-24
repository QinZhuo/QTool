using QTool.Inspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace QTool.ECS {
	public class QEntityTrigger : QComponent<QEntityTrigger> {
		private List<QEntityObject> entities = new();
		public IEnumerable<QEntityObject> Others => entities;
		private void OnTriggerEnter(Collider other) {
			if (other.TryGetComponent<QEntityObject>(out var otherEntity)) { 
				entities.Add(otherEntity);
			}
		}
		private void OnTriggerExit(Collider other) {
			if (other.TryGetComponent<QEntityObject>(out var otherEntity)) {
				entities.Remove(otherEntity);
			}
		}
	}
}

