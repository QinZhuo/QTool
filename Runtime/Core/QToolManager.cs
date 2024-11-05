using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace QTool {
	public class QToolManager : QSingletonManager<QToolManager> {
		public static bool Destoryed { get; private set; } = false;
		public event Action OnUpdate = null;
		private void Update() {
			OnUpdate?.Invoke();
		}
		private void OnDestroy() {
			Destoryed = true;
		}
	}
}
