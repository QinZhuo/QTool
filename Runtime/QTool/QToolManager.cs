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
		public event Action OnGUIEvent = null;
		private void Update()
		{
			OnUpdateEvent?.Invoke();
			Fps.Push(1);
		}
		public int FPS => (int)Fps.SecondeSum;
		QAverageValue Fps = new QAverageValue();
		bool UsingCommmond = false;
		int CommondTypeIndex = 0;
		int CommondIndex = 0;
		List<string> Types = new List<string>();
		QDictionary<string, List<string>> Commonds = new QDictionary<string, List<string>>((key)=>new List<string>());
		QList<string> CommondParams = new QList<string>();
		public void InitCommond()
		{
			if (Commonds.Count == 0)
			{
				foreach (var kv in QCommand.NameDictionary)
				{
					if (kv.Value.IsStringCommond)
					{
						if(kv.Value.name.SplitTowString("/",out var start,out var end))
						{
							Types.AddCheckExist(start);
							Commonds[start].AddCheckExist(kv.Key);
						}
						//else if(kv.Value.fullName.SplitTowString("/", out start, out end))
						//{
						//	Types.AddCheckExist(start);
						//	Commonds[start].Add(end);
						//}
					}
				}
			}
		}
		private void OnGUI()
		{
			try
			{
				OnGUIEvent?.Invoke();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
				GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label(FPS.ToString());
				GUILayout.EndHorizontal();
				GUILayout.EndArea();
				if (UsingCommmond)
				{
					GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
					GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
					GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
					InitCommond();
					CommondTypeIndex = GUILayout.Toolbar(CommondTypeIndex, Types.ToArray());
					if(CommondIndex>= Commonds[Types[CommondTypeIndex]].Count)
					{
						CommondIndex = 0;
					}
					CommondIndex = GUILayout.SelectionGrid(CommondIndex, Commonds[Types[CommondTypeIndex]].ToArray(), 10);
					var name = Commonds[Types[CommondTypeIndex]][CommondIndex];
					GUILayout.FlexibleSpace();
					using (new GUILayout.HorizontalScope())
					{
						GUILayout.Label(name);
						if (QCommand.NameDictionary[name].paramInfos != null)
						{
							for (int i = 0;i < QCommand.NameDictionary[name].paramInfos.Length; i++)
							{
								var p = QCommand.NameDictionary[name].paramInfos[i];
								if (CommondParams[i].IsNullOrEmpty())
								{
									CommondParams[i] = p.DefaultValue.ToQDataType(p.ParameterType);
								}
								CommondParams[i] = GUILayout.TextField(CommondParams[i], 20);
							}
						}
					}
					if (GUILayout.Button("确定"))
					{
						QTime.RevertScale(nameof(QCommand));
						UsingCommmond = false;
						QCommand.NameDictionary[name].Invoke(CommondParams);
					}
					GUILayout.EndArea();
				}
				else if (QDemo.Ctrl && QDemo.Enter)
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
