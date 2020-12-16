using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace QTool
{
    public static class QToolToolBar
    {
        [MenuItem("QTool/QTool/显示日志")]
        public static void SwitchLog()
        {
            UnityEngine.Debug.Log(PoolManager.ShowLog ? "显示PoolManager日志" : "隐藏PoolManager日志");
        }
    }
}

