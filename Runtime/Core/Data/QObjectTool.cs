using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using QTool.Reflection;
using System;
using System.Runtime.CompilerServices;
namespace QTool
{
	
	public static class QObjectTool
	{
		public const string ResourcesKey = "/" + nameof(Resources) + "/";
		public static string GetPath(UnityEngine.Object obj) {
			if (obj != null) {
				if (obj is GameObject gObj) {
					obj = gObj;
				}
				else if (obj is Component monoObj) {
					obj = monoObj.gameObject;
				}
#if UNITY_EDITOR
				if (obj.IsAsset()) {
					var key = UnityEditor.AssetDatabase.GetAssetPath(obj);
					if (key.Contains(ResourcesKey)) {
						key = key.SplitEndString(ResourcesKey).WithoutExtension();
					}
					else {
						Debug.LogError($"无法引用非{ResourcesKey}目录下的文件");
						return null;
					}
					return key;
				}
#endif
			}
			return string.Empty;
		}
		internal static async Task<UnityEngine.Object> LoadObjectAsync(string key, Type type)
		{
			var id = key;
			if (id.IsNull()) return null;
			if (id.StartsWith("{") && id.EndsWith("}"))
			{
				id = id.GetBlockValue(":\"", "\"}");
			}
			var path = GetResourcesPath(id);
			var obj = await Resources.LoadAsync(path);
			if (obj != null && !obj.GetType().Is(type))
			{
				obj = await Resources.LoadAsync(path, type);
			}
			if (obj == null && !id.IsNull() && id != "null")
			{
				Debug.LogError("异步加载资源[" + key + "](" + type + ")失败 资源为空");
			}
			return obj;
		}
		public static string GetResourcesPath(string id) {
			var loadPath = id;
			if (id.Contains(ResourcesKey)) {
				loadPath = id.SplitEndString(ResourcesKey);
				if (loadPath.Contains(".")) {
					loadPath = loadPath.WithoutExtension();
				}
			}
			else {
				loadPath = id.WithoutExtension();
			}
			return loadPath;
		}
		public static UnityEngine.Object GetObject(string id, Type type) {

			if (type.Is(typeof(Component))) {
				return GetObject<GameObject>(id)?.GetComponent(type);
			}
			if (string.IsNullOrWhiteSpace(id))
				return null;
			UnityEngine.Object obj = null;
			var loadPath = GetResourcesPath(id);
			try {
				obj = Resources.Load(loadPath);
			}
			catch (Exception e) {
				Debug.LogWarning(e);
			}
			if (obj != null) {
				return obj;
			}
			QDebug.LogWarning($"未找到[{id}][{loadPath}]{type}");
			return null;
		}
		public static T GetObject<T>(string id) where T : UnityEngine.Object
		{
			return GetObject(id, typeof(T)) as T;
		}

		#region GetAwaiter
		public static AsyncOperationAwaiter GetAwaiter(this AsyncOperation asyncOperation) {
			return new AsyncOperationAwaiter(asyncOperation);
		}
		public struct AsyncOperationAwaiter : INotifyCompletion {
			AsyncOperation asyncOperation;
			public AsyncOperationAwaiter(AsyncOperation asyncOperation) {
				this.asyncOperation = asyncOperation;
			}
			public bool IsCompleted => asyncOperation == null || asyncOperation.isDone;

			public void GetResult() {

			}

			public void OnCompleted(Action continuation) {
				asyncOperation.completed += (asyncOperation) => {
					continuation?.Invoke();
				};
			}
		}
		public static ResourceRequestAwaiter GetAwaiter(this ResourceRequest resourceRequest) {
			return new ResourceRequestAwaiter(resourceRequest);
		}


		public struct ResourceRequestAwaiter : INotifyCompletion {
			ResourceRequest resourceRequest;
			public ResourceRequestAwaiter(ResourceRequest resourceRequest) {
				this.resourceRequest = resourceRequest;
			}
			public bool IsCompleted => resourceRequest.isDone;

			public UnityEngine.Object GetResult() {
				return resourceRequest?.asset;
			}

			public void OnCompleted(Action continuation) {
				resourceRequest.completed += (resourceRequest) => {
					continuation?.Invoke();
				};
			}
		}
		#endregion

	}

}
