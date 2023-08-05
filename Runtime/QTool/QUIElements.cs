using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;
using QTool.Inspector;
using QTool.Reflection;
#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

namespace QTool
{
	public static class QUIElements
	{
		public static TwoPaneSplitView Split(this VisualElement root, params VisualElement[] visualElements)
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
		public static Button AddButton(this VisualElement root, string text, Action clickEvent = null)
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
		public static VisualElement ToolTip(this VisualElement root, string text)
		{
			root.tooltip = text;
			return root;
		}
		public static void Add(this VisualElement root, QInspectorType inspectorType,object target)
		{
			foreach (var butInfo in inspectorType.buttonFunc)
			{
				var name = butInfo.Value.name.IsNull() ? butInfo.Key.Name : butInfo.Value.name;
				root.AddButton(name, () =>
				{
					butInfo.Key.Invoke(target);
				});
			}
		}
#if UNITY_EDITOR
		[UnityEditor.SettingsProvider]
		public static UnityEditor.SettingsProvider QToolSetting()
		{
			return new UnityEditor.SettingsProvider("Project/" + nameof(QTool) + "设置", UnityEditor.SettingsScope.Project)
			{
				activateHandler = (searchContext, root) =>
				{
					foreach (var SettingType in typeof(InstanceScriptable<>).GetAllTypes())
					{
						root.AddLabel(SettingType.QName());
						root.Add(new InspectorElement(new UnityEditor.SerializedObject(SettingType.InvokeFunction(nameof(global::QTool.QToolSetting.Instance)) as ScriptableObject)));
					}
				}
			};
		}
		public static TextField AddTextField(this VisualElement root, UnityEditor.SerializedObject serializedObject, string path)
		{
			var visual = new TextField();
			visual.multiline = true;
			visual.bindingPath = path;
			visual.Bind(serializedObject);
			root.Add(visual);
			return visual;
		}
	
		public static PropertyField Add(this VisualElement root, UnityEditor.SerializedProperty serializedProperty)
		{
			var visual = new PropertyField(serializedProperty, serializedProperty.QName());
			root.Add(visual);
			return visual;
		}
		public static void Add(this VisualElement root, UnityEditor.SerializedObject serializedObject)
		{
			var iterator = serializedObject.GetIterator();
			if (iterator.NextVisible(true))
			{
				do
				{
					var visual = root.Add(iterator);
					if ("m_Script".Equals(iterator.name))
					{
						visual.SetEnabled(false);
					}

				} while (iterator.NextVisible(false));
				serializedObject.ApplyModifiedProperties();
			}
		}
		public static string AddMarkdown(this VisualElement root, TextAsset textAsset)
		{
			var path = UnityEditor.AssetDatabase.GetAssetPath(textAsset);
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
#endif
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
							var label = root.AddLabel(text).ToolTip(line);
							label.style.fontSize = Mathf.Lerp(25, 10, (key.Length - 1) / 5f);
						}
						break;
					case ">":
						{
							var label = root.AddLabel("||  " + text).ToolTip(line);
							label.style.fontSize = 15;
							label.style.color = Color.HSVToRGB(1, 0, 0.8f);
						}
						break;
					case "-":
						{
							root.AddLabel("◆ " + text).ToolTip(line);
						}
						break;
					default:
						if (line.Length >= 3 && line.Replace("*", "").Replace("-", "").Replace("_", "").IsNull())
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
