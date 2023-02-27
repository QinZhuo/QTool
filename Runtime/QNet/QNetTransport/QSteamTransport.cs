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
using UnityEngine;
namespace QTool.Net
{
	public class QSteamTransport : QNetTransport
	{
		public override bool ServerActive => throw new NotImplementedException();
		public override void ServerStart()
		{
		}
		public override void ServerStop()
		{
			throw new NotImplementedException();
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
	}
}
#endif
