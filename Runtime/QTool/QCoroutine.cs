using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public static class QCoroutine
	{
		private static List<IEnumerator> List { set; get; } = new List<IEnumerator>();
		private static List<IEnumerator> AddList { set; get; } = new List<IEnumerator>();
		public static void Start(this IEnumerator enumerator)
		{
			if (UpdateIEnumerator(enumerator))
			{
				AddList.Add(enumerator);
			}
		}
		public static void Stop(this IEnumerator enumerator)
		{
			List.Remove(enumerator);
		}
		public static void Complete(this IEnumerator enumerator)
		{
			int count = 0;
			while (UpdateIEnumerator(enumerator))
			{
				if (++count > 1000)
				{
					throw new System.Exception(nameof(QCoroutine) + "." + nameof(Complete) + " 超过最大迭代次数" + 1000);
				}
			}
		}
		private static bool UpdateIEnumerator(IEnumerator enumerator)
		{
			var result = enumerator.MoveNext();
			if (enumerator.Current is IEnumerator child)
			{
				if (UpdateIEnumerator(child)) return true;
			}
			return result;
		}
		public static void Update()
		{
			if (AddList.Count > 0)
			{
				List.AddRange(AddList);
				AddList.Clear();
			}
			List.RemoveAll((ie) => !UpdateIEnumerator(ie));
		}
		public static void StopAll()
		{
			List.Clear();
		}
	}
}
