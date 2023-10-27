using QTool.FlowGraph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace QTool
{

	public static class QRandom
	{
		public static Random Instance { get; set; } = new Random();
		public static Color Color(this Random random)
		{
			return UnityEngine.Color.HSVToRGB(random.Range(0, 1f), 0.5f, 1);
		}
		public static T RandomPop<T>(this IList<T> list, Random random = null)
		{
			return RandomPop(random, list);
		}
		public static T RandomPop<T>(this Random random, IList<T> list)
		{
			if (list == null || list.Count == 0) return default;
			var index = random.Range(0, list.Count);
			var obj = list[index];
			list.RemoveAt(index);
			return obj;
		}
		public static T RandomGet<T>(this IList<T> list, Random random = null)
		{
			return RandomGet(random,list);
		}
		public static T RandomGet<T>(this Random random, IList<T> list)
		{
			if (list == null || list.Count == 0) return default;
			return list[random.Range(0, list.Count)];
		}
		public static IList<T> RandomList<T>(this IList<T> list, Random random = null)
		{
			return RandomList(random, list);
		}
		public static IList<T> RandomList<T>(this Random random, IList<T> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				var cur = list[i];
				list.Remove(cur);
				list.Insert(random.Range(0, i + 1), cur);
			}
			return list;
		}
		public static void Split(this Random random, int sum, params Action<int>[] actions)
		{
			if (actions.Length == 0) return;
			List<Action<int>> actionList = new List<Action<int>>(actions);
			random.RandomList(actionList);
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
		public static float NormalRange(this Random random, float min, float max)
		{
			if (min == max) return min;
			var mid = (min + max) / 2;
			var offset = max - min;
			return mid + Normal(random) * offset;
		}
		/// <summary>
		/// 正态分布随机数
		/// </summary>
		public static float Normal(this Random random)
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
		public static float Range(this Random random, float min, float max)
		{
			if (random == null) random = Instance;
			return (float)random.NextDouble() * (max - min) + min;
		}
		public static int Range(this Random random, int min, int max)
		{
			if (random == null) random = Instance;
			return random.Next(min, max);
		}
		public static Vector2 Vector2(this Random random, float range = 1, float centerOffset = 0)
		{
			var dir = new Vector2(random.Range(-1, 1f), random.Range(-1, 1f)).normalized;
			return dir * random.Range(centerOffset, range);
		}
		public static Vector3 Vector3(this Random random, float range = 1, float centerOffset = 0)
		{
			var dir = new Vector3(random.Range(-1, 1f), random.Range(-1, 1f), random.Range(-1, 1f)).normalized;
			return dir * random.Range(centerOffset, range);
		}

		public static int MaxRandomTimes { get; set; } = 1000;
		public static Vector3 RandomPlacePosition<T>(this T target, Func<Vector3> RandomPosition, Func<Vector3, bool> CanPlace = null) where T : Component
		{
			var size = target.GetBounds().size;
			var radius = Mathf.Max(size.x,size.y,size.z);
			var is2D = target.GetComponent<Collider2D>() != null;
			Vector3 position = default;
			for (int times = 0; times < MaxRandomTimes; times++)
			{
				position = RandomPosition();
				var other = is2D ? ((Vector2)position).OverlapCircle<T>(radius, obj => obj != target) : new Ray(position + UnityEngine.Vector3.up, UnityEngine.Vector3.down).RayCast<T>(radius, obj => obj != target);
				if (other == null && (CanPlace == null || CanPlace(position)))
				{
					return position;
				}
			}
			QDebug.LogError(target + " 随机位置失败 " + radius + " " + position);
			return position;
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
			QRandom.Instance.RandomList(pointList);
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
