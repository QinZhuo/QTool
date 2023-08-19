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
					UndoList.Clear();
					QPlayerPrefs.SetString(typeof(T).Name + "_" + nameof(FilePath), value);
					LastWriteTime = default;
					QPlayerPrefs.Get(typeof(T).Name + "_" + nameof(FilePathList), FilePathList);
					var select = value.Replace('/', '\\');
					FilePathList.AddCheckExist(select);
					QPlayerPrefs.Set(typeof(T).Name + "_" + nameof(FilePathList), FilePathList);
					if (PathPopup != null)
					{
						PathPopup.value = select;
					}
				}
			}
		}
		
		private static List<string> FilePathList = new List<string>();
		private static DateTime LastWriteTime = default;

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
		public static UnityEditor.SerializedProperty serializedProperty { get; set; }
#endif

		protected virtual void OnFocus()
		{
			if (!AutoSaveLoad) return;
			var path = FilePath;
			if (QFileManager.ExistsFile(path))
			{
				var time = QFileManager.GetLastWriteTime(path);
				if (time > LastWriteTime)
				{
#if UNITY_EDITOR
					var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
					titleContent.text = asset.name + " - " + typeof(T).Name.SplitStartString("Window");
					if (asset != null)
					{
						Data = GetData(asset);
					}
					LastWriteTime = time;
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
		public virtual bool AutoSaveLoad { get; set; } = true;
		protected virtual void OnLostFocus()
		{
			var path = FilePath;
			if (AutoSaveLoad)
			{
				SaveData();
				if (!path.IsNull())
				{
#if UNITY_EDITOR
					UnityEditor.AssetDatabase.ImportAsset(path);
#endif
				}
			}
		}
		protected static PopupField<string> PathPopup { get; private set; } = null;
		private Vector2 MousePosition { get; set; }
		protected virtual void CreateGUI()
		{
			var Toolbar = rootVisualElement.AddVisualElement();
			Toolbar.style.flexDirection = FlexDirection.Row;
			QPlayerPrefs.Get(typeof(T).Name + "_" + nameof(FilePathList), FilePathList);
			PathPopup = Toolbar.AddPopup("", FilePathList, FilePath.Replace('/', '\\'), path => { OnLostFocus(); FilePath = path.Replace('\\', '/'); OnFocus(); });
			Toolbar.AddButton("撤销", Undo);
			rootVisualElement.RegisterCallback<MouseMoveEvent>(data =>
			{
				MousePosition = data.mousePosition;
			});
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
