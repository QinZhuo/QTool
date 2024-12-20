using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QLobby : IKey<ulong>
	{
		public ulong Key { get; set; }
		public string Name { get; private set; }
		public QLobbyState State { get;private set; }
		public ulong Owner { get; set; }
		public ulong[] FixedMembers { get; private set; } = new ulong[0];
		[QIgnore]
		public QList<ulong, QLobbyMember> Members { get; private set; } = new QList<ulong, QLobbyMember>(() => new QLobbyMember());
		public int MemberLimit { get; set; }
		public QDictionary<string, string> Data { get; private set; } = new QDictionary<string, string>();
		public QLobbyMember this[ulong playerId]
		{
			get
			{
				return Members[playerId];
			}
		}
		public void Update()
		{
			Name = GetData(nameof(Name), Name);
			State = GetData(nameof(State), QLobbyState.组队);
			FixedMembers = GetData(nameof(FixedMembers), FixedMembers);
			foreach (var memeber in Members)
			{
				memeber.Update();
			}
			if (this == CurrentLobby)
			{
				OnLobbyUpdate?.Invoke();
			}
			QDebug.Log(nameof(QLobby) + "." + nameof(Update) + " " + this + "\n" + Members.ToOneString());
		}
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
			else if (Data.ContainsKey(key.ToLower()))
			{
				return Data[key.ToLower()].ParseQData(defaultValue);
			}
			else
			{ 
				return defaultValue;
			}
		}
		public override string ToString()
		{
			return "【" + Name + "】(" + Key + ") [" + Members.Count + "/" + MemberLimit + "] 【" + Owner + "】" + Data.ToOneString(" ", kv => kv.Key + ":" + kv.Value);
		}
		#region 静态

		public static QLobby CurrentLobby = new QLobby();
		public static ulong LastLobbyKey { get => QPlayerPrefs.Get<ulong>(nameof(LastLobbyKey)); set => QPlayerPrefs.Set<ulong>(nameof(LastLobbyKey), value); }
		public static QList<ulong, QLobby> LobbyList { get; private set; } = new QList<ulong, QLobby>(() => new QLobby());
		public static void Leave()
		{
			CurrentLobby = new QLobby();
		} 
		public static event Action OnLobbyUpdate = null;
		#endregion
	
	}
	public class QLobbyMember : IKey<ulong>
	{
		public ulong Key { get; set; }
		public string Name { get; set; }
		public QLobbyState State { get; set; }
		public Texture2D Texture { get; set; }
		public Color Color => Key.ToString().ToColor();
		public QDictionary<string, string> Data { get; private set; } = new QDictionary<string, string>();
		public T GetData<T>(string key, T defaultValue = default)
		{
			if (Data.ContainsKey(key))
			{
				return Data[key].ParseQData(defaultValue);
			}
			else if (Data.ContainsKey(key.ToLower()))
			{
				return Data[key.ToLower()].ParseQData(defaultValue);
			}
			else
			{
				return defaultValue;
			}
		}
		internal void Update()
		{
			Name = GetData(nameof(Name), Name);
			State = GetData(nameof(State), QLobbyState.组队);
		}
		public override string ToString()
		{
			return "【"+Name + "】(" + Key + ") \t" + Data.ToOneString(" ", kv => kv.Key + ":" + kv.Value);
		}
	}
	public enum QLobbyState
	{
		组队 = 0,
		匹配 = 1,
		加载 = 2,
		游戏 = 3,
	}
}
