using QTool;
using QTool.Reflection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
namespace QTool {

	public class QDataTableWindow : QFileEditorWindow<QDataTableWindow> {

		[OnOpenAsset(0)]
		public static bool OnOpen(int instanceID, int line) {
			if (EditorUtility.InstanceIDToObject(instanceID) is TextAsset textAsset) {
				if (textAsset == null)
					return false;
				var path = AssetDatabase.GetAssetPath(textAsset);
				if (path.EndsWith(QDataTable.Extension)) {
					Open(path);
					return true;
				}
			}
			return false;
		}
		[MenuItem("QTool/窗口/QDataList")]
		public static void OpenWindow() {
			var window = GetWindow<QDataTableWindow>();
			window.minSize = new Vector2(500, 300);
		}

		public QSerializeHasReadOnlyType typeInfo;
		public QDataTable table = new QDataTable();
		public QList<object> objList = new QList<object>();
		public QList<QMemeberInfo> Members = new QList<QMemeberInfo>();   
		ListView listView = null;
		VisualElement titlesView = null;
		long lastCkickTime = 0;
		VisualElement CellView = null;
		#region 初始化UI
		protected override void CreateGUI() {
			base.CreateGUI();
			var root = rootVisualElement.AddScrollView();
			root.style.height = new Length(100, LengthUnit.Percent);
			titlesView = root.AddVisualElement(FlexDirection.Row);
			listView = root.AddListView(table, (visual, y) => {
				visual.Clear();
				AddRow(visual, table[y], objList.Get(y));
				visual.AddMenu(menu => {
					menu.menu.AppendAction("添加行", action => { AddAt(y); listView.Rebuild(); SetDataDirty(); });
					menu.menu.AppendAction("删除行", action => { RemoveAt(y); listView.Rebuild(); SetDataDirty(); });
				});
			},
				() => {
					var layout = new VisualElement();
					layout.style.flexDirection = FlexDirection.Row;
					return layout;
				});
			root.RegisterCallback<MouseDownEvent>(data => {
				AutoLoad = true;
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
		private void AddRow(VisualElement visual, QDataTable.Row row, object obj = null, bool isTitle = false) {
			if (row == null) return;
			for (int x = 0; x < row.Count; x++) {
				var member = Members[x];
				try {
					var title = row.Table.Titles[x];
					if (isTitle) {
						var label = visual.AddLabel(row[x]?.Replace("\\n", " "), TextAnchor.MiddleCenter);
						label.style.width = 180;
						label.style.marginLeft = 10;
						label.style.marginRight = 10;
					}
					else {
						var value = obj == null || member == null ? row[x] : member.Get(obj)?.ToQDataType(member.Type).Trim('\"');
						var label = visual.AddLabel(value?.Replace("\\n", " "), TextAnchor.MiddleCenter);
						label.Tooltip(value);
						label.style.width = 180;
						label.style.marginLeft = 10;
						label.style.marginRight = 10;
						if (member != null && member.Set == null) {
							label.SetEnabled(false);
						}
						else {
							label.RegisterCallback<ClickEvent>((eventData) => {
								if (CellView.style.display == DisplayStyle.Flex) {
									return;
								}
								if (eventData.timestamp - lastCkickTime < 500) {
									CellView.Clear();
									CellView.style.width = new StyleLength(label.style.width.value.value * 2);
									VisualElement view = null;
									if (typeInfo == null || member == null) {
										view = CellView.Add("", value, typeof(string), (newValue) => {
											row[title] = (string)newValue;
											label.text = ((string)newValue)?.Replace("\\n", " ");
											SetDataDirty();
										});
									}
									else {
										view = CellView.Add("", member.Get(obj), member.Type, (newValue) => {
											if (member.Key == nameof(member.Key)) {
												QPopupData.ClearAll();
											}
											member.Set(obj, newValue);
											label.text = member.Get(obj).ToQDataType(member.Type)?.Replace("\\n", " ").Trim('\"');
											label.Tooltip(label.text);
											SetDataDirty();
										}, member.MemeberInfo);
									}
									AutoLoad = false;
									CellView.style.display = DisplayStyle.Flex;
									var foldout = CellView.Q<Foldout>();
									if (foldout != null) {
										foldout.value = true;
									}
									lastCkickTime = 0;
								}
								else {
									lastCkickTime = eventData.timestamp;
								}
							});
							label.AddMenu(menu => {
								menu.menu.AppendAction("清空" + title, action => {
									if (typeInfo == null || member == null) {
										row[title] = "";
										label.text = "";
									}
									else {
										member.Set(obj, null);
										label.text = member.Get(obj).ToQDataType(member.Type)?.Replace("\\n", " ").Trim('\"');
									}
									SetDataDirty();
								});
							});
						}
					}
				}
				catch (Exception e) {
					Debug.LogException(new Exception($" {x} {obj} : {member} \n{row.Table.Titles}\n{row} ", e));
				}
			}
		}
		#endregion
		public override void SetDataDirty() {
			if (typeInfo != null) {
				objList.ToQDataList(table, typeInfo.Type);
			}
			Data = table?.ToString();
		}
		protected override async void ParseData() {
			try {
				var path = FilePath;
				var type = QReflection.ParseType(path.FileName());
				if (type == null)
					type = QReflection.ParseType(path.DirectoryName().FileName());
				if (type != null) {
					table = new QDataTable(Data);
					table.ParseQDataList(objList, type);
					typeInfo = QSerializeHasReadOnlyType.Get(type);
					for (int i = 0; i < table.Titles.Count; i++) {
						Members[i] = typeInfo.GetMemberInfo(table.Titles[i]);
						if (Members[i] == null) {
							QDebug.LogError(type.Name + " 列[" + table.Titles[i] + "]为空");
						}
					}
				}
				else {
					table = new QDataTable(Data);
					objList.Clear();
					typeInfo = null;
					Members.Clear();
				}
				PlayerPrefs.SetString(nameof(QDataTableWindow) + "_LastPath", path);
				while (listView == null || titlesView == null) {
					await Task.Yield();
				}
				titlesView.Clear();
				AddRow(titlesView, table.Titles, null, true);
				listView.itemsSource = table;
				listView.style.width = table.Titles.Count * 200 + 100;
				listView.Rebuild();
			}
			catch (Exception e) {
				Debug.LogError("解析QDataList类型[" + typeInfo?.Type + "]出错");
				Debug.LogException(e);
				Close();
			}
		}
		public void AddAt(int y) {
			table.CreateAt(QSerializeType.Get(typeof(QDataTable)), y - 1);
			if (objList != null) {
				objList.CreateAt(QSerializeType.Get(typeof(List<object>)), y - 1);
			}
		}
		public void RemoveAt(int y) {
			table.RemoveAt(y);
			if (objList != null) {
				objList?.RemoveAt(y);
			}
		}
	
	}

}
