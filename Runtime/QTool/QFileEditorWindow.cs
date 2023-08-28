using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using QTool.Reflection;

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
					QPlayerPrefs.Get(typeof(T).Name + "_" + nameof(FilePathList), FilePathList);
					if (select.ExistsFile())
					{
						FilePathList.AddCheckExist(select);
					}
					else
					{
						return;
					}
					AutoSaveLoad = true;
					UndoList.Clear();
					QPlayerPrefs.SetString(typeof(T).Name + "_" + nameof(FilePath), value);
					QPlayerPrefs.Set(typeof(T).Name + "_" + nameof(FilePathList), FilePathList);
					if (PathPopup != null)
					{
						PathPopup.value = select;
					}
				}
			}
		}
		
		private static List<string> FilePathList = new List<string>();
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
		public static UnityEditor.SerializedProperty SerializedProperty { get; set; }
#endif

		protected virtual void OnFocus()
		{
			var path = FilePath;
			if (!AutoSaveLoad) return;
			if (path.IsNull())
			{
				Data = "";
			}
			else if (QFileManager.ExistsFile(path))
			{
				{
#if UNITY_EDITOR
					var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
					titleContent.text = asset.name + " - " + typeof(T).Name.SplitStartString("Window");
					if (asset != null)
					{
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
				Data = GetData();
			}
			if (!Data.IsNull())
			{
				ParseData();
			}
		}
		
		public abstract string GetData(UnityEngine.Object file=null);
		public abstract void SaveData();
		private  string _Data;
		public string Data
		{
			get => _Data;
			private set
			{
				ChangeData(value);
			}
		}
		public static bool AutoSaveLoad { get; set; } = true;
		protected virtual void OnLostFocus()
		{
			var path = FilePath;
			if (AutoSaveLoad)
			{
				if (!path.IsNull())
				{
					SaveData();
#if UNITY_EDITOR
					UnityEditor.AssetDatabase.ImportAsset(path);
#endif
				}
			}
		}
		protected static PopupField<string> PathPopup { get; private set; } = null;
		protected virtual void CreateGUI()
		{
			var Toolbar = rootVisualElement.AddVisualElement();
			Toolbar.style.flexDirection = FlexDirection.Row;
			QPlayerPrefs.Get(typeof(T).Name + "_" + nameof(FilePathList), FilePathList);
			FilePathList.RemoveAll(path => !path.Replace('/', '\\').ExistsFile());
			PathPopup = Toolbar.AddPopup("", FilePathList, FilePath.Replace('/', '\\'), path => { OnLostFocus(); FilePath = path.Replace('\\', '/'); if (!FilePath.ExistsFile()) Data = "";  OnFocus(); });
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
				ParseData();
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
}
