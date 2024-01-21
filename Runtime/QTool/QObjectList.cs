using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool
{
	[ExecuteInEditMode]
	public class QObjectList : MonoBehaviour
	{
		[QName("预制体")]
		public GameObject prefab;
		[QName("测试数目")]
		public int TestCount = 4;
		public List<GameObject> List { get; private set; } = new List<GameObject>();
		public int Count => List.Count;
		

		public virtual GameObject this[string name]
		{
			get
			{
				if (name.IsNull())
				{
					Debug.LogError(this + "索引为空[" + name + "]");
					return null;
				}
				var trans = transform.Find(name);
				var view = (trans != null && trans.gameObject.activeSelf) ? trans.gameObject : null;
				if (view == null)
				{
					view = QPoolManager.Get(prefab);
					view.transform.SetParent(transform, false);
					view.transform.localScale = Vector3.one;
					view.transform.localRotation = Quaternion.identity;
					view.transform.SetAsLastSibling();
					List.Add(view);
					view.name = name;
					OnCreate?.Invoke(view);
					var poolObj = view.GetComponent<QPoolObject>();
					UnityEngine.Events.UnityAction action = null;
					action = () =>
					 {
						 List.Remove(view);
						 OnRelease?.Invoke(view);
						 poolObj.OnRelease.RemoveListener(action);
					 };
					poolObj.OnRelease.AddListener(action);
				}
				return view;
			}
		}
		public GameObject Get(string name, GameObject prefab)
		{
			this.prefab = prefab;
			return this[name];
		}
		public virtual void Clear()
		{
			for (int i = List.Count - 1; i >= 0; i--)
			{
				var view = List[i];
				view.PoolRelease();
			}
			OnClear?.Invoke();
		}
		public event System.Action<GameObject> OnCreate;
		public event System.Action<GameObject> OnRelease;
		public event System.Action OnClear;
#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if (transform is RectTransform rectTransform)
			{
			
				if (UnityEditor.Selection.activeGameObject != gameObject)
				{
					Gizmos.color = name.ToColor().Lerp(Color.clear, 0.7f);
					Gizmos.DrawCube(rectTransform.Center(), rectTransform.Size());
				}
				else
				{
					Gizmos.color = name.ToColor().Lerp(Color.clear, 0.8f);
					Gizmos.DrawCube(rectTransform.Center(), rectTransform.Size());
				}
			}
		}
	
		private int lastCount = 0;
		private void Update()
		{
			if (!Application.isPlaying && prefab != null && TestCount != lastCount)
			{
				lastCount = TestCount;
				OnDestroy(); 
				for (int i = 0; i < TestCount; i++)
				{
					var obj = prefab.CheckInstantiate(transform);
					obj.hideFlags = HideFlags.HideAndDontSave;
					obj.name = prefab.name + "测试";
				}
			}
		}
		private void OnDestroy()
		{
			if (prefab == null) return;
			transform.ClearChild(child => child.name == prefab.name + "测试");
		}
#endif
	}

}
