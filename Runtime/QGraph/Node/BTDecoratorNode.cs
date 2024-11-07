using QTool.Graph;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QTool {
	
	[Category("行为树/修饰")]
	public abstract class BTDecoratorNode : BTNode, INodeUpdate {
		[QName, QOutputPort, QFlowPort(onlyOneConnection = true)]
		public QFlow child;
		protected override void OnStart() {
			RunPort(nameof(child));
		}
		private QNodeRuntime GetChild() {
			var portId = Ports[nameof(child)].Port.Connections[0][0];
			return Graph.Nodes[portId.node];
		}
		public QNodeState GetChildState() {
			var node = GetChild();
			if (node == null)
				return QNodeState.success;
			return node.State;
		}
		protected override void OnEnd() {
			GetChild()?.End(false);
		}

		public abstract void OnUpdate();
	}
	public class Invert : BTDecoratorNode {
		public override void OnUpdate() {
			switch (GetChildState()) {
				case QNodeState.success:
					End(false);
					break;
				case QNodeState.fail:
					End(true);
					break;
				default:
					break;
			}
		}
	}
	public class TimeLimit : BTDecoratorNode {
		public float timeLimit = 1;
		private float startTime = 0;
		public override string Description => $"{(Time - startTime).ToString("f1")}/{timeLimit}";
		protected override void OnStart() {
			startTime = timeLimit;
			RunPort(nameof(child));
		}
		public override void OnUpdate() {
			switch (GetChildState()) {
				case QNodeState.success:
					End(true);
					break;
				case QNodeState.fail:
					End(false);
					break;
				case QNodeState.running:
					if (Time - startTime > timeLimit) {
						End(false);
					}
					break;
				default:
					break;
			}
		}
	}
	public class Cooldown : BTDecoratorNode {
		public float cooldown = 1;
		private float lastTime = 0;
		public override string Description => $"{(Time-lastTime).ToString("f1")}/{cooldown}";
		protected override void OnStart() {
			if (Time - lastTime >= cooldown) {
				lastTime = Time;
				RunPort(nameof(child));
			}
			else {
				End(false);
			}
		}
		public override void OnUpdate() {
			switch (GetChildState()) {
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
	public class Guard : BTDecoratorNode {
		public string tag;
		protected override void OnStart() {
			if (!Graph.GetVar<string>(tag).IsNull()) {
				End(false);
			}
			else {
				Graph.SetVar(tag, Node.Key);
				RunPort(nameof(child));
			}
		}
		public override void OnUpdate() {
			switch (GetChildState()) {
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
		protected override void OnEnd() {
			base.OnEnd();
			if (Graph.GetVar<string>(tag) == Node.Key) {
				Graph.SetVar(tag, "");
			}
		}
	}
	public class TimesFilter : BTDecoratorNode {
		public int times = 2;
		private int curTimes = 0;
		public override string Description => $"{curTimes}/{times}";
		protected override void OnStart() {
			curTimes++;
			if (curTimes >= times) {
				curTimes = 0;
				RunPort(nameof(child));
			}
			else {
				End(false);
			}
		}
		public override void OnUpdate() {
			switch (GetChildState()) {
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
	[QOldName("ConditionStop")]
	public class Condition : BTDecoratorNode {
		[QInputPort] public bool condition = true;
		[QInputPort] public bool checkStop = false;
		protected override void OnStart() {
			if (condition) {
				RunPort(nameof(child));
			}
			else {
				End(false);
			}
		}
		public override void OnUpdate() {
			switch (GetChildState()) {
				case QNodeState.success:
					End(true);
					break;
				case QNodeState.fail:
					End(false);
					break;
				case QNodeState.running:
					if (checkStop) {
						FreshPortValue(nameof(condition));
						if (!condition) {
							End(false);
						}
					}
					break;
				default:
					break;
			}
		}
	}
	public class ForceResult : BTDecoratorNode {
		public bool success;
		public override void OnUpdate() {
			switch (GetChildState()) {
				case QNodeState.success:
				case QNodeState.fail:
					End(success);
					break;
				default:
					break;
			}
		}
	}

	public class RepeatTimes : BTDecoratorNode {
		public int times = 2;
		[QName]
		private int curTimes { get; set; } = 0;
		public override string Description => $"重复{curTimes}/{times}";
		protected override void OnStart() {
			RunPort(nameof(child));
			curTimes = 1;
		}
		public override void OnUpdate() {
			switch (GetChildState()) {
				case QNodeState.success:
					if(curTimes< times) {
						RunPort(nameof(child));
						curTimes++;
					}
					else {
						End(true);
					}
					break;
				case QNodeState.fail:
					if (curTimes < times) {
						RunPort(nameof(child));
						curTimes++;
					}
					else {
						End(false);
					}
					break;
				default:
					break;
			}
		}
	}
	public class RepeatUntil: BTDecoratorNode {
		public enum EndState {
			success = 0,
			fail = 1,
		}
		public EndState state = EndState.success;
		public override void OnUpdate() {
			var curState = GetChildState();
			switch (GetChildState()) {
				case QNodeState.success:
				case QNodeState.fail:
					if ((int)curState == (int)state) {
						End(true);
						break;
					}
					else {
						OnStart();
					}
					break;
				default:
					break;
			}
		}
	}
}