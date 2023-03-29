using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Inspector;
using QTool.Reflection;
using System.Reflection;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{

	public static class QGUI
	{
		public static Color SelectColor { get; private set; } = new Color32(15, 129, 190, 100);
		public static Color BackColor { get; private set; } = new Color32(45, 45, 45, 255);
		public static Color AlphaBackColor { get; private set; } = new Color32(0, 0, 0, 40);
		public static Color ContentColor { get; private set; } = new Color32(225, 225, 225, 255);
		public static Color ButtonColor { get; private set; } = new Color32(70, 70, 70, 255);
		public static GUISkin Skin => _Skin ??= new GUISkin()
		{
			box=BackStyle,
			font=GUI.skin.font,
			window=BackStyle,
			label=LabelStyle,
			textField=TextFieldStyle,
			textArea=TextFieldStyle,
			button=ButtonStyle,
			scrollView=BackStyle,
			toggle=ToggleStyle,
			verticalScrollbar=ScrollbarStyle,
			verticalScrollbarThumb = ScrollbarStyle,
			horizontalScrollbar = ScrollbarStyle,
			horizontalScrollbarThumb = ScrollbarStyle,
		};
		private static GUISkin _Skin = null;
		private static GUIStyle _ButtonStyle;
		public static GUIStyle ButtonStyle => _ButtonStyle ??= new GUIStyle()
		{
			alignment = TextAnchor.MiddleCenter,
			normal = new GUIStyleState
			{
				background = GetBackTexture(ButtonColor, RectRaudius),
				textColor = ContentColor,
			},
			hover = new GUIStyleState
			{
				background = GetBackTexture(new Color32(90, 90, 90, 255), RectRaudius),
				textColor = ContentColor,
			},
			active = new GUIStyleState
			{
				background = GetBackTexture(new Color32(55, 55, 55, 255), RectRaudius),
				textColor = ContentColor,
			},
			onNormal = new GUIStyleState
			{
				background = GetBackTexture(new Color32(80, 80, 80, 255), RectRaudius),
				textColor = ContentColor,
			},
			border = new RectOffset { bottom = RectRaudius, left = RectRaudius, right = RectRaudius, top = RectRaudius },
		}; 
		public static GUIStyle ScrollbarStyle => _ScrollbarStyle ??= new GUIStyle()
		{
			fixedWidth=15,
			alignment = TextAnchor.MiddleCenter,
			normal = new GUIStyleState
			{
				background = GetBackTexture(ButtonColor, RectRaudius),
				textColor = ContentColor,
			},
			border = new RectOffset { bottom = RectRaudius, left = RectRaudius, right = RectRaudius, top = RectRaudius },
		};

		private static GUIStyle _ScrollbarStyle;
		public static GUIStyle ToggleStyle => _ToggleStyle ??= new GUIStyle()
		{
			alignment = TextAnchor.MiddleCenter,
			normal = new GUIStyleState
			{
				background = GetTexture(34).DrawCircle(ButtonColor, 20).DrawCircle(Color.black, 15).DrawEnd(),
				textColor = ContentColor,
			},
			onNormal = new GUIStyleState
			{
				background = GetTexture(34).DrawCircle(ButtonColor, 20).DrawCircle(Color.black, 15).DrawCircle(ButtonColor, 10).DrawEnd(),
				textColor = ContentColor,
			},
		};

		private static GUIStyle _ToggleStyle;
		public static GUIStyle FoldoutStyle => _FoldoutStyle ??= new GUIStyle()
		{
			alignment = TextAnchor.MiddleCenter,
			normal = new GUIStyleState
			{
				background =GetTexture(32).DrawTriangle(ButtonColor,14).DrawEnd(),
				textColor = ContentColor,
			},
			onNormal = new GUIStyleState
			{
				background = GetTexture(32).DrawTriangle(ButtonColor,14,false).DrawEnd(),
				textColor = ContentColor,
			},
		};

		private static GUIStyle _FoldoutStyle;
		public static GUIStyle LabelStyle => _LabelStyle ??= new GUIStyle()
		{
			normal = new GUIStyleState
			{
				textColor = ContentColor,
			},
			border = new RectOffset(RectRaudius, RectRaudius, RectRaudius, RectRaudius),
			padding = new RectOffset ( RectRaudius, RectRaudius, RectRaudius, RectRaudius),
			alignment = TextAnchor.MiddleLeft,
		};

		private static GUIStyle _LabelStyle;
		public static GUIStyle SelectStyle => _SelectStyle ??= new GUIStyle()
		{
			normal = new GUIStyleState
			{
				background=GetBackTexture(SelectColor, RectRaudius),
				textColor = ContentColor,
			},
			border = new RectOffset(RectRaudius, RectRaudius, RectRaudius, RectRaudius),
		};

		private static GUIStyle _SelectStyle;
		public static GUIStyle TextFieldStyle => _TextFieldStyle ??= new GUIStyle()
		{
			normal = new GUIStyleState
			{
				background = GetBackTexture(AlphaBackColor, RectRaudius),
				textColor = ContentColor,
			},
			
			border = new RectOffset(RectRaudius, RectRaudius, RectRaudius, RectRaudius),
			padding = new RectOffset(RectRaudius, RectRaudius, RectRaudius, RectRaudius),
			alignment = TextAnchor.MiddleLeft,
		};

		private static GUIStyle _TextFieldStyle;
		public const int RectRaudius = 4;
		
		public static GUIStyle BackStyle => _BackStyle ??= new GUIStyle()
		{
			alignment = TextAnchor.MiddleCenter,
			normal = new GUIStyleState
			{
				background = GetBackTexture(BackColor, RectRaudius),
				textColor = ContentColor,
			},
			border = new RectOffset(RectRaudius, RectRaudius, RectRaudius, RectRaudius),
			overflow = new RectOffset(2, 2, 2, 2),
		};
		private static GUIStyle _BackStyle;
		public static GUIStyle AlphaBackStyle => _AlphaBackStyle ??= new GUIStyle(BackStyle)
		{
			normal = new GUIStyleState
			{
				background = GetBackTexture(AlphaBackColor, RectRaudius),
				textColor = ContentColor,
			},
		}; 
		private static GUIStyle _AlphaBackStyle;
		public static GUIStyle TitleStyle => _TitleStyle ??= new GUIStyle(BackStyle)
		{
			normal = new GUIStyleState
			{
				background = GetBackTexture(new Color32(20,20,20,100), RectRaudius),
				textColor = ContentColor,
			},
		};
		static GUIStyle _TitleStyle;
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
		static QDictionary<QToolBar, Vector2> ToolBarScroll = new QDictionary<QToolBar, Vector2>(); 
		public static object Draw(this QToolBar toolBar)
		{
			if (toolBar == null) return default;
			if (!toolBar.IsButton)
			{
				if (toolBar.Selected)
				{
					var value= toolBar.ChildToolBar.Draw();
					if (Equals(toolBar.ChildToolBar?.ChildToolBar?.Value, "返回"))
					{
						toolBar.CancelSelect();
					}
					return value;
				}
				else
				{
					using (var scroll=new GUILayout.ScrollViewScope(ToolBarScroll[toolBar], GUILayout.Height(Height)))
					{
						toolBar.Select = GUILayout.Toolbar(toolBar.Select, toolBar.Values.ToArray(), ButtonStyle, GUILayout.Width(toolBar.Width * toolBar.Values.Count), GUILayout.Height(Height));
						ToolBarScroll[toolBar] = scroll.scrollPosition;
					}
				}
			}
			return toolBar.Value;
		}


		public static QDictionary<Color, Texture2D> ColorTexture { get; private set; } = new QDictionary<Color, Texture2D>((key) =>
		{
			var tex = new Texture2D(1, 1);
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
		public static List<Texture2D> Texture2DCache = new List<Texture2D>();
		public static Texture2D GetTexture(int size = 8)
		{
			var tex = new Texture2D(size * 2 + 1, size * 2 + 1);
			for (int i = 0; i < tex.width; i++)
			{
				for (int j = 0; j < tex.height; j++)
				{
					tex.SetPixel(i, j, Color.clear);
				}
			}
			tex.Apply();
			Texture2DCache.Add(tex);
			return tex;
		}
		public static Texture2D GetBackTexture(Color color, int radius = 8, int line = 1)
		{
			return GetTexture(radius).DrawCircle(Color.Lerp(color, Color.black, 0.5f), radius).DrawCircle(color, radius - 1).DrawEnd();
		}
		public static Texture2D DrawEnd(this Texture2D tex)
		{
			tex.Apply();
			return tex;
		}
		public static Texture2D DrawTriangle(this Texture2D tex, Color color, int radius = 8,bool down=true)
		{
			for (int j = 0; j < tex.height; j++)
			{
				for (int i = 0; i < tex.width; i++)
				{
					var x = i - tex.width / 2+radius;
					var y = j - tex.height / 2+radius;
					if (down)
					{
						if (x >= radius - y / Mathf.Sqrt(3) &&
						x <= radius + y / Mathf.Sqrt(3) &&
						y <= radius * Mathf.Sqrt(3))
						{
							tex.SetPixel(i, j, color);
						}
					}
					else
					{
						if (y >= radius - x / Mathf.Sqrt(3) &&
						   y <= radius + x / Mathf.Sqrt(3) &&
						   x <= radius * Mathf.Sqrt(3))
						{
							tex.SetPixel(i, j, color);
						}
					}
					
				}
			}
			return tex;
		}
		public static Texture2D DrawCircle(this Texture2D tex, Color color, int radius = 8)
		{
			for (int i = 0; i < tex.width; i++)
			{
				for (int j = 0; j < tex.height; j++)
				{
					var x = i - tex.width / 2;
					var y = j - tex.height / 2;
					if (x * x + y * y < radius * radius)
					{
						tex.SetPixel(i, j, color);
					}
				}
			}
			tex.Apply();
			return tex;
		}
		public const float Size = 10;
		public const float Height = Size *3f;
		public static GUILayoutOption HeightLayout { get; private set; } = GUILayout.Height(Height);
		public static QDictionary<int, Action> OnChangeDelayCall = new QDictionary<int, Action>();
		public static QDictionary<Type, Func<object, string, object>> DrawOverride = new QDictionary<Type, Func<object, string, object>>();
		public static List<string> TypeMenuList = new List<string>() { typeof(UnityEngine.Object).FullName.Replace('.', '/') };
		public static List<Type> TypeList = new List<Type>() { typeof(UnityEngine.Object) };

		private static bool IsRuntimeDraw { get; set; }
		public static void BeginRuntimeGUI()
		{
			IsRuntimeDraw = true;
			GUI.skin = Skin;
		}
		public static void EndRuntimeGUI()
		{
			IsRuntimeDraw = false;
			if (GUI.skin == Skin)
			{
				GUI.skin = null;
			}
		}
		public static async Task WaitLayout()
		{
			await QTask.Wait(() => Event.current?.type != EventType.Layout);
		}
		public static bool Button(string text, float width = -1)
		{
			if (width > 0)
			{
				return GUILayout.Button(text, ButtonStyle,HeightLayout, GUILayout.Width(width));
			}
			else
			{

				return GUILayout.Button(text, ButtonStyle, HeightLayout);
			}
		}
		public static void Title(string value)
		{
			GUILayout.Label(value, TitleStyle, GUILayout.Height(Height/2));
		}
		public static void Label(string value)
		{
			GUILayout.Label(value, LabelStyle, HeightLayout);
		}
		public static void LabelField(string name, string value)
		{
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				if (value.IsNull())
				{
					EditorGUILayout.LabelField(name);
				}
				else
				{
					EditorGUILayout.LabelField(name, value);
				}
			}
			else
#endif
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(name,LabelStyle, HeightLayout);
				GUILayout.Label(value,LabelStyle, HeightLayout);
				GUILayout.EndHorizontal();
			}
		}
		public static string TextField(string lable, string text)
		{
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				if (lable.IsNull())
				{
					return EditorGUILayout.TextArea(text);
				}
				else
				{
					return EditorGUILayout.TextField(lable, text);
				}
			}
			else
#endif
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(lable, HeightLayout);
				text = TextField(text);
				GUILayout.EndHorizontal();
				return text;
			}
		}
		public static string TextField(string text)
		{
			return GUILayout.TextField(text, TextFieldStyle, HeightLayout, GUILayout.MinWidth(Height*3));
		}
		public static int IntField(string lable, int value)
		{
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				if (lable.IsNull())
				{
					return EditorGUILayout.IntField(value);
				}
				else
				{
					return EditorGUILayout.IntField(lable, value);
				}
			}
			else
#endif
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(lable, HeightLayout);
				var str = TextField(value.ToString());
				GUILayout.EndHorizontal();
				if (int.TryParse(str, out var newInt))
				{
					return newInt;
				}
				else
				{
					return value;
				}
			}
		}
		public static int Popup(int select,string[] values)
		{
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				return EditorGUILayout.Popup(select, values);
			}
			else
#endif
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(values?.Get(select)?.ToString(), LabelStyle, HeightLayout);
				GUILayout.EndHorizontal();
				return select;
			}
		}
		public static int Popup(int select, GUIContent[] values)
		{
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				return EditorGUILayout.Popup(select, values);
			}
			else
#endif
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(values.Get(select)?.ToString(), LabelStyle, HeightLayout);
				GUILayout.EndHorizontal();
				return select;
			}
		}
		public static Enum EnumField(string lable, Enum value)
		{
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				if (lable.IsNull())
				{
					return EditorGUILayout.EnumPopup(value);
				}
				else
				{
					return EditorGUILayout.EnumPopup(lable, value);
				}
			}
			else
#endif
			{
				//var dataList= QEnumListData.Get(value,"");
				GUILayout.BeginHorizontal();
				// GUILayout.SelectionGrid(dataList.SelectIndex, dataList.List.ToArray(), 1);
				GUILayout.Label(lable, LabelStyle, HeightLayout);
				GUILayout.Label(value.ToString(), LabelStyle, HeightLayout);
				GUILayout.EndHorizontal();
				return value;
			}
		}
		public static Enum EnumFlagsField(string lable, Enum value)
		{
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				if (lable.IsNull())
				{
					return EditorGUILayout.EnumFlagsField(value);
				}
				else
				{
					return EditorGUILayout.EnumFlagsField(lable, value);
				}
			}
			else
#endif
			{
				//var dataList= QEnumListData.Get(value,"");
				GUILayout.BeginHorizontal();
				// GUILayout.SelectionGrid(dataList.SelectIndex, dataList.List.ToArray(), 1);
				GUILayout.Label(lable, LabelStyle, HeightLayout);
				GUILayout.Label(value.ToString(), LabelStyle, HeightLayout);
				GUILayout.EndHorizontal();
				return value;
			}
		}
		public static long LongField(string lable, long value)
		{
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				if (lable.IsNull())
				{
					return EditorGUILayout.LongField(value);
				}
				else
				{
					return EditorGUILayout.LongField(lable, value);
				}
			}
			else
#endif
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(lable, LabelStyle, HeightLayout);
				var str = TextField(value.ToString());
				GUILayout.EndHorizontal();
				if (long.TryParse(str, out var newInt))
				{
					return newInt;
				}
				else
				{
					return value;
				}
			}
		}
		public static float FloatField(string lable, float value)
		{
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				if (lable.IsNull())
				{
					return EditorGUILayout.FloatField(value);
				}
				else
				{
					return EditorGUILayout.FloatField(lable, value);
				}
			}
			else
#endif
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(lable, HeightLayout);
				var str = TextField(value.ToString());
				GUILayout.EndHorizontal();
				if (float.TryParse(str, out var newInt))
				{
					return newInt;
				}
				else
				{
					return value;
				}
			}
		}
		public static double DoubleField(string lable, double value)
		{
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				if (lable.IsNull())
				{
					return EditorGUILayout.DoubleField(value);
				}
				else
				{
					return EditorGUILayout.DoubleField(lable, value);
				}
			}
			else
#endif
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(lable, LabelStyle, HeightLayout);
				var str = TextField(value.ToString());
				GUILayout.EndHorizontal();
				if (double.TryParse(str, out var newInt))
				{
					return newInt;
				}
				else
				{
					return value;
				}
			}
		}
		public static UnityEngine.Object ObjectField(string lable, UnityEngine.Object value, Type type)
		{
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				if (lable.IsNull())
				{
					return EditorGUILayout.ObjectField(value, type, true);
				}
				else
				{
					return EditorGUILayout.ObjectField(lable, value, type, true);
				}
			}
			else
#endif
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(lable, HeightLayout);
				GUILayout.Label(value.ToString(), HeightLayout);
				GUILayout.EndHorizontal();
				return value;
			}
		}
		public static bool Toggle(string lable, bool value)
		{
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				if (lable.IsNull())
				{
					return EditorGUILayout.Toggle(value);
				}
				else
				{
					return EditorGUILayout.Toggle(lable, value);
				}
			}
			else
#endif
			{
				if (!lable.IsNull())
				{
					GUILayout.BeginHorizontal();
				}
				value = GUILayout.Toggle(value, "", ToggleStyle, HeightLayout, GUILayout.Width(Height));
				if (!lable.IsNull())
				{
					GUILayout.Label(lable, LabelStyle, HeightLayout);
					GUILayout.EndHorizontal();
				}
				return value;
			}
		}
		private static QDictionary<int, bool> FoldoutCache = new QDictionary<int, bool>();
		/// <summary>
		/// 折叠按钮
		/// </summary>
		public static bool Foldout(string key,int hashCode=default)
		{
			if (hashCode.IsNull())
			{
				hashCode = key.GetHashCode();
			}
#if UNITY_EDITOR
			if (!IsRuntimeDraw)
			{
				FoldoutCache[hashCode] = EditorGUILayout.Foldout(FoldoutCache[hashCode], key);
			}
			else
#endif
			{
				if (!key.IsNull())
				{
					GUILayout.BeginHorizontal();
				}
				FoldoutCache[hashCode] = GUILayout.Toggle(FoldoutCache[hashCode], "", FoldoutStyle, HeightLayout,GUILayout.Width(Height));
				if (!key.IsNull())
				{
					GUILayout.Label(key, LabelStyle, HeightLayout);
					GUILayout.EndHorizontal();
				}
			}
			return FoldoutCache[hashCode];
		}
		public static string DrawQIdObject(string lable, string id, Type type, Rect? rect = null, params GUILayoutOption[] options)
		{
			var name = lable + "【" + (id == null ? "" : id.Substring(0, Mathf.Min(4, id.Length))) + "~】";
			var oldObj = QIdObject.GetObject(id, type);
			var newObj = oldObj;
			if (rect == null)
			{
				newObj = ObjectField(name, oldObj, type);
			}
#if UNITY_EDITOR
			else
			{
				newObj = EditorGUI.ObjectField(rect.Value, name, oldObj, type, true);
			}
#endif
			if (newObj != oldObj)
			{
				id = QIdObject.GetId(newObj);
			}
			return id;
		}
		public static QIdObject Draw(this QIdObject ir, string lable, params GUILayoutOption[] options)
		{
			var newId = DrawQIdObject(lable, ir.id, typeof(UnityEngine.Object), null, options);
			if (newId != ir.id)
			{
				ir.id = newId;
			}
			return ir;
		}
		public static object Draw(this QEnumAttribute att, object obj)
		{
			var str = obj.ToGUIContent().text;
			using (new GUILayout.HorizontalScope())
			{
				var data = QEnumListData.Get(obj?.GetType(), att.funcKey);
				data.UpdateList(str);
				if (data.SelectIndex < 0)
				{
					data.SelectIndex = 0;
					str = data.SelectValue;
				}
				var newIndex = Popup(data.SelectIndex, data.List.ToArray());
				if (newIndex != data.SelectIndex)
				{
					data.SelectIndex = newIndex;
					if (data.SelectIndex >= 0)
					{
						str = data.SelectValue;
					}
				}
			}
			return str;
		}
		public static object Draw(this object obj, string name, Type type = null, ICustomAttributeProvider customAttribute = null, Func<int, object, string, Type, object> DrawElement = null, Action<int, int> IndexChange = null)
		{
			var hasName = !string.IsNullOrWhiteSpace(name);
			if (type == null)
			{
				if (obj == null)
				{
					Label(name);
					return obj;
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
					obj = Toggle(name, (bool)obj); break;
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
							obj = EnumFlagsField(name, (Enum)obj);
						}
						else
						{
							obj = EnumField(name, (Enum)obj);
						}
					}
					else
					{
						obj = IntField(name, (int)obj);
					}
					break;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					obj = LongField(name, (long)obj); break;
				case TypeCode.Single:
					obj = FloatField(name, (float)obj); break;
				case TypeCode.Decimal:
				case TypeCode.Double:
					obj = DoubleField(name, (double)obj); break;
				case TypeCode.String:
					var enumView = customAttribute?.GetAttribute<QEnumAttribute>();
					if (enumView != null)
					{
						obj = enumView.Draw(obj); break;
					}
					else
					{
						obj = TextField(name, obj?.ToString()); break;
					}
				case TypeCode.Object:
					switch (typeInfo.objType)
					{
						case QObjectType.DynamicObject:
							{
								using (new GUILayout.HorizontalScope())
								{
									if (obj == null)
									{
										obj = "";
									}
									var objType = obj.GetType();
									var oldType = TypeList.IndexOf(objType);
									var newType =Popup(oldType, TypeMenuList.ToArray());
									if (newType != oldType)
									{
										objType = TypeList[newType];
										obj = objType.CreateInstance();
									}
									if (objType != type)
									{
										obj = Draw(obj, name, objType);
									}
								}
							}
							break;
						case QObjectType.UnityObject:
							{
								obj = ObjectField(name, (UnityEngine.Object)obj, type);
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

									PushBackColor(BackColor);
									using (new GUILayout.VerticalScope(QGUI.AlphaBackStyle))
									{
										PopBackColor();
										var show = false;
										if (hasName)
										{
											show = Foldout(name);
										}
										if (!hasName || show)
										{
											using (new GUILayout.HorizontalScope())
											{
												if (hasName)
												{
													GUILayout.Space(10);
												}
												using (new GUILayout.VerticalScope())
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
																member.Set(obj, member.Get(obj).Draw(member.QName, member.Type));
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

									using (new GUILayout.VerticalScope(AlphaBackStyle))
									{

										GUI.backgroundColor = color;
										var canHideChild = DrawElement == null;
										if (hasName)
										{
											if (canHideChild)
											{
												canHideChild = !Foldout(name);
											}
											else
											{
												Label(name);
											}
										}
										if (!canHideChild || !hasName)
										{
											using (new GUILayout.HorizontalScope())
											{
												if (hasName)
												{
													GUILayout.Space(Size);
												}
												using (new GUILayout.VerticalScope())
												{
													for (int i = 0; i < list.Count; i++)
													{
														using (new GUILayout.VerticalScope(AlphaBackStyle))
														{
															var key = name + "[" + i + "]";
															if (DrawElement == null)
															{
																list[i] = list[i].Draw(key, typeInfo.ElementType, customAttribute);
															}
															else
															{
																list[i] = DrawElement.Invoke(i, list[i], key, typeInfo.ElementType);
															}
															using (new GUILayout.HorizontalScope())
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
					LabelField(name, obj?.ToString());
					break;
			}
			return obj;
		}
		public static GUIContent ToGUIContent(this object obj)
		{

			if (obj is UnityEngine.Object uObj)
			{
#if UNITY_EDITOR
				return new GUIContent(uObj.name, AssetPreview.GetAssetPreview(uObj), uObj.ToString());
#else
				return new GUIContent(uObj.name, uObj.ToString());
#endif
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
				return new GUIContent(memberInfo.QName(), memberInfo.Name);
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

#if UNITY_EDITOR

		[UnityEditor.SettingsProvider]
		public static SettingsProvider QToolSetting()
		{
			return new SettingsProvider("Project/" + nameof(QTool) + "设置", SettingsScope.Project)
			{
				guiHandler = (searchContext) =>
				{
					foreach (var SettingType in typeof(InstanceScriptable<>).GetAllTypes())
					{
						using (new GUILayout.VerticalScope(AlphaBackStyle))
						{
							GUILayout.Label(SettingType.QName(), TitleStyle);
							new SerializedObject(SettingType.InvokeFunction(nameof(QTool.QToolSetting.Instance)) as ScriptableObject).Draw();
						}
					}
				}
			};
		}
		public static void ProgressBar(string info, float progress, Color color)
		{
			GUILayout.Box("", AlphaBackStyle);
			var lastRect = GUILayoutUtility.GetLastRect();
			var rateRect = lastRect;
			progress = Mathf.Clamp(progress, 0.01f, 1);
			rateRect.width *= progress;
			if (progress > 0)
			{
				PushColor(color);
				GUI.Box(rateRect, "", AlphaBackStyle);
				PopColor();
			}
			GUI.Label(lastRect, info, CenterLable);
		}
		
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
		public static void Draw(this SerializedObject serializedObject)
		{
			var iterator = serializedObject.GetIterator();
			if (iterator.NextVisible(true))
			{
				do
				{
					var GUIEnabled = GUI.enabled;
					if ("m_Script".Equals(iterator.name))
					{
						GUI.enabled = false;
					}
					if (iterator.IsShow())
					{
						EditorGUILayout.PropertyField(iterator, new GUIContent(iterator.QName()));
					}
					if ("m_Script".Equals(iterator.name))
					{
						GUI.enabled = GUIEnabled;
					}
				} while (iterator.NextVisible(false));
				serializedObject.ApplyModifiedProperties();
			}
		}

	

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
		private static bool UnityAttributeView(this SerializedProperty property, Rect rect, GUIContent content = null, string parentType = "")
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
					}
					break;
				case SerializedPropertyType.Float:
					{
						var range = property.GetAttribute<RangeAttribute>(parentType);
						if (range != null)
						{
							EditorGUI.Slider(rect, property, range.min, range.max, content);
							return true;
						}
					}
					break;
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
					}
					break;
				case SerializedPropertyType.String:
					{
						var textArea = property.GetAttribute<TextAreaAttribute>(parentType);
						if (textArea != null)
						{
							EditorGUI.LabelField(rect.HorizontalRect(0, 0.4f), content);
							property.stringValue = EditorGUI.TextArea(rect.HorizontalRect(0.4f, 1f), property.stringValue);
							return true;
						}
					}
					break;
				default:
					break;
			}
			return false;
		}
		private static bool PrivateDraw(this SerializedProperty property, Rect rect, GUIContent content = null, string parentType = "")
		{
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
						cur.PrivateDraw(rect, null, parentType);
					} while (cur.NextVisible(false));
				}
				return expanded;
			}
			else if (UnityAttributeView(property, rect, content, parentType))
			{
				return false;
			}
			else
			{
				if (content == null)
				{
					content = new GUIContent(cur.QName(parentType));
				}
				return EditorGUI.PropertyField(rect, cur, content, true);
			}
		}
		public static float GetHeight(this SerializedProperty property)
		{
			return GetHeight(property, property.hasChildren);
		}
		public static float GetHeight(this SerializedProperty property, bool containsChild)
		{
			if (!property.IsShow()) return 0;
			switch (property.propertyType)
			{
				case SerializedPropertyType.String:
					{
						var textArea = property.GetAttribute<TextAreaAttribute>();
						if (textArea != null)
						{
							var lineCount = property.stringValue.Split('\n').Length;
							return EditorGUI.GetPropertyHeight(property, false) * Mathf.Clamp(lineCount, textArea.minLines, textArea.maxLines);
						}
					}
					break;
				default:
					break;
			}
			return EditorGUI.GetPropertyHeight(property, containsChild && property.isExpanded);
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
	
#endif

	}
	public class QToolBar
	{
		internal List<string> Values = new List<string>() ;
		internal List<QToolBar> ChildToolbars = new List<QToolBar>() ;
		public QToolBar ChildToolBar => ChildToolbars.Get(Select);
		public int Select { get; internal set; } = -1;
		public bool Selected => Select >= 0;
		public bool IsButton => ChildToolbars.Count == 0;
		public int Width { get; set; } = 100;
		public object Value { get; set; }
		public void CancelSelect()
		{
			if (Selected)
			{
				ChildToolBar.CancelSelect();
				Select = -1;
			}
		}
		public QToolBar()
		{
		}
		public QToolBar this[string key]
		{
			get
			{
				var index = Values.IndexOf(key);
				if (index < 0)
				{
					if (ChildToolbars.Count == 0&&!Value.IsNull())
					{
						Values.Add("返回");
						ChildToolbars.Add(new QToolBar { Value = "返回" });
					}
					Values.Add(key);
					var newToolBar = new QToolBar();
					newToolBar.Value = key;
					ChildToolbars.Add(newToolBar);
					return newToolBar;
				}
				else
				{
					return ChildToolbars[index];
				}
			}
		}
		public override string ToString()
		{
			return Selected?(Value + "/" + ChildToolBar): Value?.ToString();
		}
	}
#if UNITY_EDITOR
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
#endif
	public class QEnumListData
	{
		static QDictionary<string, QEnumListData> DrawerDic = new QDictionary<string, QEnumListData>((key) => new QEnumListData());

		public List<GUIContent> List = new List<GUIContent>();
		public int SelectIndex = 0;
		public string SelectValue
		{
			get
			{
				if (SelectIndex >= 0 && SelectIndex < List.Count)
				{
					return List[SelectIndex].text == "null" ? null : List[SelectIndex].text;
				}
				else
				{
					if (SelectIndex < 0)
					{
						SelectIndex = 0;
					}
					return default;
				}
			}
		}

		public void UpdateList(string key)
		{
			if (key == "null" || key.IsNull())
			{
				SelectIndex = 0;
			}
			else
			{
				SelectIndex = List.FindIndex((obj) => obj.text == key);
			}
		}
#if UNITY_EDITOR
		public static QEnumListData Get(SerializedProperty property, string funcKey)
		{
			if (property.propertyType == SerializedPropertyType.ObjectReference)
			{
				return Get(QReflection.ParseType(property.type.SplitEndString("PPtr<$").TrimEnd('>')), funcKey);
			}
			else
			{
				return Get((object)property, funcKey);
			}
		}
#endif

		public static QEnumListData Get(object obj, string funcKey)
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
			var drawerKey = funcKey;
			if (drawerKey.IsNull())
			{
#if UNITY_EDITOR
				if (obj is SerializedProperty property)
				{
					drawerKey = property.propertyType + "_" + property.name;
				}
				else
#endif
				{
					drawerKey = type + "";
				}
			}
			var drawer = DrawerDic[drawerKey];
			if (!funcKey.IsNull())
			{
				if (obj.InvokeFunction(funcKey) is IList itemList)
				{
					if (drawer.List.Count != itemList.Count)
					{
						drawer.List.Clear();
						foreach (var item in itemList)
						{
							drawer.List.Add(item.ToGUIContent());
						}
					}
				}
			}
			else if (drawer.List.Count == 0)
			{
				if (type.IsAbstract)
				{
					foreach (var childType in type.GetAllTypes())
					{
						drawer.List.Add(new GUIContent(childType.Name));
					}
				}
				else if (type.IsEnum)
				{
					foreach (var name in Enum.GetNames(type))
					{
						drawer.List.Add(name.ToGUIContent());
					}
				}
#if UNITY_EDITOR
				else if (obj is SerializedProperty property)
				{
					drawer.List.Clear();
					switch (property.propertyType)
					{
						case SerializedPropertyType.Enum:
							{
								foreach (var item in property.enumNames)
								{
									drawer.List.Add(new GUIContent(item));
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
					QGUI.Label("错误函数" + funcKey);
				}
			}
			if (drawer.List.Count <= 0)
			{
				drawer.List.AddCheckExist(new GUIContent("null"));
			}
			return drawer;
		}
	}

}
