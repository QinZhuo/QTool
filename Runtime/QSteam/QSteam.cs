#if Steamworks
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
#if !DISABLESTEAMWORKS
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices;
using System.Linq;

namespace QTool
{


	public static class QSteam
    {
		public static bool Initialized { get; private set; } = false;
		static QSteam()
		{
			if (!Packsize.Test())
			{
				Debug.LogError("[Steamworks.NET]包装尺寸测试返回 false，此平台中运行的 Steamworks.NET 版本错误");
			}
			if (!DllCheck.Test())
			{
				Debug.LogError("[Steamworks.NET] DllCheck 测试返回 false，一个或多个 Steamworks 二进制文件似乎是错误的版本");
			}
			try
			{
				//如果非Steam启动游戏 会进行下面是否拥有游戏的判断
				if (SteamAPI.RestartAppIfNecessary(new AppId_t(QToolSetting.Instance.SteamId)))
				{
					Application.Quit();
					return;
				}
			}
			catch (System.DllNotFoundException e)
			{
				Debug.LogError("[Steamworks.NET]无法加载[lib]steam_api.dll/so/dylib。它可能不在正确的位置。有关详细信息，请参阅自述文件\n" + e);
				Application.Quit();
				return;
			}
			Initialized=SteamAPI.Init();
			SteamClient.SetWarningMessageHook(SteamAPIDebugTextHook);
			QToolManager.Instance.OnUpdateEvent += SteamAPI.RunCallbacks;
			QToolManager.Instance.OnDestroyEvent += SteamAPI.Shutdown;
		}
		[AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
		private static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText)
		{
			Debug.LogWarning(pchDebugText);
		}
#region 成就

		public static ulong Id =>SteamUser.GetSteamID().m_SteamID;
		public static string Name => SteamUser.GetSteamID().GetName();
		public static bool AchievementState(string key)
		{
			if (SteamUserStats.GetAchievement(key, out bool state))
			{
				return state;
			}
			else
			{
				return false;
			}
		}
		public static void AchievementClear(string key, bool toSteam = true)
		{
			Debug.Log("QSteam取消成就 [ " + key + " ] ");
			SteamUserStats.ClearAchievement(key);
			if (toSteam)
			{
				SteamUserStats.StoreStats();
			}
		}
		public static void AchievementOver(string key,bool toSteam=true)
		{
			key = key.Trim();
			Debug.Log("QSteam完成成就 [ " + key + " ] ");
			SteamUserStats.SetAchievement(key);
			if (toSteam)
			{
				SteamUserStats.StoreStats();
			}
		}
		public static void AchievementSetValue(string key, int value, bool toSteam = true)
		{
			key = key.Trim();
			Debug.Log("QSteam成就数值更改 [ " + key + ":" + value + " ] ");
			SteamUserStats.SetStat(key, value);
			if (toSteam)
			{
				SteamUserStats.StoreStats();
			}
		}
		public static void AchievementSetValue(string key, float value, bool toSteam = true)
		{
			key = key.Trim();
			Debug.Log("QSteam成就数值更改 [ " + key + ":" + value + " ] ");
			SteamUserStats.SetStat(key, value);
			if (toSteam)
			{
				SteamUserStats.StoreStats();
			}
		}
		public static int AchievementChangeValue(string key,int changeValue, bool toSteam = true)
		{
			var baseValue = 0;
			key = key.Trim();
			SteamUserStats.GetStat(key, out baseValue);
			AchievementSetValue(key, baseValue += changeValue, toSteam);
			return baseValue += changeValue;
		}
		public static float AchievementChangeValue(string key, float changeValue, bool toSteam = true)
		{
			float baseValue = 0f;
			key = key.Trim();
			SteamUserStats.GetStat(key, out baseValue);
			AchievementSetValue(key, baseValue += changeValue, toSteam);
			return baseValue += changeValue;
		}
		#endregion
#region 网络通信
		public static EResult SendSocket(this HSteamNetConnection conn, byte[] data)
		{
			GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
			IntPtr pData = pinnedArray.AddrOfPinnedObject();
			EResult res = SteamNetworkingSockets.SendMessageToConnection(conn, pData, (uint)data.Length, Constants.k_nSteamNetworkingSend_Reliable, out long _);
			if (res != EResult.k_EResultOK)
			{
				Debug.LogWarning($"Send issue: {res}");
			}
			pinnedArray.Free();
			return res;
		}
		public static byte[] ProcessMessage(this IntPtr ptrs)
		{
			SteamNetworkingMessage_t data = Marshal.PtrToStructure<SteamNetworkingMessage_t>(ptrs);
			byte[] managedArray = new byte[data.m_cbSize];
			Marshal.Copy(data.m_pData, managedArray, 0, data.m_cbSize);
			SteamNetworkingMessage_t.Release(ptrs);
			return managedArray;
		}

        public static async Task<T> GetResult<T>(this SteamAPICall_t steamAPICall_t)
        {
            bool runing = true;
            CallResult<T> tempCall = CallResult<T>.Create();
            T returnValue = default;
            tempCall.Set(steamAPICall_t, (info, failure) =>
            {
                if (failure)
                {
                    ESteamAPICallFailure reason = SteamUtils.GetAPICallFailureReason(tempCall.Handle);
                    Debug.LogError("OnLobbyMatchList encountered an IOFailure due to: " + reason);
                }
                else
                {
                    returnValue = info;
                }
                runing = false;
            });
            while (runing)
            {
                await Task.Delay(10);
            }
            tempCall.Dispose();
            return returnValue;
        }
        public static int DefaultLobbyMemebersCount = 4;
        public static List<Lobby> lobbyList = new List<Lobby>();
        public static Lobby CurLobby;
 
        private static int chatId = 0;
        public static CSteamID ToSteamId(this ulong userId)
        {
            return (CSteamID)userId;
        }
		public static SteamNetworkingIdentity ToNetId(this CSteamID steamID)
		{
			var netId = new SteamNetworkingIdentity();
			netId.SetSteamID(steamID);
			return netId;
		}
		public static void SetLobbyMemberData(string key, string value)
        {
            SteamMatchmaking.SetLobbyMemberData(CurLobby.steamID, key, value);
        }

        public static string GetName(this CSteamID userId)
        {
            return SteamFriends.GetFriendPersonaName(userId);
        }
        public static bool ChatSend(string text)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return SteamMatchmaking.SendLobbyChatMsg(CurLobby.steamID, bytes, bytes.Length + 1);
        }
        public static event Action<string, CSteamID> OnChatReceive;
        public const int ReceiveBufferSize = 4096;
        public static async Task StartChatReceive()
        {

            var chatLobbyId = CurLobby.steamID;
            if (chatLobbyId.IsValid()) return;
            var buffer = new byte[ReceiveBufferSize];
            while (chatLobbyId == CurLobby.steamID && Application.isPlaying)
            {
                await Task.Yield();
                var length = SteamMatchmaking.GetLobbyChatEntry(CurLobby.steamID, chatId, out var id, buffer, buffer.Length, out var type);
                if (length > 0)
                {
                    chatId++;
                    var text = System.Text.Encoding.UTF8.GetString(buffer, 0, length);
                    try
                    {
                        OnChatReceive?.Invoke(text, id);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("消息接收出错" + e);
                    }
                }
            }
        }
        public static event Action<byte[], CSteamID> OnReceiveMessage;
        public static Callback<LobbyDataUpdate_t> OnUpdateLobby;
		public static void StartUpdateLobby()
        {
        }
        public const int ReceiveMessageBufferCount = 10;
        public static IntPtr[] buffers = new IntPtr[ReceiveMessageBufferCount];
 
        public static void ExitLobby()
        {
            SteamMatchmaking.LeaveLobby(CurLobby.steamID);
			Debug.Log("离开房间[" + CurLobby.steamID + "]");
			OnUpdateLobby?.Unregister();
            CurLobby = default;
        }
        static void SetCurRoom(ulong id)
        {
            UpdateLobby((CSteamID)id, ref CurLobby);
            OnUpdateLobby = Callback<LobbyDataUpdate_t>.Create((info) =>
            {
                UpdateLobby((CSteamID)info.m_ulSteamIDLobby, ref CurLobby);
            });
            SetLobbyMemberData("加入时间", DateTime.Now.ToString());
            _=StartChatReceive();
            chatId = 0;
        }
        public static async Task<bool> FastJoin(string game,bool autoCreate=true)
        {
            await FreshLobbys(game);
            if (lobbyList.Count > 0)
            {
                for (int i = 0; i < lobbyList.Count; i++)
                {
                    if (await JoinLobby(lobbyList[i].steamID))
                    {
						return true;
					}
					else
					{
						Debug.LogError("加入房间 " + lobbyList[i].steamID + "失败");
						return false;
					}
                }
            }
			if (autoCreate)
			{
				await CreateLobby(game);
				return true;	
			}
            return false;
        }
        public static async Task<bool> JoinLobby(CSteamID id)
        {
            var join = await SteamMatchmaking.JoinLobby(id).GetResult<LobbyEnter_t>();
            var m_LobbyEnterResponse = (EChatRoomEnterResponse)join.m_EChatRoomEnterResponse;
            if (m_LobbyEnterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Debug.Log("加入房间失败 " + id);
                return false;
            }
            SetCurRoom(join.m_ulSteamIDLobby);
            Debug.Log("加入房间 " + join.m_ulSteamIDLobby);
            return true;
        }
        public static async Task CreateLobby(string Game, ELobbyType eLobbyType = ELobbyType.k_ELobbyTypePublic, int maxMembers = -1)
        {
            while (!await PrivateCreateLobby(eLobbyType, maxMembers))
            {
                await Task.Delay(100);
            }
            if (!string.IsNullOrWhiteSpace(Game))
            {
                CurLobby[nameof(Game)] = Game;
            }
        }
        private static async Task<bool> PrivateCreateLobby(ELobbyType eLobbyType = ELobbyType.k_ELobbyTypePublic, int maxMembers = -1)
        {
            if (maxMembers < 0)
            {
                maxMembers = DefaultLobbyMemebersCount;
            }
            var create = await SteamMatchmaking.CreateLobby(eLobbyType, maxMembers).GetResult<LobbyCreated_t>();
            if (create.m_ulSteamIDLobby != 0)
            {
                Debug.Log("Steam创建房间成功[" + create.m_ulSteamIDLobby + "]");
                SetCurRoom(create.m_ulSteamIDLobby);
                return true;
            }
            else
            {
                Debug.LogError("Steam创建房间出错");
                return false;
            }
        }
        public static void UpdateLobby(CSteamID id, ref Lobby lobby)
        {
            lobby.steamID = id;
            lobby.owner = SteamMatchmaking.GetLobbyOwner(id);
            lobby.members = new LobbyMember[SteamMatchmaking.GetNumLobbyMembers(id)];
            lobby.MemberLimit = SteamMatchmaking.GetLobbyMemberLimit(id);
            for (int t = 0; t < lobby.members.Length; t++)
            {
                lobby.members[t].m_SteamID = SteamMatchmaking.GetLobbyMemberByIndex(id, t);
                lobby.members[t].netId.SetSteamID(lobby.members[t].m_SteamID);

            }
            lobby.data = new LobbyMetaData[SteamMatchmaking.GetLobbyDataCount(id)];
            for (int t = 0; t < lobby.data.Length; ++t)
            {
                bool lobbyDataRet = SteamMatchmaking.GetLobbyDataByIndex(id, t, out lobby.data[t].m_Key, Constants.k_nMaxLobbyKeyLength, out lobby.data[t].m_Value, Constants.k_cubChatMetadataMax);
                if (!lobbyDataRet)
                {
                    Debug.LogError("SteamMatchmaking.GetLobbyDataByIndex returned false." + t);
                    continue;
                }
            }
        }
        public static async Task<List<Lobby>> FreshLobbys(string Game)
        {
            return await FreshLobbys(nameof(Game), Game);
        }
        public static async Task<List<Lobby>> FreshLobbys(string key,string value)
        {
            Debug.Log("刷新房间列表   " + key + ":" + value);
            SteamMatchmaking.AddRequestLobbyListStringFilter(key, value, ELobbyComparison.k_ELobbyComparisonEqual);
            return await FreshLobbys();
        }
        private static async Task<List<Lobby>> FreshLobbys()
        {
            var mList = await SteamMatchmaking.RequestLobbyList().GetResult<LobbyMatchList_t>();
            if (!Application.isPlaying) return null;
            lobbyList.Clear();
            for (int i = 0; i < mList.m_nLobbiesMatching; i++)
            {
                var id = SteamMatchmaking.GetLobbyByIndex(i);
                var lobby = new Lobby();
                UpdateLobby(id, ref lobby);
                lobbyList.Add(lobby);
            }
            return lobbyList;
        }

		public struct LobbyMetaData
		{
			public string m_Key;
			public string m_Value;
		}
		public struct LobbyMember
		{
			public CSteamID m_SteamID;
			public LobbyMetaData[] m_Data;
			public SteamNetworkingIdentity netId;
		}
		public struct Lobby
		{
			public CSteamID steamID;
			public CSteamID owner;
			public LobbyMember[] members;
			public int MemberLimit;
			public LobbyMetaData[] data;
			public string this[string key]
			{
				get
				{
					foreach (var kv in data)
					{
						if (kv.m_Key == key) return kv.m_Value;
					}
					return "";
				}
				set
				{
					if (!SteamMatchmaking.SetLobbyData(steamID, key, value))
					{
						Debug.LogError("设置大厅数据出错[" + steamID + "]" + key + ":" + value);
					}
				}
			}
			public override string ToString()
			{
				return steamID + ": " + members?.Length + "/" + MemberLimit;
			}
		}
		#endregion
	}
}
#endif
#endif
