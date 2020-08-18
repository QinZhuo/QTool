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
[Dynamic]
public interface IValueBase:IKey<string>
{

    //public NetInput netInput = new NetInput();
    //public void Merger(NetInput other)
    //{
    //    netInput.NetStay = netInput.NetStay || other.NetStay;
    //    if (other.NetVector2 != Vector2.one)
    //    {
    //        netInput.NetVector2 = other.NetVector2;
    //    }
    //}
}
public class IntValue : IValueBase
{
    public string Key { get; set; }
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
    public List<IValueBase> al=new List<IValueBase>();
    public List<IValueBase> bl=new List<IValueBase>();
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

        al.Add(new IntValue { value = 153 });
        TimeCheck("自定义序列化",() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                info = QSerialize.Serialize(al);
                bl = QSerialize.Deserialize <List<IValueBase>>(info);
            }
        });
        Debug.LogError((bl[0] as IntValue).value);
      //  Debug.LogError(al["a"].netInput.NetVector2 .x+ ":" +bl[0].netInput.NetVector2.x);
      
    }
    //[ContextMenu("test2")]
    //public void Test2Func()
    //{
    //    al = new KeyList<string, IValueBase>();
    //    al.Add(new IValueBase { Key = "a" });
    //    al["a"].netInput.NetVector2 = new V2(Vector2.one * 123);
    //    //   list = new List<IValueBase>();
    //    //  list.Add(new IntValue { value = 431 });
    //    TimeCheck("Xml序列化", () =>
    //    {
    //        for (int i = 0; i < 1000; i++)
    //        {
    //            info = Encoding.UTF8.GetBytes(FileManager.Serialize(al,typeof(IntValue)));
    //            bl = FileManager.Deserialize<KeyList<string,IValueBase>>(Encoding.UTF8.GetString(info));
    //        }
    //    });

    //}
    //[ContextMenu("test3")]
    //public void Test3Func()
    //{
    //    al = new KeyList<string, IValueBase>();
    //    al.Add(new IValueBase { Key = "a" });
    //    al["a"].netInput.NetVector2 = new V2(Vector2.one * 123);
    //    TimeCheck("Json序列化", () =>
    //    {
    //        for (int i = 0; i < 1000; i++)
    //        {
    //            info =Encoding.UTF8.GetBytes( JsonConvert.SerializeObject(al));
    //            bl = JsonConvert.DeserializeObject<KeyList<string,IValueBase>>(Encoding.UTF8.GetString( info));
    //        }
    //    });
    //}
}
