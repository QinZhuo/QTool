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
		[UnityEditor.Callbacks.OnOpenAsset(0)]
		public static bool OnOpen(int instanceID, int line)
		{
			if (EditorUtility.InstanceIDToObject(instanceID) is TextAsset textAsset)
			{
				var path = AssetDatabase.GetAssetPath(textAsset);
				var ext = Path.GetExtension(path).ToLower();
				if (".md".Equals(ext) || ".markdown".Equals(ext))
				{
					target = textAsset;
					OpenWindow();
					return true;
				}
			}
			return false;
		}
		[SerializeField]private static TextAsset target;
		[MenuItem("QTool/窗口/Markdown")]
		public static void OpenWindow()
		{
			var window= GetWindow<QMarkdownWindow>();
			window.minSize = new Vector2(250, 400);
			window.titleContent = new GUIContent("Markdown - " + Application.productName);
			window.Show();
		}
	
		public static string StartKey => nameof(QTool) + "/" + nameof(QMarkdownWindow);
		private void OnEnable()
		{
		}
		private void OnLostFocus()
		{
		
		}
		private void CreateGUI()
		{
			var root = rootVisualElement;
			var editor = new ScrollView();
			var view = new ScrollView();
			root.Split(editor, view);
			if (target != null)
			{
				var text = new TextField();
				text.multiline = true;
				editor.Add(text);
				view.AddMarkdown(target.text);
			}
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
	public static class QUIElements
	{
		private static StyleSheet m_QMarkdown;
		public static StyleSheet QMarkdown => m_QMarkdown ??= Resources.Load<StyleSheet>(nameof(QMarkdown));
		public static TwoPaneSplitView Split(this VisualElement root,params VisualElement[] visualElements)
		{
			var split = new TwoPaneSplitView();
			split.fixedPaneInitialDimension = 200;
			foreach (var visualElement in visualElements)
			{
				split.Add(visualElement);
			}
			root.Add(split);
			return split;
		}
		public static Button AddButton(this VisualElement root,string text, Action clickEvent=null)
		{
			var button = new Button(clickEvent);
			button.text = text;
			root.Add(button);
			return button;
		}
		public static Label AddLabel(this VisualElement root, string text)
		{
			var label = new Label(text);
			label.enableRichText = false;
			root.Add(label);
			return label;
		}
		public static VisualElement ToolTip(this VisualElement root, string text)
		{
			root.tooltip = text;
			return root;
		}
		public static string AddMarkdown(this VisualElement root,TextAsset textAsset)
		{
			var path = AssetDatabase.GetAssetPath(textAsset);
			var ext = Path.GetExtension(path).ToLower();
			if (".md".Equals(ext) || ".markdown".Equals(ext))
			{
				root.AddMarkdown(textAsset.text);
			}
			else
			{
				root.AddLabel(textAsset.text);
			}
			return path;
		}
		public static VisualElement AddMarkdown(this VisualElement root, string markdown)
		{
			root.styleSheets.Add(QMarkdown);
			foreach (var line in markdown.Split('\n'))
			{
				line.SplitTowString(" ", out var key, out var text);
				switch (key)
				{
					case "#":
					case "##":
					case "###":
					case "####":
					case "#####":
					case "######":
						{
							var label= root.AddLabel(text).ToolTip(line);
							label.name = "h"+key.Length;
						}
						break;
					case ">":
						{
							var label= root.AddLabel("||  "+text).ToolTip(line);
							label.name = "r";
						}
						break;
					case "-":
						{
							root.AddLabel("◆ " + text).ToolTip(line);
						}break;
					default:
						if(line.Length>=3&&line.Replace("*","").Replace("-","").Replace("_","").IsNull())
						{
							root.AddLabel(" ");
						} 
						else
						{
							root.AddLabel(line);
						}
						break;
				}
			}
			return root;
		}
	} 
}

