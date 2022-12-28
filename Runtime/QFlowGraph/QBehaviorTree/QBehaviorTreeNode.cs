using QTool.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FlowGraph
{
	[QCommandType("行为树")]
	public static class QBehaviorTreeNode
	{
		#region 修饰节点
		[QName("修饰/取反")]
		public static IEnumerator Inverse(QFlowNode This, [QOutputPort] QFlow next)
		{
			var nextNode = This.GetConnectNode(nameof(next));
			yield return This.RunPortIEnumerator(nameof(next));
			if (nextNode.State == QNodeState.成功)
			{
				This.State = QNodeState.失败;
			}
			else
			{
				This.State = QNodeState.成功;
			}
		}

		public enum LoopMode
		{
			任意,
			直到成功,
			直到失败,
		}
		[QName("修饰/循环")]
		public static IEnumerator Loop(QFlowNode This, [QOutputPort] QFlow next, [QName("循环模式")] LoopMode loopMode, [QName("最大次数")] int maxTimes = -1)
		{
			if (maxTimes > 0)
			{
				for (int i = 0; i < maxTimes; i++)
				{
					var nextNode = This.GetConnectNode(nameof(next));
					yield return This.RunPortIEnumerator(nameof(next));
					switch (loopMode)
					{
						case LoopMode.直到成功:
							if (nextNode.State == QNodeState.成功)
							{
								yield break;
							}
							break;
						case LoopMode.直到失败:
							if (nextNode.State == QNodeState.失败)
							{
								yield break;
							}
							break;
						default:
							break;
					}
					yield return QFlowGraph.Step;
				}
				This.State = QNodeState.失败;
			}
			else
			{
				while (true)
				{
					var nextNode = This.GetConnectNode(nameof(next));
					yield return This.RunPortIEnumerator(nameof(next));
					switch (loopMode)
					{
						case LoopMode.直到成功:
							if (nextNode.State == QNodeState.成功)
							{
								yield break;
							}
							break;
						case LoopMode.直到失败:
							if (nextNode.State == QNodeState.失败)
							{
								yield break;
							}
							break;
						default:
							break;
					}
					yield return QFlowGraph.Step;
				}
			}
		}

		#endregion

		#region 复合节点

		[QName("复合/选择")]
		public static IEnumerator Selector(QFlowNode This,[QOutputPort]List<QFlow> nexts)
		{
			for (int i = 0; i < nexts.Count; i++)
			{
				var nextNode= This.GetConnectNode(nameof(nexts), i);
				if (nextNode != null)
				{
					yield return This.RunPortIEnumerator(nameof(nexts), i);
					if(nextNode.State== QNodeState.成功)
					{
						yield break;
					}
				}
			}
			This.State = QNodeState.失败;
		}
		[QName("复合/顺序")]
		public static IEnumerator Sequence(QFlowNode This, [QOutputPort] List<QFlow> nexts)
		{
			for (int i = 0; i < nexts.Count; i++)
			{
				var nextNode = This.GetConnectNode(nameof(nexts), i);
				if (nextNode != null)
				{
					yield return This.RunPortIEnumerator(nameof(nexts), i);
					if (nextNode.State == QNodeState.失败)
					{
						This.State = QNodeState.失败;
						yield break;
					}
				}
			}
		}
		[QName("复合/并行")]
		public static IEnumerator Parallel(QFlowNode This, [QOutputPort] List<QFlow> nexts)
		{
			var nodes = new List<QFlowNode>();
			
			for (int i = 0; i < nexts.Count; i++)
			{
				var node= This.GetConnectNode(nameof(nexts), i);
				if (node != null)
				{
					This.RunPort(nameof(nexts), i);
					nodes.Add(node);
				}
			}
			while (true)
			{
				bool allSucess = true;
				foreach (var item in nodes)
				{
					switch (item.State)
					{
						case QNodeState.成功:
							break;
						case QNodeState.失败:
							{
								This.State = QNodeState.失败;
								yield break;
							}
						default:
							allSucess = false;
							break;
					}
				}
				if (allSucess)
				{
					yield break;
				}
			}
		}
		#endregion

		#region 任务节点
		[QName("任务/结果"), QEndNode]
		public static void Result(QFlowNode This, bool sucess = true)
		{
			if (sucess)
			{
				This.State = QNodeState.成功;
			}
			else
			{
				This.State = QNodeState.失败;
			}
		}
		#endregion
	}
}
