#if Steamworks
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
#if !DISABLESTEAMWORKS
using Steamworks;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
namespace QTool.Net
{
	public class QSteamTransport : QNetTransport
	{
		public override bool ServerActive => server != null;
		NextServer server;
		public int maxConnections = 10;
		public override void ServerStart()
		{
			SteamNetworkingUtils.InitRelayNetworkAccess();
			if (!ServerActive)
			{
				server = new NextServer(maxConnections);
				server.OnConnected += OnServerConnected;
				server.OnDisconnected += OnServerDisconnected;
				server.OnReceivedData += OnServerDataReceived;
				server.OnReceivedError += OnServerError;
			}
			else
			{
				Debug.LogError("服务器已正在运行");
			}
		}
		public override void ServerStop()
		{
			if (ServerActive)
			{
				server.Shutdown();
				server = null;
				QDebug.Log(nameof(QSteamTransport) + "服务器已关闭");
			}
		}
		public override void ServerSend(int connectionId, ArraySegment<byte> segment)
		{
			if (ServerActive)
			{
				server.Send(connectionId, segment.ToArray());
			}
		}
		public override void ServerDisconnect(int connectionId)
		{
			if (ServerActive)
			{
				server.Disconnect(connectionId);
			}
		}
		public override string ClientPlayerId => throw new NotImplementedException();
		public override bool ClientConnected => throw new NotImplementedException();

		public override void ClientConnect(string address)
		{
			throw new NotImplementedException();
		}
		public override void ClientDisconnect()
		{
			throw new NotImplementedException();
		}
		public override void ClientSend(ArraySegment<byte> segment)
		{
			throw new NotImplementedException();
		}


	}

	public class NextServer
	{
		internal event Action<int> OnConnected;
		internal event Action<int, ArraySegment<byte>> OnReceivedData;
		internal event Action<int> OnDisconnected;
		internal event Action<int, Exception> OnReceivedError;

		private int maxConnections;
		public Dictionary<HSteamNetConnection, CSteamID> ConnectClients = new Dictionary<HSteamNetConnection, CSteamID>();

		private HSteamListenSocket listenSocket;

		private Callback<SteamNetConnectionStatusChangedCallback_t> c_onConnectionChange = null;

		internal NextServer(int maxConnections)
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

		protected const int MAX_MESSAGES = 256;
		public void ReceiveData()
		{
			foreach (HSteamNetConnection conn in ConnectClients.Keys.ToList())
			{
				IntPtr[] ptrs = new IntPtr[MAX_MESSAGES];
				int messageCount;
				if ((messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(conn, ptrs, MAX_MESSAGES)) > 0)
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
}
#endif
