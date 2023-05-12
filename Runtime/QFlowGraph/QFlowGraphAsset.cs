using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FlowGraph
{

	[CreateAssetMenu(menuName = nameof(QTool) + "/" + "流程图", fileName = "流程图")]
	public class QFlowGraphAsset : ScriptableObject
	{
		[SerializeField]
		public QFlowGraph Graph = new QFlowGraph(); 
		
		public override string ToString()
		{
			return name;
		}
	}
}

