using QTool.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Timers;
using UnityEngine;
namespace QTool.Net
{

	public sealed class QNetManager : InstanceBehaviour<QNetManager>
	{
		[QEnum,QName("传输方式")]
		public QNetTransport transport;
		[QName("网络帧率"),SerializeField,Tooltip("每秒进行多少次网络帧同时更改物理帧率 两者保持同步")]
		[Range(15,60)]
		private int netFps = 30;
		public System.Random Random { get; private set; } = null;
		protected override void Awake()
		{
			base.Awake();
			Application.runInBackground = true;
			Physics.autoSimulation = false;
			Physics.autoSyncTransforms = false;
			Time.fixedDeltaTime = 1f / netFps;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			QToolManager.Instance.OnGUIEvent += GUI;
#endif
		}
		private void OnDestroy()
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (QToolManager.Instance != null)
			{
				QToolManager.Instance.OnGUIEvent -= GUI;
			}
#endif
			QTime.RevertScale(this);
			ServerUpdateTimer?.Clear();
		}
		public bool NetActive => Application.isPlaying&&( transport.ServerActive || transport.ClientConnected);
		public T PlayerValue<T>(string player,string key, T localValue)
		{
			if (transport.ClientConnected)
			{
				if (player==transport.ClientPlayerId&&(!LocalAction.Values.ContainsKey(key)|| !LocalAction.Values[key].Equals( localValue)))
				{
					SendAction.Values[key] = localValue;
					LocalAction.Values[key] = localValue;
				}
				if (ClientActionData.ContainsKey(player)&& ClientActionData[player].Values.ContainsKey(key)&& ClientActionData[player].Values[key] is T value)
				{
					return value;
				}
			}
			return default;
		}
		public void PlayerAction<T>(string player, string key, T value,Action<T> action=null)
		{
			if (transport.ClientConnected)
			{
				if (action != null)
				{
					QEventManager.RegisterOnce(player + "_" + key, (object obj)=>action?.Invoke((T)obj));
				}
				if (player == transport.ClientPlayerId)
				{
					SendAction.TriggerEvent(key, value);
				}
			}
		}
		
		QDictionary<string, Action<object>> NetEvent = new QDictionary<string, Action<object>>();
		public GameObject playerPrefab;
		internal QDictionary<string, GameObject> PlayerObjects = new QDictionary<string, GameObject>();
		QNetActionData LocalAction = new QNetActionData();
		QNetActionData SendAction = new QNetActionData();
		private int ServerSeed = 0;
		public float NetDeltaTime => Time.fixedDeltaTime;
		public float NetTime { get; private set; }
		#region 服务器数据

		const int GameDataArray=-101;
		const int GameDataArrayLength = 1000;
		QTimer ServerUpdateTimer;
		[QName("启动服务器", "!" + nameof(NetActive))]
		public void StartServer()
		{
			transport.ServerStart();
			ServerSeed = UnityEngine.Random.Range(0,int.MaxValue);
			ServerActionData[nameof(QNetManager)].Events.Add(new QKeyValue<string, object>(nameof(ServerSeed), ServerSeed));
			transport.OnServerConnected = (id) =>
			{
				ServerConnects.Add(id);
				var max = Mathf.CeilToInt(ServerIndex * 1f / GameDataArrayLength) * GameDataArrayLength;
				for (int startIndex = 0; startIndex < max; startIndex+=GameDataArrayLength)
				{
					var end = Mathf.Min(startIndex+GameDataArrayLength, ServerIndex);
					using (var writer = new QBinaryWriter())
					{
						writer.Write(GameDataArray);
						var count = end - startIndex;
						writer.Write(count);
						for (int i = startIndex; i < end; i++)
						{
							writer.WriteObject(i);
							writer.WriteObject(ServerGameData[i]);
						}
						QDebug.Log("服务端发送游戏数据 " + startIndex + "=>" + end + " [" + count + "]"+ writer.BaseStream.Length.ToSizeString());
						transport.ServerSend(id, writer.ToArray());
					}
				}
			}; 
			transport.OnServerDataReceived = (id, data) =>
			{
				var netAction= data.Deserialize<QNetActionData>();
				foreach (var eventData in netAction.Events)
				{
					switch (eventData.Key)
					{
						case nameof(DefaultNetAction.PlayerConnected):
							{
								var player = eventData.Value?.ToString();
								ServerPlayers[id] = player;
								QDebug.Log("["+ServerIndex + "] 添加玩家[" + id + "][" + player+"]");
							}
							break;
						default:
							break;
					}
				}
				ServerActionData[ServerPlayers[id]].MergeValues(netAction);
				ServerActionData[ServerPlayers[id]].Events.AddRange(netAction.Events);
				
			};
			transport.OnServerError = (id, e) =>
			{
				Debug.LogError(id + " " + e);
			};
			transport.OnServerDisconnected = (id) =>
			{
				ServerConnects.Remove(id);
			};
			ServerUpdateTimer = new QTimer(Time.fixedDeltaTime);
			
		}
		
		[QName("启动主机", "!" + nameof(NetActive))]
		public void StartHost()
		{
			StartServer();
			StartClient();
		}
		public QDictionary<int, string> ServerPlayers = new QDictionary<int, string>();
		public QDictionary<int, QList<string, QNetActionData>> ServerGameData = new QDictionary<int, QList<string, QNetActionData>>((key)=>new QList<string, QNetActionData>(()=>new QNetActionData()));
		public int ServerIndex { get; private set; } = 0;
		public QList<string, QNetActionData> ServerActionData => ServerGameData[ServerIndex];

		List<int> ServerConnects = new List<int>();
		private void ServerUpdate()
		{
			if (transport.ServerActive)
			{
				using (var writer = new QBinaryWriter())
				{
					writer.WriteObject(ServerIndex);
					writer.WriteObject(ServerActionData);
					var data = writer.ToArray();
					foreach (var player in ServerConnects)
					{
						transport.ServerSend(player, data);
					}
					ServerIndex++;
				}
			}
		}

		#endregion

		#region 客户端数据
		public QDictionary<int, QList<string, QNetActionData>> ClientGameData = new QDictionary<int, QList<string, QNetActionData>>();
		public int ClientIndex { get; private set; } =0;
		internal int IdIndex { get; set; } = 0;

		private QNetSyncFlag SyncCheckFlag = new QNetSyncFlag();
		internal static Dictionary<string, List<IQNetSyncCheck>> QNetSyncCheckList { get; private set; } = new Dictionary<string, List<IQNetSyncCheck>>();
		public QList<string, QNetActionData> ClientActionData = new QList<string, QNetActionData>(()=>new QNetActionData());
		[QName("启动客户端", "!" + nameof(NetActive))]
		public void StartClient(string ip="127.0.0.1")
		{
			transport.ClientConnect(ip);
			transport.OnClientConnected += () =>
			{
				PlayerAction(transport.ClientPlayerId, nameof(DefaultNetAction.PlayerConnected), transport.ClientPlayerId);
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
		}
		private void ReceiveGameData(QBinaryReader reader)
		{
			var index = reader.ReadInt32();
			if (index == GameDataArray)
			{
				var count = reader.ReadInt32();
				QDebug.Log("客户端接收游戏数据[" + count + "]"+reader.BaseStream.Length.ToSizeString());
				for (int i = 0; i < count; i++)
				{
					ReceiveGameData(reader);
				}
			}
			else
			{
				var GameData = reader.ReadObject<QList<string, QNetActionData>>(); ;
				ClientGameData[index] = GameData;
			}
		
		}

		private void ClientFixedUpdate()
		{
			if (transport.ClientConnected)
			{
				if (SendAction.Active)
				{
					transport.ClientSend(SendAction.Serialize());
				}
				SendAction.Clear();
				ClientNetUpdate();
			}
		}
		private void ClientNetUpdate()
		{
			if (ClientGameData.ContainsKey(ClientIndex))
			{
				foreach (var actionData in ClientGameData[ClientIndex])
				{

					foreach (var eventData in actionData.Events)
					{
						switch (eventData.Key)
						{
							case nameof(DefaultNetAction.PlayerConnected):
								if (!PlayerObjects.ContainsKey(actionData.Key))
								{
									var obj = GameObject.Instantiate(playerPrefab); ;
									PlayerObjects[actionData.Key] = obj;
									foreach (var qNet in obj.GetComponents<QNetBehaviour>())
									{
										qNet.PlayerId = actionData.Key;
									}
								}
								break;
							case nameof(DefaultNetAction.SyncCheck):
								{
									var flag = (QNetSyncFlag)eventData.Value;
									
									if (flag.Index == SyncCheckFlag.Index)
									{
										if (flag.Value != SyncCheckFlag.Value)
										{
											Debug.LogWarning("[" + flag.Index + "]同步验证失败[" + flag + "]:[" + SyncCheckFlag + "]");
											if (transport.ServerActive)
											{
												using (var writer = new QBinaryWriter())
												{
													writer.Write(QNetSyncCheckList.Count);
													foreach (var QIdCheck in QNetSyncCheckList)
													{
														using (var QIdData=new QBinaryWriter())
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
																	Debug.LogError("保存[" + QIdCheck.Key + "]" + QIdCheck.Value[i] + "出错 "+e.ToShortString(1000));
																}
															}
															writer.Write(QIdData.ToArray());
														}
													}
													PlayerAction(transport.ClientPlayerId, nameof(DefaultNetAction.SyncLoad), writer.ToArray());
												}
											}
										}
									}
								}
								break;
							case nameof(DefaultNetAction.SyncLoad):
								{
									if (eventData.Value is byte[] loadData)
									{
										Debug.LogWarning("["+ClientIndex+"]尝试修复同步");
										using (var reader = new QBinaryReader(loadData))
										{
											var Count = reader.ReadInt32();
											for (int i = 0; i < Count; i++)
											{
												using (var QIdData = new QBinaryReader(reader.ReadBytes()))
												{
													var qidKey = QIdData.ReadString();
													if (QNetSyncCheckList.ContainsKey(qidKey))
													{
														var QIdCheck = QNetSyncCheckList[qidKey];
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
									else
									{
										Debug.LogWarning("同步数据为空");
									}
								}
								break;
							case nameof(ServerSeed):
								{
									Random = new System.Random((int)eventData.Value);
								}
								break;
							default:
								QEventManager.Trigger(actionData.Key + "_" + eventData.Key, eventData.Value);
								break;
						}
					}
					ClientActionData[actionData.Key].MergeValues(actionData);
				}
				OnNetUpdate?.Invoke();
				Physics.Simulate(Time.fixedDeltaTime);
				Physics.SyncTransforms();
				if (ClientIndex % (netFps/2) == 0)
				{
					SyncCheckFlag.Index = ClientIndex;
					SyncCheckFlag.Value = 0;
					OnSyncCheck?.Invoke(SyncCheckFlag);
					PlayerAction(transport.ClientPlayerId, nameof(DefaultNetAction.SyncCheck), SyncCheckFlag);
				}
				ClientIndex++;
				NetTime += NetDeltaTime;
				
			}
			if (ClientGameData.ContainsKey(ClientIndex + 1))
			{
				QTime.ChangeScale(this, 100);
			}
			else
			{
				QTime.RevertScale(this);
			}

		}
		internal event Action OnNetUpdate=null;
		internal event Action<QNetSyncFlag> OnSyncCheck = null;
		#endregion

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
			transport.ClientEarlyUpdate();
			transport.ClientLateUpdate();
			transport.ServerEarlyUpdate();
			transport.ServerLateUpdate();
		}
#if UNITY_EDITOR || DEVELOPMENT_BUILD

		[HideInInspector,SerializeField]
		private string ServerIp = "127.0.0.1";
		string LocalIp = "";
		private void GUI()
		{
			if (LocalIp.IsNullOrEmpty())
			{
				LocalIp = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();
			}
			GUILayout.Label(LocalIp);
			if (NetActive)
			{
				GUILayout.BeginVertical("Box");
				if (transport.ClientConnected)
				{
					GUILayout.Label("当前帧" + ClientIndex + "/" + ClientGameData.Count+" "+nameof(NetTime)+":"+new TimeSpan((long)( TimeSpan.TicksPerSecond* NetTime)).ToString(@"hh\:mm\:ss"));
				}
				GUILayout.EndVertical();
			}
			else
			{
				if (GUILayout.Button("开启主机", GUILayout.Width(150), GUILayout.Height(50)))
				{
					StartHost();
				}
				ServerIp= GUILayout.TextField(ServerIp);
				if (GUILayout.Button("开启客户端", GUILayout.Width(150), GUILayout.Height(50)))
				{
					StartClient(ServerIp);
				}
			}
			
		}
#endif

	}

	public class QNetActionData : IKey<string>
	{
		public string Key { get; set; }
		public QDictionary<string, object> Values = new QDictionary<string, object>();
		public List<QKeyValue<string, object>> Events = new List<QKeyValue<string, object>>();
		public bool Active => Values.Count + Events.Count > 0;
		public override string ToString()
		{
			return this.ToQData();
		}
		public void MergeValues(QNetActionData other)
		{
			foreach (var kv in other.Values)
			{
				Values[kv.Key] = kv.Value;
			}
		}
		public void TriggerEvent(string key, object value)
		{
			Events.Add(new QKeyValue<string, object>(key, value));
		}
		public void Clear()
		{
			Values.Clear();
			Events.Clear();
		}
	}
	public class QNetSyncFlag
	{
		[QName("帧索引")]
		internal int Index { get; set; }
		[QName("同步检测标志")]
		internal int Value { get; set; }
		public void Check(int value)
		{
			Value ^= value;
		}
		public void Check(float value)
		{
			Check((int)(value * 100));
		}
		public void Check(Vector3 value)
		{
			Check(value.x);
			Check(value.y);
			Check(value.z);
		}
		public override string ToString()
		{
			return "[" + Index + ":" + Value + "]";
		}
	}
	public enum DefaultNetAction
	{
		PlayerConnected,
		SyncCheck,
		SyncLoad,
	}
}

