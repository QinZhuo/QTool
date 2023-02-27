using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Net
{
	public class QSteamTransport : QNetTransport
	{
		public override string ClientPlayerId => throw new NotImplementedException();

		public override bool ClientConnected => throw new NotImplementedException();

		public override bool ServerActive => throw new NotImplementedException();

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

		public override void ServerDisconnect(int connectionId)
		{
			throw new NotImplementedException();
		}

		public override string ServerGetClientAddress(int connectionId)
		{
			throw new NotImplementedException();
		}

		public override void ServerSend(int connectionId, ArraySegment<byte> segment)
		{
			throw new NotImplementedException();
		}

		public override void ServerStart()
		{
			throw new NotImplementedException();
		}

		public override void ServerStop()
		{
			throw new NotImplementedException();
		}
	}
}
