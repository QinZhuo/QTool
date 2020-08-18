using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System;
using QTool.ByteExtends;
[System.Serializable]
public struct V2
{
    public float x;
    public float y;
    public static bool operator ==(V2 a, Vector2 b)
    {
        return a.x == b.x && a.y == b.y;
    }
    public static bool operator !=(V2 a, Vector2 b)
    {
        return a.x != b.x || a.y != b.y;
    }
    public V2(Vector2 vector2)
    {
        x = vector2.x;
        y = vector2.y;
    }
    [JsonIgnore]
    public Vector2 Vector2
    {
        get
        {
            return new Vector2(x, y);
        }
    }
}
[System.Serializable]
public class NetInput
{
    public bool NetStay = false;
    public V2 NetVector2;
}
[System.Serializable]
public class IValueBase:IKey<string>
{
    public string Key { get; set; }

    public NetInput netInput = new NetInput();
    public void Merger(NetInput other)
    {
        netInput.NetStay = netInput.NetStay || other.NetStay;
        if (other.NetVector2 != Vector2.one)
        {
            netInput.NetVector2 = other.NetVector2;
        }
    }
}
public class IntValue : IValueBase
{
    public int value;
}
[System.Serializable]
public class TestClass
{
    public List<float> list;
    public string asdl;
    public float p2;
}
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

    public Vector3 v3 = new Vector3();
    public KeyList<string,IValueBase> list=new KeyList<string,IValueBase>();
    public KeyList<string,IValueBase> blist=new KeyList<string,IValueBase>();
    // public TestClass a = new TestClass { };
   // public Dictionary<string, float> aDic = new Dictionary<string, float>();
   // public Dictionary<string, float> bDic = new Dictionary<string, float>();
    public int[] b;
    public byte[] info;
    public void TimeCheck(string name, System.Action action)
    {
        var last = System.DateTime.Now;
        action?.Invoke();
        Debug.LogError("【"+name+"】运行时间:" + (System.DateTime.Now - last).TotalMilliseconds+" 长度"+info.LongLength.ComputeScale());
    }
    [ContextMenu("Name")]
    public void FullName()
    {
        Debug.LogError(typeof(TestClass).Name);
    }
    [ContextMenu("test")]
    public void TestFunc()
    {
        // list = new List<IValueBase>();
        //  list.Add(new IntValue { value = 4654 });
        list.Add(new IValueBase { Key = "a" });
        list = new KeyList<string, IValueBase>();
        list["a"].netInput.NetVector2.x = 5;
        TimeCheck("自定义序列化",() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                info = QSerialize.Serialize(list);
                blist = QSerialize.Deserialize <KeyList<string, IValueBase>>(info);
            }
        });
     //   Debug.LogError(blist["a"].netInput.NetVector2);
      
    }
    [ContextMenu("test2")]
    public void Test2Func()
    {
     //   list = new List<IValueBase>();
      //  list.Add(new IntValue { value = 431 });
        TimeCheck("Xml序列化", () =>
        {
            for (int i = 0; i < 1000; i++)
            {
                info = Encoding.UTF8.GetBytes(FileManager.Serialize(list,typeof(IntValue)));
                blist = FileManager.Deserialize<KeyList<string,IValueBase>>(Encoding.UTF8.GetString(info));
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
                info =Encoding.UTF8.GetBytes( JsonConvert.SerializeObject(list));
                blist = JsonConvert.DeserializeObject<KeyList<string,IValueBase>>(Encoding.UTF8.GetString( info));
            }
        });
    }
}
