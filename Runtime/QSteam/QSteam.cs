#if Steamworks
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices;
using System.Linq;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif


namespace QTool
{


	public static class QSteam
    {
        static QSteam()
        {

        }
#region 成就

		public static ulong Id =>
#if !DISABLESTEAMWORKS
		SteamUser.GetSteamID().m_SteamID;
#else
		0;
#endif
		public static string Name =>
#if !DISABLESTEAMWORKS
		SteamUser.GetSteamID().GetName();
#else
		"未命名";
#endif
		public static bool AchievementState(string key)
		{
#if !DISABLESTEAMWORKS
			if (SteamUserStats.GetAchievement(key,out bool state))
			{
				return state;
			}
#endif
			return false;
		}
		public static void AchievementClear(string key, bool toSteam = true)
		{
#if !DISABLESTEAMWORKS
			Debug.Log("QSteam取消成就 [ " + key + " ] ");
			SteamUserStats.ClearAchievement(key);
			if (toSteam)
			{
				SteamUserStats.StoreStats();
			}
#endif
		}
		public static void AchievementOver(string key,bool toSteam=true)
		{
#if !DISABLESTEAMWORKS
			key = key.Trim();
			Debug.Log("QSteam完成成就 [ " + key + " ] ");
			SteamUserStats.SetAchievement(key);
			if (toSteam)
			{
				SteamUserStats.StoreStats();
			}
#endif
		}
		public static void AchievementSetValue(string key, int value, bool toSteam = true)
		{
#if !DISABLESTEAMWORKS
			key = key.Trim();
			Debug.Log("QSteam成就数值更改 [ " + key+":"+value + " ] ");
			SteamUserStats.SetStat(key, value);
			if (toSteam)
			{
				SteamUserStats.StoreStats();
			}
#endif
		}
		public static void AchievementSetValue(string key, float value, bool toSteam = true)
		{
#if !DISABLESTEAMWORKS
			key = key.Trim();
			Debug.Log("QSteam成就数值更改 [ " + key + ":" + value + " ] ");
			SteamUserStats.SetStat(key, value);
			if (toSteam)
			{
				SteamUserStats.StoreStats();
			}
#endif
		}
		public static int AchievementChangeValue(string key,int changeValue, bool toSteam = true)
		{
			var baseValue = 0;
#if !DISABLESTEAMWORKS
			key = key.Trim();
			SteamUserStats.GetStat(key, out baseValue);
			AchievementSetValue(key, baseValue += changeValue, toSteam);
#endif
			return baseValue += changeValue;
		}
		public static float AchievementChangeValue(string key, float changeValue, bool toSteam = true)
		{

			float baseValue = 0f;
#if !DISABLESTEAMWORKS
			key = key.Trim();
			SteamUserStats.GetStat(key, out baseValue);
			AchievementSetValue(key, baseValue += changeValue, toSteam);
#endif
			return baseValue += changeValue;
		}
#endregion
#region 网络通信

#if !DISABLESTEAMWORKS
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

		public static void Stop()
		{
			OnSendFail.Unregister();
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
        public static IntPtr ToIntPtr(this byte[] bytes)
        {
            unsafe
            {
                fixed (byte* p = &bytes[0])
                {
                    return (IntPtr)p;
                }
            }
        }
        public static void SendLobbyMessage(byte[] bytes, int sendFlag= 8)
        {
            foreach (var m in CurLobby.members)
            {
                SendMessage(m.m_SteamID, bytes, sendFlag);
            }
        }
        public static async void SendMessage(CSteamID steamId, byte[] bytes,int sendFlag=8)
        {
            if (steamId == default)
            {
                Debug.LogError("发送目的地为空" + steamId);
                return;
            }
            if (steamId.m_SteamID == Id)
            {
                await Task.Delay(10);
                try
                {
                    OnReceiveMessage?.Invoke(bytes, steamId);
                }
                catch (Exception e)
                {
                    Debug.LogError("消息接收出错：" + e);
                }
            }
            else
            {
				var netId = steamId.ToNetId();
				var result = SteamNetworkingMessages.SendMessageToUser(ref netId, bytes.ToIntPtr(), (uint)bytes.Length, sendFlag, 0);
                if (result != EResult.k_EResultAdministratorOK && result != EResult.k_EResultOK)
                {
                    Debug.LogError("发送消息失败:" + ":" + steamId + ":" + result);
                }
            }
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
		public static Callback<SteamNetworkingMessagesSessionFailed_t> OnSendFail;
		public static Callback<SteamNetworkingMessagesSessionRequest_t> OnNewReceive;
		public static void StartUpdateLobby()
        {
        }
        public const int ReceiveMessageBufferCount = 10;
        public static IntPtr[] buffers = new IntPtr[ReceiveMessageBufferCount];
        public static async Task StartReceiveMessage()
        {
			
			OnSendFail = Callback<SteamNetworkingMessagesSessionFailed_t>.Create((info) =>
			{
				Debug.LogError("连接出错 关闭与"+info.m_info.m_identityRemote.GetSteamID()+"的会话 "
					+ SteamNetworkingMessages.CloseSessionWithUser(ref info.m_info.m_identityRemote) 
					+ " ：\n" + info.m_info.m_szConnectionDescription + "\n" + info.m_info.m_szEndDebug);
				
			});
			OnNewReceive = Callback<SteamNetworkingMessagesSessionRequest_t>.Create((info) =>
			 {
				 if (SteamNetworkingMessages.AcceptSessionWithUser(ref info.m_identityRemote))
				 {
					 Debug.Log("开启与[" + info.m_identityRemote.GetSteamID() + "]的会话");
				 }
				
			 });
			Debug.Log("开始接收消息");
			var chatLobbyId = CurLobby.steamID;
            if (chatLobbyId.m_SteamID == 0) return;
            while (chatLobbyId == CurLobby.steamID && Application.isPlaying)
            {
                await Task.Yield();
                var length = SteamNetworkingMessages.ReceiveMessagesOnChannel(0, buffers, buffers.Length);

                for (int i = 0; i < length; i++)
                {
                    try
                    {
                        SteamNetworkingMessage_t netMessage = Marshal.PtrToStructure<SteamNetworkingMessage_t>(buffers[i]);
                  
                        if (netMessage.m_cbSize > 0)
                        {
                            byte[] message = new byte[netMessage.m_cbSize];
                            var id = netMessage.m_identityPeer.GetSteamID();
                            Marshal.Copy(netMessage.m_pData, message, 0, message.Length);
                            try
                            {
                                if (message.Length == 1 && message[0] == connect )
                                {
									Debug.Log(id + " 连接 "+Id+" 成功");
								}
                                else
                                {
                                    OnReceiveMessage?.Invoke(message,id );
                                };
                            }
                            catch (Exception e)
                            {
                                Debug.LogError("接收消息出错：" + e);
                                throw;
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("接收消息出错：" + e);
                    }
                    finally
                    {

                        Marshal.DestroyStructure<SteamNetworkingMessage_t>(buffers[i]);
                    }
                }
            }
        }
	
        public static void ExitLobby()
        {
			foreach (var member in CurLobby.members)
			{
				var netId = member.m_SteamID.ToNetId();
				Debug.Log("关闭与[" + netId.GetSteamID() + "]的会话 " + SteamNetworkingMessages.CloseSessionWithUser(ref netId));
			}
            SteamMatchmaking.LeaveLobby(CurLobby.steamID);
			Debug.Log("离开房间[" + CurLobby.steamID + "]");
			OnNewReceive?.Unregister();
			OnUpdateLobby?.Unregister();
			OnSendFail?.Unregister();
            CurLobby = default;
        }
        const byte connect = 111;
        static void SetCurRoom(ulong id)
        {
            UpdateLobby((CSteamID)id, ref CurLobby);
            OnUpdateLobby = Callback<LobbyDataUpdate_t>.Create((info) =>
            {
                UpdateLobby((CSteamID)info.m_ulSteamIDLobby, ref CurLobby);
            });
            SetLobbyMemberData("加入时间", DateTime.Now.ToString());
            StartChatReceive();
            StartReceiveMessage();
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
#endif
#endregion
	}
}

#endif
