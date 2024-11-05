using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace QTool {
	public class QToolManager : QSingletonManager<QToolManager> {
		#region 静态
		public static bool Destoryed { get; private set; } = false;
		#endregion
		public event Action OnUpdate = null;
		protected override void Awake() {
			if (!Application.isPlaying) {
				gameObject.CheckDestory();
				return;
			}
			base.Awake();
			DontDestroyOnLoad(gameObject);
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
