using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    [System.Serializable]
    public class TestClass
    {
        public List<Vector3> list;
        public float p2;
    }
    public Vector3 v3 = new Vector3();
    public List<TestClass> list;
    public List<TestClass> blist;
   // public TestClass a = new TestClass { };
    public int[] b;
    public byte[] info;
    public void TimeCheck(string name, System.Action action)
    {
        var last = System.DateTime.Now;
        action?.Invoke();
        Debug.LogError("【"+name+"】运行时间:" + (System.DateTime.Now - last).TotalMilliseconds);
    }
    [ContextMenu("test")]
    public void TestFunc()
    {

        TimeCheck("自定义序列化",() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var info = QSerialize.Serialize(list);
                blist = QSerialize.Deserialize<List<TestClass>>(info);
            }
        });


    }
    [ContextMenu("test2")]
    public void Test2Func()
    {

        TimeCheck("Xml序列化", () =>
        {
            for (int i = 0; i < 100; i++)
            {
                var info = FileManager.Serialize(list);
                blist = FileManager.Deserialize<List<TestClass>>(info);
            }
        });


    }
}
