using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Reflection;

namespace QTool
{
	public static class QCoroutine
	{
		private static List<IEnumerator> List { set; get; } = new List<IEnumerator>();
		private static List<IEnumerator> AddList { set; get; } = new List<IEnumerator>();
		private static List<IEnumerator> RemoveList { set; get; } = new List<IEnumerator>();
		public static bool IsRunning(this IEnumerator enumerator)
		{
			if (List.Contains(enumerator) || AddList.Contains(enumerator))
			{
				return true;
			}
			return false;
		}
		public static IEnumerator Wait(Func<bool> func)
		{
			while (!func())
			{
				yield return null;
			}
		}
		public static IEnumerator WaitAllOver(this IList<IEnumerator> enumerators)
		{
			yield return Wait(() =>
			{
				bool AllOver = true;
				foreach (var item in enumerators)
				{
					if (item.IsRunning())
					{
						AllOver = false;
						break;
					}
				}
				return AllOver;
			});
		}
		public static void Start(this IEnumerator enumerator)
		{
			if (UpdateIEnumerator(enumerator))
			{
				AddList.Add(enumerator);
			}
		}
		public static void Start(this IEnumerator enumerator,List<IEnumerator> coroutineList)
		{
			coroutineList.Add(enumerator);
			enumerator.OnCallBack(() => coroutineList.Remove(enumerator)).Start();
		}
		public static IEnumerator OnCallBack(this IEnumerator enumerator,Action callBack)
		{
			yield return enumerator;
			callBack?.Invoke();
		}
		public static void Stop(this IEnumerator enumerator)
		{
			RemoveList.Add(enumerator);
		}
		static Dictionary<YieldInstruction, float> YieldInstructionList = new Dictionary<YieldInstruction, float>();
		/// <summary>
		/// 处理Unity内置的等待逻辑
		/// </summary>
		private static bool UpdateIEnumerator(YieldInstruction yieldInstruction)
		{
			if (yieldInstruction is WaitForSeconds waitForSeconds)
			{
				var m_Seconds = (float)waitForSeconds.GetValue("m_Seconds");
				
				if (!YieldInstructionList.ContainsKey(yieldInstruction))
				{
					YieldInstructionList.Add(yieldInstruction, Time.time);
				}
				if (Time.time > YieldInstructionList[yieldInstruction] + m_Seconds)
				{
					YieldInstructionList.Remove(yieldInstruction);
					return false;
				}
				else
				{
					return true;
				}
			}
			else
			{
				Debug.LogError(nameof(QCoroutine) + "暂不支持 " + yieldInstruction + "\n" + QSerializeType.Get(yieldInstruction.GetType()));
				return false;
			}
		}
		/// <summary>
		/// 更新迭代
		/// </summary>
		/// <param name="enumerator">迭代器</param>
		/// <returns>true继续等待 false时结束等待</returns>
		private static bool UpdateIEnumerator(IEnumerator enumerator)
		{
			var start = enumerator.Current;
			//Debug.LogError(start + " " + Time.time);
			if (enumerator.Current is YieldInstruction ie)
			{
				if (UpdateIEnumerator(ie))
				{
					return true;
				}
			}
			else if (enumerator.Current is IEnumerator nextChild)
			{
				if (UpdateIEnumerator(nextChild))
				{
					return true;
				}
			}
			var result = enumerator.MoveNext();
			if (enumerator.Current != null && start != enumerator.Current)
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
			List.RemoveAll(ie => RemoveList.Contains(ie));
			RemoveList.Clear();
			if (AddList.Count > 0)
			{
				List.AddRange(AddList);
				AddList.Clear();
			}
			List.RemoveAll(ie => !UpdateIEnumerator(ie));
		}
		public static void StopAll()
		{
			RemoveList.AddRange(List);
		}
	}
	public class QCoroutineQueue<T>
	{
		private System.Func<T, IEnumerator> action;
		private Queue<T> Queue { get; set; } = new Queue<T>();
		public int Count => Queue.Count;
		public bool IsRunning => Count > 0;
		public T Current => IsRunning ? Queue.Peek() : default;
		public QCoroutineQueue(System.Func<T, IEnumerator> action)
		{
			this.action = action;
		}
		private void Run(T value)
		{
			action(value).OnCallBack(() =>
			{
				Queue.Dequeue();
				if (Queue.Count > 0)
				{
					Run(Queue.Peek());
				}
			}).Start();
		}
		public IEnumerator Start(T value)
		{
			Queue.Enqueue(value);
			if (Queue.Count == 1)
			{
				Run(value);
			}
			while (Queue.Count > 0)
			{
				yield return null;
			}
		}
	}
}
