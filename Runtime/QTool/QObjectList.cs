using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool
{
	public class QObjectList : MonoBehaviour
	{
		[QName("预制体")]
		public GameObject prefab;
		public List<GameObject> List { get; private set; } = new List<GameObject>();
		public int Count => List.Count;

		public virtual GameObject this[int index]
		{
			get
			{
				return this[index.ToString()];
			}
		}
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
					view = QGameObjectPool.Get(prefab);
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

		private void Awake()
		{
			if (prefab != null && prefab.transform.parent == transform)
			{
				prefab.gameObject.SetActive(false);
			}
		}
		public GameObject Get(string name) => this[name];
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
#endif
	}
}
