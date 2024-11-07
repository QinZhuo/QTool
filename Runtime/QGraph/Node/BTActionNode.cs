using QTool.Graph;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool {
	[QEndNode]
	[Category("行为树/行为")]
	public abstract class BTActionNode : BTNode {

	}
	public class RandomPercent : BTActionNode {
		[Range(0f, 100f)]
		public float percent = 50;

		protected override void OnStart() {
			if (Random.Range(0, 100) <= percent) {
				End(true);
			}
			else {
				End(false);
			}
		}
	}
	public class Result : BTActionNode {
		public bool success;

		protected override void OnStart() {
			End(success);
		}
	}
	public class WaitTime : BTActionNode, INodeUpdate {
		public float delay = 1;
		private float startTime = 0;
		public override string Description => State== QNodeState.running? $"等待 {(Time - startTime).ToString("0.0")}/{delay}s": $"等待 {delay}s";
		protected override void OnStart() {
			startTime = Time;
		}
		public void OnUpdate() {
			if (Time - startTime >= delay) {
				End();
			}
		}
	}
}