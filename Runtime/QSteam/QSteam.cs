#if Steamworks
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
#if !DISABLESTEAMWORKS
using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace QTool.Steam
{
	public static class QSteam
	{
		public static CSteamID Id => SteamUser.GetSteamID();
		public static string Name => SteamFriends.GetPersonaName();
		public static QDictionary<string, string> MemeberData => QLobby.CurrentLobby.Members[Id.m_SteamID].Data;
		public static bool IsLobbyOwner =>QLobby.CurrentLobby.IsNull() || QLobby.CurrentLobby.Owner == Id.m_SteamID;
		public static Texture2D AvatarImage => Id.GetImage();
		private static Callback<GameLobbyJoinRequested_t> OnJoinRequested = null;
		private static Callback<LobbyDataUpdate_t> OnLobbyDataUpdate = null;
		private static Callback<LobbyChatUpdate_t> OnLobbyChatUpdate = null;

		private static int chatId = 0;
		static QSteam()
		{
			if (!Packsize.Test())
			{
				Debug.LogError(nameof(QSteam) + " 包装尺寸测试返回 false，此平台中运行的 Steamworks.NET 版本错误");
			}
			if (!DllCheck.Test())
			{
				Debug.LogError(nameof(QSteam) + " DllCheck 测试返回 false，一个或多个 Steamworks 二进制文件似乎是错误的版本");
			}
			try
			{
				//如果非Steam启动游戏 会进行下面是否拥有游戏的判断
				if (SteamAPI.RestartAppIfNecessary(new AppId_t(QToolSetting.Instance.SteamId)))
				{
					Debug.LogError(nameof(QSteam) + " 游戏验证未通过");
					QTool.Quit();
					return;
				}
			}
			catch (System.DllNotFoundException e)
			{
				Debug.LogError(nameof(QSteam) + " 无法加载[lib]steam_api.dll/so/dylib。它可能不在正确的位置。有关详细信息，请参阅自述文件\n" + e);
				QTool.Quit();
				return;
			}
			if (!SteamAPI.Init())
			{
				Debug.LogError(nameof(QSteam) + " 初始化失败");
				QTool.Quit();
				return;
			}
			SteamClient.SetWarningMessageHook(SteamAPIDebugTextHook);
			QToolManager.Instance.OnUpdateEvent += SteamAPI.RunCallbacks;
			OnJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(info =>
			{
				_ = JoinLobby(info.m_steamIDLobby);
			});
			OnLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(info =>
			{
				UpdateCurrentLobby(info.m_ulSteamIDLobby.ToSteamId());
			});
			OnLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(info =>
			{
				UpdateCurrentLobby(info.m_ulSteamIDLobby.ToSteamId());
			});
			QEventManager.RegisterOnce(QToolEvent.游戏退出完成, LeaveLobby, SteamAPI.Shutdown,
				OnJoinRequested.Unregister, OnLobbyDataUpdate.Unregister, OnLobbyChatUpdate.Unregister);
			SteamNetworkingUtils.InitRelayNetworkAccess();
			QDebug.Log(nameof(QSteam) + " 初始化成功 [" + Name + "][" + Id + "]");
			var commands = Environment.GetCommandLineArgs();
			for (int i = 0; i < commands.Length-1; i++)
			{
				switch (commands[i])
				{
					case "+connect_lobby":
						{
							if (ulong.TryParse(commands[i + 1], out ulong lobbyID))
							{
								if (lobbyID > 0)
								{
									_ = JoinLobby(lobbyID.ToSteamId());
								}
							}
						}
						break;
					default:
						break;
				}
			}
		}
	
		[AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
		private static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText)
		{
			Debug.LogWarning(pchDebugText);
		}
		public static int Ping(this SteamNetworkPingLocation_t pingLocation)
		{
			return SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref pingLocation);
		}
		public static void SetType(this QLobby lobby, ELobbyType type)
		{
			SteamMatchmaking.SetLobbyType(lobby.Key.ToSteamId(), type);
		}
		public static CSteamID ToSteamId(this ulong userId)
		{
			return (CSteamID)userId;
		}
		public static Texture2D GetImage(this CSteamID player)
		{
			return SteamFriends.GetSmallFriendAvatar(player).GetImage();
		}
		private static QDictionary<int, Texture2D> ImageCache = new QDictionary<int, Texture2D>();
		public static Texture2D GetImage(this int image)
		{
			if (image < 0) return null;
			if (ImageCache.ContainsKey(image)) return ImageCache[image];
			if(SteamUtils.GetImageSize(image, out var width, out var height))
			{
				var data = new byte[4 * width * height];
				if (SteamUtils.GetImageRGBA(image, data, data.Length))
				{
					var texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
					texture.LoadRawTextureData(data);
					texture.Apply();
					ImageCache[image] = texture;
					return texture;
				}
			}
			return null;
		}
		public static string GetName(this CSteamID userId)
		{
			return SteamFriends.GetFriendPersonaName(userId);
		}
#region 成就
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
		public static void AchievementClear(string key="", bool toSteam = true)
		{
			if (key.IsNull())
			{
				QDebug.Log(nameof(QSteam) + "重置所有成就");
				SteamUserStats.ResetAllStats(true);
			}
			else
			{
				QDebug.Log(nameof(QSteam) + "取消成就 [ " + key + " ] ");
				SteamUserStats.ClearAchievement(key);
			}
			if (toSteam)
			{
				SteamUserStats.StoreStats();
			}
		}
		public static void AchievementOver(string key,bool toSteam=true)
		{
			QDebug.Log(nameof(QSteam) + "完成成就 [ " + key + " ] ");
			SteamUserStats.SetAchievement(key);
			if (toSteam)
			{
				SteamUserStats.StoreStats();
			}
		}
		public static void AchievementSetValue(string key, int value, bool toSteam = true)
		{
			QDebug.Log(nameof(QSteam) + "成就数值更改 [ " + key + ":" + value + " ] ");
			SteamUserStats.SetStat(key, value);
			if (toSteam)
			{
				SteamUserStats.StoreStats();
			}
		}
		public static void AchievementSetValue(string key, float value, bool toSteam = true)
		{
			QDebug.Log(nameof(QSteam) + "成就数值更改 [ " + key + ":" + value + " ] ");
			SteamUserStats.SetStat(key, value);
			if (toSteam)
			{
				SteamUserStats.StoreStats();
			}
		}
		public static int AchievementChangeValue(string key,int changeValue, bool toSteam = true)
		{
			SteamUserStats.GetStat(key, out int baseValue);
			AchievementSetValue(key, baseValue + changeValue, toSteam);
			return baseValue + changeValue;
		}
		public static float AchievementChangeValue(string key, float changeValue, bool toSteam = true)
		{
			SteamUserStats.GetStat(key, out float baseValue);
			AchievementSetValue(key, baseValue + changeValue, toSteam);
			return baseValue + changeValue;
		}
#endregion
#region 网络通信
		public static EResult SendMessage(this HSteamNetConnection conn, byte[] data)
		{
			GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
			IntPtr pData = pinnedArray.AddrOfPinnedObject();
			EResult res = SteamNetworkingSockets.SendMessageToConnection(conn, pData, (uint)data.Length, Constants.k_nSteamNetworkingSend_ReliableNoNagle, out long _);
			if (res != EResult.k_EResultOK)
			{
				Debug.LogWarning(nameof(QSteam)+ " 向[" + conn + "]发送消息失败 "+res);
			}
			pinnedArray.Free();
			return res;
		}
		public static byte[] ToBytes(this ref IntPtr ptrs)
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
            CallResult<T> CallBack = CallResult<T>.Create();
            T returnValue = default;
            CallBack.Set(steamAPICall_t, (info, failure) =>
            {
                if (failure)
                {
                    ESteamAPICallFailure reason = SteamUtils.GetAPICallFailureReason(CallBack.Handle);
                    Debug.LogError(nameof(QSteam)+" 获取调用结果["+typeof(T)+"]出错 " + reason);
                }
                else
                {
                    returnValue = info;
                }
                runing = false;
            });
			await QTask.Wait(() => !runing);
            CallBack.Dispose();
            return returnValue;
        }
		public static void SetData<T>(this QLobby lobby, string key, T value)
		{
			var str = value.ToQData().Trim('"');
			if (lobby.IsNull())
			{
				lobby.Data[key] = str;
			}
			else
			{
				if (SteamMatchmaking.SetLobbyData(lobby.Key.ToSteamId(), key, str))
				{
					QDebug.Log("房间." + key + " = " + str);
				}
				else
				{
					Debug.LogError("设置大厅数据出错[" + Id + "]" + key + ":" + str);
				}
			}
		}

		public static void SetLobbyMemberData<T>(string key, T value)
		{
			var data = value.ToQData().Trim('"');
			MemeberData[key] = data;
			QDebug.Log(QLobby.CurrentLobby.Key + "." + Name + "." + key + " = " + data);
			if (!QLobby.CurrentLobby.IsNull())
			{
				SteamMatchmaking.SetLobbyMemberData(QLobby.CurrentLobby.Key.ToSteamId(), nameof(QLobby.Data), MemeberData.ToQData());
			}
		}

		/// <summary>
		/// 调用邀请好友面板
		/// </summary>
		public static void InviteDialog()
		{
			SteamFriends.ActivateGameOverlayInviteDialog(QLobby.CurrentLobby.Key.ToSteamId());
		}
		public static bool ChatSend(string text)
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
			return SteamMatchmaking.SendLobbyChatMsg(QLobby.CurrentLobby.Key.ToSteamId(), bytes, bytes.Length + 1);
		}
        public static event Action<string, CSteamID> OnChatReceive;
        public const int ReceiveBufferSize = 4096;
        public static async Task StartChatReceive()
        {
            var chatLobbyId =QLobby.CurrentLobby.Key.ToSteamId();
            if (chatLobbyId.IsValid()) return;
            var buffer = new byte[ReceiveBufferSize]; 
            while (chatLobbyId == QLobby.CurrentLobby.Key.ToSteamId() && Application.isPlaying)
            {
                await Task.Yield();
                var length = SteamMatchmaking.GetLobbyChatEntry(QLobby.CurrentLobby.Key.ToSteamId(), chatId, out var id, buffer, buffer.Length, out var type);
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

	
		public static void LeaveLobby()
        {
			if (QLobby.CurrentLobby.IsNull()) return;
            SteamMatchmaking.LeaveLobby(QLobby.CurrentLobby.Key.ToSteamId());
			QDebug.Log(nameof(QSteam)+" 离开房间[" + QLobby.CurrentLobby.Key + "]");
			QLobby.Leave();
        }
	
        public static async Task<bool> FastJoin()
        {
            await FreshLobbys();
            if (QLobby.LobbyList.Count > 0)
            {
                for (int i = 0; i < QLobby.LobbyList.Count; i++)
                {
                    if (await JoinLobby(QLobby.LobbyList[i].Key.ToSteamId()))
                    {
						return true;
					}
					else
					{
						Debug.LogError("加入房间 " + QLobby.LobbyList[i].Key.ToSteamId() + "失败");
						continue;
					}
                }
			}
			else
			{
				return await CreateLobby();
			}
			return false;
		}
		public static async Task<bool> JoinLobby(CSteamID lobbyId)
		{
			if (lobbyId.m_SteamID != QLobby.CurrentLobby.Key)
			{
				var join = await SteamMatchmaking.JoinLobby(lobbyId).GetResult<LobbyEnter_t>();
				var m_LobbyEnterResponse = (EChatRoomEnterResponse)join.m_EChatRoomEnterResponse;
				if (m_LobbyEnterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
				{
					QDebug.LogError("加入房间失败[" + lobbyId + "]");
					return false;
				}
				QLobby.CurrentLobby.FreshData();
			}
			return true;
		}
        public static async Task<bool> CreateLobby(int maxMembers = 10,ELobbyType eLobbyType = ELobbyType.k_ELobbyTypePublic)
        {
			if (!QLobby.CurrentLobby.IsNull() && QLobby.CurrentLobby.Owner == Id.m_SteamID) return true;
			var create = await SteamMatchmaking.CreateLobby(eLobbyType, maxMembers).GetResult<LobbyCreated_t>();
			if (create.m_ulSteamIDLobby != 0)
			{
				QDebug.Log(nameof(QSteam) + " 创建房间成功[" + create.m_ulSteamIDLobby + "]");
				await JoinLobby(QLobby.CurrentLobby.Key.ToSteamId());
				QLobby.CurrentLobby.SetData(nameof(Name), Name);
				QLobby.CurrentLobby.SetData(nameof(Application.productName), Application.productName);
				QLobby.CurrentLobby.SetData(nameof(Application.version), Application.version);
				return true;
			}
			else
			{
				Debug.LogError(nameof(QSteam) + " 创建房间出错" + create.m_eResult);
				return false;
			}
		}
		public static bool CheckLobby(this CSteamID steamID)
		{
			return SteamMatchmaking.GetLobbyData(steamID, nameof(Application.productName)) == Application.productName &&
			   (Application.isEditor || SteamMatchmaking.GetLobbyData(steamID, nameof(Application.version)) == Application.version);
		}
		public static void UpdateCurrentLobby(CSteamID currentlobbyId)
		{
			if (QLobby.CurrentLobby.Key != currentlobbyId.m_SteamID)
			{
				if (QLobby.CurrentLobby.IsNull())
				{
					foreach (var kv in QLobby.CurrentLobby.Data)
					{
						SteamMatchmaking.SetLobbyData(currentlobbyId, kv.Key, kv.Value);
					}
				}
				SteamMatchmaking.SetLobbyMemberData(currentlobbyId, nameof(QLobby.Data),MemeberData.ToQData());
				QLobby.CurrentLobby.Key = currentlobbyId.m_SteamID;
				_ = StartChatReceive();
				chatId = 0;
				QDebug.Log("加入房间[" + currentlobbyId + "]");
			}
			QLobby.CurrentLobby.FreshData();
			QLobby.OnUpdate?.Invoke();
		}
		public static void FreshData(this QLobby lobby)
		{
			if (lobby.Key == 0) return;
			var lobbyId = lobby.Key.ToSteamId();
			lobby.Owner = SteamMatchmaking.GetLobbyOwner(lobbyId).m_SteamID;
			lobby.Members.Clear();
			var memeberCount= SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            lobby.MemberLimit = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);
			for (int t = 0; t < memeberCount; t++)
			{
				var memeberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, t);
				var memeber = lobby.Members[memeberId.m_SteamID];
				memeber.Name = memeberId.GetName();
				if (lobby == QLobby.CurrentLobby)
				{
					SteamMatchmaking.GetLobbyMemberData(lobbyId, memeberId, nameof(memeber.Data)).ParseQData(memeber.Data);
				}
			}
			lobby.Data.Clear();
			var count = SteamMatchmaking.GetLobbyDataCount(lobbyId);
			for (int t = 0; t < count; ++t) 
            {
				bool lobbyDataRet = SteamMatchmaking.GetLobbyDataByIndex(lobbyId, t, out var key, Constants.k_nMaxLobbyKeyLength,out var value, Constants.k_cubChatMetadataMax);
				lobby.Data[key] = value;
				if (!lobbyDataRet)
                {
                    Debug.LogError(nameof(QSteam)+" 获取房间["+lobbyId+"]信息出错 " + t);
                    continue;
                }
			}
			
			QDebug.Log(nameof(QSteam)+" 房间信息更新 " + lobby.ToDetailString());
		}
		public static void AddLobbyFilter(string key, string value, ELobbyComparison comparison = ELobbyComparison.k_ELobbyComparisonEqual)
		{
			QDebug.Log(nameof(QSteam) + " 过滤房间列表[" + key + ":" + value + "]");
			SteamMatchmaking.AddRequestLobbyListStringFilter(key, value, comparison);
		}
        public static async Task<List<QLobby>> FreshLobbys()
		{
			AddLobbyFilter(nameof(Application.productName), Application.productName, ELobbyComparison.k_ELobbyComparisonEqual); 
			if (!Application.isEditor)
			{
				AddLobbyFilter(nameof(Application.version), Application.version, ELobbyComparison.k_ELobbyComparisonEqual);
			}
			var matchList = await SteamMatchmaking.RequestLobbyList().GetResult<LobbyMatchList_t>();
			if (!Application.isPlaying) return null;
            QLobby.LobbyList.Clear();
		
            for (int i = 0; i < matchList.m_nLobbiesMatching; i++)
            {
				var lobby = QLobby.LobbyList[SteamMatchmaking.GetLobbyByIndex(i).m_SteamID];
				lobby.FreshData();
				QDebug.Log(nameof(QSteam) + " 房间信息 " + lobby);
			}
			QDebug.Log(nameof(QSteam) + " 刷新房间结束 " + QLobby.LobbyList.Count);
			return QLobby.LobbyList;
        }
		
#endregion
	}
}
#endif
#endif
