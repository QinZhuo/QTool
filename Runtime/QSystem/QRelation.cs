using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool
{
	/// <summary>
	/// 关系
	/// </summary>
	public class QRelation:IKey<string>
	{
		public static QList<string, QRelation> Relations = new QList<string, QRelation>(()=>new QRelation());
		public string Key { get;  set; }
		private QRelation()
		{

		}
		public QRelation(string name)
		{
			Key = name;
		}
		public float this[QRelation other]
		{
			get
			{
				if (other == null) return 0;
				return Values[other];
			}
			set
			{
				if (other == null) return;
				Values[other] = value;
				other[this] = value;
			}
		}
		private QDictionary<QRelation, float> Values = new QDictionary<QRelation, float>();
	}
}
