using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.FlowGraph;
namespace QTool
{
	public class QRandomPoint : MonoBehaviour
	{
		private void Reset()
		{
			key = name;
		}
		public string key;
		public bool CanCreate => transform.childCount == 0;
		private void OnDrawGizmosSelected()
		{
			Gizmos.color = key.ToColor();
			Gizmos.DrawSphere(transform.position, 0.2f);
		}
	}
	[QCommandType("基础/程序生成")]
	public static class QRandomNode

	{
		[QName("随机生成")]
		private static IEnumerator RandomCreate(QFlowNode This,[QInputPort("场景"),QFlowPort] GameObject root,[QName("位置点")]string pointKey,[QName("预制体")]GameObject prefab,[QName("创建数目")] int count = 1,[QOutputPort,QFlowPort] GameObject newObject=default)
		{
			var pointList = new List<QRandomPoint>(); 
			if (root == null)
			{
				pointList.AddRange(Object.FindObjectsOfType<QRandomPoint>());
			}
			else
			{
				pointList.AddRange(root.GetComponentsInChildren<QRandomPoint>());
			}
			pointList.RemoveAll((point) => !pointKey.Contains(point.key)||!point.CanCreate);
			pointList.Random();
			for (int i = 0; i < count&&i<pointList.Count; i++)
			{
				var obj= prefab.CheckInstantiate(pointList[i].transform);
				obj.transform.rotation = pointList[i].transform.rotation;
				if (This != null)
				{
					yield return This.RunPortIEnumerator(nameof(newObject));
				}
			}
		}
	}
}

