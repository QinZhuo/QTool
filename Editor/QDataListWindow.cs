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

	public class QDataListWindow : QTextEditorWindow<QDataListWindow>
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
					var window = GetWindow<QDataListWindow>();
					window.minSize = new Vector2(300, 200);
					return true;
				}
			}
			return false;
		}
		public QSerializeType typeInfo;
		public QDataList qdataList;
		public QList<object> objList = new QList<object>();
		public QList<QMemeberInfo> Members = new QList<QMemeberInfo>();

		ListView listView = null;
		Label lastClick = null;
		VisualElement CellView = null;
		protected override void CreateGUI()
		{
			base.CreateGUI();
			var root = rootVisualElement.AddScrollView();
			root.style.height = new Length(100, LengthUnit.Percent);
			CellView = rootVisualElement.AddVisualElement();
			CellView.style.position = Position.Absolute;
			CellView.visible = false;
			CellView.style.backgroundColor = Color.Lerp(Color.black, Color.white, 0.3f);
			CellView.style.SetBorder(Color.black);
			listView = root.AddListView(qdataList, (visual, y) =>
			{
				visual.Clear();
				var width = 0;
				for (int x = 0; x < qdataList[y].Count; x++)
				{
					var row = qdataList[y];
					var value = GetValue(x, y);
					var member = Members[x];
					var title = qdataList.TitleRow[x];
					var obj = objList[y - 1];
					if (y == 0)
					{
						var label = visual.AddLabel(value, TextAnchor.MiddleCenter);
						label.style.width = 180;
						label.style.marginLeft = 10;
						label.style.marginRight = 10;
						width += 200;
					}
					else
					{
						var label = visual.AddLabel(value, TextAnchor.MiddleCenter);
						label.style.width = 180;
						label.style.marginLeft = 10;
						label.style.marginRight = 10;
						label.name = title;
						label.RegisterCallback<ClickEvent>((eventData) =>
						{
							if (lastClick == label)
							{
								if (CellView.visible)
								{
									return;
								}
								CellView.Clear();
								var pos = label.worldTransform.MultiplyPoint(label.transform.position);
								CellView.style.left = pos.x;
								CellView.style.top = pos.y;
								CellView.style.width = new StyleLength(label.style.width.value.value * 2);
								VisualElement view = null;
								if (typeInfo == null || member == null)
								{
									view = CellView.Add("", value, typeof(string), (newValue) =>
									{
										row[title] = (string)newValue;
										label.text = (string)newValue;
									});
								}
								else
								{
									view = CellView.Add("", member.Get(obj), member.Type, (newValue) =>
									{
										member.Set(obj, newValue);
										label.text = newValue.ToQDataType(member.Type, false).Trim('\"');
									});
								}
								CellView.visible = true;
								var foldout = CellView.Q<Foldout>();
								if (foldout != null)
								{
									foldout.value = true;
								}
							}
							else
							{
								CellView.visible = false;
								lastClick = label;
							}
						});
					}
				}
				visual.AddManipulator(new ContextualMenuManipulator(menu =>
				{
					menu.menu.AppendAction("添加行", action => { AddAt(y); listView.Rebuild(); });
					menu.menu.AppendAction("删除行", action => { RemoveAt(y); listView.Rebuild(); });
				}));
				if (y == 0)
				{
					listView.style.width = width;
				}
			},
			() =>
			{
				var layout = new VisualElement();
				layout.style.flexDirection = FlexDirection.Row;
				return layout;
			});
			
		}

		protected override void OnLostFocus()
		{
			if (typeInfo != null)
			{
				objList.ToQDataList(qdataList, typeInfo.Type);
			}
			Text = qdataList?.ToString();
			base.OnLostFocus();
		}
		protected override async void ParseText()
		{
			if (CellView!=null&&CellView.visible) return;
			try
			{
				var path = FilePath;
				var type = QReflection.ParseType(path.GetBlockValue(nameof(QDataList) + "Asset" + '/', ".txt").SplitStartString("/"));
				if (type != null)
				{
					typeInfo = QSerializeType.Get(type);
					qdataList = QDataList.GetData(path);
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
					qdataList = QDataList.GetData(path);
					typeInfo = null;
				}
				PlayerPrefs.SetString(nameof(QDataListWindow) + "_LastPath", path);
				await QTask.Wait(() => listView != null);
				listView.itemsSource = qdataList;
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
