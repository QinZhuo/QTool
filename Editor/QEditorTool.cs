using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using QTool.Reflection;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor.Callbacks;
using UnityEditor.Build;
using QTool.Inspector;
using System.IO.Compression;
using UnityEngine.UIElements;

#if Addressable
using UnityEditor.AddressableAssets.Settings;
#endif
namespace QTool
{
	[InitializeOnLoad]
	public static class QEditorTool
	{
		static QEditorTool()
		{
			Editor.finishedDefaultHeaderGUI += OnHeaderGUI;
			EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
		}
		static void OnHeaderGUI(Editor editor)
		{
			if (editor.target.IsAsset())
			{
				if (QIdTool.AssetIdCache.ContainsKey(editor.target))
				{
					GUILayout.Label("QId : " + QIdTool.AssetIdCache[editor.target]);
				}
			}
			else
			{
				if (editor.target is GameObject gameObj)
				{
					var qid = gameObj.GetComponent<QId>();
					if (qid != null)
					{
						GUILayout.Label("QId : " + qid.Id);
					}
				}
			}

		}
		static void OnHierarchyGUI(int instanceID, Rect rect)
		{
			if (EditorUtility.InstanceIDToObject(instanceID) is GameObject gameObj)
			{
				var qid = gameObj.GetComponent<QId>();
				if (qid != null)
				{
					GUI.Label(rect.HorizontalRect(0.5f, 1), "[" + qid.Id.ToShortString(5) + "]", QEditorGUI.RightLabel);
				}
			}
		}
		private const string CscRspPath = "Assets/csc.rsp";
		private static Stack<string> CscRspStack = new Stack<string>();
		public static void PushCscRsp()
		{
			CscRspStack.Push(QFileTool.Load(CscRspPath));
		}
		public static void PopCscRsp()
		{
			QFileTool.Save(CscRspPath, CscRspStack.Pop());
			//AssetDatabase.Refresh();
		}
		public static void SetScriptingDefineSymbolsByCscRsp(params string[] Symbols)
		{
			if (Symbols.Length == 0)
			{
				QFileTool.Save(CscRspPath, "");
			}
			else
			{
				QFileTool.Save(CscRspPath, "-define:" + Symbols.ToOneString(" "));
			}
			//AssetDatabase.Refresh();
		}
		[MenuItem("QTool/测试/终端命令")]
		public static void ProcessCommand()
		{
			EditorUtility.ClearProgressBar();
			QEditorWindow.Show("终端命令", window =>
			{
				Debug.LogError(window[0]);
				if (window[0].IsNull()) return;
				if (window[0].SplitTowString(" ", out var start, out var end))
				{
					QTool.ProcessCommand(start, end, "");
				}
			}, "命令");
		}
		[MenuItem("QTool/翻译/查看本地化信息")]
		public static void QLocalizationDataLog()
		{
			Debug.LogError(QLocalizationData.List.ToOneString());
			GUIUtility.systemCopyBuffer = QLocalizationData.List.ToOneString();
		}
		[MenuItem("QTool/翻译/翻译所有文件夹名")]
		public static void TranslateDirectory()
		{
			QEditorWindow.Show("翻译所有文件名", window =>
			{
				var rootPath = window[0];
				if (rootPath.IsNull()) return;
				rootPath.ForeachAllDirectory(async path =>
				{
					var name = path.SplitEndString("/");
					if (!name.Contains("_Q_"))
					{
						var newPath = path + "_Q_" + await name.NetworkTranslateAsync(SystemLanguage.English, SystemLanguage.Chinese);
						Directory.Move(path, newPath);
					}
				});
			}, "文件夹路径");
		}
		[MenuItem("QTool/翻译/翻译所有文件名")]
		public static void TranslateFile()
		{
			QEditorWindow.Show("翻译所有文件名", window =>
			{
				var rootPath = window[0];
				if (rootPath.IsNull()) return;
				rootPath.ForeachAllFiles(async path =>
				{
					var name = path.FileName();
					if (!name.Contains("_Q_"))
					{
						var newName = name + "_Q_" + await name.NetworkTranslateAsync(SystemLanguage.English, SystemLanguage.Chinese);
						path.FileRename(newName);
					}
				});
			}, "文件夹路径");
		}
		[MenuItem("QTool/测试/QId对象信息")]
		public static void QIdObjectInfo()
		{
			Debug.LogError(nameof(QId) + "信息 \n" + QId.InstanceIdList.ToOneString());
		}
		[MenuItem("QTool/测试/运行测试包")]
		private static void RunBuild()
		{
			var path = PlayerPrefs.GetString(nameof(QBuildPlayerTool));
			if (path.IsNull())
			{
				Debug.LogError(nameof(QBuildPlayerTool) + " 测试包路径为空");
				return;
			}
			try
			{
				System.Diagnostics.Process.Start(path);
			}
			catch (Exception e)
			{
				Debug.LogError("运行：" + path + "出错：\n" + e);
			}
		}
		[MenuItem("QTool/存档/清空存档")]
		public static void ClearSaveData()
		{
			PlayerPrefs.DeleteAll();
			Application.persistentDataPath.ClearData();
		}
		[MenuItem("QTool/存档/打开存档位置")]
		public static void OpenSaveData()
		{
			System.Diagnostics.Process.Start("explorer.exe", Application.persistentDataPath.Replace('/', '\\'));
		}
	}

	public class QEditorWindow : EditorWindow
	{
		public class Text
		{
			public string value;
		}
		private QList<Text> Texts = new QList<Text>(() => new Text());
		public string this[int index] => Texts[index].value;
		public static bool Show(string title, Action<QEditorWindow> comfirmAction, params string[] inputTextName)
		{
			var window = CreateInstance<QEditorWindow>();
			window.maxSize = new Vector2(300, 200);
			bool comfirm = false;
			window.titleContent = new GUIContent(title);
			window.rootVisualElement.Clear();
			for (int i = 0; i < inputTextName.Length; i++)
			{
				var text = window.Texts[i];
				window.rootVisualElement.AddText(inputTextName[i], window[i], value => text.value= value);
			}
			window.rootVisualElement.AddButton("确定", () => { comfirm = true; window.Close(); });
			window.ShowModal();
			if (comfirm)
			{
				comfirmAction(window);
			}
			return comfirm;
		}
	}



	[CustomPropertyDrawer(typeof(QVoxelData))]
	public class QVoxelDataDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var voxel= property.GetObject() as QVoxelData;
			EditorGUI.LabelField(position, label.text, voxel.MeshData?.ToString());
		}
		
	}
}

