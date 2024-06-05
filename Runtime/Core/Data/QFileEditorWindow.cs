using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using QTool.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif
namespace QTool {
	public abstract class QFileEditorWindow<T>
#if UNITY_EDITOR
	: EditorWindow
#endif
	where T : QFileEditorWindow<T> {
		private static T _instance;
		public static T Instance => _instance ??= GetWindow<T>();
		private QHistoryList FilePathList = new QHistoryList(typeof(T).Name);
		[SerializeField]
		private string _filePath = "";
		public string FilePath {
			get => _filePath;
			set {
				if (value != _filePath) {
					StartRecord();
					var select = value.Replace('/', '\\');
					if (select.ExistsFile()) {
						FilePathList.Add(select);
					}
					else {
						return;
					}
					_filePath = value;
#if UNITY_2022_1_OR_NEWER
					if (PathPopup != null) {
						PathPopup.SetValueWithoutNotify(select);
					}
#endif
					OnFocus();
					EndRecord();
				}
			}
		}

		[SerializeField]
		private string _Data = null;
		public string Data {
			get => _Data;
			set {
				if (!Equals(_Data, value)) {
					StartRecord();
					_Data = value;
					PlayerPrefs.SetString(typeof(T).Name + "_" + nameof(Data), value);
					EndRecord();
				}
			}
		}
		public new GUIContent titleContent {
			get {
#if UNITY_EDITOR
				return base.titleContent;
#else
				return new GUIContent();
#endif
			}
		}
		public new VisualElement rootVisualElement {
			get {

#if UNITY_EDITOR
				return base.rootVisualElement;
#else
				return new VisualElement();
#endif
			}
		}

		protected virtual void OnFocus() {
			var path = FilePath;
			if (!AutoLoad)
				return;
			if (path.IsNull()) {
				Data = "";
			}
			else if (!path.IsNull() && QFileTool.ExistsFile(path)) {
#if UNITY_EDITOR
				if (!EditorApplication.isUpdating) {
					var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
					if (!asset.IsNull()) {
						titleContent.text = asset.name + " - " + typeof(T).Name.SplitStartString("Window");
						Data = GetData(asset);
					}
					else {
						Debug.LogError("读取[" + path + "]为空");
					}
				}
#endif
			}
			else {
#if UNITY_EDITOR
				Close();
#endif
				return;
			}
			if (!Data.IsNull()) {
				try {
					ParseData();
				}
				catch (Exception e) {
					Debug.LogError(e);
				}
			}
		}
		protected bool RecordChange { get; private set; } = true;
		public void StartRecord() {
			if (RecordChange) {
				RecordChange = false;
#if UNITY_EDITOR
				Undo.RecordObject(this, "Data Change");
#endif
			}
		}
		public void EndRecord() {
			RecordChange = true;
		}
		public void OnUndoRedo() {
			RecordChange = false;
			PathPopup.value = _filePath.Replace('/', '\\');
			ParseData();
			RecordChange = true;
		}
		public abstract string GetData(UnityEngine.Object file = null);
		public abstract void SetDataDirty();
		public void SaveData() {
			SetDataDirty();
			QFileTool.SaveCheckChange(FilePath, Data);
		}

		public bool AutoLoad { get; set; } = true;
		protected virtual void OnLostFocus() {
			var path = FilePath;
			if (!path.IsNull() && !Data.IsNull()) {
#if UNITY_EDITOR
				if (EditorApplication.isUpdating)
					return;
#endif
				SaveData();
#if UNITY_EDITOR
				AssetDatabase.ImportAsset(path);
#endif
			}
		}
		private void OnEnable() {
			AutoLoad = true;
#if UNITY_EDITOR
			Undo.undoRedoPerformed += OnUndoRedo;
#endif
		}
		private void OnDisable() {
#if UNITY_EDITOR
			Undo.undoRedoPerformed -= OnUndoRedo;
#endif
			OnLostFocus();
		}

#if UNITY_2022_1_OR_NEWER
		protected static PopupField<string> PathPopup { get; private set; } = null;
#endif
		protected virtual void CreateGUI() {
			var Toolbar = rootVisualElement.AddVisualElement();
			Toolbar.style.flexDirection = FlexDirection.Row;
			FilePathList.RemoveAll(path => path.IsNull() || !path.Replace('/', '\\').ExistsFile());
			FilePathList.Save();
#if UNITY_2022_1_OR_NEWER
			PathPopup = Toolbar.AddPopup("", FilePathList.List, FilePath?.Replace('/', '\\'), path => {
				OnLostFocus();
				AutoLoad = true;
				FilePath = path.Replace('\\', '/');
				if (!FilePath.ExistsFile())
					Data = "";
			});
#endif
		}

		protected abstract void ParseData();
	}
	[Serializable]
	public class QHistoryList {
		private string SaveKey { get; set; }
		private int MaxCount { get; set; } = 30;
		private List<string> m_dataList = null;
		public List<string> List {
			get {
				if (m_dataList == null) {
					m_dataList = QPlayerPrefs.Get(SaveKey, new List<string>());
				}
				return m_dataList;
			}
		}
		public string Current => List.StackPeek();
		private QHistoryList() {

		}
		public QHistoryList(string Key, int count = 30) {
			SaveKey = Key + "." + nameof(QHistoryList);
		}
		public void Save() {
			RemoveAll(path => List.IndexOf(path) > MaxCount);
		}
		public void RemoveAll(Predicate<string> check) {
			List.RemoveAll(check);
			QPlayerPrefs.Set(SaveKey, m_dataList);
		}
		public void Add(string path) {
			List.Remove(path);
			List.Add(path);
			Save();
		}
	}
}
