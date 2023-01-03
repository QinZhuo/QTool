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
		int CommondIndex = 0;
		List<string> Commonds = new List<string>();
		object[] CommondObjs = null;
		public void InitCommond()
		{
			if (Commonds.Count == 0)
			{
				foreach (var kv in QCommand.NameDictionary)
				{
					if (kv.Value.IsStringCommond)
					{
						Commonds.Add(kv.Key);
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
					InitCommond();
					var newIndex = GUILayout.SelectionGrid(CommondIndex, Commonds.ToArray(), 10);
					if (newIndex != CommondIndex)
					{
						CommondIndex = newIndex;
						CommondObjs = new object[QCommand.NameDictionary[Commonds[CommondIndex]].paramInfos.Length];
					}
					GUILayout.FlexibleSpace();
					using (new GUILayout.HorizontalScope())
					{
						GUILayout.Label(Commonds[CommondIndex]);
						for (int i = 0; i < QCommand.NameDictionary[Commonds[CommondIndex]].paramInfos.Length; i++)
						{
							var p = QCommand.NameDictionary[Commonds[CommondIndex]].paramInfos[i];
							if (CommondObjs[i] == null)
							{
								CommondObjs[i] = p.DefaultValue;
							}

							if (p.ParameterType == typeof(int) || p.ParameterType == typeof(float))
							{
								var newText = GUILayout.TextField(CommondObjs[i]?.ToString(),20);
								if (float.TryParse(newText, out var value))
								{
									CommondObjs[i] = value;
								}
								else
								{
									CommondObjs[i] = 0;
								}
							}
							else if (p.ParameterType == typeof(string) || p.ParameterType == typeof(object))
							{
								var newText = GUILayout.TextField(CommondObjs[i]?.ToString(), 20);
								CommondObjs[i] = newText;
							}
							else
							{
								GUILayout.Label(CommondObjs[i]?.ToString());
							}
						}
					}
					if (GUILayout.Button("确定"))
					{
						QTime.RevertScale(nameof(QCommand));
						UsingCommmond = false;
						QCommand.NameDictionary[Commonds[CommondIndex]].Invoke(CommondObjs);
					}
				}
				else if (QDemo.Ctrl && QDemo.Enter)
				{
					QTime.ChangeScale(nameof(QCommand), 0);
					UsingCommmond = true;
				}
#endif
			}
			catch { }
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
