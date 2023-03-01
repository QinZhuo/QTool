using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Net
{
	
	public abstract class QNetTransport : MonoBehaviour
	{
		#region 服务器
		public Action<int> OnServerConnected;
		public Action<int, ArraySegment<byte>> OnServerDataReceived;
		public Action<int, ArraySegment<byte>> OnServerDataSent;
		public Action<int, Exception> OnServerError;
		public Action<int> OnServerDisconnected;
		public abstract bool ServerActive { get; }
		public abstract void ServerStart();

		public abstract void ServerSend(int connectionId, byte[] segment);
		public abstract void ServerSendUpdate();
		public abstract void ServerReceiveUpdate();
		public abstract void ServerDisconnect(int connectionId);
		public abstract void ServerStop();
	
		#endregion

		#region 客户端
		public abstract string ClientPlayerId { get; }
		public Action OnClientConnected;
		public Action<ArraySegment<byte>> OnClientDataReceived;
		public Action<ArraySegment<byte>> OnClientDataSent;
		public Action<Exception> OnClientError;
		public Action OnClientDisconnected;
		public abstract bool ClientConnected { get; }
		public abstract void ClientConnect(string address);
		public abstract void ClientSend(byte[] segment);
		public abstract void ClientSendUpdate();
		public abstract void ClientReceiveUpdate();
		public abstract void ClientDisconnect();

		#endregion
#pragma warning disable UNT0001
		public void Update() { }
		public void LateUpdate() { }
#pragma warning restore UNT0001 
		public virtual void Shutdown()
		{
			ClientDisconnect();
			ServerStop();
		}
		public virtual void OnDestroy()
		{
			Shutdown();
		}
	}
}
