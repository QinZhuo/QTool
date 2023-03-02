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
			var id = QSteam.Id;
			SteamNetworkingUtils.InitRelayNetworkAccess();
			base.Awake();
		}
		public override void ServerStart()
		{
			if (!ServerActive)
			{
				Server = new QSteamServer(maxConnections);
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
		public override void ClientDisconnect()
		{
			Client?.Disconnect();
			Client = null;
			QDebug.Log(nameof(QSteamTransport) + "客户端断开连接");
			base.ClientDisconnect();
		}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		private string ServerIp = "localhost";
		public override void DebugGUI()
		{
			if (ServerActive)
			{
				GUILayout.Label(QSteam.Id.ToString());
			}
			if (!ClientConnected)
			{
				ServerIp = GUILayout.TextField(ServerIp);
				if (GUILayout.Button("开启客户端", GUILayout.Width(150), GUILayout.Height(50)))
				{
					GetComponent<QNetManager>().StartClient(ServerIp);
				}
				if (GUILayout.Button("快速开始"))
				{
					 _=QSteam.FastStart();
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

		private int maxConnections;
		public Dictionary<HSteamNetConnection, CSteamID> ConnectClients = new Dictionary<HSteamNetConnection, CSteamID>();

		private HSteamListenSocket listenSocket;

		private Callback<SteamNetConnectionStatusChangedCallback_t> c_onConnectionChange = null;

		internal QSteamServer(int maxConnections)
		{
			this.maxConnections = maxConnections;
			c_onConnectionChange = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
			SteamNetworkingUtils.InitRelayNetworkAccess();
			listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, new SteamNetworkingConfigValue_t[0]);
		}

		private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
		{
			ulong clientSteamID = param.m_info.m_identityRemote.GetSteamID64();
			if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
			{
				if (ConnectClients.Count >= maxConnections)
				{
					Debug.Log($"Incoming connection {clientSteamID} would exceed max connection count. Rejecting.");
					SteamNetworkingSockets.CloseConnection(param.m_hConn, 0, "Max Connection Count", false);
					return;
				}

				EResult res;

				if ((res = SteamNetworkingSockets.AcceptConnection(param.m_hConn)) == EResult.k_EResultOK)
				{
					Debug.Log($"Accepting connection {clientSteamID}");
				}
				else
				{
					Debug.Log($"Connection {clientSteamID} could not be accepted: {res.ToString()}");
				}
			}
			else if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
			{
				var steamId = param.m_info.m_identityRemote.GetSteamID();
				ConnectClients.Add(param.m_hConn, steamId);
				OnConnected.Invoke((int)param.m_hConn.m_HSteamNetConnection);
				Debug.Log(nameof(QSteamServer)+"["+ (int)param.m_hConn.m_HSteamNetConnection+"]["+clientSteamID+"]["+steamId+"]客户端连接成功");
			}
			else if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer || param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
			{
				InternalDisconnect(param.m_hConn);
			}
			else
			{
				Debug.Log($"Connection {clientSteamID} state changed: {param.m_info.m_eState.ToString()}");
			}
		}

		private void InternalDisconnect(HSteamNetConnection socket)
		{
			if (ConnectClients.ContainsKey(socket))
			{
				OnDisconnected.Invoke((int)socket.m_HSteamNetConnection);
				SteamNetworkingSockets.CloseConnection(socket, 0, "Graceful disconnect", false);
				ConnectClients.Remove(socket);
				Debug.Log($"Client with ConnectionID {ConnectClients[socket].m_SteamID} disconnected.");
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
		private CSteamID hostSteamID = CSteamID.Nil;
		private HSteamNetConnection HostConnection;
	
		internal async void Connect(string host)
		{
			c_onConnectionChange = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
			try
			{
				hostSteamID = new CSteamID(UInt64.Parse(host));
				SteamNetworkingIdentity smi = new SteamNetworkingIdentity();
				smi.SetSteamID(hostSteamID);

				SteamNetworkingConfigValue_t[] options = new SteamNetworkingConfigValue_t[] { };
				HostConnection = SteamNetworkingSockets.ConnectP2P(ref smi, 0, options.Length, options);
				QDebug.Log("尝试连接 " + host);
				if (!await QTask.Wait(5,true).IsCancel()&&!Connected)
				{
					Debug.LogError(nameof(QSteamClient)+" 连接 "+ host +" 超时");
					OnConnectionFailed();
					return;
				}

			}
			catch (FormatException)
			{
				Debug.LogError(nameof(QSteamClient) + "连接出错 ["+host+"]不是SteamId");
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
			ulong clientSteamID = param.m_info.m_identityRemote.GetSteamID64();
			if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
			{
				OnConnected.Invoke();
				Debug.Log("Connection established.");
				Connected = true;
			}
			else if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer || param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
			{
				Debug.Log($"Connection was closed by peer, {param.m_info.m_szEndDebug}");
				Disconnect();
			}
			else
			{
				Debug.Log($"Connection state changed: {param.m_info.m_eState.ToString()} - {param.m_info.m_szEndDebug}");
			}
		}

		public void Disconnect()
		{
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
