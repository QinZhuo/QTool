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
			_= QSteam.FreshLobbys();
		}
		public override void ServerStart()
		{
			if (!ServerActive)
			{
				Server = new QSteamServer();
				Server.OnConnected += OnServerConnected;
				Server.OnDisconnected += OnServerDisconnected;
				Server.OnReceivedData += OnServerDataReceived;
				Server.OnReceivedError += OnServerError;
			}
			else
			{
				Debug.LogError("服务器已正在运行");
			}
			base.ServerStart();
		}
		protected override void ServerSend(int connectionId,byte[] segment)
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
				QDebug.Log(nameof(QSteamTransport) + "服务器已关闭");
			}
			base.ServerStop();
		}

		public override string ClientId => QSteam.Id.ToString();
		QSteamClient Client { get; set; }
		public override void ClientConnect(string address)
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
		[QName]
		public async void Steamtest()
		{
			ClientConnect(QSteam.CurrentLobby.steamID.ToString());
			//await QSteam.CreateLobby();

			//Callback<SteamNetConnectionStatusChangedCallback_t>.Create((param) =>
			//{
			//	var clientSteamID = param.m_info.m_identityRemote.GetSteamID();
			//	if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
			//	{
			//		SteamNetworkingSockets.AcceptConnection(param.m_hConn);
			//		QDebug.Log("接收 " + clientSteamID.GetName());
			//	}
			//	else
			//	{
			//		QDebug.Log(nameof(QSteamServer) + " " + clientSteamID.GetName() + " 连接状态更改 " + param.m_info.m_eState);
			//	}
			//});
			//SteamNetworkingSockets.CreateListenSocketP2P(0, 0, new SteamNetworkingConfigValue_t[0]);
			//var netId = new SteamNetworkingIdentity();
			//netId.SetSteamID(QSteam.Id);
			//SteamNetworkingSockets.ConnectP2P(ref netId, 0, 0, new SteamNetworkingConfigValue_t[0]);
		}
		public override void ClientDisconnect()
		{
			Client?.Disconnect();
			Client = null;
			QDebug.Log(nameof(QSteamTransport) + "客户端断开连接");
			base.ClientDisconnect();
		}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		private Vector2 ScrollPosition=Vector2.zero;
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
									GetComponent<QNetManager>().StartClient(lobby.steamID.ToString());
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
		internal event Action<int, Exception> OnReceivedError;
		public Dictionary<HSteamNetConnection, CSteamID> ConnectClients = new Dictionary<HSteamNetConnection, CSteamID>();

		private HSteamListenSocket listenSocket;

		private Callback<SteamNetConnectionStatusChangedCallback_t> c_onConnectionChange = null;

		internal QSteamServer()
		{
			SteamNetworkingUtils.InitRelayNetworkAccess();
			_ = QSteam.CreateLobby();
			c_onConnectionChange = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
			listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, new SteamNetworkingConfigValue_t[0]);
		}

		private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
		{
			var clientSteamID = param.m_info.m_identityRemote.GetSteamID();
			if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
			{
				//if (clientSteamID.IsNull())
				//{
				//	Debug.Log($"Incoming connection {clientSteamID} would exceed max connection count. Rejecting.");
				//	SteamNetworkingSockets.CloseConnection(param.m_hConn, 0, "Max Connection Count", false);
				//	return;
				//}
				QDebug.Log(nameof(QSteamServer) + "[" + clientSteamID.GetName() + "]尝试连接");
				EResult res;

				if ((res = SteamNetworkingSockets.AcceptConnection(param.m_hConn)) == EResult.k_EResultOK)
				{
					QDebug.Log(nameof(QSteamServer)+ "["+ clientSteamID.GetName() + "]成功连接");
				}
				else
				{
					QDebug.LogWarning(nameof(QSteamServer) + "[" + clientSteamID.GetName() + "]连接失败:"+ res);
				}
			}
			else if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
			{
				var steamId = param.m_info.m_identityRemote.GetSteamID();
				ConnectClients.Add(param.m_hConn, steamId);
				OnConnected.Invoke((int)param.m_hConn.m_HSteamNetConnection);
				QDebug.Log(nameof(QSteamServer)+"["+ (int)param.m_hConn.m_HSteamNetConnection+"]["+clientSteamID+"]["+steamId+"]客户端连接成功");
			}
			else if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer || param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
			{
				Debug.LogError("断开连接");
				InternalDisconnect(param.m_hConn);
			}
			else
			{
				QDebug.Log(nameof(QSteamServer)+" "+clientSteamID.GetName()+ " 连接状态更改 "+param.m_info.m_eState);
			}
		}

		private void InternalDisconnect(HSteamNetConnection socket)
		{
			if (ConnectClients.ContainsKey(socket))
			{
				OnDisconnected.Invoke((int)socket.m_HSteamNetConnection);
				SteamNetworkingSockets.CloseConnection(socket, 0, "正常断开连接", false);
				Debug.Log(nameof(QSteamServer)+" 玩家 ["+new CSteamID(ConnectClients[socket].m_SteamID).GetName()+"] 断开连接.");
				ConnectClients.Remove(socket);
			}
		}

		public void Disconnect(int connectionId)
		{
			var connection = new HSteamNetConnection((uint)connectionId);
			if (ConnectClients.ContainsKey(connection))
			{
				Debug.Log($"Connection id {connectionId} disconnected.");
				SteamNetworkingSockets.CloseConnection(connection, 0, "Disconnected by server", false);
				ConnectClients.Remove(connection);
				OnDisconnected(connectionId);
			}
			else
			{
				Debug.LogWarning("Trying to disconnect unknown connection id: " + connectionId);
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
						OnReceivedData((int)conn.m_HSteamNetConnection,new ArraySegment<byte>(ptrs[i].ProcessMessage()));
					}
				}
			}
		}

		public void Send(int connectionId, byte[] data)
		{
			var connection = new HSteamNetConnection((uint)connectionId);
			if (ConnectClients.ContainsKey(connection))
			{
				EResult res = connection.SendSocket(data);

				if (res == EResult.k_EResultNoConnection || res == EResult.k_EResultInvalidParam)
				{
					Debug.Log($"Connection to {connectionId} was lost.");
					InternalDisconnect(connection);
				}
				else if (res != EResult.k_EResultOK)
				{
					Debug.LogError($"Could not send: {res.ToString()}");
				}
			}
			else
			{
				Debug.LogError("Trying to send on unknown connection: " + connectionId);
				OnReceivedError.Invoke(connectionId, new Exception("ERROR Unknown Connection"));
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
		public bool Error { get; private set; }


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
				netId.SetSteamID(QSteam.CurrentLobby.owner);
				HostConnection = SteamNetworkingSockets.ConnectP2P(ref netId, 0,0, new SteamNetworkingConfigValue_t[0]);
				QDebug.Log("尝试连接 " + QSteam.CurrentLobby.owner.GetName());

			}
			catch (FormatException)
			{
				Debug.LogError(nameof(QSteamClient) + "连接出错 ["+host+"]不是房间Id");
				Error = true;
				OnConnectionFailed();
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message);
				Error = true;
				OnConnectionFailed();
			}
			finally
			{
				if (Error)
				{
					Debug.LogError("Connection failed.");
					OnConnectionFailed();
				}
			}
		}

		private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
		{
			if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
			{
				OnConnected.Invoke();
				Debug.Log(nameof(QSteamClient) +" 连接["+ param.m_info.m_identityRemote.GetSteamID().GetName()+ "]成功");
				Connected = true;
			}
			else if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer || param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
			{
				Debug.LogError(nameof(QSteamClient) + " 连接被关闭 ["+ param.m_info.m_identityRemote.GetSteamID().GetName() +"]:"+param.m_info.m_szEndDebug);
				Disconnect();
			}
			else
			{
				Debug.Log(nameof(QSteamClient) + " 连接状态更改 " + param.m_info.m_eState+" : "+param.m_info.m_szEndDebug);
			}
		}

		public void Disconnect()
		{
			QSteam.LeaveLobby();
			Dispose();
			if (HostConnection.m_HSteamNetConnection != 0)
			{
				Debug.Log("Sending Disconnect message");
				SteamNetworkingSockets.CloseConnection(HostConnection, 0, "Graceful disconnect", false);
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
			Debug.Log("Disconnected.");
			SteamNetworkingSockets.CloseConnection(HostConnection, 0, "Disconnected", false);
		}

		public void ReceiveData()
		{
			IntPtr[] ptrs = new IntPtr[QSteamTransport.MAX_MESSAGES];
			int messageCount = 0;
			if ((messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(HostConnection, ptrs, QSteamTransport.MAX_MESSAGES)) > 0)
			{
				for (int i = 0; i < messageCount; i++)
				{
					OnReceivedData?.Invoke(new ArraySegment<byte>(ptrs[i].ProcessMessage()));
				}
			}
		}

		public void Send(byte[] data)
		{
			EResult res = HostConnection.SendSocket(data);

			if (res == EResult.k_EResultNoConnection || res == EResult.k_EResultInvalidParam)
			{
				Debug.Log($"Connection to server was lost.");
				InternalDisconnect();
			}
			else if (res != EResult.k_EResultOK)
			{
				Debug.LogError($"Could not send: {res.ToString()}");
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
