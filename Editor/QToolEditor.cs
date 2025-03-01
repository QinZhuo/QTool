using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using QTool.Reflection;
//#if Addressable
//using UnityEditor.AddressableAssets.Settings;
//#endif
namespace QTool
{
	[InitializeOnLoad]
	public static class QToolEditor
	{
		private const string CscRspPath = "Assets/csc.rsp";
		private static Stack<string> CscRspStack = new Stack<string>();
		public static void PushCscRsp()
		{
			CscRspStack.Push(QFileTool.Load(CscRspPath));
		}
		public static void PopCscRsp()
		{
			var data = CscRspStack.Pop();
			if (data.IsNull())
			{
				QFileTool.FileDelete(CscRspPath);
			}
			else
			{
				QFileTool.Save(CscRspPath, data);
			}
		}
		public static void SetScriptingDefineSymbolsByCscRsp(string Symbols)
		{
			if (Symbols.IsNull())
			{
				QFileTool.FileDelete(CscRspPath);
			}
			else
			{
				QFileTool.Save(CscRspPath, "-define:" + Symbols);
			}
		}
		
		[MenuItem("QTool/命令/终端命令")]
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
			Debug.LogError(QDataTable<QLocalizationData>.DataTable.ToOneString());
			GUIUtility.systemCopyBuffer = QDataTable<QLocalizationData>.DataTable.ToOneString();
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
		
		[MenuItem("QTool/打包/运行包")]
		public static void RunBuild()
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

