#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using QTool.Reflection;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
namespace QTool {
	public abstract class QFileEditorWindow<T> : EditorWindow where T : QFileEditorWindow<T> {
		public static void Open(string path, bool clearList = true) {
			var window = GetWindow<T>();
			window.minSize = new Vector2(500, 300);
			window.FilePath = path;
			if (clearList) {
				window.GraphList.Clear();
			}
			window.GraphList.Add(path);
			window.ParseData();
		}
		#region 基础属性
		[SerializeField]
		private string _filePath = "";
		public string FilePath {
			get => _filePath;
			set {
				if (value != _filePath) {
					StartRecord();
					OnLostFocus();
					AutoLoad = true;
					if (!ExistsFile(value)) {
						return;
					}
					_filePath = value;
					OnChangeFile();
					OnFocus();
					EndRecord();
				}
			}
		}
		protected virtual void OnChangeFile() {

		}
		private bool ExistsFile(string path) => QFileTool.ExistsFile(path) || QObjectTool.GetObject<UnityEngine.Object>(path) != null;
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
		protected List<string> GraphList { get;private set; } = new();
		protected bool RecordChange { get; private set; } = true;
		#endregion
		
		#region Undo Redo
		public void StartRecord() {
			if (RecordChange) {
				RecordChange = false;
				Undo.RecordObject(this, "Data Change");
			}
		}
		public void EndRecord() {
			RecordChange = true;
		}
		public void OnUndoRedo() {
			RecordChange = false;
			ParseData();
			RecordChange = true;
		}
		private void OnEnable() {
			AutoLoad = true;
			Undo.undoRedoPerformed += OnUndoRedo;
		}
		private void OnDisable() {
			Undo.undoRedoPerformed -= OnUndoRedo;
			OnLostFocus();
		}

		#endregion
		protected virtual void OnFocus() {
			var path = FilePath;
			if (!AutoLoad) {
				AutoLoad = true;
				return;
			}
			if (path.IsNull()) {
				Data = "";
			}
			else if (!path.IsNull() && ExistsFile(path)) {
				if (!EditorApplication.isUpdating) {
					titleContent.text = path.SplitEndString("/") + " - " + typeof(T).Name.SplitStartString("Window");
					if (path.StartsWith(Application.streamingAssetsPath)||path.StartsWith("Asset")) {
						Data = File.ReadAllText(path);
					}
					else {
						var obj = QObjectTool.GetObject<UnityEngine.Object>(path);
						if (obj != null) {
							Data = File.ReadAllText(AssetDatabase.GetAssetPath(obj));
						}
					}
				}
			}
			else {
				Close();
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
		public abstract void SetDataDirty();

		public bool AutoLoad { get; set; } = true;
		protected virtual void OnLostFocus() {
			var path = FilePath;
			if (!path.IsNull() && !Data.IsNull()) {
				if (EditorApplication.isUpdating)
					return;
				SetDataDirty();
				if (QFileTool.SaveCheckChange(FilePath, Data)) {
					if (!path.StartsWith(Application.streamingAssetsPath)) {
						AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
					}
				}
			}
		}
		protected abstract void ParseData();
		#region UI显示
		public VisualElement toolbar { get; private set; }
		public ToolbarBreadcrumbs breadcrumbs { get; private set; }
		protected virtual void CreateGUI() {
			toolbar = rootVisualElement.AddVisualElement();
			toolbar.style.flexDirection = FlexDirection.Row;
			breadcrumbs = new ToolbarBreadcrumbs();
			toolbar.Add(breadcrumbs);
		}
		#endregion


	}
}
#endif
