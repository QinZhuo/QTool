using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using QTool.Reflection;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif
namespace QTool
{
	public abstract class QFileEditorWindow<T>
#if UNITY_EDITOR
	: UnityEditor.EditorWindow
#endif
	where T : QFileEditorWindow<T>
	{
		public static string FilePath
		{
			get => QPlayerPrefs.GetString(typeof(T).Name + "_" + nameof(FilePath));
			set
			{
				if (value != FilePath)
				{
					var select = value.Replace('/', '\\');
					if (select.ExistsFile())
					{
						FilePathList.Add(select);
					}
					else
					{
						return;
					}
					UndoList.Clear();
					QPlayerPrefs.SetString(typeof(T).Name + "_" + nameof(FilePath), value);
#if UNITY_2022_1_OR_NEWER
					if (PathPopup != null)
					{
						PathPopup.value = select;
					}
#endif
				}
			}
		}

		private static QHistoryList FilePathList = new QHistoryList(typeof(T).Name);
		public new GUIContent titleContent
		{
			get
			{
#if UNITY_EDITOR
				return base.titleContent;
#else
				return new GUIContent();
#endif
			}
		}
		public new VisualElement rootVisualElement
		{
			get
			{

#if UNITY_EDITOR
				return base.rootVisualElement;
#else
				return new VisualElement();
#endif
			}
		}
#if UNITY_EDITOR
		public static SerializedProperty SerializedProperty { get; set; }
#endif

		protected virtual void OnFocus()
		{
			var path = FilePath;
			if (!AutoLoad) return;
			if (path.IsNull())
			{
				Data = "";
			}
			else if (QFileTool.ExistsFile(path))
			{
				{
#if UNITY_EDITOR
					var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
					if (asset != null)
					{
						titleContent.text = asset.name + " - " + typeof(T).Name.SplitStartString("Window");
						Data = GetData(asset);
					}
					else
					{
						Debug.LogError("读取[" + path + "]为空");
					}
#endif
				}
			}
			else
			{
#if UNITY_EDITOR
				Close();
#endif
				return;
				//Data = GetData(); 
			}
			if (!Data.IsNull())
			{
				try
				{
					ParseData();
				}
				catch (Exception e)
				{
					Debug.LogError(e);
				}
			}
		}

		public abstract string GetData(UnityEngine.Object file = null);
		public abstract void SaveData();
		private string _Data;
		public string Data
		{
			get => _Data;
			private set
			{
				ChangeData(value);
			}
		}
		public bool AutoLoad { get; set; } = true;
		protected virtual void OnLostFocus()
		{
			var path = FilePath;
			if (!path.IsNull() && !Data.IsNull())
			{
#if UNITY_EDITOR
				if (!EditorApplication.isCompiling)
				{
#endif
					SaveData();
#if UNITY_EDITOR 
				}
				AssetDatabase.ImportAsset(path);
#endif
			}
		}
		private void OnEnable()
		{
			AutoLoad = true;
		}
		private void OnDisable()
		{
			OnLostFocus();
		}
#if UNITY_2022_1_OR_NEWER
		protected static PopupField<string> PathPopup { get; private set; } = null;
#endif
		protected virtual void CreateGUI()
		{
			var Toolbar = rootVisualElement.AddVisualElement();
			Toolbar.style.flexDirection = FlexDirection.Row;
			FilePathList.RemoveAll(path => path.IsNull() || !path.Replace('/', '\\').ExistsFile());
			FilePathList.Save();
#if UNITY_2022_1_OR_NEWER
			PathPopup = Toolbar.AddPopup("", FilePathList.List, FilePath?.Replace('/', '\\'), path => { OnLostFocus(); AutoLoad = true; FilePath = path.Replace('\\', '/'); if (!FilePath.ExistsFile()) Data = ""; OnFocus(); });
#endif
			Toolbar.AddButton("撤销", Undo);
		}
		protected abstract void ParseData();
		private static Stack<string> UndoList = new Stack<string>();
		private bool IsUndo = false;
		public void Undo()
		{
			if (UndoList.Count > 0)
			{
				IsUndo = true;
				Data = UndoList.Pop();
				try
				{
					ParseData();
				}
				catch (Exception e)
				{
					Debug.LogError(e);
				}
				IsUndo = false;
			}
		}
		protected virtual void ChangeData(string newValue)
		{
			if (!Equals(_Data, newValue))
			{
				if (!IsUndo)
				{
					UndoList.Push(newValue);
				}
				_Data = newValue;
			}
		}
	}
	public class QHistoryList
	{
		private string SaveKey { get; set; }
		private int MaxCount { get; set; } = 30;
		private List<string> m_dataList = null;
		public List<string> List
		{
			get
			{
				if (m_dataList == null)
				{
					m_dataList = QPlayerPrefs.Get(SaveKey, new List<string>());
				}
				return m_dataList;
			}
		}
		private QHistoryList()
		{

		}
		public QHistoryList(string Key, int count = 30)
		{
			SaveKey = Key + "." + nameof(QHistoryList);
		}
		public void Save()
		{
			RemoveAll(path => List.IndexOf(path) > MaxCount);
		}
		public void RemoveAll(Predicate<string> check)
		{
			List.RemoveAll(check);
			QPlayerPrefs.Set(SaveKey, m_dataList);
		}
		public void Add(string path)
		{
			List.Remove(path);
			List.Add(path);
			Save();
		}
	}
}
