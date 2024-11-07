using QTool.Graph;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QTool {

	[Category("行为树/复合")]
	public abstract class BTCompositeNode : BTNode, INodeUpdate {
		[QName, QOutputPort]
		public QFlow child;
		public int ChildCount => Ports[nameof(child)].Port.Connections[0].Count;

		protected override void OnStart() {
		}

		public abstract void OnUpdate();
		protected override void OnEnd() {
			for (int i = 0; i < ChildCount; i++) {
				var node = GetChild(i);
				node.End(false);
			}
		}
		protected int curIndex = 0;
		public QNodeRuntime GetChild(int index) {
			var portId = Ports[nameof(child)].Port.Connections[0][index];
			return Graph.Nodes[portId.node];
		}
		public void RunChild(int index) {
			curIndex = index;
			GetChild(index)?.Start();
		}
	}
	public class Selector : BTCompositeNode, INodeUpdate {
		protected override void OnStart() {
			RunChild(0);
		}
		public override void OnUpdate() {
			if (curIndex >= ChildCount) {
				End(false);
				return;
			}
			var node = GetChild(curIndex);
			switch (node.State) {
				case QNodeState.success:
					End();
					return;
				case QNodeState.fail:
					RunChild(curIndex + 1);
					break;
				default:
					return;
			}
		}
	}

	public class Sequence : BTCompositeNode, INodeUpdate {
		protected override void OnStart() {
			RunChild(0);
		}
		public override void OnUpdate() {
			if (curIndex >= ChildCount) {
				End();
				return;
			}
			var node = GetChild(curIndex);
			switch (node.State) {
				case QNodeState.success:
					RunChild(curIndex + 1);
					return;
				case QNodeState.fail:
					End(false);
					break;
				default:
					return;
			}
		}
	}
	public class RandomSelector : BTCompositeNode {
		private List<int> Indexs = new List<int>();
		protected override void OnStart() {
			Indexs.Clear();
			for (int index = 0; index < ChildCount; index++) {
				Indexs.Add(index);
			}
			RandomRun();
		}
		private void RandomRun() {
			if (Indexs.Count == 0) {
				End(false);
				return;
			}
			var i = UnityEngine.Random.Range(0, Indexs.Count);
			var index = Indexs[i];
			Indexs.RemoveAt(i);
			RunChild(index);
		}
		public override void OnUpdate() {
			var node = GetChild(curIndex);
			switch (node.State) {
				case QNodeState.success:
					End();
					return;
				case QNodeState.fail:
					RandomRun();
					break;
				default:
					return;
			}
		}
	}
	public class RandomSequence : BTCompositeNode {
		private List<int> Indexs = new List<int>();
		protected override void OnStart() {
			Indexs.Clear();
			for (int index = 0; index < ChildCount; index++) {
				Indexs.Add(index);
			}
			RandomRun();
		}
		private void RandomRun() {
			if (Indexs.Count == 0) {
				End();
				return;
			}
			var i = UnityEngine.Random.Range(0, Indexs.Count);
			var index = Indexs[i];
			Indexs.RemoveAt(i);
			RunChild(index);
		}
		public override void OnUpdate() {
			var node = GetChild(curIndex);
			switch (node.State) {
				case QNodeState.success:
					RandomRun();
					return;
				case QNodeState.fail:
					End(false);
					break;
				default:
					return;
			}
		}
	}
	public class Parallel : BTCompositeNode, INodeUpdate {
		protected override void OnStart() {
			RunPort(nameof(child));
		}
		public override void OnUpdate() {
			var runningCount = 0;
			for (int i = 0; i < ChildCount; i++) {
				var node = GetChild(i);
				if(node.State== QNodeState.running) {
					runningCount++;
				}
			}
			if (runningCount == 0) {
				End();
			}
		}
	}
	public class ParallelComplete : Parallel, INodeUpdate {
		public override void OnUpdate() {
			for (int i = 0; i < ChildCount; i++) {
				var node = GetChild(i);
				switch (node.State) {
					case QNodeState.success:
						End(true);
						break;
					case QNodeState.fail:
						End(false);
						break;
					default:
						break;
				}
			}
		}
	}
	public class ParallelSelector : Parallel, INodeUpdate {
		public override void OnUpdate() {
			var runningCount = 0;
			for (int i = 0; i < ChildCount; i++) {
				var node = GetChild(i);
				switch (node.State) {
					case QNodeState.running:
						runningCount++;
						break;
					case QNodeState.success:
						End();
						return;
					default:
						break;
				}
			}
			if (runningCount == 0) {
				End(false);
			}
		}
	}
	public class ParallelSequence : Parallel, INodeUpdate {
		public override void OnUpdate() {
			var runningCount = 0;
			for (int i = 0; i < ChildCount; i++) {
				var node = GetChild(i);
				switch (node.State) {
					case QNodeState.running:
						runningCount++;
						break;
					case QNodeState.fail:
						End(false);
						return;
					default:
						break;
				}
			}
			if (runningCount == 0) {
				End();
			}
		}
	}
}