#if Steamworks
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
#if !DISABLESTEAMWORKS
using Steamworks;
#endif
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
		protected override void Awake()
		{
			base.Awake();
			var id = QSteam.Id;
			SteamNetworkingUtils.InitRelayNetworkAccess();
			_ = QSteam.FreshLobbys();
		}
		public override void ServerStart()
		{
			if (!ServerActive)
			{
				Server = new QSteamServer();
				Server.OnConnected += OnServerConnected;
				Server.OnDisconnected += OnServerDisconnected;
				Server.OnReceivedData += OnServerDataReceived;
				Server.OnError += OnServerError;
			}
			else
			{
				Debug.LogError("服务器已正在运行");
			}
			base.ServerStart();
		}
		protected override void ServerSend(int connectionId, byte[] segment)
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
		public override void ServerDisconnect(int connectionId)
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
		public override int Ping => SteamPing;
		private QSteamClient Client { get; set; }
		private int SteamPing { get; set; }
		public override void FreshPing()
		{
			if (!QSteam.CurrentLobby.PingLocation.IsNull())
			{
				SteamPing = QSteam.Ping(QSteam.CurrentLobby.PingLocation);
			}
		}
		protected override void ClientConnect(string address)
		{
			Client = new QSteamClient();
			Client.OnConnected += OnClientConnected;
			Client.OnDisconnected += OnClientDisconnected;
			Client.OnReceivedData += OnClientDataReceived;
			Client.Connect(address);
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
		private Vector2 ScrollPosition = Vector2.zero;
		public override void DebugGUI()
		{
			if (ServerActive)
			{
				GUILayout.Label(QSteam.CurrentLobby.ToString());
			}
			else
			{
				if (GUILayout.Button("创建房间", GUILayout.Width(200), GUILayout.Height(30)))
				{
					GetComponent<QNetManager>().StartHost();
				}
			}
			if (!ClientConnected)
			{
				if (QSteam.CurrentLobby.IsNull())
				{
					if (QSteam.LobbyList.Count > 0)
					{
						using (var scroll = new GUILayout.ScrollViewScope(ScrollPosition, true, false, GUILayout.Width(200)))
						{
							foreach (var lobby in QSteam.LobbyList)
							{
								if (GUILayout.Button(lobby.ToString(), GUILayout.Width(175), GUILayout.Height(40)))
								{
									GetComponent<QNetManager>().StartClient(lobby.SteamID.ToString());
								}
							}
							ScrollPosition = scroll.scrollPosition;
						}
					}
					if (GUILayout.Button("刷新房间", GUILayout.Width(200), GUILayout.Height(30)))
					{
						_ = QSteam.FreshLobbys();
					}
				}
			}
		}
#endif
	}

	public class QSteamServer
	{
		internal event Action<int> OnConnected;
		internal event Action<int, ArraySegment<byte>> OnReceivedData;
		internal event Action<int> OnDisconnected;
		internal event Action<int, Exception> OnError;
		public Dictionary<HSteamNetConnection, CSteamID> ConnectClients = new Dictionary<HSteamNetConnection, CSteamID>();

		private HSteamListenSocket listenSocket;

		private Callback<SteamNetConnectionStatusChangedCallback_t> c_onConnectionChange = null;

		internal QSteamServer()
		{
			QSteam.CreateLobby();
			c_onConnectionChange = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
			listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, new SteamNetworkingConfigValue_t[0]);
		}

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
				else if (!QSteam.CurrentLobby.Members.ContainsKey(clientSteamID))
				{
					Debug.LogError(nameof(QSteamServer) + " 非房间内玩家[" + clientSteamID.GetName() + "]无法连接");
					SteamNetworkingSockets.CloseConnection(param.m_hConn, 0, "非房间内玩家", false);
					return;
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
				OnConnected.Invoke((int)param.m_hConn.m_HSteamNetConnection);
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

		private void InternalDisconnect(HSteamNetConnection socket)
		{
			if (ConnectClients.ContainsKey(socket))
			{
				OnDisconnected.Invoke((int)socket.m_HSteamNetConnection);
				SteamNetworkingSockets.CloseConnection(socket, 0, "正常断开连接", false);
				QDebug.Log(nameof(QSteamServer) + " 玩家 [" + new CSteamID(ConnectClients[socket].m_SteamID).GetName() + "] 断开连接.");
				ConnectClients.Remove(socket);
			}
		}

		public void Disconnect(int connectionId)
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

		public void FlushData()
		{
			foreach (HSteamNetConnection conn in ConnectClients.Keys)
			{
				SteamNetworkingSockets.FlushMessagesOnConnection(conn);
			}
		}

		public void ReceiveData()
		{
			foreach (HSteamNetConnection conn in ConnectClients.Keys.ToList())
			{
				IntPtr[] ptrs = new IntPtr[QSteamTransport.MAX_MESSAGES];
				int messageCount;
				if ((messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(conn, ptrs, QSteamTransport.MAX_MESSAGES)) > 0)
				{
					for (int i = 0; i < messageCount; i++)
					{
						OnReceivedData((int)conn.m_HSteamNetConnection, new ArraySegment<byte>(ptrs[i].ToBytes()));
					}
				}
			}
		}

		public void Send(int connectionId, byte[] data)
		{
			var connection = new HSteamNetConnection((uint)connectionId);
			if (ConnectClients.ContainsKey(connection))
			{
				EResult res = connection.Send(data);

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
		public void Shutdown()
		{
			QSteam.LeaveLobby();
			SteamNetworkingSockets.CloseListenSocket(listenSocket);
			if (c_onConnectionChange != null)
			{
				c_onConnectionChange.Dispose();
				c_onConnectionChange = null;
			}
		}
	}
	public class QSteamClient
	{
		public bool Connected { get; private set; }


		internal event Action<ArraySegment<byte>> OnReceivedData;
		internal event Action OnConnected;
		internal event Action OnDisconnected;
		private Callback<SteamNetConnectionStatusChangedCallback_t> c_onConnectionChange = null;
		private HSteamNetConnection HostConnection;

		internal async void Connect(string host)
		{
			c_onConnectionChange = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
			try
			{
				await QSteam.JoinLobby(new CSteamID(UInt64.Parse(host)));
				SteamNetworkingIdentity netId = new SteamNetworkingIdentity();
				netId.SetSteamID(QSteam.CurrentLobby.Owner);
				HostConnection = SteamNetworkingSockets.ConnectP2P(ref netId, 0, 0, new SteamNetworkingConfigValue_t[0]);
				QDebug.Log(nameof(QSteamClient) + " 尝试连接 " + QSteam.CurrentLobby.Owner.GetName());

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
			if (HostConnection.m_HSteamNetConnection != 0)
			{
				QDebug.Log(nameof(QSteamClient) + " 主动断开连接");
				SteamNetworkingSockets.CloseConnection(HostConnection, 0, " 客户端主动断开连接 ", false);
				HostConnection.m_HSteamNetConnection = 0;
			}
		}

		protected void Dispose()
		{
			if (c_onConnectionChange != null)
			{
				c_onConnectionChange.Dispose();
				c_onConnectionChange = null;
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
			IntPtr[] ptrs = new IntPtr[QSteamTransport.MAX_MESSAGES];
			int messageCount = 0;
			if ((messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(HostConnection, ptrs, QSteamTransport.MAX_MESSAGES)) > 0)
			{
				for (int i = 0; i < messageCount; i++)
				{
					OnReceivedData?.Invoke(new ArraySegment<byte>(ptrs[i].ToBytes()));
				}
			}
		}

		public void Send(byte[] data)
		{
			EResult res = HostConnection.Send(data);

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

		private void OnConnectionFailed()
		{
			OnDisconnected.Invoke();
		}
		public void FlushData()
		{
			SteamNetworkingSockets.FlushMessagesOnConnection(HostConnection);
		}

	}
}
#endif
