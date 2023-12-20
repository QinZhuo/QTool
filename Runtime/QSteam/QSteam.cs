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

namespace QTool
{


	public static class QSteam
	{
		public static string CommandLine { get; private set; }
		public static CSteamID Id => SteamUser.GetSteamID();
		public static string Name => SteamFriends.GetPersonaName();
		public static Texture2D AvatarImage => Id.GetImage();
		public static bool IsRoomOwner => CurrentLobby.IsNull() || CurrentLobby.Owner == Id;
		private static Callback<GameLobbyJoinRequested_t> OnJoinRequested = null;
		private static Callback<LobbyDataUpdate_t> OnLobbyDataUpdate = null;
		private static Callback<LobbyChatUpdate_t> OnLobbyChatUpdate = null;

		public static Action OnLobbyUpdate = null;
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
			SteamApps.GetLaunchCommandLine(out var commandLine, 1024);
			CommandLine = commandLine;
			OnJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(info =>
			{
				_ = JoinLobby(info.m_steamIDLobby);
			});
			OnLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(info =>
			{
				LobbyUpdate((CSteamID)info.m_ulSteamIDLobby, ref _CurrentLobby);
			});
			OnLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(info =>
			{
				LobbyUpdate((CSteamID)info.m_ulSteamIDLobby, ref _CurrentLobby);
			});
			QEventManager.RegisterOnce(QToolEvent.游戏退出完成, LeaveLobby, SteamAPI.Shutdown,
				OnJoinRequested.Unregister, OnLobbyDataUpdate.Unregister, OnLobbyChatUpdate.Unregister);
			SteamNetworkingUtils.InitRelayNetworkAccess();
			QDebug.Log(nameof(QSteam) + " 初始化成功 [" + Name + "][" + Id + "]");
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
        public static List<QLobby> LobbyList { get; private set; } = new List<QLobby>();

		private static QLobby _CurrentLobby = default;
		public static QLobby CurrentLobby => _CurrentLobby;
 
        private static int chatId = 0;
		public static QDictionary<string, string> LocalMemberData { get; private set; } = new QDictionary<string, string>();
		public static void SetLobbyMemberData<T>(string key, T value)
        {
			var data = value.ToQData();
			QDebug.Log(CurrentLobby.SteamID+"."+Name + "." + key + " = " + data);
			if (CurrentLobby.IsNull())
			{
				LocalMemberData[key] = data;
			}
			else
			{
				SteamMatchmaking.SetLobbyMemberData(CurrentLobby.SteamID, key, data);
			}
		}
		public static T GetLobbyMemberData<T>(string key, T defaultValue = default)
		{
			if (CurrentLobby.IsNull())
			{
				if (LocalMemberData.ContainsKey(key))
				{
					return LocalMemberData[key].ParseQData(defaultValue);
				}
				else
				{
					return defaultValue;
				}
			}
			return Id.GetLobbyMemberData(key, defaultValue);
		}
		public static T GetLobbyMemberData<T>(this CSteamID steamID, string key, T defaultValue = default)
		{
			var data = SteamMatchmaking.GetLobbyMemberData(CurrentLobby.SteamID, steamID, key);
			QDebug.Log(CurrentLobby.SteamID + "." + steamID.GetName() + "." + key + ":" + data);
			return data.IsNull() ? defaultValue : data.ParseQData(defaultValue);
		} 
		/// <summary>
		/// 调用邀请好友面板
		/// </summary>
		public static void InviteDialog()
		{
			SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobby.SteamID);
		}
		public static bool ChatSend(string text)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return SteamMatchmaking.SendLobbyChatMsg(_CurrentLobby.SteamID, bytes, bytes.Length + 1);
        }
        public static event Action<string, CSteamID> OnChatReceive;
        public const int ReceiveBufferSize = 4096;
        public static async Task StartChatReceive()
        {
            var chatLobbyId = _CurrentLobby.SteamID;
            if (chatLobbyId.IsValid()) return;
            var buffer = new byte[ReceiveBufferSize]; 
            while (chatLobbyId == _CurrentLobby.SteamID && Application.isPlaying)
            {
                await Task.Yield();
                var length = SteamMatchmaking.GetLobbyChatEntry(_CurrentLobby.SteamID, chatId, out var id, buffer, buffer.Length, out var type);
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
			if (CurrentLobby.IsNull()) return;
            SteamMatchmaking.LeaveLobby(_CurrentLobby.SteamID);
			QDebug.Log(nameof(QSteam)+" 离开房间[" + _CurrentLobby.SteamID + "]");
            _CurrentLobby = default;
        }
        static void SetCurRoom(ulong id)
        {
            LobbyUpdate((CSteamID)id, ref _CurrentLobby);
			_ =StartChatReceive();
            chatId = 0;
		}
        public static async Task<bool> FastJoin()
        {
            await FreshLobbys();
            if (LobbyList.Count > 0)
            {
                for (int i = 0; i < LobbyList.Count; i++)
                {
                    if (await JoinLobby(LobbyList[i].SteamID))
                    {
						return true;
					}
					else
					{
						Debug.LogError("加入房间 " + LobbyList[i].SteamID + "失败");
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
        public static async Task<bool> JoinLobby(CSteamID id)
        {
            var join = await SteamMatchmaking.JoinLobby(id).GetResult<LobbyEnter_t>();
            var m_LobbyEnterResponse = (EChatRoomEnterResponse)join.m_EChatRoomEnterResponse;
            if (m_LobbyEnterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
				QDebug.LogError("加入房间失败[" + id + "]");
                return false;
            }
            SetCurRoom(join.m_ulSteamIDLobby);
			QDebug.Log("加入房间[" + join.m_ulSteamIDLobby + "]");
            return true;
        }
        public static async Task<bool> CreateLobby(int maxMembers = 10,ELobbyType eLobbyType = ELobbyType.k_ELobbyTypePublic)
        {
			if (!CurrentLobby.IsNull() && CurrentLobby.Owner == Id) return true;
			var create = await SteamMatchmaking.CreateLobby(eLobbyType, maxMembers).GetResult<LobbyCreated_t>();
			if (create.m_ulSteamIDLobby != 0)
			{
				QDebug.Log(nameof(QSteam) + " 创建房间成功[" + create.m_ulSteamIDLobby + "]");
				SetCurRoom(create.m_ulSteamIDLobby);
				_CurrentLobby[nameof(Name)] = Name;
				_CurrentLobby[nameof(Application.productName)] = Application.productName;
				_CurrentLobby[nameof(Application.version)] = Application.version;
				if (LocalMemberData.Count > 0)
				{
					foreach (var kv in LocalMemberData)
					{
						SteamMatchmaking.SetLobbyMemberData(CurrentLobby.SteamID, kv.Key, kv.Value);
					}
					LocalMemberData.Clear();
				}
				return true;
			}
			else
			{
				Debug.LogError(nameof(QSteam) + " 创建房间出错" + create.m_eResult);
				return false;
			}
		}
		public static void UpdateLobby()
		{
			if (!_CurrentLobby.IsNull())
			{
				LobbyUpdate(_CurrentLobby.SteamID, ref _CurrentLobby);
			}
		}
		public static void LobbyUpdate(CSteamID id, ref QLobby lobby)
        {
            lobby.SteamID = id;
            lobby.Owner = SteamMatchmaking.GetLobbyOwner(id);
            lobby.Members = new QLobbyMember[SteamMatchmaking.GetNumLobbyMembers(id)];
            lobby.MemberLimit = SteamMatchmaking.GetLobbyMemberLimit(id);
            for (int t = 0; t < lobby.Members.Length; t++)
            {
                lobby.Members[t].SteamID = SteamMatchmaking.GetLobbyMemberByIndex(id, t);
			}
			lobby.Data = new QDictionary<string, string>();
			var count = SteamMatchmaking.GetLobbyDataCount(id);

			for (int t = 0; t < count; ++t) 
            {
				bool lobbyDataRet = SteamMatchmaking.GetLobbyDataByIndex(id, t, out var key, Constants.k_nMaxLobbyKeyLength,out var value, Constants.k_cubChatMetadataMax);
				lobby.Data[key] = value;
				if (!lobbyDataRet)
                {
                    Debug.LogError(nameof(QSteam)+" 获取房间["+id+"]信息出错 " + t);
                    continue;
                }
			}
			OnLobbyUpdate?.Invoke();
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
            LobbyList.Clear();
		
            for (int i = 0; i < matchList.m_nLobbiesMatching; i++)
            {
                var id = SteamMatchmaking.GetLobbyByIndex(i);
                var lobby = new QLobby();
                LobbyUpdate(id, ref lobby);
                LobbyList.Add(lobby);
				QDebug.Log(nameof(QSteam) + " 房间信息 "+lobby);
			}
			QDebug.Log(nameof(QSteam) + " 刷新房间结束 " + LobbyList.Count);
			return LobbyList;
        }
		public struct QLobbyMember:IKey<CSteamID>
		{
			public CSteamID Key { get => SteamID; set => SteamID = value; }
			public CSteamID SteamID { get; internal set; }
			public T GetData<T>(string key)
			{
				return SteamID.GetLobbyMemberData<T>(key);
			}
			public override string ToString()
			{
				return SteamID.GetName() + " " + SteamID;
			}
		}
		public struct QLobby
		{
			public CSteamID SteamID { get; internal set; }
			public CSteamID Owner { get; internal set; }
			public QLobbyMember[] Members { get; internal set; }
			public int MemberLimit { get; internal set; }
			public QDictionary<string,string> Data { get; internal set; }
			public string this[string key]
			{
				get
				{
					if (Data != null)
					{
						if (Data.ContainsKey(key))
						{
							return Data[key];
						}
						else
						{
							foreach (var kv in Data)
							{
								if (kv.Key.ToLower() == key.ToLower()) return kv.Value;
							}
						}
					}
					return "";
				}
				set
				{
					if (SteamMatchmaking.SetLobbyData(SteamID, key, value))
					{
						QDebug.Log("房间." + key + " = " + value);
					}
					else
					{
						Debug.LogError("设置大厅数据出错[" + SteamID + "]" + key+ ":" + value);
					}
				}
			}
			public void SetType(ELobbyType type)
			{
				SteamMatchmaking.SetLobbyType(SteamID, type);
			}
			public override string ToString()
			{
				var name = this[nameof(QSteam.Name)];
				if (name.IsNull())
				{
					name = Owner.GetName();
				}
				return name + " [" + Members?.Length + "/" + MemberLimit+"]\n["+ SteamID+"]";
			}
			public string ToDetailString()
			{
				var dataStr= ToString();
				dataStr += Data.ToOneString(" ") + "\n";
				dataStr += Members.ToOneString(" ");
				return dataStr;
			}
		}
#endregion
	}
}
#endif
#endif
