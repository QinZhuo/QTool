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
		public override bool ServerActive => Server != null;
		QSteamServer Server { get; set; }
		public int maxConnections = 10;
		public override void ServerStart()
		{
			SteamNetworkingUtils.InitRelayNetworkAccess();
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
		}
		public override void ServerSend(int connectionId,byte[] segment)
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
		}

		public override string ClientPlayerId => "test";
		public override bool ClientConnected => Client!=null&&Client.Connected;
		QSteamClient Client { get; set; }
		public override void ClientConnect(string address)
		{
			SteamNetworkingUtils.InitRelayNetworkAccess();
			Client = new QSteamClient();
			Client.OnConnected += OnClientConnected;
			Client.OnDisconnected += OnClientDisconnected;
			Client.OnReceivedData += OnClientDataReceived;
			Client.Connect(address);
		}
		public override void ClientSend(byte[] segment)
		{
			Client.Send(segment.ToArray());
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
		}
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
				Debug.Log($"Client with SteamID {clientSteamID} connected. Assigning connection id {steamId}");
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

		private CancellationTokenSource cancelToken;
		private TaskCompletionSource<Task> connectedComplete;
		private CSteamID hostSteamID = CSteamID.Nil;
		private HSteamNetConnection HostConnection;
		private List<Action> BufferedData;

		internal QSteamClient()
		{
			BufferedData = new List<Action>();
		}

		internal async void Connect(string host)
		{
			cancelToken = new CancellationTokenSource();
			c_onConnectionChange = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);

			try
			{
				hostSteamID = new CSteamID(UInt64.Parse(host));
				connectedComplete = new TaskCompletionSource<Task>();
				OnConnected += SetConnectedComplete;

				SteamNetworkingIdentity smi = new SteamNetworkingIdentity();
				smi.SetSteamID(hostSteamID);

				SteamNetworkingConfigValue_t[] options = new SteamNetworkingConfigValue_t[] { };
				HostConnection = SteamNetworkingSockets.ConnectP2P(ref smi, 0, options.Length, options);

				Task connectedCompleteTask = connectedComplete.Task;
				Task timeOutTask = Task.Delay(5, cancelToken.Token);

				if (await Task.WhenAny(connectedCompleteTask, timeOutTask) != connectedCompleteTask)
				{
					if (cancelToken.IsCancellationRequested)
					{
						Debug.LogError($"The connection attempt was cancelled.");
					}
					else if (timeOutTask.IsCompleted)
					{
						Debug.LogError($"Connection to {host} timed out.");
					}

					OnConnected -= SetConnectedComplete;
					OnConnectionFailed();
				}

				OnConnected -= SetConnectedComplete;
			}
			catch (FormatException)
			{
				Debug.LogError($"Connection string was not in the right format. Did you enter a SteamId?");
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
				Connected = true;
				OnConnected.Invoke();
				Debug.Log("Connection established.");

				if (BufferedData.Count > 0)
				{
					Debug.Log($"{BufferedData.Count} received before connection was established. Processing now.");
					{
						foreach (Action a in BufferedData)
						{
							a();
						}
					}
				}
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
			cancelToken?.Cancel();
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
			int messageCount;

			if ((messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(HostConnection, ptrs, QSteamTransport.MAX_MESSAGES)) > 0)
			{
				for (int i = 0; i < messageCount; i++)
				{
					var data = ptrs[i].ProcessMessage();
					if (Connected)
					{
						OnReceivedData?.Invoke(new ArraySegment<byte>(data));
					}
					else
					{
						BufferedData.Add(() => OnReceivedData?.Invoke(new ArraySegment<byte>(data)));
					}
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

		private void SetConnectedComplete() => connectedComplete.SetResult(connectedComplete.Task);
		private void OnConnectionFailed() => OnDisconnected.Invoke();
		public void FlushData()
		{
			SteamNetworkingSockets.FlushMessagesOnConnection(HostConnection);
		}
	}
}
#endif
