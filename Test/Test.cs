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
    public class TestClass
    {
        public int p1 {  get;  set; }
        public float p2;
        public int this[int x]
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }
        public int this[int x,int y]
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }
    }
    public Vector3 v3 = new Vector3();
    public List<int> list = new List<int> { 123,45,56 };
    public List<int> blist = new List<int> { 123, 45, 56 };
    public TestClass a = new TestClass { };
    public int[] b;
    public byte[] info;
    [ContextMenu("test")]
    public void TestFunc()
    {
        info = QSerialize.Serialize(list);
     //   Debug.LogError(QSerialize.Deserialize<Vector3>(info));
    

    }
}
