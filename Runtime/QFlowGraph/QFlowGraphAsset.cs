using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FlowGraph
{
  
    public class QFlowGraphAsset : ScriptableObject
    {
		[SerializeField]
		public QFlowGraph Graph;
        public void Init(string qsmStr)
        {
			Graph= qsmStr.ParseQData(Graph);
			Graph.SerializeString = qsmStr;
		}
		public override string ToString()
		{
			return name;
		}
		public void Save()
        {
            try
            {
#if UNITY_EDITOR
				Graph.Name = name;
				Graph.OnBeforeSerialize();
				QFileManager.Save(UnityEditor.AssetDatabase.GetAssetPath(this), Graph.SerializeString);
				if (!Application.isPlaying)
				{
					UnityEditor.AssetDatabase.Refresh();
				}
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError(name + " 储存出错 :" + e);
                return;
            }

        }
      
    }
}

