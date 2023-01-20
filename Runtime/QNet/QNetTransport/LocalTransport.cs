using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Net
{
	public class LocalTransport : QNetTransport
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

		public override void ClientSend(ArraySegment<byte> segment)
		{
			OnServerDataReceived?.Invoke(0,segment);
		}

		public override void ServerDisconnect(int connectionId)
		{
			
		}

		public override string ServerGetClientAddress(int connectionId)
		{
			return "Local";
		}

		public override void ServerSend(int connectionId, ArraySegment<byte> segment)
		{
			OnClientDataReceived.Invoke(segment);
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
