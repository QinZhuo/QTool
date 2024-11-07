using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Graph {
	public class QGraphController : QGraphAgent {

		[SerializeField]
		[QObject]
		private List<string> _graphAssets;
		protected virtual void OnEnable() {
			foreach (var asset in _graphAssets) {
				StartGraph(QGraph.Load(asset));
			}
		}
		protected virtual void OnDisable() {
			for (int i = Graphs.Count - 1; i >= 0; i--) {
				var graph = Graphs[i];
				graph.Stop(false);
			}
		}
	}
}
