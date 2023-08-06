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
	public class QMarkdownWindow : EditorWindow
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
					PlayerPrefs.SetString(nameof(QMarkdownWindow), path);
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
			window.minSize = new Vector2(250, 400);
			window.Show();
		}
		#endregion
		private async void OnFocus()
		{
			titleContent = new GUIContent("Markdown - " + Application.productName);
			var path = PlayerPrefs.GetString(nameof(QMarkdownWindow));
			if (!path.IsNull())
			{
				var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
				if (asset != null)
				{
					await QTask.Wait(() => markdownText != null);
					markdownText.value = asset.text;
				}
			}
		}
		private void OnLostFocus()
		{
			
			var path = PlayerPrefs.GetString(nameof(QMarkdownWindow));
			if (File.Exists(path)&& markdownText!=null)
			{
				QFileManager.Save(path, markdownText.text);
				AssetDatabase.ImportAsset(path);
			}
		}
		private TextField markdownText =null;
		private ScrollView markdownView = null;
		private Button undoButton = null;
		private Stack<string> UndoList = new Stack<string>();
		private void CreateGUI()
		{
			var root = rootVisualElement;
			markdownView = new ScrollView();
			var left = new VisualElement();
			undoButton= left.AddButton("撤销", Undo);
			var editorRange = left.AddScrollView();
			markdownText = editorRange.AddText("", "",null, true);
			root.Split(left, markdownView);
			markdownText.RegisterCallback<ChangeEvent<string>>(OnTextChange);
			editorRange.verticalScroller.valueChanged += (value) =>
			{
				markdownView.verticalScroller.value = value / editorRange.verticalScroller.highValue * markdownView.verticalScroller.highValue;
			};
		}
		public void Undo()
		{
			if (UndoList.Count > 0)
			{
				markdownText.value = UndoList.Peek();
			}
		}
		private void OnTextChange(ChangeEvent<string> evt)
		{
			if (UndoList.Count > 0 && evt.newValue == UndoList.Peek())
			{
				UndoList.Pop();
			}
			else
			{
				UndoList.Push(evt.previousValue);
			}
			undoButton.visible = true;
			markdownView.AddMarkdown(evt.newValue);
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

}

