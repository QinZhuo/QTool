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
			window.titleContent = new GUIContent("Markdown - " + Application.productName);
			window.Show();
			var path = PlayerPrefs.GetString(nameof(QMarkdownWindow));
			if (!path.IsNull())
			{
				var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
				if (asset != null)
				{
					window.markdownText.value = asset.text;
				}
			}
		}
		#endregion
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
		private void CreateGUI()
		{
			var root = rootVisualElement;
			markdownView = new ScrollView();
			var left = new ScrollView();
			markdownText = left.AddText("", "", OnTextChange, true);
			root.Split(left, markdownView);
			left.verticalScroller.valueChanged += (value) =>
			{
				markdownView.verticalScroller.value = value / left.verticalScroller.highValue * markdownView.verticalScroller.highValue;
			};
		}

		private void OnTextChange(string text)
		{
			markdownView.AddMarkdown(text);
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

