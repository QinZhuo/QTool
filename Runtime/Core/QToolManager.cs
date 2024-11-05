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
		protected override void Awake() {
			base.Awake();
			Instance.OnUpdate += QCoroutine.Update;
		}
		private void Update() {
			OnUpdate?.Invoke();
		}
		private void OnDestroy() {
			Destoryed = true;
			Instance.OnUpdate -= QCoroutine.Update;
		}
	}
}
