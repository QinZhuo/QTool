using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using QTool.Inspector;
using QTool.Reflection;
using System.Reflection;
using QTool.FlowGraph;
namespace QTool
{


	public static class QGUI
	{
		static QGUI()
		{
			DrawOverride[typeof(QFlowGraph)] = (obj, name) =>
			{
				if (obj == null)
				{
					obj = new QFlowGraph { Name = name };
				}
				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.Label(name);
					if (GUILayout.Button("编辑"))
					{
						var graph = obj as QFlowGraph;
						QFlowGraphWindow.Open(graph, () => { graph.SetDirty(); });
					}
					return obj;
				}
			
			};
		}
	
		public static QDictionary<int, Action> OnChangeDelayCall = new QDictionary<int, Action>();
		public static QDictionary<string, bool> FoldoutDic = new QDictionary<string, bool>();
		public static QDictionary<Type, Func<object, string, object>> DrawOverride = new QDictionary<Type, Func<object, string, object>>();
		static Color BackColor = new Color(0, 0, 0, 0.6f);

		public static List<string> TypeMenuList = new List<string>() { typeof(UnityEngine.Object).FullName.Replace('.', '/') };
		public static List<Type> TypeList = new List<Type>() { typeof(UnityEngine.Object) };
		public static object Draw(this object obj, string name, Type type, Action<object> changeValue = null, ICustomAttributeProvider customAttribute = null, Func<int, object, string, Type, object> DrawElement = null, Action<int, int> IndexChange = null, params GUILayoutOption[] layoutOption)
		{
			var hasName = !string.IsNullOrWhiteSpace(name);
			if (type == null)
			{
				EditorGUILayout.LabelField(name, layoutOption);
			}
			if (obj == null && type.IsValueType)
			{
				obj = type.CreateInstance();
			}
			if (DrawOverride.ContainsKey(type))
			{
				return DrawOverride[type].Invoke(obj, name);
			}

			var typeInfo = QSerializeType.Get(type);
			if (type != typeof(object) && !TypeList.Contains(type) && !type.IsGenericType)
			{
				TypeList.Add(type);
				TypeMenuList.AddCheckExist(type.FullName.Replace('.', '/'));
			}
			switch (typeInfo.Code)
			{
				case TypeCode.Boolean:
					obj = EditorGUILayout.Toggle(name, (bool)obj, layoutOption); break;
				case TypeCode.Char:
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
					if (type.IsEnum)
					{
						var flagsEnum = type.GetAttribute<System.FlagsAttribute>();
						if (flagsEnum != null)
						{
							obj = EditorGUILayout.EnumFlagsField(name, (Enum)obj, layoutOption);
						}
						else
						{
							obj = EditorGUILayout.EnumPopup(name, (Enum)obj, layoutOption);
						}
					}
					else
					{
						obj = EditorGUILayout.IntField(name, (int)obj, layoutOption);
					}
					break;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					obj = EditorGUILayout.LongField(name, (long)obj, layoutOption); break;
				case TypeCode.Single:
					obj = EditorGUILayout.FloatField(name, (float)obj, layoutOption); break;
				case TypeCode.Decimal:
				case TypeCode.Double:
					obj = EditorGUILayout.DoubleField(name, (double)obj, layoutOption); break;
				case TypeCode.String:
					var enumView = customAttribute?.GetAttribute<QEnumAttribute>();
					if (enumView != null)
					{
						obj = QEnumDrawer.Draw(obj, enumView); break;
					}
					else if (string.IsNullOrWhiteSpace(name))
					{
						obj = EditorGUILayout.TextArea(obj?.ToString(), layoutOption); break;
					}
					else
					{
						obj = EditorGUILayout.TextField(name, obj?.ToString(), layoutOption); break;
					}
				case TypeCode.Object:
					switch (typeInfo.objType)
					{
						case QObjectType.DynamicObject:
							{
								using (new EditorGUILayout.HorizontalScope(layoutOption))
								{
									if (obj == null)
									{
										obj = "";
									}
									var objType = obj.GetType();
									var oldType = TypeList.IndexOf(objType);
									var newType = EditorGUILayout.Popup(oldType, TypeMenuList.ToArray(), GUILayout.Width(20), GUILayout.Height(20));
									if (newType != oldType)
									{
										objType = TypeList[newType];
										obj = objType.CreateInstance();
									}
									obj = Draw(obj, name, objType);
								}
							}
							break;
						case QObjectType.UnityObject:
							{
								obj = EditorGUILayout.ObjectField(name, (UnityEngine.Object)obj, type, true, layoutOption);
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
									obj = QIdObjectReferenceDrawer.Draw(name, (QIdObject)obj, layoutOption);
								}
								else
								{

									PushBackColor(BackColor);
									using (new EditorGUILayout.VerticalScope(QGUI.BackStyle, layoutOption))
									{
										PopBackColor();
										if (hasName)
										{
											FoldoutDic[name] = EditorGUILayout.Foldout(FoldoutDic[name], name);
										}
										if (!hasName || FoldoutDic[name])
										{
											using (new EditorGUILayout.HorizontalScope(layoutOption))
											{
												if (hasName)
												{
													EditorGUILayout.Space(10);
												}
												using (new EditorGUILayout.VerticalScope())
												{

													foreach (var member in typeInfo.Members)
													{
														try
														{
															if (member.Type.IsValueType)
															{
																member.Set(obj, member.Get(obj).Draw(member.QName, member.Type));
															}
															else
															{
																member.Set(obj, member.Get(obj).Draw(member.QName, member.Type, (value) => member.Set(obj, value)));
															}
														}
														catch (Exception e)
														{
															Debug.LogError("序列化【" + member.Key + "】出错\n" + e);
														}

													}
												}
											}
										}
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
									var color = GUI.backgroundColor;
									GUI.backgroundColor = BackColor;

									using (new EditorGUILayout.VerticalScope(QGUI.BackStyle, layoutOption))
									{

										GUI.backgroundColor = color;
										var canHideChild = DrawElement == null;
										if (hasName)
										{
											if (canHideChild)
											{
												FoldoutDic[name] = EditorGUILayout.Foldout(FoldoutDic[name], name);
											}
											else
											{
												EditorGUILayout.LabelField(name);
											}
										}
										if (!canHideChild || !hasName || FoldoutDic[name])
										{
											using (new EditorGUILayout.HorizontalScope())
											{
												if (hasName)
												{
													EditorGUILayout.Space(10);
												}
												using (new EditorGUILayout.VerticalScope())
												{
													for (int i = 0; i < list.Count; i++)
													{
														using (new EditorGUILayout.VerticalScope(QGUI.BackStyle))
														{
															var key = name + "[" + i + "]";
															if (DrawElement == null)
															{
																list[i] = list[i].Draw(key, typeInfo.ElementType, null, customAttribute);
															}
															else
															{
																list[i] = DrawElement.Invoke(i, list[i], key, typeInfo.ElementType);
															}
															using (new EditorGUILayout.HorizontalScope())
															{
																GUILayout.FlexibleSpace();
																QGUI.PushColor(Color.blue.LerpTo(Color.white, 0.5f));
																if (GUILayout.Button(new GUIContent("", "新增当前数据"), GUILayout.Width(10), GUILayout.Height(10)))
																{
																	obj = list.CreateAt(typeInfo, i);
																	IndexChange?.Invoke(-1, i + 1);
																}
																QGUI.PopColor();
																QGUI.PushColor(Color.red.LerpTo(Color.white, 0.5f));
																if (GUILayout.Button(new GUIContent("", "删除当前数据"), GUILayout.Width(10), GUILayout.Height(10)))
																{
																	obj = list.RemoveAt(typeInfo, i);
																	IndexChange?.Invoke(i, -1);
																}
																QGUI.PopColor();
															}
														}

													}
												}
											}
											if (list.Count == 0)
											{
												if (GUILayout.Button("添加新元素", GUILayout.Height(20)))
												{
													obj = list.CreateAt(typeInfo);
												}
											}

										}
									}
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
					;
					EditorGUILayout.LabelField(name, obj?.ToString(), layoutOption);
					break;
			}
			if (changeValue != null)
			{
				GUILayoutUtility.GetLastRect().MouseMenuClick((menu) =>
				{
					menu.AddItem(new GUIContent("复制" + name), false, () => GUIUtility.systemCopyBuffer = obj.ToQDataType(type));
					if (!string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
					{
						menu.AddItem(new GUIContent("粘贴" + name), false, () => changeValue(GUIUtility.systemCopyBuffer.ParseQDataType(type, true, obj)));
					}

				});
			}

			return obj;
		}
		public static GUIContent ToGUIContent(this object obj)
		{
			if (obj is UnityEngine.Object uObj)
			{
				return new GUIContent(uObj.name, AssetPreview.GetAssetPreview(uObj),uObj.ToString());
			}
			else if (obj is Color color)
			{
				return new GUIContent(ColorUtility.ToHtmlStringRGB(color), ColorTexture[color]);
			}
			else if (obj is Color32 color32)
			{
				return new GUIContent(ColorUtility.ToHtmlStringRGB(color32), ColorTexture[color32]);
			}
			else if (obj is MemberInfo memberInfo)
			{
				return new GUIContent(memberInfo.QName(),memberInfo.Name);
			}
			else if (obj is IKey<string> ikey)
			{
				return new GUIContent(ikey.Key, obj.ToString());
			}
			else
			{
				return new GUIContent(obj?.ToString());
			}
		}
		static QDictionary<Color, Texture> ColorTexture = new QDictionary<Color, Texture>((key) =>
		{
			var tex = new Texture2D(20, 20);
			for (int i = 0; i < tex.width; i++)
			{
				for (int j = 0; j < tex.height; j++)
				{
					tex.SetPixel(i, j, key);
				}
			}
			tex.Apply();
			return tex;
		});
		public static void MouseMenuClick(this Rect rect, System.Action<GenericMenu> action, Action click = null)
		{
			if (EventType.MouseUp.Equals(Event.current.type))
			{
				if (rect.Contains(Event.current.mousePosition))
				{
					switch (Event.current.button)
					{
						case 0:
							{
								if (click != null)
								{
									click.Invoke();
									Event.current.Use();
								}
							}
							break;
						case 1:
							{
								if (action != null)
								{
									var rightMenu = new GenericMenu();
									action.Invoke(rightMenu);
									rightMenu.ShowAsContext();
									Event.current.Use();
								}

							}
							break;
						default:
							break;
					}
				}

			}
		}
		public static void Draw(this SerializedProperty property, Rect rect, GUIContent content = null)
		{
			if (!property.IsShow()) return;
			var QOnChange = property.GetAttribute<QOnChangeAttribute>();
			if (QOnChange != null)
			{
				OnChangeDelayCall[property.serializedObject.targetObject.GetHashCode()]?.Invoke();
				OnChangeDelayCall[property.serializedObject.targetObject.GetHashCode()] = null;
				EditorGUI.BeginChangeCheck(); ;
			}
			var readonlyAtt = property.GetAttribute<QReadonlyAttribute>();
			if (readonlyAtt != null)
			{
				var last = GUI.enabled;
				GUI.enabled = false;
				property.PrivateDraw(rect, content);
				GUI.enabled = last;
			}
			else
			{
				var QToolbar = property.GetAttribute<QToolbarAttribute>();
				if (QToolbar == null)
				{
					property.PrivateDraw(rect, content);
				}
				else
				{
					QToolbarDrawer.Draw(QToolbar, rect, property, content);
				}
			}
			if (QOnChange != null)
			{
				if (EditorGUI.EndChangeCheck())
				{
					OnChangeDelayCall[property.serializedObject.targetObject.GetHashCode()] += () => { property.InvokeFunction(QOnChange.changeCallBack); };
				}
			}
			return;
		}
		private static bool UnityAttributeView(this SerializedProperty property,Rect rect, GUIContent content = null, string parentType = "")
		{
			if (content == null)
			{
				content = new GUIContent(property.QName(parentType));
			}
			switch (property.propertyType)
			{
				case SerializedPropertyType.Integer:
					{
						var range = property.GetAttribute<RangeAttribute>(parentType);
						if (range != null)
						{
							EditorGUI.IntSlider(rect, property, (int)range.min, (int)range.max, content);
							return true;
						}
					}break;
				case SerializedPropertyType.Float:
					{
						var range = property.GetAttribute<RangeAttribute>(parentType);
						if (range != null)
						{
							EditorGUI.Slider(rect, property, range.min, range.max, content);
							return true;
						}
					}break;
				case SerializedPropertyType.Vector2:
					{
						var range = property.GetAttribute<RangeAttribute>(parentType);
						if (range != null)
						{
							var value = property.vector2Value;
							EditorGUI.LabelField(rect.HorizontalRect(0, 0.4f), content);
							value.x = EditorGUI.FloatField(rect.HorizontalRect(0.4f, 0.5f), value.x);
							EditorGUI.MinMaxSlider(rect.HorizontalRect(0.5f, 0.9f), ref value.x, ref value.y, range.min, range.max);
							value.y = EditorGUI.FloatField(rect.HorizontalRect(0.9f, 1), value.y);
							property.vector2Value = value;
							return true;
						}
					}break;
				case SerializedPropertyType.String:
					{
						var textArea = property.GetAttribute<TextAreaAttribute>(parentType);
						if (textArea != null)
						{
							EditorGUI.LabelField(rect.HorizontalRect(0, 0.4f), content);
							property.stringValue=EditorGUI.TextArea(rect.HorizontalRect(0.4f, 1f), property.stringValue);
							return true;
						}
					}
					break;
				default:
					break;
			}
			return false;
		}
		private static bool PrivateDraw(this SerializedProperty property, Rect rect , GUIContent content = null, string parentType = "")
		{
			if (UnityAttributeView(property, rect, content, parentType)) return false;
			var cur = property.Copy();
			if (cur.hasVisibleChildren && !cur.isArray)
			{
				rect = new Rect(rect.position, new Vector2(rect.width, cur.GetHeight(false)));
				var expanded = EditorGUI.PropertyField(rect, cur, new GUIContent(cur.QName(parentType)), false);
				parentType = property.type;
				if (expanded)
				{
					rect = new Rect(rect.x + 20, rect.y, rect.width - 10, rect.height);
					var end = cur.GetEndProperty();
					var visable = cur.NextVisible(true);
					do
					{
						if (SerializedProperty.EqualContents(cur, end)) return expanded;
						rect = new Rect(new Vector2(rect.x, rect.yMax + 2), new Vector2(rect.width, cur.GetHeight(visable)));
						cur.PrivateDraw(rect,null, parentType);
					} while (cur.NextVisible(false));
				}
				return expanded;
			}
			else
			{
				if (content == null)
				{
					content = new GUIContent(cur.QName(parentType));
				}
				return EditorGUI.PropertyField(rect, cur, content,true);
			}
		}
		public static float GetHeight(this SerializedProperty property)
		{
			return GetHeight(property, property.hasChildren);
		}
		public static float GetHeight(this SerializedProperty property, bool containsChild)
		{
			if (!property.IsShow()) return 0;
			return EditorGUI.GetPropertyHeight(property, containsChild && property.isExpanded);
		}
		public static bool Toggle(string label, bool value, params GUILayoutOption[] options)
		{
			PushColor(value ? Color.black : Color.white);
			value = GUILayout.Toggle(value, label, BackStyle, options);
			PopColor();
			return value;
		}
		static QDictionary<string, string[]> yearCache = new QDictionary<string, string[]>();
		static QDictionary<string, string[]> monthCache = new QDictionary<string, string[]>();
		static QDictionary<string, string[]> dayCache = new QDictionary<string, string[]>();
		static List<string> temp = new List<string>();
		public static DateTime DateEnum(DateTime value, DateTime start, DateTime end, params GUILayoutOption[] options)
		{
			var key = start.Year + "_" + start.Month + "_" + start.Day + "_" + end.Year + "_" + end.Month + "_" + end.Day;
			#region InitCache
			if (!yearCache.ContainsKey(key))
			{
				temp.Clear();
				for (int i = start.Year; i <= end.Year; i++)
				{
					temp.Add(i.ToString());
				}
				yearCache.Add(key, temp.ToArray());
			}
			if (!monthCache.ContainsKey(key))
			{
				temp.Clear();
				if (start.Year == end.Year)
				{
					for (int i = start.Month; i <= end.Month; i++)
					{
						temp.Add(i.ToString());
					}
				}
				else
				{
					for (int i = 0; i <= 12; i++)
					{
						temp.Add(i.ToString());
					}
				}
				monthCache.Add(key, temp.ToArray());
			}
			if (!dayCache.ContainsKey(key))
			{
				temp.Clear();
				if (start.Year == end.Year && start.Month == end.Month)
				{
					for (int i = start.Day; i <= end.Day; i++)
					{
						temp.Add(i.ToString());
					}
				}
				else
				{
					for (int i = 0; i <= 31; i++)
					{
						temp.Add(i.ToString());
					}
				}
				dayCache.Add(key, temp.ToArray());
			}
			#endregion
			EditorGUILayout.Popup(value.Year - start.Year, yearCache[key], GUILayout.Width(50));
			EditorGUILayout.Popup(start.Year == end.Year ? (value.Month - start.Month) : value.Month, monthCache[key], GUILayout.Width(40));
			EditorGUILayout.Popup(start.Year == end.Year && start.Month == end.Month ? (value.Day - start.Day) : value.Day, dayCache[key], GUILayout.Width(40));
			return value;
		}
		static Stack<Color> colorStack = new Stack<Color>();
		public static void PushColor(Color newColor)
		{
			colorStack.Push(GUI.color);
			GUI.color = newColor;
		}
		public static void PopColor()
		{
			GUI.color = colorStack.Pop();
		}
		static Stack<Color> backColorStack = new Stack<Color>();
		public static void PushBackColor(Color newColor)
		{
			backColorStack.Push(GUI.color);
			GUI.backgroundColor = newColor;
		}
		public static void PopBackColor()
		{
			GUI.backgroundColor = backColorStack.Pop();
		}
		static Stack<Color> contentColorStack = new Stack<Color>();
		public static void PushContentColor(Color newColor)
		{
			contentColorStack.Push(GUI.color);
			GUI.contentColor = newColor;
		}
		public static void PopContentColor()
		{
			GUI.contentColor = contentColorStack.Pop();
		}
		public static void ProgressBar(string info, float progress, Color color)
		{
			GUILayout.Box("", BackStyle);
			var lastRect = GUILayoutUtility.GetLastRect();
			var rateRect = lastRect;
			progress = Mathf.Clamp(progress, 0.01f, 1);
			rateRect.width *= progress;
			if (progress > 0)
			{
				PushColor(color);
				GUI.Box(rateRect, "", CellStyle);
				PopColor();
			}
			GUI.Label(lastRect, info, CenterLable);
		}
		public static GUIStyle TitleLable => _titleLabel ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
		static GUIStyle _titleLabel;
		public static GUIStyle CenterLable => _centerLable ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, richText = true };
		static GUIStyle _centerLable;
		public static GUIStyle TextArea => _textArea ??= new GUIStyle(EditorStyles.textField) { alignment = TextAnchor.MiddleCenter };
		static GUIStyle _textArea;
		public static GUIStyle RichLable => _richLable ??= new GUIStyle(EditorStyles.label) { richText = true };
		static GUIStyle _richLable;
		public static GUIStyle LeftLable => _leftLable ??= new GUIStyle(EditorStyles.label) { richText = true };
		static GUIStyle _leftLable;
		public static GUIStyle RightLabel => _rightLabel ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
		static GUIStyle _rightLabel;
		public static Texture2D NodeEditorBackTexture2D => _nodeEditorBackTexture2D ??= Resources.Load<Texture2D>("NodeEditorBackground");
		static Texture2D _nodeEditorBackTexture2D = null;
		public static Texture2D DotTexture2D => _dotTextrue2D ??= Resources.Load<Texture2D>("NodeEditorDot");
		static Texture2D _dotTextrue2D;
		public static GUIStyle BackStyle => _backStyle ??= new GUIStyle("helpBox") { alignment = TextAnchor.MiddleCenter };
		static GUIStyle _backStyle;

		public static GUIStyle CellStyle => _cellStyle ??= new GUIStyle("GroupBox") { alignment = TextAnchor.MiddleCenter };
		static GUIStyle _cellStyle;
	}
	public static class QEditorTool
	{
		public static object GetPathObject(this object target, string path)
		{
			if (path.SplitTowString(".", out var start, out var end))
			{
				try
				{
					if (start == "Array" && end.StartsWith("data"))
					{
						var list = target as IList;
						if (list == null)
						{
							return null;
						}
						else
						{
							return list[int.Parse(end.GetBlockValue('[', ']'))];
						}
					}
					else
					{

						return target.GetPathObject(start).GetPathObject(end);
					}

				}
				catch (Exception e)
				{
					throw new Exception("路径出错：" + path, e);
				}
			}
			else
			{
				var memebers = QReflectionType.Get(target.GetType()).Members;
				if (memebers.ContainsKey(path))
				{
					var Get = memebers[path].Get;
					return Get(target);
				}
				else
				{
					throw new Exception(" 找不到 key " + path);
				}
			}
		}
		public static bool GetPathBool(this object target, string key)
		{
			var not = key.Contains("!");
			if (key.SplitTowString("==", out var start, out var value) || key.SplitTowString("!=", out start, out value))
			{
				not = false;
				var info = target.GetPathObject(start)?.ToString() == value;
				return not ? !(bool)info : (bool)info;
			}
			else
			{
				if (not)
				{
					key = key.TrimStart('!');
				}
				object info = null;
				switch (key)
				{
					case nameof(Application.isPlaying):
						info = Application.isPlaying;
						break;
					default:
						info= target.GetPathObject(key);
						break;
				}
				if (info == null)
				{
					return !not;
				}
				else
				{
					return not ? !(bool)info : (bool)info;
				}

			}

		}
		public static Rect HorizontalRect(this Rect rect, float left, float right)
		{
			var leftOffset = left * rect.width;
			var width = (right - left) * rect.width;
			rect.x += leftOffset;
			rect.width = width;
			return rect;
		}

		public static object[] GetAttributes<T>(this SerializedProperty prop, string parentKey)
		{
			var type = string.IsNullOrWhiteSpace(parentKey) ? prop.serializedObject.targetObject?.GetType() : QReflection.ParseType(parentKey);
			var field = GetChildObject(type, prop.name);
			if (field != null)
			{
				return field.GetCustomAttributes(typeof(T), true);
			}
			return new object[0];
		}
		public static FieldInfo GetChildObject(Type type, string key)
		{
			if (type == null || string.IsNullOrWhiteSpace(key)) return null;
			const BindingFlags bindingFlags = System.Reflection.BindingFlags.GetField
											  | System.Reflection.BindingFlags.GetProperty
											  | System.Reflection.BindingFlags.Instance
											  | System.Reflection.BindingFlags.NonPublic
											  | System.Reflection.BindingFlags.Public;
			return type.GetField(key, bindingFlags);
		}
		public static T GetAttribute<T>(this SerializedProperty property, string parentKey = "") where T : Attribute
		{
			object[] attributes = GetAttributes<T>(property, parentKey);
			if (attributes.Length > 0)
			{
				return attributes[0] as T;
			}
			else
			{
				return null;
			}
		}

		public static string QName(this SerializedProperty property, string parentName = "")
		{
			var att = property.GetAttribute<QNameAttribute>(parentName);
			if (att != null && !string.IsNullOrWhiteSpace(att.name))
			{
				return att.name;
			}
			else
			{
				return property.displayName;
			}
		}


		public static object GetObject(this SerializedProperty property)
		{
			return property?.serializedObject.targetObject.GetPathObject(property.propertyPath);
		}


		public static Bounds GetBounds(this GameObject obj)
		{
			var bounds = new Bounds(obj.transform.position, Vector3.zero);
			Renderer[] meshs = obj.GetComponentsInChildren<Renderer>();
			foreach (var mesh in meshs)
			{
				if (mesh)
				{
					if (bounds.extents == Vector3.zero)
					{
						bounds = mesh.bounds;
					}
					else
					{
						bounds.Encapsulate(mesh.bounds);
					}
				}
			}
			return bounds;
		}

		public static bool Active(this QNameAttribute att, object target)
		{
			if (att.visibleControl.IsNull())
			{
				return true;
			}
			else
			{
				return (bool)target.GetPathBool(att.visibleControl);
			}
		}
		public static bool IsShow(this SerializedProperty property)
		{
			var att = property.GetAttribute<QNameAttribute>();
			if (att == null)
			{
				return true;
			}
			else
			{
				return att.Active(property.serializedObject.targetObject);
			}
		}
		public static void AddObject(this List<GUIContent> list, object obj)
		{
			if (obj != null)
			{
				if (obj is UnityEngine.GameObject)
				{
					var uObj = obj as UnityEngine.GameObject;
					var texture = AssetPreview.GetAssetPreview(uObj);
					list.Add(new GUIContent(uObj.name, texture, uObj.name));
				}
				else
				{
					list.Add(new GUIContent(obj.ToString()));
				}
			}
			else
			{
				list.Add(new GUIContent("空"));
			}
		}
	}

	public class QStylesWindows : EditorWindow
	{

		private Vector2 scrollVector2 = Vector2.zero;
		private string search = "";

		[MenuItem("QTool/工具/GUIStyle查看器")]
		public static void InitWindow()
		{
			EditorWindow.GetWindow(typeof(QStylesWindows));
		}

		void OnGUI()
		{

			GUILayout.BeginHorizontal("HelpBox");
			GUILayout.Space(30);
			search = EditorGUILayout.TextField("", search, "SearchTextField", GUILayout.MaxWidth(position.x / 3));
			GUILayout.Label("", "SearchCancelButtonEmpty");
			GUILayout.EndHorizontal();
			scrollVector2 = GUILayout.BeginScrollView(scrollVector2);
			foreach (GUIStyle style in GUI.skin.customStyles)
			{
				if (style.name.ToLower().Contains(search.ToLower()))
				{
					DrawStyleItem(style);
				}
			}
			GUILayout.EndScrollView();
		}

		void DrawStyleItem(GUIStyle style)
		{
			GUILayout.BeginHorizontal("box");
			GUILayout.Space(40);
			EditorGUILayout.SelectableLabel(style.name);
			GUILayout.Space(40);
			GUILayout.FlexibleSpace();
			EditorGUILayout.SelectableLabel(style.name, style);
			GUILayout.Space(40);
			EditorGUILayout.SelectableLabel("", style, GUILayout.Height(40), GUILayout.Width(40));
			GUILayout.Space(50);
			if (GUILayout.Button("复制GUIStyle名字"))
			{
				TextEditor textEditor = new TextEditor();
				textEditor.text = style.name;
				textEditor.OnFocus();
				textEditor.Copy();
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
		}
	}
}
