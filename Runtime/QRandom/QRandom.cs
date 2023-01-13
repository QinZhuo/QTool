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
			var index = random.Range(0, list.Count);
			var obj= list[index];
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



}
