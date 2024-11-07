using QTool.Graph;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool {
	[QEndNode]
	[Category("��Ϊ��/��Ϊ")]
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
		public override string Description => State== QNodeState.running? $"�ȴ� {(Time - startTime).ToString("0.0")}/{delay}s": $"�ȴ� {delay}s";
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