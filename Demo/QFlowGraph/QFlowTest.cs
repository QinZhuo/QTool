using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
using QTool.FlowGraph;
using QTool.Reflection;
using System.Threading.Tasks;
using QTool.Test;
using QTool.Inspector;
using static QTool.Test.QToolTest;

public class QFlowTest : MonoBehaviour
{
	[System.Serializable]
	public class Test1111
	{
		public QFlowGraph testGraph;
		public List<QFlowGraph> qFlows;
	}
	public QFlowGraphAsset graphAsset;
	public QFlowGraph graph;
	public Test1111 test1111;
    void Start()
    {
        graphAsset?.Graph?.Run(nameof(QFlowNodeTest.Start));
    }
    [ContextMenu("运行【测试事件】")]
    public void RunEventTest()
    {
        graphAsset?.Graph?.Run("测试事件");
    }
    [ContextMenu("运行时创建QFlowGraph测试")]
    public void Test()
    {
        var graph = new QFlowGraph();
        var logNode= graph.AddNode(nameof(QFlowNodeTest.LogErrorTest));
        logNode["value"] = "QFlowGraph测试";
        var waitNode = graph.AddNode(nameof(QFlowNodeTest.CoroutineWaitTest));
        waitNode["time"]=3;
        logNode.SetNextNode(waitNode);
        waitNode.SetNextNode(logNode);
        graph.Run(logNode.Key);
     
    }
	// Update is called once per frame 
	[ContextMenu("停止【测试事件】")]
	public void Stop()
	{
		graphAsset?.Graph.Stop(); 
	}
    void Update()
    {
        
    }
}

[QCommandType("QFlowNode测试")]
public static class QFlowNodeTest
{
	public static IEnumerator StepUpdateTest()
	{
		while (true)
		{
			yield return QFlowGraph.Step;
		}
	}

	[QStartNode]
    public static void Start()
    {

    }
    [QStartNode]
    public static void EventStartTest([QNodeName]string eventKey="事件名")
    {

    }
    public static IEnumerator CoroutineWaitTest(float time)
    {
        yield return new WaitForSeconds(time);
    }
    public static async Task<string> TaskWaitReturnTest(int time=1,string strValue="wkejw")
    {
        await Task.Delay(time*1000);
        return strValue;
    }
    public static void LogErrorTest([QInputPort("自己")]object value)
    {
        Debug.LogError(value);
    }
    public enum T1
    {
        E1,
        E2,
    }
  
    public static void EnumTest(TestEnum testEnum, T1 testEnum2,  out string value, [QEnum(nameof(QDataListTestType) + ".List")] string defaultTest1 = "1239180")
    {
        value = testEnum2.ToString();
        Debug.LogError(value + "  " + defaultTest1);
    }
    public static void OutTest([QName("输入Bool")] bool inBool, [QName("输出Bool")] out bool outBool, int inInt, out int outInt, float inFloat, out float outFloat)
    {
        outBool = inBool;
        outInt = inInt;
        outFloat = inFloat;
    }
    public static void ObjectTest(object obj, QIdObject objRef,Object _object,GameObject gameObject,Sprite sprite,UnityEngine.UI.Image image, Vector3 vector3)
    {
		image.color = Color.black;
    }
    public static void ListTest([QFlowPort] List<int> list, List<Vector3> v3List, [QFlowPort]List<Vector3> v3FlowList,int[] intArray, [QFlowPort,QOutputPort] bool[] boolArray, List<object> objList)
    {

    }
    public static void BoolTest(QFlowNode This,bool boolValue,[QFlowPort,QOutputPort]bool True, [QFlowPort] out object False)
    {
        False = true;
        if (boolValue)
        {
            This.SetNetFlowPort(nameof(True));
        }
        else
        {
            This.SetNetFlowPort(nameof(False));
        }
    }
    public static void GetTime_AutoUseTest([QOutputPort(autoRunNode = true)]out float time)
    {
        time = Time.time;
    }
    public static float AddTest1(float a,float b)
    {
        return a + b;
    }
    public static void AddTest2(QFlowNode This, float a,float b,[QOutputPort(autoRunNode = true)] float result)
    {
        This[nameof(result)] = a + b;
    }
	public static void ListLogTest(List<string> list)
	{
		Debug.LogError(list.ToOneString());
	}
    [QName("异步测试")]
    public static void AsyncTest(QFlowNode This, [QOutputPort]QFlow One, [QOutputPort] QFlow Tow)
    {
        This.SetNetFlowPort(nameof(One));
        This.RunPort(nameof( Tow));
    }
    [QName("任务测试")]
    public static IEnumerator TaskTest(QFlowNode This,List<QFlow> task, QFlow failureEvent, [QOutputPort,QFlowPort] QFlow success, [QOutputPort, QFlowPort] string failure)
    {
        List<int> taskList = new List<int> { };
        for (int i = 0; i < task.Count; i++)
        {
            taskList.Add(i);
        }
        This.TriggerPortList.Clear();
        Debug.LogError("任务开始");
        while (taskList.Count>0)
        {
            foreach (var port in This.TriggerPortList)
            {
                if (port.port == nameof(task))
                {
                    Debug.LogError("完成 "+nameof(task)+port.index );
                    taskList.Remove(port.index);
                }
                else if(port.port== nameof(failureEvent))
                {
                    Debug.LogError("任务失败");
                    This.SetNetFlowPort(nameof(failure));
                    yield break;
                }
            }
            This.TriggerPortList.Clear();
            yield return null;
        }
        Debug.LogError("任务成功"); 
        This.SetNetFlowPort(nameof(success));
    }
}
