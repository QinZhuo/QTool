using QTool.FlowGraph;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool
{

	public static class QRandom
	{
		public static System.Random DefaultRandom = null;
		public static Color Color(this System.Random random)
		{
			return UnityEngine.Color.HSVToRGB(random.Range(0, 1f), 0.5f, 1);
		}
		public static T RandomPop<T>(this IList<T> list)
		{
			if (list == null || list.Count == 0) return default;
			var index = DefaultRandom.Range(0, list.Count);
			var obj = list[index];
			list.RemoveAt(index);
			return obj;
		}
		public static T RandomGet<T>(this IList<T> list)
		{
			if (list == null || list.Count == 0) return default;
			return list[DefaultRandom.Range(0, list.Count)];
		}

		public static IList<T> Random<T>(this IList<T> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				var cur = list[i];
				list.Remove(cur);
				list.Insert(DefaultRandom.Range(0, i + 1), cur);
			}
			return list;
		}
		public static void Split(this System.Random random, int sum, params Action<int>[] actions)
		{
			if (actions.Length == 0) return;
			List<Action<int>> actionList = new List<Action<int>>(actions);
			actionList.Random();
			for (int i = 0; i < actionList.Count - 1; i++)
			{
				var value = random.Range(0, sum + 1);
				actionList[i](value);
				sum -= value;
			}
			actionList[actionList.Count - 1](sum);
		}
		/// <summary>
		/// 正态分布随机数
		/// </summary>
		public static float NormalRange(this System.Random random, float min, float max)
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
		public static float Range(this System.Random random, float min, float max)
		{
			if (random == null) return UnityEngine.Random.Range(min, max);
			return (float)random.NextDouble() * (max - min) + min;
		}
		public static int Range(this System.Random random, int min, int max)
		{
			if (random == null) return UnityEngine.Random.Range(min, max);
			return random.Next(min, max);
		}
		public static Vector2 Vector2(this System.Random random)
		{
			return new Vector2(random.Range(-1, 1f), random.Range(-1, 1f));
		}
		public static Vector3 Vector3(this System.Random random)
		{
			return new Vector3(random.Range(-1, 1f), random.Range(-1, 1f), random.Range(-1, 1f));
		}
		public static int MaxRandomTimes { get; set; } = 10;
		public static void RandomRangeCreate<T>(this GameObject root, float range = 10, float centerOffset = 0, GameObject prefab = null, Action<GameObject> callBack = null) where T : Component
		{
			var center = root == null ? UnityEngine.Vector3.zero : root.transform.position;
			var is2D = prefab.GetComponent<Collider2D>() != null;
			var radius = prefab.GetBounds().size.magnitude;

			var creating = true;
			var times = 0;
			while (creating && times++ < MaxRandomTimes)
			{
				var dir = DefaultRandom.Vector2();
				var offset = dir.normalized * centerOffset + dir * (range - centerOffset);
				var position = center + (is2D ? offset : new Vector3(offset.x, 0, offset.y));
				var other = is2D ? ((Vector2)position).OverlapCircle<T>(radius) : new Ray(position + UnityEngine.Vector3.up, UnityEngine.Vector3.down).RayCast<T>(radius);
				if (other == null)
				{
					var newObject = prefab.CheckInstantiate();
					newObject.transform.position = position;
					callBack?.Invoke(newObject);
					creating = false;
				}
			}
		}
	}

	[QCommandType("基础/程序生成")]
	public static class QRandomNode
	{
		[QName("随机点生成物体")]
		private static IEnumerator RandomPointCreate(QFlowNode This, [QInputPort("场景"), QFlowPort] GameObject root, [QName("位置点")] string pointKey, [QName("预制体")] GameObject prefab, [QName("创建数目")] int count = 1, [QFlowPort, QOutputPort, QName("物体")] GameObject newObject = default)
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
			pointList.Random();
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
