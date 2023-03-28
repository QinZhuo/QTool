using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

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
		}
		public event Action OnUpdateEvent = null;
		public event Action OnDestroyEvent = null;
		public event Action OnLateDestroyEvent = null;
		public event Action OnGUIEvent = null;
		public event Action OnPostRenderEvent = null;
		private void Update()
		{
			OnUpdateEvent?.Invoke();
			QDebug.Fps.Push(1);
		}
		private void OnDestroy()
		{
			OnDestroyEvent?.Invoke();
			OnLateDestroyEvent?.Invoke();
		}
	
	
		private void OnGUI()
		{
			try
			{
				QGUI.BeginRuntimeGUI();
				OnGUIEvent?.Invoke();
				QDebug.QDebugGUI();
				QGUI.EndRuntimeGUI();
			}
			catch (Exception e)
			{
				Debug.LogError("GUI绘制出错：" + e.ToShortString(3000));
			}
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
