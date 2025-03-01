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
		public static T RandomPop<T>(this IList<T> list, Func<T, float> toRateIndex = null, params float[] rate)
		{
			return Instance.RandomPop(list, toRateIndex, rate);
		}
		public static T RandomPop<T>(this Random random, IList<T> list, Func<T, float> toRateIndex = null, params float[] rate)
		{
			if (list == null || list.Count == 0) return default;
			var index = random.RandomIndex(list, toRateIndex, rate);
			var obj = list[index];
			list.RemoveAt(index);
			return obj;
		}
		public static T RandomGet<T>(this IList<T> list, Func<T, float> toRateIndex = null, params float[] rate)
		{
			return Instance.RandomGet(list, toRateIndex, rate); 
		}
		public static T RandomGet<T>(this Random random, IList<T> list, Func<T, float> toRateIndex = null, params float[] rate)
		{
			if (list == null || list.Count == 0) return default;
			return list[random.RandomIndex(list, toRateIndex, rate)];
		}
		public static int RandomIndex<T>(this IList<T> list, Func<T, float> toRateIndex = null, params float[] rate)
		{
			return Instance.RandomIndex(list, toRateIndex, rate);
		}
		public static int RandomIndex<T>(this Random random, IList<T> list, Func<T, float> toRateIndex = null, params float[] rate)
		{
			if (toRateIndex == null)
			{
				return random.Range(0, list.Count);
			}
			else
			{
				var sum = 0f;
				if (rate.Length > 0)
				{
					var Counts = new QDictionary<int, int>();
					foreach (var item in list)
					{
						Counts[(int)toRateIndex(item)]++;
					}
					for (int i = 0; i < rate.Length; i++)
					{
						if (Counts[i] > 0)
						{
							sum += rate[i];
						}
					}
					var value = random.Range(0, sum);
					if (rate.Length > 0)
					{
						for (int i = 0; i < list.Count; i++)
						{
							var index = (int)toRateIndex(list[i]);
							if (Counts[index] > 0)
							{
								var curValue = rate[index] / Counts[index];
								if (value < curValue)
								{
									return i;
								}
								value -= curValue;
							}
						}
					}
				}
				else
				{
					foreach (var item in list)
					{
						sum += toRateIndex(item);
					}
					var value = random.Range(0, sum);
					for (int i = 0; i < list.Count; i++)
					{
						var curValue = toRateIndex(list[i]);
						if (value < curValue)
						{
							return i;
						}
						value -= curValue;
					}
				}
			}
			return -1;
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
				u = random.Range(-1f, 1f);
				v = random.Range(-1f, 1f);
				w = u * u + v * v;
			} while (w == 0 || w >= 1);
			return u * Mathf.Sqrt((-2 * Mathf.Log(w)) / w);
		}
		public static float Range(this Random random, float min, float max)
		{
			if (random == null) random = Instance;
			//QDebug.Log(" "+random.Next());
			return (float)random.NextDouble() * (max - min) + min;
		}
		public static int Range(this Random random, int min, int max)
		{
			if (random == null) random = Instance;
			//QDebug.Log(" " + random.Next());
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
			var radius = Mathf.Max(size.x, size.y, size.z);
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

	//[QCommandType("基础/随机")]
	//public static class QRandomNode
	//{
	//	[QName("百分比概率")]
	//	private static void PercentRun(QFlowNode This, int percent = 50, [QOutputPort] QFlow True = default, [QOutputPort] QFlow False = default)
	//	{
	//		var value = QRandom.Instance.Range(0, 100);
	//		if (value < percent)
	//		{
	//			This.SetNetFlowPort(nameof(True));
	//		}
	//		else
	//		{
	//			This.SetNetFlowPort(nameof(False));
	//		}
	//	}
	//	[QIgnore]
	//	public static GameObject RandomPointCreate(this GameObject prefab, string pointKey, Transform parent = null)
	//	{
	//		QPositionPoint createPoint = null;
	//		if (parent == null)
	//		{
	//			createPoint = QPositionPoint.GetRandomPoint(pointKey);
	//		}
	//		else
	//		{
	//			var list = new List<QPositionPoint>(parent.GetComponentsInChildren<QPositionPoint>());
	//			list.RemoveAll(point => point.name != pointKey || !point.isActiveAndEnabled);
	//			createPoint = list.RandomGet();
	//		}
	//		if (createPoint == null)
	//		{
	//			QDebug.LogWarning("位置点[" + pointKey + "]不足 ");
	//			return null;
	//		}
	//		return createPoint.CreateAndDisable(prefab, parent);
	//	}
	//	[QName("随机点生成物体")]
	//	private static IEnumerator RandomPointCreate([QInputPort("场景"), QFlowPort] GameObject root, [QName("位置点")] string pointKey, [QName("预制体")] GameObject prefab, [QName("创建数目")] int count = 1, QFlowNode This = null, [QFlowPort, QOutputPort, QName("物体")] GameObject newObject = default)
	//	{
	//		var pointList = new List<QPositionPoint>();
	//		if (root == null)
	//		{
	//			pointList.AddRange(QPositionPoint.GetPoints(pointKey));
	//		}
	//		else
	//		{
	//			pointList.AddRange(root.GetComponentsInChildren<QPositionPoint>());
	//			pointList.RemoveAll((point) => point.name != pointKey);
	//		}
	//		pointList.RandomList();
	//		if (pointList.Count < count)
	//		{
	//			QDebug.LogWarning("位置点[" + pointKey + "]不足 " + pointList.Count + "<" + count);
	//		}
	//		for (int i = 0; i < count && i < pointList.Count; i++)
	//		{
	//			newObject = pointList[i].CreateAndDisable(prefab, root.transform);
	//			if (This != null)
	//			{
	//				This[nameof(newObject)] = newObject;
	//				yield return This.RunPortIEnumerator(nameof(newObject));
	//			}
	//		}
	//	}
	//	[QIgnore]
	//	public static string ToInfoString(QFlowNode node)
	//	{
	//		var info = "";
	//		switch (node.command.method.Name)
	//		{
	//			case nameof(PercentRun):
	//				{
	//					return "{0}%概率".ToLozalizationKey(node.Ports["percent"].ToInfoString()) + " " + node.Ports["True"].ToInfoString();
	//				}
	//			default:
	//				break;
	//		}
	//		info += node.Ports[QFlowKey.NextPort].GetConnectNode()?.ToInfoString();
	//		return info;
	//	}
	//}
}
