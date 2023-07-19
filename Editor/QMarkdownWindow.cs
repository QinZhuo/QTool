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
				var asset= AssetDatabase.LoadAssetAtPath<TextAsset>(path);
				if (asset != null)
				{
					window.markdownString = asset.text;
				}
			}
		}
		#endregion
		private void OnEnable()
		{
			
		}
		private void OnLostFocus()
		{

		}
		[SerializeField] private string markdownString = "";
		private ScrollView markdownView = null;
		private void CreateGUI()
		{
			var root = rootVisualElement;
			markdownView = new ScrollView();
			var left = new ScrollView();
			var text = left.AddTextField(new SerializedObject(this), nameof(markdownString));
			root.Split(left, markdownView);
			text.RegisterValueChangedCallback(OnTextChange);
			left.verticalScroller.valueChanged += (value) =>
			{
				markdownView.verticalScroller.value = value / left.verticalScroller.highValue * markdownView.verticalScroller.highValue;
			};
		}

		private void OnTextChange(ChangeEvent<string> text)
		{
			markdownView.AddMarkdown(text.newValue);
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
		public static TwoPaneSplitView Split(this VisualElement root,params VisualElement[] visualElements)
		{
			var split = new TwoPaneSplitView();
			split.fixedPaneInitialDimension = 300;
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
#if UNITY_2022_1_OR_NEWER
			label.enableRichText = false;
#endif
			root.Add(label);
			return label;
		}
		public static ScrollView AddScrollView(this VisualElement root)
		{
			var visual = new ScrollView();
			root.Add(visual);
			return visual;
		}
		public static TextField AddTextField(this VisualElement root,SerializedObject serializedObject,string path)
		{
			var visual = new TextField();
			visual.multiline = true;
			visual.bindingPath = path;
			visual.Bind(serializedObject);
			root.Add(visual);
			return visual;
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
			root.Clear();
			if (markdown.IsNull()) return root;
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
							label.style.fontSize = Mathf.Lerp(25, 10, (key.Length - 1) / 5f);
						}
						break;
					case ">":
						{
							var label= root.AddLabel("||  "+text).ToolTip(line);
							label.style.fontSize = 15;
							label.style.color = Color.HSVToRGB(1,0, 0.8f);
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

