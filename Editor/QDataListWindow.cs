using QTool.Inspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using QTool.Reflection;
using QTool;
using UnityEngine.UIElements;
namespace QTool.FlowGraph
{

	public class QDataListWindow : QFileEditorWindow<QDataListWindow>
	{

		[OnOpenAsset(0)]
		public static bool OnOpen(int instanceID, int line)
		{
			if (EditorUtility.InstanceIDToObject(instanceID) is TextAsset textAsset)
			{
				if (textAsset == null) return false;
				var path = AssetDatabase.GetAssetPath(textAsset);
				if (path.Contains(nameof(QDataList) + "Asset" + '/') && path.EndsWith(".txt"))
				{
					FilePath = path;
					OpenWindow();
					return true;
				}
			}
			return false;
		}
		[MenuItem("QTool/窗口/QDataList")]
		public static void OpenWindow()
		{
			var window = GetWindow<QDataListWindow>();
			window.minSize = new Vector2(500, 300);
		}
		public override string GetData(UnityEngine.Object file)
		{
			return (file as TextAsset)?.text;
		}

		public override void SaveData()
		{
			QFileTool.Save(FilePath, Data);
		}
		public QSerializeType typeInfo;
		public QDataList qdataList;
		public QList<object> objList = new QList<object>();
		public QList<QMemeberInfo> Members = new QList<QMemeberInfo>();
		private int DragIndex { get; set; } = -1;
		ListView listView = null;
		long lastCkickTime = 0;
		VisualElement CellView = null;
		#region 初始化UI
		protected override void CreateGUI()
		{
			base.CreateGUI();	
			var root = rootVisualElement.AddScrollView();
			root.style.height = new Length(100, LengthUnit.Percent);
		
			listView = root.AddListView(qdataList, (visual, y) =>
			{
				visual.Clear();
				for (int x = 0; x < qdataList[y].Count; x++)
				{
					var row = qdataList[y];
					var value = GetValue(x, y);
					var member = Members[x];
					var title = qdataList.TitleRow[x];
					var obj = objList[y - 1];
					if (y == 0)
					{
						var label = visual.AddLabel(value?.Replace("\\n", " "), TextAnchor.MiddleCenter);
						label.style.width = 180;
						label.style.marginLeft = 10;
						label.style.marginRight = 10;
					}
					else
					{
						var label = visual.AddLabel(value?.Replace("\\n", " "), TextAnchor.MiddleCenter);
						label.Tooltip(value);
						label.style.width = 180;
						label.style.marginLeft = 10;
						label.style.marginRight = 10;
						label.name = title;
						label.RegisterCallback<ClickEvent>((eventData) =>
						{
							if (CellView.style.display == DisplayStyle.Flex)
							{
								return;
							}
							if (eventData.timestamp- lastCkickTime<500)
							{
								CellView.Clear();
								CellView.style.width = new StyleLength(label.style.width.value.value * 2);
								VisualElement view = null;
								if (typeInfo == null || member == null)
								{
									view = CellView.Add("", value, typeof(string), (newValue) =>
									{
										row[title] = (string)newValue;
										label.text = ((string)newValue)?.Replace("\\n", " ");
									});
								}
								else
								{
									view = CellView.Add("", member.Get(obj), member.Type, (newValue) =>
									{
										member.Set(obj, newValue);
										label.text = member.Get(obj).ToQDataType(member.Type, false)?.Replace("\\n", " ").Trim('\"');
										label.Tooltip(label.text);
									}, member.MemeberInfo);
								}
								AutoSaveLoad = false;
								CellView.style.display = DisplayStyle.Flex;
								var foldout = CellView.Q<Foldout>();
								if (foldout != null)
								{
									foldout.value = true;
								}
								lastCkickTime = 0;
							}
							else
							{
								lastCkickTime = eventData.timestamp;
							}
						});
						label.AddMenu(menu =>
						{
							menu.menu.AppendAction("清空" + title, action =>
							{
								if (typeInfo == null || member == null)
								{
									row[title] = "";
									label.text = "";
								}
								else
								{
									member.Set(obj, null);
									label.text = member.Get(obj).ToQDataType(member.Type, false)?.Replace("\\n", " ").Trim('\"');
								}
							});
						});
					}
				}
				visual.RegisterCallback<MouseDownEvent>(data =>
				{
					DragIndex = y;
				});
				visual.RegisterCallback<MouseUpEvent>(data =>
				{
					if (DragIndex > 0 && y > 0 && DragIndex != y)
					{
						if (typeInfo != null)
						{
							objList.Replace(DragIndex - 1, y - 1);
						}
						qdataList.Replace(DragIndex, y);
						listView.Rebuild();
					}
					DragIndex = -1;
				});
				visual.AddMenu(menu =>
				{
					menu.menu.AppendAction("添加行", action => { AddAt(y); listView.Rebuild(); });
					menu.menu.AppendAction("删除行", action => { RemoveAt(y); listView.Rebuild(); });
				});
			},
			() =>
			{
				var layout = new VisualElement();
				layout.style.flexDirection = FlexDirection.Row;
				return layout;
			});
			root.RegisterCallback<MouseDownEvent>(data =>
			{
				AutoSaveLoad = true;
				CellView.style.display = DisplayStyle.None;
			});
			CellView = rootVisualElement.AddVisualElement();
			CellView.style.position = Position.Absolute;
			CellView.style.display = DisplayStyle.None;
			CellView.style.backgroundColor = Color.Lerp(Color.black, Color.white, 0.3f);
			CellView.style.SetBorder(Color.black);
			CellView.style.overflow = Overflow.Visible;
			CellView.style.right = new Length(0, LengthUnit.Percent);
		}
		#endregion
		protected override void OnLostFocus()
		{
			if (typeInfo != null)
			{
				objList.ToQDataList(qdataList, typeInfo.Type);
			}
			ChangeData(qdataList?.ToString());
			base.OnLostFocus();
		}
		protected override async void ParseData()
		{
			try
			{
				var path = FilePath;
				var type = QReflection.ParseType(path.GetBlockValue(nameof(QDataList) + "Asset" + '/', ".txt").SplitStartString("/"));
				if (type != null)
				{
					typeInfo = QSerializeType.Get(type);
					qdataList = QDataList.Load(path);
					qdataList.ParseQDataList(objList, type);
					for (int i = 0; i < qdataList.TitleRow.Count; i++)
					{
						Members[i] = typeInfo.GetMemberInfo(qdataList.TitleRow[i]);
						if (Members[i] == null)
						{
							Debug.LogError("列[" + type + "]为空");
						}
					}
				}
				else
				{
					qdataList = QDataList.Load(path);
					typeInfo = null;
				}
				PlayerPrefs.SetString(nameof(QDataListWindow) + "_LastPath", path);
				await QTask.Wait(() => listView != null);
				listView.itemsSource = qdataList;
				listView.style.width = qdataList.TitleRow.Count * 200+100;
				listView.Rebuild();
			}
			catch (Exception e)
			{
				Debug.LogError("解析QDataList类型[" + typeInfo?.Type + "]出错：\n" + e);
				Close();
			}
		}
		public void AddAt(int y)
		{
			qdataList.CreateAt(QSerializeType.Get(typeof(QDataList)), y);
			if (objList != null)
			{
				objList.CreateAt(QSerializeType.Get(typeof(List<object>)),y-1);
			}
		}
		public void RemoveAt(int y)
		{
			qdataList.RemoveAt(y);
			if (objList != null)
			{
				objList?.RemoveAt(y-1);
			}
		}
		public string GetValue(int x,int y)
		{
			if (y == 0||typeInfo==null)
			{
				return qdataList[y][x];
			}
			else
			{
				var member = Members[x];
				if (member == null) return "";
				var obj = objList[y - 1];
				return member.Get(obj)?.ToQDataType(member.Type,false).Trim('\"');
			}
		}
	}
	
}
