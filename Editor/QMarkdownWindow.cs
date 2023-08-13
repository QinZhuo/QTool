using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using QTool.Inspector;
using UnityEngine.UIElements;
using System;
using System.IO;
using UnityEditor.UIElements;
using QTool.Reflection;

namespace QTool
{

	public class QMarkdownWindow : QTextEditorWindow<QMarkdownWindow>
	{
		#region 静态函数
		[UnityEditor.Callbacks.OnOpenAsset(0)]
		public static bool OnOpen(int instanceID, int line)
		{
			if (EditorUtility.InstanceIDToObject(instanceID) is TextAsset textAsset)
			{
				var path = AssetDatabase.GetAssetPath(textAsset);
				var ext = Path.GetExtension(path).ToLower();
				if (".md".Equals(ext) || ".markdown".Equals(ext))
				{
					FilePath = path;
					OpenWindow();
					return true;
				}
			}
			return false;
		}

		[MenuItem("QTool/窗口/Markdown")]
		public static void OpenWindow()
		{
			var window= GetWindow<QMarkdownWindow>();
			window.minSize = new Vector2(500, 300);
		}
		#endregion
		protected override async void ParseText()
		{
			await QTask.Wait(() => markdownText != null);
			markdownText.value = Text;
		}
		private TextField markdownText =null;
		private ScrollView markdownView = null;

		protected override void CreateGUI()
		{
			base.CreateGUI();
			var root = rootVisualElement;
			markdownView = new ScrollView();
			var editorRange = new ScrollView();
			markdownText = editorRange.AddText("", "", null, true);
			root.Split(editorRange, markdownView);
			markdownText.RegisterCallback<ChangeEvent<string>>((e) => Text=e.newValue);
			editorRange.verticalScroller.valueChanged += (value) =>
			{
				markdownView.verticalScroller.value = value / editorRange.verticalScroller.highValue * markdownView.verticalScroller.highValue;
			};
		}
		protected override async void OnChangeText(string newValue)
		{
			base.OnChangeText(newValue);
			await QTask.Wait(() => markdownView != null);
			markdownView.AddMarkdown(newValue);
		}
	}
	[CustomEditor(typeof(TextAsset))]
	public class MarkdownEditor : Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();
			root.AddMarkdown(target as TextAsset);
			return root;
		}
		
	}

	public abstract class QTextEditorWindow<T> : EditorWindow where T : QTextEditorWindow<T>
	{
		public static string FilePath
		{
			get => PlayerPrefs.GetString(typeof(T).Name + "_" + nameof(FilePath));
			set
			{
				if (value != FilePath)
				{
					PlayerPrefs.SetString(typeof(T).Name + "_" + nameof(FilePath), value);
					UndoList.Clear();
					LastOpenTime = default;
				}
			}
		}
		private static DateTime LastOpenTime =default;
		protected virtual void OnFocus()
		{
			var path = FilePath;
			if (QFileManager.ExistsFile(path))
			{
				var time = QFileManager.GetLastWriteTime(path);
				if(time > LastOpenTime)
				{
					var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
					titleContent = new GUIContent(asset.name + " - " + typeof(T).Name);
					if (asset != null)
					{
						Text = asset.text;
						ParseText();
						Changed = false;
					}
					time = LastOpenTime;
				}
			}
		}
		private string _Text;
		public virtual string Text
		{
			get => _Text;
			set
			{
				OnChangeText(value);
			}
		}
		protected virtual void OnLostFocus()
		{
			var path = FilePath;
			if (!path.IsNull()&& Changed)
			{
				QFileManager.Save(path, Text);
				AssetDatabase.ImportAsset(path);
			}
		}
		protected VisualElement Toolbar { get; private set; } = null;
		protected virtual void CreateGUI()
		{
			Toolbar = rootVisualElement.AddVisualElement();
			Toolbar.style.flexDirection = FlexDirection.Row;
			Toolbar.AddButton("撤销", Undo);
		}
		protected abstract void ParseText();
		private static Stack<string> UndoList = new Stack<string>();
		private bool IsUndo = false;
		protected bool Changed = false;
		public void Undo()
		{
			if (UndoList.Count > 0)
			{
				IsUndo = true;
				Text = UndoList.Pop();
				ParseText();
				IsUndo = false;
			}
		}
		protected virtual void OnChangeText(string newValue)
		{
			if (_Text != newValue)
			{
				if (!IsUndo)
				{
					UndoList.Push(newValue);
				}
				_Text = newValue;
				Changed = true;
			}
		}
	}
}

