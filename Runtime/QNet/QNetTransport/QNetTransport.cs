using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Net
{
	[RequireComponent(typeof(QNetManager))]
	public abstract class QNetTransport : MonoBehaviour
	{
		public QNetManager Manager { get; private set; }
		protected virtual void Awake()
		{
			Manager = GetComponent<QNetManager>();
			OnClientConnected += () => ClientConnected = true;
			QEventManager.RegisterOnce(QToolEvent.游戏退出, Shutdown);
		}
		protected virtual void OnDestroy()
		{
			
		}
		/// <summary>
		/// 关闭所有网络连接
		/// </summary>
		public virtual void Shutdown()
		{
			ClientDisconnect();
			ServerStop();
		}
		#region 服务器
		/// <summary>
		/// 客户端连入服务器事件
		/// </summary>
		public Action<ulong> OnServerConnected;
		/// <summary>
		/// 服务器接收消息时事件
		/// </summary>
		public Action<ulong, byte[]> OnServerDataReceived;
		/// <summary>
		/// 服务器错误事件
		/// </summary>
		public Action<ulong, Exception> OnServerError;
		/// <summary>
		/// 服务器断开客户端连接事件
		/// </summary>
		public Action<ulong> OnServerDisconnected;
		/// <summary>
		/// 服务器是否启动
		/// </summary>
		public bool ServerActive { get; private set; }
		/// <summary>
		/// 启动服务器
		/// </summary>
		public virtual void ServerStart()
		{
			ServerActive = true;
		}
		/// <summary>
		/// 服务器发送消息给客户端 本地Id为0走内部直接调用
		/// </summary>
		/// <param name="connectionId">连接Id</param>
		/// <param name="segment">消息</param>
		public void CheckServerSend(ulong connectionId, byte[] segment)
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
		/// <summary>
		/// 服务器发送消息给客户端
		/// </summary>
		/// <param name="connectionId">连接Id</param>
		/// <param name="segment">消息</param>
		protected abstract void ServerSend(ulong connectionId, byte[] segment);
		/// <summary>
		/// 服务器发送逻辑更新
		/// </summary>
		public virtual void ServerSendUpdate() { }
		/// <summary>
		/// 服务器接收逻辑更新
		/// </summary>
		public virtual void ServerReceiveUpdate() { }
		/// <summary>
		/// 服务器断开客户端连接
		/// </summary>
		/// <param name="connectionId">客户端连接Id</param>
		public abstract void ServerDisconnect(ulong connectionId);
		/// <summary>
		/// 停止服务器
		/// </summary>
		public virtual void ServerStop()
		{
			ServerActive = false;
		}
	
		#endregion

		#region 客户端
		/// <summary>
		/// 客户端Id 每个客户端的Id必须时唯一的 主要用于断线重连
		/// </summary>
		public abstract string ClientId { get; }
		public virtual int Ping => 0;
		/// <summary>
		/// 客户端连接成功事件
		/// </summary>
		public Action OnClientConnected;
		/// <summary>
		/// 客户端接收消息事件
		/// </summary>
		public Action<ArraySegment<byte>> OnClientDataReceived;
		/// <summary>
		/// 客户端错误事件
		/// </summary>
		public Action<Exception> OnClientError;
		/// <summary>
		/// 客户端断开连接事件
		/// </summary>
		public Action OnClientDisconnected;
		/// <summary>
		/// 客户端是否连接
		/// </summary>
		public bool ClientConnected { get; private set; }
		/// <summary>
		/// 连接客户端
		/// </summary>
		/// <param name="address">主机地址</param>
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
		/// <summary>
		/// 客户端连接
		/// </summary>
		/// <param name="address">主机地址</param>
		protected abstract void ClientConnect(string address);
		/// <summary>
		/// 客户端发送消息 如果是主机会直接内部调用
		/// </summary>
		/// <param name="segment">消息</param>
		public void CheckClientSend(byte[] segment)
		{
			if (ServerActive)
			{
				OnServerDataReceived(0, segment);
			}
			else
			{
				ClientSend(segment);
			}
		}
		/// <summary>
		/// 客户端发送消息
		/// </summary>
		/// <param name="segment">消息</param>
		protected abstract void ClientSend(byte[] segment);
		/// <summary>
		/// 客户端发送消息更新
		/// </summary>
		public virtual void ClientSendUpdate() { }
		/// <summary>
		/// 客户端接收消息更新
		/// </summary>
		public virtual void ClientReceiveUpdate() { }
		/// <summary>
		/// 客户端断开连接
		/// </summary>
		public virtual void ClientDisconnect()
		{
			ClientConnected = false;
		}
		/// <summary>
		/// 禁止子类实现Update() LateUpdate()函数
		/// </summary>
		#endregion
#pragma warning disable UNT0001
		public void Update() { }
		public void LateUpdate() { }
#pragma warning restore UNT0001

		protected UnityEngine.UIElements.Button StartHostButton=null;
		/// <summary>
		/// 用于Debug显示的GUI
		/// </summary>
		public virtual void DebugUI()
		{
#if UNITY_2021_1_OR_NEWER

			if (StartHostButton == null)
			{
				StartHostButton = QToolManager.Instance.RootVisualElement.AddButton("开启主机", () =>
				{
					GetComponent<QNetManager>().StartHost();
				});
			}
			StartHostButton.visible = !ClientConnected && !ServerActive;

#endif
		}
	}
}
