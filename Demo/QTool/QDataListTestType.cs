using QTool.Inspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Test
{
	public class QDataListTestType : QDataList<QDataListTestType>, IKey<string>
	{
		public string Key { get; set; }
		[QName("数值")]
		[QEnum(nameof(QDataListTestType) + "." + nameof(List))]
		public string value = "";
		public Vector3 v3;
		public List<int> array;
		public TestEnum testEnum;
		[QName("效果"),TextArea(2,4)]
		public string textArea;
		public override string ToString()
		{
			return Key + ":[" + value + "]";
		}
	}
}
