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
        [MenuItem("QTool/QTool/显示日志")]
        public static void SwitchLog()
        {
            UnityEngine.Debug.Log(PoolManager.ShowLog ? "显示PoolManager日志" : "隐藏PoolManager日志");
        }
        [MenuItem("QTool/QTool/清空PlayerPrefs存档")]
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
        [MenuItem("QTool/QTool/打包测试当前场景 %R")]
        public static void TestBuild()
        {
            if (!BuildPipeline.isBuildingPlayer)
            {
                var buildOption = new BuildPlayerOptions
                {
                    scenes = new string[] { SceneManager.GetActiveScene().path },
                    locationPathName = "Builds/Windows/test.exe",
                    target = BuildTarget.StandaloneWindows,
                    options = BuildOptions.None,
                };
                var buildInfo = BuildPipeline.BuildPlayer(buildOption);
                if (buildInfo.summary.result == BuildResult.Succeeded)
                {
                    var path = buildInfo.summary.outputPath;
                    Debug.Log("打包成功" + path);
                    System.Diagnostics.Process.Start(path, "server");
                }
                else
                {
                    Debug.LogError("打包失败");
                }
            }
        }
    }
}

