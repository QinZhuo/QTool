using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
using QTool.Inspector;
public class QPoolTest : MonoBehaviour
{
	[QToggle("使用对象池")]
	public bool usePool = true;
	public GameObject prefab;
	public int size = 10;
	List<GameObject> objList = new List<GameObject>();
	int count;
	
	private void Awake()
	{
		Application.targetFrameRate = 60;
		Pool= QPoolManager.GetPool("测试Pool", prefab);
	}
	public ObjectPool<GameObject> Pool;
	private void FixedUpdate()
	{
		if (count >= 10)
		{
			foreach (var item in objList)
			{
				if (usePool)
				{
					Pool.Push(item);
				}
				else
				{
					Destroy(item);
				}
			}
			objList.Clear();
			for (int i = 0; i <Random.Range(0, size); i++)
			{
				if (usePool)
				{ 
					var obj =Pool.Get();
					obj.transform.position = Vector3.right * i;
					objList.Add(obj);
				}
				else
				{
					objList.Add(Instantiate(prefab, Vector3.right * i, Quaternion.identity));
				}
				
			}
			
			count = 0;
		}
		
		count++;
	}
}
