using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Net
{
	public class QLocalTransport : QNetTransport
	{
		public override ulong PlayerId => (ulong)SystemInfo.graphicsDeviceID;
		protected override void ClientConnect(string address) { }
		public override void ClientReceiveUpdate() { }
		protected override void ClientSend(byte[] segment) { }
		public override void ServerDisconnect(ulong connectionId) { }
		protected override void ServerSend(ulong connectionId, byte[] segment) { }
	}
}
