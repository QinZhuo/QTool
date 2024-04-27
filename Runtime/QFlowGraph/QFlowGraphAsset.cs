using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace QTool.FlowGraph
{
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

