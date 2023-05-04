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
		public static float Get(string a,string b)
		{
			return Relations[a][b];
		}
		public string Key { get;  set; }
		private QRelation()
		{

		}
		public QRelation(string key)
		{
			Key = key;
		}
		public float this[string key]
		{
			get => this[Relations[key]];
			set => this[Relations[key]] = value;
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
				other.Values[this] = value;
			}
		}
		private QDictionary<QRelation, float> Values = new QDictionary<QRelation, float>();
	}
}