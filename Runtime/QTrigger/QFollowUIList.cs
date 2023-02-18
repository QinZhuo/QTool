using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[RequireComponent(typeof(QObjectList))]
	public class QFollowUIList :MonoBehaviour
	{
		private QObjectList _ObjectList;
		public QObjectList ObjectList => _ObjectList ??= GetComponent<QObjectList>();
		public void Push(GameObject obj)
		{
			ObjectList.Push(obj);
		}
		public QFollowUI this[Transform target]
		{
			get
			{
				if (target == null)
				{
					Debug.LogError("follow目标为空");
					return null;
				}
				var obj = ObjectList[target.GetHashCode().ToString()];
				var followUI = obj.GetComponent<QFollowUI>();
				if (followUI != null)
				{
					followUI.Target = target;
					return followUI;
				}
				else
				{
					throw new System.Exception(target + " 目标不存在脚本 " + typeof(QFollowUI));
				}
			}
		}
	}

}