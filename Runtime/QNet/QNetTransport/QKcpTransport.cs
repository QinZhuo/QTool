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
        [QName("最大消息长度"), QReadonly]
        public int ReliableMaxMessageSize = 0;
        KcpServer Server { get; set; }
        KcpClient Client { get; set; }

		protected override void Awake()
        {
			Log.Info = Debug.Log;
			Log.Warning = Debug.LogWarning;
            Log.Error = Debug.LogError;

#if ENABLE_IL2CPP
            // NonAlloc doesn't work with IL2CPP builds
            NonAlloc = false;
#endif

            Client = NonAlloc
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

            Server = NonAlloc
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
			base.Awake();
        }

        void OnValidate()
        {
            ReliableMaxMessageSize = KcpConnection.ReliableMaxMessageSize(ReceiveWindowSize);
        }
		public override string ClientId => SystemInfo.deviceName + (Debug.isDebugBuild ? "_" + System.Diagnostics.Process.GetCurrentProcess().Id : "");
		public override int Ping => UnityPing==null||!UnityPing.isDone?0:UnityPing.time;
		private Ping UnityPing = null;
		private string pingAddress="";
		public override void FreshPing()
		{
			UnityPing?.DestroyPing();
			UnityPing = new Ping(pingAddress);
		}
		protected override void ClientConnect(string address)
        {
			pingAddress = address;
			FreshPing();
			Client.Connect(address, Port, NoDelay, Interval, FastResend, CongestionWindow, SendWindowSize, ReceiveWindowSize, Timeout, MaxRetransmit, MaximizeSendReceiveBuffersToOSLimit);
		}
        protected override void ClientSend(byte[] segment)
        {
			var data = new ArraySegment<byte>(segment);
			Client.Send(data, KcpChannel.Reliable);
        }
		public override void ClientDisconnect()
		{
			Client.Disconnect();
			base.ClientDisconnect();
		}
		public override void ClientReceiveUpdate()
		{
			Client.TickIncoming();
		}
        public override void ClientSendUpdate() => Client.TickOutgoing();


		public override void ServerStart()
		{
			Server.Start(Port);
			base.ServerStart();
		}
        protected override void ServerSend(int connectionId,byte[] segment)
		{
			var data = new ArraySegment<byte>(segment);
			Server.Send(connectionId, data, KcpChannel.Reliable);
        }
        public override void ServerDisconnect(int connectionId) =>  Server.Disconnect(connectionId);
		public override void ServerStop()
		{
			Server.Stop();
			base.ServerStop();
		}
		public override void ServerReceiveUpdate()
		{
			Server.TickIncoming();
		}
        public override void ServerSendUpdate() => Server.TickOutgoing();
#if UNITY_EDITOR||DEVELOPMENT_BUILD
		private string ServerIp = "localhost";
		protected UnityEngine.UIElements.TextField IpText = null;
		protected UnityEngine.UIElements.Button StartClientButton = null;
		public override void DebugUI()
		{
			base.DebugUI();
#if UNITY_2021_1_OR_NEWER

			if (StartClientButton == null && IpText == null)
			{
				IpText = QToolManager.Instance.RootVisualElement.AddText("主机Ip", ServerIp, newValue => ServerIp = newValue);
				StartClientButton = QToolManager.Instance.RootVisualElement.AddButton("连接主机", () =>
				{
					GetComponent<QNetManager>().StartClient(IpText.text);
				});
			}
			IpText.visible = !ClientConnected;
			StartClientButton.visible = !ClientConnected && !ServerActive;

#endif
		}
#endif
	}
}
