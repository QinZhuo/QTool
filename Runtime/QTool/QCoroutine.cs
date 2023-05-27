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
		private static bool UpdateIEnumerator(IEnumerator enumerator)
		{
			var start = enumerator.Current;
			if (enumerator.Current is IEnumerator nextChild)
			{
				if (UpdateIEnumerator(nextChild))
				{
					return true;
				}
			}
			var result = enumerator.MoveNext();
			if (start != enumerator.Current)
			{
				return UpdateIEnumerator(enumerator);
			}
			else
			{
				return result;
			}
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