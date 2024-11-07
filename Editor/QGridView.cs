using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.Reflection;
namespace QTool
{

	public class QGridView
	{
		public Func<Vector2Int> GetSize;
		public Vector2Int GridSize { private set; get; }
		public Vector2 ViewDataSize { private set; get; }
		public Vector2 ViewSize { private set; get; }
		public Vector2 ViewScrollPos { private set; get; }
		QList<float> CellWidth = new QList<float>();

		public bool HasChanged { set; get; } = false;

		public Func<int, int, int, bool> ClickCell = null;
		public QGridView(Func<int, int, string> GetStringValue, Func<Vector2Int> GetSize, Func<int, int, int, bool> ClickCell)
		{
			this.GetStringValue = GetStringValue;
			this.GetSize = GetSize;
			this.ClickCell = ClickCell;
		}
		readonly static Vector2 DefualtCellSize = new Vector2(100, 30);
		public string Copy()
		{
			var qdata = new QDataTable();
			for (int i = 0; i < GridSize.x; i++)
			{
				for (int j = 0; j < GridSize.y; j++)
				{
					qdata[j][i] = GetStringValue(i, j);
				}
			}
			return qdata.ToString();
		}
		public float GetWidth(int x = 0)
		{
			return Mathf.Max(CellWidth[x], 100);
		}
		public float GetHeight(int x = 0)
		{
			return 30;
		}
		public void Space(int start, int end, bool width = true)
		{
			Func<int, float> GetValue = null;
			if (width)
			{
				GetValue = GetWidth;
			}
			else
			{
				GetValue = GetHeight;
			}
			var sum = 0f;
			for (int i = start; i < end; i++)
			{
				sum += GetValue(i);
			}
			GUILayout.Space(sum);
		}
		public RectInt GetViewRange()
		{
			var range = new RectInt();
			var viewRect = new Rect(ViewScrollPos, ViewDataSize);
			var sum = 0f;
			for (range.x = 1; range.x < GridSize.x; range.x++)
			{
				sum += GetWidth(range.x);
				if (sum >= viewRect.xMin)
				{
					break;
				}
			}
			for (range.width = 1; range.xMax < GridSize.x; range.width++)
			{
				sum += GetWidth(range.xMax);
				if (sum >= viewRect.xMax)
				{
					range.width++;
					break;
				}
			}

			sum = 0;
			for (range.y = 1; range.y < GridSize.y; range.y++)
			{
				sum += GetHeight(range.y);
				if (sum >= viewRect.yMin)
				{
					break;
				}
			}
			for (range.height = 1; range.yMax < GridSize.y; range.height++)
			{
				sum += GetHeight(range.yMax);
				if (sum >= viewRect.yMax)
				{
					range.height++;
					break;
				}
			}
			if (range.yMax > GridSize.y)
			{
				range.height -= range.yMax - GridSize.y;
			}
			if (range.xMax > GridSize.x)
			{
				range.width -= range.xMax - GridSize.x;
			}
			return range;
		}
		void DrawLine(Rect lastRect)
		{
			var lastColor = Handles.color;
			Handles.color = Color.gray;
			Handles.DrawLine(new Vector3(lastRect.xMin, lastRect.yMax), new Vector3(lastRect.xMax, lastRect.yMax));
			Handles.DrawLine(new Vector3(lastRect.xMax, lastRect.yMin), new Vector3(lastRect.xMax, lastRect.yMax));
			Handles.color = lastColor;
		}
		public Func<int, int, string> GetStringValue = null;
		public Vector2Int ClickIndex = Vector2Int.one * -1;
		public int buttonIndex = -1;
		public Rect DrawCell(int x, int y)
		{
			var width = GUILayout.Width(GetWidth(x));
			var height = GUILayout.Height(GetHeight(y));
			var str = GetStringValue(x, y);
			if (string.IsNullOrEmpty(str))
			{
				str = "";
			}
			GUILayout.Label(str,width, height);
			var rect = GUILayoutUtility.GetLastRect();
			if (Event.current.type != EventType.Layout)
			{
				if (EventType.MouseUp.Equals(Event.current.type))
				{
					if (rect.Contains(Event.current.mousePosition))
					{
						buttonIndex = Event.current.button;
						ClickIndex = new Vector2Int
						{
							x = x,
							y = y
						};
						Event.current.Use();
					}
				}
			}
			return rect;
		}
		RectInt ViewRange;
		int DragXIndex = -1;
		float startPos = 0;
		public void DoLayout(Action Repaint)
		{
			try
			{
				if (DragXIndex >= 0)
				{
					CellWidth[DragXIndex] = Event.current.mousePosition.x - startPos;
					if (Event.current.type == EventType.MouseUp)
					{
						DragXIndex = -1;
						Event.current.Use();
					}
					Repaint();
				}
				if (Event.current.type != EventType.Repaint)
				{
					GridSize = GetSize();

					ViewRange = GetViewRange();

				}
				using (new GUILayout.VerticalScope())
				{
					using (new GUILayout.HorizontalScope())
					{
						var rect = DrawCell(0, 0);
						Handles.DrawLine(new Vector3(0, rect.yMax), new Vector3(ViewSize.x, rect.yMax));
						Handles.DrawLine(new Vector3(rect.xMax, rect.yMin), new Vector3(rect.xMax, ViewSize.y));

						var pos = rect.xMin;
						rect.x += rect.width - 5;
						rect.width = 10;
						if (rect.Contains(Event.current.mousePosition))
						{
							if (Event.current.type == EventType.MouseDown)
							{
								startPos = pos ;
								DragXIndex = 0;
								Event.current.Use();
							}
						}

						using (new GUILayout.ScrollViewScope(new Vector2(ViewScrollPos.x, 0), GUIStyle.none, GUIStyle.none, GUILayout.Height(GetHeight())))
						{
							using (new GUILayout.HorizontalScope())
							{
								Space(1, ViewRange.x);
								for (int x = ViewRange.x; x < ViewRange.xMax; x++)
								{
									var drawRect = DrawCell(x, 0);
									DrawLine(drawRect);
									pos = drawRect.xMin;
									drawRect.x += drawRect.width - 5;
									drawRect.width = 10;
									if (drawRect.Contains(Event.current.mousePosition))
									{
										if (Event.current.type == EventType.MouseDown)
										{
											startPos = pos + rect.xMax - ViewScrollPos.x;
											DragXIndex = x;
											Event.current.Use();
										}
									}

								}
								Space(ViewRange.xMax, GridSize.x);
								GUILayout.FlexibleSpace();
							}

						}
						GUILayout.Space(13);
					}
					GUILayout.Space(5);
					using (new GUILayout.HorizontalScope())
					{
						using (new GUILayout.ScrollViewScope(new Vector2(0, ViewScrollPos.y), GUIStyle.none, GUIStyle.none, GUILayout.Width(GetWidth())))
						{
							using (new GUILayout.VerticalScope())
							{
								Space(1, ViewRange.y, false);
								for (int y = ViewRange.y; y < ViewRange.yMax; y++)
								{
									DrawLine(DrawCell(0, y));
								}
								Space(ViewRange.yMax, GridSize.y, false);
								GUILayout.Space(13);
							}
						}
						GUILayout.Space(6);
						using (var dataView = new GUILayout.ScrollViewScope(ViewScrollPos))
						{
							using (new GUILayout.VerticalScope())
							{
								Space(1, ViewRange.y, false);
								for (int y = ViewRange.y; y < ViewRange.yMax; y++)
								{
									using (new GUILayout.HorizontalScope())
									{
										Space(1, ViewRange.x);
										for (int x = ViewRange.x; x < ViewRange.xMax; x++)
										{
											DrawLine(DrawCell(x, y));
										}
										Space(ViewRange.xMax, GridSize.x);
										GUILayout.FlexibleSpace();
									}
								}
								Space(ViewRange.yMax, GridSize.y, false);
							}
							ViewScrollPos = dataView.scrollPosition;
						}
						if (Event.current.type == EventType.Repaint)
						{
							ViewDataSize = GUILayoutUtility.GetLastRect().size;
						}
					}
				}
				if (Event.current.type == EventType.Repaint)
				{
					ViewSize = GUILayoutUtility.GetLastRect().size;
				}
				
				if (buttonIndex >= 0)
				{
					if (ClickCell(ClickIndex.x, ClickIndex.y, buttonIndex))
					{
						HasChanged = true;
						Repaint();
					}
					buttonIndex = -1;
					
				
					
				}
				
			}
			catch (UnityEngine.ExitGUIException)
			{
			}
		}

	}
	public class QEidtCellWindow : EditorWindow
	{
		public static bool IsShow
		{
			get
			{
				return Instance!=null;
			}
		}
		static QEidtCellWindow Instance { set; get; }
		public static object Show(string key,object value,Type type,out bool changed,ICustomAttributeProvider customAttribute)
		{
			if (Instance == null)
			{
				Instance = GetWindow<QEidtCellWindow>();
				Instance.minSize = new Vector2(300, 200);
				Instance.maxSize = new Vector2(600, 900);
			}
			Instance.titleContent = new GUIContent( key);
			Instance.type = type;
			Instance.value = value;
			var oldValue = Instance.value.ToQDataType(type)?.GetHashCode();
			Instance.customAttribute = customAttribute;
			Instance.ShowModal();
			changed = oldValue != Instance.value.ToQDataType(type)?.GetHashCode();
			return Instance.value;
		}
		public Type type;
		public object value;
		public Vector2 scrollPos;
		public ICustomAttributeProvider customAttribute;
		private void OnGUI()
		{
			using (var scroll= new GUILayout.ScrollViewScope(scrollPos))
			{
				scrollPos = scroll.scrollPosition;
				value= value.Draw("", type,customAttribute);	
			}
		}
	}
}
