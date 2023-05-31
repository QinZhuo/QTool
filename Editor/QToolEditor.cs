using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using QTool.Asset;
using QTool.Reflection;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor.Callbacks;
using UnityEditor.Build;
using QTool.Inspector;
using System.IO.Compression;

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
				if(editor.target is GameObject gameObj)
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
					try
					{
						GUILayout.BeginArea(rect);
						GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						GUILayout.Label("[" + qid.Id.ToShortString(5) + "]");
						GUILayout.EndHorizontal();
						GUILayout.EndArea();
					}
					catch (Exception) { }
				}
			}
		}
		[MenuItem("QTool/翻译/查看翻译语言信息")]
		public static void LanguageTest()
		{
			Debug.LogError(global::QTool.QTranslate.QTranslateData.ToString());
			GUIUtility.systemCopyBuffer = global::QTool.QTranslate.QTranslateData.ToString();
		}
		[MenuItem("QTool/翻译/生成自动翻译文件")]
		public static async void AutoTranslate() 
		{
			var newData = new QDataList();
			newData.SetTitles(QTranslate.QTranslateData.TitleRow.ToArray());
			QDictionary<string, string> keyCache = new QDictionary<string, string>();
			
			for (int rowIndex = 0; rowIndex < QTranslate.QTranslateData.Count; rowIndex++)
			{
				var data = QTranslate.QTranslateData[rowIndex];
				for (int i = 2; i < data.Count; i++)
				{
					keyCache.Clear();
					var text =  data[1];
					keyCache[ "%".GetHashCode().ToString()] = "%";
					text = text.Replace("%", "[" + "%".GetHashCode() + "]");
					text = text.ForeachBlockValue('{', '}', (key) => { var value = key.GetHashCode().ToString();keyCache[value] ="{"+ key+"}";   return "["+value+"]"; });
					text = text.ForeachBlockValue('<', '>', (key) => { var value = key.GetHashCode().ToString(); keyCache[value] ="<" +key+">"; return "["+value+"]"; });
					var key = QTranslate.QTranslateData.TitleRow[i];

					if (!text.IsNull() && data[i].IsNull() && !QTranslate.QTranslateData.TitleRow[i].IsNull()&&!key.IsNull()&& QTranslate.TranslateKeys.ContainsKey(key))
					{  
						var language = QTranslate.GetTranslateKey(key);
						var newLine = newData[data[0]];
						newLine[1] = data[1];
					    var translateText = "#" + await text.NetworkTranslateAsync(language.WebAPI);
						translateText = translateText.ForeachBlockValue('[', ']', (key) => keyCache.ContainsKey(key)?keyCache[key]:key );
						newLine[i] = translateText;
						QDebug.Log("翻译" + language.Key + " " + rowIndex + "/" + QTranslate.QTranslateData.Count + " " + " [" + data[1] + "]=>[" + newLine[i] + "]");
					}
				}
			}
			Debug.LogError(newData.ToString());
			newData.Save(QDataList.GetModPath( nameof(QTranslate.QTranslateData),nameof(AutoTranslate)));
		}
		[MenuItem("QTool/测试/QId对象信息")]
		public static void QIdObjectInfo()
		{
			Debug.LogError(nameof(QId) + "信息 \n" + QId.InstanceIdList.ToOneString());
		}
		[MenuItem("QTool/测试/QPool对象池信息")]
		public static void QPoolInfo()
		{
			foreach (var pool in QPoolManager.Pools)
			{
				Debug.LogError(pool);
			}
		}
		[MenuItem("QTool/测试/运行测试包")]
		private static void RunBuild()
		{
			var path= PlayerPrefs.GetString(nameof(QToolBuild));
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
		[MenuItem("QTool/存档/清空全部")]
        public static void ClearSaveData()
        {
            ClearPlayerPrefs();
            ClearPersistentData();
        }
        [MenuItem("QTool/存档/清空PlayerPrefs")]
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
        [MenuItem("QTool/存档/清空PersistentData")]
        public static void ClearPersistentData()
        {
            Application.persistentDataPath.ClearData();
		}
		[MenuItem("QTool/存档/打开存档位置")]
		public static void OpenSaveData()
		{
			System.Diagnostics.Process.Start("explorer.exe", Application.persistentDataPath.Replace('/','\\'));
		}

		#region OldBuild

		//		public static string Build( string[] scenes,BuildOptions options= BuildOptions.None)
		//		{
		//			BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
		//			if (!BuildPipeline.isBuildingPlayer)
		//			{
		//				var startTime = DateTime.Now;


		//#if Addressable
		//				UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent(out var result);
		//				if(string.IsNullOrWhiteSpace(result.Error))
		//				{
		//					QDebug.Log("Addressable Build 完成 ：" + result.Duration+"s");
		//				}
		//				else
		//				{
		//					Debug.LogError("Addressable Build 失败 ："+result.Error);
		//					return "";
		//				}
		//#endif
		//				var buildOption = new BuildPlayerOptions
		//				{
		//					scenes = scenes,
		//					locationPathName = GetBuildPath(),
		//					target = buildTarget,
		//					options = options,
		//				};
		//				var buildInfo = BuildPipeline.BuildPlayer(buildOption);
		//				if (buildInfo.summary.result == BuildResult.Succeeded)
		//				{
		//					QDebug.Log("打包成功" + buildOption.locationPathName);
		//					QDebug.Log("打包用时：" + Math.Ceiling((DateTime.Now - startTime).TotalMinutes) + " 分钟");
		//					var versions = PlayerSettings.bundleVersion.Split('.');
		//					if (versions.Length > 0)
		//					{
		//						versions[versions.Length - 1] = (int.Parse(versions[versions.Length - 1]) + 1).ToString();
		//					}
		//					if(!options.HasFlag(BuildOptions.Development))
		//					{
		//						PlayerSettings.bundleVersion = versions.ToOneString(".");
		//						QEventManager.Trigger("游戏版本", PlayerSettings.bundleVersion);
		//					}
		//					var tempPath = buildOption.locationPathName.SplitStartString(".exe") + "_BackUpThisFolder_ButDontShipItWithYourGame";
		//					if (Directory.Exists(tempPath))
		//					{
		//						Directory.Delete(tempPath, true); 
		//					}
		//					return buildOption.locationPathName;
		//				}
		//				else
		//				{
		//					Debug.LogError("打包失败 "+GetBuildPath());
		//				}
		//			}
		//			return "";
		//		}

		//      [MenuItem("QTool/打包/打包发布版")]
		//      public static void BuildRun()
		//{
		//	var sceneList = new List<string>();
		//	foreach (var scene in EditorBuildSettings.scenes)
		//	{
		//		sceneList.AddCheckExist(scene.path);
		//	}
		//	PlayerPrefs.SetString("QToolBuildPath", Build(sceneList.ToArray()));
		//	RunBuild();
		//}
		//[MenuItem("QTool/打包/打包开发版")]
		//public static void BuildDevelopmentRun()
		//{
		//	var sceneList = new List<string>();
		//	foreach (var scene in EditorBuildSettings.scenes)
		//	{
		//		sceneList.AddCheckExist(scene.path);
		//	}
		//	PlayerPrefs.SetString("QToolBuildPath", Build(sceneList.ToArray(), BuildOptions.Development));
		//	RunBuild();
		//}
		//[MenuItem("QTool/打包/打包当前场景")]
		//public static void BuildRandRunScene()
		//{
		//	PlayerPrefs.SetString("QToolBuildPath", Build(new string[] { SceneManager.GetActiveScene().path }, BuildOptions.Development));
		//	RunBuild();
		//}
		//[MenuItem("QTool/打包/运行测试包")]
		//private static void RunBuild()
		//{
		//	var path = PlayerPrefs.GetString("QToolBuildPath", GetBuildPath());
		//	try
		//	{
		//		System.Diagnostics.Process.Start(path);
		//	}
		//	catch (Exception e)
		//	{
		//		Debug.LogError("运行：" + path + "出错：\n" + e);
		//	}
		//}


		#endregion

	}
	[InitializeOnLoad]
	public class QToolBuild:Editor, IPreprocessBuildWithReport, IPostprocessBuildWithReport
	{
		static QToolBuild()
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
						var path= QFileManager.GetFolderPath(report.summary.outputPath);
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
			QEventManager.Trigger("游戏版本", PlayerSettings.bundleVersion);
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

