using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace QTool
{
	public class QObjectList : MonoBehaviour {
		[QName("预制体")]
		public GameObject prefab;
		public QDictionary<int, GameObject> List { get; private set; } = new QDictionary<int, GameObject>();
		public int Count => List.Count - clearList.Count;
		public Transform ListParent => prefab?.transform?.parent == null ? transform : prefab.transform.parent;

		public virtual GameObject this[int index] {
			get {
				var view = List[index];
				if (view == null) {
					view = QGameObjectPool.Get(prefab);
					view.transform.SetParent(ListParent, false);
					view.transform.SetAsLastSibling();
					List[index] = view;
					view.name = name;
					OnCreate?.Invoke(view);
					var poolObj = view.GetComponent<QPoolObject>();
					UnityEngine.Events.UnityAction action = null;
					action = () => {
						List.Remove(index);
						OnRelease?.Invoke(view);
						poolObj.OnRelease.RemoveListener(action);
					};
					poolObj.OnRelease.AddListener(action);
				}
				else {
					if (clearList.Count > 0) {
						clearList.Remove(view);
						view.transform.SetAsLastSibling();
					}
				}
				return view;
			}
		}
		public virtual GameObject this[string name] {
			get {
				if (name.IsNull()) {
					Debug.LogError(this + "索引为空[" + name + "]");
					return null;
				}
				return this[name.GetHashCode()];
			}
		}

		private void Awake() {
			if (prefab != null && prefab.transform.parent != null) {
				prefab.gameObject.SetActive(false);
			}
		}
		public GameObject Get(string name) => this[name];
		public GameObject Get(string name, GameObject prefab) {
			this.prefab = prefab;
			return this[name];
		}
		private List<GameObject> clearList = new List<GameObject>();
		public void DelayClear() {
			foreach (var item in clearList) {
				clearList.AddCheckExist(item);
			}
			QToolManager.Instance.OnUpdate += ClearUpdate;
		}
		private void ClearUpdate() {
			clearList.PoolReleaseList();
			QToolManager.Instance.OnUpdate -= ClearUpdate;
		}
		public virtual void Clear() {
			for (int i = List.Count - 1; i >= 0; i--) {
				var view = List[i];
				view.PoolRelease();
			}
		}
		public event System.Action<GameObject> OnCreate;
		public event System.Action<GameObject> OnRelease;
	}
}
