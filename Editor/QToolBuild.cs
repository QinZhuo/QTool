using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace QTool
{

	[InitializeOnLoad]
	public class QToolBuild : Editor, IPreprocessBuildWithReport, IPostprocessBuildWithReport
	{
		private void Awake()
		{
			BuildPlayerWindow.RegisterBuildPlayerHandler(
			buildPlayerOptions =>
			{
				QTool.IsBuilding = true;
//#if Addressable
//				AddressableAssetSettings.BuildPlayerContent();
//#endif
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
			QDebug.Log("开始打包[" + report.summary.platformGroup + "]" + report.summary.outputPath);
			PlayerPrefs.SetString(nameof(QToolBuild), report.summary.outputPath);
			var path = Path.GetDirectoryName(report.summary.outputPath);
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
						var path = QFileManager.DirectoryName(report.summary.outputPath);
						var tempPath = path + "/" + Application.productName + "_BackUpThisFolder_ButDontShipItWithYourGame";
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
			QDebug.Log("打包完成 " + report.summary.outputPath);
		}
	}
}
