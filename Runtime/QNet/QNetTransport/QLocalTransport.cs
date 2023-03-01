using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Net
{
	public class QLocalTransport : QNetTransport
	{
		public override string ClientPlayerId => SystemInfo.deviceName + (Debug.isDebugBuild ? "_" + System.Diagnostics.Process.GetCurrentProcess().Id : "");

		public override bool ClientConnected => clinetActive;


		public override bool ServerActive => serverActive;
		bool serverActive { get; set; } = false;
		bool clinetActive { get; set; } = false;
		public override void ClientConnect(string address)
		{
			clinetActive = true;
			OnServerConnected?.Invoke(0);
			OnClientConnected?.Invoke();
		}

		public override void ClientDisconnect()
		{
			
		}

		public override void ClientReceiveUpdate()
		{
			
		}

		public override void ClientSend(byte[] segment)
		{
			OnServerDataReceived?.Invoke(0,new ArraySegment<byte>(segment));
		}

		public override void ClientSendUpdate()
		{
			
		}

		public override void ServerDisconnect(int connectionId)
		{
			
		}

		public override void ServerReceiveUpdate()
		{
			
		}

		public override void ServerSend(int connectionId,byte[] segment)
		{
			OnClientDataReceived?.Invoke(new ArraySegment<byte>(segment));
		}

		public override void ServerSendUpdate()
		{
			
		}

		public override void ServerStart()
		{
			serverActive = true;
		}

		public override void ServerStop()
		{
			
		}
	}
}
