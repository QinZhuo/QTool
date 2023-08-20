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
		public bool is2D = false;
		private void OnValidate()
		{
			UpdateMesh();
		}
		public Vector3 GetPos(float angle,float distance)
		{
			if (is2D)
			{
				return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * distance;
			}
			else
			{
				return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad)) * distance;
			}
		}
		public void UpdateMesh()
		{
		
			List<Vector2> points = new List<Vector2>();
			points.Add(transform.position);
			MeshData.Clear();
			var halfAngle = angle / 2;
			for (float curAngle = -halfAngle; curAngle < halfAngle; curAngle+=5)
			{
				var start = transform.position + GetPos(curAngle, distance);
				var end = transform.position + GetPos(Mathf.Min(curAngle + 10, halfAngle), distance);
				MeshData.AddTriangle(transform.position, start, end, Color.red);
				if (is2D)
				{
					points.AddCheckExist(start);
					points.AddCheckExist(end);
				}
			}
			gameObject.GenerateMesh(MeshData);
			if (is2D)
			{
				var polygon = GetComponent<PolygonCollider2D>();
				if (polygon != null)
				{
					polygon.points = points.ToArray();
				}
			}
		}
	}
}
