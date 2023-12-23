using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QLobby:IKey<ulong>
	{
		public ulong Key { get; set; }
		public string Name => Data[nameof(QSteam.Name)];
		public ulong Owner { get; set; }
		public QList<ulong, QLobbyMember> Members { get; private set; } = new QList<ulong, QLobbyMember>();
		public int MemberLimit { get; set; }
		public QDictionary<string, string> Data { get;private set; } = new QDictionary<string, string>();

		public bool IsNull()
		{
			return Key == 0;
		}
		public T GetData<T>(string key, T defaultValue = default)
		{
			if (Data.ContainsKey(key))
			{
				return Data[key].ParseQData(defaultValue);
			}
			else
			{
				return defaultValue;
			}
		}
		public override string ToString()
		{
			var name = Name;
			if (name.IsNull())
			{
				name = Members[Owner].Name;
			}
			return name + " [" + Members.Count + "/" + MemberLimit + "]\n[" + Key + "]";
		}
		public string ToDetailString()
		{
			var dataStr = ToString();
			dataStr += Data.ToOneString(" ") + "\n";
			dataStr += Members.ToOneString(" ");
			return dataStr;
		}

		#region 静态
		public static QList<ulong, QLobby> List { get; private set; } = new QList<ulong, QLobby>();

		public static QLobby Current = new QLobby();
		public static void Leave()
		{
			Current = new QLobby();
		}

		public static Action OnUpdate = null;
		#endregion
	}

	public class QLobbyMember : IKey<ulong>
	{
		public ulong Key { get; set; }
		public string Name { get; set; }
		public QDictionary<string, string> Data { get; private set; } = new QDictionary<string, string>();
		public T GetData<T>(string key, T defaultValue = default)
		{
			if (Data.ContainsKey(key))
			{
				return Data[key].ParseQData(defaultValue);
			}
			else
			{
				return defaultValue;
			}
		}
		public override string ToString()
		{
			return Key.ToString();
		}
	}

}
