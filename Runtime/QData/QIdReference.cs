using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QIdReference : ScriptableObject
	{
		[QReadonly]
		public string id;
		[QName("资源引用")]
		public Object obj;
		public override string ToString()
		{
			return "[" + id + "]:[" + obj + "]";
		}
#if UNITY_EDITOR
		private void OnEnable()
		{
			if (id.IsNullOrEmpty())
			{
				FreshId();
			}
		}
		[QName("刷新ID")]
		public void FreshId()
		{
			id= UnityEditor.AssetDatabase.GetAssetPath(obj);
		}
#endif
	}
}
