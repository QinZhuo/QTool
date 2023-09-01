using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using QTool.Reflection;
using UnityEngine.SceneManagement;
namespace QTool
{
	public static class QAnalysis
	{
		public enum QAnalysisEventName
		{
			游戏_开始,
			游戏_结束,
			游戏_暂离,
			游戏_错误,
			游戏_场景,
			系统地区,
		}
		public static string PlayerId { private set; get; }
		public static bool InitOver
		{
			get
			{
				if (string.IsNullOrWhiteSpace(PlayerId))
				{
					Debug.LogError(StartKey + "未设置账户ID");
					return false;
				}
				return true;
			}
		}
		public static int MinSendCount { get; set; } = 100;
		public static int AutoSendCount { get; set; } =1000;

		[System.Diagnostics.Conditional("UNITY_STANDALONE")]
		public static void Start(string playerId)
		{
			try
			{
				if (QPlayerPrefs.HasKey(EventListKey))
				{
					QPlayerPrefs.GetString(EventListKey).ParseQData(EventList);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("读取记录信息出错：\n" + e);
			}
			sendTask =SendAndClear();
			if (Application.isEditor&& !playerId.StartsWith("Editor_"))
			{
				playerId = "Editor_" + playerId;
			}
			else if(Debug.isDebugBuild&& !playerId.StartsWith("Debug_"))
			{
				playerId = "Debug_" + playerId;
			}
			if (playerId == PlayerId)
			{
				Debug.LogError(StartKey+" 已登录" + playerId);
				return;
			}
			PlayerId = playerId;
			if (!InitOver)
			{
				return;
			}
			InvokeEvent(nameof(QAnalysisEventName.游戏_开始),new StartInfo().Start());
			InvokeEvent(nameof(QAnalysisEventName.系统地区), QTool.RealyCulture.EnglishName);
			errorInfoList.Clear();
			Application.focusChanged += OnFocus;
			Application.logMessageReceived += LogCallback;
			Application.wantsToQuit += OnWantsQuit;
			SceneManager.sceneLoaded += OnSceneLoaded;
		}
		static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			InvokeEvent(nameof(QAnalysisEventName.游戏_场景),scene.name);
		}
		static List<string> errorInfoList = new List<string>();
		static void LogCallback(string condition, string stackTrace, LogType type)
		{
			switch (type)
			{
				case LogType.Warning:
				case LogType.Log:
				case LogType.Error:
					break;
				default:
					if (!errorInfoList.Contains(condition))
					{
						errorInfoList.Add(condition);
						InvokeEvent(nameof(QAnalysisEventName.游戏_错误), Application.platform+" "+Application.platform+"\n"+condition + '\n' + stackTrace.Replace("(at <00000000000000000000000000000000>:0)","").ToShortString(10000));
					}
					break;
			}
		}
		static void OnFocus(bool focus)
		{
			if (!focus)
			{
				InvokeEvent(nameof(QAnalysisEventName.游戏_暂离));
			}
		}
		static bool OnWantsQuit()
		{
			if (stopTask==null)
			{
				stopTask = Stop();
			}
			stopTask.GetAwaiter().OnCompleted(() =>
			{
				Application.Quit();
			});
			return false;
		}
		static Task stopTask = null;
		static Task sendTask = null;

		public static async Task Stop()
		{
			if (sendTask != null)
			{
				await sendTask;
			}
			if (!InitOver)
			{
				return;
			}
			InvokeEvent(nameof(QAnalysisEventName.游戏_结束));
			Application.focusChanged -= OnFocus;
			Application.logMessageReceived -= LogCallback;
			await SendAndClear();
			Application.wantsToQuit -= OnWantsQuit;
			SceneManager.sceneLoaded -= OnSceneLoaded;
			PlayerId = null;
			stopTask = null;
		}
		static string _startKey = null;
		public static string StartKey => _startKey??= nameof(QAnalysis) + "_" + Application.productName;
		public static string EventListKey => StartKey + "_" + nameof(EventList);
	
		static async Task SendAndClear()
		{
			if (sendTask != null)
			{
				await sendTask;
			}
			if (!QToolSetting.Instance.QAnalysisMail.InitOver)
			{
				Debug.LogError(nameof(QToolSetting.Instance.QAnalysisMail) + " 未设置");
				return;
			}
			if (QPlayerPrefs.HasKey(EventListKey))
			{
				var count = EventList.Count;
				if (count > MinSendCount)
				{
					List<QAnalysisEvent> tempList = new List<QAnalysisEvent>();
					var data = "";
					var id = EventList.QueuePeek().eventId;
					lock (EventList)
					{
						tempList.AddRange(EventList);
						data = QPlayerPrefs.GetString(EventListKey);
						EventList.Clear();
						QPlayerPrefs.DeleteKey(EventListKey);
					}
					if (!await QMailTool.SendAsync(QToolSetting.Instance.QAnalysisMail, QToolSetting.Instance.QAnalysisMail.account, StartKey + "_" + SystemInfo.deviceName +"_"+ id, data))
					{
						lock (EventList)
						{
							EventList.AddRange(tempList);
							QPlayerPrefs.SetString(EventListKey, EventList.ToQData());
							Debug.LogWarning("还原信息：\n" + EventList.ToQData());
						}	
					}
				}
			
			}
			sendTask = null;
		}

		static List<QAnalysisEvent> EventList = new List<QAnalysisEvent>();

		[System.Diagnostics.Conditional("UNITY_STANDALONE")]
		public static void InvokeEvent(string eventKey,object value=null)
		{

#if UNITY_EDITOR
			if ((Application.isEditor&& !QPlayerPrefs.HasKey(nameof(QAnalysis) + "_EditorTest")))
			{
				return;
			}
#endif
			if (InitOver)
			{
				try
				{
					if (eventKey.Contains("_"))
					{
						eventKey = eventKey.Replace("_", "/");
					}
					var eventData = new QAnalysisEvent(PlayerId,eventKey, value);
					EventList.Add(eventData);
					QPlayerPrefs.SetString(EventListKey, EventList.ToQData());
					QDebug.Log(StartKey + " 触发事件 " + eventData);
					if (AutoSendCount >= 1 && EventList.Count >= AutoSendCount)
					{
						sendTask = SendAndClear();
					}

				}
				catch (Exception e)
				{
					Debug.LogError(nameof(QAnalysis) + "触发事件 " + eventKey + " " + value + " 出错：\n" + e);
				}

			}
		}
		[System.Diagnostics.Conditional("UNITY_STANDALONE"),]
		public static void InvokeEvent(string eventKey,string key, object value)
		{
			InvokeEvent(eventKey, new QKeyValue<string, object>(key, value));
		}
	}
	public class StartInfo
	{
		public RuntimePlatform platform { get; set;	 } 
		public string version { get; set; }
		public string deviceName { get; set; }
		public string deviceUniqueIdentifier { get; set; }
		public string os { get; set; }
		public string deviceModel { get; set; } 
		public string cpu { get; set; }
		public int cpuCount { get; set; } 
		public int cpuFrequency { get; set; } 
		public int systemMemorySize { get; set; } 
		public string gpu { get; set; }
		public int gpuMemorySize { get; set; } 
		public StartInfo Start()
		{
			platform= Application.platform;
			version = Application.version;
			deviceName = SystemInfo.deviceName;
			deviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
			os = SystemInfo.operatingSystem;
			deviceModel = SystemInfo.deviceModel;
			cpu = SystemInfo.processorType;
			cpuCount = SystemInfo.processorCount;
			cpuFrequency = SystemInfo.processorFrequency;
			systemMemorySize = SystemInfo.systemMemorySize;
			gpu = SystemInfo.graphicsDeviceName;
			gpuMemorySize = SystemInfo.graphicsMemorySize;
			return this;
		}
	}

	public struct QAnalysisEvent :IKey<string>
	{
		public string eventId;
		public string playerId;
		public string eventKey;
		public object eventValue;
		public DateTime eventTime;
		[QIgnore]
		public string Key { get => eventId; set => eventId = value; }
	
		public QAnalysisEvent(string playerId, string eventKey,object eventValue=null)
		{
			eventId = QId.NewId();
			this.playerId = playerId;
			this.eventKey = eventKey;
			this.eventValue = eventValue;
			eventTime = DateTime.Now;
		}
		public override string ToString()
		{
			return this.ToQData(false);
		}
		public object GetValue(string dataKey)
		{
			if (eventValue == null)
			{
				return null;
			}
			if (eventKey == dataKey)
			{
				return eventValue;
			}
			else 
			{
				var childKey = dataKey.SplitEndString(eventKey+"/").Replace("/",".");
				return eventValue.GetValue(childKey);
			}
		}

		public static int SortMethod(QAnalysisEvent a, QAnalysisEvent b)
		{
			return DateTime.Compare(a.eventTime, b.eventTime);
		}
	}

}
