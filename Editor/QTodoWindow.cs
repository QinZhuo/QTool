using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using QTool.Inspector;
namespace QTool
{
	public class QTodoWindow : EditorWindow
	{
		public static QTodoWindow Instance { private set; get; }
		[MenuItem("QTool/窗口/待办事项")]
		public static void OpenWindow()
		{
			if (Instance == null)
			{
				Instance = GetWindow<QTodoWindow>();
				Instance.minSize = new Vector2(250, 400);
			}
			Instance.titleContent = new GUIContent("代办事项 - " + Application.productName);
			Instance.Show();
		}
		public static string StartKey => nameof(QTool) + "/" + nameof(QTodoData);
		public QList<string,QTodoData> TodoList = new QList<string,QTodoData>(()=>new QTodoData());
		private void OnEnable()
		{
			StartKey.ForeachDirectoryFiles((path) =>
			{
				TodoList.Add(new QTodoData().LoadQData(path));
			});
		}
		private void OnLostFocus()
		{
			foreach (var todo in TodoList)
			{
				todo.SaveQData(StartKey + "/" + todo.Key);
			}
		}
		string text = "";
		Vector2 scrolPosition;
		private void OnGUI()
		{
			using (var scroll= new GUILayout.ScrollViewScope(scrolPosition))
			{
				foreach (var todo in TodoList)
				{
					if (!todo.IsOver)
					{
						using (new GUILayout.HorizontalScope(QGUI.Skin.box))
						{
							GUILayout.Space(30);
							QGUI.Label(todo.Key);
							GUILayout.FlexibleSpace();
							if (QGUI.Button("完成", 60))
							{
								todo.IsOver = true;
							}
							GUILayout.Space(30);
						}
					}
				}
			
				foreach (var todo in TodoList)
				{
					if (todo.IsOver)
					{
						using (new GUILayout.HorizontalScope(QGUI.Skin.box))
						{
							GUILayout.Space(30);
							QGUI.Label(todo.Key);
							GUILayout.FlexibleSpace();
							if (GUILayout.Button("已完成", QGUI.Skin.label))
							{
								todo.IsOver = false;
							}
							GUILayout.Space(30);
						}
					}
				}
				scrolPosition = scroll.scrollPosition;
			}
			using (new GUILayout.HorizontalScope(QGUI.Skin.box))
			{
				text =EditorGUILayout.DelayedTextField(text);
				if(!text.IsNull()&&( Event.current.keyCode == KeyCode.Return||Event.current.keyCode == KeyCode.KeypadEnter))
				{
					TodoList[text].Key = text;
					TodoList[text].IsOver = false;
					text = "";
				}
			}

		}
	}
	public class QTodoData : IKey<string>
	{
		public string Key { get; set; }
		public bool IsOver { get; set; } = false;
	}
}

