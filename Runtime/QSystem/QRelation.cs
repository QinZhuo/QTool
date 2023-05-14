using System.Collections.Generic;

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
	public enum QTeamRelaction
	{
		队友 = 0,
		敌人 = 1,
	}
	public interface IQTeam
	{
		public string Team { get; }
	}
	/// <summary>
	/// 队伍
	/// </summary>
	public static class QTeam<T> where T:IQTeam
	{
		public static QDictionary<string, List<T>> Teams = new QDictionary<string, List<T>>((key)=>new List<T>());
		public static void GetRelactionList(string teamKey, List<T> list, QTeamRelaction relactionType = QTeamRelaction.敌人, float relactionValue = 0)
		{
			list.Clear();
			foreach (var team in Teams)
			{
				var relation = QRelation.Get(team.Key, teamKey);
				if (relactionType == QTeamRelaction.敌人)
				{
					if (relation < relactionValue)
					{
						list.AddRange(list);
					}
				}
				else
				{
					if (relation > relactionValue)
					{
						list.AddRange(list);
					}
				}
			}
		}
	}
	public static class QTeamTool
	{
		public static QTeamRelaction GetRelaction<T>(this T a,T b) where T:IQTeam
		{
			if (QRelation.Get(a.Team, b.Team) >= 0)
			{
				return QTeamRelaction.队友;
			}
			else
			{
				return QTeamRelaction.敌人;
			}
		}
		public static void TeamAdd<T>(this T a) where T : IQTeam
		{
			QTeam<T>.Teams[a.Team].AddCheckExist(a);
		}
		public static void TeamRemove<T>(this T a) where T : IQTeam
		{
			QTeam<T>.Teams[a.Team].Remove(a);
		}
		public static void GetRelactionList<T>(this T a, List<T> list, QTeamRelaction relactionType = QTeamRelaction.敌人, float relactionValue = 0) where T : IQTeam
		{
			QTeam<T>.GetRelactionList(a.Team, list, relactionType, relactionValue);
		}
	}
}
