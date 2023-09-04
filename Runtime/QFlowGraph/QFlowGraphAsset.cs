using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FlowGraph
{

	[CreateAssetMenu(menuName = nameof(QTool) + "/" + "QFlowGraph", fileName = "QFlowGraph")]
	public class QFlowGraphAsset : ScriptableObject
	{
		[SerializeField]
		public QFlowGraph Graph = new QFlowGraph(); 
		private void OnEnable()
		{
			Graph.Name = name;
		}
		public override string ToString()
		{
			return name;
		}
	}
}

