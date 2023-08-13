using QTool.Inspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Test
{
	public class QDataListTestType : QEffectData<QDataListTestType>
	{
		public override string Key { get; set; }
		[QName("数值")]
		[QPopup(nameof(QDataListTestType) + "." + nameof(List))]
		public string value = "";
		public Vector3 v3;
		public List<int> array;
		public TestEnum testEnum;
		public override string ToString()
		{
			return "[" + Key + "]:[" + value + "]";
		}
	}
}
