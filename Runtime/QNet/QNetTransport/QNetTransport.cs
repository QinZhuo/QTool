using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Net
{
	
	public abstract class QNetTransport : MonoBehaviour
	{
		#region 客户端
		public abstract string ClientPlayerId { get; }
		public Action OnClientConnected;
		public Action<ArraySegment<byte>> OnClientDataReceived;
		public Action<ArraySegment<byte>> OnClientDataSent;
		public Action<Exception> OnClientError;
		public Action OnClientDisconnected;
		public abstract bool ClientConnected { get; }
		public abstract void ClientConnect(string address);
		public abstract void ClientSend(ArraySegment<byte> segment);
		public virtual void ClientSend(byte[] segment)
		{
			ClientSend(new ArraySegment<byte>(segment));
		}

		public abstract void ClientDisconnect();
		public virtual void ClientEarlyUpdate() { }
		public virtual void ClientLateUpdate() { }
		#endregion

		#region 服务器
		public Action<int> OnServerConnected;
		public Action<int, ArraySegment<byte>> OnServerDataReceived;
		public Action<int, ArraySegment<byte>> OnServerDataSent;
		public Action<int, Exception> OnServerError;
		public Action<int> OnServerDisconnected;
		public abstract bool ServerActive { get; }
		public abstract void ServerStart();
		
		public abstract void ServerSend(int connectionId, ArraySegment<byte> segment);
		public virtual void ServerSend(int connectionId, byte[] segment)
		{
			ServerSend(connectionId, new ArraySegment<byte>(segment));
		}
		public abstract void ServerDisconnect(int connectionId);
		public abstract string ServerGetClientAddress(int connectionId);
		public abstract void ServerStop();
		public virtual void ServerEarlyUpdate() { }
		public virtual void ServerLateUpdate() { }
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
