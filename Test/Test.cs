using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System;

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
        public List<int> list;
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
    [ContextMenu("WriteTest")]
    public void WriteTest()
    {
        var str = @"dalsjdiqowjdoqiwdj
qweojqwiejfo
qefoijfur
qfoiwefhoqwiehfoqwe
qeofijqweijf";
        TimeCheck("writer写入", () =>
        {
            for (int i = 0; i < 10000; i++)
            {
                using (MemoryStream stream=new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(str);
                        var bytes = stream.ToArray();
                    }
                }
            }
        });


    }
    [ContextMenu("WriteTest2")]
    public void WriteTest2()
    {
        var str = @"dalsjdiqowjdoqiwdj
qweojqwiejfo
qefoijfur
qfoiwefhoqwiehfoqwe
qeofijqweijf";
        List<byte> list = new List<byte>();
        TimeCheck("自定义写入", () =>
        {
            for (int i = 0; i < 10000; i++)
            {
                list.AddRange(Encoding.UTF8.GetBytes(str));
                var bytes = list.ToArray();
                list.Clear();
            }
        });


    }
    [ContextMenu("ReadTest")]
    public void ReadTest()
    {
        var str = @"dalsjdiqowjdoqiwdj
qweojqwiejfo
qefoijfur
qfoiwefhoqwiehfoqwe
qeofijqweijf";
        byte[] bytes = null;
        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(str);
                bytes = stream.ToArray();
            }
        }
        TimeCheck("reader读取", () =>
        {
            for (int i = 0; i < 10000; i++)
            {
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        var a= reader.ReadString();
                    }
                }
            }
        });


    }
    [ContextMenu("ReadTest2")]
    public void ReadTest2()
    {
        var str = @"dalsjdiqowjdoqiwdj
qweojqwiejfo
qefoijfur
qfoiwefhoqwiehfoqwe
qeofijqweijf";
        byte[] bytes = null;
        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(str);
                bytes = stream.ToArray();
            }
        }
        List<byte> list = new List<byte>();
        TimeCheck("自定义读取", () =>
        {
            for (int i = 0; i < 10000; i++)
            {
                list.Clear();
                list.AddRange(bytes);
                var a= Encoding.UTF8.GetString( list.ToArray());
            }
        });


    }
    [ContextMenu("test")]
    public void TestFunc()
    {

        TimeCheck("自定义序列化",() =>
        {
            for (int i = 0; i < 1000; i++)
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
            for (int i = 0; i < 1000; i++)
            {
                var info = FileManager.Serialize(list);
                blist = FileManager.Deserialize<List<TestClass>>(info);
            }
        });


    }
    [ContextMenu("test3")]
    public void Test3Func()
    {
        TimeCheck("Json序列化", () =>
        {
            for (int i = 0; i < 1000; i++)
            {
                var info =Encoding.UTF8.GetBytes( JsonConvert.SerializeObject(list));
                blist = JsonConvert.DeserializeObject<List<TestClass>>(Encoding.UTF8.GetString( info));
            }
        });
    }
}
