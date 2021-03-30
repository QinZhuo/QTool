using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QTool
{
    public static class QToolToolBar
    {
        [MenuItem("QTool/Tool/显示日志")]
        public static void SwitchLog()
        {
            ToolDebug.ShowLog = !ToolDebug.ShowLog;
            UnityEngine.Debug.Log(ToolDebug.ShowLog ? "显示PoolManager日志" : "隐藏PoolManager日志");
        }
        [MenuItem("QTool/Tool/清空PlayerPrefs存档")]
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
        public static string BasePath
        {
            get
            {
                return Application.dataPath.Substring(0, Application.dataPath.IndexOf("Assets")) ;
            }
        }
        public static string WindowsLocalPath
        {
            get
            {
                return "Builds/Windows/test.exe";
            }
        }
        [MenuItem("QTool/Tool/运行测试包 %T")]
        public static void RunTest()
        {
            System.Diagnostics.Process.Start(BasePath + WindowsLocalPath);
        }
        [MenuItem("QTool/Tool/打包测试当前场景 %#T")]
        public static void TestBuild()
        {
            if (!BuildPipeline.isBuildingPlayer)
            {
                var buildOption = new BuildPlayerOptions
                {
                    scenes = new string[] { SceneManager.GetActiveScene().path },
                    locationPathName = WindowsLocalPath,
                    target = BuildTarget.StandaloneWindows,
                    options = BuildOptions.None,
                };
                var buildInfo = BuildPipeline.BuildPlayer(buildOption);
                if (buildInfo.summary.result == BuildResult.Succeeded)
                {
                    Debug.Log("打包成功" + BasePath+WindowsLocalPath);
                    System.Diagnostics.Process.Start(BasePath + WindowsLocalPath);
                }
                else
                {
                    Debug.LogError("打包失败");
                }
            }
        }
       
    }
}

