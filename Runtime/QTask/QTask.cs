using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{
	public class QTaskCancelException : Exception
	{
		public override string ToString()
		{
			return "QTask 异步逻辑被取消运行";
		}
	}
	public static class QTask
	{
		static Dictionary<string, Task> OnlyOneRun = new Dictionary<string, Task>();
		/// <summary>
		/// 保证只有一个相同Key的Task正在运行
		/// </summary>
		public static async Task RunOnlyOne(string onlyOneKey, Func<Task> taskFunc)
		{
			if (!OnlyOneRun.ContainsKey(onlyOneKey))
			{
				lock (OnlyOneRun)
				{
					OnlyOneRun.Add(onlyOneKey,taskFunc());
				}
				await OnlyOneRun[onlyOneKey];
				lock (OnlyOneRun)
				{
					OnlyOneRun.RemoveKey(onlyOneKey);
				}
			}
			else
			{
				await OnlyOneRun[onlyOneKey];
			}
		}
		public static async Task Run(this Task task)
		{
			Exception exception = null;
			try
			{
				await task;
			}
			catch (Exception e)
			{
				exception = e;
			}
			if (exception != null)
			{
				if(exception is QTaskCancelException)
				{ 
					throw exception;
				}
				else
				{
					Debug.LogError(exception.ToShortString(2000));
				}
			}
			else if(task.Exception!=null)
			{
				Debug.LogError(task.Exception.ToShortString(2000));
			}
		}
		public static async Task<T> Run<T>(this Task<T> task)
		{
			_=(task as Task).Run();
			return await task;
		}
		public static async Task Run(this Task task,Func<Task>  nextAction)
		{
			await task.Run();
			await nextAction();
		}
		public static async Task Run(this Task task, Action nextAction)
		{
			await task.Run();
			nextAction();
		}
		public static int RunningFlag { get; private set; } = QId.NewId().GetHashCode();
		public static void StopAllWait()
		{
			RunningFlag = QId.NewId().GetHashCode();
		}
		public static async Task WaitAllOver(this IList<Task> tasks)
		{
			foreach (var task in tasks)
			{
				if (task == null) continue;
				await task.Run();
			}
		}
		public static async Task WaitAllOver(params Task[] tasks)
		{
			await tasks.WaitAllOver();
		}
		public static async Task WaitAnyOver(this IList<Task> tasks)
		{
			foreach (var task in tasks)
			{
				_ = task.Run();
			}
			while (true)
			{
				foreach (var task in tasks)
				{
					if (task.IsCompleted)
					{
						return;
					}
				}
				await Step();
			}
		}
		public static async Task WaitAnyOver(params Task[] tasks)
		{
			await tasks.WaitAnyOver();
		}
		public static async Task Step()
		{
			await Task.Yield();
		}
		public static async Task Wait(float second, bool ignoreTimeScale = false)
		{
			if (Application.isPlaying)
			{
				var startTime = (ignoreTimeScale ? Time.unscaledTime : Time.time);
				await Wait(() => startTime + second <= (ignoreTimeScale ? Time.unscaledTime : Time.time));
			}
			else
			{
				var startTime = DateTime.Now;
				await Wait(() =>(DateTime.Now-startTime).TotalSeconds>second );
			}
		}
		public static async Task Wait(Func<bool> flagFunc)
		{
			var flag = RunningFlag;
			var playingFlag = Application.isPlaying;
			if (flagFunc == null) return;
			while (!flagFunc.Invoke())
			{
				await Step();
				if (!playingFlag.Equals(Application.isPlaying) || !RunningFlag.Equals(flag))
				{
					throw new QTaskCancelException();
				}
			}
		}
		public static async Task<bool> IsCancel(this Task task)
		{
			Exception exception = null;
			try
			{
				await task.Run();
			}
			catch (Exception e)
			{
				exception = e;
			}
			return exception is QTaskCancelException;
		}

	
#if UNITY_EDITOR
		public static PackageRequestAwaiter GetAwaiter(this UnityEditor.PackageManager.Requests.Request request)
		{
			return new PackageRequestAwaiter(request);
		}
		public struct PackageRequestAwaiter : IAwaiter
		{
			UnityEditor.PackageManager.Requests.Request request;
			public PackageRequestAwaiter(UnityEditor.PackageManager.Requests.Request request)
			{
				this.request = request;
			}
			public bool IsCompleted => request.IsCompleted;

			public void GetResult()
			{
			}

			public async void OnCompleted(Action continuation)
			{
				while (!request.IsCompleted)
				{
					await Step();
				}
				continuation?.Invoke();
			}
		}
#endif
		public static IEnumeratorAwaiter GetAwaiter(this IEnumerator target)
		{
			return new IEnumeratorAwaiter(target);
		}
		public class IEnumeratorAwaiter : IAwaiter
		{
			IEnumerator target;
			public IEnumeratorAwaiter(IEnumerator target)
			{
				this.target = target;
			}
			public bool IsCompleted { get; private set; } = false;

			public void GetResult()
			{
			}

			public async void OnCompleted(Action continuation)
			{
				while (target.MoveNext())
				{
					if (target.Current is WaitForSeconds waitForSeconds)
					{
						var m_Seconds = (float)waitForSeconds.GetValue("m_Seconds");
						if (await Wait(m_Seconds).IsCancel())
						{ 
							return;
						}
					}
					else if(target.Current is WaitForSecondsRealtime waitRealtime)
					{
						if (await Wait(waitRealtime.waitTime, true).IsCancel())
						{
							return;
						}
					}
					else
					{
						Debug.LogError("不支持 "+target.Current);
						typeof(WaitForSeconds).ForeachMemeber((file) =>
						{
							Debug.LogError(file.Name);
						}, (member) =>
						{
							Debug.LogError(member.Name);
						});
						await Step();
					}
				}
				IsCompleted = true;
				continuation?.Invoke();
			}
		}
		public static AsyncOperationAwaiter GetAwaiter(this AsyncOperation asyncOperation)
		{
			return new AsyncOperationAwaiter(asyncOperation);
		}
		public struct AsyncOperationAwaiter : IAwaiter
		{
			AsyncOperation asyncOperation;
			public AsyncOperationAwaiter(AsyncOperation asyncOperation)
			{
				this.asyncOperation = asyncOperation;
			}
			public bool IsCompleted => asyncOperation==null|| asyncOperation.isDone;

			public void GetResult()
			{
				
			}

			public void OnCompleted(Action continuation)
			{
				asyncOperation.completed  += (asyncOperation) =>
				{
					continuation?.Invoke();
				};
			}
		}
		public static ResourceRequestAwaiter GetAwaiter(this ResourceRequest resourceRequest)
		{
			return new ResourceRequestAwaiter(resourceRequest);
		}
#region ResourceRequestAwaiter

		public struct ResourceRequestAwaiter : IAwaiter<UnityEngine.Object>
		{
			ResourceRequest resourceRequest;
			public ResourceRequestAwaiter(ResourceRequest resourceRequest)
			{
				this.resourceRequest = resourceRequest;
			}
			public bool IsCompleted => resourceRequest.isDone;

			public UnityEngine.Object GetResult()
			{
				return resourceRequest?.asset;
			}

			public void OnCompleted(Action continuation)
			{
				resourceRequest.completed += (resourceRequest) =>
				{
					continuation?.Invoke();
				};
			}
		}
#endregion
	}

	public interface IAwaiter : INotifyCompletion
	{

		public bool IsCompleted { get; }

		public void GetResult();
	}
	public interface IAwaiter<T>: INotifyCompletion
	{

		public bool IsCompleted { get; }

		public T GetResult();
	}
}
