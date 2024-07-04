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
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

namespace QTool
{
	public static class QUIElements
	{
		public static TwoPaneSplitView Split(this VisualElement root, VisualElement left, VisualElement right, float pos = 300, TwoPaneSplitViewOrientation dreciton = TwoPaneSplitViewOrientation.Horizontal)
		{
			var split = new TwoPaneSplitView();
			split.fixedPaneInitialDimension = pos;
			split.orientation = dreciton;
			split.Add(left);
			split.Add(right);
			root.Add(split);
			return split;
		}
		public static Toggle AddToggle(this VisualElement root, string text, bool value, Action<bool> changeEvent = null)
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
		public static EnumField AddEnum(this VisualElement root, string label, Enum defaultValue, Action<Enum> changeEvent = null)
		{
			var visual = new EnumField(label, defaultValue);
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

		public static VisualElement AddObject(this VisualElement root, string label, Type type, UnityEngine.Object defaultValue = null, Action<UnityEngine.Object> changeEvent = null)
		{
#if UNITY_EDITOR
			var visual = new ObjectField(label);
			visual.objectType = type;
			visual.value = defaultValue;
			if (changeEvent != null)
			{
				visual.RegisterCallback<ChangeEvent<UnityEngine.Object>>((evt) =>
				{
					changeEvent(evt.newValue);
				});
			}
#else
			var visual = new Label(label+" "+defaultValue);
#endif
			root.Add(visual);
			return visual;
		}
		public static TextField AddText(this VisualElement root, string label, string defaultValue = "", Action<string> changeEvent = null, bool multiline = false)
		{
			var visual = new TextField(label);
			visual.value = defaultValue;
			if (changeEvent != null)
			{
				visual.RegisterCallback<ChangeEvent<string>>((evt) =>
				{
					changeEvent(evt.newValue);
				});
			}
			visual.multiline = multiline;
			root.Add(visual);
			return visual;
		}

		public static PopupField<T> AddPopup<T>(this VisualElement root, string label, List<T> choices, T defaultValue, Action<T> changeEvent = null)
		{
			if (defaultValue == null)
			{
				defaultValue = (T)typeof(T).CreateInstance();
			}
			if (!choices.Contains(defaultValue))
			{
				choices.Add(defaultValue);
			}
			var visual = new PopupField<T>(label, choices, defaultValue);
			visual.RegisterCallback<ChangeEvent<T>>((evt) =>
			{
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static PopupField<string> AddQPopupAttribute(this VisualElement root, QPopupAttribute att, object obj, Action<object> changeEvent = null)
		{
			var str = obj.QName();
			if (str.IsNull())
			{
				str = "\t";
			}
			var data = QPopupData.Get(obj, att.getListFuncs);
			return root.AddPopup("", data.List, str, (value) =>
			{
				changeEvent(value);
			});
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
		public static VisualElement AddEnumFlags(this VisualElement root, string label, Enum defaultValue = null, Action<Enum> changeEvent = null)
		{
#if UNITY_EDITOR
			var visual = new EnumFlagsField(label, defaultValue);
			visual.RegisterCallback<ChangeEvent<Enum>>((evt) =>
			{
				changeEvent(evt.newValue);
			});
#else
			var visual = new Label(label+" : "+ defaultValue);
#endif
			root.Add(visual);
			return visual;
		}
		public static Label AddLabel(this VisualElement root, string text=nameof(Label), TextAnchor textAlign = TextAnchor.MiddleLeft)
		{
			var label = new Label(text);
			label.name = text;
			label.style.overflow = Overflow.Hidden;
			label.style.unityTextAlign = textAlign;
			root.Add(label);
			return label;
		}
		public static Foldout AddFoldout(this VisualElement root, string text = "", bool value = false)
		{
			var visual = new Foldout();
			visual.text = text;
			visual.value = value;
			root.Add(visual);
			return visual;
		}
		public static ListView AddListView(this VisualElement root, IList itemsSource, Action<VisualElement, int> bindItem = null, Func<VisualElement> makeItem = null)
		{
			var visual = new ListView();
			visual.itemsSource = itemsSource;
			visual.bindItem = bindItem;
			if (makeItem == null)
			{
				makeItem = () => new VisualElement();
			}
			visual.makeItem = makeItem;
			root.Add(visual);
			return visual;
		}
#if UNITY_2022_1_OR_NEWER
		public static TreeView AddTreeView(this VisualElement root, Action<VisualElement, int> bindItem, Func<VisualElement> makeItem = null)
		{
			var visual = new TreeView();
			visual.bindItem = bindItem;
			if (makeItem == null)
			{
				makeItem = () => new VisualElement();
			}
			visual.makeItem = makeItem;
			root.Add(visual);
			return visual;
		}
#endif
		public static VisualElement Name(this VisualElement visual,string name)
		{
			visual.name = name;
			return visual;
		}
		public static VisualElement AddVisualElement(this VisualElement root, FlexDirection flexDirection = FlexDirection.Column)
		{
			var visual = new VisualElement();
			visual.style.flexDirection = flexDirection;
			root.Add(visual);
			return visual;
		}
		public static T IgnoreClick<T>(this T root, bool value = true) where T : VisualElement
		{
			root.pickingMode = value ? PickingMode.Ignore : PickingMode.Position;
			return root;
		}
		public static VisualElement SetBackground(this VisualElement root, Color color = default, float offset = 0)
		{
			root.style.position = Position.Absolute;
			root.style.left = offset;
			root.style.top = offset;
			root.style.right = offset;
			root.style.bottom = offset;
			root.style.backgroundColor = color;
			return root;
		}
		public static QConnectElement AddConnect(this VisualElement root, Color color, float lineWidth = 3)
		{
			var visual = new QConnectElement();
			root.Add(visual);
			visual.StartColor = color;
			visual.EndColor = color;
			visual.LineWidth = lineWidth;
			return visual;
		}
		public static Vector2 GetWorldPosition(this VisualElement root)
		{
			return root.worldTransform.MultiplyPoint(root.transform.position);
		}
		public static void SetWorldPosition(this VisualElement root, Vector2 worldPosition)
		{
			root.style.position = Position.Absolute;
			root.transform.position = worldPosition - root.worldBound.position;
		}
		public static ScrollView AddScrollView(this VisualElement root)
		{
			var visual = new ScrollView();
			root.Add(visual);
			return visual;
		}

#if UNITY_2021_1_OR_NEWER
		public static GroupBox AddGroupBox(this VisualElement root, string name = "")
		{
			var visual = new GroupBox();
			visual.name = name;
			visual.style.flexDirection = FlexDirection.Row;
			visual.style.flexWrap = Wrap.Wrap;
			root.Add(visual);
			return visual;
		}
#else
		public static void Rebuild(this ListView root)
		{
			root.Refresh();
		}
#endif

		public static void AddMenu(this VisualElement root, Action<ContextualMenuPopulateEvent> menuBuilder)
		{
			root.AddManipulator(new ContextualMenuManipulator(menuBuilder));
		}
		public static QDictionary<Type, Func<string, object, Action<object>, VisualElement>> TypeOverride = new QDictionary<Type, Func<string, object, Action<object>, VisualElement>>();
		public static VisualElement Add(this VisualElement root, string name, object obj, Type type, Action<object> changeEvent, ICustomAttributeProvider customAttribute = null)
		{
			if (type == null)
			{
				if (obj == null)
				{
					return root.AddLabel(name);
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
			if (TypeOverride.ContainsKey(type))
			{
				var visual = TypeOverride[type](name, obj, changeEvent);
				root.Add(visual);
				return visual;
			}
			switch (typeInfo.Code)
			{
				case TypeCode.Boolean:
					return root.AddToggle(name, (bool)obj, (value) => changeEvent(value));
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
							return root.AddEnumFlags(name, (Enum)obj, (value) =>
							 {
								 changeEvent(value);
							 });
						}
						else
						{
							return root.AddEnum(name, (Enum)obj, (value) =>
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
							Debug.LogWarning("错误[" + name + "][" + obj?.GetType() + "][" + obj + "] " + e);
						}
						return root.AddInt(name, intValue, (value) => { changeEvent(value); });

					}
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return root.AddInt(name, (int)obj, (value) => { changeEvent(value); });
				case TypeCode.Single:
					return root.AddFloat(name, (float)obj, (value) => { changeEvent(value); });
				case TypeCode.Decimal:
				case TypeCode.Double:
					return root.AddDouble(name, (double)obj, (value) => { changeEvent(value); });
				case TypeCode.String:
					var qpopup = customAttribute?.GetAttribute<QPopupAttribute>();
					if (qpopup != null)
					{
						return root.AddQPopupAttribute(qpopup, obj, (value) => changeEvent(value));
					}
					else
					{
						return root.AddText(name, (string)obj, (value) => { changeEvent(value); });
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
								var dynamicObjectView = root.AddVisualElement();
								dynamicObjectView.style.flexDirection = FlexDirection.Row; ;
								dynamicObjectView.AddLabel(name);
								var typePopup = dynamicObjectView.AddPopup("", typeInfo.Type.GetAllTypes(), obj.GetType(), (newType) =>
								 {
									 obj = newType.CreateInstance();
									 dynamicObjectView.Remove(dynamicObjectView.Q<VisualElement>(nameof(dynamicObjectView)));
									 if (QSerializeType.Get(newType).ObjType != QObjectType.DynamicObject)
									 {
										 var temp = dynamicObjectView.Add("", obj, newType, changeEvent);
										 temp.name = nameof(dynamicObjectView);
									 }
									 changeEvent?.Invoke(obj);
								 });
								typePopup.style.maxWidth = 100;
								if (QSerializeType.Get(typePopup.value).ObjType != QObjectType.DynamicObject)
								{
									var objView = dynamicObjectView.Add("", obj, typePopup.value, changeEvent);
									objView.name = nameof(dynamicObjectView);
								}
								return dynamicObjectView;
							}
						case QObjectType.UnityObject:
							{
								return root.AddObject(name, type, (UnityEngine.Object)obj, (value) => { changeEvent(value); });
							}
						case QObjectType.Object:
							{
								if (obj == null)
								{
									obj = type.CreateInstance();
								}
								if (typeof(QIdObject).IsAssignableFrom(type))
								{
									return root.AddLabel("暂不支持[" + type + "]");
									//obj = (QIdObject)obj.Draw(name);
								}
								else
								{
									var foldout = root.AddFoldout(name);

									foreach (var member in typeInfo.Members)
									{
										foldout.contentContainer.Add(member.QName, member.Get(obj), member.Type, (value) =>
										{
											member.Set(obj, value);
											changeEvent?.Invoke(obj);
										}, member.MemeberInfo);
									}
									return foldout;
								}
							}
						case QObjectType.FixedString: {

							return root.AddText(name, obj.ToString(), newValue => {
								switch (type.Name) {
									case nameof(FixedString32Bytes): {
										var fixedStr = (FixedString32Bytes)obj;
										fixedStr.CopyFromTruncated(newValue);
										changeEvent?.Invoke(fixedStr);
									}
									break;
									case nameof(FixedString64Bytes): {
										var fixedStr = (FixedString64Bytes)obj;
										fixedStr.CopyFromTruncated(newValue);
										changeEvent?.Invoke(fixedStr);
									}
									break;
									case nameof(FixedString512Bytes): {
										var fixedStr = (FixedString512Bytes)obj;
										fixedStr.CopyFromTruncated(newValue);
										changeEvent?.Invoke(fixedStr);
									}
									break;
									case nameof(FixedString4096Bytes): {
										var fixedStr = (FixedString4096Bytes)obj;
										fixedStr.CopyFromTruncated(newValue);
										changeEvent?.Invoke(fixedStr);
									}
									break;
									default:
										break;
								}
							});
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
										changeEvent?.Invoke(list);
									}
									var foldout = root.AddFoldout(name);
									void FreshList()
									{
										foldout.contentContainer.Clear();
										for (int i = 0; i < list.Count; i++)
										{
											object index = i;
											var child = foldout.contentContainer.Add(name + i, list[i], typeInfo.ElementType, (value) =>
											{
												list[(int)index] = value;
												changeEvent?.Invoke(list);
											}, customAttribute);
											child.AddMenu((menu) =>
											{
												menu.menu.AppendAction("新增", action =>
												{
													list = list.CreateAt(typeInfo, (int)index);
													FreshList();
													changeEvent?.Invoke(list);
												});
												menu.menu.AppendAction("删除", action =>
												{
													list = list.RemoveAt(typeInfo, (int)index);
													FreshList();
													changeEvent?.Invoke(list);
												});
											});
										}
										foldout.AddMenu((menu) =>
										{
											menu.menu.AppendAction("新增", action =>
											{
												list = list.CreateAt(typeInfo);
												FreshList();
												changeEvent?.Invoke(list);
											});
										});
									}
									FreshList();
									return foldout;
								}
							}
							break;
						default:
							return root.AddLabel("不支持类型[" + type + "]");
					}
					break;

				case TypeCode.DateTime:
				case TypeCode.Empty:
				case TypeCode.DBNull:
				default:
					return root.AddLabel(name + "\t" + obj?.ToString());
			}
			return root.AddLabel(name + " " + obj);
		}
		public static void SetBorder(this IStyle style, Color color, float width = 1, float radius = 5)
		{
			style.borderTopWidth = width;
			style.borderBottomWidth = width;
			style.borderLeftWidth = width;
			style.borderRightWidth = width;
			style.borderTopColor = color;
			style.borderBottomColor = color;
			style.borderLeftColor = color;
			style.borderRightColor = color;
			style.borderTopLeftRadius = radius;
			style.borderTopRightRadius = radius;
			style.borderBottomLeftRadius = radius;
			style.borderBottomRightRadius = radius;
		}
	
		public static VisualElement Tooltip(this VisualElement root, string text)
		{
			root.tooltip = text?.Replace("\\n", " "); 
			return root;
		}
		public static void Add(this VisualElement root, QInspectorType inspectorType, object target)
		{
			foreach (var func in inspectorType.buttonFunc)
			{
				root.AddButton(func.QName, () =>
				{
					func.Invoke(target);
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
					root = root.AddScrollView();
					foreach (var SettingType in typeof(QInstanceScriptable<>).GetAllTypes())
					{
						root.AddLabel(QReflection.QName(SettingType));
						if (SettingType.InvokeFunction(nameof(global::QTool.QToolSetting.IsExist)) is bool boolValue && boolValue)
						{
							root.Add(new InspectorElement(new UnityEditor.SerializedObject(SettingType.InvokeFunction(nameof(global::QTool.QToolSetting.Instance)) as ScriptableObject)));
						}
					}
				}
			};
		}

		public static PropertyField Add(this VisualElement root, UnityEditor.SerializedProperty serializedProperty)
		{
			var visual = new PropertyField(serializedProperty, QReflection.QName(serializedProperty));
			visual.name = visual.label;
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
					else
					{
						var att = iterator.GetAttribute<QNameAttribute>();
						if (att != null && !att.visibleControl.IsNull())
						{
							var copy = iterator.Copy();
							visual.style.display = copy.IsShow() ? DisplayStyle.Flex : DisplayStyle.None;
							root.RegisterCallback<ClickEvent>(data =>
							{
								visual.style.display = copy.IsShow() ? DisplayStyle.Flex : DisplayStyle.None;
							});
						}
					}

				} while (iterator.NextVisible(false));
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
							root.AddLabel(text).Tooltip(line).style.fontSize = size;
							newline = false;
							CheckNewLine();
						}
						break;
					case ">":
						{
							var label = root.AddLabel("||  " + text).Tooltip(line);
							label.style.fontSize = 15;
							label.style.color = Color.HSVToRGB(1, 0, 0.8f);
							newline = false;
						}
						break;
					case "-":
						{
							root.AddLabel("◆ " + text).Tooltip(line);
							newline = false;
						}
						break;
					default:
						if (line.IsNull() || line.Length >= 3 && line.Replace("*", "").Replace("-", "").Replace("_", "").IsNull())
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
	public class QPopupData
	{
		static QDictionary<string, QPopupData> DrawerDic = new QDictionary<string, QPopupData>((key) => new QPopupData());

		public static void ClearAll()
		{
			QDataList.UnLoadAll();
			DrawerDic.Clear();
		}

		public List<string> List = new List<string>();
		public Type Type { get; private set; }
		
		public static QPopupData Get(object obj, params string[] getListFuncs)
		{
			Type type = null;
			if (obj is Type)
			{
				type = obj as Type;
			}
			else
			{
				type = obj?.GetType();
			}

			var drawerKey = getListFuncs.ToOneString(" ");
			if (drawerKey.IsNull())
			{
#if UNITY_EDITOR
				if (obj is UnityEditor.SerializedProperty property)
				{
					type = QReflection.ParseType(property.type.SplitEndString("PPtr<$").TrimEnd('>'));
				}
#endif
				drawerKey = type?.ToString();
			}
			var drawer = DrawerDic[drawerKey];
			drawer.Type = type;

			if (getListFuncs.Length > 0)
			{
				drawer.List.Clear();
				drawer.List.Add("\t");
				foreach (var getListFunc in getListFuncs)
				{
					if (getListFunc.StartsWith(nameof(Resources) + "/"))
					{
						var assets = Resources.LoadAll(getListFunc.SplitEndString(nameof(Resources) + "/"));
						foreach (var asset in assets)
						{
							drawer.List.Add(asset.name);
						}
					}
					else
					{
						var funcKey = getListFunc;
						if (!funcKey.Contains('.') && !funcKey.EndsWith(".List") && QReflection.ParseType(funcKey).Is(typeof(QDataList<>)))
						{
							funcKey += ".List"; 
						}
						if (obj.InvokeFunction(funcKey) is IEnumerable itemList)
						{
							foreach (var item in itemList)
							{
								drawer.List.Add(item.QName());
							}
						}
					}
				}
			}
			else if (drawer.List.Count == 0)
			{
				if (type.IsAbstract)
				{
					drawer.List.Add("\t");
					foreach (var childType in type.GetAllTypes())
					{
						drawer.List.Add(childType.Name);
					}
				}
				else if (type.IsEnum)
				{
					foreach (var name in Enum.GetNames(type))
					{
						drawer.List.Add(name);
					}
				}
#if UNITY_EDITOR
				else if (obj is UnityEditor.SerializedProperty property)
				{
					drawer.List.Clear();
					switch (property.propertyType)
					{
						case UnityEditor.SerializedPropertyType.Enum:
							{
								foreach (var item in property.enumNames)
								{
									drawer.List.Add(item);
								}
							}
							break;
						default:
							break;
					}
				}
#endif
				else
				{
					Debug.LogError("错误函数[" + getListFuncs.ToOneString(" ") + "]");
				}
			}
			return drawer;
		}
	}
}
