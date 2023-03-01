using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Net
{
	public class QLocalTransport : QNetTransport
	{
		public override string ClientId => SystemInfo.deviceName + (Debug.isDebugBuild ? "_" + System.Diagnostics.Process.GetCurrentProcess().Id : "");
		public override void ClientConnect(string address) { }
		public override void ClientReceiveUpdate() { }
		protected override void ClientSend(byte[] segment) { }
		public override void ServerDisconnect(int connectionId) { }
		protected override void ServerSend(int connectionId, byte[] segment) { }
	}
}
