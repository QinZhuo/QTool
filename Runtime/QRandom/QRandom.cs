using QTool.FlowGraph;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool
{

	public static class QRandom
	{
		public static Color Color(this System.Random random )
		{
			return UnityEngine.Color.HSVToRGB(random.Range(0, 1f), 0.5f, 1);
		}
		public static T RandomPop<T>(this IList<T> list, System.Random random = null)
		{
			if (list == null || list.Count == 0) return default;
			var index = random.Range(0, list.Count);
			var obj = list[index];
			list.RemoveAt(index);
			return obj;
		}
		public static T RandomGet<T>(this IList<T> list, System.Random random=null)
		{
			if (list == null || list.Count == 0) return default;
			return list[random.Range(0, list.Count)];
		}

		public static IList<T> Random<T>(this IList<T> list, System.Random random = null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				var cur = list[i];
				list.Remove(cur);
				list.Insert(random.Range(0, i+1), cur);
			}
			return list;
		}
		/// <summary>
		 /// 正态分布随机数
		 /// </summary>
		public static float NormalRange(this System.Random random,float min,float max)
		{
			if (min == max) return min;
			var mid = (min + max) / 2;
			var offset = max - min;
			return mid + Normal(random) * offset;
		}
		/// <summary>
		/// 正态分布随机数
		/// </summary>
		public static float Normal(this System.Random random)
		{
			var u = 0f;
			var v = 0f;
			var w = 0f;
			do
			{
				u = random.Range(-1, 1);
				v = random.Range(-1, 1);
				w = u * u + v * v;
			} while (w == 0 || w >= 1);
			return u * Mathf.Sqrt((-2 * Mathf.Log(w)) / w);
		}
		public static float Range(this System.Random random,float min,float max)
		{
			if (random == null) return UnityEngine.Random.Range(min, max);
			return (float)(random.NextDouble() % (max - min) + min);
		}
		public static int Range(this System.Random random, int min, int max)
		{
			if (random == null) return UnityEngine.Random.Range(min, max);
			return random.Next() % (max - min) + min;
		}
		public static Vector2 Direction2D(this System.Random random)
		{
			return new Vector2(random.Range(-1, 1f), random.Range(-1, 1f)).normalized;
		}
		public static Vector3 Direction(this System.Random random)
		{
			return new Vector3(random.Range(-1, 1f), random.Range(-1, 1f), random.Range(-1, 1f)).normalized;
		}
	}

	[QCommandType("基础/程序生成")]
	public static class QRandomNode
	{
		public static System.Random Random =null;
		[QIgnore]
		public static IEnumerator RandomRangeCarete<T>(QFlowNode This, [QInputPort("场景"), QFlowPort] GameObject root, [QName("范围")] float range=10, [QName("中心偏移")] float centerOffset = 0, [QName("预制体")] GameObject prefab = null, [QName("创建数目")] int count = 1, [QOutputPort, QFlowPort] GameObject newObject = default) where T:Component
		{
			var center = root == null ? Vector3.zero : root.transform.position;
			for (int i = 0; i < count; i++)
			{
				var creating = true;
				while (creating)
				{
					var dir = Random.Direction2D();
					var offset = dir.normalized * centerOffset + dir;
					var position = center + new Vector3(offset.x, 0, offset.y);
					if (new Ray(position + Vector3.up, Vector3.down).RayCast<T>() == null)
					{
						newObject = prefab.CheckInstantiate();
						newObject.transform.position = position;
						if (This != null)
						{
							This[nameof(newObject)] = newObject;
							yield return This.RunPortIEnumerator(nameof(newObject));
						}
						creating = false;
					}
				}
			}
		}
		[QName("随机点生成物体")]
		private static IEnumerator RandomPointCreate(QFlowNode This, [QInputPort("场景"), QFlowPort] GameObject root, [QName("位置点")] string pointKey, [QName("预制体")] GameObject prefab, [QName("创建数目")] int count = 1, [QOutputPort, QFlowPort] GameObject newObject = default)
		{
			var pointList = new List<QPositionPoint>();
			if (root == null)
			{
				pointList.AddRange(QPositionPoint.GetPoints(pointKey));
				pointList.RemoveAll((point) => point.HasChild);
			}
			else
			{
				var keyPotnts= QPositionPoint.GetPoints(pointKey);
				pointList.AddRange(root.GetComponentsInChildren<QPositionPoint>());
				pointList.RemoveAll((point) => !keyPotnts.Contains(point) || point.HasChild);
			}
			pointList.Random(Random);
			if (pointList.Count < count)
			{
				QDebug.LogWarning("位置点[" + pointKey + "]不足 " + pointList.Count + "<" + count);
			}
			for (int i = 0; i < count && i < pointList.Count; i++)
			{
				newObject = prefab.CheckInstantiate(pointList[i].transform);
				newObject.transform.rotation = pointList[i].transform.rotation;
				if (This != null)
				{
					This[nameof(newObject)] = newObject;
					yield return This.RunPortIEnumerator(nameof(newObject));
				}
			}
		}
	}

}
