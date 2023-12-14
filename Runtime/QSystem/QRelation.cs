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
	[System.Flags] 
	public enum QTeamRelaction
	{  
		自己 = 1 << 1,
		队友 = 1 << 2,
		敌人 = 1 << 3,
	}
	public interface IQTeam : IKey<string>
	{
		public string Team { get; }
		public Transform transform { get; }
	}
	public static class QTeam
	{
		static QTeam()
		{
			QEventManager.Register(QToolEvent.游戏退出,Teams.Clear);
		}
		public static QDictionary<string, List<IQTeam>> Teams = new QDictionary<string, List<IQTeam>>((key) => new List<IQTeam>());

		public static QTeamRelaction GetRelaction<T>(this T a, T b) where T : IQTeam
		{
			if (a.Equals(b))
			{
				return QTeamRelaction.自己;
			}
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
			list.Clear();
			foreach (var team in Teams)
			{
				if (team is T b)
				{
					if (relactionType.HasFlag(a.GetRelaction(b)))
					{
						list.Add(b);
					}
				}
			}
		}
	
		public static void SortByDistance<T>(this List<T> list, Vector3 center) where T : IQTeam
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
