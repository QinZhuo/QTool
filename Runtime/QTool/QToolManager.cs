using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
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
		private void Update()
		{
			OnUpdateEvent?.Invoke();
			Fps.Push(1);
		}
		private void OnDestroy()
		{
			OnDestroyEvent?.Invoke();
			OnLateDestroyEvent?.Invoke();
		}
		public int FPS => (int)Fps.SecondeSum;
		QAverageValue Fps = new QAverageValue();
		bool UsingCommmond = false;
		private QToolBar toolBar = null;
		public void InitCommond()
		{
			if (toolBar==null)
			{
				toolBar = new QToolBar();
				foreach (var kv in QCommand.NameDictionary)
				{
					if (kv.Value.IsStringCommond)
					{
						if (kv.Value.name.SplitTowString("/",out var start,out var end))
						{
							toolBar["命令"][start][kv.Key].Value = kv.Value;
						}
						else if (kv.Value.fullName.SplitTowString("/", out start, out end))
						{
							toolBar["命令"][start][kv.Key].Value = kv.Value;
						}
						else
						{
							Debug.LogError("命令出错[" + kv.Value.fullName + "]:" + kv.Key);
						}
					}
				}
			}
		}
		
		private void OnGUI()
		{
			try
			{
#if DEVELOPMENT_BUILD || UNITY_EDITOR
				GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label(FPS.ToString());
				GUILayout.EndHorizontal();
				GUILayout.EndArea();
				if (UsingCommmond)
				{
					QGUI.BeginRuntimeGUI();
					GUI.Box(new Rect(-1, 0, Screen.width+1, Screen.height+1),"", QGUI.BackStyle);
					GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
					InitCommond();
					if( toolBar.Draw() is QCommandInfo qCommand)
					{
						qCommand.Draw("命令");
					}
					//CommondTypeIndex = QGUI.DropdownButton(CommondTypeIndex, Types.ToArray());
					//if(CommondIndex>= Commonds[Types[CommondTypeIndex]].Count)
					//{
					//	CommondIndex = 0;
					//}
					//CommondIndex = QGUI.DropdownButton(CommondIndex, Commonds[Types[CommondTypeIndex]].ToArray());
					//var name = Commonds[Types[CommondTypeIndex]][CommondIndex];
					//if (QCommand.NameDictionary[name].paramInfos != null&&CommondParams.Count!= QCommand.NameDictionary[name].paramInfos.Length)
					//{
					//	CommondParams.Clear();
					//}
					//if (QCommand.NameDictionary[name].paramInfos != null)
					//{
					//	for (int i = 0; i < QCommand.NameDictionary[name].paramInfos.Length; i++)
					//	{
					//		var p = QCommand.NameDictionary[name].paramInfos[i];
					//		if (CommondParams[i].IsNull())
					//		{
					//			CommondParams[i] = p.DefaultValue.ToQDataType(p.ParameterType);
					//		}
					//		using (new GUILayout.HorizontalScope())
					//		{
					//			GUILayout.Label("    " + QCommand.NameDictionary[name].paramViewNames[i] + ":");
					//			CommondParams[i] = GUILayout.TextField(CommondParams[i], 20);
					//		}
					//	}
					//}
					//if (GUILayout.Button("运行"))
					//{
					//	QTime.RevertScale(nameof(QCommand));
					//	UsingCommmond = false;
					//	QCommand.NameDictionary[name].Invoke(CommondParams);
					//}
					GUILayout.EndArea();
					QGUI.EndRuntimeGUI();
				}
				else if (QDemoInput.Ctrl && QDemoInput.Enter)
				{
					QTime.ChangeScale(nameof(QCommand), 0);
					UsingCommmond = true;
				}
#endif
			}
			catch (Exception e)
			{
				Debug.LogError("GUI绘制出错：" + e.ToShortString(1000));
			}
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
