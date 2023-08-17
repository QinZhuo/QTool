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

	public class QMarkdownWindow : QFileEditorWindow<QMarkdownWindow>
	{
		#region 静态函数
		[UnityEditor.Callbacks.OnOpenAsset(0)]
		public static bool OnOpen(int instanceID, int line)
		{
			if (EditorUtility.InstanceIDToObject(instanceID) is TextAsset asset)
			{
				var path = AssetDatabase.GetAssetPath(asset);
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

		[MenuItem("QTool/窗口/QMarkdown")]
		public static void OpenWindow()
		{
			var window = GetWindow<QMarkdownWindow>();
			window.minSize = new Vector2(500, 300);
		}
		#endregion
		protected override async void ParseData()
		{
			await QTask.Wait(() => markdownText != null);
			markdownText.value = Data;
			OnChangeData(Data);
		}
		private TextField markdownText = null;
		private ScrollView markdownView = null;
		private long lastClick = 0;
		private bool Collapse = false;
		protected override void CreateGUI()
		{
			base.CreateGUI();
			var root = rootVisualElement;
			markdownView = new ScrollView();
			var editorRange = new ScrollView();
			markdownText = editorRange.AddText("", "", null, true);
			markdownText.style.minHeight = 1000;
			var split= root.Split(editorRange, markdownView);
			markdownText.RegisterCallback<ChangeEvent<string>>((e) => Data = e.newValue);
			editorRange.verticalScroller.valueChanged += (value) =>
			{
				markdownView.verticalScroller.value = value / editorRange.verticalScroller.highValue * markdownView.verticalScroller.highValue;
			};
			markdownView.RegisterCallback<ClickEvent>(data =>
			{
				if (data.timestamp - lastClick < 500)
				{
					if (!Collapse)
					{
						Collapse = true;
						split.CollapseChild(0);
					}
					else
					{
						Collapse = false;
						split.UnCollapse();
					}
				}
				else
				{
					lastClick = data.timestamp;
				}
			});
		}
		protected override async void OnChangeData(string newValue)
		{ 
			base.OnChangeData(newValue);
			await QTask.Wait(() => markdownView != null); 
			markdownView.AddMarkdown(newValue);
		}

		public override string GetData(UnityEngine.Object file)
		{
			return (file as TextAsset).text;
		}

		public override void SaveData()
		{
			QFileManager.Save(FilePath, Data);
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

