using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace QTool
{

	public class QBuildPlayerTool : IPreprocessBuildWithReport, IPostprocessBuildWithReport
	{
		public int callbackOrder => 0;
		//打包前处理
		public void OnPreprocessBuild(BuildReport report)
		{
			QDebug.Log("开始打包[" + report.summary.platformGroup + "]" + report.summary.outputPath);
			PlayerPrefs.SetString(nameof(QBuildPlayerTool), report.summary.outputPath);
			var path = Path.GetDirectoryName(report.summary.outputPath);
			if (!Application.dataPath.StartsWith(path))
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
						var path = QFileTool.DirectoryName(report.summary.outputPath);
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
		public static void Build(BuildTarget buildTarget, string ScriptingDefine = null)
		{
			Build(buildTarget, BuildOptions.None, ScriptingDefine);
		}
		public static string GetBuildPath(BuildTarget buildTarget)
		{
			var path = nameof(Build) + "/" + buildTarget + "/" + Application.productName;
			switch (buildTarget)
			{
				case BuildTarget.StandaloneWindows:
				case BuildTarget.StandaloneWindows64:
					path += ".exe";
					break;
				case BuildTarget.StandaloneOSX:
					path += ".app";
					break;
				case BuildTarget.Android:
					path += ".apk";
					break;
				default:
					break;
			}
			return path;
		}
		public static void Build(BuildTarget buildTarget, BuildOptions buildOptions, string ScriptingDefine = null)
		{
			if (ScriptingDefine != null)
			{
				QEditorTool.PushCscRsp();
				QEditorTool.SetScriptingDefineSymbolsByCscRsp();
			}
			BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, GetBuildPath(buildTarget), buildTarget, buildOptions);
			if (ScriptingDefine != null)
			{
				QEditorTool.PopCscRsp();
			}
		}
	}
}
