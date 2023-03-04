using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Net
{
	public abstract class QNetTransport : MonoBehaviour
	{
		protected virtual void Awake()
		{
			OnClientConnected += () => ClientConnected = true;
			QToolManager.Instance.OnDestroyEvent += Shutdown;
		}
		public virtual void Shutdown()
		{
			ClientDisconnect();
			ServerStop();
		}
		#region 服务器
		public Action<int> OnServerConnected;
		public Action<int, ArraySegment<byte>> OnServerDataReceived;
		public Action<int, ArraySegment<byte>> OnServerDataSent;
		public Action<int, Exception> OnServerError;
		public Action<int> OnServerDisconnected;
		public bool ServerActive { get; private set; }
		
		public virtual void ServerStart()
		{
			ServerActive = true;
		}
		public void CheckServerSend(int connectionId, byte[] segment)
		{
			if (connectionId==0)
			{
				OnClientDataReceived(new ArraySegment<byte>(segment));
			}
			else
			{
				ServerSend(connectionId, segment);
			}
		}
		protected abstract void ServerSend(int connectionId, byte[] segment);
		public virtual void ServerSendUpdate() { }
		public virtual void ServerReceiveUpdate() { }
		public abstract void ServerDisconnect(int connectionId);
		public virtual void ServerStop()
		{
			ServerActive = false;
		}
	
		#endregion

		#region 客户端
		public abstract string ClientId { get; }
		public Action OnClientConnected;
		public Action<ArraySegment<byte>> OnClientDataReceived;
		public Action<ArraySegment<byte>> OnClientDataSent;
		public Action<Exception> OnClientError;
		public Action OnClientDisconnected;
		public bool ClientConnected { get; private set; }
		public void CheckClientConnect(string address)
		{
			if (ServerActive)
			{
				OnServerConnected?.Invoke(0);
				OnClientConnected?.Invoke();
			}
			else
			{
				ClientConnect(address);
			}
		}
		public abstract void ClientConnect(string address);
		public void CheckClientSend(byte[] segment)
		{
			if (ServerActive)
			{
				OnServerDataReceived(0, new ArraySegment<byte>(segment));
			}
			else
			{
				ClientSend(segment);
			}
		}
		protected abstract void ClientSend(byte[] segment);
		public virtual void ClientSendUpdate() { }
		public virtual void ClientReceiveUpdate() { }
		public virtual void ClientDisconnect()
		{
			ClientConnected = false;
		}

		#endregion
#pragma warning disable UNT0001
		public void Update() { }
		public void LateUpdate() { }
#pragma warning restore UNT0001
	
		
		public virtual void DebugGUI()
		{
			if (!ServerActive)
			{
				if (GUILayout.Button("开启主机", GUILayout.Width(200), GUILayout.Height(30)))
				{
					GetComponent<QNetManager>().StartHost();
				}
			}
		}
	}
}
