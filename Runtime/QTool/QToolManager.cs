using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace QTool
{
	public class QToolManager : QInstanceManager<QToolManager>
	{
		public static bool Destoryed { get; private set; } = false;
		public event Action OnUpdateEvent = null;
		public event Action OnGUIEvent = null;
		public event Action OnPostRenderEvent = null;
		public int FrameIndex { get; private set; } = 0;
#if UNITY_2021_1_OR_NEWER
		private VisualElement _rootVisualElement = null;
		public VisualElement RootVisualElement
		{
			get
			{
				if (_rootVisualElement == null)
				{
					var uiDoc = gameObject.GetComponent<UIDocument>(true);
					uiDoc.panelSettings = Resources.Load<PanelSettings>(nameof(PanelSettings));
					_rootVisualElement = uiDoc.rootVisualElement;
				}
				return _rootVisualElement;
			}
		}
#endif
		protected override void Awake()
		{
			if (!Application.isPlaying)
			{
				gameObject.CheckDestory();
				return;
			}
			base.Awake();
			DontDestroyOnLoad(gameObject);
			Instance.OnUpdateEvent += QCoroutine.Update;
			Instance.OnUpdateEvent += QEventManager.Update;
		}
		private void Update()
		{
			FrameIndex++;
			OnUpdateEvent?.Invoke();
		}
		private void OnDestroy()
		{
			Destoryed = true;
			QTask.StopAllWait();
			QEventManager.InvokeEvent(QEventKey.游戏退出);
			Instance.OnUpdateEvent -= QCoroutine.Update;
			Instance.OnUpdateEvent -= QEventManager.Update;
		}
		private void OnGUI()
		{
			OnGUIEvent?.Invoke();
		}
		private void OnEnable()
		{
			RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
		}
		private void OnDisable()
		{
			RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
		}
		private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			OnPostRender();
		}
		private void OnPostRender()
		{
			OnPostRenderEvent?.Invoke();
		}
	}

	public abstract class QToolManager<T>:MonoBehaviour where T : QToolManager<T>
    {
        private static T _instance;
        public static T Instance {
            get
            {
                if (_instance == null)
                {
					if (QToolManager.Instance == null) return null;
                    _instance = QToolManager.Instance.transform.GetComponent<T>();
                    if (_instance == null)
                    {
                        _instance = QToolManager.Instance.gameObject.AddComponent<T>();
                        _instance.SetDirty();
                    }
                }
                return _instance;
            }
        }
        protected virtual void Awake() 
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
        }
    }

   
}
