using System;
using System.Linq;
using System.Net;
using UnityEngine;
using Unity.Collections;
using kcp2k;
namespace QTool.Net
{
    [DisallowMultipleComponent,QName("Kcp传输")]
    public class KcpTransport : QNetTransport
    {
        // scheme used by this transport
        public const string Scheme = "kcp";
		public override string ClientPlayerId => SystemInfo.deviceName + (Debug.isDebugBuild ? "_"+ System.Diagnostics.Process.GetCurrentProcess().Id : "");
		// common
		[Header("传输配置")]
        public ushort Port = 7777;
        [Tooltip("双模式同时侦听 IPv6 和 IPv4。如果平台仅支持 IPv4，则禁用。")]
        public bool DualMode = true;
        [Tooltip("建议使用无延迟以减少延迟。这也可以在缓冲区未满的情况下更好地扩展。")]
        public bool NoDelay = true;
        [Tooltip("KCP 内部更新间隔。100 毫秒是 KCP 默认值，但建议使用较低的间隔，以最大程度地减少延迟并扩展到更多联网实体。")]
        public uint Interval = 10;
        [Tooltip("KCP 超时（以毫秒为单位）。请注意，KCP 会自动发送 ping。")]
        public int Timeout = 10000;

        [Header("Advanced")]
        [Tooltip("KCP 快速重新发送参数。更快的重新发送，更高的带宽成本。正常模式下为 0，涡轮增压模式下为 2。")]
        public int FastResend = 2;
        [Tooltip("KCP 拥塞窗口。在正常模式下启用，在涡轮模式下禁用。如果连接经常阻塞，请为大型游戏禁用此功能。")]
        public bool CongestionWindow = false; // KCP 'NoCongestionWindow' is false by default. here we negate it for ease of use.
        [Tooltip("可以修改 KCP 窗口大小以支持更高的负载。.")]
        public uint SendWindowSize = 4096; //Kcp.WND_SND; 32 by default. Mirror sends a lot, so we need a lot more.
        [Tooltip("可以修改 KCP 窗口大小以支持更高的负载。这也会增加最大邮件大小。")]
        public uint ReceiveWindowSize = 4096; //Kcp.WND_RCV; 128 by default. Mirror sends a lot, so we need a lot more.
        [Tooltip("KCP 将尝试在断开连接之前将丢失的消息重新传输到 MaxRetransmit（又名 dead_link）。")]
        public uint MaxRetransmit = Kcp.DEADLINK * 2; // default prematurely disconnects a lot of people (#3022). use 2x.
        [Tooltip("启用以使用位置分配非分配 Kcp Kcp服务器/客户端/连接版本。强烈推荐在所有 Unity 平台上使用。")]
        public bool NonAlloc = true;
        [Tooltip("启用以自动将客户端和服务器发送/接收缓冲区设置为操作系统限制。避免在重负载下缓冲区太小的问题，从而可能断开连接。如果这仍然太小，请增加操作系统限制。")]
        public bool MaximizeSendReceiveBuffersToOSLimit = true;

        [Header("Calculated Max (based on Receive Window Size)")]
        [Tooltip("KCP reliable max message size shown for convenience. Can be changed via ReceiveWindowSize.")]
        [ReadOnly] public int ReliableMaxMessageSize = 0; // readonly, displayed from OnValidate
        [Tooltip("KCP unreliable channel max message size for convenience. Not changeable.")]
        [ReadOnly] public int UnreliableMaxMessageSize = 0; // readonly, displayed from OnValidate

        // server & client (where-allocation NonAlloc versions)
        KcpServer server;
        KcpClient client;

        // debugging
        [Header("Debug")]
        public bool debugLog;
        // show statistics in OnGUI
        public bool statisticsGUI;
        // log statistics for headless servers that can't show them in GUI
        public bool statisticsLog;

        // translate Kcp <-> Mirror channels
        
        void Awake()
        {
            // logging
            //   Log.Info should use Debug.Log if enabled, or nothing otherwise
            //   (don't want to spam the console on headless servers)
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

            // client
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

            // server
            server = NonAlloc
                ? new KcpServerNonAlloc(
                      (connectionId) => OnServerConnected.Invoke(connectionId),
                      (connectionId, message, channel) => OnServerDataReceived.Invoke(connectionId, message),
                      (connectionId) => OnServerDisconnected.Invoke(connectionId),
                      (connectionId, error, reason) => OnServerError.Invoke(connectionId, new Exception(reason)),
                      DualMode,
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
                      DualMode,
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

            Debug.Log("KcpTransport initialized!");
        }

        void OnValidate()
        {
            // show max message sizes in inspector for convenience
            ReliableMaxMessageSize = KcpConnection.ReliableMaxMessageSize(ReceiveWindowSize);
            UnreliableMaxMessageSize = KcpConnection.UnreliableMaxMessageSize;
        }

        // client
        public override bool ClientConnected => client.connected;
        public override void ClientConnect(string address)
        {
            client.Connect(address, Port, NoDelay, Interval, FastResend, CongestionWindow, SendWindowSize, ReceiveWindowSize, Timeout, MaxRetransmit, MaximizeSendReceiveBuffersToOSLimit);
        }
        public override void ClientSend(ArraySegment<byte> segment)
        {
            client.Send(segment, KcpChannel.Reliable);

            // call event. might be null if no statistics are listening etc.
            OnClientDataSent?.Invoke(segment);
        }
        public override void ClientDisconnect() => client.Disconnect();
        // process incoming in early update
        public override void ClientEarlyUpdate()
        {
            // only process messages while transport is enabled.
            // scene change messsages disable it to stop processing.
            // (see also: https://github.com/vis2k/Mirror/pull/379)
            if (enabled) client.TickIncoming();
        }
        // process outgoing in late update
        public override void ClientLateUpdate() => client.TickOutgoing();

     
        public override bool ServerActive => server.IsActive();
        public override void ServerStart() => server.Start(Port);
        public override void ServerSend(int connectionId, ArraySegment<byte> segment)
        {

            server.Send(connectionId, segment,KcpChannel.Reliable);

            // call event. might be null if no statistics are listening etc.
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
            // only process messages while transport is enabled.
            // scene change messsages disable it to stop processing.
            // (see also: https://github.com/vis2k/Mirror/pull/379)
            if (enabled) server.TickIncoming();
        }
        // process outgoing in late update
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

        // PrettyBytes function from DOTSNET
        // pretty prints bytes as KB/MB/GB/etc.
        // long to support > 2GB
        // divides by floats to return "2.5MB" etc.
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

        public override string ToString() => "KCP";
    }
}
