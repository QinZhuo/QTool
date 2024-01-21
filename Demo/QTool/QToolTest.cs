using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
using System.Text;
using System;
using System.Reflection;
using QTool.Inspector;
using System.Runtime.Serialization.Formatters.Binary;
using QTool.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.ExceptionServices;

namespace QTool.Test
{


	public class QToolTest : MonoBehaviour
	{
		[QToggle("显示变量")]
		public bool toggleTest;
		[QName("QInspector测试", nameof(toggleTest))]
		[QPopup(nameof(enumTest))]
		public string testValue;
		public static string[] enumTest => new string[] { "enumTest1", "enumTest2" };
		[QName("", nameof(toggleTest))]
		public QIdObject qIdObject;
		byte[] testBytes;
		[QName("序列化测试数据来源", nameof(toggleTest))]
		public QTestClass test1;
		[QReadonly]
		[QName("序列化测试结果", nameof(toggleTest))]
		public QTestClass test2;
		[QName("序列化测试次数", nameof(toggleTest))]
		public int testTimes = 100;
		[QName("命令", nameof(toggleTest)), TextArea(2, 4)]
		public string commandStr;
		[QName("QDataList数据", nameof(toggleTest))]
		[TextArea(5, 10)]
		public string QDataStr;
		[QName("位置测试")]
		public Transform testObject;
		public Color color;
		[QName("预加载测试")]
		private void PreLoadTest()
		{
			_=QDataList.PreLoadAsync();
		}
		[QName("解析类型测试")]
		public void CreateTest()
		{
			var run = Assembly.GetExecutingAssembly();
			QDebug.DebugRun("系统创建", () =>
			{
				for (int i = 0; i < 10000; i++)
				{
					run.CreateInstance(nameof(QTestClass));
				}
			});
			var ar = new object[0];
			QDebug.DebugRun("QInstance创建", () =>
			{
				for (int i = 0; i < 10000; i++)
				{
					Activator.CreateInstance(QReflection.ParseType(nameof(QTestClass)));
				}
			});
			Debug.LogError((QTestClass)Activator.CreateInstance(QReflection.ParseType(nameof(QTestClass))));

		}
		[QName("切换语言")]
		public void ChangeLangua()
		{
			QLocalizationData.Language = QLocalizationData.Language == Application.systemLanguage ? SystemLanguage.English : Application.systemLanguage;
			Debug.LogError(QLocalizationData.Language);
			for (int i = 0; i < 10; i++)
			{
				var testData = QLocalizationData.LoadQDataList(QLocalizationData.Language.ToString());
				testData["测试翻译" + i].SetValue(nameof(QLocalizationData.Localization), "【翻译结果" + i + "】");
				testData.Save();
			}
		}
		[QName("运算符测试")]
		public void OperarterTest()
		{
			var a = UnityEngine.Random.Range(1, 100);
			var b = UnityEngine.Random.Range(1, 100);
			QDebug.Log(a + " + " + b + " = " + a.OperaterAdd(b) + " " + (a + b));
			Vector2 v2A = new Vector2(UnityEngine.Random.Range(1, 100), UnityEngine.Random.Range(1, 100));
			Vector2 v2B = new Vector2(UnityEngine.Random.Range(1, 100), UnityEngine.Random.Range(1, 100));
			QDebug.Log(v2A + " + " + v2B + " = " + v2A.OperaterAdd(v2B) + " " + (v2A + v2B));
		}

		[QName("序列化测试")]
		public void TestFunc()
		{
			Debug.LogError(test1.ToQData().ToKeyString());
			Debug.LogError(test1.ToQData());
			Debug.LogError(test1.QDataCopy().ToQData());
			Debug.LogError(test1.ToQData(false));
			Debug.LogError(test1.ToQData(false).ParseQData<QTestClass>(null, false).ToQData());
			QDebug.DebugRun("QData写入", () =>
			{
				for (int i = 0; i < testTimes; i++)
				{
					testBytes = test1.ToQData().GetBytes();
				}
			});
			QDebug.DebugRun("QData读取", () =>
			{
				for (int i = 0; i < testTimes; i++)
				{
					test2 = testBytes.GetString().ParseQData<QTestClass>(null, true);
				}
			});
			QDebug.DebugRun("QData读取 有Target", () =>
			{
				for (int i = 0; i < testTimes; i++)
				{
					test2 = testBytes.GetString().ParseQData<QTestClass>(test2, true);
				}
			});
			QDebug.DebugRun("QData写入 无Name", () => 
			{
				for (int i = 0; i < testTimes; i++)
				{
					testBytes = test1.ToQData(false).GetBytes();
				}
			});

			QDebug.DebugRun("QData读取 无Name", () =>
			{
				for (int i = 0; i < testTimes; i++)
				{
					test2 = testBytes.GetString().ParseQData<QTestClass>(null, false);
				}
			});
			QDebug.DebugRun("QData读取 无Name 有Target", () =>
			{
				for (int i = 0; i < testTimes; i++)
				{
					test2 = testBytes.GetString().ParseQData<QTestClass>(test2, false);
				}
			});

		}
		[QName("Action速度测试")]
		public void ActionTest()
		{
			List<Action> actions = new List<Action>();
			Action testAction = null;
			for (int i = 0; i < 100000; i++)
			{
				var info= " test " + i;
				actions.Add(new Action(() => { var c = info + info; }));
				testAction += actions[i];
			}
			QDebug.DebugRun("action ", testAction);
			QDebug.DebugRun("foreach ", ()=> {
				foreach (var action in actions)
				{
					action();
				}
			});
		}

		[QName("命令测试")]
		public void CommandTest()
		{
			QCommand.Invoke(commandStr);
		}


		[QName("时间测试")]
		public void TimeTest()
		{
			QTime.ChangeScale("测试时间", UnityEngine.Random.Range(0, 2));
		}
		[QName("加密文件测试")]
		public void SecretTest()
		{
			QFileTool.Save(nameof(QToolTest) + QFileTool.SecretExtension, nameof(SecretTest));
			Debug.LogError(QFileTool.Load(nameof(QToolTest) + QFileTool.SecretExtension));
		}
		public void ScreenTest(bool value)
		{
			QScreen.SetResolution(900, 600, value);
		}
		public Texture2D CaptureTexture2d;
		[QName]
		public void QRandomTest()
		{
			for (int i = 0; i < 10; i++)
			{
				Debug.LogError(QRandom.Instance.Normal());
			}
		}
		[QName("截图测试")]
		public void RenderTest()
		{
			CaptureTexture2d = QScreen.Capture();
			//if (CaptureTexture2d == null || CaptureTexture2d.width != Screen.width || CaptureTexture2d.height != Screen.height)
			//{
			//	CaptureTexture2d = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
			//}
			//Camera.main.Render();
			//CaptureTexture2d.ReadPixels(new Rect(0, 0,100, 100), 0, 0);
			//CaptureTexture2d.Apply();
		}
		[QName("多值枚举序列化测试")]
		public void EnumTest()
		{
			var qdata = (TestEnum.攻击 | TestEnum.防御).ToQData(); 
			Debug.LogError(qdata + "  :   " + qdata.ParseQData<TestEnum>());
		}
		[QName("QDataList测试")]
		public void QDataListTest()
		{
			Debug.LogError(QDataListTestType.Get("T1")?.ToQData());

			var data = new QDataList(QDataStr);
			Debug.LogError(data.ToString());
			data[2].SetValue("3", "2 3");
			data[3][4] = "3\n4";
			data["newLine"][4] = "n 4";
			data["newLine"].SetValue("5", true);
			data["setting"].SetValue("off\nOn");
			Debug.LogError(data);
			Debug.LogError(data["setting"].GetValue<string>());
			Debug.LogError(test1.ToQData());
			var tobj = test1.ToQData().ParseQData<QTestClass>();
			Debug.LogError(tobj.ToQData());

			Debug.LogError(test1.ToQData(false));
			tobj = test1.ToQData(false).ParseQData<QTestClass>(null, false);
			Debug.LogError(tobj.ToQData(false));

			Debug.LogError((new int[][] { new int[] { 1, 2 }, new int[] { 3, 4 } }).ToQData().ParseQData<int[][]>().ToQData());

			Debug.LogError(new List<QTestClass>() { new QTestClass { Key = "1" }, new QTestClass { Key = "2" } }.ToQDataList());
			QFileTool.Save("saveTest.txt", data.ToQData());
			QPlayerPrefs.Set("test1", data.ToString());
			Debug.LogError(QPlayerPrefs.GetString("test1"));
		}
		[QName("ToComuteFloatTest")]
		public void ToComuteFloatTest()
		{
			QDebug.Log("1.1" + "  :  " + "1.1".ToComputeFloat());
			QDebug.Log("1.2" + "  :  " + "1.2".ToComputeFloat());
			QDebug.Log("1.25" + "  :  " + "1.25".ToComputeFloat());
			QDebug.Log("" + "  :  " + "".ToComputeFloat());
			QDebug.Log("0.4.18" + "  :  " + "0.4.18".ToComputeFloat());
			QDebug.Log("0.4.20" + "  :  " + "0.4.20".ToComputeFloat());
		}
		[QName("PlayerLoop")]
		public static void PlayerLoop()
		{
			var playerLoop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();

			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"PlayerLoop List");
			foreach (var header in playerLoop.subSystemList)
			{
				sb.AppendFormat("------{0}------", header.type.Name);
				sb.AppendLine();
				foreach (var subSystem in header.subSystemList)
				{
					sb.AppendFormat("{0}", subSystem.type.Name);
					sb.AppendLine();

					if (subSystem.subSystemList != null)
					{
						QDebug.LogWarning("More Subsystem:" + subSystem.subSystemList.Length);
					}
				}
			}

			QDebug.Log(sb.ToString());
		}
		[QName("QTaskTest")]
		public async void QTaskTest()
		{
			//await Cysharp.Threading.Tasks.UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update);
			Debug.LogError(await Resources.LoadAsync<Texture2D>("NodeEditorBackground"));
			Debug.LogError("开始10秒完成");
			if (await QTask.Wait(10).IsCancel())
			{
				Debug.LogError("取消运行");
			}
			else
			{

				Debug.LogError("等待10秒完成");
			}

		}
		[QName("位置测试")]
		public void RoundTest()
		{
			testObject.GridFixed(Vector3.one);
		}
		[QName]
		public void FuncTest()
		{
			Func<string> test=()=>"123";
			test += () => "345";
			foreach (Func<string> item in test.GetInvocationList())
			{
				Debug.LogError(item());
			}
		}

		IEnumerator RunTest()
		{
			for (int i = 0; i < 1000; i++)
			{
				yield return Child();
			}
		//	while (true)
			{
				Debug.LogError("111");
				//yield return Child();
			//	yield return Child();
			//	yield return Child();
				//	yield return null;
			}
		}
		IEnumerator Child()
		{
			//Debug.Log("child ");
			yield return null;
		}
		List<IEnumerator> Ielist = new List<IEnumerator>();
		IEnumerator StopE()
		{
			var time = 1;
			while (time <= 50)
			{
				yield return null;
				Debug.LogError("stop" + time++);
			}
			foreach (var item in Ielist)
			{
				item.Stop();
			}
			Debug.LogError("stop");
		}
		[QName("协程运行测试")]
		public void Run()
		{
			RunTest().Start(Ielist);
			RunTest().Start(Ielist);
			Ielist.WaitCountZero().OnCallBack(() => Debug.LogError("AllOver")).Start();
			
		}
		[QName("协程停止测试")]
		public void Stop()
		{
			StopE().Start();
		}

	}
    [Flags]
    public enum TestEnum
    {
        无 = 0,
        攻击 = 1 << 1,
        防御 = 1 << 2,
        死亡 = 1 << 3,
    }
    [Serializable]
    public class QTestClass:IKey<string>
    {
		public string Key { get; set; }
        public Rect rect;
        public TestEnum testEnume = TestEnum.攻击 | TestEnum.死亡;
		[QName("List测试")]
        public List<QTestClass2> list;
        public List<List<float>> list2Test = new List<List<float>> { new List<float>() { 1, 2, 3 } };
        [QName("名字测试1"),TextArea(2,4)]
        public string asdl;
        public float p2;
        public byte[] array = new byte[] { 123 };
        [XmlIgnore] 
        public byte[,,] arrayTest = new byte[1, 2, 2] { { { 1, 2 }, { 3, 4 } } };
		//[QName]
		//public QTestClass child = null;
        [XmlIgnore]
        public object obj = new Vector3
        {
            x = 1,
            y = 2,
            z = 3
        };
		public QTestClass()
		{
		}

	}
    [System.Serializable]
    public class QTestClass2 :IQData
    {
        public List<Vector2> list;
        [QName("名字测试2")]
        public string asdl;

		public void ParseQData(StringReader reader)
		{
			reader.ReadQData(list);
			reader.ReadQData(asdl);
		}

		public void ToQData(StringWriter writer)
		{
			writer.WriteQData(list);
			writer.WriteQData(asdl);
		}
	}

}
