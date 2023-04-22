using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool
{
	/// <summary>
	/// 关系
	/// </summary>
	public class QRelation
	{
		public string Name { get; private set; }
		public QRelation(string name)
		{
			Name = name;
		}
		public float this[QRelation other]
		{
			get
			{
				if (other == null) return 0;
				return Relations[other];
			}
			set
			{
				if (other == null) return;
				Relations[other] = value;
				other[this] = value;
			}
		}
		private QDictionary<QRelation, float> Relations = new QDictionary<QRelation, float>();
	}
}
