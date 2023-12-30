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
		public static IEnumerator WaitCountZero<T>(this IList<T> List)
		{
			yield return Wait(() => List.Count == 0);
		}
		public static IEnumerator Start(this IEnumerator enumerator)
		{
			//QDebug.LogError("Start " + enumerator);
			if (UpdateIEnumerator(enumerator))
			{
				AddList.Add(enumerator);
			}
			//else
			//{
			//	QDebug.LogError("Stop " + enumerator);
			//}
			return enumerator;
		}
		/// <summary>
		/// 以同步方式
		/// </summary>
		public static void RunImmediate(this IEnumerator enumerator)
		{
			//QDebug.LogError(enumerator);
			int count = 0;
			while (UpdateIEnumerator(enumerator))
			{
				if (count++ > 10000)
				{
					throw new Exception("协程运行无法立即完成 已循环调用" + count + "次 " + nameof(RunImmediate) + enumerator);
				}
			}
		}
		public static IEnumerator Start(this IEnumerator enumerator, List<IEnumerator> coroutineList)
		{
			IEnumerator ie = null;
			ie = enumerator.OnCallBack(() => coroutineList.Remove(ie));
			coroutineList.Add(ie);
			return ie.Start();
		}
		public static IEnumerator OnCallBack(this IEnumerator enumerator,Action callBack)
		{
			yield return enumerator;
			callBack?.Invoke();
		}
		public static void Stop(this IEnumerator enumerator)
		{
			//QDebug.LogError("Stop " + enumerator);
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
			AddList.Clear();
			RemoveList.Clear();
			List.Clear();
		}
	}
	public class QCoroutineQueue<T>
	{
		private Func<T, IEnumerator> action;
		private Queue<T> Queue { get; set; } = new Queue<T>();
		public int Count => Queue.Count;
		public bool IsRunning => Count > 0;
		public T Current => IsRunning ? Queue.Peek() : default;
		public QCoroutineQueue(Func<T, IEnumerator> action)
		{
			this.action = action;
		}
		private void Run(T value)
		{
			action(value).OnCallBack(() =>
			{
				if (value.Equals(Current))
				{
					Queue.Dequeue();
				}
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
