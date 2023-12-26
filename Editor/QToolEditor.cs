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
	public static class QToolEditor
	{
		static QToolEditor()
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
			var path = PlayerPrefs.GetString(nameof(QToolBuild));
			if (path.IsNull())
			{
				Debug.LogError(nameof(QToolBuild) + " 测试包路径为空");
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
	public class QLoactionWindow : EditorWindow
	{
		public SystemLanguage From = SystemLanguage.ChineseSimplified;
		public List<SystemLanguage> ToList = new List<SystemLanguage>() { SystemLanguage.English };
		[MenuItem("QTool/窗口/翻译")]
		public static void OpenWindow()
		{
			var window = GetWindow<QLoactionWindow>();
			window.minSize = new Vector2(400, 300);
		}
		private void CreateGUI()
		{
			QPlayerPrefs.Get(nameof(QLoactionWindow) + "_" + nameof(ToList), ToList);
			Label toText = null;
			rootVisualElement.AddEnum("当前语言", From, value => From = (SystemLanguage)value);
			var fromText = rootVisualElement.AddText("", "", null, true);

			rootVisualElement.Add("目标语言", ToList, typeof(List<SystemLanguage>), newList =>
			{
				ToList = (List<SystemLanguage>)newList;
				QPlayerPrefs.Set(nameof(QLoactionWindow) + "_" + nameof(ToList), ToList);
			});
			toText = rootVisualElement.AddLabel("");
			rootVisualElement.AddButton("翻译并添加本地化", async () =>
			{
				toText.text = "";
				if (!fromText.text.IsNull())
				{
					var fromDataList = QLocalizationData.LoadQDataList(From.ToString());
					var key = fromText.text.ToKeyString();
					var fromData = fromDataList[key];
					fromData[TitleKey] = fromText.text;
					foreach (var To in ToList)
					{
						if (From == To) continue;
						var toDataList = QLocalizationData.LoadQDataList(To.ToString());
						await CheckTranslate(fromData, toDataList, To);
						toText.text += toDataList[key][TitleKey];
						toDataList.Save();
					}
					fromDataList.Save();
					GUIUtility.systemCopyBuffer = toText.text;
					AssetDatabase.Refresh();
				}
			});
			rootVisualElement.AddButton("翻译全部" + nameof(QLocalization) + "本地化信息", QLocalizationTranslate);
		}
		public async void QLocalizationTranslate()
		{
			var fromDataList = QLocalizationData.LoadQDataList(From.ToString());
			foreach (var To in ToList)
			{
				if (From == To) continue;
				var toDataList = QLocalizationData.LoadQDataList(To.ToString());
				foreach (var data in fromDataList)
				{
					await CheckTranslate(data, toDataList, To);
				}
				toDataList.Save();
			}
			QDebug.Log("翻译完成");
			AssetDatabase.Refresh();
		}
		private const string TitleKey = nameof(QLocalizationData.Localization);
		private async Task CheckTranslate(QDataRow fromData, QDataList toDataList, SystemLanguage toLanguage)
		{
			var toDataValue = toDataList[fromData.Key][TitleKey];
			if (!fromData[TitleKey].IsNull() && (toDataValue.IsNull() || toDataValue.EndsWith(QLocalizationData.AutoTranslateEndKey)))
			{
				toDataList[fromData.Key][TitleKey] = await fromData[TitleKey].NetworkTranslateAsync(From, toLanguage) + QLocalizationData.AutoTranslateEndKey;
				QDebug.Log("翻译[" + fromData[TitleKey] + "] => " + toLanguage + " => [" + toDataList[fromData.Key][TitleKey] + "]");
				await Task.Delay(100);
			}
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
	[InitializeOnLoad]
	public class QToolBuild:Editor, IPreprocessBuildWithReport, IPostprocessBuildWithReport
	{
		private void Awake()
		{
			BuildPlayerWindow.RegisterBuildPlayerHandler(
	buildPlayerOptions => {
			QTool.IsBuilding = true;
#if Addressable
			AddressableAssetSettings.BuildPlayerContent();
#endif
			BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(buildPlayerOptions);
			QTool.IsBuilding = false;
		});
		}
		public static string BuildPath => Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")) + "Builds/" + EditorUserBuildSettings.selectedBuildTargetGroup + "/" + PlayerSettings.productName + "_v" + PlayerSettings.bundleVersion.Replace(".", "_");

		public int callbackOrder => 0;
		bool CheckBuildPath(string path)
		{
			return !Application.dataPath.StartsWith(path);
		}
		//打包前处理
		public void OnPreprocessBuild(BuildReport report)
		{
			QDebug.Log("开始打包["+report.summary.platformGroup+"]" + report.summary.outputPath);
			PlayerPrefs.SetString(nameof(QToolBuild), report.summary.outputPath);
			var path= Path.GetDirectoryName(report.summary.outputPath);
			if (!CheckBuildPath(path))
			{
				if (Directory.Exists(path))
				{
					QDebug.Log("删除打包路径下文件 " + path);
					Directory.Delete(path, true);
				}
				Directory.CreateDirectory(path);
			}
		}
		//打包后处理
		public void OnPostprocessBuild(BuildReport report)
		{
			switch (report.summary.platformGroup)
			{
				case BuildTargetGroup.Standalone:
					{
						var path= QFileManager.DirectoryName(report.summary.outputPath);
						var tempPath = path+"/"+Application.productName+ "_BackUpThisFolder_ButDontShipItWithYourGame";
						if (Directory.Exists(tempPath))
						{
							Directory.Delete(tempPath, true);
						}
						if (!Debug.isDebugBuild)
						{
							tempPath = path + "/steam_appid.txt";
							if (File.Exists(tempPath))
							{
								File.Delete(tempPath);
							}
						}
					}
					break;
				default:
					break;
			}
			var versions = PlayerSettings.bundleVersion.Split('.');
			if (versions.Length > 0)
			{
				versions[versions.Length - 1] = (int.Parse(versions[versions.Length - 1]) + 1).ToString();
			}
			PlayerSettings.bundleVersion = versions.ToOneString(".");
			QEventManager.InvokeEvent("游戏版本", PlayerSettings.bundleVersion);
			QDebug.Log("打包完成 "+ report.summary.outputPath);
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

