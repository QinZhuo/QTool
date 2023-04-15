using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{

	[DisallowMultipleComponent,RequireComponent(typeof(MeshFilter)),RequireComponent(typeof(MeshRenderer))]
	public class QAngleMesh : MonoBehaviour
	{
		public QMeshData MeshData { private set; get; } = new QMeshData();
		[QName("角度"), Range(0, 360)]
		public float angle = 30;
		[QName("距离"), Range(0, 30)]
		public float distance = 5;
		private void OnValidate()
		{
			UpdateMesh();
		}
		public static Vector3 GetPos(float angle,float distance)
		{
			return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad))* distance;
		}
		public void UpdateMesh()
		{
			MeshData.Clear();
			var halfAngle = angle / 2;
			for (float curAngle = -halfAngle; curAngle < halfAngle; curAngle+=5)
			{
				MeshData.AddTriangle(transform.position, GetPos(curAngle, distance), GetPos(Mathf.Min(curAngle + 10,halfAngle), distance), Color.red);
			}
			gameObject.GenerateMesh(MeshData);
		}
	}
}
