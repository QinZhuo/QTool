using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QPositionPoint : MonoBehaviour
	{
		private static QDictionary<string, List<QPositionPoint>> Points = new QDictionary<string, List<QPositionPoint>>((key)=>new List<QPositionPoint>());
		public static QPositionPoint GetPoint(string key)
		{
			if (Points.ContainsKey(key))
			{
				return Points[key].Get(0);
			}
			else
			{
				return null;
			}
		}
		public static List<QPositionPoint> GetPoints(string key)
		{
			return Points[key];
		}
		private void OnValidate()
		{
			name = gameObject.QName();
		}
		private void Awake()
		{
			name = gameObject.QName();
			Points[name].Add(this);
		}
		private void OnDestroy()
		{
			if (Points.ContainsKey(name))
			{
				Points[name].Remove(this);
			}
		}

		public bool HasChild => transform.childCount > 0;

		private void OnDrawGizmos()
		{
			Gizmos.color = name.ToColor();
			Gizmos.DrawSphere(transform.position, 0.2f);
		}
	}
	
}

