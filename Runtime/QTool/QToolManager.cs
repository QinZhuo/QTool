using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace QTool
{
	public class QToolManager : InstanceManager<QToolManager>
	{
		protected override void Awake()
		{
			if (!Application.isPlaying)
			{
				Destroy(gameObject);
				return;
			}
			base.Awake();
			DontDestroyOnLoad(gameObject);
			Instance.OnUpdateEvent += QCoroutine.Update;
#if UNITY_2021_1_OR_NEWER
			var uiDoc = gameObject.GetComponent<UIDocument>(true);
			uiDoc.panelSettings = Resources.Load<PanelSettings>(nameof(PanelSettings));
			RootVisualElement = uiDoc.rootVisualElement;
#endif
		}
		public event Action OnUpdateEvent = null;
		public event Action OnDestroyEvent = null;
		public event Action OnLateDestroyEvent = null;
		public event Action OnGUIEvent = null;
		public event Action OnPostRenderEvent = null;
		public int FrameIndex { get; private set; } = 0;
#if UNITY_2021_1_OR_NEWER
		public VisualElement RootVisualElement { get; private set; }
#endif
		private void Update()
		{
			FrameIndex++;
			OnUpdateEvent?.Invoke();
		}
		private void OnDestroy()
		{
			QTask.StopAllWait();
			OnDestroyEvent?.Invoke();
			OnLateDestroyEvent?.Invoke();
			Instance.OnUpdateEvent -= QCoroutine.Update;
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

	public abstract class QToolManagerBase<T>:MonoBehaviour where T : QToolManagerBase<T>
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
