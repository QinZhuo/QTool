using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Inspector;
using QTool.Reflection;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{

	public static class QEditorGUI
	{
		#region GUI样式
		private static GUIStyle _RichLabel;
		public static GUIStyle RichLabel => _RichLabel ??= new GUIStyle("label")
		{
			richText = true
		};
		private static GUIStyle _RightLabel;
		public static GUIStyle RightLabel => _RightLabel ??= new GUIStyle("label")
		{
			alignment = TextAnchor.UpperRight
		};
		public static Color SelectColor { get; private set; } = new Color32(15, 129, 190, 100);
		public static Color BackColor { get; private set; } = new Color32(45, 45, 45, 255);
		public static Color AlphaBackColor { get; private set; } = new Color32(0, 0, 0, 40);
		public static Color ContentColor { get; private set; } = new Color32(225, 225, 225, 255);
		public static Color ButtonColor { get; private set; } = new Color32(70, 70, 70, 255);

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

		#endregion

		
		public const float Size = 10;
		public const float Height = Size * 3f;
		public static QDictionary<Type, Func<object, string, object>> DrawOverride = new QDictionary<Type, Func<object, string, object>>();

		public static List<string> TypeMenuList = new List<string>() { typeof(UnityEngine.Object).FullName.Replace('.', '/') };

		public static List<Type> TypeList = new List<Type>() { typeof(UnityEngine.Object) };

	
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
			FoldoutCache[hashCode] = EditorGUILayout.Foldout(FoldoutCache[hashCode], key);
#endif
			return FoldoutCache[hashCode];
		}
		public static object Draw(this object obj, string name, Type type = null, ICustomAttributeProvider customAttribute = null, Func<int, object, string, Type, object> DrawElement = null, Action<int, int> IndexChange = null) {
			var hasName = !string.IsNullOrWhiteSpace(name);
			if (type == null) {
				if (obj == null) {
					GUILayout.Label(name);
					return obj;
				}
				else {
					type = obj.GetType();
				}
			}
			if (obj == null && type.IsValueType) {
				obj = type.CreateInstance();
			}
			if (DrawOverride.ContainsKey(type)) {
				return DrawOverride[type].Invoke(obj, name);
			}
			var typeInfo = QSerializeType.Get(type);
			if (type != typeof(object) && !TypeList.Contains(type) && !type.IsGenericType) {
				TypeList.Add(type);
				TypeMenuList.AddCheckExist(type.FullName.Replace('.', '/'));
			}
			switch (typeInfo.Code) {
				case TypeCode.Boolean:
					obj = EditorGUILayout.Toggle(name, (bool)obj);
					break;
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
							obj = EditorGUILayout.EnumFlagsField(name, (Enum)obj);
						}
						else {
							obj = EditorGUILayout.EnumPopup(name, (Enum)obj);
						}
					}
					else {
						obj = EditorGUILayout.IntField(name, (int)obj);
					}
					break;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					obj = EditorGUILayout.LongField(name, (long)obj);
					break;
				case TypeCode.Single:
					obj = EditorGUILayout.FloatField(name, (float)obj);
					break;
				case TypeCode.Decimal:
				case TypeCode.Double:
					obj = EditorGUILayout.DoubleField(name, (double)obj);
					break;
				case TypeCode.String:
					obj = EditorGUILayout.TextField(name, obj?.ToString());
					break;
				case TypeCode.Object:
					switch (typeInfo.ObjType) {
						case QObjectType.DynamicObject: {
							using (new GUILayout.HorizontalScope()) {
								if (obj == null) {
									obj = "";
								}
								var objType = obj.GetType();
								var oldType = TypeList.IndexOf(objType);
								var newType = EditorGUILayout.Popup(oldType, TypeMenuList.ToArray());
								if (newType != oldType) {
									objType = TypeList[newType];
									obj = objType.CreateInstance();
								}
								if (objType != type) {
									obj = Draw(obj, name, objType);
								}
							}
						}
						break;
						case QObjectType.UnityObject: {
							obj = EditorGUILayout.ObjectField(name, (UnityEngine.Object)obj, type, true);
						}
						break;
						case QObjectType.Object: {
							if (obj == null) {
								obj = type.CreateInstance();
							}
							using (new GUILayout.VerticalScope()) {
								var show = false;
								if (hasName) {
									show = Foldout(name);
								}
								if (!hasName || show) {
									using (new GUILayout.HorizontalScope()) {
										if (hasName) {
											GUILayout.Space(10);
										}
										using (new GUILayout.VerticalScope()) {

											foreach (var member in typeInfo.Members) {
												try {
													if (member.Type.IsValueType) {
														member.Set(obj, member.Get(obj).Draw(member.QName, member.Type));
													}
													else {
														member.Set(obj, member.Get(obj).Draw(member.QName, member.Type));
													}
												}
												catch (Exception e) {
													Debug.LogError("序列化【" + member.Key + "】出错\n" + e);
												}

											}
										}
									}
								}
							}
						}
						break;

						case QObjectType.Array:
						case QObjectType.List: {
							if (typeof(IList).IsAssignableFrom(type)) {
								if (typeInfo.ArrayRank > 1) {
									break;
								}
								var list = obj as IList;
								if (list == null) {
									obj = typeInfo.ArrayRank == 0 ? type.CreateInstance() : type.CreateInstance(null, 0);
									list = obj as IList;
								}
								using (new GUILayout.VerticalScope()) {
									var canHideChild = DrawElement == null;
									if (hasName) {
										if (canHideChild) {
											canHideChild = !Foldout(name);
										}
										else {
											EditorGUILayout.LabelField(name);
										}
									}
									if (!canHideChild || !hasName) {
										using (new GUILayout.HorizontalScope()) {
											if (hasName) {
												GUILayout.Space(Size);
											}
											using (new GUILayout.VerticalScope()) {
												for (int i = 0; i < list.Count; i++) {
													using (new GUILayout.VerticalScope()) {
														var key = name + "[" + i + "]";
														if (DrawElement == null) {
															list[i] = list[i].Draw(key, typeInfo.ElementType, customAttribute);
														}
														else {
															list[i] = DrawElement.Invoke(i, list[i], key, typeInfo.ElementType);
														}
														using (new GUILayout.HorizontalScope()) {
															GUILayout.FlexibleSpace();
															QEditorGUI.PushColor(Color.blue.Lerp(Color.white, 0.5f));
															if (GUILayout.Button(new GUIContent("", "新增当前数据"), GUILayout.Width(10), GUILayout.Height(10))) {
																obj = list.CreateAt(typeInfo, i);
																IndexChange?.Invoke(-1, i + 1);
															}
															QEditorGUI.PopColor();
															QEditorGUI.PushColor(Color.red.Lerp(Color.white, 0.5f));
															if (GUILayout.Button(new GUIContent("", "删除当前数据"), GUILayout.Width(10), GUILayout.Height(10))) {
																obj = list.RemoveAt(typeInfo, i);
																IndexChange?.Invoke(i, -1);
															}
															QEditorGUI.PopColor();
														}
													}

												}
											}
										}
										if (list.Count == 0) {
											if (GUILayout.Button("添加新元素", GUILayout.Height(20))) {
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
					EditorGUILayout.LabelField(name, obj?.ToString());
					break;
			}
			return obj;
		}
	
	
		public static Rect HorizontalRect(this Rect rect, float left, float right)
		{
			left = Mathf.Clamp(left,0,1);
			right = Mathf.Clamp(right, 0, 1);
			var leftOffset = left * rect.width;
			var width = (right - left) * rect.width;
			rect.x += leftOffset;
			rect.width = width;
			return rect;
		}
		public static Rect Box(this Color color)
		{
			PushColor(color);
			GUILayout.Box("",GUILayout.Height(Size*2));
			PopColor();
			return GUILayoutUtility.GetLastRect();
		}
		public static Rect Box(this Color color,Rect rect,float left,float right)
		{
			PushColor(color);
			rect = rect.HorizontalRect(left, right);
			GUI.Box(rect, "");
			PopColor();
			return rect;
		}

		internal static string DragKey { get; set; } = null;
#if UNITY_EDITOR
		public static bool DragBar(this Color color,string key, Rect rect,ref float value,Action<GenericMenu> action=null)
		{
			var width = 6 / rect.width;
			var dragBox= color.Box(rect, value, value+ width);
			var newBox=dragBox.Drag(rect, key, action);
			var drag= newBox != dragBox && Event.current.type != EventType.Layout;
			if (drag)
			{
				value=(newBox.x-rect.xMin)/rect.width;
			}
			value = Mathf.Clamp(value, 0, 1- width);
			return drag;
		}
		public static Rect Drag(this Rect selectRect,Rect rangeRect,string key,Action<GenericMenu> action)
		{
			switch (Event.current.type)
			{
				case EventType.MouseDown:
					{
						if (selectRect.Contains(Event.current.mousePosition))
						{
							DragKey = key;
							Event.current.Use();
						}
					}
					break;
				case EventType.MouseDrag:
					{
						if (DragKey==key)
						{
							selectRect.center = Event.current.mousePosition;
							selectRect.x = Mathf.Clamp(selectRect.x,rangeRect.xMin, rangeRect.xMax-selectRect.width);
							selectRect.y = Mathf.Clamp(selectRect.y,rangeRect.yMin, rangeRect.yMax - selectRect.height);
							Event.current.Use();
						}
					}
					break;
				case EventType.MouseUp:
					{
						DragKey = null;
					}
					break;
				default: break;
			}
			selectRect.MouseMenu(action);
			return selectRect;
		}
		public static void ProgressBar(string info, float progress, Color color)
		{
			var rect = Box(Color.white);
			if (progress > 0)
			{
				PushColor(color);
				Box(color, rect,0, progress);
				PopColor();
			}
			GUI.Label(rect, info);
		}
		public static void MouseMenu(this Rect rect, Action<GenericMenu> action)
		{
			if (EventType.MouseUp.Equals(Event.current.type))
			{
				if (rect.Contains(Event.current.mousePosition))
				{
					switch (Event.current.button)
					{
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
						EditorGUILayout.PropertyField(iterator, new GUIContent(QReflection.QName(iterator)));
					}
					if ("m_Script".Equals(iterator.name))
					{
						GUI.enabled = GUIEnabled;
					}
				} while (iterator.NextVisible(false));
				serializedObject.ApplyModifiedProperties();
			}
		}

	

	
		public static void Draw(this SerializedProperty property, Rect rect, GUIContent content = null)
		{
			if (!property.IsShow()) return;
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
				case SerializedPropertyType.Vector2Int:
					{
						var range = property.GetAttribute<RangeAttribute>(parentType);
						if (range != null)
						{
							var value = property.vector2IntValue;
							EditorGUI.LabelField(rect.HorizontalRect(0, 0.4f), content);
							value.x = EditorGUI.IntField(rect.HorizontalRect(0.4f, 0.48f), value.x);
							var x = (float)value.x;
							var y = (float)value.y;
							EditorGUI.MinMaxSlider(rect.HorizontalRect(0.5f, 0.9f), ref x, ref y, range.min, range.max);
							value.x =Mathf.RoundToInt(x);
							value.y = Mathf.RoundToInt(y);
							value.y = EditorGUI.IntField(rect.HorizontalRect(0.92f, 1), value.y);
							property.vector2IntValue = value;
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
							value.x = EditorGUI.FloatField(rect.HorizontalRect(0.4f, 0.48f), value.x);
							EditorGUI.MinMaxSlider(rect.HorizontalRect(0.5f, 0.9f), ref value.x, ref value.y, range.min, range.max);
							value.y = EditorGUI.FloatField(rect.HorizontalRect(0.92f, 1), value.y);
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
			if (UnityAttributeView(property, rect, content, parentType))
			{
				return false;
			}
			else
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
	
	
}
