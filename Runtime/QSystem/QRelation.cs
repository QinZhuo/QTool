using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool
{
	/// <summary>
	/// 关系
	/// </summary>
	public class QRelation : IKey<string>
	{
		public static QList<string, QRelation> Relations = new QList<string, QRelation>(() => new QRelation());
		public string Key { get; set; }
		private QRelation() { }
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
		public static float Get(string a, string b)
		{
			return Relations[a][b];
		}
	}
	/// <summary>
	/// 队伍
	/// </summary>
	public static class QTeam<T>
	{
		public static QDictionary<string, List<T>> Teams = new QDictionary<string, List<T>>((key)=>new List<T>());
		public static void GetList(string teamKey, List<T> list, float min = 0, float max = 10)
		{
			list.Clear();
			foreach (var team in Teams)
			{
				var relation = QRelation.Get(team.Key, teamKey);
				if (relation <= max && relation >= min)
				{
					list.AddRange(team.Value);
				}
			}
		}
	}
}
