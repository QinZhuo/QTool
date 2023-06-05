using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
public class QMeshTest : MonoBehaviour
{
	public Vector3 normal = Vector3.up;
	[QName]
	public void Test()
	{
		gameObject.Split(transform.position, normal,true);
	}
}
