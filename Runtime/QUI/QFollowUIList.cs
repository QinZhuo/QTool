using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QFollowUIList : QObjectList
	{
		public void FreshPosition()
		{
			foreach (var obj in List)
			{
				obj.GetComponent<QFollowUI>()?.FreshPosition();
			}
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
				var obj = this[target.name+"_"+ target.gameObject.GetHashCode().ToString()];
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