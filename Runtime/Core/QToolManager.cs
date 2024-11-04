using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace QTool {
	public class QToolManager : QInstanceManager<QToolManager> {
		#region 静态
		public static bool Destoryed { get; private set; } = false;
		#endregion
		public event Action OnUpdate = null;
		public int FrameIndex { get; private set; } = 0;
#if UNITY_2021_1_OR_NEWER
		private VisualElement _rootVisualElement = null;
		public VisualElement RootVisualElement {
			get {
				if (_rootVisualElement == null) {
					var uiDoc = gameObject.GetComponent<UIDocument>(true);
					uiDoc.panelSettings = Resources.Load<PanelSettings>(nameof(PanelSettings));
					_rootVisualElement = uiDoc.rootVisualElement;
				}
				return _rootVisualElement;
			}
		}
#endif
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
