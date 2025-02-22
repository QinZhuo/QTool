using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;
using QTool.Inspector;
using QTool.Reflection;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace QTool {
	public static class QUIElements {
		public static TwoPaneSplitView Split(this VisualElement root, VisualElement left, VisualElement right, float pos = 300, TwoPaneSplitViewOrientation dreciton = TwoPaneSplitViewOrientation.Horizontal) {
			var split = new TwoPaneSplitView();
			split.fixedPaneInitialDimension = pos;
			split.orientation = dreciton;
			split.Add(left);
			split.Add(right);
			root.Add(split);
			return split;
		}
		public static Toggle AddToggle(this VisualElement root, string text, bool value, Action<bool> changeEvent = null) {
			var visual = new Toggle();
			visual.name = text;
			visual.text = text;
			visual.value = value;
			visual.RegisterCallback<ChangeEvent<bool>>((evt) => {
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static Button AddButton(this VisualElement root, string text, Action clickEvent = null) {
			var visual = new Button(clickEvent);
			visual.name = text;
			visual.text = text;
			root.Add(visual);
			return visual;
		}
		public static Label SetText(this Label root, string text) {
			root.text = text;
			root.style.SetDisplay(!text.IsNull());
			return root;
		}
		public static EnumField AddEnum(this VisualElement root, string label, Enum defaultValue, Action<Enum> changeEvent = null) {
			var visual = new EnumField(label, defaultValue);
			visual.RegisterCallback<ChangeEvent<Enum>>((evt) => {
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static FloatField AddFloat(this VisualElement root, string label, float defaultValue = 0, Action<float> changeEvent = null) {
			var visual = new FloatField(label);
			visual.value = defaultValue;
			visual.RegisterCallback<ChangeEvent<float>>((evt) => {
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static DoubleField AddDouble(this VisualElement root, string label, double defaultValue = 0, Action<double> changeEvent = null) {
			var visual = new DoubleField(label);
			visual.value = defaultValue;
			visual.RegisterCallback<ChangeEvent<double>>((evt) => {
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}

		public static VisualElement AddObject(this VisualElement root, string label, Type type, UnityEngine.Object defaultValue = null, Action<UnityEngine.Object> changeEvent = null) {
#if UNITY_EDITOR
			var visual = new ObjectField(label);
			visual.allowSceneObjects = false;
			visual.objectType = type;
			visual.value = defaultValue;
			if (changeEvent != null) {
				visual.RegisterCallback<ChangeEvent<UnityEngine.Object>>((evt) => {
					changeEvent(evt.newValue);
				});
			}
#else
			var visual = new Label(label+" "+defaultValue);
#endif
			root.Add(visual);
			return visual;
		}
		public static TextField AddText(this VisualElement root, string label, string defaultValue = "", Action<string> changeEvent = null, bool multiline = false) {
			var visual = new TextField(label);
			visual.value = defaultValue;
			if (changeEvent != null) {
				visual.RegisterCallback<ChangeEvent<string>>((evt) => {
					changeEvent(evt.newValue);
				});
			}
			visual.multiline = multiline;
			root.Add(visual);
			return visual;
		}

		public static PopupField<T> AddPopup<T>(this VisualElement root, string label, List<T> choices, T defaultValue, Action<T> changeEvent = null) {
			if (defaultValue == null) {
				defaultValue = (T)typeof(T).CreateInstance();
			}
			if (!choices.Contains(defaultValue)) {
				choices.Add(defaultValue);
			}
			var visual = new PopupField<T>(label, choices, defaultValue);
			visual.RegisterCallback<ChangeEvent<T>>((evt) => {
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static PopupField<string> AddQPopupAttribute(this VisualElement root, QPopupAttribute att, object obj, Action<object> changeEvent = null) {
			var str = obj.QName();
			if (str.IsNull()) {
				str = "\t";
			}
			var data = QPopupData.Get(obj, att.getListFuncs);
			return root.AddPopup("", data.List, str, (value) => {
				changeEvent(value);
			});
		}
		public static IntegerField AddInt(this VisualElement root, string label, int defaultValue = 0, Action<int> changeEvent = null) {
			var visual = new IntegerField(label);
			visual.value = defaultValue;
			visual.RegisterCallback<ChangeEvent<int>>((evt) => {
				changeEvent(evt.newValue);
			});
			root.Add(visual);
			return visual;
		}
		public static VisualElement AddEnumFlags(this VisualElement root, string label, Enum defaultValue = null, Action<Enum> changeEvent = null) {
#if UNITY_EDITOR
			var visual = new EnumFlagsField(label, defaultValue);
			visual.RegisterCallback<ChangeEvent<Enum>>((evt) => {
				changeEvent(evt.newValue);
			});
#else
			var visual = new Label(label+" : "+ defaultValue);
#endif
			root.Add(visual);
			return visual;
		}
		public static Label AddLabel(this VisualElement root, string text = nameof(Label), TextAnchor textAlign = TextAnchor.MiddleLeft) {
			var label = new Label(text);
			label.name = text;
			label.style.overflow = Overflow.Hidden;
			label.style.unityTextAlign = textAlign;
			root.Add(label);
			return label;
		}
		private static QDictionary<string, bool> FoldoutCache;
		private static int GetDeep(this VisualElement root) {
			if (root.parent == null) {
				return 1;
			}
			else {
				return root.parent.GetDeep() + 1;
			}
		}
		public static VisualElement AddFoldout(this VisualElement root, string text, Action<VisualElement> action) {
			if (text.IsNull()) {
				action?.Invoke(root);
				return root;
			}
			if (FoldoutCache == null) {
				FoldoutCache = PlayerPrefs.GetString(nameof(FoldoutCache), "{}").ParseQData(FoldoutCache);
			}
			var visual = new Foldout();
			root.Add(visual);
			var cacheKey = root.GetDeep() + text;
			visual.text = text;
			visual.value = FoldoutCache.TryGetValue(cacheKey, out var value) ? value : false;
			visual.RegisterValueChangedCallback(e => {
				if (e.target != visual)
					return;
				if (e.newValue) {
					action?.Invoke(visual.contentContainer);
				}
				else {
					visual.contentContainer.Clear();
				}
				if (FoldoutCache[cacheKey] != e.newValue) {
					FoldoutCache[cacheKey] = e.newValue;
					PlayerPrefs.SetString(nameof(FoldoutCache), FoldoutCache.ToQData());
				}
			});
			if (visual.value) {
				action?.Invoke(visual.contentContainer);
			}
			return visual;
		}
		public static ListView AddListView(this VisualElement root, IList itemsSource, Action<VisualElement, int> bindItem = null, Func<VisualElement> makeItem = null) {
			var visual = new ListView();
			visual.itemsSource = itemsSource;
			visual.bindItem = bindItem;
			if (makeItem == null) {
				makeItem = () => new VisualElement();
			}
			visual.makeItem = makeItem;
			root.Add(visual);
			return visual;
		}
#if UNITY_2022_1_OR_NEWER
		public static TreeView AddTreeView(this VisualElement root, Action<VisualElement, int> bindItem, Func<VisualElement> makeItem = null) {
			var visual = new TreeView();
			visual.bindItem = bindItem;
			if (makeItem == null) {
				makeItem = () => new VisualElement();
			}
			visual.makeItem = makeItem;
			root.Add(visual);
			return visual;
		}
#endif
		public static VisualElement Name(this VisualElement visual, string name) {
			visual.name = name;
			return visual;
		}
		public static VisualElement AddVisualElement(this VisualElement root, FlexDirection flexDirection = FlexDirection.Column) {
			var visual = new VisualElement();
			visual.style.flexDirection = flexDirection;
			root.Add(visual);
			return visual;
		}
		public static T IgnoreClick<T>(this T root, bool value = true) where T : VisualElement {
			root.pickingMode = value ? PickingMode.Ignore : PickingMode.Position;
			return root;
		}
		public static VisualElement SetBackground(this VisualElement root, Color color = default, float offset = 0) {
			root.style.position = Position.Absolute;
			root.style.left = offset;
			root.style.top = offset;
			root.style.right = offset;
			root.style.bottom = offset;
			root.style.backgroundColor = color;
			return root;
		}
		public static QConnectElement AddConnect(this VisualElement root, Color color, float lineWidth = 3) {
			var visual = new QConnectElement();
			root.Add(visual);
			visual.StartColor = color;
			visual.EndColor = color;
			visual.LineWidth = lineWidth;
			return visual;
		}
		public static Vector2 GetWorldPosition(this VisualElement root) {
			return root.worldTransform.MultiplyPoint(root.transform.position);
		}
		public static void SetWorldPosition(this VisualElement root, Vector2 worldPosition) {
			root.style.position = Position.Absolute;
			root.transform.position = worldPosition - root.worldBound.position;
		}
		public static ScrollView AddScrollView(this VisualElement root) {
			var visual = new ScrollView();
			root.Add(visual);
			return visual;
		}

#if UNITY_2021_1_OR_NEWER
		public static GroupBox AddGroupBox(this VisualElement root, string name = "") {
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
#if UNITY_EDITOR
		public static void SetCursor(this VisualElement element, MouseCursor cursor) {
			object objCursor = new UnityEngine.UIElements.Cursor();
			PropertyInfo fields = typeof(UnityEngine.UIElements.Cursor).GetProperty("defaultCursorId", BindingFlags.NonPublic | BindingFlags.Instance);
			fields.SetValue(objCursor, (int)cursor);
			element.style.cursor = new StyleCursor((UnityEngine.UIElements.Cursor)objCursor);
		}
#endif
		public static void AddMenu(this VisualElement root, Action<ContextualMenuPopulateEvent> menuBuilder) {
			root.AddManipulator(new ContextualMenuManipulator(e => {
				if (e.target != root)
					return;
				menuBuilder(e);
			}));
		}
		public struct ChangeEvent {
			public VisualElement newView;
			public object obj;
			public ChangeEvent(object obj, VisualElement newView = null) {
				this.obj = obj;
				this.newView = newView;
			}
		}
		public static VisualElement Add(this VisualElement root, string name, object obj, Type type, Action<ChangeEvent> changeEvent, ICustomAttributeProvider customAttribute = null) {
			if (type == null) {
				if (obj == null) {
					return root.AddLabel(name);
				}
				else {
					type = obj.GetType();
				}
			}
			if (obj == null && type.IsValueType) {
				obj = type.CreateInstance();
			}
			var typeInfo = QSerializeType.Get(type);
			if (typeInfo.CustomTypeInfo?.TargetType != null) {
				return root.Add(name, typeInfo.CustomTypeInfo.ChangeType(obj), typeInfo.CustomTypeInfo.TargetType, newValue => {
					changeEvent(new ChangeEvent(typeInfo.CustomTypeInfo.ChangeType(newValue.obj)));
				}, customAttribute);
			}
			switch (typeInfo.Code) {
				case TypeCode.Boolean:
					return root.AddToggle(name, (bool)obj, (value) => changeEvent(new ChangeEvent(value)));
				case TypeCode.Char:
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
					if (type.IsEnum) {
						var flagsEnum = type.GetAttribute<FlagsAttribute>();
						if (flagsEnum != null) {
							return root.AddEnumFlags(name, (Enum)obj, (value) => {
								changeEvent(new ChangeEvent(value));
							});
						}
						else {
							return root.AddEnum(name, (Enum)obj, (value) => {
								changeEvent(new ChangeEvent(value));
							});
						}
					}
					else {
						var intValue = 0;
						try {
							intValue = (int)obj;
						}
						catch (Exception e) {
							Debug.LogWarning("错误[" + name + "][" + obj?.GetType() + "][" + obj + "] " + e);
						}
						return root.AddInt(name, intValue, (value) => { changeEvent(new ChangeEvent(value)); });

					}
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return root.AddInt(name, (int)obj, (value) => { changeEvent(new ChangeEvent(value)); });
				case TypeCode.Single:
					return root.AddFloat(name, (float)obj, (value) => { changeEvent(new ChangeEvent(value)); });
				case TypeCode.Decimal:
				case TypeCode.Double:
					return root.AddDouble(name, (double)obj, (value) => { changeEvent(new ChangeEvent(value)); });
				case TypeCode.String:
					var attribute = customAttribute?.GetAttribute<PropertyAttribute>();
					if (attribute is QObjectAttribute qObject) {
						var visual = QObjectDrawer.CreateGUI(name, obj, type, changeEvent, qObject);
						root.Add(visual);
						return visual;
					}
					else if (attribute is QPopupAttribute qPopup && qPopup != null) {
						return root.AddQPopupAttribute(qPopup, obj, (value) => changeEvent(new ChangeEvent(value)));
					}
					else {
						return root.AddText(name, (string)obj, (value) => { changeEvent(new ChangeEvent(value)); });
					}
				case TypeCode.Object:
					void FreshChild(VisualElement old, object obj) {
						var index = root.IndexOf(old);
						root.RemoveAt(index);
						var newView = root.Add(name, obj, type, changeEvent, customAttribute);
						root.Insert(index, newView);
						changeEvent?.Invoke(new ChangeEvent(obj, newView));
					}
					if (obj == null) {
						var view = root.AddVisualElement(FlexDirection.Row);
						view.AddLabel(name);
						view.AddLabel("null");
						view.AddMenu(menu => {
							menu.menu.AppendAction($"新建/{name} {typeInfo.Type}", e => {
								obj = typeInfo.Type.CreateInstance();
								FreshChild(view, obj);
							});
						});
						return null;
					}
					switch (typeInfo.ObjType) {
						case QObjectType.DynamicObject: {
							var view = root.Add(name, obj, obj.GetType(), changeEvent);
							view.AddMenu(data => {
								var types = typeInfo.Type.GetAllTypes();
								foreach (var type in types) {
									if (type.Is(typeof(UnityEngine.Object)))
										continue;
									data.menu.AppendAction("更改类型/" + type.GetFriendlyName(), data => {
										obj = type.CreateInstance();
										FreshChild(view, obj);
									}, type == obj?.GetType() ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
								}
							});
							return view;
						}
						case QObjectType.UnityObject: {
							return root.AddObject(name, type, (UnityEngine.Object)obj, (value) => { changeEvent(new ChangeEvent(value)); });
						}
						case QObjectType.Object: {
							var foldout = root.AddFoldout(name, view => {
								foreach (var member in typeInfo.Members) {
									view.Add(member.QName, member.Get(obj), member.Type, (value) => {
										member.Set(obj, value.obj);
										changeEvent?.Invoke(new ChangeEvent(obj));
									}, member.MemeberInfo);
								}
							});
							return foldout;
						}
						case QObjectType.Array:
						case QObjectType.List: {
							if (typeof(IList).IsAssignableFrom(type)) {
								if (typeInfo.ArrayRank > 1) {
									break;
								}
								if (obj is IList list) {
									VisualElement foldout = null;
									foldout = root.AddFoldout(name, view => {
										view.Clear();
										for (int i = 0; i < list.Count; i++) {
											object index = i;
											VisualElement child = null;
											child = view.Add($"[{i}] {list[i]}", list[i], typeInfo.ElementType, (value) => {
												if (value.newView != null) {
													child = value.newView;
												}
												list[(int)index] = value.obj;
												if (typeInfo.ElementType != typeof(string)) {
													if (child != null) {
														var label = child.Q<Label>();
														label.text = $"[{i}] {value.obj}";
													}
												}
												changeEvent?.Invoke(new ChangeEvent(list));
											}, customAttribute);
											child.RegisterCallback<ClickEvent>(e => {
												if (e.clickCount == 2) {
													list[(int)index]?.GetType().LocateTypeFile();
												}
											});
											child.AddMenu((menu) => {
												menu.menu.AppendAction($"添加/{name} {index}", action => {
													list = list.CreateAt(typeInfo, (int)index);
													FreshChild(foldout, list);
												});
												menu.menu.AppendAction($"删除/{name} {index}", action => {
													list = list.RemoveAt(typeInfo, (int)index);
													FreshChild(foldout, list);
												});
											});
										}
										view.parent.AddMenu((menu) => {
											menu.menu.AppendAction($"添加/{name}", action => {
												list = list.CreateAt(typeInfo);
												FreshChild(foldout, list);
											});
										});
									});
									return foldout;
								}
							}
						}
						break;
						case QObjectType.Dictionary: {
							if (typeof(IDictionary).IsAssignableFrom(type)) {
								if (obj is IDictionary dic) {
									VisualElement foldout = null;
									foldout = root.AddFoldout(name, view => {
										view.Clear();
										foreach (DictionaryEntry item in dic) {
											view.Add(item.Key?.ToString(), item.Value, typeInfo.ElementType, (value) => {
												dic[item.Key] = value.obj;
												changeEvent?.Invoke(new ChangeEvent(dic));
											}, customAttribute);
										}
									});
									return foldout;
								}
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
		public static bool GetDisplay(this IStyle style) {
			return style.display == DisplayStyle.Flex;
		}
		public static void SetDisplay(this IStyle style, bool display) {
			style.display = display ? DisplayStyle.Flex : DisplayStyle.None;
		}
		public static void SetBorder(this IStyle style, Color color, float width = 1, float radius = 5) {
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
		public static void SetFixedSize(this IStyle style, float width, float height) {
			style.width = width;
			style.height = height;
			style.minWidth = width;
			style.minHeight = height;
			style.maxWidth = width;
			style.maxHeight = height;
		}

		public static VisualElement Tooltip(this VisualElement root, string text) {
			root.tooltip = text?.Replace("\\n", " ");
			return root;
		}
		public static VisualElement AddMarkdown(this VisualElement root, string markdown) {
			root.Clear();
			var newline = false;
			void CheckNewLine() {
				if (!newline) {
					root.AddLabel("");
					newline = true;
				}
			}
			if (markdown.IsNull()) return root;
			foreach (var line in markdown.Split('\n')) {
				line.SplitTowString(" ", out var key, out var text);
				switch (key) {
					case "#":
					case "##":
					case "###":
					case "####":
					case "#####":
					case "######": {
						CheckNewLine();
						var size = Mathf.Lerp(25, 10, (key.Length - 1) / 5f);
						root.AddLabel(text).Tooltip(line).style.fontSize = size;
						newline = false;
						CheckNewLine();
					}
					break;
					case ">": {
						var label = root.AddLabel("||  " + text).Tooltip(line);
						label.style.fontSize = 15;
						label.style.color = Color.HSVToRGB(1, 0, 0.8f);
						newline = false;
					}
					break;
					case "-": {
						root.AddLabel("◆ " + text).Tooltip(line);
						newline = false;
					}
					break;
					default:
						if (line.IsNull() || line.Length >= 3 && line.Replace("*", "").Replace("-", "").Replace("_", "").IsNull()) {
							CheckNewLine();
						}
						else {
							root.AddLabel(line);
							newline = false;
						}
						break;
				}
			}
			return root;
		}


		#region 编辑器拓展
#if UNITY_EDITOR
		[SettingsProvider]
		public static SettingsProvider QToolSetting() {
			return new SettingsProvider("Project/" + nameof(QTool) + "设置", SettingsScope.Project) {
				activateHandler = (searchContext, root) => {
					root = root.AddScrollView();
					foreach (var SettingType in typeof(QSingletonScriptable<>).GetAllTypes()) {
						root.AddLabel(QReflection.QName(SettingType));
						if (SettingType.InvokeFunction("Exists") is bool boolValue && boolValue) {
							root.Add(new InspectorElement(new SerializedObject(SettingType.InvokeFunction("Instance") as ScriptableObject)));
						}
					}
				}
			};
		}

		public static PropertyField Add(this VisualElement root, SerializedProperty serializedProperty) {
			var visual = new PropertyField(serializedProperty);
			visual.name = visual.label;
			root.Add(visual);
			return visual;
		}
		public static void Add<T>(this VisualElement root, SerializedProperty serializedProperty) {
			var type = typeof(T);
			root = root.Add(QReflection.QName(serializedProperty), serializedProperty.stringValue.ParseQDataType(type), type, newObj => {
				serializedProperty.stringValue = QData.ToQDataType(newObj.obj, type);
			});
		}
		public static void Add(this VisualElement root, SerializedObject serializedObject) {
			var iterator = serializedObject.GetIterator();
			if (iterator.NextVisible(true)) {
				do {
					var visual = root.Add(iterator);
					if ("m_Script".Equals(iterator.name)) {
						visual.SetEnabled(false);
					}
					else {
						var att = iterator.GetAttribute<QNameAttribute>();
						if (att != null && !att.visibleControl.IsNull()) {
							var copy = iterator.Copy();
							visual.style.display = copy.IsShow() ? DisplayStyle.Flex : DisplayStyle.None;
							root.RegisterCallback<ClickEvent>(data => {
								visual.style.display = copy.IsShow() ? DisplayStyle.Flex : DisplayStyle.None;
							});
						}
					}

				} while (iterator.NextVisible(false));
			}
		}
		[CustomEditor(typeof(TextAsset))]
		public class MarkdownEditor : Editor {
			public override VisualElement CreateInspectorGUI() {
				var root = new VisualElement();
				root.AddMarkdown(target as TextAsset);
				return root;
			}

		}
		public static string AddMarkdown(this VisualElement root, TextAsset textAsset) {
			var path = UnityEditor.AssetDatabase.GetAssetPath(textAsset);
			var ext = Path.GetExtension(path).ToLower();
			if (".md".Equals(ext) || ".markdown".Equals(ext)) {
				root.AddMarkdown(textAsset.text);
			}
			else {
				root.AddLabel(textAsset.text);
			}
			return path;
		}
#endif
		#endregion
	}
	public class QPopupData {
		static QDictionary<string, QPopupData> DrawerDic = new QDictionary<string, QPopupData>((key) => new QPopupData());

		public static void ClearAll() {
			QDataTable.UnLoadAll();
			DrawerDic.Clear();
		}

		public List<string> List = new List<string>();
		public Type Type { get; private set; }

		public static QPopupData Get(object obj, params string[] getListFuncs) {
			Type type = null;
			if (obj is Type) {
				type = obj as Type;
			}
			else {
				type = obj?.GetType();
			}

			var drawerKey = getListFuncs.ToOneString(" ");
			if (drawerKey.IsNull()) {
#if UNITY_EDITOR
				if (obj is SerializedProperty property) {
					type = QReflection.ParseType(property.type.SplitEndString("PPtr<$").TrimEnd('>'));
				}
#endif
				drawerKey = type?.ToString();
			}
			var drawer = DrawerDic[drawerKey];
			drawer.Type = type;

			if (getListFuncs.Length > 0) {
				drawer.List.Clear();
				drawer.List.Add("\t");
				foreach (var getListFunc in getListFuncs) {
					if (getListFunc.StartsWith(nameof(Resources) + "/")) {
						var assets = Resources.LoadAll(getListFunc.SplitEndString(nameof(Resources) + "/"));
						foreach (var asset in assets) {
							drawer.List.Add(asset.name);
						}
					}
					else {
						var funcKey = getListFunc;
						if (obj.InvokeFunction(funcKey) is IEnumerable itemList) {
							foreach (var item in itemList) {
								drawer.List.Add(item.QName());
							}
						}
					}
				}
			}
			else if (drawer.List.Count == 0) {
				if (type.IsAbstract) {
					drawer.List.Add("\t");
					foreach (var childType in type.GetAllTypes()) {
						drawer.List.Add(childType.Name);
					}
				}
				else if (type.IsEnum) {
					foreach (var name in Enum.GetNames(type)) {
						drawer.List.Add(name);
					}
				}
#if UNITY_EDITOR
				else if (obj is UnityEditor.SerializedProperty property) {
					drawer.List.Clear();
					switch (property.propertyType) {
						case UnityEditor.SerializedPropertyType.Enum: {
							foreach (var item in property.enumNames) {
								drawer.List.Add(item);
							}
						}
						break;
						default:
							break;
					}
				}
#endif
				else {
					Debug.LogError("错误函数[" + getListFuncs.ToOneString(" ") + "]");
				}
			}
			return drawer;
		}
	}
}
