using QTool.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Timers;
using UnityEngine;
using QTool.Reflection;
using System.Collections;

namespace QTool.Net
{

	public class QNetManager : QInstanceBehaviour<QNetManager>
	{
		#region 基础逻辑	
		/// <summary>
		/// 管理器数据Id
		/// </summary>
		const ulong ManagerId = ulong.MaxValue;
		[QPopup, QName("传输方式")]
		public QNetTransport transport;
		[QName("网络帧率"), SerializeField, Tooltip("每秒进行多少次网络帧同时更改物理帧率 两者保持同步")]
		[Range(15, 60)]
		private int netFps = 30;
		[QName("玩家预制体")]
		public GameObject playerPrefab;
		/// <summary>
		/// 玩家数据
		/// </summary>
		public QList<ulong, QPlayerInfo> Players = new QList<ulong, QPlayerInfo>(() => new QPlayerInfo());
		public float NetDeltaTime => Time.fixedDeltaTime;
		/// <summary>
		/// 网络时间
		/// </summary>
		public float NetTime { get; private set; }
		/// <summary>
		/// 随机数
		/// </summary>
		public System.Random Random { get; private set; } = null;
		protected override void Awake()
		{
			base.Awake();
			Application.runInBackground = true;
#if UNITY_2022_1_OR_NEWER
			Physics.simulationMode = SimulationMode.Script;
#else
			Physics.autoSimulation = false;
#endif
			Physics.autoSyncTransforms = false;
			Time.fixedDeltaTime = 1f / netFps;
			QTool.AddPlayerLoop(typeof(QNetManager), QNetManagerLoop, "FixedUpdate");
			QToolManager.Instance.OnUpdateEvent -= QCoroutine.Update;
			QId.GetNewIdFunc = GetNetId;
		}
		private void OnDestroy()
		{
			if (QToolManager.IsExist)
			{
				QToolManager.Instance.OnUpdateEvent += QCoroutine.Update;
			}
			QTime.RevertScale(this);
			QTool.RemovePlayerLoop(typeof(QNetManager), "FixedUpdate");
			ServerUpdateTimer?.Clear();
			QCoroutine.StopAll();
			QId.GetNewIdFunc = QTool.GetGuid;
		}

		private void FixedUpdate()
		{
			ClientFixedUpdate();
			if (transport.ServerActive)
			{
				if (ServerUpdateTimer.Check(Time.fixedUnscaledDeltaTime))
				{
					ServerUpdate();
				}
			}
		}
		private void Update()
		{
#if UNITY_2022_1_OR_NEWER
			DebugUI();
#endif
			transport.ClientReceiveUpdate();
			transport.ClientSendUpdate();
			transport.ServerReceiveUpdate();
			transport.ServerSendUpdate();
		}
		public new static void Destroy(UnityEngine.Object obj)
		{
			var gameObj = obj.GetGameObject();
			if (gameObj != null)
			{
				var nets = gameObj.GetComponentsInChildren<QNetBehaviour>();
				foreach (var net in nets)
				{
					net.NetDestroy();
				}
			}
			GameObject.Destroy(obj);
		}
		#endregion
		#region 服务器逻辑
		/// <summary>
		/// 重连数据Id
		/// </summary>
		const int GameDataArrayId = int.MinValue;
		/// <summary>
		/// 重连时每次发送帧数据数量
		/// </summary>
		const int GameDataArrayLength = 200;
		/// <summary>
		/// 服务器更新计时器
		/// </summary>
		private QTimer ServerUpdateTimer;
		/// <summary>
		/// 服务器连接
		/// </summary>
		private List<ulong> ServerConnectList = new List<ulong>();
		/// <summary>
		/// 连接Id与玩家Id映射
		/// </summary>
		private QDictionary<ulong, ulong> PlayerIds = new QDictionary<ulong, ulong>();
		/// <summary>
		/// 服务器游戏数据
		/// </summary>
		public QDictionary<int, QList<ulong, QNetFrameData>> ServerGameData = new QDictionary<int, QList<ulong, QNetFrameData>>((key) => new QList<ulong, QNetFrameData>(() => new QNetFrameData()));
		/// <summary>
		/// 服务器帧索引
		/// </summary>
		public int ServerIndex { get; private set; } = 0;
		/// <summary>
		/// 服务器当前帧游戏数据
		/// </summary>
		public QList<ulong, QNetFrameData> ServerActionData => ServerGameData[ServerIndex];
		public void StartServer()
		{
			ServerActionData[ManagerId].InvokeEvent(nameof(QNetActionKey.ServerSeed), UnityEngine.Random.Range(0, int.MaxValue));
			transport.OnServerConnected = (id) =>
			{
				QDebug.Log("[" + id + "]连接主机");
				ServerConnectList.Add(id);
				var max = Mathf.CeilToInt(ServerIndex * 1f / GameDataArrayLength) * GameDataArrayLength;
				for (int startIndex = 0; startIndex < max; startIndex+=GameDataArrayLength)
				{
					var end = Mathf.Min(startIndex+GameDataArrayLength, ServerIndex);
					using (var writer = new QBinaryWriter())
					{
						writer.Write(GameDataArrayId);
						var count = end - startIndex;
						writer.Write(count);
						for (int i = startIndex; i < end; i++)
						{
							writer.WriteObject(i);
							writer.WriteObject(ServerGameData[i]);
						}
						QDebug.Log("服务端发送游戏数据 " + startIndex + "=>" + end + " [" + count + "]"+ writer.BaseStream.Length.ToSizeString());
						transport.CheckServerSend(id, writer.ToArray());
					}
				}
			}; 
			transport.OnServerDataReceived = (connectId, data) =>
			{
				var netAction= data.Deserialize<QNetFrameData>();
				foreach (var eventData in netAction.Events)
				{
					switch (eventData.Key)
					{
						case nameof(QNetActionKey.PlayerConnected):
							{
								var playerId = (ulong)eventData.Value[0];
								PlayerIds[connectId] = playerId;
								QDebug.Log("["+ServerIndex + "] 添加玩家[" + connectId + "][" + playerId+"]");
							}
							break;
						default:
							break;
					}
				}
				var playerKey = PlayerIds[connectId];
				ServerActionData[playerKey].MergeValues(netAction);
				ServerActionData[playerKey].Events.AddRange(netAction.Events);

			};
			transport.OnServerError = (id, e) =>
			{
				Debug.LogError(id + " " + e);
			};
			transport.OnServerDisconnected = (id) =>
			{
				ServerConnectList.Remove(id);
			};
			ServerUpdateTimer = new QTimer(Time.fixedDeltaTime);
			transport.ServerStart();
		}
		public void StartHost()
		{
			StartServer();
			StartClient();
		}
		private void ServerUpdate()
		{
			if (transport.ServerActive)
			{
				using (var writer = new QBinaryWriter())
				{
					writer.WriteObject(ServerIndex);
					writer.WriteObject(ServerActionData);
					var data = writer.ToArray();
					foreach (var player in ServerConnectList.ToArray())
					{
						transport.CheckServerSend(player, data);
					}
					ServerIndex++;
				}
			}
		}
		#endregion
		#region 客户端逻辑
		/// <summary>
		/// 客户端游戏数据
		/// </summary>
		public QDictionary<int, QList<ulong, QNetFrameData>> ClientGameData = new QDictionary<int, QList<ulong, QNetFrameData>>();
		/// <summary>
		/// 客户端帧索引
		/// </summary>
		public int ClientIndex { get; private set; } = 0;
		/// <summary>
		/// 本地数据
		/// </summary>
		public QDictionary<string, object> LocalValues = new QDictionary<string, object>();
		/// <summary>
		/// 发送数据
		/// </summary>
		private QNetFrameData SendData = new QNetFrameData();
		/// <summary>
		/// 客户端游戏数据
		/// </summary>
		public QList<ulong, QNetFrameData> ClientFrameData = new QList<ulong, QNetFrameData>(()=>new QNetFrameData());
		/// <summary>
		/// 同步检测标志
		/// </summary>
		private QNetSyncFlag QSyncCheck = new QNetSyncFlag();
		/// <summary>
		/// 同步检测列表
		/// </summary>
		internal Dictionary<string, List<QNetBehaviour>> QNetSyncList { get; private set; } = new Dictionary<string, List<QNetBehaviour>>();
		/// <summary>
		/// 网络更新事件
		/// </summary>
		internal event Action OnNetUpdate = null;
		/// <summary>
		/// 网络Id索引
		/// </summary>
		private int NetIdIndex { get; set; } = 0;
		/// <summary>
		/// 获取网络Id
		/// </summary>
		public string GetNetId()
		{
			return Instance.NetIdIndex++ + "_" + Instance.ClientIndex;
		}
		public void StartClient(string ip="127.0.0.1")
		{
			transport.OnClientConnected += () =>
			{
				PlayerAction(nameof(QNetActionKey.PlayerConnected), transport.PlayerId);
			};
			transport.OnClientDataReceived += (data) =>
			{
				using (var reader = new QBinaryReader(data))
				{
					ReceiveGameData(reader);
				}
			};
			transport.OnClientError += (e) =>
			{
				Debug.LogError(e);
			};
			transport.OnClientDisconnected += () =>
			{
				QDebug.Log("断开链接");
			};
			transport.CheckClientConnect(ip);
		}
		private void ReceiveGameData(QBinaryReader reader)
		{
			var index = reader.ReadInt32();
			if (index == GameDataArrayId)
			{
				var count = reader.ReadInt32();
				QDebug.Log("客户端接收游戏数据[" + count + "]" + reader.BaseStream.Length.ToSizeString());
				for (int i = 0; i < count; i++)
				{
					ReceiveGameData(reader);
				}
			}
			else
			{
				var GameData = reader.ReadObject<QList<ulong, QNetFrameData>>(); ;
				ClientGameData[index] = GameData;
			}
		}

		private void ClientFixedUpdate()
		{
			if (transport.ClientConnected)
			{
				if (SendData.Active)
				{
					transport.CheckClientSend(SendData.Serialize());
				}
				SendData.Clear();
				if (ClientGameData.ContainsKey(ClientIndex))
				{
					byte[] loadEventData = null;
					if (ClientGameData[ClientIndex].ContainsKey(ManagerId))
					{
						foreach (var eventData in ClientGameData[ClientIndex][ManagerId].Events)
						{
							switch (eventData.Key)
							{
								case nameof(QNetActionKey.SyncLoad):
									{
										if (eventData.Value[0] is byte[] loadData)
										{
											loadEventData = loadData;
										}
										else
										{
											Debug.LogWarning("同步数据为空");
										}
										QSyncCheck.Reset(ClientIndex);
									}
									break;
								case nameof(QNetActionKey.ServerSeed):
									{
										var seed = (int)eventData.Value[0];
										Random = new System.Random(seed);
										QRandom.Instance = Random;
										QDebug.LogError("随机种子[" + seed + "]");
									}
									break;
								default:
									break;
							}
						}
					}
					foreach (var actionData in ClientGameData[ClientIndex])
					{
						var player = Players[actionData.Key];
						foreach (var eventData in actionData.Events)
						{
							switch (eventData.Key)
							{
								case nameof(QNetActionKey.PlayerConnected):
									if (player.gameObject==null)
									{
										if (playerPrefab != null)
										{
											var obj = Instantiate(playerPrefab);
											player.gameObject = obj;
											QDebug.Log("创建玩家[" + actionData.Key + "]对象[" + obj + "]");
											foreach (var qNet in obj.GetComponents<QNetBehaviour>())
											{
												qNet.PlayerId = actionData.Key;
											}
										}
										else
										{
											Debug.LogWarning(nameof(QNetManager) + " 未设置玩家预制体");
										}

									}
									break;
								case nameof(QNetActionKey.SyncCheck):
									{
										if (actionData.Key != transport.PlayerId)
										{
											var flag = (QNetSyncFlag)eventData.Value[0];
											if (flag.Index == QSyncCheck.Index)
											{
												if (flag.Value != QSyncCheck.Value)
												{
													Debug.LogWarning("[" + flag.Index + "/" + ClientIndex + "]同步验证失败[" + flag + "]:[" + QSyncCheck + "]");
													QSyncCheck.Reset(-1);
												}
											}
										}
									}
									break;
								case nameof(QNetActionKey.SyncLoad): break;
								default:
									player.Action?.Invoke(eventData.Key, eventData.Value);
									break;
							}
						}
						ClientFrameData[actionData.Key].MergeValues(actionData);
					}
					OnNetUpdate?.Invoke();
					QCoroutine.Update();
					Physics.Simulate(Time.fixedDeltaTime);
					Physics.SyncTransforms();
					if (loadEventData != null)
					{
						Debug.LogWarning("[" + ClientIndex + "]尝试修复同步");
						using (var reader = new QBinaryReader(loadEventData))
						{
							var Count = reader.ReadInt32();
							for (int i = 0; i < Count; i++)
							{
								using (var QIdData = new QBinaryReader(reader.ReadBytes()))
								{
									var qidKey = QIdData.ReadString();
									if (QNetSyncList.ContainsKey(qidKey))
									{
										var QIdCheck = QNetSyncList[qidKey];
										var dataCount = QIdData.ReadInt32();
										for (int j = 0; j < dataCount; j++)
										{
											try
											{
												QIdCheck[j].OnSyncLoad(QIdData);
											}
											catch (Exception e)
											{
												Debug.LogError("读取[" + qidKey + "]" + QIdCheck[j] + "出错 " + e.ToShortString(1000));
											}
										}
									}
								}
							}
						}
					}
					if (QSyncCheck.Index == -1)
					{
						if (transport.ServerActive)
						{
							QSyncCheck.Reset(ClientIndex);
							using (var writer = new QBinaryWriter())
							{
								writer.Write(QNetSyncList.Count);
								foreach (var QIdCheck in QNetSyncList)
								{
									using (var QIdData = new QBinaryWriter())
									{
										QIdData.Write(QIdCheck.Key);
										QIdData.Write(QIdCheck.Value.Count);
										for (int i = 0; i < QIdCheck.Value.Count; i++)
										{
											try
											{
												QIdCheck.Value[i].OnSyncSave(QIdData);
											}
											catch (Exception e)
											{
												Debug.LogError("保存[" + QIdCheck.Key + "]" + QIdCheck.Value[i] + "出错 " + e.ToShortString(1000));
											}
										}
										writer.Write(QIdData.ToArray());
									}
								}
								ServerActionData[ManagerId].InvokeEvent(nameof(QNetActionKey.SyncLoad), writer.ToArray());
							}
						}
					}
					if (ClientIndex % (netFps / 2) == 0)
					{
						QSyncCheck.Reset(ClientIndex);
						foreach (var checkList in QNetSyncList)
						{
							foreach (var check in checkList.Value)
							{
								QSyncCheck.Check(check.GetCheckValue());
							}
						}
						PlayerAction(nameof(QNetActionKey.SyncCheck), QSyncCheck);
					}
					ClientIndex++;
					NetTime += NetDeltaTime;
				}
			}
		}
		private void QNetManagerLoop()
		{
			if (ClientGameData.ContainsKey(ClientIndex + 1))
			{
				QTime.ChangeScale(this, 50);
			}
			else
			{
				QTime.RevertScale(this);
			}
		}
		public T PlayerValue<T>(ulong playerId, string key, T localValue)
		{
			if (transport.ClientConnected)
			{
				if (playerId == transport.PlayerId && (!LocalValues.ContainsKey(key) || !LocalValues.Equals(localValue)))
				{
					LocalValues[key] = localValue;
					SendData.Values[key] = localValue;
				}
				if (ClientFrameData.ContainsKey(playerId) && ClientFrameData[playerId].Values.ContainsKey(key) && ClientFrameData[playerId].Values[key] is T value)
				{
					return value;
				}
			}
			return default;
		}
		internal static void PlayerAction(string key, params object[] value)
		{
			if (Instance.transport.ClientConnected)
			{
				Instance.SendData.InvokeEvent(key, value);
			}
		}
		#endregion
		#region 测试UI
#if UNITY_2022_1_OR_NEWER
		UnityEngine.UIElements.Label DebugInfoLabel = null;
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
		private void DebugUI()
		{
			var info = "运行信息" + ClientIndex + "/" + ClientGameData.Count + " " + nameof(NetTime) + ":" + new TimeSpan((long)(TimeSpan.TicksPerSecond * NetTime)).ToString(@"hh\:mm\:ss") + " 延迟 " + transport.Ping + " ms";
			transport.DebugUI();
			if (DebugInfoLabel == null)
			{
				DebugInfoLabel = QToolManager.Instance.RootVisualElement.AddVisualElement().SetBackground().IgnoreClick().AddLabel(nameof(DebugInfoLabel));
				DebugInfoLabel.style.color = Color.white;
				DebugInfoLabel.pickingMode = UnityEngine.UIElements.PickingMode.Ignore;
				DebugInfoLabel.style.position = UnityEngine.UIElements.Position.Absolute;
				DebugInfoLabel.style.width = new UnityEngine.UIElements.Length(100, UnityEngine.UIElements.LengthUnit.Percent);
				DebugInfoLabel.style.textShadow = new UnityEngine.UIElements.TextShadow { offset = Vector2.one, color = Color.black, blurRadius = 1 };
			}
			DebugInfoLabel.text = info;
		}
#endif
		#endregion
		#region 内置类型
		public class QPlayerInfo : IKey<ulong>
		{
			public ulong Key { get; set; }
			public GameObject gameObject { internal set; get; }

			internal Action<string, object[]> Action = null;
		}
		public class QNetSyncFlag
		{
			[QName("帧索引")]
			internal int Index { get; private set; }
			[QName("同步检测标志")]
			internal int Value { get;private set; }
			public void Check(int value)
			{
				Value ^= value;
			}
			public void Reset(int index)
			{
				Index = index;
				Value = 0;
			}
			public override string ToString()
			{
				return "[" + Index + ":" + Value + "]";
			}
		}
		/// <summary>
		/// 网络帧数据
		/// </summary>
		public class QNetFrameData : IKey<ulong>
		{
			public ulong Key { get; set; }
			public QDictionary<string, object> Values = new QDictionary<string, object>();
			public List<QKeyValue<string, object[]>> Events = new List<QKeyValue<string, object[]>>();
			public bool Active => Values.Count + Events.Count > 0;
			public override string ToString()
			{
				return this.ToQData();
			}
			public void MergeValues(QNetFrameData other)
			{
				foreach (var kv in other.Values)
				{
					Values[kv.Key] = kv.Value;
				}
			}
			public void InvokeEvent(string key, params object[] value)
			{
				Events.Add(new QKeyValue<string, object[]>(key, value));
			}
			public void Clear()
			{
				Values.Clear();
				Events.Clear();
			}
		}
		private enum QNetActionKey
		{
			PlayerConnected,
			ServerSeed,
			SyncCheck,
			SyncLoad,
		}
		#endregion
	}


	/// <summary>
	/// 等待网络时间
	/// </summary>
	public class WaitForNetTime : CustomYieldInstruction
	{
		public WaitForNetTime(float time)
		{
			Wait = (int)(time / QNetManager.Instance.NetDeltaTime);
		}
		protected int Index { get; set; } = -1;
		protected int Wait { get; set; } = 1;
		public override bool keepWaiting
		{
			get
			{
				var curIndex = QNetManager.Instance == null ? (int)(Time.time / QNetManager.Instance.NetDeltaTime) : QNetManager.Instance.ClientIndex;
				if (Index < 0)
				{
					Index = curIndex;
				}
				if (Index + Wait > curIndex)
				{
					return true;
				}
				else
				{
					Index = -1;
					return false;
				}
			}
		}
	}
	
}

