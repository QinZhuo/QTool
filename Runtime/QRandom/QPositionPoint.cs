using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QPositionPoint : MonoBehaviour
	{
		private static QDictionary<string, List<QPositionPoint>> Points = new QDictionary<string, List<QPositionPoint>>((key) => new List<QPositionPoint>());
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
		public static QPositionPoint GetRandomPoint(string key)
		{
			if (Points.ContainsKey(key))
			{
				return Points[key].RandomGet();
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
		private void OnEnable()
		{
			Points[name].Add(this);
		}
		private void OnDisable()
		{
			if (Points.ContainsKey(name))
			{
				Points[name].Remove(this);
			}
		}
		public GameObject CreateAndDisable(GameObject prefab, Transform parent = null)
		{
			var newObject = prefab.CheckInstantiate(parent);
			newObject.transform.position = transform.position;
			enabled = false;
			return newObject;
		}
		private void OnDrawGizmos()
		{
			Gizmos.color = name.ToColor();
			Gizmos.DrawSphere(transform.position, 0.5f);
		}
	}
}

