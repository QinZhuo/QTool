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
		[QName("QInspector测试",nameof(toggleTest))]
		[QEnum(nameof(enumTest))]
        public string testValue ;
		public static string[] enumTest=> new string[] { "enumTest1", "enumTest2" };
		
        byte[] testBytes;
		[QName("序列化测试数据来源", nameof(toggleTest))]
        public TTestClass test1;
		[QReadonly]
		[QName("序列化测试结果", nameof(toggleTest))]
		public TTestClass test2;
		[QName("序列化测试次数", nameof(toggleTest))]
		public int testTimes = 100;
		[QName("命令", nameof(toggleTest)),TextArea(2,4)]
		public string commandStr;
		[QName("QDataList数据", nameof(toggleTest))]
		[TextArea(5, 10)]
		public string QDataStr;
		[QName("解析类型测试")]
        public void CreateTest()
        {
            var run = Assembly.GetExecutingAssembly();
            Tool.RunTimeCheck("系统创建", () =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    run.CreateInstance(nameof(TTestClass));
                }
            });
            var ar = new object[0];
            Tool.RunTimeCheck("QInstance创建", () =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    Activator.CreateInstance(QReflection.ParseType(nameof(TTestClass)));
                }
            });
           Debug.LogError( (TTestClass)Activator.CreateInstance(QReflection.ParseType(nameof(TTestClass))));

        }
        [QName("切换语言")]
        public void ChangeLangua()
        {
            QTranslate.ChangeGlobalLanguage(QTranslate.GlobalLanguage == "schinese" ? "english" : "schinese");
			for (int i = 0; i < 10; i++)
			{
				var testData = QTranslate.GetQDataList("测试翻译"+i);
				testData["测试翻译" + i].SetValue(QTranslate.GlobalLanguage, "【翻译结果" + i + "】");
				testData.Save();
			}
			
		}
		[QName("运算符测试")]
		public void OperarterTest()
		{
			var a = UnityEngine.Random.Range(1,100);
			var b = UnityEngine.Random.Range(1, 100);
			QDebug.Log(a + " + " + b + " = " + a.OperaterAdd(b) + " " + (a + b));
			Vector2 v2A = new Vector2(UnityEngine.Random.Range(1, 100), UnityEngine.Random.Range(1, 100));
			Vector2 v2B = new Vector2(UnityEngine.Random.Range(1, 100), UnityEngine.Random.Range(1, 100));
			QDebug.Log(v2A + " + " + v2B + " = " + v2A.OperaterAdd(v2B) + " " + (v2A + v2B));
		}
		
        [QName("序列化测试")]
        public void TestFunc()
        {
			Debug.LogError(test1.ToQData().ToIdString());
            Tool.RunTimeCheck("QData写入", () =>
            {
                for (int i = 0; i < testTimes; i++)
                {
                    testBytes = test1.ToQData().GetBytes();
                }
            }, () => testBytes.Length, () => test1.ToQData());
			Tool.RunTimeCheck("QData读取", () =>
			{
				for (int i = 0; i < testTimes; i++)
				{
					test2 = testBytes.GetString().ParseQData<TTestClass>(null, true);
				}
			});
			Tool.RunTimeCheck("QData读取 有Target", () =>
            {
                for (int i = 0; i < testTimes; i++)
                {
					test2 = testBytes.GetString().ParseQData<TTestClass>(test2, true);
                }
            });
            Tool.RunTimeCheck("QData写入 无Name", () =>
            {
                for (int i = 0; i < testTimes; i++)
                {
                    testBytes = test1.ToQData(false).GetBytes();
                }
            }, () => testBytes.Length, () => test1.ToQData(false));

			Tool.RunTimeCheck("QData读取 无Name", () =>
			{
				for (int i = 0; i < testTimes; i++)
				{
					test2 = testBytes.GetString().ParseQData<TTestClass>(null,false);
				}
			});
			Tool.RunTimeCheck("QData读取 无Name 有Target", () =>
            {
                for (int i = 0; i < testTimes; i++)
                {
					test2 = testBytes.GetString().ParseQData<TTestClass>(test2,false);
                }
            });
				
		}
		
        [QName("命令测试")]
        public void CommandTest()
        { 
            QCommand.Invoke(commandStr);
        }
		[QName("QRuntimeValue测试")]
		public void QRuntimeValueTest()
		{
			QRuntimeData data = new QRuntimeData();
			data.Values["test"] = new QRuntimeValue(10);
			data.Values["test"].OffsetValue += 90;
			data.Values["test"].PercentValue = 1.5f;
			data.Values["test"].PercentValue -= 0.2f;
			Debug.LogError("130 : " + data.ToQData().ParseQData(data).Values["test"].ToQData()+"  :  "+data.Values["test"].Value);
		}
		

		[QName("时间测试")]
		public void TimeTest()
		{
			QTime.ChangeScale("测试时间", UnityEngine.Random.Range(0, 2));
		}
		[QName("加密文件测试")]
		public void SecretTest()
		{
			QFileManager.Save(nameof(QToolTest)+QFileManager.SecretExtension,nameof(SecretTest));
			Debug.LogError(QFileManager.Load(nameof(QToolTest) + QFileManager.SecretExtension));
		}
		public void ScreenTest(bool value)
		{
			QScreen.SetResolution(900, 600, value);
		}
		[QName("多值枚举序列化测试")]
		public void EnumTest()
		{
			var qdata = (TestEnum.攻击 | TestEnum.防御).ToQData();
			Debug.LogError(qdata + "  :   " + qdata.ParseQData<TestEnum>());
		}
		[QName("QDataList测试")]
        public  void QDataListTest()
		{
			Debug.LogError(QDataListTestType.Get("T1")?.ToQData());

			var data = new QDataList(QDataStr);
            Debug.LogError(data.ToString());
            data[2].SetValue("3", "2 3");
            data[3][4] = "3\n4";
            data["newLine"][4] = "n 4";
            data["newLine"].SetValue("5", true);
            data["setting"].SetValue( "off\nOn");
            Debug.LogError(data);
            Debug.LogError(data["setting"].GetValue<string>());
            Debug.LogError(test1.ToQData());
            var tobj = test1.ToQData().ParseQData<TTestClass>();
            Debug.LogError(tobj.ToQData());

            Debug.LogError(test1.ToQData(false));
            tobj = test1.ToQData(false).ParseQData<TTestClass>(null,false);
            Debug.LogError(tobj.ToQData(false));

            Debug.LogError((new int[][] {new int[] { 1, 2 },new int[] { 3, 4 } }).ToQData().ParseQData<int[][]>().ToQData());

			Debug.LogError(new List<TTestClass>() { new TTestClass { Key = "1" }, new TTestClass { Key = "2" } }.ToQDataList());
			QFileManager.Save("saveTest.txt" , data.ToQData());
			QPlayerPrefs.Set("test1", data.ToString());
			Debug.LogError(QPlayerPrefs.GetString("test1"));
		}
		[QName("ToComuteFloatTest")]
		public void ToComuteFloatTest()
		{
			QDebug.Log("1.1"+"  :  "+"1.1".ToComputeFloat());
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
						UnityEngine.Debug.LogWarning("More Subsystem:" + subSystem.subSystemList.Length);
					}
				}
			}

			UnityEngine.Debug.Log(sb.ToString());
		}
		[QName("QTaskTest")]
		public async void QTaskTest()
		{
			//UnityEngine.LowLevel.PlayerLoop.GetDefaultPlayerLoop();
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
		
	
	}
    [Flags]
    public enum TestEnum
    {
        无 = 0,
        攻击 = 1 << 1,
        防御 = 1 << 2,
        死亡 = 1 << 3,
    }
	[QDynamic]
    [System.Serializable]
    public class TTestClass:IKey<string>
    {
		public string Key { get; set; }
        public Rect rect;
        public TestEnum testEnume = TestEnum.攻击 | TestEnum.死亡;
		[QName("List测试")]
        public List<float> list;
        public List<List<float>> list2Test = new List<List<float>> { new List<float>() { 1, 2, 3 } };
        [QName("名字测试1")]
        public string asdl;
        public float p2;
        public byte[] array = new byte[] { 123 };
        [XmlIgnore] 
        public byte[,,] arrayTest = new byte[1, 2, 2] { { { 1, 2 }, { 3, 4 } } };
        public TestClass2 child; 
        [XmlIgnore]
        public object obj = new Vector3
        {
            x = 1,
            y = 2,
            z = 3
        };
       
    }
    [System.Serializable]
    public class TestClass2 :IQData
    {
        public List<float> list;
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
