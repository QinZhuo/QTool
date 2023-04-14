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
namespace QTool.FlowGraph
{

	public class QDataListWindow : EditorWindow
	{

		[OnOpenAsset(0)]
		public static bool OnOpen(int instanceID, int line)
		{
			if (EditorUtility.InstanceIDToObject(instanceID) is TextAsset textAsset)
			{
				return Open(textAsset);
			}
			return false;
		}
		public QSerializeType typeInfo;
		public QDataList qdataList;
		public QGridView gridView;
		public QList<object> objList = new QList<object>();
		public QList<QMemeberInfo> Members = new QList<QMemeberInfo>();
		DateTime lastTime = DateTime.MinValue;
		public static bool Open(TextAsset textAsset)
		{
			if (textAsset == null) return false;
			var path = AssetDatabase.GetAssetPath(textAsset);
			if (path.Contains(nameof(QDataList) + "Asset" + '/') && path.EndsWith(".txt"))
			{
				var window = GetWindow<QDataListWindow>();
				window.minSize = new Vector2(400, 300);
				window.titleContent = new GUIContent(textAsset.name + " - " + nameof(QDataList));
				window.Open(path);
				return true;
			}
			return false;
		}

		public void Open(string path)
		{
			try
			{
				lastTime = QFileManager.GetLastWriteTime(path);
				var type = QReflection.ParseType(path.GetBlockValue(nameof(QDataList) + "Asset" + '/', ".txt").SplitStartString("/"));
				if (type != null)
				{
					typeInfo = QSerializeType.Get(type);
					qdataList = QDataList.GetData(path);
					qdataList.ParseQDataList(objList, type);
					for (int i = 0; i < qdataList.TitleRow.Count; i++)
					{
						Members[i] = typeInfo.GetMemberInfo(qdataList.TitleRow[i]);
						if (Members[i]==null)
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
				PlayerPrefs.SetString(nameof(QDataListWindow) + "_LastPath",path);
			}
			catch (Exception e)
			{
				Debug.LogError("解析QDataList类型[" + typeInfo?.Type + "]出错：\n" + e);
				OpenNull();
			}
			
		}
		private void OnLostFocus()
		{
			if (gridView.HasChanged&&! QEidtCellWindow.IsShow)
			{
				if (typeInfo != null)
				{
					objList.ToQDataList(qdataList, typeInfo.Type);
				} 
				qdataList?.Save();
				lastTime = DateTime.Now;
				gridView.HasChanged = false;
				AssetDatabase.Refresh();
			}
		}
		internal bool AutoOpen = true;
		private void OnFocus()
		{
			var key = nameof(QDataListWindow) + "_LastPath";
			if (PlayerPrefs.HasKey(key))
			{
				var path = PlayerPrefs.GetString(key);
				if (QFileManager.GetLastWriteTime(path) > lastTime)
				{
					Open(path);
				}
			}
		}
		private void OnEnable()
		{
			if (gridView == null)
			{
				gridView = new QGridView(GetValue, () => new Vector2Int
				{
					x = qdataList.TitleRow.Count,
					y = qdataList.Count,
				}, ClickCell);
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
		public void SetValue(int x, int y,string value)
		{
			if (y == 0 || typeInfo == null)
			{
				qdataList[y][x] = value;
			}
			else
			{
				var member = Members[x];
				if (member == null) return;
				var obj = objList[y - 1];
				member.Set(obj, value.ParseQDataType(member.Type, false));
			}
		}
		public bool ClickCell(int x,int y,int buttonIndex)
		{
			var change = false;
			if (buttonIndex == 0)
			{
				if (y == 0)
				{
					return false;
				}
				else if (typeInfo == null)
				{
					qdataList[y].SetValueType(QEidtCellWindow.Show(qdataList[y].Key + "." + qdataList.TitleRow[x], qdataList[y][x], typeof(string), out change, null), typeof(string), x);
				}
				else
				{
					var member = Members[x];
					if (member == null) return false;
					var obj = objList[y - 1];
					member.Set(obj, QEidtCellWindow.Show((obj as IKey<string>).Key + "." + member.QName, member.Get(obj), member.Type, out change, Members[x].MemeberInfo));
				}
			}
			else
			{
			
				if (y > 0)
				{
					var menu = new GenericMenu();
					menu.AddItem(new GUIContent("复制"), false, () =>
					{
						GUIUtility.systemCopyBuffer = GetValue(x, y);
					});
					menu.AddItem(new GUIContent("粘贴"), false, () =>
					{
						try
						{
							SetValue(x, y, GUIUtility.systemCopyBuffer);
							change = true;
							gridView.HasChanged = true;
						}
						catch (Exception e)
						{
							Debug.LogError(e);
						}
					});
					menu.AddItem(new GUIContent("清空"), false, () =>
					{
						try
						{
							SetValue(x, y, "");
							 change = true;
							gridView.HasChanged = true;
						}
						catch (Exception e)
						{
							Debug.LogError(e);
						}
					});
					if (x == 0)
					{
						menu.AddItem(new GUIContent("添加行"), false, () =>
						{
							AddAt(y);
							change = true;
							gridView.HasChanged = true;
						});
						menu.AddItem(new GUIContent("删除行"), false, () =>
						{
							RemoveAt(y);
							change = true;
							gridView.HasChanged = true;
						});
					}

					menu.ShowAsContext();
				}
			
			}

			return change;

		}
		public void OpenNull()
		{
			typeInfo = null;
			qdataList = null;
			Repaint();
		
			
		}
		private void OnGUI()
		{
			if (qdataList==null ) return;
			try
			{
				gridView.DoLayout(Repaint);
			}
			catch (Exception e)
			{

				Debug.LogError("表格出错：" + e);
				OpenNull();
			}
			
		}
	}
	
}
