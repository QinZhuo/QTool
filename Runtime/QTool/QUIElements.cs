using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;
using QTool.Inspector;
using QTool.Reflection;
using System.Reflection;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

namespace QTool
{
	public static class QUIElements
	{
		public static TwoPaneSplitView Split(this VisualElement root, VisualElement left,VisualElement right,float pos=300, TwoPaneSplitViewOrientation dreciton= TwoPaneSplitViewOrientation.Horizontal)
		{
			var split = new TwoPaneSplitView();
			split.fixedPaneInitialDimension = pos;
			split.orientation = dreciton;
			split.Add(left);
			split.Add(right);
			root.Add(split);
			return split;
		}
		public static Toggle AddToggle(this VisualElement root, string text,bool value, Action<bool> changeEvent = null)
		{
			var visual = new Toggle();
			visual.name = text;
			visual.text = text;
			visual.value = value;
			visual.RegisterCallback<ChangeEvent<bool>>((evt) =>
			{
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static Button AddButton(this VisualElement root, string text, Action clickEvent = null)
		{
			var visual = new Button(clickEvent);
			visual.name = text;
			visual.text = text;
			root.Add(visual);
			return visual;
		}
		public static EnumField AddEnum(this VisualElement root, string label, Enum defaultValue , Action<Enum> changeEvent = null)
		{
			var visual = new EnumField(label,defaultValue);
			visual.RegisterCallback<ChangeEvent<Enum>>((evt) =>
			{
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static FloatField AddFloat(this VisualElement root, string label, float defaultValue = 0, Action<float> changeEvent = null)
		{
			var visual = new FloatField(label);
			visual.value = defaultValue;
			visual.RegisterCallback<ChangeEvent<float>>((evt) =>
			{
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static DoubleField AddDouble(this VisualElement root, string label, double defaultValue = 0, Action<double> changeEvent = null)
		{
			var visual = new DoubleField(label);
			visual.value = defaultValue;
			visual.RegisterCallback<ChangeEvent<double>>((evt) =>
			{
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static ObjectField AddObject(this VisualElement root, string label,Type type, UnityEngine.Object defaultValue = null, Action<UnityEngine.Object> changeEvent = null)
		{
			var visual = new ObjectField(label);
			visual.objectType = type;
			visual.value = defaultValue;
			visual.RegisterCallback<ChangeEvent<UnityEngine.Object>>((evt) =>
			{
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static TextField AddText(this VisualElement root, string label, string defaultValue = "", Action<string> changeEvent = null, bool multiline = false)
		{
			var visual = new TextField(label);
			visual.value = defaultValue;
			visual.RegisterCallback<ChangeEvent<string>>((evt) =>
			{
				changeEvent(evt.newValue);
			});
			visual.multiline = multiline;
			root.Add(visual);
			return visual;
		}
	
		public static PopupField<T> AddPopup<T>(this VisualElement root, string label, List<T> choices, T defaultValue, Action<T> changeEvent = null)
		{
			var visual = new PopupField<T>(label, choices, defaultValue);
			visual.RegisterCallback<ChangeEvent<T>>((evt) =>
			{
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static DoubleField AddQEnumAttribute(this VisualElement root, QEnumAttribute att, object obj, Action<object> changeEvent = null)
		{
			var str = obj.ToGUIContent().text;
			var visual = new DoubleField();
			var data = QEnumListData.Get(obj?.GetType(), att.funcKey);
			root.AddPopup("", data.List, str, (value) =>
			{
				changeEvent(value);
			});
			root.Add(visual);
			return visual;
		}
		public static IntegerField AddInt(this VisualElement root, string label, int defaultValue = 0, Action<int> changeEvent = null)
		{
			var visual = new IntegerField(label);
			visual.value = defaultValue;
			visual.RegisterCallback<ChangeEvent<int>>((evt) =>
			{
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static EnumFlagsField AddEnumFlags(this VisualElement root, string label, Enum defaultValue = null, Action<Enum> changeEvent = null)
		{
			var visual = new EnumFlagsField(label, defaultValue);
			visual.RegisterCallback<ChangeEvent<Enum>>((evt) =>
			{
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static Label AddLabel(this VisualElement root, string text, TextAnchor textAlign= TextAnchor.MiddleLeft)
		{
			var label = new Label(text);
			label.name = text;
			label.style.unityTextAlign = textAlign;
#if UNITY_2022_1_OR_NEWER
			label.enableRichText = false;
#endif
			root.Add(label);
			return label;
		}
		public static Foldout AddFoldout(this VisualElement root,string text="",bool value=false)
		{
			var visual = new Foldout();
			visual.text = text;
			visual.value = value;
			root.Add(visual);
			return visual;
		}
		public static ListView AddListView(this VisualElement root, IList itemsSource, Func<VisualElement> makeItem, Action<VisualElement, int> bindItem)
		{
			var visual = new ListView();
			visual.itemsSource = itemsSource;
			visual.makeItem = makeItem;
			visual.bindItem = bindItem;
			root.Add(visual);
			return visual;
		}
		public static VisualElement AddVisualElement(this VisualElement root)
		{
			var visual = new VisualElement();
			root.Add(visual);
			return visual;
		}
		public static ScrollView AddScrollView(this VisualElement root)
		{
			var visual = new ScrollView();
			root.Add(visual);
			return visual;
		}
#if UNITY_2021_1_OR_NEWER
		public static TreeView AddTreeView(this VisualElement root, Func<VisualElement> makeItem, Action<VisualElement, int> bindItem)
		{
			var visual = new TreeView(makeItem, bindItem);
			root.Add(visual);
			return visual;
		}

		public static GroupBox AddGroupBox(this VisualElement root,string name="")
		{
			var visual = new GroupBox();
			visual.name = name;
			visual.style.flexDirection = FlexDirection.Row;
			visual.style.flexWrap = Wrap.Wrap;
			root.Add(visual);
			return visual;
		}
#endif
		public static List<Type> TypeList = new List<Type>() { typeof(UnityEngine.Object),typeof(string) };
		public static void Add(this VisualElement root, string name, object obj, Type type, Action<object> changeEvent, ICustomAttributeProvider customAttribute = null)
		{
			var hasName = !string.IsNullOrWhiteSpace(name);
			if (type == null)
			{
				if (obj == null)
				{
					root.AddLabel(name);
					return;
				}
				else
				{
					type = obj.GetType();
				}
			}
			if (obj == null && type.IsValueType)
			{
				obj = type.CreateInstance();
			}
			var typeInfo = QSerializeType.Get(type);
			if (type != typeof(object) && !TypeList.Contains(type) && !type.IsGenericType)
			{
				TypeList.Add(type);
			}
			switch (typeInfo.Code)
			{
				case TypeCode.Boolean:
					root.AddToggle(name, (bool)obj, (value) => changeEvent(value)); break;
				case TypeCode.Char:
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
					if (type.IsEnum)
					{
						var flagsEnum = type.GetAttribute<FlagsAttribute>();
						if (flagsEnum != null)
						{
							root.AddEnumFlags(name, (Enum)obj, (value) =>
							 {
								 changeEvent(value);
							 });
						}
						else
						{
							root.AddEnum(name, (Enum)obj, (value) =>
							{
								changeEvent(value);
							});
						}
					}
					else
					{
						var intValue = 0;
						try
						{
							intValue = (int)obj;
						}
						catch (Exception e)
						{
							Debug.LogWarning("错误[" + name + "][" + obj?.GetType() + "]["+obj+"] " + e);
						}
						root.AddInt(name, intValue, (value) => { changeEvent(value); });

					}
					break;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					root.AddInt(name, (int)obj, (value) => { changeEvent(value); }); break;
				case TypeCode.Single:
					root.AddFloat(name, (float)obj, (value) => { changeEvent(value); }); break;
				case TypeCode.Decimal:
				case TypeCode.Double:
					root.AddDouble(name, (double)obj, (value) => { changeEvent(value); }); break;
				case TypeCode.String:
					var enumView = customAttribute?.GetAttribute<QEnumAttribute>();
					if (enumView != null)
					{
						root.AddQEnumAttribute(enumView, obj, (value) => changeEvent(value));break;
					}
					else
					{
						root.AddText(name, (string)obj, (value) => { changeEvent(value); }); break;
					}
				case TypeCode.Object:
					switch (typeInfo.ObjType)
					{
						case QObjectType.DynamicObject:
							{
								if (obj == null)
								{
									obj = "";
								}
								var objView = root.AddVisualElement();
								objView.style.flexDirection = FlexDirection.Row; ;
								var typePopup= objView.AddPopup(name, TypeList, obj.GetType(), (newType) =>
								{
									obj = newType.CreateInstance();
									root.Remove(objView);
									root.Add(name, obj, type, changeEvent, customAttribute);
								});
								if (QSerializeType.Get(typePopup.value).ObjType!= QObjectType.DynamicObject)
								{
									objView.Add("", obj, typePopup.value, changeEvent);
								}
							}
							break;
						case QObjectType.UnityObject:
							{
								root.AddObject(name,type, (UnityEngine.Object)obj, (value) => { changeEvent(value); });
							}
							break;
						case QObjectType.Object:
							{
								if (obj == null)
								{
									obj = type.CreateInstance();
								}
								if (typeof(QIdObject).IsAssignableFrom(type))
								{
									obj = (QIdObject)obj.Draw(name);
								}
								else
								{
									if (hasName)
									{
										root= root.AddFoldout(name).contentContainer;
									}

									foreach (var member in typeInfo.Members)
									{
										root.Add(member.QName, member.Get(obj), member.Type, (value) => { member.Set(obj, value); });
									}
								}
							}
							break;
						case QObjectType.Array:
						case QObjectType.List:
							{
								if (typeof(IList).IsAssignableFrom(type))
								{
									if (typeInfo.ArrayRank > 1)
									{
										break;
									}
									var list = obj as IList;
									if (list == null)
									{
										obj = typeInfo.ArrayRank == 0 ? type.CreateInstance() : type.CreateInstance(null, 0);
										list = obj as IList;
									}
									root = root.AddFoldout(name).contentContainer;
									root.AddListView(list, () => new VisualElement(), (element, index) =>
									{
										element.Clear();
										element.Add(index.ToString(), list[index], typeInfo.ElementType, (value) =>
										{
											list[index] = value;
										});
									});
								}
							}
							break;

						default:
							break;
					}
					break;

				case TypeCode.DateTime:

				case TypeCode.Empty:
				case TypeCode.DBNull:
				default:
					root.AddLabel(name+"\t"+ obj?.ToString());
					break;
			}
		}
		public static VisualElement AddQCommandInfo(this VisualElement root, QCommandInfo commandInfo,Action callBack)
		{
			root = root.AddVisualElement();
			root.style.flexDirection = FlexDirection.Row;
			root.AddLabel(commandInfo.name);
			for (int i = 0; i < commandInfo.paramInfos.Length; i++)
			{
				var info = commandInfo.paramInfos[i];
				if (i >= commandInfo.TempValues.Count && info.HasDefaultValue)
				{
					commandInfo.TempValues[i] = info.DefaultValue;
				}
				root.Add(commandInfo.paramViewNames[i], commandInfo.TempValues[i], info.ParameterType, (value) =>
				 {
					 commandInfo.TempValues[commandInfo.paramInfos.IndexOf(info)] = value;
				 });
			}
			root.AddButton("运行", () => { commandInfo.Invoke(commandInfo.TempValues.ToArray());callBack(); });
			return root;
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
			var newline = false;
			void CheckNewLine()
			{
				if (!newline)
				{
					root.AddLabel("");
					newline = true;
				}
			}
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
							CheckNewLine();
							var size = Mathf.Lerp(25, 10, (key.Length - 1) / 5f);
							root.AddLabel(text).ToolTip(line).style.fontSize = size;
							newline = false;
							CheckNewLine();
						}
						break;
					case ">":
						{
							var label = root.AddLabel("||  " + text).ToolTip(line);
							label.style.fontSize = 15;
							label.style.color = Color.HSVToRGB(1, 0, 0.8f);
							newline = false;
						}
						break;
					case "-":
						{
							root.AddLabel("◆ " + text).ToolTip(line);
							newline = false;
						}
						break;
					default:
						if (line.IsNull()||line.Length >= 3 && line.Replace("*", "").Replace("-", "").Replace("_", "").IsNull())
						{
							CheckNewLine();
						}
						else
						{
							root.AddLabel(line);
							newline = false;
						}
						break;
				}
			}
			return root;
		}
	}
}
