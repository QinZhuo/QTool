using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FlowGraph
{
  
	[CreateAssetMenu(menuName = nameof(QTool)+"/"+nameof(QFlowGraphAsset))]
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
				var path = UnityEditor.AssetDatabase.GetAssetPath(this);
				if (!path.EndsWith(".asset"))
				{
					QFileManager.Save(path, Graph.SerializeString);
					if (!Application.isPlaying)
					{
						UnityEditor.AssetDatabase.Refresh();
					}
				}
				else
				{
					this.SetDirty();
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

