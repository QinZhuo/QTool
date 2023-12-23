#if Steamworks
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
#if !DISABLESTEAMWORKS
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool.Net
{
	public class QSteamTransport : QNetTransport
	{
		public const int MAX_MESSAGES = 256;
		QSteamServer Server { get; set; }
		public int maxConnections = 10;
		public bool UseP2P = true;
		protected override void Awake()
		{
			base.Awake();
			if (!QLobby.Current.IsNull() || QSteam.MemeberData.Count > 0)
			{
				if (QSteam.IsLobbyOwner)
				{
					Manager.StartHost();
				}
				else
				{
					Manager.StartClient(QLobby.Current.Key.ToString());
				}
			}
			QLobby.OnUpdate += UpdateLobby;
		}
		protected override void OnDestroy()
		{
			base.OnDestroy();
			QLobby.OnUpdate -= UpdateLobby;
		}
		private void UpdateLobby()
		{
			if (QLobby.Current.IsNull()) return;
			if (ServerActive)
			{
				foreach (var client in Server.ConnectClients.ToArray())
				{
					if (!QLobby.Current.Members.ContainsKey(client.Value.m_SteamID))
					{
						Server.Disconnect(client.Value.m_SteamID);
					}
				}
			}
		}
		public override void ServerStart()
		{
			try
			{
				if (!ServerActive)
				{
					Server = new QSteamServer(UseP2P);
					Server.OnConnected += OnServerConnected;
					Server.OnDisconnected += OnServerDisconnected;
					Server.OnReceivedData += OnServerDataReceived;
					Server.OnError += OnServerError;
					base.ServerStart();
				}
				else
				{
					Debug.LogError("服务器已正在运行");
				}
			}
			catch (Exception e)
			{
				throw new Exception("启动Steam服务器失败", e);
			}
		}
		protected override void ServerSend(ulong connectionId, byte[] segment)
		{
			if (ServerActive)
			{
				Server.Send(connectionId, segment);
			}
		}
		public override void ServerSendUpdate()
		{
			Server?.FlushData();
		}

		public override void ServerReceiveUpdate()
		{
			Server?.ReceiveData();
		}
		public override void ServerDisconnect(ulong connectionId)
		{
			if (ServerActive)
			{
				Server.Disconnect(connectionId);
			}
		}
		public override void ServerStop()
		{
			if (ServerActive)
			{
				Server.Shutdown();
				Server = null;
				QDebug.Log(nameof(QSteamTransport) + " 服务器关闭");
			}
			base.ServerStop();
		}

		public override string ClientId => QSteam.Id.ToString();
		private QSteamClient Client { get; set; }
		protected override void ClientConnect(string address)
		{
			try
			{
				SteamNetworkingUtils.InitRelayNetworkAccess();
				Client = new QSteamClient(UseP2P);
				Client.OnConnected += OnClientConnected;
				Client.OnDisconnected += OnClientDisconnected;
				Client.OnReceivedData += OnClientDataReceived;
				Client.Connect(address);
			}
			catch (Exception e)
			{
				throw new Exception("连接Steam服务器出错", e);
			}
			
		}
		protected override void ClientSend(byte[] segment)
		{
			Client.Send(segment);
		}
		public override void ClientSendUpdate()
		{
			Client?.FlushData();
		}
		public override void ClientReceiveUpdate()
		{
			Client?.ReceiveData();
		}
		public override void ClientDisconnect()
		{
			Client?.Disconnect();
			Client = null;
			QDebug.Log(nameof(QSteamTransport) + " 客户端断开连接");
			base.ClientDisconnect();
		}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		protected UnityEngine.UIElements.ListView RoomListView = null;
		protected UnityEngine.UIElements.Button FreshButton = null;
		private async void FreshList()
		{
			await QSteam.FreshLobbys();
#if UNITY_2021_1_OR_NEWER
			RoomListView.Rebuild();
#endif
		}
		public override void DebugUI()
		{
			base.DebugUI();
#if UNITY_2021_1_OR_NEWER
			if (RoomListView == null)
			{
				FreshButton= QToolManager.Instance.RootVisualElement.AddButton("刷新列表", FreshList);
				RoomListView = QToolManager.Instance.RootVisualElement.AddListView(QLobby.List, (visual,index) =>
				{
					var lobby = QLobby.List[index];
					visual.AddButton("加入 "+lobby.ToString(), () => Manager.StartClient(lobby.Key.ToString()));
				});
				RoomListView.fixedItemHeight = 40;
				FreshList();
			}
			FreshButton.visible = !ClientConnected;
			RoomListView.visible = !ClientConnected;
#endif
		}
#endif
	}
	public abstract class QSteamNetBase
	{
		public bool UseP2P { get; protected set; }
		protected Callback<P2PSessionRequest_t> OnP2PRequest = null;
		protected Callback<P2PSessionConnectFail_t> OnP2PFail = null;
		public enum P2PMessage:byte
		{
			Connect,
			Accept,
			DisConnect
		}
		public QSteamNetBase(bool UseP2P)
		{
			this.UseP2P = UseP2P;
			OnP2PRequest = Callback<P2PSessionRequest_t>.Create(OnP2PConnect);
			OnP2PFail = Callback<P2PSessionConnectFail_t>.Create(OnP2PConnectFail);
		}
		protected virtual void OnP2PConnect(P2PSessionRequest_t result)
		{
			QDebug.Log("接受[" + result.m_steamIDRemote.GetName() + "]P2P连接");
			SteamNetworking.AcceptP2PSessionWithUser(result.m_steamIDRemote);
		}
		protected virtual void OnP2PConnectFail(P2PSessionConnectFail_t result)
		{
			SteamNetworking.CloseP2PSessionWithUser(result.m_steamIDRemote);
			switch (result.m_eP2PSessionError)
			{
				case 1:
					Debug.LogError("Connection failed: The target user is not running the same game.");
					break;
				case 2:
					Debug.LogError("Connection failed: The local user doesn't own the app that is running.");
					break;
				case 3:
					Debug.LogError("Connection failed: Target user isn't connected to Steam.");
					break;
				case 4:
					Debug.LogError("Connection failed: The connection timed out because the target user didn't respond.");
					break;
				default:
					Debug.LogError("Connection failed: Unknown error.");
					break;
			}
		}

	}
	public class QSteamServer: QSteamNetBase
	{
		internal event Action<ulong> OnConnected;
		internal event Action<ulong, byte[]> OnReceivedData;
		internal event Action<ulong> OnDisconnected;
		internal event Action<ulong, Exception> OnError;

		#region CS
		internal QSteamServer(bool UseP2P) : base(UseP2P)
		{
			_ = QSteam.CreateLobby();
			if (this.UseP2P)
			{
				try
				{
					InteropHelp.TestIfAvailableClient();
				}
				catch
				{
					Debug.LogError("SteamWorks not initialized.");
				}
			}
			else
			{
				c_onConnectionChange = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
				listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, new SteamNetworkingConfigValue_t[0]);
			}
		}
		protected override void OnP2PConnect(P2PSessionRequest_t result)
		{
			if (QLobby.Current.Members.ContainsKey(result.m_steamIDRemote.m_SteamID))
			{
				base.OnP2PConnect(result);
			}
			else
			{
				QDebug.LogError(result.m_steamIDRemote.GetName() + " P2P连接失败 房间内不存在该玩家 " + QLobby.Current);
			}
		}
		public Dictionary<HSteamNetConnection, CSteamID> ConnectClients = new Dictionary<HSteamNetConnection, CSteamID>();
		private HSteamListenSocket listenSocket;
		private Callback<SteamNetConnectionStatusChangedCallback_t> c_onConnectionChange = null;

		private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
		{
			var clientSteamID = param.m_info.m_identityRemote.GetSteamID();
			if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
			{
				if (clientSteamID == QSteam.Id)
				{
					Debug.LogError(nameof(QSteamServer) + " 主机[" + QSteam.Id.GetName() + "]无法自连接");
					SteamNetworkingSockets.CloseConnection(param.m_hConn, 0, "主机无法自连接", false);
					return;
				}
				else
				{
					if (!QLobby.Current.Members.ContainsKey(clientSteamID.m_SteamID))
					{
						Debug.LogError(nameof(QSteamServer) + " 非房间内玩家[" + clientSteamID.GetName() + "][" + clientSteamID + "]无法连接");
						SteamNetworkingSockets.CloseConnection(param.m_hConn, 0, "非房间内玩家", false);
						return;
					}
				}
				QDebug.Log(nameof(QSteamServer) + "[" + clientSteamID.GetName() + "]尝试连接");
				EResult res;

				if ((res = SteamNetworkingSockets.AcceptConnection(param.m_hConn)) == EResult.k_EResultOK)
				{
					QDebug.Log(nameof(QSteamServer) + "[" + clientSteamID.GetName() + "]成功连接");
				}
				else
				{
					QDebug.LogWarning(nameof(QSteamServer) + "[" + clientSteamID.GetName() + "]连接失败:" + res);
				}
			}
			else if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
			{
				var steamId = param.m_info.m_identityRemote.GetSteamID();
				ConnectClients.Add(param.m_hConn, steamId);
				OnConnected.Invoke(param.m_hConn.m_HSteamNetConnection);
				QDebug.Log(nameof(QSteamServer) + "[" + (int)param.m_hConn.m_HSteamNetConnection + "][" + clientSteamID + "][" + steamId + "]客户端连接成功");
			}
			else if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer || param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
			{
				Debug.LogError(nameof(QSteamServer) + "[" + clientSteamID.GetName() + "]断开连接");
				InternalDisconnect(param.m_hConn);
			}
			else
			{
				QDebug.Log(nameof(QSteamServer) + " " + clientSteamID.GetName() + " 连接状态更改 " + param.m_info.m_eState);
			}
		}
		#endregion
		private void InternalDisconnect(HSteamNetConnection socket)
		{
			if (ConnectClients.ContainsKey(socket))
			{
				OnDisconnected.Invoke(socket.m_HSteamNetConnection);
				SteamNetworkingSockets.CloseConnection(socket, 0, "正常断开连接", false);
				QDebug.Log(nameof(QSteamServer) + " 玩家 [" + ConnectClients[socket].GetName() + "] 断开连接.");
				ConnectClients.Remove(socket);
			}
		}

		public void Disconnect(ulong connectionId)
		{
			if (UseP2P)
			{
				SteamNetworking.CloseP2PSessionWithUser(connectionId.ToSteamId());
			}
			else
			{
				var connection = new HSteamNetConnection((uint)connectionId);
				if (ConnectClients.ContainsKey(connection))
				{
					QDebug.Log(nameof(QSteamServer) + " 断开[" + ConnectClients[connection].GetName() + "]连接");
					SteamNetworkingSockets.CloseConnection(connection, 0, "服务器主动断开连接", false);
					ConnectClients.Remove(connection);
					OnDisconnected(connectionId);
				}
				else
				{
					QDebug.LogWarning(nameof(QSteamServer) + "尝试断开未知连接[" + connectionId + "]");
				}
			}
		}

		public void FlushData()
		{
			if (!UseP2P)
			{
				foreach (HSteamNetConnection conn in ConnectClients.Keys)
				{
					SteamNetworkingSockets.FlushMessagesOnConnection(conn);
				}
			}
		}

		public void ReceiveData()
		{
			if (UseP2P)
			{
				if (SteamNetworking.IsP2PPacketAvailable(out uint size))
				{
					var buffer = new byte[size];
					SteamNetworking.ReadP2PPacket(buffer, size, out _, out var steamid);
					if (buffer.Length == 1)
					{
						QDebug.Log(steamid.GetName()+" "+(P2PMessage)buffer[0]);
						switch ((P2PMessage)buffer[0])
						{
							case P2PMessage.Connect:
								Send(steamid.m_SteamID, new byte[] { (byte)P2PMessage.Accept });
								OnConnected?.Invoke(steamid.m_SteamID);
								return;
							case P2PMessage.DisConnect:
								OnDisconnected?.Invoke(steamid.m_SteamID);
								return;
							default:
								break;
						}
					}
					OnReceivedData(steamid.m_SteamID, buffer);
				}
			}
			else
			{
				foreach (HSteamNetConnection conn in ConnectClients.Keys.ToList())
				{
					IntPtr[] ptrs = new IntPtr[QSteamTransport.MAX_MESSAGES];
					int messageCount;
					while ((messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(conn, ptrs, QSteamTransport.MAX_MESSAGES)) > 0)
					{
						for (int i = 0; i < messageCount; i++)
						{
							OnReceivedData(conn.m_HSteamNetConnection, ptrs[i].ToBytes());
						}
					}
				}
			}
		}

		public void Send(ulong connectionId, byte[] data)
		{
			if (UseP2P)
			{
				SteamNetworking.SendP2PPacket(connectionId.ToSteamId(), data, (uint)data.Length,EP2PSend.k_EP2PSendReliable);
			}
			else
			{
				var connection = new HSteamNetConnection((uint)connectionId);
				if (ConnectClients.ContainsKey(connection))
				{
					EResult res = connection.SendMessage(data);

					if (res == EResult.k_EResultNoConnection || res == EResult.k_EResultInvalidParam)
					{
						QDebug.Log(nameof(QSteamServer) + " 与[" + ConnectClients[connection].GetName() + "]的连接丢失");
						InternalDisconnect(connection);
					}
					else if (res != EResult.k_EResultOK)
					{
						Debug.LogError(nameof(QSteamServer) + " 发送消息失败 " + res);
					}
				}
				else
				{
					Debug.LogError(nameof(QSteamServer) + " 尝试发送消息给未知连接 " + connectionId);
					OnError.Invoke(connectionId, new Exception("未知连接"));
				}
			}
		}
		public void Shutdown()
		{
			QSteam.LeaveLobby();
			if (UseP2P)
			{
				foreach (var clinet in ConnectClients.ToArray())
				{
					Disconnect(clinet.Value.m_SteamID);
				}
			}
			else
			{
				SteamNetworkingSockets.CloseListenSocket(listenSocket);
				if (c_onConnectionChange != null)
				{
					c_onConnectionChange.Dispose();
					c_onConnectionChange = null;
				}
			}
		}
	}
	public class QSteamClient: QSteamNetBase
	{
		public bool Connected { get; private set; }
		private Callback<SteamNetConnectionStatusChangedCallback_t> c_onConnectionChange = null;
		private HSteamNetConnection HostConnection;
		private CSteamID hostId;
		internal event Action<ArraySegment<byte>> OnReceivedData;
		internal event Action OnConnected;
		internal event Action OnDisconnected;
		public QSteamClient(bool UseP2P) : base(UseP2P)
		{
		}
		protected override void OnP2PConnect(P2PSessionRequest_t result)
		{
			if (hostId == result.m_steamIDRemote)
			{
				base.OnP2PConnect(result);
			}
		}
		internal async void Connect(string host)
		{
			try
			{
				await QSteam.JoinLobby(ulong.Parse(host).ToSteamId());
				hostId = QLobby.Current.Owner.ToSteamId();
				if (UseP2P)
				{
					Send(new byte[] { (byte)P2PMessage.Connect });
				}
				else
				{
					c_onConnectionChange = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
					SteamNetworkingIdentity netId = new SteamNetworkingIdentity();
					netId.SetSteamID(hostId);
					HostConnection = SteamNetworkingSockets.ConnectP2P(ref netId, 0, 0, new SteamNetworkingConfigValue_t[0]);
				}
				QDebug.Log(nameof(QSteamClient) + " 尝试连接 " + hostId.GetName());

			}
			catch (FormatException)
			{
				Debug.LogError(nameof(QSteamClient) + " 连接出错 [" + host + "]不是房间Id");
				OnConnectionFailed();
			}
			catch (Exception ex)
			{
				Debug.LogError(nameof(QSteamClient) + " 连接[" + host + "]失败 " + ex.Message);
				OnConnectionFailed();
			}
		}

		private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
		{
			if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
			{
				OnConnected.Invoke();
				QDebug.Log(nameof(QSteamClient) + " 连接[" + param.m_info.m_identityRemote.GetSteamID().GetName() + "]成功");
				Connected = true;
			}
			else if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer || param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
			{
				Debug.LogError(nameof(QSteamClient) + " 连接被关闭 [" + param.m_info.m_identityRemote.GetSteamID().GetName() + "]:" + param.m_info.m_szEndDebug);
				Disconnect();
			}
			else
			{
				QDebug.Log(nameof(QSteamClient) + " 连接状态更改 " + param.m_info.m_eState + " : " + param.m_info.m_szEndDebug);
			}
		}

		public void Disconnect()
		{
			QSteam.LeaveLobby();
			Dispose();
			if (UseP2P)
			{
				Send(new byte[] { (byte)P2PMessage.DisConnect });
				SteamNetworking.CloseP2PSessionWithUser(hostId);
				hostId = default;
			}
			else
			{
				if (HostConnection.m_HSteamNetConnection != 0)
				{
					QDebug.Log(nameof(QSteamClient) + " 主动断开连接");
					SteamNetworkingSockets.CloseConnection(HostConnection, 0, " 客户端主动断开连接 ", false);
					HostConnection.m_HSteamNetConnection = 0;
				}
			}
		
		}

		protected void Dispose()
		{
			if (!UseP2P)
			{
				if (c_onConnectionChange != null)
				{
					c_onConnectionChange.Dispose();
					c_onConnectionChange = null;
				}
			}
		}

		private void InternalDisconnect()
		{
			Connected = false;
			OnDisconnected.Invoke();
			QDebug.Log(nameof(QSteamClient) + " 断开连接");
			SteamNetworkingSockets.CloseConnection(HostConnection, 0, "断开连接", false);
		}

		public void ReceiveData()
		{
			if (UseP2P)
			{
				if (SteamNetworking.IsP2PPacketAvailable(out uint size))
				{
					var buffer = new byte[size];
					SteamNetworking.ReadP2PPacket(buffer, size, out _, out var steamid);
					if (buffer.Length == 1)
					{
						QDebug.Log(steamid.GetName() + " " + (P2PMessage)buffer[0]);
						if ((P2PMessage)buffer[0] == P2PMessage.Accept)
						{
							OnConnected?.Invoke();
							return;
						}
					}
					OnReceivedData(buffer);
				}
			}
			else
			{
				IntPtr[] ptrs = new IntPtr[QSteamTransport.MAX_MESSAGES];
				int messageCount = 0;
				if ((messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(HostConnection, ptrs, QSteamTransport.MAX_MESSAGES)) > 0)
				{
					for (int i = 0; i < messageCount; i++)
					{
						OnReceivedData(new ArraySegment<byte>(ptrs[i].ToBytes()));
					}
				}
			}
		}

		public void Send(byte[] data)
		{
			if (UseP2P)
			{
				SteamNetworking.SendP2PPacket(hostId, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
			}
			else
			{
				var res = HostConnection.SendMessage(data);
				if (res == EResult.k_EResultNoConnection || res == EResult.k_EResultInvalidParam)
				{
					Debug.LogError(nameof(QSteamClient) + " 服务器连接丢失");
					InternalDisconnect();
				}
				else if (res != EResult.k_EResultOK)
				{
					Debug.LogError(nameof(QSteamClient) + " 发送消息失败 " + res);
				}
			}
		}

		private void OnConnectionFailed()
		{
			OnDisconnected.Invoke();
		}
		public void FlushData()
		{
			if (!UseP2P)
			{
				SteamNetworkingSockets.FlushMessagesOnConnection(HostConnection);
			}
		}

	}
}
#endif

#endif
