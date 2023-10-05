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
		public static float GetValue(string a, string b)
		{
			return Relations[a][b];
		}
		public static QTeamRelaction GetRelaction(string a,string b)
		{
			return GetValue(a, b) >= 0 ? QTeamRelaction.队友 : QTeamRelaction.敌人;
		}
	}
	public enum QTeamRelaction
	{
		队友 = 0,
		敌人 = 1,
	}
	public interface IQTeam
	{
		public string Team { get; }
		public UnityEngine.Transform transform { get; }
	}
	public static class QTeam
	{
		public static QDictionary<string, List<IQTeam>> Teams = new QDictionary<string, List<IQTeam>>((key) => new List<IQTeam>());
		public static QTeamRelaction GetRelaction<T>(this T a,T b) where T:IQTeam
		{
			if (QRelation.GetValue(a.Team, b.Team) >= 0)
			{
				return QTeamRelaction.队友;
			}
			else
			{
				return QTeamRelaction.敌人;
			}
		}
		public static void QTeamAdd<T>(this T a) where T : IQTeam
		{
			Teams[a.Team].AddCheckExist(a);
		}
		public static void QTeamRemove<T>(this T a) where T : IQTeam
		{
			Teams[a.Team].Remove(a);
		}
		public static void GetRelactionList<T>(this T a, List<T> list, QTeamRelaction relactionType = QTeamRelaction.敌人, float relactionValue = 0) where T : class, IQTeam
		{
			GetRelactionList(a.Team, list, relactionType, relactionValue);
		}
		public static void GetRelactionList<T>(string teamKey, List<T> list, QTeamRelaction relactionType = QTeamRelaction.敌人, float relactionValue = 0) where T : class, IQTeam
		{
			list.Clear();
			foreach (var team in Teams)
			{
				var relation = QRelation.GetValue(team.Key, teamKey);
				if (relation < relactionValue)
				{
					if (relactionType == QTeamRelaction.敌人)
					{
						foreach (var obj in team.Value)
						{
							if(obj is T t)
							{
								list.Add(t);
							}
						}
					}
				}
				else
				{
					if (relactionType == QTeamRelaction.队友)
					{
						foreach (var obj in team.Value)
						{
							if (obj is T t)
							{
								list.Add(t);
							}
						}
					}
				}
			}
		}
		public static void SortByDistance(this List<IQTeam> list, Vector3 center)
		{
			list.Sort((a, b) =>
			{
				var aDis = Vector2.Distance(a.transform.position, center);
				var bDis = Vector2.Distance(b.transform.position, center);
				if (aDis == bDis)
				{
					return 0;
				}
				else if (aDis > bDis)
				{
					return 1;
				}
				else
				{
					return -1;
				}
			});
		}
	}
}
