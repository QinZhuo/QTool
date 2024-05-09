using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QIdReference : ScriptableObject
	{
		[QName("资源引用")]
		public Object obj;
		public override string ToString()
		{
			return "[" + obj + "]";
		}
	}
}
