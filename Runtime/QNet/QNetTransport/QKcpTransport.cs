using System;
using System.Linq;
using System.Net;
using UnityEngine;
using kcp2k;
using QTool.Inspector;
namespace QTool.Net
{
    [DisallowMultipleComponent,QName("Kcp传输")]
    public class QKcpTransport : QNetTransport
    {
	
		[QName("端口")]
        public ushort Port = 7777;
        [QName("无延迟"),Tooltip("建议使用无延迟以减少延迟。这也可以在缓冲区未满的情况下更好地扩展。")]
        public bool NoDelay = true;
        [QName("更新间隔"),Tooltip("KCP 内部更新间隔。100 毫秒是 KCP 默认值，但建议使用较低的间隔，以最大程度地减少延迟并扩展到更多联网实体。")]
        public uint Interval = 10;
        [QName("超时时长"),Tooltip("KCP 超时（以毫秒为单位）。请注意，KCP 会自动发送 ping。")]
        public int Timeout = 10000;
        [QName("快速重发"),Tooltip("KCP 快速重新发送参数。更快的重新发送，更高的带宽成本。正常模式下为 0，涡轮增压模式下为 2。")]
        public int FastResend = 2;
        [QName("拥塞窗口"),Tooltip("KCP 拥塞窗口。在正常模式下启用，在涡轮模式下禁用。如果连接经常阻塞，请为大型游戏禁用此功能。")]
        public bool CongestionWindow = false;
        [QName("发送窗口大小",nameof(CongestionWindow)), Tooltip("可以修改 KCP 窗口大小以支持更高的负载。.")]
        public uint SendWindowSize = 4096; 
        [QName("接收窗口大小", nameof(CongestionWindow)), Tooltip("可以修改 KCP 窗口大小以支持更高的负载。这也会增加最大邮件大小。")]
        public uint ReceiveWindowSize = 4096; 
        [QName("最大重传"),Tooltip("KCP 将尝试在断开连接之前将丢失的消息重新传输到 MaxRetransmit（又名 dead_link）。")]
        public uint MaxRetransmit = Kcp.DEADLINK * 2; 
        [QName("无内存分配"),Tooltip("启用以使用位置分配非分配 Kcp Kcp服务器/客户端/连接版本。强烈推荐在所有 Unity 平台上使用。")]
        public bool NonAlloc = true;
        [QName("自动缓冲区"),Tooltip("启用以自动将客户端和服务器发送/接收缓冲区设置为操作系统限制。避免在重负载下缓冲区太小的问题，从而可能断开连接。如果这仍然太小，请增加操作系统限制。")]
        public bool MaximizeSendReceiveBuffersToOSLimit = true;
        [QName("最大可靠消息"), QReadonly]
        public int ReliableMaxMessageSize = 0;
        [QName("最大不可靠消息"), QReadonly]
		public int UnreliableMaxMessageSize = 0;
        KcpServer server;
        KcpClient client;

		[QName("日志")]
        public bool debugLog;
		[QName("日志UI",nameof(debugLog))]
        public bool statisticsGUI;
		[QName("日志信息", nameof(debugLog))]
		public bool statisticsLog;
	
		void Awake()
        {
            if (debugLog)
                Log.Info = Debug.Log;
            else
                Log.Info = _ => {};
            Log.Warning = Debug.LogWarning;
            Log.Error = Debug.LogError;

#if ENABLE_IL2CPP
            // NonAlloc doesn't work with IL2CPP builds
            NonAlloc = false;
#endif

            client = NonAlloc
                ? new KcpClientNonAlloc(
                      () => OnClientConnected.Invoke(),
                      (message, channel) => OnClientDataReceived.Invoke(message),
                      () => OnClientDisconnected.Invoke(),
                      (error, reason) => OnClientError.Invoke(new Exception(reason)))
                : new KcpClient(
                      () => OnClientConnected.Invoke(),
                      (message, channel) => OnClientDataReceived.Invoke(message),
                      () => OnClientDisconnected.Invoke(),
                      (error, reason) => OnClientError.Invoke(new Exception(reason)));

            server = NonAlloc
                ? new KcpServerNonAlloc(
                      (connectionId) => OnServerConnected.Invoke(connectionId),
                      (connectionId, message, channel) => OnServerDataReceived.Invoke(connectionId, message),
                      (connectionId) => OnServerDisconnected.Invoke(connectionId),
                      (connectionId, error, reason) => OnServerError.Invoke(connectionId, new Exception(reason)),
                      true,
                      NoDelay,
                      Interval,
                      FastResend,
                      CongestionWindow,
                      SendWindowSize,
                      ReceiveWindowSize,
                      Timeout,
                      MaxRetransmit,
                      MaximizeSendReceiveBuffersToOSLimit)
                : new KcpServer(
                      (connectionId) => OnServerConnected.Invoke(connectionId),
                      (connectionId, message, channel) => OnServerDataReceived.Invoke(connectionId, message),
                      (connectionId) => OnServerDisconnected.Invoke(connectionId),
                      (connectionId, error, reason) => OnServerError.Invoke(connectionId, new Exception(reason)),
                      true,
                      NoDelay,
                      Interval,
                      FastResend,
                      CongestionWindow,
                      SendWindowSize,
                      ReceiveWindowSize,
                      Timeout,
                      MaxRetransmit,
                      MaximizeSendReceiveBuffersToOSLimit);

            if (statisticsLog)
                InvokeRepeating(nameof(OnLogStatistics), 1, 1);
        }

        void OnValidate()
        {
            ReliableMaxMessageSize = KcpConnection.ReliableMaxMessageSize(ReceiveWindowSize);
            UnreliableMaxMessageSize = KcpConnection.UnreliableMaxMessageSize;
        }
		public override string ClientPlayerId => SystemInfo.deviceName + (Debug.isDebugBuild ? "_" + System.Diagnostics.Process.GetCurrentProcess().Id : "");
		public override bool ClientConnected => client.connected;
        public override void ClientConnect(string address)
        {
            client.Connect(address, Port, NoDelay, Interval, FastResend, CongestionWindow, SendWindowSize, ReceiveWindowSize, Timeout, MaxRetransmit, MaximizeSendReceiveBuffersToOSLimit);
        }
        public override void ClientSend(ArraySegment<byte> segment)
        {
            client.Send(segment, KcpChannel.Reliable);
            OnClientDataSent?.Invoke(segment);
        }
        public override void ClientDisconnect() => client.Disconnect();
        public override void ClientEarlyUpdate()
        {
            if (enabled) client.TickIncoming();
        }
        public override void ClientLateUpdate() => client.TickOutgoing();

     
        public override bool ServerActive => server.IsActive();
        public override void ServerStart() => server.Start(Port);
        public override void ServerSend(int connectionId, ArraySegment<byte> segment)
        {
            server.Send(connectionId, segment,KcpChannel.Reliable);
            OnServerDataSent?.Invoke(connectionId, segment);
        }
        public override void ServerDisconnect(int connectionId) =>  server.Disconnect(connectionId);
        public override string ServerGetClientAddress(int connectionId)
        {
            IPEndPoint endPoint = server.GetClientEndPoint(connectionId);
            return endPoint != null ? endPoint.Address.ToString() : "";
        }
        public override void ServerStop() => server.Stop();
        public override void ServerEarlyUpdate()
        {
            if (enabled) server.TickIncoming();
        }
        public override void ServerLateUpdate() => server.TickOutgoing();
        public long GetAverageMaxSendRate() =>
            server.connections.Count > 0
                ? server.connections.Values.Sum(conn => (long)conn.MaxSendRate) / server.connections.Count
                : 0;
        public long GetAverageMaxReceiveRate() =>
            server.connections.Count > 0
                ? server.connections.Values.Sum(conn => (long)conn.MaxReceiveRate) / server.connections.Count
                : 0;
        long GetTotalSendQueue() =>
            server.connections.Values.Sum(conn => conn.SendQueueCount);
        long GetTotalReceiveQueue() =>
            server.connections.Values.Sum(conn => conn.ReceiveQueueCount);
        long GetTotalSendBuffer() =>
            server.connections.Values.Sum(conn => conn.SendBufferCount);
        long GetTotalReceiveBuffer() =>
            server.connections.Values.Sum(conn => conn.ReceiveBufferCount);

    
        public static string PrettyBytes(long bytes)
        {
            // bytes
            if (bytes < 1024)
                return $"{bytes} B";
            // kilobytes
            else if (bytes < 1024L * 1024L)
                return $"{(bytes / 1024f):F2} KB";
            // megabytes
            else if (bytes < 1024 * 1024L * 1024L)
                return $"{(bytes / (1024f * 1024f)):F2} MB";
            // gigabytes
            return $"{(bytes / (1024f * 1024f * 1024f)):F2} GB";
        }

// OnGUI allocates even if it does nothing. avoid in release.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (!statisticsGUI) return;
			GUILayout.Space(50);

			if (ServerActive)
            {
                GUILayout.BeginVertical("Box");
                GUILayout.Label("SERVER");
                GUILayout.Label($"  connections: {server.connections.Count}");
                GUILayout.Label($"  MaxSendRate (avg): {PrettyBytes(GetAverageMaxSendRate())}/s");
                GUILayout.Label($"  MaxRecvRate (avg): {PrettyBytes(GetAverageMaxReceiveRate())}/s");
                GUILayout.Label($"  SendQueue: {GetTotalSendQueue()}");
                GUILayout.Label($"  ReceiveQueue: {GetTotalReceiveQueue()}");
                GUILayout.Label($"  SendBuffer: {GetTotalSendBuffer()}");
                GUILayout.Label($"  ReceiveBuffer: {GetTotalReceiveBuffer()}");
                GUILayout.EndVertical();
            }
            if (ClientConnected)
            {
                GUILayout.BeginVertical("Box");
                GUILayout.Label("CLIENT");
                GUILayout.Label($"  MaxSendRate: {PrettyBytes(client.connection.MaxSendRate)}/s");
                GUILayout.Label($"  MaxRecvRate: {PrettyBytes(client.connection.MaxReceiveRate)}/s");
                GUILayout.Label($"  SendQueue: {client.connection.SendQueueCount}");
                GUILayout.Label($"  ReceiveQueue: {client.connection.ReceiveQueueCount}");
                GUILayout.Label($"  SendBuffer: {client.connection.SendBufferCount}");
                GUILayout.Label($"  ReceiveBuffer: {client.connection.ReceiveBufferCount}");
                GUILayout.EndVertical();
            }
        }
#endif

        void OnLogStatistics()
        {
            if (ServerActive)
            {
                string log = "kcp SERVER @ time: " + Time.timeAsDouble + "\n";
                log += $"  connections: {server.connections.Count}\n";
                log += $"  MaxSendRate (avg): {PrettyBytes(GetAverageMaxSendRate())}/s\n";
                log += $"  MaxRecvRate (avg): {PrettyBytes(GetAverageMaxReceiveRate())}/s\n";
                log += $"  SendQueue: {GetTotalSendQueue()}\n";
                log += $"  ReceiveQueue: {GetTotalReceiveQueue()}\n";
                log += $"  SendBuffer: {GetTotalSendBuffer()}\n";
                log += $"  ReceiveBuffer: {GetTotalReceiveBuffer()}\n\n";
                Debug.Log(log);
            }

            if (ClientConnected)
            {
                string log = "kcp CLIENT @ time: " +Time.timeAsDouble+ "\n";
                log += $"  MaxSendRate: {PrettyBytes(client.connection.MaxSendRate)}/s\n";
                log += $"  MaxRecvRate: {PrettyBytes(client.connection.MaxReceiveRate)}/s\n";
                log += $"  SendQueue: {client.connection.SendQueueCount}\n";
                log += $"  ReceiveQueue: {client.connection.ReceiveQueueCount}\n";
                log += $"  SendBuffer: {client.connection.SendBufferCount}\n";
                log += $"  ReceiveBuffer: {client.connection.ReceiveBufferCount}\n\n";
                Debug.Log(log);
            }
        }

    }
}
